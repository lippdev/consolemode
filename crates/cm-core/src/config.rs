//! The config file, in the same JSON shape as Console Mode 1.7 (`config.json`, camelCase).
//!
//! 2.0 keeps its own file in its own folder (`%LOCALAPPDATA%\ConsoleMode2`), so 1.7 and 2.0
//! never share or overwrite each other. On its first start 2.0 copies the 1.7 file once,
//! reading it only. Settings 2.0 doesn't know yet are kept as they came and written back,
//! so nothing a 1.7 user set is lost on the way.

use std::{
    collections::BTreeMap,
    fs, io,
    path::{Path, PathBuf},
};

use serde::{Deserialize, Serialize};
use serde_json::{Map, Value};

/// The 1.7 version number for "monitor fields hold stable ids" (ConfigService.CurrentVersion).
pub const VERSION: u32 = 2;

#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct DisplayMode {
    pub width: u32,
    pub height: u32,
    pub frequency: u32,
}

impl DisplayMode {
    /// Whether a screen in `current` is in this mode (1.7 `DisplayModeMatch`, #212/#215). TVs and their
    /// drivers run the "60 Hz" of video at 59.94 Hz and list both: asking for @60 can leave the TV at
    /// @59, which is that mode, not a failure. Only that way round: a screen at @60 when @59 was picked
    /// is in a different mode from the list, so that one is still applied. 0 = any size or rate.
    pub fn is_met_by(&self, current: &DisplayMode) -> bool {
        (self.width == 0 || current.width == self.width)
            && (self.height == 0 || current.height == self.height)
            && (self.frequency == 0 || current.frequency == self.frequency || current.frequency + 1 == self.frequency)
    }
}

/// One game's own settings (1.7 `GameProfile`). Empty fields keep the session's value.
#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GameProfile {
    pub steam_app_id: u32,
    #[serde(default)]
    pub name: String,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub mode: Option<DisplayMode>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub hdr: Option<bool>,
    /// 0 = no limit; None = the session's.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub fps_limit: Option<u32>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub audio_device_id: Option<String>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub audio_device_name: Option<String>,
    /// 2.0: go straight into the game when Steam opens (None = yes).
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub direct_launch: Option<bool>,
    // 1.7 fields 2.0 doesn't use yet are kept
    #[serde(flatten)]
    pub other: Map<String, Value>,
}

