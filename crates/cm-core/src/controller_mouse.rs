//! The controller as a mouse (ported from 1.7's ControllerMouseMath): the left stick moves the
//! pointer, the right stick scrolls, A and B are the buttons. Pure, so it is tested.

use crate::config::Config;

/// Radial dead zone of the left stick (pointer); worn sticks rest a little off center.
pub const MOVE_DEAD_ZONE: f64 = 0.18;
/// The right stick (wheel) needs a clearer push, so aiming with the left one never scrolls by accident.
pub const SCROLL_DEAD_ZONE: f64 = 0.3;
/// Full tilt crosses about this many screen heights per second, so a 4K TV feels like a 1080p one.
pub const SCREENS_PER_SECOND: f64 = 1.1;
pub const MIN_NOTCHES_PER_SECOND: f64 = 3.;
pub const MAX_NOTCHES_PER_SECOND: f64 = 16.;

/// The "Velocidade" choices of Sistema → Controles, as the config keeps them.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Speed { Slow, Medium, Fast }

impl Speed {
    pub fn from_config(s: &str) -> Self { match s { "slow" => Speed::Slow, "fast" => Speed::Fast, _ => Speed::Medium } }
    pub fn key(self) -> &'static str { match self { Speed::Slow => "slow", Speed::Medium => "medium", Speed::Fast => "fast" } }
    /// Times the 1.7 speed (`Medium` is exactly 1.7's).
    pub fn factor(self) -> f64 { match self { Speed::Slow => 0.55, Speed::Medium => 1., Speed::Fast => 1.7 } }
}

impl Config {
    pub fn pointer_speed(&self) -> Speed { Speed::from_config(&self.controller_mouse_speed) }
}

/// XInput stick value (-32768..32767, Y up) to -1..1.
pub fn from_xinput(value: i16) -> f64 { (value as f64 / 32767.).max(-1.) }

/// Sony HID stick byte (0..255, 128 = center, Y down) to -1..1 with Y up like XInput.
pub fn from_hid_byte(value: u8, invert: bool) -> f64 {
    ((value as f64 - 128.) / 127.).clamp(-1., 1.) * if invert { -1. } else { 1. }
}

/// Radial dead zone, then a squared curve: small tilts aim precisely, full tilt travels.
/// Input and output in -1..1; the direction is kept.
pub fn curve(x: f64, y: f64, dead_zone: f64) -> (f64, f64) {
    let magnitude = (x * x + y * y).sqrt();
    if magnitude <= dead_zone { return (0., 0.); }
    let scaled = ((magnitude - dead_zone) / (1. - dead_zone)).min(1.);
    let factor = scaled * scaled / magnitude;
    (x * factor, y * factor)
}

/// Left stick to pointer pixels, carrying the fractions between samples so slow moves don't stall.
#[derive(Default)]
pub struct PointerMotion {
    rest_x: f64,
    rest_y: f64,
}

impl PointerMotion {
    pub fn new() -> Self { Self::default() }

    /// `x`, `y`: stick, -1..1, up positive (the screen's Y grows downward). `seconds`: time since the
    /// previous sample. `screen_height`: height of the screen in pixels.
    pub fn step(&mut self, x: f64, y: f64, seconds: f64, screen_height: i32) -> (i32, i32) {
        self.step_scaled(x, y, seconds, screen_height, 1.)
    }

    /// Like [`step`](Self::step), `speed` times as fast (the "Velocidade" setting).
    pub fn step_scaled(&mut self, x: f64, y: f64, seconds: f64, screen_height: i32, speed: f64) -> (i32, i32) {
        let (vx, vy) = curve(x, y, MOVE_DEAD_ZONE);
        if vx == 0. && vy == 0. {
            self.reset();
            return (0, 0);
        }
        let pixels_per_second = screen_height.max(720) as f64 * SCREENS_PER_SECOND * speed;
        self.rest_x += vx * pixels_per_second * seconds;
        self.rest_y -= vy * pixels_per_second * seconds;
        let (dx, dy) = (self.rest_x.trunc(), self.rest_y.trunc());
        self.rest_x -= dx;
        self.rest_y -= dy;
        (dx as i32, dy as i32)
    }

    pub fn reset(&mut self) { self.rest_x = 0.; self.rest_y = 0.; }
}

