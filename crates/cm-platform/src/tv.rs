//! TV control over the network (1.7.1/1.8 AndroidTvController, AdbClient, TvNetwork, WakeOnLan):
//! Google TV / Android TV with ADB on port 5555 ("USB debugging" / "Network debugging" on the
//! TV), nothing to install on the PC. Wakes the TV (Wake-on-LAN first when it is in deep standby
//! and a MAC is set), presses "HDMI n" or runs the user's command, and on the way back sends the
//! user's command and/or standby.
//!
//! This PC's ADB key (RSA-2048, Windows CNG) is kept in `adbkey` in the data folder, encrypted
//! with DPAPI (see `secret`); the first time, the TV asks "Allow debugging from this computer?".
//! The key is never logged. The rules and the wire format are in `cm_core::tv`.

use std::{
    io::{ErrorKind, Read, Write},
    net::{TcpStream, ToSocketAddrs, UdpSocket},
    path::Path,
    sync::Mutex,
    time::{Duration, Instant},
};

use cm_core::tv::{self as core, TvControl, TvError, adb};
use windows::Win32::Security::Cryptography::{
    BCRYPT_ALG_HANDLE, BCRYPT_KEY_HANDLE, BCRYPT_OPEN_ALGORITHM_PROVIDER_FLAGS, BCRYPT_PAD_PKCS1, BCRYPT_PKCS1_PADDING_INFO, BCRYPT_RSA_ALGORITHM,
    BCRYPT_RSAFULLPRIVATE_BLOB, BCRYPT_SHA1_ALGORITHM, BCryptCloseAlgorithmProvider, BCryptDestroyKey, BCryptExportKey, BCryptFinalizeKeyPair,
    BCryptGenerateKeyPair, BCryptImportKeyPair, BCryptOpenAlgorithmProvider, BCryptSignHash,
};

pub type Log<'a> = &'a dyn Fn(&str);

/// Sends the magic packet (the real one broadcasts; tests record it instead).
pub type Waker<'a> = &'a dyn Fn(&[u8; 6]);

// ───────────── the public steps ─────────────

/// Session start (and Testar): wake the TV and switch it to the PC's input.
/// `approval`: how long to wait for "Allow" on the TV; `timeout`: the whole step.
pub fn turn_on(tv: &TvControl, approval: Duration, timeout: Duration, data_dir: &Path, log: Log) -> Result<(), TvError> {
    turn_on_with(tv, approval, timeout, data_dir, log, &broadcast_wol)
}

/// Sistema → TV → Testar: the same as a session start, with time to accept on the TV.
pub fn test(tv: &TvControl, data_dir: &Path, log: Log) -> Result<(), TvError> {
    turn_on(tv, core::PAIRING_TIMEOUT, core::TEST_TIMEOUT, data_dir, log)
}

/// Going back to the PC: the user's command, then standby if that is on. Nothing to send: Ok
/// without touching the network. No Wake-on-LAN here: a TV that is off stays off.
pub fn go_back(tv: &TvControl, timeout: Duration, data_dir: &Path, log: Log) -> Result<(), TvError> {
    let commands = adb::restore_commands(&tv.restore_command, tv.turn_off_on_restore);
    if commands.is_empty() { return Ok(()); }
    check(tv)?;
    let dl = Deadline::after(timeout);
    let mut a = connect_waking(tv, core::RESTORE_APPROVAL, false, &dl, data_dir, log, &broadcast_wol)?;
    for (i, c) in commands.iter().enumerate() {
        if i > 0 { dl.sleep(core::BETWEEN_RESTORE_COMMANDS)?; }
        a.shell(c, &dl).map_err(|f| f.into_error(&dl))?;
    }
    Ok(())
}

fn check(tv: &TvControl) -> Result<(), TvError> {
    if tv.provider != core::ANDROID_TV { return Err(TvError::NotConfigured); }
    if tv.host.trim().is_empty() { return Err(TvError::HostMissing); }
    Ok(())
}