#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct Config {
    pub custom_apps: Vec<crate::custom_apps::CustomAppEntry>,
    pub custom_app_id: String,
    pub custom_app_path: String,
    pub custom_app_arguments: String,
    pub custom_app_watch_process: String,
    pub version: u32,
    /// Empty: follow Windows.
    pub app_language: String,
    /// Stable id of the game screen (MONITOR\<hw>\<driver key>).
    pub focus_monitor: String,
    pub hide_monitors: Vec<String>,
    pub hide_strategy: String,
    /// The launcher: "bigPicture" | "playnite" | "xboxMode" | "customApp" ...
    pub fullscreen_mode: String,
    pub playnite_path: String,
    pub audio_device_id: String,
    pub audio_device_name: String,
    pub audio_auto_switch: bool,
    /// 0 = no limit.
    pub fps_limit: u32,
    pub fps_overlay: String,
    /// Mode per screen, keyed by stable id.
    pub monitor_modes: BTreeMap<String, DisplayMode>,
    pub hdr_enable: bool,
    pub vrr_enable: bool,
    /// "auto" | "xbox" | "playstation".
    pub controller_layout: String,
    pub confirmed_setup: String,
    pub check_updates: bool,
    pub close_steam_on_restore: bool,
    pub restore_desktop_icons: bool,
    /// Hide the desktop icons while the session runs and show them again after it (1.7.1 #210). Off by default.
    pub hide_desktop_icons: bool,
    pub park_pointer: bool,
    pub interface_sounds: bool,
    pub minimize_to_tray: bool,
    pub game_profiles: Vec<GameProfile>,
    /// Turning the TV on / to the PC's input when a session starts (1.7.1 `tv`, see `tv`).
    pub tv: crate::tv::TvControl,

    // ── controller shortcuts, as 1.7 keeps them: XInput button bitmasks, 0 = not set (see `shortcuts`) ──
    pub home_shortcut: i32,
    pub menu_shortcut: i32,
    pub exit_shortcut: i32,
    /// Select + X from the start, also for a config from before it existed (1.7).
    pub task_view_shortcut: i32,
    /// A PlayStation pad drives Task View once the shortcut opened it (Windows does it for Xbox ones).
    #[serde(rename = "taskViewPlayStation")]
    pub task_view_playstation: bool,
    /// A controller connecting while the app is in the tray brings it up.
    pub auto_start_on_controller: bool,

    // ── 2.0 only (1.7 keeps them in its own unknown-settings bag if it ever reads this file) ──
    /// Alpha shell preferences never inherit 1.7 startup integration.
    pub start_with_windows: bool,
    pub start_in_tray: bool,
    pub protocol_registered: bool,
    /// Request Windows' gamepad experience when Console Mode opens normally.
    pub gaming_home_on_open: bool,
    /// "lua" | "ambar" | "jade".
    pub theme: String,
    /// Recent Steam games in the Home row. Off by default.
    pub show_steam_games: bool,
    /// Steam app ids, in the order the user arranged them; pinned ones; hidden ones.
    pub game_order: Vec<u32>,
    pub pinned_games: Vec<u32>,
    pub hidden_games: Vec<u32>,
    /// The controller moves the pointer when the app is not in front (left stick, A, B, right stick).
    pub controller_mouse: bool,
    /// "slow" | "medium" | "fast".
    pub controller_mouse_speed: String,
    /// Where this file came from the first time ("1.7" when imported), for the logs.
    pub imported_from: String,
    /// 2.0's first-run tour was finished or skipped.
    pub tour_done: bool,
    /// The session menu's tour (1.7 SessionMenuTourDone): offered the first time the menu opens.
    pub session_menu_tour_done: bool,
    /// The version that ran last, to tell the first start after an update ("what's new", see `release_notes`).
    pub last_run_version: String,
    /// The opening with the logo when the app opens, its sound, and its style (1.7 IntroAnimation, IntroSound,
    /// IntroStyle). No style picked: the app's own, with the others now and then as a surprise (see `boot_intro`).
    pub intro_animation: bool,
    pub intro_sound: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub intro_style: Option<String>,
    /// UI sounds: "console" (the samples) or "soft" (synthesized), and their volume 0–100.
    pub sound_style: String,
    pub sound_volume: u8,

    /// Every other setting, as it came (1.7's shortcuts, TV control, intro...).
    #[serde(flatten)]
    pub other: Map<String, Value>,
}

impl Default for Config {
    /// The 1.7 defaults for the fields both share (Models.cs AppConfig).
    fn default() -> Self {
        Self {
            version: VERSION,
            custom_apps: Vec::new(), custom_app_id: String::new(), custom_app_path: String::new(),
            custom_app_arguments: String::new(), custom_app_watch_process: String::new(),
            app_language: String::new(),
            focus_monitor: String::new(),
            hide_monitors: Vec::new(),
            hide_strategy: "disconnect".into(),
            fullscreen_mode: "bigPicture".into(),
            playnite_path: String::new(),
            audio_device_id: String::new(),
            audio_device_name: String::new(),
            audio_auto_switch: false,
            fps_limit: 0,
            fps_overlay: "external".into(),
            monitor_modes: BTreeMap::new(),
            hdr_enable: false,
            vrr_enable: false,
            controller_layout: "auto".into(),
            confirmed_setup: String::new(),
            check_updates: true,
            close_steam_on_restore: true,
            restore_desktop_icons: true,
            hide_desktop_icons: false,
            park_pointer: true,
            interface_sounds: true,
            minimize_to_tray: false,
            game_profiles: Vec::new(),
            tv: crate::tv::TvControl::default(),
            home_shortcut: 0,
            menu_shortcut: 0,
            exit_shortcut: 0,
            task_view_shortcut: 0x0020 | 0x4000,
            task_view_playstation: true,
            auto_start_on_controller: false,
            start_with_windows: false,
            start_in_tray: false,
            protocol_registered: false,
            gaming_home_on_open: false,
            theme: "lua".into(),
            show_steam_games: false,
            game_order: Vec::new(),
            pinned_games: Vec::new(),
            hidden_games: Vec::new(),
            controller_mouse: false,
            controller_mouse_speed: "medium".into(),
            imported_from: String::new(),
            tour_done: false,
            session_menu_tour_done: false,
            last_run_version: String::new(),
            intro_animation: true,
            intro_sound: true,
            intro_style: None,
            sound_style: "console".into(),
            sound_volume: 60,
            other: Map::new(),
        }
    }
}

