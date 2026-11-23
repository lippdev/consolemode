//! Per-game profiles while a session runs (ported from 1.7's GameProfiles, issue #69): Steam's
//! running game is read on every tick; when a game with a profile starts, its settings are applied,
//! and when it closes the screen goes back to how it was when it started.

use crate::config::GameProfile;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum GameChange { None, Started, Ended, Switched }

/// What happened between two reads of Steam's running game (0 = none). An unknown read (None)
/// changes nothing.
pub fn change(previous: u32, current: Option<u32>) -> GameChange {
    let Some(now) = current else { return GameChange::None };
    if now == previous { return GameChange::None; }
    if previous == 0 { GameChange::Started } else if now == 0 { GameChange::Ended } else { GameChange::Switched }
}

/// Whether a profile would change anything at all.
pub fn is_empty(p: &GameProfile) -> bool {
    p.mode.is_none() && p.hdr.is_none() && p.fps_limit.is_none() && p.audio_device_id.as_deref().is_none_or(|a| a.trim().is_empty())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::config::DisplayMode;

    #[test]
    fn a_game_starting_ending_or_switching_is_noticed() {
        assert_eq!(change(0, Some(814380)), GameChange::Started);
        assert_eq!(change(814380, Some(0)), GameChange::Ended);
        assert_eq!(change(814380, Some(374320)), GameChange::Switched);
        assert_eq!(change(814380, Some(814380)), GameChange::None);
        assert_eq!(change(0, Some(0)), GameChange::None);
    }

    #[test]
    fn an_unknown_read_changes_nothing() {
        assert_eq!(change(0, None), GameChange::None);
        assert_eq!(change(814380, None), GameChange::None);
    }

    #[test]
    fn a_profile_with_nothing_set_is_empty() {
        let mut p = GameProfile { steam_app_id: 1, ..Default::default() };
        assert!(is_empty(&p));
        p.audio_device_id = Some("  ".into());
        assert!(is_empty(&p), "a blank audio id changes nothing");
        p.fps_limit = Some(0);
        assert!(!is_empty(&p), "no limit is a setting too");
        p.fps_limit = None;
        p.mode = Some(DisplayMode { width: 1920, height: 1080, frequency: 60 });
        assert!(!is_empty(&p));
    }
}
