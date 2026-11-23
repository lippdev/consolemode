//! Win32 pieces: official app icons read at runtime (never bundled), taking the foreground, local time.

use gpui::RenderImage;
use image::{Frame, RgbaImage};
use windows::Win32::{
    Foundation::HWND,
    Graphics::Gdi::{BI_RGB, BITMAP, BITMAPINFO, BITMAPINFOHEADER, DIB_RGB_COLORS, DeleteObject, GetDC, GetDIBits, GetObjectW, ReleaseDC},
    System::{SystemInformation::GetLocalTime, Threading::{AttachThreadInput, GetCurrentThreadId}},
    UI::WindowsAndMessaging::{
        BringWindowToTop, DestroyIcon, GetForegroundWindow, GetIconInfo, GetWindowThreadProcessId, HICON, ICONINFO, PrivateExtractIconsW,
        SetForegroundWindow,
    },
};

/// The exe's first icon at `size` px, as the BGRA image gpui draws.
pub fn exe_icon(path: &str, size: i32) -> Option<RenderImage> {
    if !std::path::Path::new(path).exists() { return None; }
    let mut name = [0u16; 260];
    for (i, c) in path.encode_utf16().take(259).enumerate() { name[i] = c; }
    unsafe {
        let mut icons = [HICON::default()];
        let n = PrivateExtractIconsW(&name, 0, size, size, Some(&mut icons), None, 0);
        if n == 0 || icons[0].is_invalid() { return None; }
        let icon = icons[0];
        let mut info = ICONINFO::default();
        if GetIconInfo(icon, &mut info).is_err() { let _ = DestroyIcon(icon); return None; }
        let mut bm = BITMAP::default();
        GetObjectW(info.hbmColor.into(), std::mem::size_of::<BITMAP>() as i32, Some(&mut bm as *mut _ as *mut _));
        let (w, h) = (bm.bmWidth, bm.bmHeight);
        let mut bi = BITMAPINFO {
            bmiHeader: BITMAPINFOHEADER { biSize: std::mem::size_of::<BITMAPINFOHEADER>() as u32, biWidth: w, biHeight: -h, biPlanes: 1, biBitCount: 32, biCompression: BI_RGB.0, ..Default::default() },
            ..Default::default()
        };
        let mut px = vec![0u8; (w.max(0) * h.max(0) * 4) as usize];
        let dc = GetDC(None);
        let lines = GetDIBits(dc, info.hbmColor, 0, h as u32, Some(px.as_mut_ptr() as *mut _), &mut bi, DIB_RGB_COLORS);
        ReleaseDC(None, dc);
        let _ = DeleteObject(info.hbmColor.into());
        let _ = DeleteObject(info.hbmMask.into());
        let _ = DestroyIcon(icon);
        if lines == 0 { return None; }
        // a DIB is already B,G,R,A
        Some(RenderImage::new(vec![Frame::new(RgbaImage::from_raw(w as u32, h as u32, px)?)]))
    }
}

/// The Xbox app's own logo, from its package folder (256 px, ships with the app).
pub fn xbox_logo() -> Option<RenderImage> { xbox_asset("Xbox_AppList.targetsize-256_altform-unplated.png") }

/// The same logo without its green plate: white on transparent (the high-contrast asset).
pub fn xbox_logo_mono() -> Option<RenderImage> { xbox_asset("Xbox_AppList.targetsize-256_contrast-black.png") }

fn xbox_asset(file: &str) -> Option<RenderImage> {
    let out = std::process::Command::new("powershell")
        .args(["-NoProfile", "-NonInteractive", "-Command", "(Get-AppxPackage Microsoft.GamingApp).InstallLocation"])
        .creation_flags_no_window()
        .output()
        .ok()?;
    let dir = String::from_utf8_lossy(&out.stdout).trim().to_string();
    if dir.is_empty() { return None; }
    let path = std::path::Path::new(&dir).join("assets").join(file);
    let mut rgba = image::open(&path).ok()?.to_rgba8();
    for p in rgba.chunks_exact_mut(4) { p.swap(0, 2); }
    Some(RenderImage::new(vec![Frame::new(rgba)]))
}

trait NoWindow { fn creation_flags_no_window(&mut self) -> &mut Self; }
impl NoWindow for std::process::Command {
    fn creation_flags_no_window(&mut self) -> &mut Self {
        use std::os::windows::process::CommandExt;
        self.creation_flags(0x0800_0000) // CREATE_NO_WINDOW
    }
}

pub(crate) fn hwnd(window: &gpui::Window) -> Option<HWND> {
    use raw_window_handle::{HasWindowHandle, RawWindowHandle};
    match HasWindowHandle::window_handle(window).ok()?.as_raw() {
        RawWindowHandle::Win32(h) => Some(HWND(h.hwnd.get() as *mut _)),
        _ => None,
    }
}

