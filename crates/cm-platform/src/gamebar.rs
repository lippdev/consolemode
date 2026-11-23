//! The Windows setting "Open Xbox Game Bar using this button on a controller" (ported from 1.7's
//! GameBarService). Turning it off frees a short press of the Guide button for us; Game Bar itself
//! stays enabled (Win + G). 1.7 only uses it from Settings (never around a session); Steam's own
//! "Guide button focuses Steam" lives in Steam's config files, which Steam rewrites while running,
//! so we only detect Steam and tell the user.

use winreg::{RegKey, enums::HKEY_CURRENT_USER};

const KEY: &str = r"Software\Microsoft\GameBar";
const VALUE: &str = "UseNexusForGameBarEnabled";

/// Windows treats a missing value as enabled.
pub fn guide_opens_game_bar() -> bool {
    RegKey::predef(HKEY_CURRENT_USER).open_subkey(KEY).ok().and_then(|k| k.get_value::<u32, _>(VALUE).ok()).is_none_or(|v| v != 0)
}

pub fn set_guide_opens_game_bar(enabled: bool) -> Result<(), String> {
    let (key, _) = RegKey::predef(HKEY_CURRENT_USER).create_subkey(KEY).map_err(|e| format!("Game Bar: {e}"))?;
    key.set_value(VALUE, &u32::from(enabled)).map_err(|e| format!("Game Bar: {e}"))
}

pub fn is_steam_running() -> bool { crate::launch::is_running("steam") }
