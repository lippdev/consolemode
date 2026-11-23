//! Controllers through SDL3 (Xbox and PlayStation, also while another app has the focus).
//! Runs on its own thread and hands the UI simple presses.

use std::time::{Duration, Instant};

use futures::channel::mpsc;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Pad { Up, Down, Left, Right, A, B, X, Y, LB, RB, Menu, View }

#[derive(Clone, Debug)]
pub enum Input {
    Press(Pad),
    /// The session menu shortcut was held: ☰ for HOLD, or the combo picked in Sistema → Atalhos
    /// (the session menu over a game; a quick press stays the game's own).
    MenuHeld,
    /// Home or Exit shortcut held (see pad_shortcuts).
    Shortcut(cm_core::shortcuts::Slot),
    /// A controller was plugged in (or woke up), as opposed to `Controller`, which also reports removals.
    Connected,
    /// Name of the controller in use ("DualSense", "Controle Xbox"), or None when none is connected.
    Controller(Option<String>),
    /// "Gravar combinação…": the buttons held right now on the pad being recorded (0: none yet).
    CaptureLive(u16),
    /// "Gravar combinação…": the pad let go; the combo it held (the UI checks and saves it).
    Captured(u16),
    /// The battery of every connected pad that has one (every 30 s, and when a pad comes or goes).
    Battery(Vec<cm_core::controller_battery::BatteryReading>),
}

/// How long ☰ is held to call the session menu (1.7: "Segure"), when no other combo was picked.
pub const HOLD: Duration = Duration::from_millis(800);
/// The controller thread keeps its quick pace this long after the last input.
const STAY_QUICK: Duration = Duration::from_secs(3);

