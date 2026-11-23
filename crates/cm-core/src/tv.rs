//! TV control (1.7.1/1.8 TvControlService, AndroidTvController, AdbProtocol, WakeOnLan): turn a
//! Google TV / Android TV on and switch it to the PC's HDMI input when the session starts, and
//! optionally send a command and/or standby when the desk comes back.
//!
//! The pieces with no OS calls live here: the config block (`tv` in config.json, 1.7 key names),
//! the ADB wire format (framing, checksum, Android's public key layout), the commands sent, the
//! Wake-on-LAN packet and the timeouts. The sockets, the RSA key and DPAPI are in cm-platform.

use std::time::Duration;

use serde::{Deserialize, Serialize};
use serde_json::{Map, Value};

pub const NONE: &str = "none";
pub const ANDROID_TV: &str = "androidTv";

/// The `tv` block of config.json, as 1.7 writes it (TvControlConfig).
#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct TvControl {
    /// "none" | "androidTv".
    pub provider: String,
    /// The TV's IP address or host name, optionally with ":port".
    pub host: String,
    /// For Wake-on-LAN when the TV is in deep standby; empty = don't send it.
    pub mac_address: String,
    /// HDMI input the PC is plugged into (1-4).
    pub hdmi_input: i32,
    /// Shell command that switches to the PC's input, for TVs that ignore the HDMI key codes.
    pub input_command: String,
    /// Put the TV in standby after the desk is restored. Off by default.
    pub turn_off_on_restore: bool,
    /// Shell command sent when the desk is back, before standby (Home screen, another input…).
    pub restore_command: String,
    /// Fields a later 1.x adds are kept as they came.
    #[serde(flatten)]
    pub other: Map<String, Value>,
}

impl Default for TvControl {
    fn default() -> Self {
        TvControl { provider: NONE.into(), host: String::new(), mac_address: String::new(), hdmi_input: 1, input_command: String::new(), turn_off_on_restore: false, restore_command: String::new(), other: Map::new() }
    }
}

impl TvControl {
    pub fn is_enabled(&self) -> bool { !self.provider.trim().is_empty() && self.provider != NONE }
    /// Something to send the TV when going back to the PC.
    pub fn has_restore_step(&self) -> bool { self.turn_off_on_restore || !self.restore_command.trim().is_empty() }
    /// What the session start sends after waking the TV: the user's command, else the HDMI key.
    pub fn input_shell(&self) -> String {
        if self.input_command.trim().is_empty() { format!("input keyevent {}", hdmi_key(self.hdmi_input)) } else { self.input_command.trim().to_string() }
    }
    /// What goes in the session journal so the restore (or the recovery after a crash) does the
    /// going-back step; None when there is nothing to send.
    pub fn restore_step(&self) -> Option<TvRestore> {
        (self.is_enabled() && self.has_restore_step()).then(|| TvRestore {
            provider: self.provider.clone(), host: self.host.trim().into(), restore_command: self.restore_command.trim().into(), standby: self.turn_off_on_restore,
        })
    }
}

/// The going-back step of a session, kept in the journal (no secrets: the key stays in its own file).
#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct TvRestore {
    pub provider: String,
    pub host: String,
    pub restore_command: String,
    pub standby: bool,
}

impl TvRestore {
    /// The config the platform layer needs to send it.
    pub fn as_config(&self) -> TvControl {
        TvControl { provider: self.provider.clone(), host: self.host.clone(), restore_command: self.restore_command.clone(), turn_off_on_restore: self.standby, ..Default::default() }
    }
}

// ───────────── timeouts (TvControlService) ─────────────

/// Session start: enough for a TV waking from deep standby (Wake-on-LAN) to answer.
pub const START_TIMEOUT: Duration = Duration::from_secs(30);
/// Going back to the PC.
pub const STOP_TIMEOUT: Duration = Duration::from_secs(15);
/// Sistema → TV → Testar.
pub const TEST_TIMEOUT: Duration = Duration::from_secs(90);
/// Testar: time to press "Allow" on the TV the first time.
pub const PAIRING_TIMEOUT: Duration = Duration::from_secs(60);
/// Session start: a TV that still asks to allow this PC gets this long.
pub const START_APPROVAL: Duration = Duration::from_secs(10);
pub const RESTORE_APPROVAL: Duration = Duration::from_secs(5);
/// One TCP connect attempt.
pub const CONNECT_TIMEOUT: Duration = Duration::from_secs(3);
/// The TV's answer to our CNXN / signature, before any "Allow" prompt.
pub const HANDSHAKE_TIMEOUT: Duration = Duration::from_secs(5);
/// After the magic packet: how long the TV may take to bring its network up, and the pause between tries.
pub const WAKE_WAIT: Duration = Duration::from_secs(20);
pub const WAKE_RETRY: Duration = Duration::from_secs(2);
/// Right after waking, some TVs drop the input key while the launcher loads.
pub const AFTER_WAKE: Duration = Duration::from_millis(1500);
/// Between the user's going-back command and standby, or the TV may drop the second while it switches.
pub const BETWEEN_RESTORE_COMMANDS: Duration = Duration::from_millis(1000);