/// One right-stick axis to whole wheel notches (positive = up / right). Whole notches only, since some
/// programs ignore partial ones; the first notch goes out as soon as the stick leaves the dead zone.
#[derive(Default)]
pub struct WheelMotion {
    pending: f64,
    moving: bool,
}

impl WheelMotion {
    pub fn new() -> Self { Self::default() }

    pub fn step(&mut self, axis: f64, seconds: f64) -> i32 {
        let magnitude = axis.abs();
        if magnitude <= SCROLL_DEAD_ZONE {
            self.reset();
            return 0;
        }
        let scaled = ((magnitude - SCROLL_DEAD_ZONE) / (1. - SCROLL_DEAD_ZONE)).min(1.);
        let rate = MIN_NOTCHES_PER_SECOND + (MAX_NOTCHES_PER_SECOND - MIN_NOTCHES_PER_SECOND) * scaled * scaled;
        if !self.moving {
            self.moving = true;
            self.pending = 1.;
        } else {
            self.pending += rate * seconds;
        }
        let notches = self.pending.trunc();
        self.pending -= notches;
        notches as i32 * if axis > 0. { 1 } else { -1 }
    }

    pub fn reset(&mut self) {
        self.pending = 0.;
        self.moving = false;
    }
}

/// A pad button as a mouse button: down when pressed, up when released (so a click, a double
/// click and a drag all work as with a real mouse). A button already held when the mouse mode
/// starts (the A that closed the app) is ignored until it is let go.
pub struct MouseButton {
    waiting_release: bool,
    down: bool,
}

impl MouseButton {
    pub fn new() -> Self { Self { waiting_release: true, down: false } }

    /// One sample. `Some(true)`: press the button; `Some(false)`: release it.
    pub fn update(&mut self, held: bool) -> Option<bool> {
        if self.waiting_release {
            if !held { self.waiting_release = false; }
            return None;
        }
        if held == self.down { return None; }
        self.down = held;
        Some(held)
    }

    /// Starts over, ignoring the button until it is let go. True when the mouse button is still
    /// down and the caller must release it.
    pub fn reset(&mut self) -> bool {
        let was_down = self.down;
        self.down = false;
        self.waiting_release = true;
        was_down
    }
}

impl Default for MouseButton {
    fn default() -> Self { Self::new() }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn close(a: f64, b: f64, eps: f64) -> bool { (a - b).abs() < eps }

    #[test]
    fn stick_inside_the_dead_zone_does_not_move() {
        for (x, y) in [(0., 0.), (0.1, 0.1), (0.17, 0.)] {
            assert_eq!(curve(x, y, MOVE_DEAD_ZONE), (0., 0.), "{x} {y}");
        }
    }

    #[test]
    fn curve_keeps_the_direction_and_reaches_full_speed() {
        let (x, y) = curve(0., -1., MOVE_DEAD_ZONE);
        assert!(close(x, 0., 1e-6) && close(y, -1., 1e-6));
        let (dx, dy) = curve(0.5, 0.5, MOVE_DEAD_ZONE);
        assert!(close(dx, dy, 1e-6));
    }

    #[test]
    fn half_tilt_is_much_slower_than_full_tilt() {
        let (half, _) = curve(0.5, 0., MOVE_DEAD_ZONE);
        let (full, _) = curve(1., 0., MOVE_DEAD_ZONE);
        assert!(half < full / 3., "half={half} full={full}");
    }

    #[test]
    fn xinput_sticks_are_normalized() {
        for (value, expected) in [(32767, 1.), (-32768, -1.), (0, 0.)] {
            assert!(close(from_xinput(value), expected, 1e-3), "{value}");
        }
    }

    #[test]
    fn hid_sticks_are_normalized() {
        // Sony Y grows downward: pushed up reads 0
        for (value, invert, expected) in [(128, false, 0.), (255, false, 1.), (0, false, -1.), (0, true, 1.)] {
            assert!(close(from_hid_byte(value, invert), expected, 1e-2), "{value} {invert}");
        }
    }

    #[test]
    fn pointer_moves_right_and_up_on_screen() {
        let (dx, dy) = PointerMotion::new().step(1., 1., 0.1, 1080);
        assert!(dx > 0);
        assert!(dy < 0, "stick up must move the pointer up (screen Y decreases)");
    }

