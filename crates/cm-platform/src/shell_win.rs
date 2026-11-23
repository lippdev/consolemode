//! Alpha-only Windows shell. Registry writes are called only from settings toggles.
use cm_core::control::{self, Command};
use std::{
    io::Write,
    sync::{
        Arc,
        atomic::{AtomicBool, Ordering},
        mpsc,
    },
    time::{Duration, Instant},
};
use windows::{
    Win32::{
        Foundation::*,
        Storage::FileSystem::*,
        System::{LibraryLoader::GetModuleHandleW, Pipes::*, Threading::CreateMutexW},
        UI::{Shell::*, WindowsAndMessaging::*},
    },
    core::{PCWSTR, w},
};
// Give only this user and LocalSystem access, including on machines with permissive defaults.
fn pipe_security() -> windows::core::Result<*mut std::ffi::c_void> {
    use windows::Win32::{
        Security::Authorization::*,
        Security::*,
        System::Threading::{GetCurrentProcess, OpenProcessToken},
    };
    unsafe {
        let mut token = HANDLE::default();
        OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &mut token)?;
        let _token = Handle(token);
        let mut needed = 0;
        let _ = GetTokenInformation(token, TokenUser, None, 0, &mut needed);
        let mut buf = vec![0usize; (needed as usize).div_ceil(std::mem::size_of::<usize>())];
        GetTokenInformation(
            token,
            TokenUser,
            Some(buf.as_mut_ptr().cast()),
            needed,
            &mut needed,
        )?;
        let user = &*(buf.as_ptr() as *const TOKEN_USER);
        let mut sid = windows::core::PWSTR::null();
        ConvertSidToStringSidW(user.User.Sid, &mut sid)?;
        let user_sid = sid.to_string()?;
        let _ = LocalFree(Some(HLOCAL(sid.0.cast())));
        let sddl = wide(&format!("D:P(A;;GA;;;SY)(A;;GA;;;{user_sid})"));
        let mut descriptor = PSECURITY_DESCRIPTOR::default();
        ConvertStringSecurityDescriptorToSecurityDescriptorW(
            PCWSTR(sddl.as_ptr()),
            SDDL_REVISION_1,
            &mut descriptor,
            None,
        )?;
        Ok(descriptor.0)
    }
}
fn wide(s: &str) -> Vec<u16> {
    s.encode_utf16().chain(Some(0)).collect()
}
struct Handle(HANDLE);
impl Drop for Handle {
    fn drop(&mut self) {
        unsafe {
            let _ = CloseHandle(self.0);
        }
    }
}
pub struct Instance {
    _mutex: Handle,
}
/// Tests: `CM2_INSTANCE=<name>` gives a test copy its own mutex and pipe, so it runs beside the
/// owner's copy instead of handing itself over to it.
fn named(base: &str) -> String {
    match std::env::var("CM2_INSTANCE") { Ok(s) if !s.is_empty() => format!("{base}.{s}"), _ => base.to_string() }
}
impl Instance {
    pub fn acquire() -> windows::core::Result<Option<Self>> {
        Self::acquire_named(&named(control::MUTEX))
    }
    fn acquire_named(name: &str) -> windows::core::Result<Option<Self>> {
        unsafe {
            let name = wide(name);
            let h = CreateMutexW(None, false, PCWSTR(name.as_ptr()))?;
            let exists = GetLastError() == ERROR_ALREADY_EXISTS;
            let guard = Self { _mutex: Handle(h) };
            Ok(if exists { None } else { Some(guard) })
        }
    }
}
pub fn forward(command: Command) -> std::io::Result<()> {
    let end = Instant::now() + Duration::from_secs(8);
    loop {
        match std::fs::OpenOptions::new()
            .read(true)
            .write(true)
            .open(named(control::PIPE))
        {
            Ok(mut pipe) => {
                use std::io::{BufRead, BufReader};
                // A newly activated process may be allowed to set foreground even when the
                // existing instance is not. Transfer that permission to the actual pipe
                // server before dispatch; Windows can deny this and the command still works.
                if matches!(command, Command::Show | Command::GamingHome | Command::Start | Command::Menu) {
                    use std::os::windows::io::AsRawHandle;
                    let mut pid = 0;
                    unsafe {
                        let handle = HANDLE(pipe.as_raw_handle());
                        if GetNamedPipeServerProcessId(handle, &mut pid).is_ok() && pid != 0 {
                            let _ = AllowSetForegroundWindow(pid);
                        }
                    }
                }
                writeln!(pipe, "{{\"cmd\":\"{}\"}}", command.name())?;
                let mut reply = String::new();
                BufReader::new(pipe).read_line(&mut reply)?;
                return if reply.is_empty() {
                    Err(std::io::Error::other("A instância não respondeu"))
                } else {
                    Ok(())
                };
            }
            Err(e) if Instant::now() >= end => return Err(e),
            Err(_) => std::thread::sleep(Duration::from_millis(80)),
        }
    }
}
pub struct Request {
    pub command: Option<Command>,
    pub response: mpsc::Sender<String>,
}
pub struct Pipe {
    stop: Arc<AtomicBool>,
    thread: Option<std::thread::JoinHandle<()>>,
}
impl Pipe {
    pub fn start(tx: mpsc::Sender<Request>) -> std::io::Result<Self> {
        Self::start_at(tx, named(control::PIPE))
    }
    fn start_at(tx: mpsc::Sender<Request>, pipe_name: String) -> std::io::Result<Self> {
        let stop = Arc::new(AtomicBool::new(false));
        let flag = stop.clone();
        let (ready_tx, ready_rx) = mpsc::sync_channel(1);
        let thread = std::thread::spawn(move || {
            let descriptor = match pipe_security() {
                Ok(p) => p,
                Err(e) => {
                    let _ = ready_tx.send(Err(std::io::Error::other(e)));
                    return;
                }
            };
            let security = windows::Win32::Security::SECURITY_ATTRIBUTES {
                nLength: std::mem::size_of::<windows::Win32::Security::SECURITY_ATTRIBUTES>()
                    as u32,
                lpSecurityDescriptor: descriptor,
                bInheritHandle: windows::core::BOOL(0),
            };
            let name = wide(&pipe_name);
            let h = unsafe {
                CreateNamedPipeW(
                    PCWSTR(name.as_ptr()),
                    PIPE_ACCESS_DUPLEX | FILE_FLAG_FIRST_PIPE_INSTANCE,
                    PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_NOWAIT | PIPE_REJECT_REMOTE_CLIENTS,
                    1,
                    4096,
                    4096,
                    0,
                    Some(&security),
                )
            };
            let error = std::io::Error::last_os_error();
            unsafe {
                let _ = LocalFree(Some(HLOCAL(descriptor)));
            }
            if h == INVALID_HANDLE_VALUE {
                let _ = ready_tx.send(Err(error));
                return;
            }
            let _guard = Handle(h);
            let _ = ready_tx.send(Ok(()));
            while !flag.load(Ordering::Relaxed) {
                let connected = unsafe {
                    ConnectNamedPipe(h, None).is_ok() || GetLastError() == ERROR_PIPE_CONNECTED
                };
                if !connected {
                    // nobody is calling: look again in a moment (a caller retries for 8 s)
                    std::thread::sleep(Duration::from_millis(200));
                    continue;
                }
                let deadline = Instant::now() + Duration::from_secs(5);
                let mut bytes = Vec::new();
                while Instant::now() < deadline && !flag.load(Ordering::Relaxed) {
                    let mut buf = [0u8; 512];
                    let mut n = 0;
                    unsafe {
                        let _ = ReadFile(h, Some(&mut buf), Some(&mut n), None);
                    }
                    bytes.extend_from_slice(&buf[..n as usize]);
                    if bytes.contains(&b'\n') || bytes.len() > 4096 {
                        break;
                    }
                    std::thread::sleep(Duration::from_millis(15));
                }
                let line = bytes.split(|b| *b == b'\n').next().unwrap_or_default();
                let command = if bytes.len() <= 4096 && bytes.contains(&b'\n') {
                    std::str::from_utf8(line)
                        .ok()
                        .and_then(control::parse_request)
                } else {
                    None
                };
                let (reply_tx, reply_rx) = mpsc::channel();
                if tx
                    .send(Request {
                        command,
                        response: reply_tx,
                    })
                    .is_err()
                {
                    break;
                }
                let deadline = Instant::now() + Duration::from_secs(5);
                while !flag.load(Ordering::Relaxed) && Instant::now() < deadline {
                    if let Ok(reply) = reply_rx.recv_timeout(Duration::from_millis(25)) {
                        let out = format!("{reply}\n");
                        unsafe {
                            let _ = WriteFile(h, Some(out.as_bytes()), None, None);
                        }
                        // Let the client drain the response before DisconnectNamedPipe discards buffers.
                        while Instant::now() < deadline && !flag.load(Ordering::Relaxed) {
                            let mut byte = [0u8];
                            let mut n = 0;
                            let result =
                                unsafe { ReadFile(h, Some(&mut byte), Some(&mut n), None) };
                            if result.is_err() && unsafe { GetLastError() } != ERROR_NO_DATA {
                                break;
                            }
                            std::thread::sleep(Duration::from_millis(15));
                        }
                        break;
                    }
                }
                unsafe {
                    let _ = DisconnectNamedPipe(h);
                }
            }
        });
        ready_rx.recv().map_err(std::io::Error::other)??;
        Ok(Self {
            stop,
            thread: Some(thread),
        })
    }
}
impl Drop for Pipe {
    fn drop(&mut self) {
        self.stop.store(true, Ordering::Relaxed);
        if let Some(t) = self.thread.take() {
            let _ = t.join();
        }
    }
}

