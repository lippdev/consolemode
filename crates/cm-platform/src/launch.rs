//! Starting the launchers and finding their windows (ported from 1.7's LaunchService and
//! NativeWindows). Opening apps and asking windows to close; nothing here touches the screens.

use std::{os::windows::{ffi::OsStringExt, process::CommandExt}, path::Path, process::Command};

use windows::{
    Win32::{
        Foundation::{CloseHandle, HWND, LPARAM, RECT, WPARAM},
        Graphics::Gdi::{GetMonitorInfoW, MONITOR_DEFAULTTONEAREST, MONITORINFO, MonitorFromWindow},
        System::Diagnostics::ToolHelp::{CreateToolhelp32Snapshot, PROCESSENTRY32W, Process32FirstW, Process32NextW, TH32CS_SNAPPROCESS},
        UI::{
            Input::KeyboardAndMouse::{KEYEVENTF_KEYUP, VK_F11, VK_LWIN, VK_MENU, VK_RETURN, keybd_event},
            Shell::ShellExecuteW,
            WindowsAndMessaging::{
                EnumWindows, GetClassNameW, GetForegroundWindow, GetWindowRect, GetWindowTextW, GetWindowThreadProcessId, IsIconic, IsWindow, IsWindowVisible,
                PostMessageW, SW_SHOWNORMAL, SetWindowPos, HWND_TOP, SWP_NOSIZE, SWP_NOZORDER, WM_CLOSE,
            },
        },
    },
    core::{BOOL, HSTRING, PCWSTR},
};

/// A visible top-level window.
#[derive(Clone, Debug)]
pub struct Win {
    pub hwnd: isize,
    pub title: String,
    pub class: String,
    pub pid: u32,
    pub rect: (i32, i32, i32, i32),
}

impl Win {
    pub fn area(&self) -> i64 { ((self.rect.2 - self.rect.0).max(0) as i64) * ((self.rect.3 - self.rect.1).max(0) as i64) }
    fn handle(&self) -> HWND { HWND(self.hwnd as *mut _) }
}

const CREATE_NO_WINDOW: u32 = 0x0800_0000;
const DETACHED_PROCESS: u32 = 0x0000_0008;

/// Opens a URL or file the way Explorer would (steam://..., playnite://...).
pub fn open(target: &str) -> bool {
    unsafe { ShellExecuteW(None, &HSTRING::from("open"), &HSTRING::from(target), PCWSTR::null(), PCWSTR::null(), SW_SHOWNORMAL).0 as isize > 32 }
}

pub fn start_big_picture() -> bool { open("steam://open/bigpicture") }

/// Xbox mode: Windows' own full-screen experience, opened with Win + F11 (as 1.7).
pub fn start_xbox() {
    unsafe {
        keybd_event(VK_LWIN.0 as u8, 0, Default::default(), 0);
        keybd_event(VK_F11.0 as u8, 0, Default::default(), 0);
        keybd_event(VK_F11.0 as u8, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_LWIN.0 as u8, 0, KEYEVENTF_KEYUP, 0);
    }
}

/// Starts an exe from its own folder (emulators look for their files there), detached. Its pid.
pub fn start_exe(exe: &str, args: &str) -> Option<u32> {
    let exe = exe.trim().trim_matches('"');
    let dir = Path::new(exe).parent().map(|p| p.to_path_buf()).unwrap_or_default();
    let mut cmd = Command::new(exe);
    cmd.current_dir(dir).creation_flags(DETACHED_PROCESS);
    if !args.trim().is_empty() { cmd.raw_arg(args.trim()); }
    cmd.spawn().ok().map(|c| c.id())
}

/// Playnite's full-screen app: the folder/exe chosen in the config, the usual places, its uninstall key.
pub fn playnite_exe(custom: &str) -> Option<String> {
    let c = custom.trim().trim_matches('"');
    if !c.is_empty() {
        let p = Path::new(c);
        let exe = if p.is_dir() { p.join("Playnite.FullscreenApp.exe") } else { p.to_path_buf() };
        if exe.exists() { return Some(exe.to_string_lossy().into_owned()); }
    }
    let local = std::env::var("LOCALAPPDATA").unwrap_or_default();
    let mut tries = vec![format!(r"{local}\Playnite\Playnite.FullscreenApp.exe")];
    tries.push(r"C:\Program Files\Playnite\Playnite.FullscreenApp.exe".into());
    tries.push(r"C:\Program Files (x86)\Playnite\Playnite.FullscreenApp.exe".into());
    if let Ok(key) = winreg::RegKey::predef(winreg::enums::HKEY_CURRENT_USER).open_subkey(r"Software\Microsoft\Windows\CurrentVersion\Uninstall\Playnite_is1") {
        if let Ok(dir) = key.get_value::<String, _>("InstallLocation") { tries.push(format!(r"{dir}\Playnite.FullscreenApp.exe")); }
    }
    tries.into_iter().find(|p| Path::new(p).exists())
}

