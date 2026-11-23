//! Controller battery (1.7 ControllerBattery.cs, issue #70): turns what SDL reports for a pad into
//! a percentage, picks what the header shows and decides when to warn. No OS calls; the reading
//! itself is in cm-app (SDL3's `SDL_GetGamepadPowerInfo`).
//!
//! 1.7 merged three Windows sources (Windows.Gaming.Input, XInput and the Sony HID report) because
//! it had no common API. SDL already reads all of them, so only its answer is mapped here, plus
//! the level Windows' Bluetooth stack keeps (1.7.1 #225, issue #224), for pads SDL reads wrong.

/// One pad's battery.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct BatteryReading {
    pub id: String,
    pub name: String,
    pub percent: u8,
    pub charging: bool,
    /// The pad's USB/Bluetooth ids when the source knows them (0 otherwise), so the Bluetooth
    /// reading can stand in for SDL's reading of the same pad.
    pub vendor: u16,
    pub product: u16,
    /// Read from Windows' Bluetooth stack (what Settings › Bluetooth shows), not from SDL.
    pub bluetooth: bool,
}

/// Sony's USB vendor id: SDL reads the PlayStation pads' own report, which beats any other source.
pub const SONY_VENDOR: u16 = 0x054C;

/// The level Windows keeps for a Bluetooth device, 0..100; None when it is out of range.
pub fn from_bluetooth(level: u8) -> Option<u8> { (level <= 100).then_some(level) }

/// SDL's readings with Windows' Bluetooth ones (1.7.1 `ControllerBattery.Combine`, issue #224): a
/// Bluetooth reading replaces SDL's reading of the same pad (same vendor and product id), which some
/// pads fill with a fixed value (an Amazon Luna pad read 10 % for days while Settings said 90 %),
/// and stands alone for a pad SDL gives no level for. SDL still wins for PlayStation pads.
pub fn combine(sdl: Vec<BatteryReading>, bluetooth: Vec<BatteryReading>) -> Vec<BatteryReading> {
    let same = |a: &BatteryReading, b: &BatteryReading| a.vendor != 0 && a.vendor == b.vendor && a.product == b.product;
    let bluetooth: Vec<BatteryReading> = bluetooth.into_iter()
        .filter(|b| !(b.vendor == SONY_VENDOR && sdl.iter().any(|s| same(s, b))))
        .collect();
    let mut out: Vec<BatteryReading> = sdl.into_iter().filter(|s| !bluetooth.iter().any(|b| same(b, s))).collect();
    out.extend(bluetooth);
    out
}

/// SDL's `SDL_PowerState`, without the SDL types (so this stays testable anywhere).
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Power { Unknown, OnBattery, NoBattery, Charging, Charged }

/// A pad's `(percent, charging)` from SDL's power state and percentage (-1 when SDL doesn't know it).
/// None for a wired pad, an unknown state, or a level SDL can't tell (nothing to show).
pub fn from_power(state: Power, percent: i32) -> Option<(u8, bool)> {
    let level = (percent >= 0).then(|| percent.min(100) as u8);
    match state {
        Power::OnBattery => level.map(|p| (p, false)),
        Power::Charging => level.map(|p| (p, true)),
        // full on the cable: SDL may not give a number, but it is full
        Power::Charged => Some((100, true)),
        Power::NoBattery | Power::Unknown => None,
    }
}

/// What to show for a pad, given how it is connected (`wired`: on a cable, or not known to be wireless).
/// SDL reports an Xbox pad on a cable as full and charged, though it has no battery to show (1.7 showed
/// nothing for them); a PlayStation pad on a cable does charge, and its level is shown.
pub fn reading_for(state: Power, percent: i32, wired: bool, playstation: bool) -> Option<(u8, bool)> {
    if wired && !playstation { return None; }
    from_power(state, percent)
}

/// What the header shows when several pads report: the emptiest one.
pub fn lowest(readings: &[BatteryReading]) -> Option<&BatteryReading> {
    readings.iter().min_by_key(|r| (r.percent, r.charging))
}

/// "65%".
pub fn format(r: &BatteryReading) -> String { format!("{}%", r.percent) }