fn turn_on_with(tv: &TvControl, approval: Duration, timeout: Duration, data_dir: &Path, log: Log, wake: Waker) -> Result<(), TvError> {
    check(tv)?;
    let dl = Deadline::after(timeout);
    let mut a = connect_waking(tv, approval, true, &dl, data_dir, log, wake)?;
    a.shell(&format!("input keyevent {}", adb::KEY_WAKE_UP), &dl).map_err(|f| f.into_error(&dl))?;
    dl.sleep(core::AFTER_WAKE)?;
    a.shell(&tv.input_shell(), &dl).map_err(|f| f.into_error(&dl))?;
    Ok(())
}

/// Connects; a TV in deep standby has no network until the magic packet wakes it, then takes a
/// while to bring it up (1.7 TvNetwork.ConnectAsync).
fn connect_waking(tv: &TvControl, approval: Duration, wake_on_lan: bool, dl: &Deadline, data_dir: &Path, log: Log, wake: Waker) -> Result<Adb, TvError> {
    let (host, port) = adb::parse_endpoint(&tv.host);
    let key = load_or_create_key(data_dir, log).map_err(TvError::Other)?;
    let attempt = || Adb::connect(&host, port, &key, approval, dl, log);
    let unreachable = || TvError::Unreachable(tv.host.trim().to_string());
    match attempt() {
        Ok(a) => return Ok(a),
        Err(Fail::Net(_)) => match core::parse_mac(&tv.mac_address).filter(|_| wake_on_lan) {
            Some(mac) => { log(&format!("TV: {} sem resposta; enviando Wake-on-LAN", tv.host.trim())); wake(&mac); }
            None => return Err(unreachable()),
        },
        Err(f) => return Err(f.into_error(dl)),
    }
    let until = Instant::now() + core::WAKE_WAIT;
    loop {
        dl.sleep(core::WAKE_RETRY)?;
        match attempt() {
            Ok(a) => return Ok(a),
            // still booting: try again
            Err(Fail::Net(_)) if Instant::now() < until => {}
            Err(Fail::Net(_)) => return Err(unreachable()),
            Err(f) => return Err(f.into_error(dl)),
        }
    }
}

/// The magic packet on the usual ports, a few times in case one is lost.
fn broadcast_wol(mac: &[u8; 6]) {
    let packet = core::wol_packet(mac);
    let Ok(udp) = UdpSocket::bind("0.0.0.0:0") else { return };
    let _ = udp.set_broadcast(true);
    for _ in 0..3 {
        for port in core::WOL_PORTS { let _ = udp.send_to(&packet, ("255.255.255.255", port)); }
        std::thread::sleep(Duration::from_millis(100));
    }
}

// ───────────── time limits ─────────────

struct Deadline(Instant);

impl Deadline {
    fn after(d: Duration) -> Self { Deadline(Instant::now() + d) }
    fn expired(&self) -> bool { Instant::now() >= self.0 }
    fn left(&self) -> Result<Duration, TvError> { self.0.checked_duration_since(Instant::now()).filter(|d| !d.is_zero()).ok_or(TvError::TimedOut) }
    /// `d`, but never past the deadline.
    fn cap(&self, d: Duration) -> Result<Duration, TvError> { Ok(self.left()?.min(d)) }
    fn sleep(&self, d: Duration) -> Result<(), TvError> {
        let left = self.left()?;
        std::thread::sleep(d.min(left));
        if d >= left { Err(TvError::TimedOut) } else { Ok(()) }
    }
}

/// How one exchange failed.
#[derive(Debug)]
enum Fail {
    /// Nobody listening, no route, a TV that hung up or never answered the handshake: worth a
    /// Wake-on-LAN, else "the TV didn't answer".
    Net(String),
    /// A read took longer than it may.
    Slow,
    Tv(TvError),
}

impl Fail {
    fn into_error(self, dl: &Deadline) -> TvError {
        match self {
            _ if dl.expired() => TvError::TimedOut,
            Fail::Net(e) => TvError::Other(e),
            Fail::Slow => TvError::TimedOut,
            Fail::Tv(e) => e,
        }
    }
}