impl Config {
    pub fn from_json(text: &str) -> Result<Self, serde_json::Error> {
        // A UTF-8 BOM (Windows editors) is not JSON
        serde_json::from_str(text.trim_start_matches('\u{feff}'))
    }

    pub fn to_json(&self) -> String {
        serde_json::to_string_pretty(self).expect("config always serializes")
    }

    pub fn profile(&self, app_id: u32) -> Option<&GameProfile> { self.game_profiles.iter().find(|p| p.steam_app_id == app_id) }

    pub fn profile_mut(&mut self, app_id: u32, name: &str) -> &mut GameProfile {
        if let Some(i) = self.game_profiles.iter().position(|p| p.steam_app_id == app_id) { return &mut self.game_profiles[i]; }
        self.game_profiles.push(GameProfile { steam_app_id: app_id, name: name.to_string(), ..Default::default() });
        self.game_profiles.last_mut().unwrap()
    }
}

/// Where 2.0 keeps its data, and where 1.7 keeps its config (installed builds).
#[derive(Clone, Debug)]
pub struct Paths {
    pub data_dir: PathBuf,
    pub config: PathBuf,
    pub legacy_config: Option<PathBuf>,
}

impl Paths {
    /// `local_app_data`: `%LOCALAPPDATA%`.
    pub fn new(local_app_data: &Path) -> Self {
        let data_dir = local_app_data.join("ConsoleMode2");
        Self { config: data_dir.join("config.json"), data_dir, legacy_config: Some(local_app_data.join("ConsoleMode").join("config.json")) }
    }
}

/// How the config was found this time (shown in the log, and in the UI when it matters).
#[derive(Debug, PartialEq, Eq)]
pub enum Loaded {
    /// 2.0's own file.
    Own,
    /// Copied from 1.7 (read only) and saved as 2.0's.
    Imported,
    /// Nothing to read: defaults.
    New,
    /// 2.0's file didn't parse. It was set aside as `config.bad-<n>.json` and the defaults used.
    /// The session must not start on guesses: see [`Config::is_session_ready`].
    Broken(String),
}

pub fn load(paths: &Paths) -> (Config, Loaded) {
    match fs::read_to_string(&paths.config) {
        Ok(text) => match Config::from_json(&text) {
            Ok(c) => (c, Loaded::Own),
            Err(e) => {
                let _ = set_aside(&paths.config);
                (Config::default(), Loaded::Broken(e.to_string()))
            }
        },
        Err(_) => {
            if let Some(legacy) = &paths.legacy_config {
                if let Ok(text) = fs::read_to_string(legacy) {
                    if let Ok(mut c) = Config::from_json(&text) {
                        c.imported_from = "1.7".into();
                        // 1.7 v0/v1 files hold \\.\DISPLAYn names; those still work as fallbacks
                        c.version = VERSION;
                        let _ = save(paths, &c);
                        return (c, Loaded::Imported);
                    }
                }
            }
            (Config::default(), Loaded::New)
        }
    }
}