/// Why talking to the TV failed. The app turns each into a message for the user.
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum TvError {
    NotConfigured,
    HostMissing,
    /// Nobody answered at this host (also after Wake-on-LAN).
    Unreachable(String),
    /// The TV didn't accept this PC's key.
    NotAllowed,
    /// The TV only speaks "Wireless debugging" (TLS pairing).
    TlsUnsupported,
    /// The whole step took longer than its timeout.
    TimedOut,
    /// Anything else (a broken packet…), for the log and the "could not talk" message.
    Other(String),
}

impl TvError {
    /// For the log (Portuguese, never secrets).
    pub fn describe(&self) -> String {
        match self {
            TvError::NotConfigured => "nenhum controle de TV escolhido".into(),
            TvError::HostMissing => "sem endereço da TV".into(),
            TvError::Unreachable(h) => format!("a TV em {h} não respondeu"),
            TvError::NotAllowed => "a TV não autorizou este PC".into(),
            TvError::TlsUnsupported => "a TV só aceita depuração sem fio (TLS)".into(),
            TvError::TimedOut => "tempo esgotado".into(),
            TvError::Other(e) => e.clone(),
        }
    }
}

// ───────────── ADB (AdbProtocol) ─────────────

pub mod adb {
    pub const CONNECT: u32 = 0x4e58_4e43; // CNXN
    pub const AUTH: u32 = 0x4854_5541; // AUTH
    pub const OPEN: u32 = 0x4e45_504f; // OPEN
    pub const OKAY: u32 = 0x5941_4b4f; // OKAY
    pub const CLOSE: u32 = 0x4553_4c43; // CLSE
    pub const WRITE: u32 = 0x4554_5257; // WRTE
    pub const START_TLS: u32 = 0x534c_5453; // STLS

    pub const AUTH_TOKEN: u32 = 1;
    pub const AUTH_SIGNATURE: u32 = 2;
    pub const AUTH_RSA_PUBLIC_KEY: u32 = 3;

    /// The version with the payload checksum, which every device still accepts.
    pub const VERSION: u32 = 0x0100_0000;
    pub const MAX_PAYLOAD: u32 = 256 * 1024;
    pub const MAX_SHELL_OUTPUT: usize = 1024 * 1024;
    pub const HEADER_SIZE: usize = 24;
    pub const DEFAULT_PORT: u16 = 5555;

    // android.view.KeyEvent codes
    pub const KEY_HOME: i32 = 3;
    pub const KEY_SLEEP: i32 = 223;
    pub const KEY_WAKE_UP: i32 = 224;
    pub const KEY_TV_INPUT_HDMI_1: i32 = 243;

    #[derive(Clone, Debug, PartialEq, Eq)]
    pub struct Message { pub command: u32, pub arg0: u32, pub arg1: u32, pub data: Vec<u8> }

    /// A decoded header: the payload that follows is `length` bytes with this checksum.
    #[derive(Clone, Copy, Debug, PartialEq, Eq)]
    pub struct Header { pub command: u32, pub arg0: u32, pub arg1: u32, pub length: usize, pub checksum: u32 }

    pub fn checksum(data: &[u8]) -> u32 { data.iter().fold(0u32, |s, b| s.wrapping_add(*b as u32)) }

    pub fn encode(command: u32, arg0: u32, arg1: u32, data: &[u8]) -> Vec<u8> {
        let mut out = Vec::with_capacity(HEADER_SIZE + data.len());
        for v in [command, arg0, arg1, data.len() as u32, checksum(data), command ^ 0xFFFF_FFFF] { out.extend_from_slice(&v.to_le_bytes()); }
        out.extend_from_slice(data);
        out
    }

    fn u32_at(b: &[u8], at: usize) -> u32 { u32::from_le_bytes([b[at], b[at + 1], b[at + 2], b[at + 3]]) }