impl From<TvError> for Fail { fn from(e: TvError) -> Self { Fail::Tv(e) } }

// ───────────── the ADB client (AdbClient) ─────────────

struct Adb { s: TcpStream, next_local: u32 }

impl Adb {
    fn connect(host: &str, port: u16, key: &Key, approval: Duration, dl: &Deadline, log: Log) -> Result<Adb, Fail> {
        let addrs: Vec<_> = (host, port).to_socket_addrs().map_err(|e| Fail::Net(format!("endereço: {e}")))?.collect();
        let mut last = String::from("sem endereço");
        for addr in addrs {
            match TcpStream::connect_timeout(&addr, dl.cap(core::CONNECT_TIMEOUT)?) {
                Ok(s) => {
                    let _ = s.set_nodelay(true);
                    let mut a = Adb { s, next_local: 1 };
                    a.handshake(key, approval, dl, log)?;
                    return Ok(a);
                }
                Err(e) => last = e.to_string(),
            }
        }
        Err(Fail::Net(last))
    }

    fn handshake(&mut self, key: &Key, approval: Duration, dl: &Deadline, log: Log) -> Result<(), Fail> {
        self.send(adb::CONNECT, adb::VERSION, adb::MAX_PAYLOAD, &adb::connect_banner(), dl)?;
        let (mut sent_signature, mut sent_public_key) = (false, false);
        loop {
            // after our public key the TV waits for someone to press "Allow"
            let wait = if sent_public_key { approval } else { core::HANDSHAKE_TIMEOUT };
            let m = match self.read(wait, dl) {
                Ok(m) => m,
                Err(Fail::Slow) if dl.expired() => return Err(TvError::TimedOut.into()),
                Err(Fail::Slow) if sent_public_key => return Err(TvError::NotAllowed.into()),
                Err(Fail::Slow) => return Err(Fail::Net("ADB: a TV não respondeu ao handshake".into())),
                Err(f) => return Err(f),
            };
            match m.command {
                adb::CONNECT => return Ok(()),
                // "Wireless debugging" (Android 11+ pairing) wraps ADB in TLS; the classic one on 5555 doesn't
                adb::START_TLS => return Err(TvError::TlsUnsupported.into()),
                adb::AUTH if m.arg0 == adb::AUTH_TOKEN => match adb::on_auth_token(sent_signature, sent_public_key) {
                    adb::AuthStep::SendSignature => {
                        let signature = key.sign(&m.data).map_err(TvError::Other)?;
                        self.send(adb::AUTH, adb::AUTH_SIGNATURE, 0, &signature, dl)?;
                        sent_signature = true;
                    }
                    adb::AuthStep::SendPublicKey => {
                        log("TV: chave deste PC ainda não autorizada; aguardando \"Permitir\" na TV");
                        let name = std::env::var("COMPUTERNAME").unwrap_or_else(|_| "PC".into());
                        let public = adb::encode_public_key(&key.modulus, key.exponent, &format!("ConsoleMode2@{name}")).map_err(TvError::Other)?;
                        self.send(adb::AUTH, adb::AUTH_RSA_PUBLIC_KEY, 0, &public, dl)?;
                        sent_public_key = true;
                    }
                    adb::AuthStep::NotAllowed => return Err(TvError::NotAllowed.into()),
                },
                _ => {}
            }
        }
    }

    fn shell(&mut self, command: &str, dl: &Deadline) -> Result<String, Fail> {
        let local = self.next_local;
        self.next_local += 1;
        let mut remote = 0;
        let mut out = adb::ShellOutput::default();
        self.send(adb::OPEN, local, 0, &adb::shell_service(command), dl)?;
        loop {
            let m = self.read(dl.left()?, dl)?;
            if m.arg1 != local { continue; }
            match m.command {
                adb::OKAY => remote = m.arg0,
                adb::WRITE => { out.append(&m.data).map_err(TvError::Other)?; self.send(adb::OKAY, local, m.arg0, &[], dl)?; }
                adb::CLOSE => {
                    if remote != 0 { self.send(adb::CLOSE, local, remote, &[], dl)?; }
                    return Ok(out.text());
                }
                _ => {}
            }
        }
    }

