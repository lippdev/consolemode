//! Controller input on a plain thread (gilrs), forwarded to the UI thread as Pad presses.

use std::time::{Duration, Instant};

use futures::channel::mpsc;
use gilrs::{Axis, Button, EventType, Gilrs};

use crate::log;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Pad { Up, Down, Left, Right, A, B, X, Y, LB, RB, Menu, View, Guide }

pub fn spawn() -> mpsc::UnboundedReceiver<Pad> {
    let (tx, rx) = mpsc::unbounded();
    std::thread::Builder::new().name("pad".into()).spawn(move || {
        let mut g = match Gilrs::new() {
            Ok(g) => g,
            Err(e) => { log(format!("gilrs failed: {e}")); return; }
        };
        for (id, pad) in g.gamepads() { log(format!("pad present: {id} \"{}\" ({:?})", pad.name(), pad.power_info())); }
        // left stick acts like the d-pad: one step when it crosses 0.6, then repeats while held
        let mut stick: Option<(Pad, Instant)> = None;
        loop {
            while let Some(ev) = g.next_event_blocking(Some(Duration::from_millis(16))) {
                let send = |p: Pad| { let _ = tx.unbounded_send(p); };
                match ev.event {
                    EventType::Connected => log(format!("pad connected: {} \"{}\"", ev.id, g.gamepad(ev.id).name())),
                    EventType::Disconnected => log(format!("pad disconnected: {}", ev.id)),
                    EventType::ButtonPressed(b, _) => {
                        log(format!("button {b:?} from {}", g.gamepad(ev.id).name()));
                        let p = match b {
                            Button::DPadUp => Pad::Up, Button::DPadDown => Pad::Down, Button::DPadLeft => Pad::Left, Button::DPadRight => Pad::Right,
                            Button::South => Pad::A, Button::East => Pad::B, Button::West => Pad::X, Button::North => Pad::Y,
                            Button::LeftTrigger => Pad::LB, Button::RightTrigger => Pad::RB,
                            Button::Start => Pad::Menu, Button::Select => Pad::View, Button::Mode => Pad::Guide,
                            _ => continue,
                        };
                        send(p);
                    }
                    EventType::AxisChanged(Axis::LeftStickX, v, _) => {
                        let dir = if v > 0.6 { Some(Pad::Right) } else if v < -0.6 { Some(Pad::Left) } else { None };
                        match (dir, stick) {
                            (Some(d), Some((s, _))) if d == s => {}
                            (Some(d), _) => { send(d); stick = Some((d, Instant::now())); }
                            (None, _) => stick = None,
                        }
                    }
                    _ => {}
                }
            }
            if let Some((d, t)) = stick { if t.elapsed() > Duration::from_millis(350) { let _ = tx.unbounded_send(d); stick = Some((d, Instant::now() - Duration::from_millis(230))); } }
        }
    }).unwrap();
    rx
}