/// Writes atomically: a temp file next to it, then a rename, so a crash never leaves half a file.
pub fn save(paths: &Paths, config: &Config) -> io::Result<()> {
    fs::create_dir_all(&paths.data_dir)?;
    let tmp = paths.config.with_extension("json.tmp");
    fs::write(&tmp, config.to_json())?;
    fs::rename(&tmp, &paths.config)
}

fn set_aside(path: &Path) -> io::Result<PathBuf> {
    let dir = path.parent().unwrap_or(Path::new("."));
    let n = (1..1000).find(|n| !dir.join(format!("config.bad-{n}.json")).exists()).unwrap_or(999);
    let to = dir.join(format!("config.bad-{n}.json"));
    fs::rename(path, &to)?;
    Ok(to)
}

impl Config {
    /// A session may only start from a config that names its game screen. Defaults guessing the
    /// screen once turned a TV on and switched off the only screen in use (1.7, 2026-10-29).
    pub fn is_session_ready(&self) -> bool { !self.focus_monitor.trim().is_empty() }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn mode(width: u32, height: u32, frequency: u32) -> DisplayMode { DisplayMode { width, height, frequency } }

    #[test]
    fn a_screen_at_the_video_rate_just_under_counts_as_the_mode_asked_for() {
        for (asked, got) in [(60, 59), (24, 23), (30, 29), (120, 119), (144, 143), (60, 60)] {
            assert!(mode(2560, 1440, asked).is_met_by(&mode(2560, 1440, got)), "{asked} vs {got}");
        }
    }

    #[test]
    fn a_mode_picked_at_59_is_still_applied_when_the_screen_is_at_60() {
        assert!(!mode(1920, 1080, 59).is_met_by(&mode(1920, 1080, 60)));
        assert!(!mode(1920, 1080, 60).is_met_by(&mode(1920, 1080, 58)));
        assert!(!mode(1920, 1080, 120).is_met_by(&mode(1920, 1080, 60)));
    }

    #[test]
    fn the_size_must_match_and_zero_means_any() {
        assert!(!mode(1920, 1080, 60).is_met_by(&mode(2560, 1440, 60)));
        assert!(!mode(1920, 1080, 60).is_met_by(&mode(1920, 1200, 60)));
        assert!(mode(1920, 1080, 0).is_met_by(&mode(1920, 1080, 144)));
        assert!(mode(0, 0, 0).is_met_by(&mode(3840, 2160, 30)));
    }