    fn send(&mut self, command: u32, arg0: u32, arg1: u32, data: &[u8], dl: &Deadline) -> Result<(), Fail> {
        let _ = self.s.set_write_timeout(Some(dl.cap(core::HANDSHAKE_TIMEOUT)?));
        self.s.write_all(&adb::encode(command, arg0, arg1, data)).map_err(net)
    }

    fn read(&mut self, wait: Duration, dl: &Deadline) -> Result<adb::Message, Fail> {
        let _ = self.s.set_read_timeout(Some(dl.cap(wait)?));
        let mut header = [0u8; adb::HEADER_SIZE];
        self.s.read_exact(&mut header).map_err(net)?;
        let h = adb::decode_header(&header).map_err(TvError::Other)?;
        let mut data = vec![0u8; h.length];
        self.s.read_exact(&mut data).map_err(net)?;
        adb::validate_payload(h.checksum, &data).map_err(TvError::Other)?;
        Ok(adb::Message { command: h.command, arg0: h.arg0, arg1: h.arg1, data })
    }
}

fn net(e: std::io::Error) -> Fail {
    if matches!(e.kind(), ErrorKind::TimedOut | ErrorKind::WouldBlock) { Fail::Slow } else { Fail::Net(e.to_string()) }
}

// ───────────── this PC's key (AdbKeyStore) ─────────────

const KEY_FILE: &str = "adbkey";
static KEY_GATE: Mutex<()> = Mutex::new(());

/// An RSA-2048 key in Windows CNG; `modulus` big endian, for Android's public key format.
pub struct Key { alg: BCRYPT_ALG_HANDLE, handle: BCRYPT_KEY_HANDLE, modulus: Vec<u8>, exponent: u32 }

impl Drop for Key {
    fn drop(&mut self) { unsafe { let _ = BCryptDestroyKey(self.handle); let _ = BCryptCloseAlgorithmProvider(self.alg, 0); } }
}

fn ok(status: windows::Win32::Foundation::NTSTATUS, what: &str) -> Result<(), String> {
    if status.0 >= 0 { Ok(()) } else { Err(format!("CNG: {what} falhou (0x{:08X})", status.0 as u32)) }
}

fn rsa_provider() -> Result<BCRYPT_ALG_HANDLE, String> {
    let mut alg = BCRYPT_ALG_HANDLE::default();
    ok(unsafe { BCryptOpenAlgorithmProvider(&mut alg, BCRYPT_RSA_ALGORITHM, None, BCRYPT_OPEN_ALGORITHM_PROVIDER_FLAGS(0)) }, "abrir RSA")?;
    Ok(alg)
}

/// BCRYPT_RSAKEY_BLOB: magic, bits, exponent size, modulus size, primes' sizes; then exponent and modulus (big endian).
fn public_parts(blob: &[u8]) -> Option<(u32, Vec<u8>, u32)> {
    let word = |i: usize| blob.get(i * 4..i * 4 + 4).map(|b| u32::from_le_bytes(b.try_into().unwrap()));
    let (bits, exp_len, mod_len) = (word(1)?, word(2)? as usize, word(3)? as usize);
    let exp = blob.get(24..24 + exp_len)?;
    let modulus = blob.get(24 + exp_len..24 + exp_len + mod_len)?.to_vec();
    if exp_len > 4 { return None; }
    Some((bits, modulus, exp.iter().fold(0u32, |e, b| e << 8 | *b as u32)))
}

