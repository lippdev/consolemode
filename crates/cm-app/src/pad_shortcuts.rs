//! What the controller does besides driving the app (1.7's ControllerShortcuts, ControllerHoldWatcher,
//! TaskViewDriver, ControllerMouse and ControllerConnectWatcher):
//!
//! * shortcuts that work with the app in the tray or behind other windows: Home brings it to the
//!   front, Menu opens the session menu, Exit ends the session, Task View opens Windows' Task View;
//! * a PlayStation pad drives Task View while it is open;
//! * the pad as a mouse, when the app is not in front;
//! * a controller connecting while the app sits in the tray brings it up.
//!
//! `Pads` lives on the SDL thread (input.rs) and sees every pad's buttons and sticks; the pure rules
//! are in cm-core (shortcuts, task_view_keys, controller_mouse, controller_connect). `attach` keeps
//! the thread's `State` in line with the config and the session, and `on_event` applies what the
//! thread reports on the UI side.

use std::{
    collections::HashMap,
    sync::{Arc, Mutex},
    time::{Duration, Instant},
};

use cm_core::{
    controller_connect::{self, PadButton, RESTORE_GRACE},
    controller_mouse::{self, MouseButton, PointerMotion, Speed, WheelMotion},
    shortcuts::{self, A, B, HoldTracker, Slot},
    task_view_keys::{TaskViewKey, TaskViewKeys, TaskViewPad},
};
use cm_platform::pad_win;
use gpui::{AnyWindowHandle, App, AppContext, AsyncApp, Entity};
use sdl3::joystick::JoystickId;

use crate::{home::Console, i18n::{n, t, tf}, input::Input, settings::Value};

// ───────────── what the settings rows show ─────────────

pub const HOME_ROW: &str = n("Abrir o Console Mode");
pub const MENU_ROW: &str = n("Menu da sessão");
pub const EXIT_ROW: &str = n("Voltar ao PC");
pub const TASK_VIEW_ROW: &str = n("Visão de tarefas");
pub const TASK_VIEW_PS_ROW: &str = n("Visão de tarefas no controle PlayStation");
pub const AUTO_START_ROW: &str = n("Entrar ao conectar o controle");
pub const MOUSE_ROW: &str = n("Usar o controle como mouse");
pub const SPEED_ROW: &str = n("Velocidade");
const OFF: &str = n("Desligado");
const MENU_DEFAULT: &str = n("Segurar ☰ (padrão)");
/// The last choice of every shortcut row: records the next combo held on the controller.
pub const RECORD: &str = n("Gravar combinação…");

pub fn slot_of_title(title: &str) -> Option<Slot> {
    Some(match title {
        HOME_ROW => Slot::Home,
        MENU_ROW => Slot::Menu,
        EXIT_ROW => Slot::Exit,
        TASK_VIEW_ROW => Slot::TaskView,
        _ => return None,
    })
}

/// Combo names are built once and live as long as the app: the rows hold `&'static str`.
static LABELS: Mutex<Vec<(&'static str, u16)>> = Mutex::new(Vec::new());

/// "Select + Y (Create + △)": the combo on an Xbox pad and on a PlayStation one.
pub fn label(slot: Slot, mask: u16) -> &'static str {
    if mask == 0 { return if slot == Slot::Menu { MENU_DEFAULT } else { OFF }; }
    let text = if mask == shortcuts::GUIDE { n("Botão Xbox / PS").to_string() } else { format!("{} ({})", shortcuts::format(mask, false), shortcuts::format(mask, true)) };
    let mut labels = LABELS.lock().unwrap();
    if let Some((l, _)) = labels.iter().find(|(l, _)| *l == text) { return l; }
    let l: &'static str = Box::leak(text.into_boxed_str());
    labels.push((l, mask));
    l
}

/// The mask a row label stands for (0 = off, and for the menu, the default ☰ hold).
pub fn mask_of(label: &str) -> u16 { LABELS.lock().unwrap().iter().find(|(l, _)| *l == label).map(|(_, m)| *m).unwrap_or(0) }

/// The row's choices: off first (for the menu: the default hold of ☰), then the 1.7 presets, then
/// "Gravar combinação…" to record any other combo from the controller.
pub fn choices(slot: Slot) -> Vec<&'static str> {
    std::iter::once(0).chain(shortcuts::presets(slot)).map(|m| label(slot, m)).chain(std::iter::once(RECORD)).collect()
}

/// The choice that starts a recording rather than picking a combo.
pub fn is_record(choice: &str) -> bool { choice == RECORD }

/// The saved shortcuts as the settings see them.
pub fn current_masks(c: &cm_core::config::Config) -> [u16; 4] { shortcuts::masks(c) }

/// A shortcut row's choice that another shortcut already has (shown, but can't be picked).
pub fn blocked(title: &str, choice: &str, masks: &[u16; 4]) -> bool {
    slot_of_title(title).is_some_and(|slot| shortcuts::find_conflict(slot, mask_of(choice), masks).is_some())
}