/// Every running process: (pid, parent pid, exe name).
pub fn processes() -> Vec<(u32, u32, String)> {
    let mut out = Vec::new();
    unsafe {
        let Ok(snap) = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0) else { return out };
        let mut e = PROCESSENTRY32W { dwSize: std::mem::size_of::<PROCESSENTRY32W>() as u32, ..Default::default() };
        if Process32FirstW(snap, &mut e).is_ok() {
            loop {
                let len = e.szExeFile.iter().position(|&c| c == 0).unwrap_or(e.szExeFile.len());
                out.push((e.th32ProcessID, e.th32ParentProcessID, String::from_utf16_lossy(&e.szExeFile[..len])));
                if Process32NextW(snap, &mut e).is_err() { break; }
            }
        }
        let _ = CloseHandle(snap);
    }
    out
}

/// Is a process with this name (case-insensitive, with or without .exe) running?
pub fn is_running(name: &str) -> bool {
    let n = cm_name(name);
    processes().iter().any(|(_, _, exe)| cm_name(exe) == n)
}

/// True only while an Xbox app window is actually foreground; other games and apps must
/// never be displaced by Gaming Home's activation fallback.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum FseForeground { Xbox, Confirmation, Other }

pub fn fse_foreground() -> FseForeground {
    use windows::Win32::UI::WindowsAndMessaging::GetForegroundWindow;
    let mut pid = 0;
    unsafe { GetWindowThreadProcessId(GetForegroundWindow(), Some(&mut pid)); }
    let processes = processes();
    let name = processes.iter().find(|(p, _, _)| *p == pid).map(|(_, _, n)| n.as_str()).unwrap_or("");
    let hosted = name.eq_ignore_ascii_case("ApplicationFrameHost.exe");
    let has_child = |predicate: fn(&str) -> bool| {
        let ids: Vec<u32> = processes.iter().filter(|(_, _, n)| predicate(n)).map(|(p, _, _)| *p).collect();
        foreground_has_child(&ids)
    };
    if is_xbox_process(name) || (hosted && (has_child(is_xbox_process) || foreground_is_xbox_aumid())) { FseForeground::Xbox }
    else if is_gamebar_process(name) || (hosted && has_child(is_gamebar_process)) { FseForeground::Confirmation }
    else { FseForeground::Other }
}

fn foreground_is_xbox_aumid() -> bool {
    use windows::{core::GUID, Win32::{Foundation::PROPERTYKEY, System::Com::{CoInitializeEx, CoUninitialize, CoTaskMemFree, COINIT_MULTITHREADED, StructuredStorage::PropVariantToStringAlloc}, UI::{Shell::PropertiesSystem::{IPropertyStore, SHGetPropertyStoreForWindow}, WindowsAndMessaging::GetForegroundWindow}}};
    const APP_ID: PROPERTYKEY = PROPERTYKEY { fmtid: GUID::from_u128(0x9f4c2855_9f79_4b39_a8d0_e1d42de1d5f3), pid: 5 };
    unsafe {
        let initialized = CoInitializeEx(None, COINIT_MULTITHREADED).is_ok();
        let result = (|| {
            let store: IPropertyStore = SHGetPropertyStoreForWindow(GetForegroundWindow()).ok()?;
            let value = store.GetValue(&APP_ID).ok()?;
            let value = PropVariantToStringAlloc(&value).ok()?;
            let id = value.to_string().ok();
            CoTaskMemFree(Some(value.0.cast()));
            id
        })();
        if initialized { CoUninitialize(); }
        result.is_some_and(|id| id.eq_ignore_ascii_case("Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.AppL") || id.eq_ignore_ascii_case("Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App"))
    }
}