    pub fn decode_header(header: &[u8]) -> Result<Header, String> {
        if header.len() < HEADER_SIZE { return Err("ADB: cabeçalho curto".into()); }
        let command = u32_at(header, 0);
        if u32_at(header, 20) != command ^ 0xFFFF_FFFF { return Err("ADB: cabeçalho inválido".into()); }
        let length = u32_at(header, 12);
        if length > MAX_PAYLOAD { return Err("ADB: mensagem grande demais".into()); }
        Ok(Header { command, arg0: u32_at(header, 4), arg1: u32_at(header, 8), length: length as usize, checksum: u32_at(header, 16) })
    }

    pub fn validate_payload(expected: u32, data: &[u8]) -> Result<(), String> {
        if checksum(data) == expected { Ok(()) } else { Err("ADB: checksum do pacote inválido".into()) }
    }

    /// "host::" banner sent with CNXN.
    pub fn connect_banner() -> Vec<u8> { b"host::\0".to_vec() }

    pub fn shell_service(command: &str) -> Vec<u8> { format!("shell:{command}\0").into_bytes() }

    /// Key code for "HDMI n" (1-4).
    pub fn hdmi_key(input: i32) -> i32 { KEY_TV_INPUT_HDMI_1 + input.clamp(1, 4) - 1 }

    /// "host" or "host:port" (default 5555).
    pub fn parse_endpoint(value: &str) -> (String, u16) {
        let text = value.trim();
        if let Some(colon) = text.rfind(':') {
            if colon > 0 && text.find(':') == Some(colon) {
                if let Ok(port) = text[colon + 1..].parse::<u16>() { if port > 0 { return (text[..colon].to_string(), port); } }
            }
        }
        (text.to_string(), DEFAULT_PORT)
    }

    /// What the TV gets when the desk comes back (issue #209): the user's command first (the
    /// TV's Home screen, another HDMI input…), then standby if that is on. Empty: nothing to send.
    pub fn restore_commands(restore_command: &str, standby: bool) -> Vec<String> {
        let mut v = Vec::new();
        if !restore_command.trim().is_empty() { v.push(restore_command.trim().to_string()); }
        if standby { v.push(format!("input keyevent {KEY_SLEEP}")); }
        v
    }

    /// Android's RSAPublicKey struct (android_pubkey_encode), base64, then " comment" and a NUL:
    /// what the TV shows in "Allow debugging from this computer?". `modulus`: 256 bytes, big endian.
    pub fn encode_public_key(modulus: &[u8], exponent: u32, comment: &str) -> Result<Vec<u8>, String> {
        if modulus.len() != 256 { return Err("ADB: a chave precisa ser RSA-2048".into()); }
        // little-endian 32-bit words of n
        let n: Vec<u32> = (0..64).map(|i| { let at = 256 - 4 * (i + 1); u32::from_be_bytes([modulus[at], modulus[at + 1], modulus[at + 2], modulus[at + 3]]) }).collect();
        if n[0] & 1 == 0 { return Err("ADB: módulo par".into()); }
        // Newton's iteration for the inverse of an odd number mod 2^32
        let n0 = n[0];
        let mut inv = n0;
        for _ in 0..5 { inv = inv.wrapping_mul(2u32.wrapping_sub(n0.wrapping_mul(inv))); }
        let n0_inv = 0u32.wrapping_sub(inv);
        let rr = two_pow_mod(4096, &n);

        let mut buf = Vec::with_capacity(524);
        buf.extend_from_slice(&64u32.to_le_bytes());
        buf.extend_from_slice(&n0_inv.to_le_bytes());
        for w in &n { buf.extend_from_slice(&w.to_le_bytes()); }
        for w in &rr { buf.extend_from_slice(&w.to_le_bytes()); }
        buf.extend_from_slice(&exponent.to_le_bytes());
        Ok(format!("{} {comment}\0", super::base64_encode(&buf)).into_bytes())
    }

    /// 2^e mod n (n: little-endian words, top word non-zero) by doubling: r < n, so 2r < 2n and one
    /// subtraction keeps it reduced.
    fn two_pow_mod(e: u32, n: &[u32]) -> Vec<u32> {
        let mut r = vec![0u32; n.len()];
        r[0] = 1;
        if n.len() == 1 && n[0] == 1 { return vec![0]; }
        for _ in 0..e {
            let mut carry = 0u32;
            for w in r.iter_mut() { let next = *w >> 31; *w = (*w << 1) | carry; carry = next; }
            if carry == 1 || !less(&r, n) { sub(&mut r, n); }
        }
        r
    }