/// Bring our window in front, even when another program has the foreground (same trick as 1.7's
/// NativeWindows.cs: borrow the foreground thread's input queue for the call). True when it worked.
pub fn force_foreground(window: &gpui::Window) -> bool {
    let Some(h) = hwnd(window) else { return false };
    unsafe {
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

/// "Windows 11 Pro 24H2 (build 26100.2605, X64)" for the diagnostic report (read from the registry; no rights needed).
pub fn describe_windows() -> String {
    use winreg::{RegKey, enums::HKEY_LOCAL_MACHINE};
    let key = RegKey::predef(HKEY_LOCAL_MACHINE).open_subkey(r"SOFTWARE\Microsoft\Windows NT\CurrentVersion").ok();
    let text = |name: &str| key.as_ref().and_then(|k| k.get_value::<String, _>(name).ok()).unwrap_or_default();
    let build: u32 = text("CurrentBuildNumber").trim().parse().unwrap_or(0);
    let ubr = key.as_ref().and_then(|k| k.get_value::<u32, _>("UBR").ok()).map(|u| format!(".{u}")).unwrap_or_default();
    let name = if build >= 22000 { "Windows 11" } else { "Windows 10" };
    let parts: Vec<String> = [name.to_string(), text("EditionID"), text("DisplayVersion")].into_iter().filter(|p| !p.trim().is_empty()).collect();
    format!("{} (build {build}{ubr}, {})", parts.join(" "), std::env::consts::ARCH)
}

/// The primary screen (left, top, right, bottom) in pixels.
pub fn primary_rect() -> Option<(i32, i32, i32, i32)> {
    use windows::Win32::UI::WindowsAndMessaging::{GetSystemMetrics, SM_CXSCREEN, SM_CYSCREEN};
    let (w, h) = unsafe { (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN)) };
    (w > 0 && h > 0).then_some((0, 0, w, h))
}

/// (hour, minute) on this PC's clock.
pub fn local_time() -> (u16, u16) {
    let t = unsafe { GetLocalTime() };
    (t.wHour, t.wMinute)
}

/// Windows' "Animation effects" (Settings → Accessibility → Visual effects). Off means reduced motion.
pub fn animations_enabled() -> bool {
    use windows::Win32::UI::WindowsAndMessaging::{SPI_GETCLIENTAREAANIMATION, SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS, SystemParametersInfoW};
    let mut on = windows::core::BOOL(1);
    unsafe { let _ = SystemParametersInfoW(SPI_GETCLIENTAREAANIMATION, 0, Some(&mut on as *mut _ as *mut _), SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS(0)); }
    on.as_bool()
}

/// Keeps our window above everything (the session menu over a game), or lets it go back to normal.
pub fn set_topmost(window: &gpui::Window, on: bool) {
    use windows::Win32::UI::WindowsAndMessaging::{HWND_NOTOPMOST, HWND_TOPMOST, SWP_NOMOVE, SWP_NOSIZE, SWP_SHOWWINDOW, SetWindowPos};
    let Some(h) = hwnd(window) else { return };
    unsafe { let _ = SetWindowPos(h, Some(if on { HWND_TOPMOST } else { HWND_NOTOPMOST }), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW); }
}

/// Puts the window on a screen's rectangle (left, top, right, bottom, in pixels), above everything, shown. A
/// window that is full screen stays on the screen it is on: leave full screen before moving it.
pub fn move_window_to(window: &gpui::Window, rect: (i32, i32, i32, i32)) {
    use windows::Win32::UI::WindowsAndMessaging::{HWND_TOPMOST, SWP_SHOWWINDOW, SetWindowPos};
    let Some(h) = hwnd(window) else { return };
    unsafe { let _ = SetWindowPos(h, Some(HWND_TOPMOST), rect.0, rect.1, (rect.2 - rect.0).max(320), (rect.3 - rect.1).max(240), SWP_SHOWWINDOW); }
}

/// Brings a minimized window back (ShowWindow SW_RESTORE): activating alone leaves it minimized.
pub fn restore_window(window: &gpui::Window) {
    use windows::Win32::UI::WindowsAndMessaging::{IsIconic, SW_RESTORE, ShowWindow};
    let Some(h) = hwnd(window) else { return };
    unsafe { if IsIconic(h).as_bool() { let _ = ShowWindow(h, SW_RESTORE); } }
}

/// Windows' display language (what the user picked for Windows itself), as a LANGID such as 0x0416
/// (pt-BR). The low 10 bits are the language: 0x16 pt, 0x09 en, 0x0a es, 0x07 de, 0x0c fr, 0x19 ru.
pub fn system_ui_language_id() -> u16 {
    #[link(name = "kernel32")]
    unsafe extern "system" { fn GetUserDefaultUILanguage() -> u16; }
    unsafe { GetUserDefaultUILanguage() }
}