pub fn set_startup(enabled: bool) -> Result<(), String> {
    use winreg::{RegKey, enums::*};
    let exe = std::env::current_exe().map_err(|e| e.to_string())?;
    let cmd = control::startup_command(&exe.to_string_lossy())?;
    let (key, _) = RegKey::predef(HKEY_CURRENT_USER)
        .create_subkey(control::RUN_KEY)
        .map_err(|e| e.to_string())?;
    if enabled {
        key.set_value(control::RUN_VALUE, &cmd)
            .map_err(|e| e.to_string())
    } else {
        match key.delete_value(control::RUN_VALUE) {
            Ok(()) => Ok(()),
            Err(e) if e.kind() == std::io::ErrorKind::NotFound => Ok(()),
            Err(e) => Err(e.to_string()),
        }
    }
}
pub fn set_protocol(enabled: bool) -> Result<(), String> {
    use winreg::{RegKey, enums::*};
    let hkcu = RegKey::predef(HKEY_CURRENT_USER);
    if !enabled {
        return match hkcu.delete_subkey_all(control::CLASS_KEY) {
            Ok(()) => Ok(()),
            Err(e) if e.kind() == std::io::ErrorKind::NotFound => Ok(()),
            Err(e) => Err(e.to_string()),
        };
    }
    let exe = std::env::current_exe().map_err(|e| e.to_string())?;
    let cmd = control::protocol_command(&exe.to_string_lossy())?;
    let (key, _) = hkcu
        .create_subkey(control::CLASS_KEY)
        .map_err(|e| e.to_string())?;
    (|| -> std::io::Result<()> {
        key.set_value("", &"URL:Console Mode 2 Alpha")?;
        key.set_value("URL Protocol", &"")?;
        key.create_subkey("DefaultIcon")?.0.set_value(
            "",
            &format!("{},0", control::quoted_exe(&exe.to_string_lossy()).unwrap()),
        )?;
        key.create_subkey(r"shell\open\command")?
            .0
            .set_value("", &cmd)
    })()
    .map_err(|e| e.to_string())
}

