//! Where the mouse pointer is, moving it, and whether a point is on a screen that is on
//! (ported from 1.7's MousePointer). The rules for when to park it are in cm_core::pointer.

use windows::Win32::{
    Foundation::POINT,
    Graphics::Gdi::{MONITOR_DEFAULTTONULL, MonitorFromPoint},
    UI::WindowsAndMessaging::{GetCursorPos, SetCursorPos},
};

pub fn position() -> Option<(i32, i32)> {
    let mut p = POINT::default();
    unsafe { GetCursorPos(&mut p).ok()?; }
    Some((p.x, p.y))
}

pub fn move_to(x: i32, y: i32) -> bool { unsafe { SetCursorPos(x, y).is_ok() } }

/// True when some screen that is on contains the point.
pub fn is_on_screen(x: i32, y: i32) -> bool { !unsafe { MonitorFromPoint(POINT { x, y }, MONITOR_DEFAULTTONULL) }.is_invalid() }
