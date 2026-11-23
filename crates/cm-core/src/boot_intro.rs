//! The opening of the app (1.7 BootIntro.cs): the logo over the interface while it loads, with a
//! sound, in one of a few styles. Pure, so the rules are tested; cm-app draws the style and fades it
//! out. Config keys as 1.7 has them: `introAnimation`, `introSound`, `introStyle`.

use std::time::Duration;

/// Console Mode's own: the logo powers on and draws itself ("Liga o PC").
pub const CONSOLE: &str = "console";
/// In the mood of the Xbox 360's: green ribbons spiralling into an orb that flashes, on a light background.
pub const X360: &str = "x360";
/// In the mood of the PS4's: waves of light and rising particles on a deep blue.
pub const PS4: &str = "ps4";

/// In the mood of the PS1: on white, the pieces fly in and lock together; then black.
pub const PS1: &str = "ps1";
/// In the mood of the PS2: towers of light over a dark blue field, specks drifting.
pub const PS2: &str = "ps2";
/// In the mood of the PS3: a white silk wave on black.
pub const PS3: &str = "ps3";
/// In the mood of the first Xbox: green liquid light merging into a core that bursts.
pub const XBOX: &str = "xbox";
/// In the mood of the Xbox One: a ring of white light drawing itself on black.
pub const XONE: &str = "xone";

pub const STYLES: [&str; 8] = [CONSOLE, X360, PS4, PS1, PS2, PS3, XBOX, XONE];
/// The console-inspired ones, the surprises are drawn from.
pub const INSPIRED: [&str; 7] = [X360, PS4, PS1, PS2, PS3, XBOX, XONE];

/// The picker's last choice, "None (opens straight away)": no opening at all (saved as `introAnimation` off).
pub const NONE: &str = "none";

/// While no style was ever picked, now and then (this share of the openings) the app opens with one of
/// the console-inspired styles instead of its own, as a surprise. Picking any style, its own included, ends that.
pub const SURPRISE_SHARE: f64 = 0.125;

/// An unknown or missing style (a hand-edited config, a style removed later) is the app's own.
pub fn normalize(style: Option<&str>) -> &'static str {
    STYLES.iter().copied().find(|s| style.is_some_and(|x| x.eq_ignore_ascii_case(s))).unwrap_or(CONSOLE)
}

/// The style to show: the one picked in Sistema; with none picked, the app's own, or a console-inspired
/// one when the roll (0 to 1) falls in the surprise share, each as likely.
pub fn pick_for_opening(chosen: Option<&str>, roll: f64) -> &'static str {
    if chosen.is_some_and(|c| !c.trim().is_empty()) { return normalize(chosen); }
    if roll >= SURPRISE_SHARE { return CONSOLE; }
    INSPIRED[((roll / SURPRISE_SHARE * INSPIRED.len() as f64) as usize).min(INSPIRED.len() - 1)]
}

/// Shown at least this long, so a fast start doesn't cut it: the logo has to have landed.
pub fn minimum_shown(style: Option<&str>) -> Duration {
    match normalize(style) {
        // the ribbons swirl in, the flash on the sound's last hit (3.8 s), then the logo settles
        X360 => Duration::from_millis(5200),
        // the logo comes in with the chime, then the waves for a moment
        PS4 => Duration::from_millis(3400),
        // as long as their sounds: white, the pieces land on the chime (3 s), then the black screen
        PS1 => Duration::from_millis(6600),
        // the towers, then the logo out of the dark
        PS2 => Duration::from_millis(5600),
        // the wave, the logo, and the chime at the end (7 s)
        PS3 => Duration::from_millis(8200),
        // the blobs merge and burst on the sound's peak (6 s), the orb settles
        XBOX => Duration::from_millis(8200),
        // the ring draws itself on the swell (1 s), the logo comes in
        XONE => Duration::from_millis(3300),
        // "Liga o PC": the name comes in at 2.4 s
        _ => Duration::from_millis(3200),
    }
}

/// Only when the window opens to be used: a start that keeps it hidden (--tray, --start) skips it.
pub fn should_show(enabled: bool, window_shown: bool) -> bool { enabled && window_shown }

/// The sound has its own setting, and plays only with the opening itself.
pub fn should_play_sound(shown: bool, sound_enabled: bool) -> bool { shown && sound_enabled }

/// How much longer to keep it up once loading is done.
pub fn still_to_show(shown_for: Duration, style: Option<&str>) -> Duration { minimum_shown(style).saturating_sub(shown_for) }