fn icon() -> HICON {
    // the same icon as the exe (crates/cm-app/assets), one icon for the whole app
    let data = include_bytes!("../../cm-app/assets/ConsoleMode2.ico");
    // The ICO directory identifies the PNG/DIB resource, not the whole file.
    let count = u16::from_le_bytes(data[4..6].try_into().unwrap()) as usize;
    let entry = (0..count)
        .map(|i| 6 + 16 * i)
        .find(|i| data[*i] == 32)
        .unwrap_or(6);
    let offset = u32::from_le_bytes(data[entry + 12..entry + 16].try_into().unwrap()) as usize;
    let len = u32::from_le_bytes(data[entry + 8..entry + 12].try_into().unwrap()) as usize;
    unsafe {
        CreateIconFromResourceEx(
            &data[offset..offset + len],
            true,
            0x00030000,
            32,
            32,
            LR_DEFAULTCOLOR,
        )
        .unwrap_or_default()
    }
}
pub fn hide_window(w: &gpui::Window) {
    if let Some(h) = super::windows_os::hwnd(w) {
        unsafe {
            let _ = ShowWindow(h, SW_HIDE);
        }
    }
}
pub fn show_window(w: &gpui::Window) {
    if let Some(h) = super::windows_os::hwnd(w) {
        unsafe {
            let _ = ShowWindow(h, SW_SHOW);
            let _ = ShowWindow(h, SW_RESTORE);
        }
    }
    super::force_foreground(w);
}
pub fn set_window_icon(w: &gpui::Window) {
    if let Some(h) = super::windows_os::hwnd(w) {
        // Windows borrows the icon for the lifetime of the app window.
        static ICON: std::sync::OnceLock<usize> = std::sync::OnceLock::new();
        let i = *ICON.get_or_init(|| icon().0 as usize);
        unsafe {
            SendMessageW(h, WM_SETICON, Some(WPARAM(0)), Some(LPARAM(i as isize)));
            SendMessageW(h, WM_SETICON, Some(WPARAM(1)), Some(LPARAM(i as isize)));
        }
    }
}
/// The tray menu's texts (Abrir, Menu da sessão, Encerrar sessão, Sair); the app sets them for its
/// language, and again when the language changes. Portuguese until then.
static TRAY_LABELS: std::sync::Mutex<[String; 4]> = std::sync::Mutex::new([String::new(), String::new(), String::new(), String::new()]);
pub fn set_tray_labels(labels: [String; 4]) {
    *TRAY_LABELS.lock().unwrap_or_else(|e| e.into_inner()) = labels;
}
fn tray_labels() -> [String; 4] {
    let set = TRAY_LABELS.lock().unwrap_or_else(|e| e.into_inner()).clone();
    let fallback = ["Abrir", "Menu da sessão", "Encerrar sessão", "Sair"];
    std::array::from_fn(|i| if set[i].is_empty() { fallback[i].to_string() } else { set[i].clone() })
}
struct TrayState {
    tx: mpsc::Sender<Command>,
    active: Arc<AtomicBool>,
    data: NOTIFYICONDATAW,
}
unsafe extern "system" fn tray_proc(h: HWND, msg: u32, wp: WPARAM, lp: LPARAM) -> LRESULT {
    unsafe {
        let ptr = GetWindowLongPtrW(h, GWLP_USERDATA) as *mut TrayState;
        if !ptr.is_null() {
            let state = &*ptr;
            if msg == WM_APP + 1 {
                if lp.0 as u32 == WM_LBUTTONUP {
                    let _ = state.tx.send(Command::Show);
                }
                if lp.0 as u32 == WM_RBUTTONUP || lp.0 as u32 == WM_CONTEXTMENU {
                    if let Ok(menu) = CreatePopupMenu() {
                        let labels = tray_labels();
                        let wide = |s: &str| -> Vec<u16> { s.encode_utf16().chain(std::iter::once(0)).collect() };
                        let (l_open, l_menu, l_stop, l_quit) = (wide(&labels[0]), wide(&labels[1]), wide(&labels[2]), wide(&labels[3]));
                        let _ = AppendMenuW(menu, MF_STRING, 1, PCWSTR(l_open.as_ptr()));
                        let flags = if state.active.load(Ordering::Relaxed) {
                            MF_STRING
                        } else {
                            MF_STRING | MF_GRAYED
                        };
                        let _ = AppendMenuW(menu, flags, 2, PCWSTR(l_menu.as_ptr()));
                        let _ = AppendMenuW(menu, flags, 3, PCWSTR(l_stop.as_ptr()));
                        let _ = AppendMenuW(menu, MF_SEPARATOR, 0, PCWSTR::null());
                        let _ = AppendMenuW(menu, MF_STRING, 4, PCWSTR(l_quit.as_ptr()));
                        let mut p = POINT::default();
                        let _ = GetCursorPos(&mut p);
                        let _ = SetForegroundWindow(h);
                        let id = TrackPopupMenu(
                            menu,
                            TPM_RETURNCMD | TPM_RIGHTBUTTON,
                            p.x,
                            p.y,
                            None,
                            h,
                            None,
                        )
                        .0;
                        let c = match id {
                            1 => Some(Command::Show),
                            2 => Some(Command::Menu),
                            3 => Some(Command::Stop),
                            4 => Some(Command::Exit),
                            _ => None,
                        };
                        if let Some(c) = c {
                            let _ = state.tx.send(c);
                        }
                        let _ = PostMessageW(Some(h), WM_NULL, WPARAM(0), LPARAM(0));
                        let _ = DestroyMenu(menu);
                    }
                }
                return LRESULT(0);
            }
            if msg == RegisterWindowMessageW(w!("TaskbarCreated")) {
                let _ = Shell_NotifyIconW(NIM_ADD, &state.data);
            }
            if msg == WM_CLOSE {
                let _ = Shell_NotifyIconW(NIM_DELETE, &state.data);
                let _ = DestroyWindow(h);
                return LRESULT(0);
            }
            if msg == WM_DESTROY {
                PostQuitMessage(0);
                return LRESULT(0);
            }
        }
        DefWindowProcW(h, msg, wp, lp)
    }
}
pub struct Tray {
    hwnd: usize,
    thread: Option<std::thread::JoinHandle<()>>,
    pub active: Arc<AtomicBool>,
}
impl Tray {
    pub fn start(tx: mpsc::Sender<Command>) -> Result<Self, String> {
        let active = Arc::new(AtomicBool::new(false));
        let flag = active.clone();
        let (ready, rx) = mpsc::sync_channel(1);
        let thread = std::thread::spawn(move || unsafe {
            let module = GetModuleHandleW(None).unwrap_or_default();
            let class = w!("ConsoleMode2Alpha.Tray");
            let wc = WNDCLASSW {
                lpfnWndProc: Some(tray_proc),
                hInstance: module.into(),
                lpszClassName: class,
                ..Default::default()
            };
            RegisterClassW(&wc);
            let Ok(h) = CreateWindowExW(
                WINDOW_EX_STYLE::default(),
                class,
                w!("Console Mode 2 Alpha"),
                WINDOW_STYLE::default(),
                0,
                0,
                0,
                0,
                None,
                None,
                Some(module.into()),
                None,
            ) else {
                let _ = ready.send(Err("Não foi possível criar a bandeja".to_string()));
                return;
            };
            let i = icon();
            let mut data = NOTIFYICONDATAW {
                cbSize: std::mem::size_of::<NOTIFYICONDATAW>() as u32,
                hWnd: h,
                uID: 2002,
                uFlags: NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage: WM_APP + 1,
                hIcon: i,
                ..Default::default()
            };
            for (d, s) in data
                .szTip
                .iter_mut()
                .zip("Console Mode 2 Alpha".encode_utf16())
            {
                *d = s;
            }
            let mut state = Box::new(TrayState {
                tx,
                active: flag,
                data,
            });
            SetWindowLongPtrW(h, GWLP_USERDATA, &mut *state as *mut _ as isize);
            if !Shell_NotifyIconW(NIM_ADD, &state.data).as_bool() {
                let _ = DestroyWindow(h);
                let _ = DestroyIcon(i);
                let _ = ready.send(Err(
                    "Não foi possível mostrar o ícone da bandeja".to_string()
                ));
                return;
            }
            let _ = ready.send(Ok(h.0 as usize));
            let mut msg = MSG::default();
            while GetMessageW(&mut msg, None, 0, 0).0 > 0 {
                let _ = TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
            let _ = DestroyIcon(i);
        });
        let hwnd = rx.recv().map_err(|e| e.to_string())??;
        Ok(Self {
            hwnd,
            thread: Some(thread),
            active,
        })
    }
}
impl Drop for Tray {
    fn drop(&mut self) {
        unsafe {
            let _ = PostMessageW(
                Some(HWND(self.hwnd as *mut _)),
                WM_CLOSE,
                WPARAM(0),
                LPARAM(0),
            );
        }
        if let Some(t) = self.thread.take() {
            let _ = t.join();
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn bundled_logo_decodes_as_a_native_icon() {
        let h = icon();
        assert!(!h.is_invalid());
        unsafe {
            DestroyIcon(h).unwrap();
        }
    }
    #[test]
    fn mutex_releases_on_drop() {
        let name = format!(r"Local\ConsoleMode2Alpha.Test.{}", std::process::id());
        let first = Instance::acquire_named(&name).unwrap().unwrap();
        assert!(Instance::acquire_named(&name).unwrap().is_none());
        drop(first);
        assert!(Instance::acquire_named(&name).unwrap().is_some());
    }
    #[test]
    fn pipe_round_trip_and_shutdown() {
        use std::io::{BufRead, BufReader};
        let name = format!(r"\\.\pipe\ConsoleMode2Alpha.Test.{}", std::process::id());
        let (tx, rx) = mpsc::channel();
        let server = Pipe::start_at(tx, name.clone()).unwrap();
        let client = std::thread::spawn(move || {
            let deadline = Instant::now() + Duration::from_secs(3);
            let mut file = loop {
                match std::fs::OpenOptions::new()
                    .read(true)
                    .write(true)
                    .open(&name)
                {
                    Ok(f) => break f,
                    Err(e) => {
                        assert!(Instant::now() < deadline, "{e}");
                        std::thread::sleep(Duration::from_millis(20));
                    }
                }
            };
            writeln!(file, r#"{{"cmd":"status"}}"#).unwrap();
            let mut line = String::new();
            BufReader::new(file).read_line(&mut line).unwrap();
            assert_eq!(line.trim(), r#"{"ok":true}"#);
        });
        let request = rx.recv_timeout(Duration::from_secs(3)).unwrap();
        assert_eq!(request.command, Some(Command::Status));
        request.response.send(r#"{"ok":true}"#.into()).unwrap();
        client.join().unwrap();
        drop(server);
    }
}