impl Key {
    fn generate() -> Result<(Key, Vec<u8>), String> {
        let alg = rsa_provider()?;
        let mut handle = BCRYPT_KEY_HANDLE::default();
        let made = ok(unsafe { BCryptGenerateKeyPair(alg, &mut handle, 2048, 0) }, "gerar a chave").and_then(|_| ok(unsafe { BCryptFinalizeKeyPair(handle, 0) }, "finalizar a chave"));
        let key = Key { alg, handle, modulus: Vec::new(), exponent: 0 };
        made?;
        let mut len = 0u32;
        ok(unsafe { BCryptExportKey(key.handle, None, BCRYPT_RSAFULLPRIVATE_BLOB, None, &mut len, 0) }, "exportar a chave")?;
        let mut blob = vec![0u8; len as usize];
        ok(unsafe { BCryptExportKey(key.handle, None, BCRYPT_RSAFULLPRIVATE_BLOB, Some(&mut blob), &mut len, 0) }, "exportar a chave")?;
        blob.truncate(len as usize);
        let key = Key::import(&blob)?;
        Ok((key, blob))
    }

    fn import(blob: &[u8]) -> Result<Key, String> {
        let (bits, modulus, exponent) = public_parts(blob).ok_or("chave ADB malformada")?;
        if bits != 2048 || modulus.len() != 256 { return Err("chave ADB não é RSA-2048".into()); }
        let alg = rsa_provider()?;
        let mut handle = BCRYPT_KEY_HANDLE::default();
        let status = unsafe { BCryptImportKeyPair(alg, None, BCRYPT_RSAFULLPRIVATE_BLOB, &mut handle, blob, 0) };
        let key = Key { alg, handle, modulus, exponent };
        ok(status, "importar a chave")?;
        Ok(key)
    }

    /// adb signs the 20-byte token as if it were a SHA-1 digest (RSA_sign with NID_sha1): PKCS#1
    /// v1.5 over the SHA-1 DigestInfo of the raw token.
    fn sign(&self, token: &[u8]) -> Result<Vec<u8>, String> {
        let padding = BCRYPT_PKCS1_PADDING_INFO { pszAlgId: BCRYPT_SHA1_ALGORITHM };
        let pad = Some(&padding as *const _ as *const std::ffi::c_void);
        let mut len = 0u32;
        ok(unsafe { BCryptSignHash(self.handle, pad, token, None, &mut len, BCRYPT_PAD_PKCS1) }, "assinar")?;
        let mut sig = vec![0u8; len as usize];
        ok(unsafe { BCryptSignHash(self.handle, pad, token, Some(&mut sig), &mut len, BCRYPT_PAD_PKCS1) }, "assinar")?;
        sig.truncate(len as usize);
        Ok(sig)
    }
}

