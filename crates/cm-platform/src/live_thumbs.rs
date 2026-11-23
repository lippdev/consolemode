//! Live pictures of other windows for the session menu's "Janelas", the way Win + Tab and Alt + Tab
//! show them: the desktop compositor (DWM) draws each window, live, into a rectangle of ours. Nothing
//! is copied into the app (no bitmaps, no memory, no CPU), and DirectX games are shown too.
//! DWM draws them on top of our own content, so the app places them exactly over its tiles.

use std::collections::HashMap;

use windows::Win32::{
    Foundation::{HWND, RECT},
    Graphics::Dwm::{
        DWM_THUMBNAIL_PROPERTIES, DWM_TNP_OPACITY, DWM_TNP_RECTDESTINATION, DWM_TNP_SOURCECLIENTAREAONLY, DWM_TNP_VISIBLE, DwmQueryThumbnailSourceSize,
        DwmRegisterThumbnail, DwmUnregisterThumbnail, DwmUpdateThumbnailProperties,
    },
};

/// The live pictures on screen, by source window.
#[derive(Default)]
pub struct LiveThumbs {
    shown: HashMap<isize, isize>,
    /// Only while "Janelas" is in focus: a tile painted after that (the menu fading out) adds none.
    pub active: bool,
}

impl LiveThumbs {
    /// Puts `src`'s live picture inside `area` (window pixels, logical: x, y, w, h), keeping its
    /// shape, centred. False when Windows can't show that window (the tile shows its name instead).
    pub fn place(&mut self, window: &gpui::Window, src: isize, area: (f32, f32, f32, f32)) -> bool {
        if !self.active { return false; }
        let Some(dest) = crate::windows_os::hwnd(window) else { return false };
        let thumb = match self.shown.get(&src) {
            Some(t) => *t,
            None => match unsafe { DwmRegisterThumbnail(dest, HWND(src as *mut _)) } {
                Ok(t) => { self.shown.insert(src, t); t }
                Err(_) => return false,
            },
        };
        let k = window.scale_factor();
        let (x, y, w, h) = (area.0 * k, area.1 * k, area.2 * k, area.3 * k);
        // fit the source's shape inside the tile
        let size = unsafe { DwmQueryThumbnailSourceSize(thumb) }.unwrap_or_default();
        let (sw, sh) = (size.cx.max(1) as f32, size.cy.max(1) as f32);
        let s = (w / sw).min(h / sh);
        let (fw, fh) = (sw * s, sh * s);
        let (l, t) = (x + (w - fw) / 2., y + (h - fh) / 2.);
        let props = DWM_THUMBNAIL_PROPERTIES {
            dwFlags: DWM_TNP_RECTDESTINATION | DWM_TNP_VISIBLE | DWM_TNP_SOURCECLIENTAREAONLY | DWM_TNP_OPACITY,
            rcDestination: RECT { left: l.round() as i32, top: t.round() as i32, right: (l + fw).round() as i32, bottom: (t + fh).round() as i32 },
            opacity: 255,
            fVisible: true.into(),
            fSourceClientAreaOnly: true.into(),
            ..Default::default()
        };
        unsafe { DwmUpdateThumbnailProperties(thumb, &props) }.is_ok()
    }

    /// Takes every picture away (the menu closed, or another button got focus).
    pub fn clear(&mut self) {
        self.active = false;
        for (_, t) in self.shown.drain() { let _ = unsafe { DwmUnregisterThumbnail(t) }; }
    }

    /// Takes away the pictures of windows no longer listed.
    pub fn retain(&mut self, keep: &[isize]) {
        self.shown.retain(|src, t| { let k = keep.contains(src); if !k { let _ = unsafe { DwmUnregisterThumbnail(*t) }; } k });
    }
}

impl Drop for LiveThumbs {
    fn drop(&mut self) { self.clear(); }
}
