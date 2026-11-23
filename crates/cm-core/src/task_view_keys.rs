//! Turns the controller into the keys Windows' Task View understands (ported from 1.7's
//! TaskViewKeys): the D-pad and the left stick move (a held direction repeats, like a held arrow
//! key), A is Enter and B is Escape. Nothing counts until every button has been let go once:
//! the shortcut that opened Task View is usually still held. Pure, so it is tested.

use std::time::Duration;

use crate::shortcuts::{A, B, DPAD_DOWN, DPAD_LEFT, DPAD_RIGHT, DPAD_UP};

/// A key the controller stands for while Task View is open.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum TaskViewKey {
    Left,
    Right,
    Up,
    Down,
    /// A: opens the window in focus (and Task View closes).
    Enter,
    /// B: closes Task View, back to where you were.
    Escape,
}

/// What the controller does at one moment: XInput bits, and the left stick (-1..1, up positive).
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct TaskViewPad {
    pub buttons: u16,
    pub stick_x: f64,
    pub stick_y: f64,
}

/// How far the stick goes (0 to 1) before it counts as a direction.
pub const STICK_THRESHOLD: f64 = 0.55;
pub const REPEAT_DELAY: Duration = Duration::from_millis(420);
pub const REPEAT_INTERVAL: Duration = Duration::from_millis(140);

#[derive(Default)]
pub struct TaskViewKeys {
    armed: bool,
    direction: Option<TaskViewKey>,
    next_repeat: Duration,
    a_down: bool,
    b_down: bool,
}

impl TaskViewKeys {
    pub fn new() -> Self { Self::default() }

    /// The keys to send for this sample, taken at `now`.
    pub fn step(&mut self, pad: TaskViewPad, now: Duration) -> Vec<TaskViewKey> {
        let mut keys = Vec::new();
        let direction = direction(pad);
        let a = pad.buttons & A != 0;
        let b = pad.buttons & B != 0;

        if !self.armed {
            if pad.buttons == 0 && direction.is_none() { self.armed = true; }
            return keys;
        }

        if direction != self.direction {
            self.direction = direction;
            if let Some(pressed) = direction {
                keys.push(pressed);
                self.next_repeat = now + REPEAT_DELAY;
            }
        } else if let Some(held) = direction {
            if now >= self.next_repeat {
                keys.push(held);
                self.next_repeat = now + REPEAT_INTERVAL;
            }
        }

        // A and B act when pressed, once per press.
        if a && !self.a_down { keys.push(TaskViewKey::Enter); }
        if b && !self.b_down { keys.push(TaskViewKey::Escape); }
        self.a_down = a;
        self.b_down = b;
        keys
    }
}

