//! The centre button of other brands' controllers through standard HID usages (1.7.1 #226,
//! `ControllerMapping.HomeUsages`, issue #217). SDL reads the centre button of the pads it has a
//! driver for (Xbox, PlayStation, Switch, Luna…); for the rest, a pad that declares one of these
//! usages gets its centre button read as Guide, so the controller shortcuts work with any brand
//! that follows the standard. Pure; the HID reading is in cm-platform (hid_win).

/// A HID usage: page and id, as the device descriptor declares it.
pub type Usage = (u16, u16);

/// Generic Desktop "System Main Menu" and Consumer "AC Home".
pub const HOME_USAGES: [Usage; 2] = [(0x01, 0x85), (0x0C, 0x0223)];

pub const MICROSOFT_VENDOR: u16 = 0x045E;

pub fn is_home_usage(u: Usage) -> bool { HOME_USAGES.contains(&u) }

/// Whose centre button is read from HID: everyone but Microsoft (Xbox pads already report Guide) and
/// Sony (the PS button comes with the rest of the pad).
pub fn reads_home_from_hid(vendor: u16) -> bool { vendor != MICROSOFT_VENDOR && vendor != crate::controller_battery::SONY_VENDOR }

/// Usage page 1 (Generic Desktop), joystick or gamepad: a game controller, not a keyboard, a
/// headset or a mouse. Keyboards that declare AC Home are never read.
pub fn is_game_controller(page: u16, usage: u16) -> bool { page == 0x01 && matches!(usage, 0x04 | 0x05) }

/// The name of a usage for the log and the report: "Generic Desktop 0x85 (System Main Menu)".
pub fn describe(u: Usage) -> String {
    match u {
        (0x01, 0x85) => "Generic Desktop 0x85 (System Main Menu)".into(),
        (0x0C, 0x0223) => "Consumer 0x223 (AC Home)".into(),
        (0x09, id) => format!("Button {id}"),
        (0x01, id) => format!("Generic Desktop 0x{id:02X}"),
        (0x0C, id) => format!("Consumer 0x{id:03X}"),
        (page, id) => format!("page 0x{page:02X} usage 0x{id:02X}"),
    }
}

/// Guide held on a collection, from the buttons pressed in its last report.
pub fn holds_home(pressed: &[Usage]) -> bool { pressed.iter().any(|&u| is_home_usage(u)) }

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn the_standard_centre_button_usages() {
        assert!(is_home_usage((0x01, 0x85)) && is_home_usage((0x0C, 0x223)));
        assert!(!is_home_usage((0x09, 1)) && !is_home_usage((0x0C, 0x224)));
        assert!(holds_home(&[(0x09, 1), (0x0C, 0x223)]));
        assert!(!holds_home(&[(0x09, 13)]) && !holds_home(&[]));
    }

    #[test]
    fn xbox_and_playstation_pads_are_left_to_sdl() {
        assert!(!reads_home_from_hid(0x045E) && !reads_home_from_hid(0x054C));
        assert!(reads_home_from_hid(0x0171) && reads_home_from_hid(0x2DC8));
    }

    #[test]
    fn only_joysticks_and_gamepads_count_as_controllers() {
        assert!(is_game_controller(0x01, 0x04) && is_game_controller(0x01, 0x05));
        assert!(!is_game_controller(0x01, 0x06) && !is_game_controller(0x0C, 0x01));
    }

    #[test]
    fn usages_have_readable_names() {
        assert_eq!(describe((0x0C, 0x223)), "Consumer 0x223 (AC Home)");
        assert_eq!(describe((0x09, 13)), "Button 13");
        assert_eq!(describe((0x01, 0x05)), "Generic Desktop 0x05");
        assert_eq!(describe((0xFF00, 1)), "page 0xFF00 usage 0x01");
    }
}