/// Puts a shortcut row on the saved combo (an unusual one from a hand-edited or 1.7 file is added as a choice).
pub fn load_row(title: &str, value: &mut Value, masks: &[u16; 4]) -> bool {
    let (Some(slot), Value::Choice(options, at)) = (slot_of_title(title), value) else { return false };
    let wanted = label(slot, masks[slot.index()]);
    // a recorded combo (or one from a hand-edited or 1.7 file) goes in just before "Gravar combinação…"
    *at = options.iter().position(|o| *o == wanted).unwrap_or_else(|| {
        let k = options.iter().position(|o| is_record(o)).unwrap_or(options.len());
        options.insert(k, wanted);
        k
    });
    true
}

/// Writes a shortcut row into the config. False when the row is not a shortcut.
pub fn store_row(title: &str, value: &Value, c: &mut cm_core::config::Config) -> bool {
    let (Some(slot), Value::Choice(options, at)) = (slot_of_title(title), value) else { return false };
    if is_record(options[*at]) { return true; }
    let mask = mask_of(options[*at]);
    if shortcuts::find_conflict(slot, mask, &shortcuts::masks(c)).is_none() { c.set_shortcut(slot, mask); }
    true
}

pub fn speed_label(s: Speed) -> &'static str { match s { Speed::Slow => n("Lenta"), Speed::Medium => n("Média"), Speed::Fast => n("Rápida") } }
pub fn speed_of_label(l: &str) -> Speed { match l { "Lenta" => Speed::Slow, "Rápida" => Speed::Fast, _ => Speed::Medium } }

// ───────────── shared between the controller thread and the UI ─────────────

/// Both sides read and write this; it is small and copied out under the lock.
#[derive(Clone, Copy)]
pub struct State {
    // from the config
    masks: [u16; 4],
    task_view_playstation: bool,
    mouse_on: bool,
    speed: Speed,
    auto_start: bool,
    // from the app
    /// The session menu's "Visão de tarefas" asked for Task View: the controller thread opens it (and, with a
    /// PlayStation pad, steers it) like the global shortcut does.
    task_view_request: bool,
    in_session: bool,
    /// Sistema → Atalhos is recording a combo: no shortcut fires and the pad's presses don't reach the app.
    capturing: bool,
    window_visible: bool,
    /// On the clock of `Hub::started`: a pad connecting before this is ignored (a session just ended).
    quiet_until: Duration,
    // from the controller thread
    mouse_active: bool,
    task_view_active: bool,
}

pub struct Hub {
    state: Mutex<State>,
    started: Instant,
}

pub type Shared = Arc<Hub>;

pub fn shared() -> Shared {
    Arc::new(Hub {
        state: Mutex::new(State {
            masks: [0, 0, 0, shortcuts::BACK | shortcuts::X],
            task_view_playstation: true,
            mouse_on: false,
            speed: Speed::Medium,
            auto_start: false,
            task_view_request: false,
            capturing: false,
            in_session: false,
            window_visible: true,
            quiet_until: Duration::ZERO,
            mouse_active: false,
            task_view_active: false,
        }),
        started: Instant::now(),
    })
}

impl Hub {
    fn get(&self) -> State { *self.state.lock().unwrap() }
    fn with(&self, f: impl FnOnce(&mut State)) { f(&mut self.state.lock().unwrap()); }

    /// The session menu's "Visão de tarefas": opens Task View through the same driver as the global
    /// shortcut, so a PlayStation pad steers it (see `Pads::tick`).
    pub fn request_task_view(&self) { self.with(|s| s.task_view_request = true); }

    /// Starts or stops recording a combo (the controller thread reports `Input::CaptureLive` and `Input::Captured`).
    pub fn set_capturing(&self, on: bool) { self.with(|s| s.capturing = on); }

    /// True once per request.
    fn take_task_view_request(&self) -> bool { let mut taken = false; self.with(|s| taken = std::mem::take(&mut s.task_view_request)); taken }
}

/// The pad's presses must not reach the app: it is hidden in the tray, or the pad is busy being a
/// mouse or driving Task View.
pub fn swallow_press(shared: &Shared, in_session: bool) -> bool {
    let s = shared.get();
    s.capturing || s.mouse_active || s.task_view_active || (!in_session && !s.window_visible)
}

