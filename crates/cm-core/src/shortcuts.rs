//! Controller shortcuts (ported from 1.7's ControllerShortcuts and ControllerHoldWatcher): which
//! buttons a shortcut may use, what a combo is called, which two shortcuts collide, and when a
//! held combo fires. Buttons are XInput bits (every pad is translated to the same bits), 0 = not set.
//! Pure, so it is tested anywhere.

use std::time::Duration;

use crate::config::Config;

pub const GUIDE: u16 = 0x0400;
pub const START: u16 = 0x0010;
pub const BACK: u16 = 0x0020;
pub const A: u16 = 0x1000;
pub const B: u16 = 0x2000;
pub const X: u16 = 0x4000;
pub const Y: u16 = 0x8000;
pub const LB: u16 = 0x0100;
pub const RB: u16 = 0x0200;
pub const DPAD_UP: u16 = 0x0001;
pub const DPAD_DOWN: u16 = 0x0002;
pub const DPAD_LEFT: u16 = 0x0004;
pub const DPAD_RIGHT: u16 = 0x0008;

/// Buttons a shortcut can use: the ones every pad reports. The D-pad is left out (1.7: a Sony
/// pad's left stick also set those bits) and so are the shoulders.
pub const ALLOWED: u16 = GUIDE | START | BACK | A | B | X | Y;

/// How long the Home and Exit shortcuts are held (1.7 `ControllerHoldWatcher.LongHold`).
pub const LONG_HOLD: Duration = Duration::from_millis(900);
/// A combo of buttons is quick: the session menu and Task View fire after this.
pub const QUICK_HOLD: Duration = Duration::from_millis(250);
/// The Guide button alone is a deliberate hold for Task View, like on a console.
pub const GUIDE_TASK_VIEW_HOLD: Duration = Duration::from_millis(600);

/// What a controller shortcut does.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Slot {
    /// Idle in the tray (or behind other windows): bring Console Mode to the front.
    Home,
    /// In a session: open the menu over the game.
    Menu,
    /// In a session: go back to the PC (restore the desk).
    Exit,
    /// Anytime: Windows' Task View (Win + Tab), driven with the controller while it is open.
    TaskView,
}

/// In the order masks are stored (`[home, menu, exit, task view]`).
pub const SLOTS: [Slot; 4] = [Slot::Home, Slot::Menu, Slot::Exit, Slot::TaskView];

impl Slot {
    pub fn index(self) -> usize { self as usize }

    /// How long the combo is held before the shortcut fires.
    pub fn hold(self, mask: u16) -> Duration {
        match self {
            Slot::Home | Slot::Exit => LONG_HOLD,
            Slot::Menu => QUICK_HOLD,
            Slot::TaskView if mask == GUIDE => GUIDE_TASK_VIEW_HOLD,
            Slot::TaskView => QUICK_HOLD,
        }
    }
}

/// The combo offered for each shortcut.
pub fn suggested(slot: Slot) -> u16 {
    match slot {
        Slot::Home => GUIDE,
        Slot::Menu => BACK | Y,
        Slot::TaskView => BACK | X,
        Slot::Exit => START | BACK,
    }
}

/// The combos the settings rows cycle through, the suggested one first. Off (0) is the row's
/// own first choice, so it is not listed here.
pub fn presets(slot: Slot) -> Vec<u16> {
    let mut list = vec![suggested(slot)];
    for m in [GUIDE, BACK | Y, BACK | X, START | BACK, BACK | B, START | Y] {
        if !list.contains(&m) { list.push(m); }
    }
    list
}

/// Config value to a valid mask: unknown bits are dropped, junk becomes "not set".
pub fn sanitize(raw: i64) -> u16 { if !(0..=u16::MAX as i64).contains(&raw) { 0 } else { raw as u16 & ALLOWED } }

pub fn button_count(mask: u16) -> u32 { mask.count_ones() }

/// Whether any one device holds the full combo (the buttons of two pads are never combined).
pub fn is_held_on_any_device(mask: u16, device_states: impl IntoIterator<Item = u16>) -> bool {
    mask != 0 && device_states.into_iter().any(|buttons| buttons & mask == mask)
}

