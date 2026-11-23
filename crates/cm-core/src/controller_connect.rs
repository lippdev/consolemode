//! When a controller connecting should bring Console Mode up (ported from 1.7's
//! ControllerConnectPolicy and the button bits of ControllerMapping). Pure, so it is tested.
//! Times are on any monotonic clock (time since the app started).

use std::time::Duration;

/// Pads already connected report themselves right after startup.
pub const STARTUP_GRACE: Duration = Duration::from_secs(15);

/// Wireless pads reconnect on their own after a session ends.
pub const RESTORE_GRACE: Duration = Duration::from_secs(30);

/// `now`: the clock now; `started_at`: when the app started; `quiet_until`: the end of the quiet
/// time after a session (ZERO = none); `can_trigger`: the option is on, the app sits in the tray
/// and no session runs.
pub fn should_trigger(now: Duration, started_at: Duration, quiet_until: Duration, can_trigger: bool) -> bool {
    can_trigger && now.saturating_sub(started_at) >= STARTUP_GRACE && now >= quiet_until
}

/// A button of the pad as SDL names it: the same on Xbox and PlayStation.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum PadButton {
    South, East, West, North, Back, Start, Guide, LeftShoulder, RightShoulder, DpadUp, DpadDown, DpadLeft, DpadRight,
}

impl PadButton {
    /// The XInput bit every shortcut works with (Cross = A, Circle = B, Square = X, Triangle = Y,
    /// Create/Share = Back, Options = Start, PS = Guide; the same table 1.7 applies to a Sony report).
    pub fn bit(self) -> u16 {
        use crate::shortcuts::*;
        match self {
            PadButton::South => A,
            PadButton::East => B,
            PadButton::West => X,
            PadButton::North => Y,
            PadButton::Back => BACK,
            PadButton::Start => START,
            PadButton::Guide => GUIDE,
            PadButton::LeftShoulder => LB,
            PadButton::RightShoulder => RB,
            PadButton::DpadUp => DPAD_UP,
            PadButton::DpadDown => DPAD_DOWN,
            PadButton::DpadLeft => DPAD_LEFT,
            PadButton::DpadRight => DPAD_RIGHT,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::shortcuts::{ALLOWED, is_acceptable, BACK, Y};

    const T0: Duration = Duration::ZERO;
    fn s(n: u64) -> Duration { Duration::from_secs(n) }

    #[test]
    fn triggers_only_after_the_startup_grace() {
        assert!(!should_trigger(s(5), T0, T0, true));
        assert!(!should_trigger(s(14), T0, T0, true));
        assert!(should_trigger(s(15), T0, T0, true));
    }

    #[test]
    fn triggers_when_grace_passed_and_allowed() { assert!(should_trigger(s(60), T0, T0, true)); }

    #[test]
    fn stays_quiet_after_a_restore() {
        assert!(!should_trigger(s(60), T0, s(120), true));
        assert!(should_trigger(s(120), T0, s(120), true));
    }

    #[test]
    fn never_triggers_when_not_allowed() { assert!(!should_trigger(s(60), T0, T0, false)); }

    #[test]
    fn the_face_buttons_and_menu_buttons_map_to_the_xinput_bits() {
        assert_eq!(PadButton::South.bit(), 0x1000);
        assert_eq!(PadButton::East.bit(), 0x2000);
        assert_eq!(PadButton::West.bit(), 0x4000);
        assert_eq!(PadButton::North.bit(), 0x8000);
        assert_eq!(PadButton::Back.bit(), 0x0020);
        assert_eq!(PadButton::Start.bit(), 0x0010);
        assert_eq!(PadButton::Guide.bit(), 0x0400);
        assert_eq!(PadButton::LeftShoulder.bit(), 0x0100);
        assert_eq!(PadButton::RightShoulder.bit(), 0x0200);
        assert_eq!([PadButton::DpadUp, PadButton::DpadDown, PadButton::DpadLeft, PadButton::DpadRight].map(PadButton::bit), [1, 2, 4, 8]);
    }

    #[test]
    fn select_and_y_is_made_of_allowed_bits() {
        let combo = PadButton::Back.bit() | PadButton::North.bit();
        assert_eq!(combo, BACK | Y);
        assert_eq!(combo & !ALLOWED, 0);
        assert!(is_acceptable(combo));
        // shoulders and the D-pad can never be part of a shortcut
        assert!(!is_acceptable(PadButton::LeftShoulder.bit() | PadButton::South.bit()));
        assert!(!is_acceptable(PadButton::DpadUp.bit() | PadButton::South.bit()));
    }
}
