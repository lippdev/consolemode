//! Reads and writes Windows' variable-refresh-rate setting (the registry value; the text rules are in
//! `cm_core::video_features`). Called by the session only, never by the interface.

use cm_core::video_features::{KEY, VALUE};
use winreg::{RegKey, enums::HKEY_CURRENT_USER};

/// The current text of the setting; None when the value is not there at all.
pub fn vrr_setting() -> Option<String> {
    RegKey::predef(HKEY_CURRENT_USER).open_subkey(KEY).ok()?.get_value::<String, _>(VALUE).ok()
}

/// Writes the setting text, or removes the value (None) as it was when there was none.
pub fn set_vrr_setting(text: Option<&str>) -> Result<(), String> {
    let (key, _) = RegKey::predef(HKEY_CURRENT_USER).create_subkey(KEY).map_err(|e| format!("VRR: {e}"))?;
    match text {
        Some(t) => key.set_value(VALUE, &t).map_err(|e| format!("VRR: {e}")),
        None => match key.delete_value(VALUE) {
            Err(e) if e.kind() != std::io::ErrorKind::NotFound => Err(format!("VRR: {e}")),
            _ => Ok(()),
        },
    }
}