/// A combo of two or more buttons, or the Guide button alone. A single face button would fire
/// in the middle of any game.
pub fn is_acceptable(mask: u16) -> bool {
    mask != 0 && mask & !ALLOWED == 0 && (mask & GUIDE != 0 || button_count(mask) >= 2)
}

/// Two combos collide when one is the other or contains it: both would fire together.
pub fn overlaps(a: u16, b: u16) -> bool {
    if a == 0 || b == 0 { return false; }
    let both = a & b;
    both == a || both == b
}

/// The other shortcut that `mask` would collide with, if any. `current`: saved masks by [`Slot::index`].
pub fn find_conflict(slot: Slot, mask: u16, current: &[u16; 4]) -> Option<Slot> {
    SLOTS.into_iter().find(|other| *other != slot && overlaps(mask, current[other.index()]))
}

/// "Select + Y" on an Xbox pad, "Create + △" on a PlayStation one; "" when not set.
pub fn format(mask: u16, playstation: bool) -> String {
    const NAMES: [(u16, &str, &str); 7] = [
        (GUIDE, "Xbox", "PS"),
        (BACK, "Select", "Create"),
        (START, "Start", "Options"),
        (A, "A", "✕"),
        (B, "B", "○"),
        (X, "X", "□"),
        (Y, "Y", "△"),
    ];
    NAMES.iter().filter(|(bit, ..)| mask & bit != 0).map(|(_, xbox, sony)| if playstation { *sony } else { *xbox }).collect::<Vec<_>>().join(" + ")
}

/// The four saved shortcuts, cleaned like 1.7's `LoadShortcuts`: unknown bits are dropped, a combo
/// that is not acceptable or that collides with an earlier slot becomes "not set". 0 = not set.
pub fn masks(c: &Config) -> [u16; 4] {
    let mut m = [0u16; 4];
    for slot in SLOTS { m[slot.index()] = sanitize(c.shortcut(slot) as i64); }
    for slot in SLOTS {
        let mask = m[slot.index()];
        if mask == 0 { continue; }
        if !is_acceptable(mask) || find_conflict(slot, mask, &m).is_some_and(|other| other.index() < slot.index()) { m[slot.index()] = 0; }
    }
    m
}

impl Config {
    /// The saved XInput bitmask of a shortcut, as it is in the file.
    pub fn shortcut(&self, slot: Slot) -> i32 {
        match slot {
            Slot::Home => self.home_shortcut,
            Slot::Menu => self.menu_shortcut,
            Slot::Exit => self.exit_shortcut,
            Slot::TaskView => self.task_view_shortcut,
        }
    }

    pub fn set_shortcut(&mut self, slot: Slot, mask: u16) {
        let v = mask as i32;
        match slot {
            Slot::Home => self.home_shortcut = v,
            Slot::Menu => self.menu_shortcut = v,
            Slot::Exit => self.exit_shortcut = v,
            Slot::TaskView => self.task_view_shortcut = v,
        }
    }
}

// ───────────── "Gravar combinação…": capturing a combo from the pad ─────────────

/// Why a captured combo was not saved.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum CaptureError {
    /// B alone, let go: the capture was cancelled (a lone B is never a shortcut).
    Cancelled,
    /// One face button alone (or nothing usable).
    NotAllowed,
    /// The combo is, or contains, or is contained in another shortcut's.
    InUse(Slot),
}

/// What a captured combo means for `slot` (1.7 `ShortcutPicker.Poll` and `TrySetShortcut`).
pub fn check_capture(slot: Slot, mask: u16, current: &[u16; 4]) -> Result<u16, CaptureError> {
    if mask == B { return Err(CaptureError::Cancelled); }
    if !is_acceptable(mask) { return Err(CaptureError::NotAllowed); }
    match find_conflict(slot, mask, current) { Some(other) => Err(CaptureError::InUse(other)), None => Ok(mask) }
}