/// A connected controller, for the diagnostic report (report.rs).
#[derive(Clone, Debug)]
pub struct PadInfo { pub id: String, pub name: String, pub kind: &'static str, pub vendor: Option<u16>, pub product: Option<u16> }

static PADS: std::sync::Mutex<Vec<PadInfo>> = std::sync::Mutex::new(Vec::new());

/// The controllers connected now.
pub fn connected() -> Vec<PadInfo> { PADS.lock().unwrap_or_else(|e| e.into_inner()).clone() }

fn describe(p: &sdl3::gamepad::Gamepad) -> Option<PadInfo> {
    use sdl3::gamepad::GamepadType as T;
    let kind = match p.r#type() { T::PS3 | T::PS4 | T::PS5 => "PlayStation", T::Xbox360 | T::XboxOne => "Xbox", T::NintendoSwitchPro | T::NintendoSwitchJoyconLeft | T::NintendoSwitchJoyconRight | T::NintendoSwitchJoyconPair => "Nintendo", _ => "Other" };
    Some(PadInfo { id: p.id().ok()?.to_string(), name: friendly_name(p), kind, vendor: p.vendor_id(), product: p.product_id() })
}

/// How often the batteries are read (1.7: every half a minute, never on the input path).
const BATTERY_EVERY: Duration = Duration::from_secs(30);

pub fn spawn(shared: crate::pad_shortcuts::Shared) -> mpsc::UnboundedReceiver<Input> {
    let (tx, rx) = mpsc::unbounded();
    std::thread::Builder::new().name("controllers".into()).spawn(move || {
        let t_init = Instant::now();
        sdl3::hint::set("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
        let Ok(sdl) = sdl3::init() else { eprintln!("SDL init failed"); return };
        let Ok(gc) = sdl.gamepad() else { eprintln!("SDL gamepad failed"); return };
        let Ok(mut pump) = sdl.event_pump() else { return };
        let mut open: Vec<sdl3::gamepad::Gamepad> = Vec::new();
        let debug = std::env::var_os("CM2_DEBUG_INPUT").is_some();
        if debug { eprintln!("[input] SDL ready after {:?}", t_init.elapsed()); }
        // left stick works like the d-pad: a step when it passes 0.6, then repeats while held
        let mut stick: Option<(Pad, Instant, bool)> = None;
        // every pad's buttons and sticks, for the shortcuts, Task View and the pad as a mouse
        let mut pads = crate::pad_shortcuts::Pads::new(shared);
        let send = |i: Input| { let _ = tx.unbounded_send(i); };
        let mut battery_due = Instant::now();
        let mut last_input = Instant::now();
        loop {
            let mut had_event = false;
            for e in pump.poll_iter() {
                // a press, or a stick pushed past its rest (a resting stick may still jitter)
                had_event |= match &e {
                    sdl3::event::Event::GamepadButtonDown { .. } | sdl3::event::Event::GamepadButtonUp { .. } => true,
                    sdl3::event::Event::GamepadAxisMotion { value, .. } => value.unsigned_abs() > 8000,
                    _ => false,
                };
                if debug { eprintln!("[input] {e:?}"); }
                use sdl3::{event::Event, gamepad::{Axis, Button}};
                pads.event(&e);
                match e {
                    Event::GamepadAdded { which, .. } => {
                        if let Ok(p) = gc.open(which) {
                            pads.added(which, crate::pad_shortcuts::is_playstation(&p));
                            send(Input::Controller(Some(friendly_name(&p))));
                            send(Input::Connected);
                            if let Some(info) = describe(&p) { PADS.lock().unwrap_or_else(|e| e.into_inner()).push(info); }
                            open.push(p);
                            // a moment for the pad to report its battery
                            // Windows' Bluetooth level too (1.7.1 #225), read on a thread of its own
                            cm_platform::hid_win::refresh_bluetooth();
                            battery_due = Instant::now() + Duration::from_millis(1500);
                        }
                    }
                    Event::GamepadRemoved { which, .. } => {
                        pads.removed(which);
                        open.retain(|p| p.id().ok() != Some(which));
                        PADS.lock().unwrap_or_else(|e| e.into_inner()).retain(|p| p.id != which.to_string());
                        send(Input::Controller(open.last().map(friendly_name)));
                        send(Input::Battery(read_batteries(&open)));
                    }
                    Event::GamepadButtonDown { button, .. } => {
                        // the Y of a held shortcut (Select + Y) is not a press of the app
                        if button == Button::North && !pads.is_tour_press() { continue; }
                        let p = match button {
                            Button::DPadUp => Pad::Up, Button::DPadDown => Pad::Down, Button::DPadLeft => Pad::Left, Button::DPadRight => Pad::Right,
                            Button::South => Pad::A, Button::East => Pad::B, Button::West => Pad::X, Button::North => Pad::Y,
                            Button::LeftShoulder => Pad::LB, Button::RightShoulder => Pad::RB, Button::Start => Pad::Menu, Button::Back => Pad::View,
                            _ => continue,
                        };
                        send(Input::Press(p));
                    }
                    Event::GamepadAxisMotion { axis, value, .. } if matches!(axis, Axis::LeftX | Axis::LeftY) => {
                        let v = value as f32 / 32767.0;
                        let dir = match (axis, v) {
                            (Axis::LeftX, v) if v > 0.6 => Some(Pad::Right), (Axis::LeftX, v) if v < -0.6 => Some(Pad::Left),
                            (Axis::LeftY, v) if v > 0.6 => Some(Pad::Down), (Axis::LeftY, v) if v < -0.6 => Some(Pad::Up),
                            _ => None,
                        };
                        match (dir, stick) {
                            (Some(d), Some((s, _, _))) if d == s => {}
                            (Some(d), _) => { send(Input::Press(d)); stick = Some((d, Instant::now(), false)); }
                            (None, Some((s, _, _))) if axis_of(s) == axis => stick = None,
                            _ => {}
                        }
                    }
                    _ => {}
                }
            }
            pads.tick(&send);
            if Instant::now() >= battery_due {
                battery_due = Instant::now() + BATTERY_EVERY;
                if !open.is_empty() { send(Input::Battery(read_batteries(&open))); cm_platform::hid_win::refresh_bluetooth(); }
            }
            if let Some((d, t, repeating)) = stick {
                let wait = if repeating { 120 } else { 380 };
                if t.elapsed() > Duration::from_millis(wait) { send(Input::Press(d)); stick = Some((d, Instant::now(), true)); }
            }
            // Every 8 ms while anything is held or moving and for a moment after any input, so
            // navigating stays immediate; every 50 ms at rest, when looking costs the most for
            // the least (a first press after a pause answers within 50 ms).
            if had_event { last_input = Instant::now(); }
            let busy = stick.is_some() || pads.busy() || last_input.elapsed() < STAY_QUICK;
            std::thread::sleep(Duration::from_millis(if busy { 8 } else { 50 }));
        }
    }).expect("controller thread");
    rx
}

fn axis_of(p: Pad) -> sdl3::gamepad::Axis {
    if matches!(p, Pad::Left | Pad::Right) { sdl3::gamepad::Axis::LeftX } else { sdl3::gamepad::Axis::LeftY }
}

fn friendly_name(p: &sdl3::gamepad::Gamepad) -> String {
    use sdl3::gamepad::GamepadType as T;
    match p.r#type() {
        T::PS5 => "DualSense".into(),
        T::PS4 => "DualShock 4".into(),
        T::Xbox360 | T::XboxOne => "Controle Xbox".into(),
        T::NintendoSwitchPro => "Switch Pro".into(),
        _ => p.name().unwrap_or_else(|| "Controle".into()),
    }
}

/// `CM2_FAKE_BATTERY=15` (or `15,charging`): every pad reports that level, to see the indicator and
/// the warning without waiting for a real battery (development).
fn fake_battery() -> Option<(u8, bool)> {
    let v = std::env::var("CM2_FAKE_BATTERY").ok()?;
    let mut parts = v.split(',');
    let percent = parts.next()?.trim().parse::<u8>().ok()?.min(100);
    Some((percent, parts.next().is_some_and(|p| p.trim().starts_with('c'))))
}

/// What SDL says about each open pad's battery (`SDL_GetGamepadPowerInfo`): a wired pad or one whose
/// level is unknown is left out. Windows' Bluetooth level replaces SDL's for the same pad, but
/// for PlayStation pads (cm_core::controller_battery::combine).
fn read_batteries(open: &[sdl3::gamepad::Gamepad]) -> Vec<cm_core::controller_battery::BatteryReading> {
    use cm_core::controller_battery::{BatteryReading, Power, reading_for};
    use sdl3::joystick::PowerLevel as P;
    let fake = fake_battery();
    let sdl = open.iter().filter_map(|p| {
        let id = p.id().ok()?.to_string();
        let (percent, charging) = match fake {
            Some(f) => f,
            None => {
                let info = p.power_info();
                let state = match info.state { P::OnBattery => Power::OnBattery, P::NoBattery => Power::NoBattery, P::Charging => Power::Charging, P::Charged => Power::Charged, P::Unknown | P::Error => Power::Unknown };
                // an Xbox pad is shown only once SDL knows it is wireless (it says "unknown" for a moment after it connects)
                let wired = !matches!(p.connection_state(), Ok(sdl3::joystick::ConnectionState::Wireless));
                reading_for(state, info.percentage, wired, crate::pad_shortcuts::is_playstation(p))?
            }
        };
        Some(BatteryReading { id, name: friendly_name(p), percent, charging, vendor: p.vendor_id().unwrap_or(0), product: p.product_id().unwrap_or(0), bluetooth: false })
    }).collect::<Vec<_>>();
    if fake.is_some() { return sdl; }
    // only pads SDL still has open: a pad just unplugged keeps its cached Bluetooth level until the next read
    let ids: Vec<(u16, u16)> = open.iter().map(|p| (p.vendor_id().unwrap_or(0), p.product_id().unwrap_or(0))).collect();
    let bluetooth = cm_platform::hid_win::bluetooth_cached().into_iter().filter(|b| b.vendor != 0 && ids.contains(&(b.vendor, b.product))).collect();
    cm_core::controller_battery::combine(sdl, bluetooth)
}
