//! Platform services for Console Mode 2. Windows only for now (Linux comes later as its own module).

pub mod live_thumbs;
pub mod sharp;
pub mod appinfo;
pub mod steam;
#[cfg(windows)]
pub mod launch;
#[cfg(windows)]
pub mod session_menu_win;
#[cfg(windows)]
pub mod windows_os;

#[cfg(windows)]
pub use windows_os::{animations_enabled, describe_windows, exe_icon, force_foreground, local_time, move_window_to, primary_rect, restore_window, set_topmost, system_ui_language_id, xbox_logo, xbox_logo_mono};

#[cfg(windows)]
pub mod displays;
#[cfg(windows)]
pub mod audio;

#[cfg(windows)]
pub mod desk_win;
#[cfg(windows)]
pub use desk_win::WinDesk;

#[cfg(windows)]
pub mod shell_win;
#[cfg(windows)]
pub mod custom_apps;
#[cfg(windows)]
pub mod http;
#[cfg(windows)]
pub mod update;
#[cfg(windows)]
pub mod desktop_icons;
#[cfg(windows)]
pub mod gamebar;
#[cfg(windows)]
pub mod fse_win;
#[cfg(windows)]
pub mod pad_win;
#[cfg(windows)]
pub mod hid_win;
#[cfg(windows)]
pub mod helper_apps;
#[cfg(windows)]
pub mod pointer;
#[cfg(windows)]
pub mod rtss;
#[cfg(windows)]
pub mod steam_client;
#[cfg(windows)]
pub mod hint_win;
#[cfg(windows)]
pub mod video_win;
#[cfg(windows)]
pub mod wake_win;
#[cfg(windows)]
pub mod secret;
#[cfg(windows)]
pub mod tv;
