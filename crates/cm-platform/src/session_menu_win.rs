//! What the session menu acts on (1.7 WindowSwitcher, TaskViewDriver, AudioService volume):
//! the windows Alt + Tab would list, bringing one to the front or asking it to close, Windows'
//! Task View, and the default output's volume.

use cm_core::windows_list::{WindowFacts, process_display_name, should_list};
use windows::{
    Win32::{
        Foundation::{CloseHandle, HWND, LPARAM, WPARAM},
        Graphics::Dwm::{DWMWA_CLOAKED, DwmGetWindowAttribute},
        Media::Audio::{Endpoints::IAudioEndpointVolume, IMMDeviceEnumerator, MMDeviceEnumerator, eConsole, eRender},
        System::{
            Com::{CLSCTX_ALL, COINIT_MULTITHREADED, CoCreateInstance, CoInitializeEx},
            Threading::{AttachThreadInput, GetCurrentProcessId, GetCurrentThreadId, OpenProcess, PROCESS_NAME_WIN32, PROCESS_QUERY_LIMITED_INFORMATION, QueryFullProcessImageNameW},
        },
        UI::{
            Input::KeyboardAndMouse::{KEYEVENTF_EXTENDEDKEY, KEYEVENTF_KEYUP, VK_LWIN, VK_TAB, keybd_event},
            WindowsAndMessaging::{
                BringWindowToTop, EnumWindows, GW_OWNER, GWL_EXSTYLE, GetClassNameW, GetForegroundWindow, GetWindow, GetWindowLongPtrW, GetWindowTextW,
                GetWindowThreadProcessId, IsIconic, IsWindow, IsWindowVisible, PostMessageW, SW_RESTORE, SetForegroundWindow, ShowWindow, WM_CLOSE,
                WS_EX_APPWINDOW, WS_EX_TOOLWINDOW,
            },
        },
    },
    core::{BOOL, PWSTR},
};

/// One entry of "Janelas".
#[derive(Clone, Debug)]
pub struct SwitchWindow { pub hwnd: isize, pub title: String, pub process: String, pub exe: Option<String> }

/// Windows Alt + Tab would list, front to back.
pub fn list_windows() -> Vec<SwitchWindow> {
    unsafe extern "system" fn each(h: HWND, l: LPARAM) -> BOOL {
        let out = unsafe { &mut *(l.0 as *mut Vec<SwitchWindow>) };
        unsafe {
            let visible = IsWindowVisible(h).as_bool();
            if !visible { return BOOL(1); }
            let mut pid = 0;
            GetWindowThreadProcessId(h, Some(&mut pid));
            let mut t = [0u16; 256];
            let tn = GetWindowTextW(h, &mut t);
            let mut c = [0u16; 128];
            let cn = GetClassNameW(h, &mut c);
            let style = GetWindowLongPtrW(h, GWL_EXSTYLE) as u32;
            let mut cloaked = 0u32;
            let _ = DwmGetWindowAttribute(h, DWMWA_CLOAKED, &mut cloaked as *mut _ as *mut _, 4);
            let facts = WindowFacts {
                title: String::from_utf16_lossy(&t[..tn.max(0) as usize]),
                class: String::from_utf16_lossy(&c[..cn.max(0) as usize]),
                visible,
                cloaked: cloaked != 0,
                has_owner: GetWindow(h, GW_OWNER).is_ok_and(|o| !o.is_invalid()),
                tool_window: style & WS_EX_TOOLWINDOW.0 != 0,
                app_window: style & WS_EX_APPWINDOW.0 != 0,
                own_process: pid == GetCurrentProcessId(),
            };
            if should_list(&facts) {
                let exe = exe_of(pid);
                let file = exe.as_deref().and_then(|e| e.rsplit('\\').next()).unwrap_or("");
                out.push(SwitchWindow { hwnd: h.0 as isize, title: facts.title, process: process_display_name(file), exe });
            }
        }
        BOOL(1)
    }
    let mut out = Vec::new();
    unsafe { let _ = EnumWindows(Some(each), LPARAM(&mut out as *mut _ as isize)); }
    out
}

fn exe_of(pid: u32) -> Option<String> {
    unsafe {
        let h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid).ok()?;
        let mut buf = [0u16; 1024];
        let mut len = buf.len() as u32;
        let ok = QueryFullProcessImageNameW(h, PROCESS_NAME_WIN32, PWSTR(buf.as_mut_ptr()), &mut len).is_ok();
        let _ = CloseHandle(h);
        ok.then(|| String::from_utf16_lossy(&buf[..len as usize]))
    }
}

/// Brings a window to the front (restoring it if minimized), borrowing the foreground thread's input
/// queue like 1.7 so Windows allows it.
pub fn activate(hwnd: isize) -> bool {
    let h = HWND(hwnd as *mut _);
    unsafe {
        if hwnd == 0 || !IsWindow(Some(h)).as_bool() { return false; }
        if IsIconic(h).as_bool() { let _ = ShowWindow(h, SW_RESTORE); }
        let fg = GetForegroundWindow();
        let theirs = GetWindowThreadProcessId(fg, None);
        let ours = GetCurrentThreadId();
        let attached = theirs != 0 && theirs != ours && AttachThreadInput(ours, theirs, true).as_bool();
        let _ = BringWindowToTop(h);
        let ok = SetForegroundWindow(h).as_bool();
        if attached { let _ = AttachThreadInput(ours, theirs, false); }
        ok
    }
}

/// Asks a window to close, like its X button (it may still ask to save).
pub fn request_close(hwnd: isize) {
    let h = HWND(hwnd as *mut _);
    unsafe { if hwnd != 0 && IsWindow(Some(h)).as_bool() { let _ = PostMessageW(Some(h), WM_CLOSE, WPARAM(0), LPARAM(0)); } }
}

/// Opens Windows' Task View (Win + Tab).
pub fn open_task_view() {
    unsafe {
        keybd_event(VK_LWIN.0 as u8, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_TAB.0 as u8, 0, Default::default(), 0);
        keybd_event(VK_TAB.0 as u8, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_LWIN.0 as u8, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }
}

fn endpoint_volume() -> Option<IAudioEndpointVolume> {
    unsafe {
        let _ = CoInitializeEx(None, COINIT_MULTITHREADED);
        let e: IMMDeviceEnumerator = CoCreateInstance(&MMDeviceEnumerator, None, CLSCTX_ALL).ok()?;
        let d = e.GetDefaultAudioEndpoint(eRender, eConsole).ok()?;
        d.Activate::<IAudioEndpointVolume>(CLSCTX_ALL, None).ok()
    }
}

/// The default output's volume, 0–100.
pub fn volume() -> Option<u8> {
    unsafe { endpoint_volume()?.GetMasterVolumeLevelScalar().ok().map(|v| (v * 100.).round().clamp(0., 100.) as u8) }
}

/// Sets the default output's volume, 0–100.
pub fn set_volume(percent: u8) -> bool {
    unsafe { endpoint_volume().is_some_and(|v| v.SetMasterVolumeLevelScalar(percent.min(100) as f32 / 100., std::ptr::null()).is_ok()) }
}