/// For the diagnostics report (English on purpose): "battery 80%", "battery 65%, charging".
pub fn describe(r: &BatteryReading) -> String { format!("battery {}{}", format(r), if r.charging { ", charging" } else { "" }) }

/// The battery icon step for a level: 0 (empty) to 7 (full), the seven icons of the font.
pub fn icon_step(percent: u8) -> u8 {
    match percent {
        0..=7 => 0,
        8..=21 => 1,
        22..=35 => 2,
        36..=49 => 3,
        50..=64 => 4,
        65..=79 => 5,
        80..=94 => 6,
        _ => 7,
    }
}

/// When to warn about a low battery, for one pad: once at 15 % or less, once more at 5 % or less,
/// and again only after the pad charged (or reads well above the threshold, new batteries).
#[derive(Debug, Default)]
pub struct LowBatteryAlarm {
    /// 0, LOW or CRITICAL
    warned: u8,
}

impl LowBatteryAlarm {
    pub const LOW: u8 = 15;
    pub const CRITICAL: u8 = 5;
    const RESET_ABOVE: u8 = 25;

    pub fn new() -> Self { Self::default() }

    /// The threshold just crossed (15 or 5) when a warning is due.
    pub fn update(&mut self, percent: u8, charging: bool) -> Option<u8> {
        if charging || percent > Self::RESET_ABOVE {
            self.warned = 0;
            return None;
        }
        if percent <= Self::CRITICAL && self.warned != Self::CRITICAL {
            self.warned = Self::CRITICAL;
            return Some(Self::CRITICAL);
        }
        if percent <= Self::LOW && self.warned == 0 {
            self.warned = Self::LOW;
            return Some(Self::LOW);
        }
        None
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn reading(id: &str, percent: u8) -> BatteryReading { BatteryReading { id: id.into(), name: id.into(), percent, ..Default::default() } }

    fn pad(id: &str, percent: u8, vendor: u16, product: u16, bluetooth: bool) -> BatteryReading {
        BatteryReading { id: id.into(), name: id.into(), percent, vendor, product, bluetooth, ..Default::default() }
    }

    #[test]
    fn the_bluetooth_level_replaces_sdls_reading_of_the_same_pad() {
        // the Luna case: SDL says 10 %, Windows' Bluetooth says 90 %
        let out = combine(vec![pad("sdl", 10, 0x0171, 0x0419, false)], vec![pad("bt", 90, 0x0171, 0x0419, true)]);
        assert_eq!(out, vec![pad("bt", 90, 0x0171, 0x0419, true)]);
    }

    #[test]
    fn sdl_wins_for_playstation_pads_and_keeps_the_others() {
        let ds = pad("sdl-ds", 55, SONY_VENDOR, 0x0CE6, false);
        let xbox = pad("sdl-xbox", 40, 0x045E, 0x0B13, false);
        let out = combine(vec![ds.clone(), xbox.clone()], vec![pad("bt-ds", 60, SONY_VENDOR, 0x0CE6, true)]);
        assert_eq!(out, vec![ds, xbox]);
    }

    #[test]
    fn a_bluetooth_pad_sdl_has_no_level_for_still_shows() {
        let xbox = pad("sdl-xbox", 40, 0x045E, 0x0B13, false);
        let out = combine(vec![xbox.clone()], vec![pad("bt", 70, 0x2DC8, 0x6012, true)]);
        assert_eq!(out, vec![xbox, pad("bt", 70, 0x2DC8, 0x6012, true)]);
        // unknown ids never match each other
        assert_eq!(combine(vec![pad("a", 1, 0, 0, false)], vec![pad("b", 2, 0, 0, true)]).len(), 2);
    }

    #[test]
    fn a_bluetooth_level_out_of_range_is_ignored() {
        assert_eq!((from_bluetooth(0), from_bluetooth(100), from_bluetooth(101), from_bluetooth(255)), (Some(0), Some(100), None, None));
    }

    #[test]
    fn sdl_states_become_a_level() {
        assert_eq!(from_power(Power::OnBattery, 50), Some((50, false)));
        assert_eq!(from_power(Power::Charging, 33), Some((33, true)));
        // clamped
        assert_eq!(from_power(Power::OnBattery, 140), Some((100, false)));
        assert_eq!(from_power(Power::OnBattery, 0), Some((0, false)));
        // full on the cable, with or without a number
        assert_eq!(from_power(Power::Charged, 100), Some((100, true)));
        assert_eq!(from_power(Power::Charged, -1), Some((100, true)));
    }

    #[test]
    fn a_wired_unknown_or_unreadable_pad_shows_nothing() {
        assert_eq!(from_power(Power::NoBattery, 80), None);
        assert_eq!(from_power(Power::Unknown, 80), None);
        // on battery but SDL can't tell the level
        assert_eq!(from_power(Power::OnBattery, -1), None);
        assert_eq!(from_power(Power::Charging, -1), None);
    }

    #[test]
    fn an_xbox_pad_on_a_cable_shows_nothing_but_a_playstation_one_charging_does() {
        assert_eq!(reading_for(Power::Charged, 100, true, false), None);
        assert_eq!(reading_for(Power::Charging, 40, true, false), None);
        assert_eq!(reading_for(Power::Charging, 40, true, true), Some((40, true)));
        // wireless: always what SDL says
        assert_eq!(reading_for(Power::OnBattery, 40, false, false), Some((40, false)));
    }

    #[test]
    fn header_shows_the_emptiest_pad() {
        assert_eq!(lowest(&[]), None);
        let all = [reading("a", 80), reading("b", 20), reading("c", 55)];
        assert_eq!(lowest(&all).unwrap().id, "b");
        // a tie: the one that is not charging (the one that will run out)
        let mut charging = reading("x", 30);
        charging.charging = true;
        let tie = [charging, reading("y", 30)];
        assert_eq!(lowest(&tie).unwrap().id, "y");
    }

    #[test]
    fn text_for_the_header_and_for_the_report() {
        assert_eq!(format(&reading("a", 65)), "65%");
        assert_eq!(describe(&reading("a", 80)), "battery 80%");
        assert_eq!(describe(&BatteryReading { id: "x".into(), name: "x".into(), percent: 65, charging: true, ..Default::default() }), "battery 65%, charging");
    }

    #[test]
    fn the_icon_follows_the_level() {
        assert_eq!([0, 7, 8, 21, 22, 49, 50, 79, 80, 94, 95, 100].map(icon_step), [0, 0, 1, 1, 2, 3, 4, 5, 6, 6, 7, 7]);
        let mut last = 0;
        for p in 0..=100 { let s = icon_step(p); assert!(s >= last && s <= 7); last = s; }
    }

    #[test]
    fn low_battery_warns_once_at_15_and_once_at_5() {
        let mut alarm = LowBatteryAlarm::new();
        assert_eq!(alarm.update(40, false), None);
        assert_eq!(alarm.update(15, false), Some(15));
        assert_eq!(alarm.update(12, false), None);
        assert_eq!(alarm.update(16, false), None, "a reading that bounces back up doesn't re-arm it");
        assert_eq!(alarm.update(14, false), None);
        assert_eq!(alarm.update(5, false), Some(5));
        assert_eq!(alarm.update(3, false), None);
        assert_eq!(alarm.update(0, false), None);
    }

    #[test]
    fn low_battery_goes_straight_to_critical_when_first_seen_very_low() {
        let mut alarm = LowBatteryAlarm::new();
        assert_eq!(alarm.update(4, false), Some(5));
        assert_eq!(alarm.update(10, false), None);
        assert_eq!(alarm.update(2, false), None);
    }

    #[test]
    fn charging_rearms_the_warning() {
        let mut alarm = LowBatteryAlarm::new();
        assert_eq!(alarm.update(10, false), Some(15));
        assert_eq!(alarm.update(10, true), None);
        assert_eq!(alarm.update(10, false), Some(15));
    }

    #[test]
    fn fresh_batteries_rearm_the_warning() {
        let mut alarm = LowBatteryAlarm::new();
        assert_eq!(alarm.update(15, false), Some(15));
        assert_eq!(alarm.update(100, false), None);
        assert_eq!(alarm.update(15, false), Some(15));
    }
}