/// Captures one pad's buttons until that pad lets go, then reports the combo (1.7 `ShortcutCapture`).
/// Each pad arms on its own after a poll with nothing held, so the A that started the capture (still
/// down) or another pad being held never counts, and two pads are never mixed into one combo.
#[derive(Default)]
pub struct Capture {
    released: std::collections::BTreeSet<u32>,
    device: Option<u32>,
    seen: u16,
    held: u16,
}

impl Capture {
    pub fn new() -> Self { Self::default() }

    /// The allowed buttons held right now on the pad being captured (for the live text).
    pub fn current_held(&self) -> u16 { self.held }

    /// One poll of every pad: (id, buttons). Returns the combo when the captured pad lets go or leaves.
    pub fn feed(&mut self, devices: &[(u32, u16)]) -> Option<u16> {
        self.held = 0;
        self.released.retain(|id| devices.iter().any(|(d, _)| d == id));
        if let Some(device) = self.device {
            let held = devices.iter().find(|(d, _)| *d == device).map(|(_, b)| b & ALLOWED);
            match held {
                Some(h) if h != 0 => { self.held = h; self.seen |= h; return None; }
                still => {
                    self.device = None;
                    if still.is_some() { self.released.insert(device); }
                    let combo = std::mem::take(&mut self.seen);
                    return (combo != 0).then_some(combo);
                }
            }
        }
        let mut sorted: Vec<(u32, u16)> = devices.to_vec();
        sorted.sort_by_key(|(id, _)| *id);
        for (id, buttons) in sorted {
            let held = buttons & ALLOWED;
            if held == 0 { self.released.insert(id); continue; }
            if !self.released.contains(&id) { continue; }
            self.device = Some(id);
            self.held = held;
            self.seen = held;
            break;
        }
        None
    }
}

/// Watches one combo: it fires once per hold, after the combo has been down for the hold time
/// (1.7 `ControllerHoldWatcher.Poll`). A combo already held when the watch starts, or when the
/// mask changes, must be released first. Time is any monotonic clock.
pub struct HoldTracker {
    mask: u16,
    hold: Duration,
    enabled: bool,
    held_since: Option<Duration>,
    fired: bool,
}

impl HoldTracker {
    pub fn new() -> Self { Self { mask: 0, hold: LONG_HOLD, enabled: false, held_since: None, fired: false } }

    /// Changing the combo re-arms the watch.
    pub fn set(&mut self, mask: u16, hold: Duration) {
        self.hold = hold;
        if self.mask != mask {
            self.mask = mask;
            self.held_since = None;
            self.fired = true;
        }
    }

    pub fn mask(&self) -> u16 { self.mask }

    /// Start or stop the watch (it depends on the situation: in a session or not).
    pub fn set_enabled(&mut self, on: bool) {
        if on == self.enabled { return; }
        self.enabled = on;
        self.held_since = None;
        // a combo held while the watch starts must be let go first
        self.fired = on;
    }

    /// One sample: `held` says whether some device holds the whole combo. True once per hold.
    pub fn poll(&mut self, held: bool, now: Duration) -> bool {
        if !self.enabled || self.mask == 0 || !held {
            self.held_since = None;
            self.fired = false;
            return false;
        }
        let since = *self.held_since.get_or_insert(now);
        if self.fired || now.saturating_sub(since) < self.hold { return false; }
        self.fired = true;
        true
    }
}

impl Default for HoldTracker {
    fn default() -> Self { Self::new() }
}

#[cfg(test)]
mod tests {
    use super::*;

    const MS: fn(u64) -> Duration = Duration::from_millis;

    fn masks_of(home: u16, menu: u16, exit: u16, task_view: u16) -> [u16; 4] { [home, menu, exit, task_view] }

    #[test]
    fn a_new_config_has_the_17_defaults() {
        let c = Config::from_json("{}").unwrap();
        assert_eq!((c.home_shortcut, c.menu_shortcut, c.exit_shortcut), (0, 0, 0));
        // Task View is on from the start (Select + X), like 1.7
        assert_eq!(c.task_view_shortcut, (BACK | X) as i32);
        assert!(c.task_view_playstation);
        assert!(!c.auto_start_on_controller);
    }

