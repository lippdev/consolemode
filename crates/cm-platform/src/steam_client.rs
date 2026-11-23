//! Steam's client as the session sees it: is it running, which game it runs, and putting its window
//! in the tray (ported from 1.7's SteamSession). Never shuts the process down. The rules for when to
//! do it are in cm_core::steam_client.

use windows::Win32::{
    Foundation::{HWND, LPARAM, WPARAM},
    UI::WindowsAndMessaging::{GW_OWNER, GetWindow, IsWindowVisible, PostMessageW, WM_CLOSE},
};
use winreg::{RegKey, enums::HKEY_CURRENT_USER};

use crate::launch;

pub fn is_running() -> bool { launch::processes().iter().any(|(_, _, n)| cm_core::steam_client::is_steam_process(n.trim_end_matches(".exe").trim_end_matches(".EXE"))) }

/// The id of the game Steam is running, 0 if none, or None when the state is unknown (value missing
/// or unreadable): then Steam must be left alone.
pub fn running_app_id() -> Option<u32> {
    RegKey::predef(HKEY_CURRENT_USER).open_subkey(r"Software\Valve\Steam").ok()?.get_value::<u32, _>("RunningAppID").ok()
}

/// Closes the Steam client window so Steam can move itself to the tray. True when the close was posted.
pub fn close_window_to_tray() -> bool {
    let names: Vec<(u32, String)> = launch::processes().into_iter().map(|(p, _, n)| (p, n.trim_end_matches(".exe").to_string())).collect();
    for w in launch::windows() {
        let Some((_, process)) = names.iter().find(|(p, _)| *p == w.pid) else { continue };
        let h = HWND(w.hwnd as *mut _);
        let has_owner = unsafe { GetWindow(h, GW_OWNER) }.is_ok_and(|o| !o.is_invalid());
        if cm_core::steam_client::is_client_window(process, &w.title, &w.class, unsafe { IsWindowVisible(h) }.as_bool(), has_owner) {
            return unsafe { PostMessageW(Some(h), WM_CLOSE, WPARAM(0), LPARAM(0)) }.is_ok();
        }
    }
    false
}
