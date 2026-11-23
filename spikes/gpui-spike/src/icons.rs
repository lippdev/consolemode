//! Official app icons read at runtime, never bundled: an exe's icon through PrivateExtractIcons,
//! the Xbox app's logo from its package folder. Plus the Win32 bits the overlay needs.

use gpui::RenderImage;
use image::{Frame, RgbaImage};
use windows::{
    Win32::{
        Foundation::HWND,
        Graphics::Gdi::{BITMAP, BITMAPINFO, BITMAPINFOHEADER, BI_RGB, DIB_RGB_COLORS, DeleteObject, GetDC, GetDIBits, GetObjectW, ReleaseDC},
        UI::WindowsAndMessaging::{DestroyIcon, GetIconInfo, HICON, ICONINFO, PrivateExtractIconsW, SetForegroundWindow, BringWindowToTop, SetWindowPos, HWND_TOPMOST, SWP_NOMOVE, SWP_NOSIZE, SWP_SHOWWINDOW, GetForegroundWindow, GetWindowThreadProcessId},
    },
};

use crate::log;

/// BGRA pixels (what gpui wants) of the exe's first icon at `size` px.
pub fn exe_icon(path: &str, size: i32) -> Option<RenderImage> {
    let mut name = [0u16; 260];
    for (i, c) in path.encode_utf16().take(259).enumerate() { name[i] = c; }
    unsafe {
        let mut icons = [HICON::default()];
        let n = PrivateExtractIconsW(&name, 0, size, size, Some(&mut icons), None, 0);
        if n == 0 || icons[0].is_invalid() { log(format!("no icon in {path}")); return None; }
        let icon = icons[0];
        let mut info = ICONINFO::default();
        GetIconInfo(icon, &mut info).ok()?;
        let mut bm = BITMAP::default();
        GetObjectW(info.hbmColor.into(), std::mem::size_of::<BITMAP>() as i32, Some(&mut bm as *mut _ as *mut _));
        let (w, h) = (bm.bmWidth, bm.bmHeight);
        let mut bi = BITMAPINFO { bmiHeader: BITMAPINFOHEADER { biSize: std::mem::size_of::<BITMAPINFOHEADER>() as u32, biWidth: w, biHeight: -h, biPlanes: 1, biBitCount: 32, biCompression: BI_RGB.0, ..Default::default() }, ..Default::default() };
        let mut px = vec![0u8; (w * h * 4) as usize];
        let dc = GetDC(None);
        let lines = GetDIBits(dc, info.hbmColor, 0, h as u32, Some(px.as_mut_ptr() as *mut _), &mut bi, DIB_RGB_COLORS);
        ReleaseDC(None, dc);
        let _ = DeleteObject(info.hbmColor.into());
        let _ = DeleteObject(info.hbmMask.into());
        let _ = DestroyIcon(icon);
        if lines == 0 { return None; }
        log(format!("icon {w}x{h} from {path}"));
        // DIB is already B,G,R,A: hand it to gpui as is.
        Some(RenderImage::new(vec![Frame::new(RgbaImage::from_raw(w as u32, h as u32, px)?)]))
    }
}

/// The Xbox app's own logo (its package ships it at 256 px).
pub fn xbox_logo() -> Option<RenderImage> {
    let out = std::process::Command::new("powershell").args(["-NoProfile", "-Command", "(Get-AppxPackage Microsoft.GamingApp).InstallLocation"]).output().ok()?;
    let dir = String::from_utf8_lossy(&out.stdout).trim().to_string();
    if dir.is_empty() { log("Xbox app not installed"); return None; }
    let path = std::path::Path::new(&dir).join(r"assets\Xbox_AppList.targetsize-256_altform-unplated.png");
    let mut rgba = image::open(&path).ok()?.to_rgba8();
    for p in rgba.chunks_exact_mut(4) { p.swap(0, 2); }
    log(format!("xbox logo {}x{}", rgba.width(), rgba.height()));
    Some(RenderImage::new(vec![Frame::new(rgba)]))
}

pub fn hwnd(window: &gpui::Window) -> Option<HWND> {
    use raw_window_handle::{HasWindowHandle, RawWindowHandle};
    match HasWindowHandle::window_handle(window).ok()?.as_raw() { RawWindowHandle::Win32(h) => Some(HWND(h.hwnd.get() as *mut _)), _ => None }
}

/// Topmost + foreground, the way the session menu has to grab the screen over a game.
pub fn force_foreground(window: &gpui::Window) {
    let Some(h) = hwnd(window) else { log("no hwnd"); return; };
    unsafe {
        let before = GetForegroundWindow();
        let _ = SetWindowPos(h, Some(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        let _ = BringWindowToTop(h);
        // like 1.7 (NativeWindows.cs): borrow the foreground thread's input queue so Windows lets us take the foreground
        let their = GetWindowThreadProcessId(before, None);
        let ours = windows::Win32::System::Threading::GetCurrentThreadId();
        let attached = their != 0 && their != ours && windows::Win32::System::Threading::AttachThreadInput(ours, their, true).as_bool();
        let _ = BringWindowToTop(h);
        let ok = SetForegroundWindow(h).as_bool();
        if attached { let _ = windows::Win32::System::Threading::AttachThreadInput(ours, their, false); }
        log(format!("overlay SetForegroundWindow={ok}, foreground before={:?} now={:?} ours={:?}", before.0, GetForegroundWindow().0, h.0));
    }
}