/// The picker's value for a saved config: "none" when the opening is off, else the style in effect.
pub fn picker_value(animation: bool, style: Option<&str>) -> &'static str { if animation { normalize(style) } else { NONE } }

/// What the config holds after the picker chose `value`: `(introAnimation, introStyle)`. "None" turns the
/// opening off and keeps the style that was there; any style turns it on and counts as a choice (no more surprises).
pub fn after_choice(value: &str, previous_style: Option<&str>) -> (bool, Option<String>) {
    if value == NONE { (false, Some(normalize(previous_style).to_string())) } else { (true, Some(normalize(Some(value)).to_string())) }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn it_shows_only_when_on_and_the_window_opens_to_be_used() {
        assert!(should_show(true, true));
        assert!(!should_show(false, true));
        assert!(!should_show(true, false));
    }

    #[test]
    fn the_sound_has_its_own_switch_and_only_plays_with_the_opening() {
        assert!(should_play_sound(true, true));
        assert!(!should_play_sound(true, false));
        assert!(!should_play_sound(false, true));
    }

    #[test]
    fn unknown_styles_fall_back_to_the_apps_own() {
        assert_eq!(["console", "X360", "ps4", "ps5", "PS1", "xone"].map(|s| normalize(Some(s))), ["console", "x360", "ps4", "console", "ps1", "xone"]);
        assert_eq!(normalize(None), "console");
    }

    #[test]
    fn unpicked_it_is_the_apps_own_with_now_and_then_a_surprise_picked_it_is_always_that_one() {
        assert_eq!(pick_for_opening(None, 0.01), "x360");
        assert_eq!(pick_for_opening(None, 0.07), "ps2");
        assert_eq!(pick_for_opening(None, 0.124), "xone");
        assert_eq!(pick_for_opening(None, 0.5), "console");
        assert_eq!(pick_for_opening(None, 0.999), "console");
        assert_eq!(pick_for_opening(Some(""), 0.01), "x360");
        assert_eq!(pick_for_opening(Some("console"), 0.01), "console");
        assert_eq!(pick_for_opening(Some("ps4"), 0.9), "ps4");
        assert_eq!(pick_for_opening(Some("x360"), 0.5), "x360");
    }

    #[test]
    fn the_surprises_are_rare_and_shared_by_every_console_style() {
        let picks: Vec<_> = (0..70_000).map(|i| pick_for_opening(None, i as f64 / 70_000.)).collect();
        let share = picks.iter().filter(|p| **p == CONSOLE).count() as f64 / 70_000.;
        assert!((0.85..=0.9).contains(&share), "{share}");
        let counts: Vec<usize> = INSPIRED.iter().map(|s| picks.iter().filter(|p| *p == s).count()).collect();
        assert!(counts.iter().max().unwrap() - counts.iter().min().unwrap() <= 1, "{counts:?}");
    }

    #[test]
    fn a_fast_start_still_shows_each_style_until_its_logo_has_landed() {
        for style in STYLES {
            let minimum = minimum_shown(Some(style));
            assert_eq!(still_to_show(Duration::ZERO, Some(style)), minimum);
            assert_eq!(still_to_show(minimum - Duration::from_millis(800), Some(style)), Duration::from_millis(800));
            assert_eq!(still_to_show(Duration::from_secs(10), Some(style)), Duration::ZERO);
        }
    }

    #[test]
    fn the_picker_shows_none_when_the_opening_is_off_and_saves_it_as_off() {
        assert_eq!(picker_value(true, None), "console");
        assert_eq!(picker_value(true, Some("ps4")), "ps4");
        assert_eq!(picker_value(false, Some("ps4")), "none");
        assert_eq!(after_choice("x360", None), (true, Some("x360".into())));
        // none keeps the style that was there (the app's own when there was none)
        assert_eq!(after_choice("none", Some("ps4")), (false, Some("ps4".into())));
        assert_eq!(after_choice("none", None), (false, Some("console".into())));
    }

    #[test]
    fn existing_configs_get_the_opening_its_sound_and_the_surprises() {
        let c = crate::config::Config::from_json("{}").unwrap();
        assert!(c.intro_animation && c.intro_sound);
        assert_eq!(c.intro_style, None);
        assert_eq!(normalize(c.intro_style.as_deref()), "console");
        // a 1.7 file that turned the sound off keeps it off
        let c = crate::config::Config::from_json(r#"{"introSound": false, "introStyle": "ps4"}"#).unwrap();
        assert!(!c.intro_sound);
        assert_eq!(c.intro_style.as_deref(), Some("ps4"));
    }
}
