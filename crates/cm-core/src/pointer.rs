//! "Park the pointer" (ported from 1.7's PointerParking, issue #172): a session that starts leaves
//! the mouse pointer in the middle of the TV, so it is moved to a corner of the game screen, out of
//! the way. Steam and others recenter it while their window opens, so it is done twice. Going back
//! to the PC puts it where it was before the session. Pure, so it is tested anywhere.

use std::time::Duration;

/// How far inside the corner the pointer sits (some TVs crop the very edge).
pub const MARGIN: i32 = 6;
/// How many times the pointer is parked per session.
pub const PARKS: u32 = 2;
/// Between two parks, so the second one lands after the launcher has settled.
pub const BETWEEN: Duration = Duration::from_secs(3);
/// Xbox mode shows no window to watch: it is parked this long after the launch instead.
pub const XBOX_DELAY: Duration = Duration::from_secs(4);

/// The bottom-right corner of a screen `(left, top, right, bottom)`, a few pixels inside it.
pub fn bottom_right(rect: (i32, i32, i32, i32)) -> (i32, i32) {
    let (w, h) = (rect.2.saturating_sub(rect.0), rect.3.saturating_sub(rect.1));
    (rect.0 + (w - 1 - MARGIN).max(0), rect.1 + (h - 1 - MARGIN).max(0))
}

/// `done`: times parked in this session; `ready`: the launcher is up (its window appeared, or the
/// Xbox delay passed); `since_last`: time since the last park.
pub fn is_due(enabled: bool, done: u32, ready: bool, since_last: Option<Duration>) -> bool {
    enabled && ready && done < PARKS && since_last.is_none_or(|d| d >= BETWEEN)
}

/// Going back to the PC: the pointer returns to where it was, but only when it was parked in this
/// session (otherwise the user may have moved it on purpose) and that spot is on a screen that is on.
pub fn should_return(before: Option<(i32, i32)>, parks: u32, on_screen: bool) -> bool { before.is_some() && parks > 0 && on_screen }

#[cfg(test)]
mod tests {
    use super::*;
    const S: Duration = Duration::from_secs(1);

    #[test]
    fn the_pointer_goes_a_few_pixels_inside_the_bottom_right_corner() {
        assert_eq!(bottom_right((0, 0, 3840, 2160)), (3833, 2153));
    }

    #[test]
    fn a_screen_left_of_or_above_the_main_one_has_negative_coordinates() {
        assert_eq!(bottom_right((-1920, 0, 0, 1080)), (-7, 1073));
        assert_eq!(bottom_right((0, -2160, 3840, 0)), (3833, -7));
    }

    #[test]
    fn a_degenerate_screen_never_pushes_the_pointer_outside_it() {
        assert_eq!(bottom_right((100, 50, 103, 53)), (100, 50));
    }

    #[test]
    fn nothing_happens_while_the_setting_is_off_or_the_launcher_is_not_up() {
        assert!(!is_due(false, 0, true, None));
        assert!(!is_due(true, 0, false, None));
    }

    #[test]
    fn the_first_park_is_due_as_soon_as_the_launcher_is_up() { assert!(is_due(true, 0, true, None)); }

    #[test]
    fn the_second_park_waits_for_the_launcher_to_settle_and_there_is_no_third() {
        assert!(!is_due(true, 1, true, Some(S * 2 + Duration::from_millis(900))));
        assert!(is_due(true, 1, true, Some(S * 3)));
        assert!(!is_due(true, PARKS, true, Some(S * 300)));
    }

    #[test]
    fn the_pointer_goes_back_only_after_a_park_and_onto_a_screen_that_is_on() {
        assert!(should_return(Some((800, 600)), 1, true));
        assert!(!should_return(Some((800, 600)), 0, true), "never parked: left alone");
        assert!(!should_return(Some((800, 600)), 2, false), "that screen stayed off");
        assert!(!should_return(None, 2, true), "position unknown");
    }
}