/// The D-pad first; otherwise the stick, along its stronger axis once it is pushed far enough.
pub fn direction(pad: TaskViewPad) -> Option<TaskViewKey> {
    if pad.buttons & DPAD_LEFT != 0 { return Some(TaskViewKey::Left); }
    if pad.buttons & DPAD_RIGHT != 0 { return Some(TaskViewKey::Right); }
    if pad.buttons & DPAD_UP != 0 { return Some(TaskViewKey::Up); }
    if pad.buttons & DPAD_DOWN != 0 { return Some(TaskViewKey::Down); }
    let (x, y) = (pad.stick_x, pad.stick_y);
    if x.abs().max(y.abs()) < STICK_THRESHOLD { return None; }
    if x.abs() >= y.abs() { return Some(if x < 0. { TaskViewKey::Left } else { TaskViewKey::Right }); }
    // Stick up is positive.
    Some(if y > 0. { TaskViewKey::Up } else { TaskViewKey::Down })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::shortcuts::{BACK, Slot, X, presets, suggested, overlaps};

    const IDLE: TaskViewPad = TaskViewPad { buttons: 0, stick_x: 0., stick_y: 0. };
    fn ms(n: u64) -> Duration { Duration::from_millis(n) }
    fn pad(buttons: u16) -> TaskViewPad { TaskViewPad { buttons, ..IDLE } }

    /// A driver past its first sample with nothing held, ready to act.
    fn armed() -> TaskViewKeys {
        let mut keys = TaskViewKeys::new();
        keys.step(IDLE, ms(0));
        keys
    }

    #[test]
    fn nothing_counts_until_the_shortcut_is_let_go() {
        let mut keys = TaskViewKeys::new();
        // Select + X (the shortcut) still held, then A pressed with it: no Enter yet.
        assert!(keys.step(pad(BACK | X), ms(0)).is_empty());
        assert!(keys.step(pad(BACK | X | A), ms(16)).is_empty());
        // All let go: armed, and only the next press counts.
        assert!(keys.step(IDLE, ms(32)).is_empty());
        assert_eq!(keys.step(pad(A), ms(48)), [TaskViewKey::Enter]);
    }

    #[test]
    fn a_does_enter_and_b_does_escape_once_per_press() {
        let mut keys = armed();
        assert_eq!(keys.step(pad(A), ms(16)), [TaskViewKey::Enter]);
        assert!(keys.step(pad(A), ms(600)).is_empty());
        assert!(keys.step(IDLE, ms(616)).is_empty());
        assert_eq!(keys.step(pad(B), ms(632)), [TaskViewKey::Escape]);
    }

    #[test]
    fn a_held_direction_repeats_like_a_held_key() {
        let mut keys = armed();
        let right = pad(DPAD_RIGHT);
        assert_eq!(keys.step(right, ms(100)), [TaskViewKey::Right]);
        // Nothing more before the repeat delay...
        assert!(keys.step(right, ms(100 + 400)).is_empty());
        // ...then one per interval.
        assert_eq!(keys.step(right, ms(100 + 420)), [TaskViewKey::Right]);
        assert!(keys.step(right, ms(100 + 500)).is_empty());
        assert_eq!(keys.step(right, ms(100 + 560)), [TaskViewKey::Right]);
    }

    #[test]
    fn changing_direction_acts_at_once() {
        let mut keys = armed();
        assert_eq!(keys.step(pad(DPAD_LEFT), ms(16)), [TaskViewKey::Left]);
        assert_eq!(keys.step(pad(DPAD_DOWN), ms(32)), [TaskViewKey::Down]);
        assert!(keys.step(IDLE, ms(48)).is_empty());
        assert_eq!(keys.step(pad(DPAD_DOWN), ms(64)), [TaskViewKey::Down]);
    }

    #[test]
    fn the_stick_moves_along_its_stronger_axis() {
        for (x, y, expected) in [
            (-0.9, 0.1, TaskViewKey::Left),
            (0.8, -0.3, TaskViewKey::Right),
            (0.2, 0.9, TaskViewKey::Up),
            (-0.1, -0.7, TaskViewKey::Down),
        ] {
            assert_eq!(direction(TaskViewPad { buttons: 0, stick_x: x, stick_y: y }), Some(expected), "{x} {y}");
        }
    }

    #[test]
    fn a_small_stick_nudge_is_no_direction() {
        assert_eq!(direction(TaskViewPad { buttons: 0, stick_x: 0.3, stick_y: -0.4 }), None);
    }

    #[test]
    fn the_dpad_wins_over_the_stick() {
        assert_eq!(direction(TaskViewPad { buttons: DPAD_UP, stick_x: 0.9, stick_y: 0. }), Some(TaskViewKey::Up));
    }

    #[test]
    fn the_task_view_shortcut_is_suggested_and_free_of_the_others() {
        let s = suggested(Slot::TaskView);
        assert!(crate::shortcuts::is_acceptable(s));
        for other in [Slot::Home, Slot::Menu, Slot::Exit] {
            assert!(!overlaps(s, suggested(other)), "{other:?}");
            assert!(presets(other).contains(&suggested(other)));
        }
    }
}