    /// Trimmed from a real 1.7 config.json.
    const V17: &str = r#"{
      "version": 2,
      "appLanguage": "",
      "focusMonitor": "MONITOR\\GSM0001\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0002",
      "hideMonitors": ["MONITOR\\TCL1003\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0011"],
      "hideStrategy": "disconnect",
      "fullscreenMode": "playnite",
      "customApps": [{"id": "a1", "path": "C:\\Games\\x.exe"}],
      "audioDeviceName": "Don't change",
      "monitorModes": {"MONITOR\\TCL1003\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0011": {"width": 1920, "height": 1080, "frequency": 300}},
      "hdrEnable": false,
      "homeShortcut": 4096,
      "tv": {"provider": "none", "host": ""},
      "gameProfiles": [{"steamAppId": 814380, "name": "Sekiro", "hdr": true, "fpsLimit": 60}]
    }"#;

    fn temp_dir(name: &str) -> PathBuf {
        let d = std::env::temp_dir().join(format!("cm2-config-test-{name}-{}", std::process::id()));
        let _ = fs::remove_dir_all(&d);
        fs::create_dir_all(&d).unwrap();
        d
    }

    #[test]
    fn reads_17_and_keeps_what_it_does_not_know() {
        let c = Config::from_json(V17).unwrap();
        assert_eq!(c.fullscreen_mode, "playnite");
        assert!(c.focus_monitor.starts_with(r"MONITOR\GSM0001"));
        assert_eq!(c.monitor_modes.values().next().unwrap().frequency, 300);
        assert_eq!(c.profile(814380).unwrap().hdr, Some(true));
        // Typed custom apps and unknown settings both survive.
        let back: Value = serde_json::from_str(&c.to_json()).unwrap();
        assert_eq!(back["homeShortcut"], 4096);
        assert_eq!(back["customApps"][0]["path"], r"C:\Games\x.exe");
        assert_eq!(back["tv"]["provider"], "none");
        // 2.0 defaults filled in
        assert_eq!(c.theme, "lua");
        assert!(!c.show_steam_games);
    }

    #[test]
    fn round_trip_is_stable() {
        let c = Config::from_json(V17).unwrap();
        let again = Config::from_json(&c.to_json()).unwrap();
        assert_eq!(c, again);
    }

    #[test]
    fn gaming_home_on_open_requires_opt_in_and_round_trips() {
        assert!(!Config::default().gaming_home_on_open);
        assert!(!Config::from_json(V17).unwrap().gaming_home_on_open);
        let enabled = Config::from_json(r#"{"gamingHomeOnOpen":true}"#).unwrap();
        assert!(enabled.gaming_home_on_open);
        assert!(Config::from_json(&enabled.to_json()).unwrap().gaming_home_on_open);
    }

    #[test]
    fn bom_is_fine() {
        assert!(Config::from_json("\u{feff}{}").is_ok());
    }

    #[test]
    fn imports_17_once_without_touching_it() {
        let root = temp_dir("import");
        fs::create_dir_all(root.join("ConsoleMode")).unwrap();
        let legacy = root.join("ConsoleMode").join("config.json");
        fs::write(&legacy, V17).unwrap();
        let paths = Paths::new(&root);

        let (c, how) = load(&paths);
        assert_eq!(how, Loaded::Imported);
        assert_eq!(c.imported_from, "1.7");
        assert!(paths.config.exists());
        assert_eq!(fs::read_to_string(&legacy).unwrap(), V17, "1.7's file must stay untouched");

        // second start: 2.0's own file, even if 1.7 changes later
        fs::write(&legacy, r#"{"fullscreenMode":"xboxMode"}"#).unwrap();
        let (c2, how2) = load(&paths);
        assert_eq!(how2, Loaded::Own);
        assert_eq!(c2.fullscreen_mode, "playnite");
        let _ = fs::remove_dir_all(&root);
    }

    #[test]
    fn broken_file_is_set_aside_and_blocks_sessions() {
        let root = temp_dir("broken");
        let paths = Paths::new(&root);
        fs::create_dir_all(&paths.data_dir).unwrap();
        // the 2026-10-29 incident: a bad escape in a monitor id
        fs::write(&paths.config, r#"{"focusMonitor": "MONITOR\SMKD1CE"}"#).unwrap();
        let (c, how) = load(&paths);
        assert!(matches!(how, Loaded::Broken(_)));
        assert!(!c.is_session_ready());
        assert!(paths.data_dir.join("config.bad-1.json").exists());
        assert!(!paths.config.exists());
        let _ = fs::remove_dir_all(&root);
    }

    #[test]
    fn save_then_load() {
        let root = temp_dir("save");
        let paths = Paths { legacy_config: None, ..Paths::new(&root) };
        let mut c = Config::default();
        c.theme = "jade".into();
        c.show_steam_games = true;
        c.profile_mut(374320, "DARK SOULS III").fps_limit = Some(60);
        save(&paths, &c).unwrap();
        let (back, how) = load(&paths);
        assert_eq!(how, Loaded::Own);
        assert_eq!(back, c);
        let _ = fs::remove_dir_all(&root);
    }

    #[test]
    fn nothing_anywhere_gives_defaults() {
        let root = temp_dir("new");
        let (c, how) = load(&Paths::new(&root));
        assert_eq!(how, Loaded::New);
        assert_eq!(c, Config::default());
        let _ = fs::remove_dir_all(&root);
    }
}