fn foreground_has_child(ids: &[u32]) -> bool {
    unsafe extern "system" fn child(hwnd: HWND, param: LPARAM) -> BOOL {
        let state = unsafe { &mut *(param.0 as *mut (&[u32], bool)) };
        let mut pid = 0;
        unsafe { GetWindowThreadProcessId(hwnd, Some(&mut pid)); }
        if state.0.contains(&pid) { state.1 = true; return BOOL(0); }
        BOOL(1)
    }
    let mut state = (ids, false);
    unsafe { let _ = windows::Win32::UI::WindowsAndMessaging::EnumChildWindows(Some(windows::Win32::UI::WindowsAndMessaging::GetForegroundWindow()), Some(child), LPARAM((&mut state as *mut _) as isize)); }
    state.1
}

fn is_xbox_process(name: &str) -> bool {
    name.eq_ignore_ascii_case("XboxPcApp.exe") || name.eq_ignore_ascii_case("XboxPcAppCE.exe")
}
fn is_gamebar_process(name: &str) -> bool {
    ["GameBar.exe", "GameBarFTServer.exe", "GameBarPresenceWriter.exe"].iter().any(|n| name.eq_ignore_ascii_case(n))
}

/// Launch the registered Home through Windows Explorer, using the Windows directory instead
/// of searching the caller's working directory or PATH.
pub fn activate_gaming_home() -> std::io::Result<()> {
    use windows::Win32::System::SystemInformation::GetWindowsDirectoryW;
    let mut buffer = [0u16; 32768];
    let len = unsafe { GetWindowsDirectoryW(Some(&mut buffer)) } as usize;
    if len == 0 || len >= buffer.len() { return Err(std::io::Error::last_os_error()); }
    let dir = std::ffi::OsString::from_wide(&buffer[..len]);
    let explorer = std::path::PathBuf::from(dir).join("explorer.exe");
    std::process::Command::new(explorer).arg(format!(r"shell:AppsFolder\{}", crate::fse_win::AUMID)).spawn()?;
    Ok(())
}

#[cfg(test)]
mod foreground_tests {
    #[test]
    fn fallback_only_targets_xbox_processes() {
        assert!(super::is_xbox_process("XboxPcApp.exe"));
        assert!(super::is_xbox_process("xboxpcappce.EXE"));
        assert!(!super::is_xbox_process("Playnite.FullscreenApp.exe"));
        assert!(!super::is_xbox_process("XboxGame.exe"));
    }
}

fn cm_name(s: &str) -> String { let s = s.to_ascii_lowercase(); s.strip_suffix(".exe").unwrap_or(&s).to_string() }

fn text(h: HWND, class: bool) -> String {
    let mut buf = [0u16; 512];
    let n = unsafe { if class { GetClassNameW(h, &mut buf) } else { GetWindowTextW(h, &mut buf) } };
    String::from_utf16_lossy(&buf[..n.max(0) as usize])
}

/// All visible top-level windows.
pub fn windows() -> Vec<Win> {
    unsafe extern "system" fn each(h: HWND, l: LPARAM) -> BOOL {
        let out = unsafe { &mut *(l.0 as *mut Vec<Win>) };
        if unsafe { IsWindowVisible(h) }.as_bool() {
            let mut r = RECT::default();
            let _ = unsafe { GetWindowRect(h, &mut r) };
            let mut pid = 0;
            unsafe { GetWindowThreadProcessId(h, Some(&mut pid)) };
            out.push(Win { hwnd: h.0 as isize, title: text(h, false), class: text(h, true), pid, rect: (r.left, r.top, r.right, r.bottom) });
        }
        BOOL(1)
    }
    let mut out: Vec<Win> = Vec::new();
    unsafe { let _ = EnumWindows(Some(each), LPARAM(&mut out as *mut _ as isize)); }
    out
}

/// The window covers its whole monitor.
pub fn is_fullscreen(w: &Win) -> bool {
    unsafe {
        let m = MonitorFromWindow(w.handle(), MONITOR_DEFAULTTONEAREST);
        let mut info = MONITORINFO { cbSize: std::mem::size_of::<MONITORINFO>() as u32, ..Default::default() };
        if !GetMonitorInfoW(m, &mut info).as_bool() { return false; }
        let r = info.rcMonitor;
        w.rect.0 <= r.left && w.rect.1 <= r.top && w.rect.2 >= r.right && w.rect.3 >= r.bottom
    }
}