    fn less(a: &[u32], b: &[u32]) -> bool {
        for i in (0..a.len()).rev() { if a[i] != b[i] { return a[i] < b[i]; } }
        false
    }

    fn sub(a: &mut [u32], b: &[u32]) {
        let mut borrow = 0u64;
        for i in 0..a.len() {
            let d = (a[i] as u64).wrapping_sub(b[i] as u64).wrapping_sub(borrow);
            a[i] = d as u32;
            borrow = (d >> 63) & 1;
        }
    }

    /// Output of a remote shell command, bounded so a chatty TV can't fill the memory.
    #[derive(Default)]
    pub struct ShellOutput { bytes: Vec<u8> }

    impl ShellOutput {
        pub fn append(&mut self, data: &[u8]) -> Result<(), String> {
            if data.len() > MAX_SHELL_OUTPUT - self.bytes.len() { return Err("ADB: saída do comando excede o limite de 1 MiB".into()); }
            self.bytes.extend_from_slice(data);
            Ok(())
        }
        pub fn text(&self) -> String { String::from_utf8_lossy(&self.bytes).into_owned() }
    }

    /// What the client does with one AUTH token from the TV (AdbClient.HandshakeAsync).
    #[derive(Clone, Copy, Debug, PartialEq, Eq)]
    pub enum AuthStep { SendSignature, SendPublicKey, NotAllowed }

    pub fn on_auth_token(sent_signature: bool, sent_public_key: bool) -> AuthStep {
        if !sent_signature { AuthStep::SendSignature } else if !sent_public_key { AuthStep::SendPublicKey } else { AuthStep::NotAllowed }
    }
}

pub use adb::hdmi_key;

// ───────────── Wake-on-LAN ─────────────

/// Accepts AA:BB:CC:DD:EE:FF, AA-BB-CC-DD-EE-FF, AABB.CCDD.EEFF or AABBCCDDEEFF.
pub fn parse_mac(text: &str) -> Option<[u8; 6]> {
    let hex: String = text.trim().chars().filter(|c| !matches!(c, ':' | '-' | '.')).collect();
    if hex.len() != 12 || !hex.is_ascii() { return None; }
    let mut mac = [0u8; 6];
    for (i, b) in mac.iter_mut().enumerate() { *b = u8::from_str_radix(&hex[i * 2..i * 2 + 2], 16).ok()?; }
    Some(mac)
}

/// The "magic packet": 6 × 0xFF, then the MAC 16 times.
pub fn wol_packet(mac: &[u8; 6]) -> Vec<u8> {
    let mut p = vec![0xFF; 6];
    for _ in 0..16 { p.extend_from_slice(mac); }
    p
}

/// The usual Wake-on-LAN ports, broadcast to both.
pub const WOL_PORTS: [u16; 2] = [9, 7];

// ───────────── base64 (the key file and Android's key format) ─────────────

const B64: &[u8; 64] = b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

pub fn base64_encode(data: &[u8]) -> String {
    let mut out = String::with_capacity(data.len().div_ceil(3) * 4);
    for chunk in data.chunks(3) {
        let b = [chunk[0], *chunk.get(1).unwrap_or(&0), *chunk.get(2).unwrap_or(&0)];
        let v = (b[0] as u32) << 16 | (b[1] as u32) << 8 | b[2] as u32;
        for k in 0..4 {
            if k <= chunk.len() { out.push(B64[(v >> (18 - 6 * k) & 63) as usize] as char); } else { out.push('='); }
        }
    }
    out
}

pub fn base64_decode(text: &str) -> Option<Vec<u8>> {
    let text = text.trim().trim_end_matches('=');
    let mut out = Vec::with_capacity(text.len() * 3 / 4);
    let (mut acc, mut bits) = (0u32, 0);
    for c in text.bytes() {
        let v = B64.iter().position(|b| *b == c)? as u32;
        acc = acc << 6 | v;
        bits += 6;
        if bits >= 8 { bits -= 8; out.push((acc >> bits) as u8); acc &= (1 << bits) - 1; }
    }
    Some(out)
}

#[cfg(test)]
mod tests {
    use super::{adb::*, *};

