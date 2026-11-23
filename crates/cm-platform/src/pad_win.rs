//! What the controller runtime needs from Windows: the pointer and the keys it sends (SendInput),
//! which window has the foreground, and the height of the screen the pointer is on.
//! Ported from 1.7's ControllerMouse and TaskViewDriver (their Win32 parts).

use windows::Win32::{
    Foundation::{HWND, POINT},
    Graphics::Gdi::{GetMonitorInfoW, MONITOR_DEFAULTTONEAREST, MONITORINFO, MonitorFromPoint},
    System::Threading::GetCurrentProcessId,
    UI::{
        Input::KeyboardAndMouse::{
            INPUT, INPUT_0, INPUT_KEYBOARD, INPUT_MOUSE, KEYBDINPUT, KEYBD_EVENT_FLAGS, KEYEVENTF_EXTENDEDKEY, KEYEVENTF_KEYUP, MOUSE_EVENT_FLAGS, MOUSEEVENTF_HWHEEL,
            MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, MOUSEEVENTF_MOVE, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, MOUSEEVENTF_WHEEL, MOUSEINPUT, SendInput, VIRTUAL_KEY, VK_DOWN,
            VK_ESCAPE, VK_LEFT, VK_RETURN, VK_RIGHT, VK_UP,
        },
        WindowsAndMessaging::{GetCursorPos, GetForegroundWindow, GetWindowThreadProcessId, IsWindowVisible},
    },
};

/// One wheel notch.
const WHEEL_DELTA: i32 = 120;

/// A mouse button the pad stands for.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Button { Left, Right }

fn send_mouse(flags: MOUSE_EVENT_FLAGS, dx: i32, dy: i32, data: i32) {
    let input = INPUT { r#type: INPUT_MOUSE, Anonymous: INPUT_0 { mi: MOUSEINPUT { dx, dy, mouseData: data as u32, dwFlags: flags, time: 0, dwExtraInfo: 0 } } };
    // Fails silently against a window running as administrator (UIPI): nothing to do from here.
    unsafe { SendInput(&[input], std::mem::size_of::<INPUT>() as i32) };
}

/// Moves the pointer by whole pixels, relative to where it is.
pub fn mouse_move(dx: i32, dy: i32) { send_mouse(MOUSEEVENTF_MOVE, dx, dy, 0); }

pub fn mouse_button(button: Button, down: bool) {
    let flags = match (button, down) {
        (Button::Left, true) => MOUSEEVENTF_LEFTDOWN,
        (Button::Left, false) => MOUSEEVENTF_LEFTUP,
        (Button::Right, true) => MOUSEEVENTF_RIGHTDOWN,
        (Button::Right, false) => MOUSEEVENTF_RIGHTUP,
    };
    send_mouse(flags, 0, 0, 0);
}

/// Scrolls by whole notches: positive is up (vertical) or right (horizontal).
pub fn mouse_wheel(vertical: i32, horizontal: i32) {
    if vertical != 0 { send_mouse(MOUSEEVENTF_WHEEL, 0, 0, vertical * WHEEL_DELTA); }
    if horizontal != 0 { send_mouse(MOUSEEVENTF_HWHEEL, 0, 0, horizontal * WHEEL_DELTA); }
}

/// A key Task View understands (what [`send_task_view_key`] sends).
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Key { Left, Right, Up, Down, Enter, Escape }

/// One key press. The arrows are extended keys (the ones outside the numeric keypad).
pub fn send_task_view_key(key: Key) {
    let (vk, extended): (VIRTUAL_KEY, bool) = match key {
        Key::Left => (VK_LEFT, true),
        Key::Right => (VK_RIGHT, true),
        Key::Up => (VK_UP, true),
        Key::Down => (VK_DOWN, true),
        Key::Enter => (VK_RETURN, false),
        Key::Escape => (VK_ESCAPE, false),
    };
    let flags = if extended { KEYEVENTF_EXTENDEDKEY } else { KEYBD_EVENT_FLAGS(0) };
    let event = |up: bool| INPUT {
        r#type: INPUT_KEYBOARD,
        Anonymous: INPUT_0 { ki: KEYBDINPUT { wVk: vk, wScan: 0, dwFlags: flags | if up { KEYEVENTF_KEYUP } else { KEYBD_EVENT_FLAGS(0) }, time: 0, dwExtraInfo: 0 } },
    };
    unsafe { SendInput(&[event(false), event(true)], std::mem::size_of::<INPUT>() as i32) };
}

/// The window that has the keyboard focus now (0 when none).
pub fn foreground_window() -> isize { unsafe { GetForegroundWindow() }.0 as isize }

/// True when the window in front belongs to this app.
pub fn app_is_foreground() -> bool {
    unsafe {
        let fg = GetForegroundWindow();
        if fg.is_invalid() { return false; }
        let mut pid = 0;
        GetWindowThreadProcessId(fg, Some(&mut pid));
        pid == GetCurrentProcessId()
    }
}

/// False while the window is hidden (in the tray).
pub fn window_visible(window: &gpui::Window) -> bool {
    super::windows_os::hwnd(window).is_some_and(|h: HWND| unsafe { IsWindowVisible(h) }.as_bool())
}

/// Height in pixels of the screen the pointer is on, so the pointer speed is the same on any resolution.
pub fn pointer_screen_height() -> i32 {
    unsafe {
        let mut p = POINT::default();
        if GetCursorPos(&mut p).is_err() { return 1080; }
        let m = MonitorFromPoint(p, MONITOR_DEFAULTTONEAREST);
        let mut info = MONITORINFO { cbSize: std::mem::size_of::<MONITORINFO>() as u32, ..Default::default() };
        if GetMonitorInfoW(m, &mut info).as_bool() { (info.rcMonitor.bottom - info.rcMonitor.top).max(1) } else { 1080 }
    }
}