/// This PC's key, made the first time and kept encrypted next to the config so the TV asks only once.
pub fn load_or_create_key(data_dir: &Path, log: Log) -> Result<Key, String> {
    let _gate = KEY_GATE.lock().unwrap_or_else(|e| e.into_inner());
    let path = data_dir.join(KEY_FILE);
    if let Ok(stored) = std::fs::read_to_string(&path) {
        let stored = stored.trim();
        let blob = if crate::secret::is_protected(stored) { crate::secret::unprotect(stored) } else { cm_core::tv::base64_decode(stored) };
        match blob.map(|b| (Key::import(&b), b)) {
            Some((Ok(key), blob)) => {
                // a key written by hand or before encryption: protect it in place
                if !crate::secret::is_protected(stored) { if let Ok(p) = crate::secret::protect(&blob) { let _ = std::fs::write(&path, p); } }
                return Ok(key);
            }
            // copied from another Windows user / PC: DPAPI can't open it; a new key just means the TV asks once more
            None if crate::secret::is_protected(stored) => log("TV: chave ADB de outro usuário/PC; criando outra (a TV pedirá permissão de novo)"),
            Some((Err(e), _)) => log(&format!("TV: chave ADB ilegível, criando outra: {e}")),
            None => log("TV: chave ADB ilegível, criando outra"),
        }
    }
    let (key, blob) = Key::generate()?;
    let _ = std::fs::create_dir_all(data_dir);
    std::fs::write(&path, crate::secret::protect(&blob)?).map_err(|e| format!("TV: não deu para gravar a chave ADB: {e}"))?;
    log("TV: chave ADB criada");
    Ok(key)
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::{cell::RefCell, net::TcpListener, path::PathBuf, sync::mpsc};
    use windows::Win32::Security::Cryptography::BCryptVerifySignature;

    fn dir(name: &str) -> PathBuf {
        let d = std::env::temp_dir().join(format!("cm2-tv-{name}-{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&d);
        std::fs::create_dir_all(&d).unwrap();
        d
    }
    fn quiet(_: &str) {}
    fn no_wake(_: &[u8; 6]) { panic!("no Wake-on-LAN expected") }

    fn verify(key: &Key, token: &[u8], sig: &[u8]) -> bool {
        let padding = BCRYPT_PKCS1_PADDING_INFO { pszAlgId: BCRYPT_SHA1_ALGORITHM };
        unsafe { BCryptVerifySignature(key.handle, Some(&padding as *const _ as *const std::ffi::c_void), token, sig, BCRYPT_PAD_PKCS1) }.0 >= 0
    }

    fn read(s: &mut TcpStream) -> adb::Message {
        let mut h = [0u8; adb::HEADER_SIZE];
        s.read_exact(&mut h).unwrap();
        let h = adb::decode_header(&h).unwrap();
        let mut data = vec![0u8; h.length];
        s.read_exact(&mut data).unwrap();
        adb::validate_payload(h.checksum, &data).unwrap();
        adb::Message { command: h.command, arg0: h.arg0, arg1: h.arg1, data }
    }
    fn send(s: &mut TcpStream, c: u32, a0: u32, a1: u32, d: &[u8]) { s.write_all(&adb::encode(c, a0, a1, d)).unwrap(); }

    /// How the fake TV answers this PC's key.
    #[derive(Clone, Copy, PartialEq)]
    enum Tv { Knows, AsksThenAllows, Refuses, TlsOnly }

    /// A fake Android TV on 127.0.0.1: checks the handshake against the key in `data`, and reports
    /// each shell command it was asked to run.
    fn fake_tv(mode: Tv, data: PathBuf) -> (String, mpsc::Receiver<String>) {
        let listener = TcpListener::bind("127.0.0.1:0").unwrap();
        let host = listener.local_addr().unwrap().to_string();
        let (tx, rx) = mpsc::channel();
        std::thread::spawn(move || {
            let (mut s, _) = listener.accept().unwrap();
            s.set_read_timeout(Some(Duration::from_secs(10))).unwrap();
            let key = load_or_create_key(&data, &quiet).unwrap();
            let hello = read(&mut s);
            assert_eq!((hello.command, hello.data.as_slice()), (adb::CONNECT, &b"host::\0"[..]));
            if mode == Tv::TlsOnly { return send(&mut s, adb::START_TLS, 1, 0, &[]); }
            let token: Vec<u8> = (0..20u8).collect();
            send(&mut s, adb::AUTH, adb::AUTH_TOKEN, 0, &token);
            let sig = read(&mut s);
            assert_eq!((sig.command, sig.arg0), (adb::AUTH, adb::AUTH_SIGNATURE));
            assert!(verify(&key, &token, &sig.data), "a signature this key made");
            if mode != Tv::Knows {
                send(&mut s, adb::AUTH, adb::AUTH_TOKEN, 0, &token);
                let public = read(&mut s);
                assert_eq!((public.command, public.arg0), (adb::AUTH, adb::AUTH_RSA_PUBLIC_KEY));
                let text = String::from_utf8(public.data).unwrap();
                assert!(text.contains(" ConsoleMode2@") && text.ends_with('\0'));
                let raw = cm_core::tv::base64_decode(text.split(' ').next().unwrap()).unwrap();
                let mut n: Vec<u8> = raw[8..264].to_vec();
                n.reverse();
                assert_eq!(n, key.modulus, "our public key");
                if mode == Tv::Refuses { send(&mut s, adb::AUTH, adb::AUTH_TOKEN, 0, &token); let _ = s.read(&mut [0u8; 1]); return; }
            }
            send(&mut s, adb::CONNECT, adb::VERSION, adb::MAX_PAYLOAD, b"device::\0");
            let mut remote = 100;
            loop {
                let mut h = [0u8; adb::HEADER_SIZE];
                if s.read_exact(&mut h).is_err() { return; }
                let h = adb::decode_header(&h).unwrap();
                let mut d = vec![0u8; h.length];
                s.read_exact(&mut d).unwrap();
                if h.command != adb::OPEN { continue; }
                let cmd = String::from_utf8(d).unwrap();
                let local = h.arg0;
                remote += 1;
                send(&mut s, adb::OKAY, remote, local, &[]);
                send(&mut s, adb::WRITE, remote, local, b"ok");
                let ack = read(&mut s);
                assert_eq!((ack.command, ack.arg0, ack.arg1), (adb::OKAY, local, remote));
                // reported before the close, so the client can't return before the test sees it
                tx.send(cmd.trim_start_matches("shell:").trim_end_matches('\0').to_string()).unwrap();
                send(&mut s, adb::CLOSE, remote, local, &[]);
                let close = read(&mut s);
                assert_eq!(close.command, adb::CLOSE);
            }
        });
        (host, rx)
    }

    fn android(host: &str) -> TvControl { TvControl { provider: core::ANDROID_TV.into(), host: host.into(), hdmi_input: 2, ..Default::default() } }

    #[test]
    fn the_key_is_made_once_kept_encrypted_and_signs() {
        let d = dir("key");
        let a = load_or_create_key(&d, &quiet).unwrap();
        let stored = std::fs::read_to_string(d.join(KEY_FILE)).unwrap();
        assert!(stored.starts_with("dpapi:"), "never plain on disk");
        let b = load_or_create_key(&d, &quiet).unwrap();
        assert_eq!((a.modulus.len(), a.exponent), (256, 65537));
        assert_eq!(a.modulus, b.modulus, "the same key the next time");
        let token = [7u8; 20];
        assert!(verify(&b, &token, &a.sign(&token).unwrap()));
        // another user's / PC's file: a new key, logged without its contents
        std::fs::write(d.join(KEY_FILE), "dpapi:AAAAAAAA").unwrap();
        let lines = RefCell::new(Vec::new());
        let c = load_or_create_key(&d, &|l: &str| lines.borrow_mut().push(l.to_string())).unwrap();
        assert_ne!(c.modulus, a.modulus);
        assert!(lines.borrow().iter().any(|l| l.contains("outro usuário/PC")));
        assert!(lines.borrow().iter().all(|l| !l.contains("dpapi:")));
        let _ = std::fs::remove_dir_all(d);
    }

    #[test]
    fn the_first_time_the_tv_asks_then_it_wakes_and_switches_input() {
        let d = dir("pair");
        let (host, cmds) = fake_tv(Tv::AsksThenAllows, d.clone());
        let lines = RefCell::new(Vec::new());
        turn_on_with(&android(&host), Duration::from_secs(5), Duration::from_secs(20), &d, &|l: &str| lines.borrow_mut().push(l.to_string()), &no_wake).unwrap();
        assert_eq!(cmds.try_iter().collect::<Vec<_>>(), ["input keyevent 224", "input keyevent 244"]);
        assert!(lines.borrow().iter().any(|l| l.contains("aguardando \"Permitir\"")));
        let _ = std::fs::remove_dir_all(d);
    }

    #[test]
    fn a_tv_that_knows_the_pc_runs_the_users_input_command() {
        let d = dir("known");
        let (host, cmds) = fake_tv(Tv::Knows, d.clone());
        let mut tv = android(&host);
        tv.input_command = "am start -n com.x/.Live".into();
        turn_on_with(&tv, Duration::from_secs(5), Duration::from_secs(20), &d, &quiet, &no_wake).unwrap();
        assert_eq!(cmds.try_iter().collect::<Vec<_>>(), ["input keyevent 224", "am start -n com.x/.Live"]);
        let _ = std::fs::remove_dir_all(d);
    }

    #[test]
    fn going_back_sends_the_command_then_standby() {
        let d = dir("back");
        let (host, cmds) = fake_tv(Tv::Knows, d.clone());
        let mut tv = android(&host);
        tv.restore_command = "input keyevent 3".into();
        tv.turn_off_on_restore = true;
        go_back(&tv, core::STOP_TIMEOUT, &d, &quiet).unwrap();
        assert_eq!(cmds.try_iter().collect::<Vec<_>>(), ["input keyevent 3", "input keyevent 223"]);
        // nothing to send: the network is not touched (no host, no listener)
        assert_eq!(go_back(&android(""), core::STOP_TIMEOUT, &d, &quiet), Ok(()));
        let _ = std::fs::remove_dir_all(d);
    }

    #[test]
    fn a_refusal_and_tls_only_tvs_are_explained() {
        let d = dir("refuse");
        let (host, _) = fake_tv(Tv::Refuses, d.clone());
        assert_eq!(turn_on_with(&android(&host), Duration::from_secs(5), Duration::from_secs(20), &d, &quiet, &no_wake), Err(TvError::NotAllowed));
        let (host, _) = fake_tv(Tv::TlsOnly, d.clone());
        assert_eq!(turn_on_with(&android(&host), Duration::from_secs(5), Duration::from_secs(20), &d, &quiet, &no_wake), Err(TvError::TlsUnsupported));
        let _ = std::fs::remove_dir_all(d);
    }

    #[test]
    fn a_tv_that_does_not_answer_is_unreachable_and_gets_the_magic_packet_with_a_mac() {
        let d = dir("off");
        // a port nobody listens on
        let host = { let l = TcpListener::bind("127.0.0.1:0").unwrap(); l.local_addr().unwrap().to_string() };
        assert_eq!(turn_on_with(&android(&host), Duration::from_secs(1), Duration::from_secs(10), &d, &quiet, &no_wake), Err(TvError::Unreachable(host.clone())));
        let woken = RefCell::new(Vec::new());
        let mut tv = android(&host);
        tv.mac_address = "AA:BB:CC:DD:EE:FF".into();
        let r = turn_on_with(&tv, Duration::from_secs(1), Duration::from_secs(5), &d, &quiet, &|m: &[u8; 6]| woken.borrow_mut().push(*m));
        assert_eq!(woken.borrow().as_slice(), &[[0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]]);
        assert!(matches!(r, Err(TvError::TimedOut) | Err(TvError::Unreachable(_))), "{r:?}");
        // and the configuration problems come first
        assert_eq!(turn_on_with(&android(""), Duration::from_secs(1), Duration::from_secs(1), &d, &quiet, &no_wake), Err(TvError::HostMissing));
        assert_eq!(turn_on_with(&TvControl::default(), Duration::from_secs(1), Duration::from_secs(1), &d, &quiet, &no_wake), Err(TvError::NotConfigured));
        let _ = std::fs::remove_dir_all(d);
    }

    #[test]
    fn a_tv_that_accepts_but_never_answers_times_out_in_time() {
        let d = dir("mute");
        let listener = TcpListener::bind("127.0.0.1:0").unwrap();
        let host = listener.local_addr().unwrap().to_string();
        std::thread::spawn(move || { let (_s, _) = listener.accept().unwrap(); std::thread::sleep(Duration::from_secs(8)); });
        let started = Instant::now();
        let r = turn_on_with(&android(&host), Duration::from_secs(1), Duration::from_secs(3), &d, &quiet, &no_wake);
        assert!(started.elapsed() < Duration::from_secs(5), "bounded by the step's timeout");
        assert!(matches!(r, Err(TvError::TimedOut) | Err(TvError::Unreachable(_))), "{r:?}");
        let _ = std::fs::remove_dir_all(d);
    }
}