/// UI side: keeps the controller thread's `State` in line with the config and the session, and
/// starts the quiet time after a session (wireless pads reconnect on their own).
pub fn attach(console: Entity<Console>, shared: Shared, cx: &mut App) {
    cx.spawn(async move |cx| {
        let mut was_running = false;
        loop {
            cx.background_executor().timer(Duration::from_millis(500)).await;
            // what the controller thread needs from the config, without copying all of it
            let (masks, tv_ps, mouse_on, speed, auto_start, running, window) = console.read_with(cx, |c, _| (
                shortcuts::masks(&c.config), c.config.task_view_playstation, c.config.controller_mouse, c.config.pointer_speed(),
                c.config.auto_start_on_controller, c.running.is_some(), c.window,
            ));
            let visible = window.and_then(|w| cx.update_window(w, |_, win, _| pad_win::window_visible(win)).ok()).unwrap_or(true);
            let now = shared.started.elapsed();
            shared.with(|s| {
                s.masks = masks;
                s.task_view_playstation = tv_ps;
                s.mouse_on = mouse_on;
                s.speed = speed;
                s.auto_start = auto_start;
                s.in_session = running;
                s.window_visible = visible;
                if was_running && !running { s.quiet_until = now + RESTORE_GRACE; }
            });
            was_running = running;
        }
    })
    .detach();
}

/// UI side: applies what the controller thread reports.
pub fn on_event(c: &Entity<Console>, window: Option<AnyWindowHandle>, shared: &Shared, ev: &Input, cx: &mut AsyncApp) {
    match ev {
        Input::Shortcut(Slot::Home) => {
            // Already in front: nothing to do. Otherwise out of the tray, or from behind the other windows.
            if shared.get().in_session || pad_win::app_is_foreground() { return; }
            crate::startup_log("Controle: atalho Home (mostrar o app)");
            show(c, window, cx);
        }
        Input::Shortcut(Slot::Exit) => {
            crate::startup_log("Controle: atalho Voltar ao PC");
            c.update(cx, |con, cx| con.return_to_pc(cx));
        }
        Input::Connected => {
            let s = shared.get();
            let now = shared.started.elapsed();
            // Only while the app sits in the tray: a pad plugged in with the window open is someone using the PC.
            let can = s.auto_start && !s.in_session && !s.window_visible;
            let go = controller_connect::should_trigger(now, Duration::ZERO, s.quiet_until, can);
            crate::startup_log(&format!("Controle conectado; entrar no modo console: {}", if go { "sim" } else { "não" }));
            if go { show(c, window, cx); }
        }
        _ => {}
    }
}

fn show(c: &Entity<Console>, window: Option<AnyWindowHandle>, cx: &mut AsyncApp) {
    c.update(cx, |con, cx| {
        con.app_in_front = true;
        cx.notify();
    });
    if let Some(w) = window {
        let _ = cx.update_window(w, |_, win, _| cm_platform::shell_win::show_window(win));
    }
}

// ───────────── "Gravar combinação…" (1.7 ShortcutPicker) ─────────────

/// A combo being recorded for a shortcut, and the buttons held right now (0: none yet).
pub struct Recording { pub slot: Slot, pub live: u16 }

/// The row title of a shortcut (how the "already used by" message names it).
fn title_of(slot: Slot) -> &'static str { match slot { Slot::Home => HOME_ROW, Slot::Menu => MENU_ROW, Slot::Exit => EXIT_ROW, Slot::TaskView => TASK_VIEW_ROW } }

/// "Select + Y (Create + △)", for the live text and the messages.
pub fn both_pads(mask: u16) -> String { format!("{} ({})", shortcuts::format(mask, false), shortcuts::format(mask, true)) }

impl Console {
    /// "Gravar combinação…" picked on a shortcut row: the next combo held on any pad is recorded.
    pub(crate) fn start_recording(&mut self, title: &str, cx: &mut gpui::Context<Self>) {
        let Some(slot) = slot_of_title(title) else { return };
        let Some(hub) = &self.pads else { return self.show_toast(t("Nenhum controle disponível para gravar a combinação."), cx) };
        hub.set_capturing(true);
        self.shortcut_capture = Some(Recording { slot, live: 0 });
        crate::startup_log(&format!("Atalhos: gravando uma combinação para \"{title}\""));
        cx.notify();
    }

    /// B on the keyboard (or the button on screen): stop without saving.
    pub(crate) fn cancel_recording(&mut self, cx: &mut gpui::Context<Self>) {
        if self.shortcut_capture.take().is_none() { return; }
        if let Some(hub) = &self.pads { hub.set_capturing(false); }
        self.sounds.play(crate::sound::Sound::Back);
        crate::startup_log("Atalhos: gravação cancelada");
        cx.notify();
    }

    pub(crate) fn capture_live(&mut self, mask: u16, cx: &mut gpui::Context<Self>) {
        if let Some(r) = &mut self.shortcut_capture { r.live = mask; cx.notify(); }
    }