    #[test]
    fn the_config_block_keeps_the_1_7_names() {
        let json = r#"{"provider":"androidTv","host":"192.168.0.50","macAddress":"AA:BB:CC:DD:EE:FF","hdmiInput":2,"inputCommand":"","turnOffOnRestore":true,"restoreCommand":"input keyevent 3","later":1}"#;
        let tv: TvControl = serde_json::from_str(json).unwrap();
        assert!(tv.is_enabled() && tv.has_restore_step());
        assert_eq!((tv.hdmi_input, tv.mac_address.as_str()), (2, "AA:BB:CC:DD:EE:FF"));
        let back = serde_json::to_value(&tv).unwrap();
        for key in ["provider", "host", "macAddress", "hdmiInput", "inputCommand", "turnOffOnRestore", "restoreCommand", "later"] { assert!(back.get(key).is_some(), "{key}"); }
        let fresh: TvControl = serde_json::from_str("{}").unwrap();
        assert_eq!(fresh, TvControl::default());
        assert!(!fresh.is_enabled() && !fresh.has_restore_step());
        assert_eq!(fresh.hdmi_input, 1);
    }

    #[test]
    fn the_input_is_the_users_command_or_the_hdmi_key() {
        let mut tv = TvControl { provider: ANDROID_TV.into(), hdmi_input: 3, ..Default::default() };
        assert_eq!(tv.input_shell(), "input keyevent 245");
        tv.input_command = "  am start -n x/y  ".into();
        assert_eq!(tv.input_shell(), "am start -n x/y");
    }

    #[test]
    fn the_journal_gets_the_going_back_step_only_when_there_is_one() {
        let mut tv = TvControl { provider: ANDROID_TV.into(), host: " 10.0.0.2 ".into(), ..Default::default() };
        assert_eq!(tv.restore_step(), None);
        tv.restore_command = "input keyevent 3".into();
        let step = tv.restore_step().unwrap();
        assert_eq!((step.host.as_str(), step.standby), ("10.0.0.2", false));
        let cfg = step.as_config();
        assert_eq!(restore_commands(&cfg.restore_command, cfg.turn_off_on_restore), ["input keyevent 3"]);
        tv.provider = NONE.into();
        assert_eq!(tv.restore_step(), None, "not controlled: nothing in the journal");
    }

    #[test]
    fn messages_carry_checksum_and_magic_and_decode_back() {
        let banner = connect_banner();
        let bytes = encode(CONNECT, VERSION, MAX_PAYLOAD, &banner);
        assert_eq!(bytes.len(), HEADER_SIZE + banner.len());
        assert_eq!(&bytes[..4], b"CNXN");
        assert_eq!(u32::from_le_bytes(bytes[16..20].try_into().unwrap()), checksum(&banner));
        assert_eq!(u32::from_le_bytes(bytes[20..24].try_into().unwrap()), CONNECT ^ 0xFFFF_FFFF);
        let h = decode_header(&bytes).unwrap();
        assert_eq!(h, Header { command: CONNECT, arg0: VERSION, arg1: MAX_PAYLOAD, length: banner.len(), checksum: checksum(&banner) });
        assert_eq!(&bytes[HEADER_SIZE..], b"host::\0");
    }

    #[test]
    fn broken_packets_are_rejected() {
        let mut bytes = encode(OKAY, 1, 2, &[]);
        bytes[20] ^= 0xFF;
        assert!(decode_header(&bytes).is_err(), "wrong magic");
        assert!(decode_header(&bytes[..10]).is_err(), "short");
        let mut big = encode(WRITE, 1, 2, &[]);
        big[12..16].copy_from_slice(&(MAX_PAYLOAD + 1).to_le_bytes());
        assert!(decode_header(&big).is_err(), "too big");
        assert!(validate_payload(0, b"response").is_err());
        assert!(validate_payload(checksum(b"response"), b"response").is_ok());
    }

    #[test]
    fn shell_output_is_bounded_across_packets() {
        let mut out = ShellOutput::default();
        out.append(b"TV ready").unwrap();
        assert_eq!(out.text(), "TV ready");
        let mut out = ShellOutput::default();
        let packet = vec![0u8; MAX_PAYLOAD as usize];
        for _ in 0..MAX_SHELL_OUTPUT / packet.len() { out.append(&packet).unwrap(); }
        assert!(out.append(&[1]).is_err());
    }

    #[test]
    fn the_handshake_signs_then_offers_the_key_then_gives_up() {
        assert_eq!(on_auth_token(false, false), AuthStep::SendSignature);
        assert_eq!(on_auth_token(true, false), AuthStep::SendPublicKey);
        assert_eq!(on_auth_token(true, true), AuthStep::NotAllowed);
    }

