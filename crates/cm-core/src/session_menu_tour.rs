//! The session menu's guided tour (1.7 SessionMenuTour.cs): which stops it makes. Pure, so it is
//! tested; the menu in cm-app highlights the button of the current stop and puts the texts beside it.
//!
//! 1.7's menu has a different list of options (a separate "Back to PC" and "Exit"), so the
//! stops follow 2.0's buttons: back to the game, windows, screen and sound, volume, FPS, Task View,
//! ControlFS and JoyChromium, and ending the session.

use crate::shortcuts;

/// The stops of the tour, in the order they come.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Stop {
    /// The first time the menu opens: asks whether to take the tour at all.
    Invite,
    BackToGame,
    Windows,
    /// The game screen and the audio output.
    Display,
    Volume,
    Fps,
    TaskView,
    /// ControlFS and JoyChromium (1.7 had a ControlFS stop that also told about JoyChromium).
    Apps,
    /// "Encerrar sessão" (outside a session: it only closes the menu).
    Exit,
}

/// The stops, with the invite first when the tour is offered rather than asked for.
pub fn stops(invite: bool) -> Vec<Stop> {
    let mut list = Vec::new();
    if invite { list.push(Stop::Invite); }
    list.extend([Stop::BackToGame, Stop::Windows, Stop::Display, Stop::Volume, Stop::Fps, Stop::TaskView, Stop::Apps, Stop::Exit]);
    list
}

/// "3 of 8": the position among the real stops (the invite doesn't count); None on the invite.
pub fn progress(stops: &[Stop], index: usize) -> Option<(usize, usize)> {
    if index >= stops.len() || stops[index] == Stop::Invite { return None; }
    let offset = usize::from(stops.first() == Some(&Stop::Invite));
    Some((index - offset + 1, stops.len() - offset))
}

/// Y opens the tour, unless it is part of a controller shortcut being held right now: Select + Y is
/// the suggested way to close this very menu, and that press must not start a tour on its way out.
pub fn is_tour_press(shortcut_masks: &[u16], device_states: &[u16]) -> bool {
    !shortcut_masks.iter().any(|mask| mask & shortcuts::Y != 0 && shortcuts::is_held_on_any_device(*mask, device_states.iter().copied()))
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::{config::Config, shortcuts::{BACK, START, Y}};

    #[test]
    fn the_offered_tour_starts_with_the_invite_and_ends_on_the_way_out() {
        let s = stops(true);
        assert_eq!(s[0], Stop::Invite);
        assert_eq!(s.last(), Some(&Stop::Exit));
        assert!(s.contains(&Stop::Fps) && s.contains(&Stop::TaskView) && s.contains(&Stop::Apps));
    }

    #[test]
    fn asked_for_it_skips_the_invite() {
        let s = stops(false);
        assert!(!s.contains(&Stop::Invite));
        assert_eq!(s[0], Stop::BackToGame);
    }

    #[test]
    fn progress_counts_the_real_stops_only() {
        let offered = stops(true);
        assert_eq!(progress(&offered, 0), None);
        assert_eq!(progress(&offered, 1), Some((1, offered.len() - 1)));
        assert_eq!(progress(&offered, offered.len() - 1), Some((offered.len() - 1, offered.len() - 1)));
        let asked = stops(false);
        assert_eq!(progress(&asked, 0), Some((1, asked.len())));
        assert_eq!(progress(&asked, asked.len()), None);
    }

    #[test]
    fn y_opens_the_tour_unless_it_is_part_of_a_shortcut_being_held() {
        let select_y = BACK | Y;
        let start_select = START | BACK;
        let masks = [0, select_y, start_select];
        assert!(is_tour_press(&masks, &[Y]));
        assert!(!is_tour_press(&masks, &[select_y]));
        // another pad holding Select doesn't make it a chord
        assert!(is_tour_press(&masks, &[Y, BACK]));
        // only shortcuts that use Y count: holding one without it doesn't hide this press
        assert!(is_tour_press(&[0, start_select, 0], &[start_select | Y]));
        assert!(is_tour_press(&[0, 0, 0], &[select_y]));
    }

    #[test]
    fn an_existing_config_has_not_seen_the_session_menu_tour() {
        assert!(!Config::from_json("{}").unwrap().session_menu_tour_done);
        // 1.7's key, so an imported config that finished it does not offer it again
        assert!(Config::from_json(r#"{"sessionMenuTourDone": true}"#).unwrap().session_menu_tour_done);
        assert!(Config::default().to_json().contains("\"sessionMenuTourDone\": false"));
    }
}