    /// The pad let go: the combo is checked like 1.7 (a lone B cancels; one face button, or a combo
    /// another shortcut has, is refused with the reason) and saved in the config's 1.7 keys.
    pub(crate) fn capture_done(&mut self, mask: u16, cx: &mut gpui::Context<Self>) {
        let Some(rec) = self.shortcut_capture.take() else { return };
        match shortcuts::check_capture(rec.slot, mask, &current_masks(&self.config)) {
            Ok(mask) => {
                self.config.set_shortcut(rec.slot, mask);
                self.save_config();
                let masks = current_masks(&self.config);
                for cat in self.system.iter_mut() {
                    for s in cat.sections.iter_mut() { for r in s.rows.iter_mut() { load_row(r.title, &mut r.value, &masks); } }
                }
                self.sounds.play(crate::sound::Sound::Confirm);
                crate::startup_log(&format!("Atalhos: \"{}\" gravado como {}", title_of(rec.slot), shortcuts::format(mask, false)));
            }
            Err(shortcuts::CaptureError::Cancelled) => { self.sounds.play(crate::sound::Sound::Back); crate::startup_log("Atalhos: gravação cancelada (B)"); }
            Err(shortcuts::CaptureError::NotAllowed) => {
                self.sounds.play(crate::sound::Sound::Back);
                self.show_toast(t("Use dois ou mais botões juntos (como Select + Y), ou só o botão Xbox / PS. Um botão sozinho dispararia no meio do jogo."), cx);
            }
            Err(shortcuts::CaptureError::InUse(other)) => {
                self.sounds.play(crate::sound::Sound::Back);
                self.show_toast(&tf("{0} já é o atalho de \"{1}\". Escolha outra combinação.", &[&both_pads(mask), &t(title_of(other))]), cx);
            }
        }
        cx.notify();
    }

    /// The card over Sistema while recording: what to do, and the buttons held right now.
    pub(crate) fn recording_view(&self, ui: &cm_ui::Ui, cx: &mut gpui::Context<Self>) -> Option<gpui::AnyElement> {
        use cm_ui::{color, glyph};
        use gpui::{FontWeight, InteractiveElement, IntoElement, ParentElement, SharedString, StatefulInteractiveElement, Styled, div};
        let rec = self.shortcut_capture.as_ref()?;
        let live = if rec.live == 0 { t("Segure os botões e depois solte…").to_string() } else { both_pads(rec.live) };
        let card = div().id("recording").occlude().w(ui.u(760.)).px(ui.u(40.)).py(ui.u(34.)).rounded(ui.u(22.)).bg(color::panel_solid())
            .border(ui.u(1.)).border_color(color::white(0.14)).shadow(ui.raised(ui.lift_lg(), 0.14)).flex().flex_col().items_center().gap(ui.u(18.))
            .child(div().flex().items_center().gap(ui.u(14.)).text_size(ui.u(30.)).font_weight(FontWeight::SEMIBOLD)
                .child(ui.icon(glyph::FIBER_RECORD, 30., color::accent())).child(t(title_of(rec.slot))))
            .child(div().min_h(ui.u(60.)).flex().items_center().text_size(ui.u(36.)).font_weight(FontWeight::SEMIBOLD)
                .text_color(if rec.live == 0 { color::mute() } else { color::ink() }).child(SharedString::from(live)))
            .child(div().text_size(ui.u(21.)).text_color(color::mute()).child(tf("Solte para salvar. Aperte {0} sozinho para cancelar.", &[&"B (○)"])))
            .child(div().id("recording-cancel").mt(ui.u(6.)).px(ui.u(28.)).py(ui.u(12.)).rounded_full().bg(color::white(0.1)).cursor_pointer()
                .hover(|s| s.bg(color::white(0.18))).text_size(ui.u(22.)).child(t("Cancelar"))
                .on_click(cx.listener(|this, _, _, cx| this.cancel_recording(cx))));
        Some(div().absolute().top_0().left_0().size_full().bg(color::shade(0.6)).flex().items_center().justify_center().child(card).into_any_element())
    }
}

// ───────────── the controller thread ─────────────

#[derive(Default)]
struct Pad {
    buttons: u16,
    /// Sticks in -1..1, up positive.
    left: (f64, f64),
    right: (f64, f64),
    playstation: bool,
}