    /// n = 2^2047 + 1: 2^2047 ≡ -1, so 2^4094 ≡ 1 and 2^4096 ≡ 4.
    #[test]
    fn the_public_key_uses_the_android_layout() {
        let mut modulus = [0u8; 256];
        modulus[0] = 0x80;
        modulus[255] = 0x01;
        let encoded = String::from_utf8(encode_public_key(&modulus, 65537, "ConsoleMode2@PC").unwrap()).unwrap();
        assert!(encoded.ends_with(" ConsoleMode2@PC\0"));
        let raw = base64_decode(&encoded[..encoded.find(' ').unwrap()]).unwrap();
        assert_eq!(raw.len(), 524);
        let word = |at: usize| u32::from_le_bytes(raw[at..at + 4].try_into().unwrap());
        assert_eq!(word(0), 64);
        // n0inv = -1 / n mod 2^32
        assert_eq!(word(8).wrapping_mul(word(4)), u32::MAX);
        assert_eq!((word(8), word(8 + 252)), (1, 0x8000_0000), "n little endian");
        assert_eq!(word(264), 4);
        assert!((1..64).all(|i| word(264 + 4 * i) == 0));
        assert_eq!(word(520), 65537);
        // n = 2^2048 - 1: 2^2048 ≡ 1, so 2^4096 ≡ 1
        let all = [0xFFu8; 256];
        let raw = base64_decode(String::from_utf8(encode_public_key(&all, 3, "x").unwrap()).unwrap().split(' ').next().unwrap()).unwrap();
        assert_eq!(u32::from_le_bytes(raw[264..268].try_into().unwrap()), 1);
        assert!(encode_public_key(&[1u8; 128], 3, "x").is_err(), "only RSA-2048");
    }

    #[test]
    fn the_endpoint_defaults_to_port_5555() {
        for (input, host, port) in [("192.168.0.50", "192.168.0.50", 5555), (" 192.168.0.50:5556 ", "192.168.0.50", 5556), ("tv.lan", "tv.lan", 5555), ("192.168.0.50:abc", "192.168.0.50:abc", 5555)] {
            assert_eq!(parse_endpoint(input), (host.to_string(), port), "{input}");
        }
    }

    #[test]
    fn hdmi_inputs_map_to_the_android_tv_input_keys() {
        assert_eq!([hdmi_key(1), hdmi_key(4), hdmi_key(0), hdmi_key(9)], [243, 246, 243, 246]);
    }

    #[test]
    fn going_back_to_the_pc_sends_the_users_command_then_standby() {
        assert!(restore_commands("", false).is_empty());
        assert!(restore_commands("   ", false).is_empty());
        assert_eq!(restore_commands("", true), ["input keyevent 223"]);
        assert_eq!(restore_commands(" input keyevent 3 ", false), ["input keyevent 3"]);
        assert_eq!(restore_commands("input keyevent 3", true), ["input keyevent 3", "input keyevent 223"]);
    }

    #[test]
    fn mac_addresses_parse_in_the_usual_formats() {
        for t in ["AA:BB:CC:DD:EE:FF", "aa-bb-cc-dd-ee-ff", "AABBCCDDEEFF", " aabb.ccdd.eeff "] { assert_eq!(parse_mac(t), Some([0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]), "{t}"); }
        for t in ["", "AA:BB:CC:DD:EE", "GG:BB:CC:DD:EE:FF", "ÁA:BB:CC:DD:EE:F"] { assert_eq!(parse_mac(t), None, "{t}"); }
    }

    #[test]
    fn the_magic_packet_is_ff_then_the_mac_16_times() {
        let mac = [1, 2, 3, 4, 5, 6];
        let p = wol_packet(&mac);
        assert_eq!(p.len(), 102);
        assert_eq!(&p[..6], &[0xFF; 6]);
        assert!(p[6..].chunks(6).all(|c| c == mac));
    }

    #[test]
    fn base64_round_trips() {
        for data in [&b""[..], b"f", b"fo", b"foo", b"foob", b"fooba", b"foobar"] { assert_eq!(base64_decode(&base64_encode(data)).unwrap(), data); }
        assert_eq!(base64_encode(b"foobar"), "Zm9vYmFy");
        assert_eq!(base64_encode(b"fo"), "Zm8=");
        assert_eq!(base64_decode("Zm8=").unwrap(), b"fo");
        assert_eq!(base64_decode("@@"), None);
    }

    #[test]
    fn errors_describe_themselves_in_the_log_without_secrets() {
        assert_eq!(TvError::Unreachable("10.0.0.2".into()).describe(), "a TV em 10.0.0.2 não respondeu");
    }
}