    #[test]
    fn the_shortcut_fields_keep_the_17_json() {
        let c = Config::from_json(r#"{"homeShortcut":1024,"menuShortcut":32800,"exitShortcut":48,"taskViewShortcut":0,"taskViewPlayStation":false,"autoStartOnController":true}"#).unwrap();
        assert_eq!(masks(&c), [GUIDE, BACK | Y, START | BACK, 0]);
        assert!(!c.task_view_playstation && c.auto_start_on_controller);
        let back: serde_json::Value = serde_json::from_str(&c.to_json()).unwrap();
        assert_eq!(back["homeShortcut"], 1024);
        assert_eq!(back["menuShortcut"], 32800);
        assert_eq!(back["taskViewShortcut"], 0);
        assert_eq!(back["taskViewPlayStation"], false);
        assert_eq!(back["autoStartOnController"], true);
    }

    #[test]
    fn a_hand_edited_file_cannot_give_one_combo_two_jobs() {
        let mut c = Config::default();
        c.home_shortcut = (BACK | Y) as i32;
        c.menu_shortcut = (BACK | Y) as i32;
        c.exit_shortcut = A as i32; // a lone face button
        c.task_view_shortcut = 0x0001; // an unknown bit
        assert_eq!(masks(&c), [BACK | Y, 0, 0, 0], "the later slot is dropped");
    }

    #[test]
    fn suggested_shortcuts_are_valid_and_do_not_collide() {
        for slot in SLOTS {
            let s = suggested(slot);
            assert!(is_acceptable(s), "{slot:?}");
            assert_eq!(find_conflict(slot, s, &[0; 4]), None);
        }
        let all = SLOTS.map(suggested);
        for slot in SLOTS { assert_eq!(find_conflict(slot, all[slot.index()], &all), None, "{slot:?}"); }
    }

    #[test]
    fn every_preset_is_acceptable_and_the_suggested_one_comes_first() {
        for slot in SLOTS {
            let list = presets(slot);
            assert_eq!(list[0], suggested(slot));
            assert!(list.iter().all(|m| is_acceptable(*m)));
            let mut sorted = list.clone();
            sorted.sort_unstable();
            sorted.dedup();
            assert_eq!(sorted.len(), list.len(), "no duplicates");
        }
    }

    #[test]
    fn only_combos_or_the_home_button_are_acceptable() {
        for (mask, expected) in [
            (GUIDE, true),
            (BACK | Y, true),
            (START | BACK | A, true),
            (GUIDE | A, true),
            (A, false), // a lone face button fires in every game
            (START, false),
            (0, false),
            (0x0001 | 0x0002, false), // D-pad: a Sony stick also sets these bits
            (A | 0x0100, false),      // shoulder bits are not reported by every pad
        ] {
            assert_eq!(is_acceptable(mask), expected, "{mask:#06x}");
        }
    }

    #[test]
    fn the_same_combo_cannot_serve_two_actions() {
        let current = masks_of(0, BACK | Y, 0, 0);
        assert_eq!(find_conflict(Slot::Exit, BACK | Y, &current), Some(Slot::Menu));
    }

    #[test]
    fn a_combo_that_contains_or_is_inside_another_collides() {
        // Start + Back is held on the way to Start + Back + Y: both would fire together.
        let current = masks_of(0, 0, START | BACK, 0);
        assert_eq!(find_conflict(Slot::Menu, START | BACK | Y, &current), Some(Slot::Exit));
        assert_eq!(find_conflict(Slot::Home, START | BACK, &current), Some(Slot::Exit));
    }

    #[test]
    fn sharing_one_button_is_fine_and_an_unset_shortcut_never_collides() {
        let current = masks_of(0, BACK | Y, 0, 0);
        assert_eq!(find_conflict(Slot::Exit, START | BACK, &current), None);
        assert_eq!(find_conflict(Slot::Home, GUIDE, &current), None);
        assert!(!overlaps(0, 0));
    }

    #[test]
    fn re_picking_a_slot_does_not_collide_with_itself() {
        assert_eq!(find_conflict(Slot::Menu, BACK | Y, &masks_of(0, BACK | Y, 0, 0)), None);
    }

    #[test]
    fn combos_are_named_for_the_pad_in_use() {
        for (mask, ps, expected) in [
            (BACK | Y, false, "Select + Y"),
            (BACK | Y, true, "Create + △"),
            (START | BACK, false, "Select + Start"),
            (START | BACK, true, "Create + Options"),
            (GUIDE, false, "Xbox"),
            (GUIDE, true, "PS"),
            (0, false, ""),
        ] {
            assert_eq!(format(mask, ps), expected);
        }
    }

    #[test]
    fn saved_values_are_cleaned_up_on_load() {
        for (raw, expected) in [(-1, 0), (70000, 0), (0x0001, 0), ((BACK | Y) as i64, BACK | Y)] {
            assert_eq!(sanitize(raw), expected, "{raw}");
        }
    }

    #[test]
    fn a_combo_must_be_held_by_one_device() {
        assert!(!is_held_on_any_device(BACK | Y, [BACK, Y]));
        assert!(is_held_on_any_device(BACK | Y, [BACK, BACK | Y]));
        assert!(!is_held_on_any_device(0, [BACK]), "an empty combo is never held");
    }

    #[test]
    fn the_hold_time_depends_on_the_shortcut() {
        assert_eq!(Slot::Home.hold(GUIDE), LONG_HOLD);
        assert_eq!(Slot::Exit.hold(START | BACK), LONG_HOLD);
        assert_eq!(Slot::Menu.hold(BACK | Y), QUICK_HOLD);
        assert_eq!(Slot::TaskView.hold(BACK | X), QUICK_HOLD);
        assert_eq!(Slot::TaskView.hold(GUIDE), GUIDE_TASK_VIEW_HOLD);
    }

    fn tracker(mask: u16) -> HoldTracker {
        let mut t = HoldTracker::new();
        t.set(mask, LONG_HOLD);
        t.set_enabled(true);
        // released at first, as when nobody holds anything
        assert!(!t.poll(false, MS(0)));
        t
    }

    #[test]
    fn a_combo_fires_once_after_the_hold_time() {
        let mut t = tracker(BACK | Y);
        assert!(!t.poll(true, MS(100)));
        assert!(!t.poll(true, MS(900)));
        assert!(t.poll(true, MS(1000)));
        assert!(!t.poll(true, MS(1100)), "once per hold");
        assert!(!t.poll(true, MS(5000)));
    }

    #[test]
    fn letting_go_early_cancels_and_a_new_hold_fires_again() {
        let mut t = tracker(GUIDE);
        assert!(!t.poll(true, MS(0)));
        assert!(!t.poll(true, MS(500)));
        assert!(!t.poll(false, MS(600)));
        assert!(!t.poll(true, MS(700)));
        assert!(!t.poll(true, MS(1500)));
        assert!(t.poll(true, MS(1600)));
        assert!(!t.poll(false, MS(1700)));
        assert!(!t.poll(true, MS(1800)));
        assert!(t.poll(true, MS(2700)));
    }

    #[test]
    fn a_combo_already_held_when_the_watch_starts_must_be_let_go_first() {
        let mut t = HoldTracker::new();
        t.set(GUIDE, LONG_HOLD);
        t.set_enabled(true);
        assert!(!t.poll(true, MS(0)));
        assert!(!t.poll(true, MS(5000)), "held since before the watch: ignored");
        assert!(!t.poll(false, MS(5100)));
        assert!(!t.poll(true, MS(5200)));
        assert!(t.poll(true, MS(6100)));
    }

    #[test]
    fn changing_the_combo_rearms_and_a_shortcut_that_is_not_set_never_fires() {
        let mut t = tracker(BACK | Y);
        t.set(BACK | X, LONG_HOLD);
        assert!(!t.poll(true, MS(0)));
        assert!(!t.poll(true, MS(2000)), "held while it was changed: let go first");
        let mut off = tracker(0);
        assert!(!off.poll(true, MS(0)));
        assert!(!off.poll(true, MS(5000)));
    }

    #[test]
    fn a_disabled_watch_never_fires() {
        let mut t = tracker(START | BACK);
        t.set_enabled(false);
        assert!(!t.poll(true, MS(0)));
        assert!(!t.poll(true, MS(5000)));
    }

    #[test]
    fn a_quick_hold_fires_sooner() {
        let mut t = HoldTracker::new();
        t.set(BACK | X, QUICK_HOLD);
        t.set_enabled(true);
        t.poll(false, MS(0));
        assert!(!t.poll(true, MS(10)));
        assert!(!t.poll(true, MS(200)));
        assert!(t.poll(true, MS(260)));
    }

    #[test]
    fn a_capture_waits_for_the_pad_to_be_let_go_first() {
        let mut c = Capture::new();
        // the A that picked "Gravar combinação…" is still down: it doesn't start the capture
        assert_eq!(c.feed(&[(1, A)]), None);
        assert_eq!(c.current_held(), 0);
        assert_eq!(c.feed(&[(1, A | B)]), None);
        assert_eq!(c.current_held(), 0, "not armed until that pad was neutral once");
        assert_eq!(c.feed(&[(1, 0)]), None);
        assert_eq!(c.feed(&[(1, BACK)]), None);
        assert_eq!(c.current_held(), BACK);
        assert_eq!(c.feed(&[(1, BACK | Y)]), None);
        assert_eq!(c.current_held(), BACK | Y);
        // letting go of one button first still records the whole combo
        assert_eq!(c.feed(&[(1, Y)]), None);
        assert_eq!(c.feed(&[(1, 0)]), Some(BACK | Y));
    }

    #[test]
    fn a_capture_never_mixes_two_pads_and_ignores_the_dpad() {
        let mut c = Capture::new();
        c.feed(&[(1, 0), (2, Y)]);
        assert_eq!(c.feed(&[(1, START), (2, Y)]), None);
        assert_eq!(c.current_held(), START, "pad 2 was already held when it began");
        assert_eq!(c.feed(&[(1, START | BACK | DPAD_UP), (2, Y | X)]), None);
        assert_eq!(c.current_held(), START | BACK);
        assert_eq!(c.feed(&[(1, 0), (2, Y | X)]), Some(START | BACK));
        // pad 2 was held all along: it must let go before it can be captured
        assert_eq!(c.feed(&[(1, 0), (2, Y | X)]), None);
        assert_eq!(c.current_held(), 0);
    }

    #[test]
    fn a_pad_that_leaves_mid_capture_ends_it_with_what_it_held() {
        let mut c = Capture::new();
        c.feed(&[(7, 0)]);
        c.feed(&[(7, GUIDE)]);
        assert_eq!(c.feed(&[]), Some(GUIDE));
        // only buttons a shortcut can use count: a d-pad press alone never starts one
        let mut c = Capture::new();
        c.feed(&[(1, 0)]);
        assert_eq!(c.feed(&[(1, DPAD_LEFT | LB)]), None);
        assert_eq!(c.current_held(), 0);
        assert_eq!(c.feed(&[(1, 0)]), None);
    }

    #[test]
    fn a_captured_combo_is_checked_like_17() {
        let current = masks_of(0, BACK | Y, START | BACK, BACK | X);
        assert_eq!(check_capture(Slot::Home, B, &current), Err(CaptureError::Cancelled));
        assert_eq!(check_capture(Slot::Home, A, &current), Err(CaptureError::NotAllowed));
        assert_eq!(check_capture(Slot::Home, GUIDE, &current), Ok(GUIDE));
        assert_eq!(check_capture(Slot::Home, BACK | Y | A, &current), Err(CaptureError::InUse(Slot::Menu)), "contains Select + Y");
        assert_eq!(check_capture(Slot::Home, A | B, &current), Ok(A | B));
        // its own combo again is fine
        assert_eq!(check_capture(Slot::Menu, BACK | Y, &current), Ok(BACK | Y));
        // B in a combo is fine; only B alone cancels
        assert_eq!(check_capture(Slot::Home, B | Y, &current), Ok(B | Y));
    }
}