pub fn is_playstation(p: &sdl3::gamepad::Gamepad) -> bool {
    use sdl3::gamepad::GamepadType as T;
    matches!(p.r#type(), T::PS3 | T::PS4 | T::PS5)
}

fn pad_button(b: sdl3::gamepad::Button) -> Option<PadButton> {
    use sdl3::gamepad::Button as B;
    Some(match b {
        B::South => PadButton::South,
        B::East => PadButton::East,
        B::West => PadButton::West,
        B::North => PadButton::North,
        B::Back => PadButton::Back,
        B::Start => PadButton::Start,
        B::Guide => PadButton::Guide,
        B::LeftShoulder => PadButton::LeftShoulder,
        B::RightShoulder => PadButton::RightShoulder,
        B::DPadUp => PadButton::DpadUp,
        B::DPadDown => PadButton::DpadDown,
        B::DPadLeft => PadButton::DpadLeft,
        B::DPadRight => PadButton::DpadRight,
        _ => return None,
    })
}

/// Task View opened from the shortcut, driven by a PlayStation pad until it closes.
struct TaskViewRun {
    keys: TaskViewKeys,
    started: Instant,
    before: isize,
    view: Option<isize>,
}

/// Task View takes the foreground within this; after it, a different foreground window means it closed.
const SETTLE: Duration = Duration::from_millis(350);
const LONGEST_OPEN: Duration = Duration::from_secs(120);

struct MouseRun {
    pointer: PointerMotion,
    wheel_x: WheelMotion,
    wheel_y: WheelMotion,
    left: MouseButton,
    right: MouseButton,
    active: bool,
    last: Duration,
    screen_height: i32,
    screen_height_at: Option<Duration>,
}

impl MouseRun {
    fn new() -> Self {
        Self {
            pointer: PointerMotion::new(), wheel_x: WheelMotion::new(), wheel_y: WheelMotion::new(),
            left: MouseButton::new(), right: MouseButton::new(), active: false, last: Duration::ZERO, screen_height: 1080, screen_height_at: None,
        }
    }

    /// Never leaves a button stuck down when the mode ends mid-press.
    fn stop(&mut self) {
        if self.left.reset() { pad_win::mouse_button(pad_win::Button::Left, false); }
        if self.right.reset() { pad_win::mouse_button(pad_win::Button::Right, false); }
        self.pointer.reset();
        self.wheel_x.reset();
        self.wheel_y.reset();
    }
}

/// Every connected pad as the shortcuts, Task View and the mouse see it.
pub struct Pads {
    shared: Shared,
    pads: HashMap<JoystickId, Pad>,
    trackers: [HoldTracker; 4],
    clock: Instant,
    task_view: Option<TaskViewRun>,
    mouse: MouseRun,
    /// "Gravar combinação…" in progress, and the live combo last reported.
    capture: Option<shortcuts::Capture>,
    capture_live: u16,
}

impl Pads {
    pub fn new(shared: Shared) -> Self {
        Self { shared, pads: HashMap::new(), trackers: Default::default(), clock: Instant::now(), task_view: None, mouse: MouseRun::new(), capture: None, capture_live: 0 }
    }

    pub fn added(&mut self, id: JoystickId, playstation: bool) { self.pads.insert(id, Pad { playstation, ..Default::default() }); }

    pub fn removed(&mut self, id: JoystickId) { self.pads.remove(&id); }

    /// Y opens the session menu's tour, unless it completes a shortcut held right now (Select + Y closes the menu).
    pub fn is_tour_press(&self) -> bool {
        let mut masks = self.shared.get().masks;
        if masks[Slot::Menu.index()] == 0 && shortcuts::find_conflict(Slot::Menu, shortcuts::BACK | shortcuts::Y, &masks).is_none() {
            masks[Slot::Menu.index()] = shortcuts::BACK | shortcuts::Y;
        }
        let held: Vec<u16> = self.pads.values().map(|p| p.buttons).collect();
        cm_core::session_menu_tour::is_tour_press(&masks, &held)
    }

    /// Keeps each pad's buttons and sticks.
    pub fn event(&mut self, e: &sdl3::event::Event) {
        use sdl3::{event::Event, gamepad::Axis};
        match *e {
            Event::GamepadButtonDown { which, button, .. } | Event::GamepadButtonUp { which, button, .. } => {
                let down = matches!(e, Event::GamepadButtonDown { .. });
                if let (Some(pad), Some(b)) = (self.pads.get_mut(&which), pad_button(button)) {
                    if down { pad.buttons |= b.bit() } else { pad.buttons &= !b.bit() }
                }
            }
            Event::GamepadAxisMotion { which, axis, value, .. } => {
                let Some(pad) = self.pads.get_mut(&which) else { return };
                let v = controller_mouse::from_xinput(value);
                match axis {
                    Axis::LeftX => pad.left.0 = v,
                    Axis::LeftY => pad.left.1 = -v,
                    Axis::RightX => pad.right.0 = v,
                    Axis::RightY => pad.right.1 = -v,
                    _ => {}
                }
            }
            _ => {}
        }
    }

    /// Every device's buttons as the shortcuts see them: each pad SDL reads, then each HID collection
    /// whose centre button is read from HID (other brands, 1.7.1 #226), as Guide alone.
    fn held(&self) -> Vec<u16> {
        let hid = cm_platform::hid_win::home_held().into_iter().map(|on| if on { shortcuts::GUIDE } else { 0 });
        self.pads.values().map(|p| p.buttons).chain(hid).collect()
    }

    /// Something needs the fast pace of the controller thread: a button or stick held (holds, the
    /// pad as a mouse, repeats), Task View being driven, or a combo being recorded. Otherwise the
    /// thread can look less often and the PC sleeps more.
    pub fn busy(&self) -> bool {
        let off = |v: (f64, f64)| v.0.abs() > 0.12 || v.1.abs() > 0.12;
        self.task_view.is_some() || self.shared.get().capturing || self.pads.values().any(|p| p.buttons != 0 || off(p.left) || off(p.right))
            || cm_platform::hid_win::home_held().contains(&true)
    }

    /// One pass of the controller thread (every few milliseconds).
    pub fn tick(&mut self, send: &dyn Fn(Input)) {
        let now = self.clock.elapsed();
        let state = self.shared.get();
        // the menu asked for it: open Task View (never close one that is already open)
        // recording a combo: the pads feed it and nothing else fires (the shortcuts must be let go afterwards)
        if state.capturing { return self.record(send); }
        self.capture = None;
        if self.shared.take_task_view_request() && self.task_view.is_none() { self.toggle_task_view(&state); }
        self.shortcuts(&state, now, send);
        self.drive_task_view();
        self.mouse(&state, now);
    }

    fn shortcuts(&mut self, s: &State, now: Duration, send: &dyn Fn(Input)) {
        let devices = self.held();
        for slot in shortcuts::SLOTS {
            // ☰ held (in a session) or Select + Y (anytime, unless another shortcut took it) is the session menu
            // when no custom combo was picked
            let default_menu = slot == Slot::Menu && s.masks[slot.index()] == 0;
            let select_y_free = default_menu && shortcuts::find_conflict(Slot::Menu, shortcuts::BACK | shortcuts::Y, &s.masks).is_none();
            let select_y_held = select_y_free && shortcuts::is_held_on_any_device(shortcuts::BACK | shortcuts::Y, devices.iter().copied());
            let mask = if default_menu {
                if select_y_held || !s.in_session {
                    if select_y_free { shortcuts::BACK | shortcuts::Y } else { 0 }
                } else {
                    shortcuts::START
                }
            } else {
                s.masks[slot.index()]
            };
            let hold = if default_menu && mask == shortcuts::START { crate::input::HOLD } else { slot.hold(mask) };
            let enabled = match slot {
                Slot::Home => !s.in_session,
                Slot::Exit => s.in_session,
                Slot::Menu | Slot::TaskView => true,
            };
            let tracker = &mut self.trackers[slot.index()];
            tracker.set(mask, hold);
            tracker.set_enabled(enabled && mask != 0);
            // ☰ alone: Select + Start (a combo of its own) must not open the menu on the way
            let held = if default_menu && mask == shortcuts::START {
                self.pads.values().any(|p| p.buttons & shortcuts::ALLOWED == shortcuts::START)
            } else {
                shortcuts::is_held_on_any_device(mask, devices.iter().copied())
            };
            if !self.trackers[slot.index()].poll(held, now) { continue; }
            match slot {
                Slot::Menu => send(Input::MenuHeld),
                Slot::TaskView => self.toggle_task_view(s),
                Slot::Home | Slot::Exit => send(Input::Shortcut(slot)),
            }
        }
    }

    /// One poll of "Gravar combinação…" (1.7 `ShortcutPicker.Poll`): the live combo, then the one held when the pad let go.
    fn record(&mut self, send: &dyn Fn(Input)) {
        for t in &mut self.trackers { t.set_enabled(false); }
        if self.capture.is_none() { self.capture = Some(shortcuts::Capture::new()); self.capture_live = 0; }
        // the HID centre buttons (other brands) record as Guide, each as a device of its own
        let hid = cm_platform::hid_win::home_held().into_iter().enumerate().map(|(i, on)| (u32::MAX - i as u32, if on { shortcuts::GUIDE } else { 0 }));
        let devices: Vec<(u32, u16)> = self.pads.iter().map(|(id, p)| (id.raw(), p.buttons)).chain(hid).collect();
        let Some(capture) = self.capture.as_mut() else { return };
        let done = capture.feed(&devices);
        let live = capture.current_held();
        if live != self.capture_live { self.capture_live = live; send(Input::CaptureLive(live)); }
        if let Some(mask) = done {
            self.capture = None;
            self.shared.with(|s| s.capturing = false);
            send(Input::Captured(mask));
        }
    }

    /// The shortcut: opens Task View, or closes it when it was opened from here.
    fn toggle_task_view(&mut self, s: &State) {
        if self.task_view.is_some() {
            pad_win::send_task_view_key(pad_win::Key::Escape);
            self.stop_task_view("fechada pelo atalho");
            return;
        }
        let before = pad_win::foreground_window();
        cm_platform::session_menu_win::open_task_view();
        crate::startup_log("Visão de tarefas: aberta pelo controle");
        // Windows already moves Task View with an Xbox pad; a PlayStation one is driven from here.
        if s.task_view_playstation && self.pads.values().any(|p| p.playstation) {
            self.task_view = Some(TaskViewRun { keys: TaskViewKeys::new(), started: Instant::now(), before, view: None });
            self.shared.with(|s| s.task_view_active = true);
        }
    }

    fn stop_task_view(&mut self, why: &str) {
        self.task_view = None;
        self.shared.with(|s| s.task_view_active = false);
        crate::startup_log(&format!("Visão de tarefas: controle solto ({why})"));
    }

    fn drive_task_view(&mut self) {
        let Some(run) = &mut self.task_view else { return };
        let elapsed = run.started.elapsed();
        let foreground = pad_win::foreground_window();
        let mut stop = None;
        if elapsed >= LONGEST_OPEN {
            stop = Some("tempo esgotado");
        } else if run.view.is_none() {
            if elapsed < SETTLE { return; }
            if foreground == run.before || foreground == 0 { stop = Some("não ficou em primeiro plano"); } else { run.view = Some(foreground); }
        } else if run.view != Some(foreground) {
            stop = Some("fechada");
        }
        if stop.is_none() {
            // the PlayStation pads: buttons from all of them, the left stick pushed furthest
            let mut pad = TaskViewPad::default();
            for p in self.pads.values().filter(|p| p.playstation) {
                pad.buttons |= p.buttons;
                if p.left.0 * p.left.0 + p.left.1 * p.left.1 > pad.stick_x * pad.stick_x + pad.stick_y * pad.stick_y { (pad.stick_x, pad.stick_y) = p.left; }
            }
            for key in run.keys.step(pad, elapsed) {
                pad_win::send_task_view_key(match key {
                    TaskViewKey::Left => pad_win::Key::Left,
                    TaskViewKey::Right => pad_win::Key::Right,
                    TaskViewKey::Up => pad_win::Key::Up,
                    TaskViewKey::Down => pad_win::Key::Down,
                    TaskViewKey::Enter => pad_win::Key::Enter,
                    TaskViewKey::Escape => pad_win::Key::Escape,
                });
                if matches!(key, TaskViewKey::Enter | TaskViewKey::Escape) { stop = Some("escolha feita"); }
            }
        }
        if let Some(why) = stop { self.stop_task_view(why); }
    }

    /// The pad as a mouse: outside sessions, and only while the app is not in front (there the pad drives the app).
    fn mouse(&mut self, s: &State, now: Duration) {
        let want = s.mouse_on && !s.in_session && self.task_view.is_none() && !self.pads.is_empty() && !pad_win::app_is_foreground();
        let m = &mut self.mouse;
        if want != m.active {
            m.active = want;
            self.shared.with(|st| st.mouse_active = want);
            if want {
                // coming in: a button still held (the A that hid the app) is ignored until let go
                m.left.reset();
                m.right.reset();
                m.last = now;
                crate::startup_log("Mouse pelo controle: ligado");
            } else {
                m.stop();
                crate::startup_log("Mouse pelo controle: desligado");
            }
        }
        if !m.active { return; }
        // a stall (sleep, debugger) must not throw the pointer across the screen
        let seconds = (now.saturating_sub(m.last)).as_secs_f64().min(0.05);
        m.last = now;
        if m.screen_height_at.is_none_or(|t| now.saturating_sub(t) > Duration::from_millis(500)) {
            m.screen_height = pad_win::pointer_screen_height();
            m.screen_height_at = Some(now);
        }

        // every pad at once: the stick pushed furthest wins, A and B count on any pad
        let (mut left, mut right) = ((0., 0.), (0., 0.));
        let (mut a, mut b) = (false, false);
        let farther = |best: &mut (f64, f64), v: (f64, f64)| if v.0 * v.0 + v.1 * v.1 > best.0 * best.0 + best.1 * best.1 { *best = v };
        for p in self.pads.values() {
            farther(&mut left, p.left);
            farther(&mut right, p.right);
            a |= p.buttons & A != 0;
            b |= p.buttons & B != 0;
        }
        let (dx, dy) = m.pointer.step_scaled(left.0, left.1, seconds, m.screen_height, s.speed.factor());
        if dx != 0 || dy != 0 { pad_win::mouse_move(dx, dy); }
        let (wy, wx) = (m.wheel_y.step(right.1, seconds), m.wheel_x.step(right.0, seconds));
        pad_win::mouse_wheel(wy, wx);
        if let Some(down) = m.left.update(a) { pad_win::mouse_button(pad_win::Button::Left, down); }
        if let Some(down) = m.right.update(b) { pad_win::mouse_button(pad_win::Button::Right, down); }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn the_rows_offer_off_first_then_the_presets() {
        for slot in shortcuts::SLOTS {
            let list = choices(slot);
            assert_eq!(list.len(), 2 + shortcuts::presets(slot).len());
            assert!(is_record(list.last().unwrap()), "the last choice records a combo");
            assert!(!blocked(title_of(slot), RECORD, &[shortcuts::BACK | shortcuts::Y; 4]), "recording is never blocked");
            assert_eq!(mask_of(list[0]), 0);
            assert_eq!(mask_of(list[1]), shortcuts::suggested(slot));
        }
        assert_eq!(choices(Slot::Menu)[0], "Segurar ☰ (padrão)");
        assert_eq!(choices(Slot::Home)[0], "Desligado");
    }

    #[test]
    fn a_combo_is_named_for_both_pads() {
        assert_eq!(label(Slot::Menu, shortcuts::BACK | shortcuts::Y), "Select + Y (Create + △)");
        assert_eq!(label(Slot::Home, shortcuts::GUIDE), "Botão Xbox / PS");
    }

    #[test]
    fn a_row_follows_the_saved_combo_and_writes_it_back() {
        let mut c = cm_core::config::Config::default();
        c.menu_shortcut = (shortcuts::START | shortcuts::Y) as i32;
        let masks = current_masks(&c);
        let mut value = Value::Choice(choices(Slot::Menu), 0);
        assert!(load_row(MENU_ROW, &mut value, &masks));
        assert_eq!(value.text(), Some("Start + Y (Options + △)"));
        c.menu_shortcut = 0;
        assert!(store_row(MENU_ROW, &value, &mut c));
        assert_eq!(c.menu_shortcut, (shortcuts::START | shortcuts::Y) as i32);
        // the row on "Gravar combinação…" never writes anything
        let rec = Value::Choice(choices(Slot::Menu), choices(Slot::Menu).len() - 1);
        assert!(store_row(MENU_ROW, &rec, &mut c));
        assert_eq!(c.menu_shortcut, (shortcuts::START | shortcuts::Y) as i32);
        assert!(!store_row("HDR", &value, &mut c));
    }

    #[test]
    fn a_combo_from_a_hand_edited_file_is_still_shown() {
        let mut c = cm_core::config::Config::default();
        c.home_shortcut = (shortcuts::A | shortcuts::B | shortcuts::X) as i32;
        let mut value = Value::Choice(choices(Slot::Home), 0);
        load_row(HOME_ROW, &mut value, &current_masks(&c));
        assert_eq!(value.text(), Some("A + B + X (✕ + ○ + □)"));
        // shown just before "Gravar combinação…", which stays last
        let Value::Choice(options, at) = &value else { unreachable!() };
        assert_eq!((*at, options.last().copied()), (options.len() - 2, Some(RECORD)));
    }

    #[test]
    fn the_list_marks_a_combo_that_belongs_to_another_shortcut() {
        let mut c = cm_core::config::Config::default();
        c.exit_shortcut = (shortcuts::BACK | shortcuts::Y) as i32;
        let masks = current_masks(&c);
        // the Menu list shows Select + Y, but Exit has it
        let taken = choices(Slot::Menu).into_iter().filter(|l| blocked(MENU_ROW, l, &masks)).map(mask_of).collect::<Vec<_>>();
        assert!(taken.contains(&(shortcuts::BACK | shortcuts::Y)) && taken.len() < choices(Slot::Menu).len() - 1, "{taken:?}");
        assert!(!blocked("HDR", "x", &masks));
    }

    #[test]
    fn a_colliding_combo_is_not_stored() {
        let mut c = cm_core::config::Config::default();
        c.exit_shortcut = (shortcuts::START | shortcuts::BACK) as i32;
        let value = Value::Choice(vec![label(Slot::Home, shortcuts::START | shortcuts::BACK)], 0);
        assert!(store_row(HOME_ROW, &value, &mut c));
        assert_eq!(c.home_shortcut, 0);
    }

    #[test]
    fn the_speed_labels_round_trip() {
        for s in [Speed::Slow, Speed::Medium, Speed::Fast] { assert_eq!(speed_of_label(speed_label(s)), s); }
    }

    #[test]
    fn recording_swallows_the_pads_presses_until_it_ends() {
        let hub = shared();
        hub.set_capturing(true);
        assert!(swallow_press(&hub, false) && swallow_press(&hub, true));
        hub.set_capturing(false);
        assert!(!swallow_press(&hub, false));
    }

    #[test]
    fn the_menus_task_view_request_is_taken_once() {
        let hub = shared();
        assert!(!hub.take_task_view_request(), "nothing asked");
        hub.request_task_view();
        hub.request_task_view();
        assert!(hub.take_task_view_request());
        assert!(!hub.take_task_view_request(), "one open per request, however many asked");
    }

    #[test]
    fn presses_are_swallowed_in_the_tray_and_while_the_pad_is_busy() {
        let hub = shared();
        assert!(!swallow_press(&hub, false), "a visible window gets its presses");
        hub.with(|s| s.window_visible = false);
        assert!(swallow_press(&hub, false), "hidden in the tray");
        assert!(!swallow_press(&hub, true), "in a session the pad belongs to the app's own gating");
        hub.with(|s| { s.window_visible = true; s.mouse_active = true; });
        assert!(swallow_press(&hub, false));
        hub.with(|s| { s.mouse_active = false; s.task_view_active = true; });
        assert!(swallow_press(&hub, true));
    }
}
