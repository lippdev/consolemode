//! When Steam's own window is put in the tray (ported from 1.7's SteamShutdown). Only when Big
//! Picture was the launcher and the setting is on, and never while a game runs.

pub fn is_steam_process(name: &str) -> bool { name.eq_ignore_ascii_case("steam") || name.eq_ignore_ascii_case("steamwebhelper") }

/// Only Steam's own processes may be asked to close: a game is an SDL_app in full screen just like
/// Big Picture, and it must never get the WM_CLOSE meant for Steam.
pub fn is_client_window(process: &str, title: &str, class: &str, visible: bool, has_owner: bool) -> bool {
    visible && !has_owner && is_steam_process(process) && title.trim().eq_ignore_ascii_case("Steam")
        && matches!(class, "Chrome_WidgetWin_1" | "vguiPopupWindow" | "SDL_app")
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Decision {
    /// Close the Steam client window to the tray.
    CloseToTray,
    /// The launcher is not Big Picture: Steam was never ours to close.
    NotBigPicture,
    SettingOff,
    /// Steam is not running: nothing to close (and asking would start it).
    NotRunning,
    /// A game launched through Steam is running (RunningAppID is not 0).
    GameRunning,
    /// The game state could not be read, so Steam is left open.
    GameStateUnknown,
}

pub fn decide(launcher_mode: &str, close_setting: bool, steam_running: bool, running_app_id: Option<u32>) -> Decision {
    if launcher_mode != "bigPicture" { return Decision::NotBigPicture; }
    if !close_setting { return Decision::SettingOff; }
    if !steam_running { return Decision::NotRunning; }
    match running_app_id { None => Decision::GameStateUnknown, Some(id) if id > 0 => Decision::GameRunning, Some(_) => Decision::CloseToTray }
}

/// The line for the log.
pub fn describe(d: Decision, running_app_id: Option<u32>) -> String {
    match d {
        Decision::CloseToTray => "Steam: fechando a janela para a bandeja".into(),
        Decision::SettingOff => "Steam: mantida aberta (desligado nos ajustes)".into(),
        Decision::NotRunning => "Steam: não estava aberta".into(),
        Decision::GameRunning => format!("Steam: mantida aberta (jogo em execução, id {})", running_app_id.unwrap_or(0)),
        Decision::GameStateUnknown => "Steam: mantida aberta (não foi possível verificar se há jogo em execução)".into(),
        Decision::NotBigPicture => "Steam: não é o lançador desta sessão".into(),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn only_steams_own_processes_count() {
        assert!(is_steam_process("steam") && is_steam_process("SteamWebHelper"));
        assert!(!is_steam_process("eldenring") && !is_steam_process("steamservice"));
    }

    #[test]
    fn the_client_window_is_the_visible_unowned_steam_titled_one() {
        assert!(is_client_window("steamwebhelper", "Steam", "Chrome_WidgetWin_1", true, false));
        assert!(is_client_window("steam", " steam ", "SDL_app", true, false));
        assert!(!is_client_window("steamwebhelper", "Steam", "Chrome_WidgetWin_1", false, false), "hidden");
        assert!(!is_client_window("steamwebhelper", "Steam", "Chrome_WidgetWin_1", true, true), "has an owner (a popup)");
        assert!(!is_client_window("steamwebhelper", "Big Picture", "SDL_app", true, false), "Big Picture itself");
        assert!(!is_client_window("eldenring", "Steam", "SDL_app", true, false), "a game must never get the close");
        assert!(!is_client_window("steam", "Steam", "Notepad", true, false));
    }

    #[test]
    fn decisions() {
        assert_eq!(decide("playnite", true, true, Some(0)), Decision::NotBigPicture);
        assert_eq!(decide("bigPicture", false, true, Some(0)), Decision::SettingOff);
        assert_eq!(decide("bigPicture", true, false, Some(0)), Decision::NotRunning);
        assert_eq!(decide("bigPicture", true, true, None), Decision::GameStateUnknown);
        assert_eq!(decide("bigPicture", true, true, Some(814380)), Decision::GameRunning);
        assert_eq!(decide("bigPicture", true, true, Some(0)), Decision::CloseToTray);
    }

    #[test]
    fn every_decision_has_a_log_line() {
        assert!(describe(Decision::GameRunning, Some(7)).contains("id 7"));
        assert!(describe(Decision::CloseToTray, Some(0)).contains("bandeja"));
    }
}