/// Big Picture's window: titled "Big Picture", or an SDL_app filling its screen (the normal Steam
/// window is an SDL_app too, so a windowed one doesn't count), best match first.
pub fn big_picture_window() -> Option<Win> {
    let names: Vec<(u32, String)> = processes().into_iter().map(|(p, _, n)| (p, cm_name(&n))).collect();
    let is_steam = |pid: u32| names.iter().any(|(p, n)| *p == pid && (n == "steamwebhelper" || n == "steam"));
    windows()
        .into_iter()
        .filter(|w| is_steam(w.pid) && (w.title.to_ascii_lowercase().contains("big picture") || (w.class == "SDL_app" && is_fullscreen(w))))
        .max_by_key(|w| w.area() + if w.title.to_ascii_lowercase().contains("big picture") { 1_000_000_000 } else { 0 })
}

/// Use Steam's native Big Picture fullscreen shortcut only on its foreground window.
pub fn fullscreen_big_picture(window: &Win) -> bool {
    if is_fullscreen(window) { return true; }
    unsafe {
        if GetForegroundWindow() != window.handle() { return false; }
        keybd_event(VK_MENU.0 as u8, 0, Default::default(), 0);
        keybd_event(VK_RETURN.0 as u8, 0, Default::default(), 0);
        keybd_event(VK_RETURN.0 as u8, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_MENU.0 as u8, 0, KEYEVENTF_KEYUP, 0);
    }
    true
}

/// Windows of a process (by exe name), largest first.
pub fn process_windows(name: &str) -> Vec<Win> {
    let n = cm_name(name);
    let pids: Vec<u32> = processes().into_iter().filter(|(_, _, e)| cm_name(e) == n).map(|(p, ..)| p).collect();
    let mut w: Vec<Win> = windows().into_iter().filter(|w| pids.contains(&w.pid)).collect();
    w.sort_by_key(|w| -w.area());
    w
}

pub const BIG_WINDOW_AREA: i64 = 200_000;

/// Playnite's full-screen window, when it is showing.
pub fn playnite_window() -> Option<Win> { process_windows("Playnite.FullscreenApp").into_iter().find(|w| w.area() > BIG_WINDOW_AREA) }

/// The window exists but is hidden or minimized (Playnite while a game it started runs).
pub fn is_put_away(hwnd: isize) -> bool {
    let h = HWND(hwnd as *mut _);
    unsafe { hwnd != 0 && IsWindow(Some(h)).as_bool() && (!IsWindowVisible(h).as_bool() || IsIconic(h).as_bool()) }
}

pub fn still_visible(hwnd: isize) -> bool {
    let h = HWND(hwnd as *mut _);
    unsafe { hwnd != 0 && IsWindow(Some(h)).as_bool() && IsWindowVisible(h).as_bool() && !IsIconic(h).as_bool() }
}

/// Asks a window to close, like its X button (never a kill).
pub fn close_window(hwnd: isize) { unsafe { let _ = PostMessageW(Some(HWND(hwnd as *mut _)), WM_CLOSE, WPARAM(0), LPARAM(0)); } }

/// Moves a window so it sits on the given screen rectangle (keeps its size).
pub fn move_to(hwnd: isize, rect: (i32, i32, i32, i32)) -> bool {
    unsafe { SetWindowPos(HWND(hwnd as *mut _), Some(HWND_TOP), rect.0, rect.1, 0, 0, SWP_NOSIZE | SWP_NOZORDER).is_ok() }
}

/// Leaves Big Picture through Steam's own URL, then asks its window to close if it stays (only a
/// Steam window: a game in full screen can look like Big Picture). True when it is gone.
pub fn close_big_picture(timeout_ms: u64) -> bool {
    let Some(first) = big_picture_window() else { return true };
    open("steam://close/bigpicture");
    let deadline = std::time::Instant::now() + std::time::Duration::from_millis(timeout_ms);
    let mut nudged = false;
    while std::time::Instant::now() < deadline {
        std::thread::sleep(std::time::Duration::from_millis(400));
        let Some(w) = big_picture_window() else { return true };
        if !nudged && deadline - std::time::Instant::now() < std::time::Duration::from_millis(timeout_ms / 2) {
            nudged = true;
            for h in [first.hwnd, w.hwnd] { close_window(h); }
        }
    }
    big_picture_window().is_none()
}

#[allow(dead_code)]
fn _unused() { let _ = CREATE_NO_WINDOW; }