    #[test]
    fn pointer_speed_follows_the_screen_height() {
        let hd = PointerMotion::new().step(1., 0., 0.1, 1080).0;
        let uhd = PointerMotion::new().step(1., 0., 0.1, 2160).0;
        assert!((hd * 2 - 1..=hd * 2 + 1).contains(&uhd), "hd={hd} uhd={uhd}");
    }

    #[test]
    fn the_speed_setting_scales_the_pointer() {
        let medium = PointerMotion::new().step_scaled(1., 0., 0.1, 1080, Speed::Medium.factor()).0;
        let same = PointerMotion::new().step(1., 0., 0.1, 1080).0;
        let slow = PointerMotion::new().step_scaled(1., 0., 0.1, 1080, Speed::Slow.factor()).0;
        let fast = PointerMotion::new().step_scaled(1., 0., 0.1, 1080, Speed::Fast.factor()).0;
        assert_eq!(medium, same, "medium is 1.7's speed");
        assert!(slow < medium && medium < fast, "{slow} {medium} {fast}");
    }

    #[test]
    fn the_speed_is_read_from_the_config() {
        assert_eq!(Speed::from_config("fast"), Speed::Fast);
        assert_eq!(Speed::from_config("slow"), Speed::Slow);
        assert_eq!(Speed::from_config("medium"), Speed::Medium);
        assert_eq!(Speed::from_config("junk"), Speed::Medium);
        for s in [Speed::Slow, Speed::Medium, Speed::Fast] { assert_eq!(Speed::from_config(s.key()), s); }
        let mut c = Config::default();
        assert_eq!(c.pointer_speed(), Speed::Medium);
        c.controller_mouse_speed = "fast".into();
        assert_eq!(c.pointer_speed(), Speed::Fast);
    }

    #[test]
    fn slow_moves_accumulate_instead_of_stalling() {
        let mut motion = PointerMotion::new();
        let total: i32 = (0..100).map(|_| motion.step(0.25, 0., 0.008, 1080).0).sum();
        assert!(total > 0, "a slight tilt must still move the pointer");
        assert_eq!(PointerMotion::new().step(0.25, 0., 0.008, 1080).0, 0, "less than a pixel in one sample");
    }

    #[test]
    fn wheel_sends_a_notch_at_once_then_by_rate() {
        let mut wheel = WheelMotion::new();
        assert_eq!(wheel.step(0.6, 0.008), 1);
        let total: i32 = (0..125).map(|_| wheel.step(1., 0.008)).sum(); // one second at full tilt
        let max = MAX_NOTCHES_PER_SECOND as i32;
        assert!((max - 1..=max + 1).contains(&total), "{total}");
    }

    #[test]
    fn wheel_down_is_negative_and_the_dead_zone_resets_it() {
        let mut wheel = WheelMotion::new();
        assert_eq!(wheel.step(-0.9, 0.008), -1);
        assert_eq!(wheel.step(0.1, 0.008), 0);
        assert_eq!(wheel.step(0.9, 0.008), 1, "back out of the dead zone: a fresh first notch");
    }

    #[test]
    fn a_button_is_down_while_held_and_up_when_let_go() {
        let mut b = MouseButton::new();
        assert_eq!(b.update(false), None); // armed
        assert_eq!(b.update(true), Some(true));
        assert_eq!(b.update(true), None);
        assert_eq!(b.update(false), Some(false));
        assert_eq!(b.update(false), None);
    }

    #[test]
    fn a_button_held_when_the_mode_starts_is_ignored_until_released() {
        let mut b = MouseButton::new();
        assert_eq!(b.update(true), None);
        assert_eq!(b.update(true), None);
        assert_eq!(b.update(false), None);
        assert_eq!(b.update(true), Some(true));
    }

    #[test]
    fn reset_mid_drag_asks_for_the_button_to_be_released() {
        let mut b = MouseButton::new();
        b.update(false);
        b.update(true);
        assert!(b.reset());
        assert!(!b.reset());
        assert_eq!(b.update(true), None, "still waiting for the button to go up");
        assert_eq!(b.update(false), None);
        assert_eq!(b.update(true), Some(true));
    }
}
