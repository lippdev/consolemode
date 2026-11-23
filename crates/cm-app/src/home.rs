//! The console: top bar with LB/RB tabs, and the Início tab in the PS5 style approved on
//! 2026-11-16 (proposal B): full-bleed art of the focused item, a row of rounded tiles under the
//! tabs (launchers, "add app", then recent Steam games) with the focused one's name beside it,
//! its logo, meta row and "Jogar" below, and the hint bar.

use std::{collections::{HashMap, HashSet}, path::PathBuf, sync::Arc};

use cm_platform::steam::Game;
use cm_ui::{Glyph, Ui, color, glyph, motion};
use gpui::{
    AnimationExt, Context, Div, FocusHandle, FontWeight, ImageSource, InteractiveElement, IntoElement, KeyDownEvent, MouseButton, MouseDownEvent,
    MouseMoveEvent, ObjectFit, ParentElement, ScrollDelta, ScrollEvent, StatefulInteractiveElement, Render, RenderImage, SharedString, Styled, Window, div, img, linear_color_stop, linear_gradient, relative,
    prelude::*,
};

use cm_core::config::{Config, Paths};

use crate::{i18n::{n, shown, t, tn}, input::Pad, settings::{self, Category, Nav, Section, Value}, sound::{Sound, Sounds}};

/// What is on screen besides the tabs.
#[derive(Clone, Copy, PartialEq)]
pub enum View { Home, CardMenu { sel: usize }, Moving, Guide { sel: usize }, Launch { step: usize }, Confirm, Tour { step: usize } }

/// A running console session, as the UI sees it.
pub struct Running {
    pub started: std::time::Instant,
    pub commands: std::sync::mpsc::Sender<crate::engine::Command>,
    pub simulated: bool,
    pub setup_key: String,
    // Keep shell status tied to the session even if settings or card order change.
    pub shell_focus: String,
    pub shell_launcher: cm_core::session::Launcher,
}

pub struct Launcher {
    /// What the card opens (cm_core's launcher, as 1.7's `fullscreenMode`).
    pub kind: cm_core::session::Launcher,
    pub name: gpui::SharedString,
    pub custom_id: Option<String>,
    pub note: &'static str,
    pub sub: &'static str,
    pub icon: Option<Arc<RenderImage>>,
    /// The launcher's own look, from the colors of its logo (card and Home background).
    pub brand: Brand,
    /// The logo drawn large and faint in that look (Xbox: its plate-less white version).
    pub mark: Option<Arc<RenderImage>>,
}

/// A launcher's colors: a dark gradient from its logo, a glow from the top left and an optional
/// second glow from the bottom right (Playnite's logo goes from magenta to cyan).
#[derive(Clone, Copy)]
pub struct Brand { pub base: (u32, u32), pub glow: (u32, f32), pub glow2: Option<(u32, f32)> }

impl Brand {
    pub const STEAM: Brand = Brand { base: (0x1b2838, 0x0b1017), glow: (0x66c0f4, 0.30), glow2: Some((0x417a9b, 0.25)) };
    pub const PLAYNITE: Brand = Brand { base: (0x24123a, 0x0d0716), glow: (0xe43fd0, 0.30), glow2: Some((0x22d3c5, 0.24)) };
    pub const XBOX: Brand = Brand { base: (0x107c10, 0x063306), glow: (0x9bf00b, 0.24), glow2: None };
    /// A custom app with no brand of its own.
    pub const NEUTRAL: Brand = Brand { base: (0x2a3038, 0x14181e), glow: (0x9cc6f2, 0.18), glow2: None };
}

fn hex(c: u32, a: f32) -> gpui::Hsla { let mut h: gpui::Hsla = gpui::rgb(c).into(); h.a = a; h }

/// The brand's backdrop with its logo large and faint, cropped by the box (card or screen).
/// `radius` rounds every layer: GPUI clips children to their parent's rectangle, not its corners.
fn brand_layer(ui: &Ui, b: Brand, icon: Option<&Arc<RenderImage>>, mark: f32, right: f32, bottom: f32, mark_alpha: f32, radius: gpui::Pixels) -> Div {
    let mut d = div().absolute().top_0().left_0().size_full().rounded(radius)
        .bg(linear_gradient(160., linear_color_stop(hex(b.base.0, 1.), 0.), linear_color_stop(hex(b.base.1, 1.), 1.)))
        .child(div().absolute().top_0().left_0().size_full().rounded(radius).bg(linear_gradient(135., linear_color_stop(hex(b.glow.0, b.glow.1), 0.), linear_color_stop(hex(b.glow.0, 0.), 0.6))));
    if let Some((c, a)) = b.glow2 {
        d = d.child(div().absolute().top_0().left_0().size_full().rounded(radius).bg(linear_gradient(315., linear_color_stop(hex(c, a), 0.), linear_color_stop(hex(c, 0.), 0.55))));
    }
    if let Some(i) = icon {
        d = d.child(img(ImageSource::Render(sharp(i, ui, mark))).absolute().right(ui.u(right)).bottom(ui.u(bottom)).size(ui.u(mark)).opacity(mark_alpha));
    }
    d
}

/// What fills the screen behind the Home for an item.
#[derive(Clone)]
pub enum Backdrop { Art(PathBuf), Brand(Brand, Option<Arc<RenderImage>>) }

impl Backdrop {
    fn element(&self, ui: &Ui) -> gpui::AnyElement {
        match self {
            Backdrop::Art(p) => bleed_free(p).into_any_element(),
            Backdrop::Brand(b, icon) => brand_layer(ui, *b, icon.as_ref(), 760., 140., 120., 0.10, gpui::px(0.)).into_any_element(),
        }
    }
}

pub enum Item { Launcher(Launcher), Add, Game(Game, Option<(u32, u32)>) }

impl Item {
    fn background(&self) -> Option<Backdrop> {
        match self {
            Item::Launcher(l) => Some(Backdrop::Brand(l.brand, l.mark.clone())),
            Item::Add => None,
            Item::Game(g, _) => g.hero.clone().map(Backdrop::Art),
        }
    }
}

const TABS: [&str; 3] = [n("Início"), n("Sessão"), n("Sistema")];

pub struct Console {
    pub(crate) config: Config,
    pub(crate) custom_editor: Option<crate::custom_apps::Editor>,
    pub(crate) picking_app: bool,
    pub(crate) paths: Paths,
    pub(crate) running: Option<Running>,
    pub(crate) return_to_pc_pending: bool,
    /// A short message at the top (session ended, a problem), and when it appeared.
    pub(crate) toast: Option<(String, std::time::Instant)>,
    pub(crate) window: Option<gpui::AnyWindowHandle>,
    /// False while a session runs and the app stepped aside: the controller belongs to the game
    /// then, except holding ☰.
    pub(crate) app_in_front: bool,
    pub(crate) hardware: crate::hardware::Hardware,
    pub items: Vec<Item>,
    /// Recent Steam games kept aside while "Mostrar jogos da Steam na Início" is off (the default).
    pub(crate) hidden_games: Vec<Item>,
    pub steam_icon: Option<Arc<RenderImage>>,
    pub focus: usize,
    pub tab: usize,
    pub controller: Option<String>,
    /// Batteries of the connected pads and the low-battery warnings already given (battery.rs).
    pub(crate) batteries: Vec<cm_core::controller_battery::BatteryReading>,
    /// The session menu's tour in progress, and whether Sistema asked for it (it starts when the menu is on screen).
    /// The opening on screen (intro.rs) and how many have run (restarts its animation).
    /// "What's new" after an update: waiting for the app to be in front and idle, and on screen (whats_new.rs).
    pub(crate) pending_whats_new: Option<cm_core::release_notes::Notice>,
    pub(crate) whats_new: Option<cm_core::release_notes::Notice>,
    /// Controller wake (Sistema → Controles): what was read and whether a reading or turning on is running.
    pub(crate) wake: crate::wake::WakeState,
    /// Sistema → TV → Testar a TV is talking to the TV.
    pub(crate) tv_testing: bool,
    /// The diagnostic report is being gathered (report.rs).
    pub(crate) report_busy: bool,
    /// The session menu's "Apps": ControlFS and JoyChromium, installed or installing (helper_apps.rs).
    pub(crate) helper_rows: [cm_core::helper_apps::Row; 2],
    /// Sistema → Atalhos: a combo being recorded from the controller (pad_shortcuts.rs).
    pub(crate) shortcut_capture: Option<crate::pad_shortcuts::Recording>,
    /// The controller thread's shared state: the session menu asks it to open Task View (pad_shortcuts.rs).
    pub(crate) pads: Option<crate::pad_shortcuts::Shared>,
    pub(crate) intro: Option<crate::intro::Intro>,
    pub(crate) intro_run: usize,
    pub(crate) guide_tour: Option<crate::guide_tour::GuideTour>,
    pub(crate) tour_on_open: bool,
    pub(crate) guide_from_tray: bool,
    pub(crate) battery_alarms: HashMap<String, cm_core::controller_battery::LowBatteryAlarm>,
    pub clock: (u16, u16),
    pub now: u64,
    pub(crate) view: View,
    pub(crate) session: Vec<Section>,
    pub(crate) system: Vec<Category>,
    pub(crate) nav: Nav,
    /// Sessão/Sistema: the list of choices open beside a row (PS5 style), its scroll and animation.
    pub(crate) dropdown: Option<crate::settings::Dropdown>,
    pub(crate) dropdown_scroll: gpui::ScrollHandle,
    pub(crate) dropdown_anim: usize,
    /// Sessão → Telas: the screen whose role just changed, and its animation.
    pub(crate) map_changed: Option<String>,
    pub(crate) map_anim: usize,
    /// Card menu "Ao abrir este jogo", per Steam app id; pinned games; session-menu volume.
    pub(crate) game_opts: HashMap<u32, Vec<Value>>,
    pub(crate) pinned: HashSet<u32>,
    pub(crate) volume: u8,
    /// Session menu panels: the selected row, the windows listed in "Janelas", the FPS shown.
    pub(crate) guide_row: usize,
    pub(crate) guide_windows: Vec<cm_platform::session_menu_win::SwitchWindow>,
    /// "Janelas": the live pictures of the windows (drawn by Windows over the tiles).
    pub(crate) live_thumbs: std::rc::Rc<std::cell::RefCell<cm_platform::live_thumbs::LiveThumbs>>,
    /// Windows' VRR during a session (None outside one), for "Tela".
    pub(crate) session_vrr: Option<bool>,
    pub(crate) session_fps: u32,
    /// HDR of the game screen while a session runs (None: no session, or Windows can't tell).
    pub(crate) session_hdr: Option<bool>,
    pub(crate) launch_task: Option<gpui::Task<()>>,
    // motion: each counter restarts one animation when it changes (lippstack animate/choreograph)
    pub(crate) prev_focus: usize,
    prev_tab: usize,
    tab_anim: usize,
    tab_dir: f32,
    pub(crate) menu_anim: usize,
    /// An overlay playing its exit (card menu, session menu) after the view went back home.
    pub(crate) exiting: Option<View>,
    pub(crate) exit_anim: usize,
    pub(crate) exit_task: Option<gpui::Task<()>>,
    pub(crate) vol_anim: usize,
    pub(crate) pane_anim: usize,
    pub(crate) pane_dir: f32,
    pub(crate) settings_selection_anim: usize,
    prev_bg: Option<Backdrop>,
    pub(crate) fade: usize,
    /// Row scroll in design units, and where its animation starts.
    pub(crate) scroll: f32,
    prev_scroll: f32,
    scroll_anim: usize,
    /// The mouse was the last thing used: arrows show, the row stops following focus.
    mouse_mode: bool,
    /// Visible width of the row (design units) at the last render, to clamp the scroll.
    row_view: f32,
    focus_handle: FocusHandle,
    pub(crate) sounds: Sounds,
    painted: bool,
    wheel: f32,
}

impl Console {
    pub fn new(items: Vec<Item>, steam_icon: Option<Arc<RenderImage>>, mut config: Config, paths: Paths, cx: &mut Context<Self>) -> Self {
        config.custom_apps = cm_core::custom_apps::entries(&config);
        let (mut games, mut items): (Vec<Item>, Vec<Item>) = items.into_iter().partition(|i| matches!(i, Item::Game(..)));
        // games the user hid stay out; the ones they moved keep their place (the rest by last played)
        let id = |i: &Item| if let Item::Game(g, _) = i { g.app_id } else { 0 };
        games.retain(|g| !config.hidden_games.contains(&id(g)));
        games.sort_by_key(|g| config.game_order.iter().position(|o| *o == id(g)).unwrap_or(usize::MAX));
        // games stay out of the row until the user turns them on in Sistema → Aparência
        if config.show_steam_games { items.append(&mut games); }
        let mut game_opts = HashMap::new();
        for p in &config.game_profiles {
            let mut v = crate::overlays::game_defaults();
            crate::bind::game_values(Some(p), &mut v);
            game_opts.insert(p.steam_app_id, v);
        }
        let mut session = settings::session();
        crate::bind::load(&mut session, &config);
        let hardware = crate::hardware::Hardware::read();
        hardware.fill(&mut session, &config);
        let mut system = settings::system();
        for cat in system.iter_mut() { crate::bind::load(&mut cat.sections, &config); }
        let pinned: HashSet<u32> = config.pinned_games.iter().copied().collect();
        let mut sounds = Sounds::open();
        sounds.enabled = config.interface_sounds;
        sounds.style = crate::sound::Style::from_config(&config.sound_style);
        sounds.volume = config.sound_volume;
        Self {
            custom_editor: None, picking_app: false, running: None, return_to_pc_pending: false, toast: None, window: None, app_in_front: true, hardware,
            config, paths, items, hidden_games: games, steam_icon, focus: 0, tab: 0, controller: None, batteries: Vec::new(), battery_alarms: HashMap::new(), guide_tour: None, tour_on_open: false, guide_from_tray: false, intro: None, intro_run: 0, wake: Default::default(), report_busy: false, tv_testing: false, helper_rows: Default::default(), shortcut_capture: None, pads: None, pending_whats_new: None, whats_new: None, clock: cm_platform::local_time(), now: unix_now(),
            view: View::Home, session, system, nav: Nav::default(),
            game_opts, pinned, volume: 60, launch_task: None, guide_row: 0, guide_windows: Vec::new(), live_thumbs: Default::default(), session_vrr: None, session_fps: 0, session_hdr: None,
            prev_focus: 0, prev_tab: 0, tab_anim: 0, tab_dir: 1., menu_anim: 0, exiting: None, exit_anim: 0, exit_task: None,
            vol_anim: 0, pane_anim: 0, pane_dir: 1., settings_selection_anim: 0, dropdown: None, dropdown_scroll: gpui::ScrollHandle::new(), dropdown_anim: 0, map_changed: None, map_anim: 0,
            prev_bg: None, fade: 0, scroll: 0., prev_scroll: 0., scroll_anim: 0, mouse_mode: false, row_view: 1700., focus_handle: cx.focus_handle(), sounds, painted: false, wheel: 0.,
        }
    }

    pub fn focus_handle(&self) -> &FocusHandle { &self.focus_handle }

    pub fn press(&mut self, p: Pad, cx: &mut Context<Self>) {
        // any press skips the opening
        if self.skip_intro(cx) { return; }
        if self.whats_new_press(p, cx) { return; }
        if self.custom_press(p, cx) { return; }
        if self.tour_press(p, cx) { return; }
        if self.overlay_press(p, cx) { return; }
        // an open list of choices, or a combo being recorded, takes every press (LB/RB too)
        if self.dropdown.is_some() || self.shortcut_capture.is_some() { return self.settings_press(p, cx); }
        match p {
            Pad::LB | Pad::RB => return self.set_tab(if p == Pad::LB { (self.tab + 2) % 3 } else { (self.tab + 1) % 3 }, cx),
            _ if self.tab != 0 => return self.settings_press(p, cx),
            Pad::Menu | Pad::Y => return self.open_card_menu(cx),
            Pad::A => return self.start_launch(cx),
            _ => {}
        }
        match p {
            Pad::Left | Pad::Right if self.tab == 0 => {
                let next = if p == Pad::Left { self.focus.saturating_sub(1) } else { (self.focus + 1).min(self.items.len() - 1) };
                self.mouse_mode = false;
                self.set_focus(next, cx);
                // controller and keyboard: the row follows focus, like the mockup
                return self.set_scroll(self.row_shift(self.focus, self.focus), cx);
            }
            Pad::B => self.sounds.play(Sound::Back),
            _ => return,
        }
        cx.notify();
    }

    fn set_focus(&mut self, next: usize, cx: &mut Context<Self>) {
        if next == self.focus || next >= self.items.len() { return; }
        self.prev_bg = self.items[self.focus].background();
        self.prev_focus = self.focus;
        self.focus = next;
        self.fade += 1;
        self.sounds.play(Sound::Move);
        cx.notify();
    }

    pub(crate) fn set_scroll(&mut self, to: f32, cx: &mut Context<Self>) {
        let max = (self.row_len() - self.row_view).max(0.);
        let to = to.clamp(0., max);
        if (to - self.scroll).abs() < 0.5 { return; }
        self.prev_scroll = self.scroll;
        self.scroll = to;
        self.scroll_anim += 1;
        cx.notify();
    }

    /// Mouse: a click focuses a card; the row only moves if that card is cut at an edge.
    fn click_card(&mut self, i: usize, cx: &mut Context<Self>) {
        if matches!(self.items[i], Item::Add) { self.click_card_focus(i, cx); return self.press(Pad::A, cx); }
        if self.focus == i { return self.press(Pad::A, cx); }
        self.click_card_focus(i, cx);
    }

    /// Right click on a card: focus it and open its PS5-style options menu.
    fn right_click_card(&mut self, i: usize, cx: &mut Context<Self>) {
        if self.view != View::Home { return self.press(Pad::B, cx); }
        self.click_card_focus(i, cx);
        self.open_card_menu(cx);
    }

    fn click_card_focus(&mut self, i: usize, cx: &mut Context<Self>) {
        self.set_focus(i, cx);
        let (l, r) = (self.card_left(i, i) - 24., self.card_left(i, i) + TILE_F + RING + 24.);
        if l < self.scroll { self.set_scroll(l, cx); } else if r > self.scroll + self.row_view { self.set_scroll(r - self.row_view, cx); }
    }

    /// Arrows: one page; the wheel: one card per notch.
    fn page(&mut self, dir: f32, cx: &mut Context<Self>) { let to = self.scroll + dir * (self.row_view - STEP).max(STEP); self.set_scroll(to, cx); }

    pub(crate) fn set_tab(&mut self, tab: usize, cx: &mut Context<Self>) {
        // the tab already in use: back to its first page (Sistema inside a category: its list)
        if tab == self.tab {
            if self.dropdown.take().is_some() { cx.notify(); }
            if tab == 2 && self.nav.category.is_some() { self.system_root(cx); }
            return;
        }
        self.dropdown = None;
        // the new tab slides in from the side it sits on (LB/left tabs from the left)
        self.tab_dir = if tab > self.tab { 1. } else { -1. };
        self.prev_tab = self.tab;
        self.tab_anim += 1;
        self.tab = tab;
        self.nav = Nav::default();
        self.sounds.play(Sound::Tab);
        cx.notify();
    }

    /// Puts the Steam games in the row, or takes them out (they keep their order and per-game settings).
    pub(crate) fn show_steam_games(&mut self, on: bool) {
        if on {
            self.items.append(&mut self.hidden_games);
        } else {
            let (games, rest): (Vec<Item>, Vec<Item>) = std::mem::take(&mut self.items).into_iter().partition(|i| matches!(i, Item::Game(..)));
            self.items = rest;
            self.hidden_games = games;
        }
        self.focus = self.focus.min(self.items.len() - 1);
        self.prev_focus = self.focus;
        self.prev_bg = None;
        self.scroll = 0.;
        self.prev_scroll = 0.;
    }

    /// Saves the config now (it is small; every change is written at once, atomically).
    pub(crate) fn save_config(&self) {
        if let Err(e) = cm_core::config::save(&self.paths, &self.config) { crate::startup_log(&format!("config: could not save: {e}")); }
    }

    pub fn tick(&mut self, cx: &mut Context<Self>) {
        let t = cm_platform::local_time();
        if t != self.clock { self.clock = t; self.now = unix_now(); cx.notify(); }
    }
}

fn unix_now() -> u64 { std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).map(|d| d.as_secs()).unwrap_or(0) }

// Tile geometry (design units), PS5 style: rounded squares in a row under the tabs; the focused
// one grows, a white ring floats off it and its name sits under it.
pub(crate) const TILE: f32 = 132.;
pub(crate) const TILE_F: f32 = 200.;
const GAP: f32 = 14.;
const STEP: f32 = TILE + GAP;
/// How far the focus ring sits outside the focused tile.
const RING: f32 = 9.;
/// The focused tile's name, this far under it.
const NAME_GAP: f32 = 16.;
/// Where the row starts, from the left and the top.
pub(crate) const ROW_X: f32 = 96.;
pub(crate) const ROW_Y: f32 = 150.;
/// Whole pixels of `v` design units.
fn whole(ui: &Ui, v: f32) -> f32 { f32::from(ui.u(v)).round() }

impl Console {
    pub(crate) fn after_add(&self) -> usize { self.items.iter().position(|i| matches!(i, Item::Add)).map(|i| i + 1).unwrap_or(usize::MAX) }
    /// What the focused tile takes beyond a plain tile: its growth and its ring on both sides.
    fn focus_extra(&self) -> f32 { TILE_F - TILE + 2. * RING }
    /// Left edge of tile `i` in the row (design units) when `focus` is the focused one.
    pub(crate) fn card_left(&self, i: usize, focus: usize) -> f32 {
        i as f32 * STEP + if i >= self.after_add() { 26. } else { 0. } + if i > focus { self.focus_extra() } else if i == focus { RING } else { 0. }
    }
    /// Right edge of the focused tile (where its menu opens).
    pub(crate) fn focus_right(&self) -> f32 { self.card_left(self.focus, self.focus) + TILE_F + RING }
    /// The row keeps the focused tile third from the left.
    pub(crate) fn row_shift(&self, i: usize, focus: usize) -> f32 { (self.card_left(i, focus) - 2. * STEP).max(0.) }
    fn row_len(&self) -> f32 {
        let last = self.items.len().saturating_sub(1);
        self.card_left(last, self.focus) + if last == self.focus { TILE_F + RING } else { TILE }
    }

    /// The name and the small line under it, under the focused tile.
    fn tile_name(&self, it: &Item) -> (String, String) {
        match it {
            Item::Game(g, _) => (g.name.clone(), ago(g.last_played, self.now)),
            Item::Launcher(l) => (l.shown_name().to_string(), t(l.note).to_string()),
            Item::Add => (t("Adicionar app").to_string(), t("qualquer programa").to_string()),
        }
    }
}

/// `icon` scaled down (on the CPU, with a good filter) to the device pixels of `size` design
/// units, once per size: the GPU shrinking a 256 px icon itself leaves jagged edges.
pub(crate) fn sharp(icon: &Arc<RenderImage>, ui: &Ui, size: f32) -> Arc<RenderImage> {
    use std::{cell::RefCell, collections::HashMap};
    thread_local! { static CACHE: RefCell<HashMap<(usize, u32), Arc<RenderImage>>> = RefCell::new(HashMap::new()); }
    let px = (f32::from(ui.u(size)) * cm_ui::device_scale()).ceil() as u32;
    let key = (Arc::as_ptr(icon) as usize, px);
    CACHE.with(|c| {
        let mut c = c.borrow_mut();
        if let Some(i) = c.get(&key) { return i.clone(); }
        if c.len() > 96 { c.clear(); }
        let i = cm_platform::sharp::downscale(icon, px).map(Arc::new).unwrap_or_else(|| icon.clone());
        c.insert(key, i.clone());
        i
    })
}

/// Art that fills its box, drawn 3 % larger on every side. Scaling a small image up (the blurred
/// heroes are 192×62) makes the GPU sample past its edge into whatever sits next to it in the
/// texture atlas, which showed as a green line along the top; the margin keeps that off screen.
fn bleed_free(path: &PathBuf) -> gpui::Img {
    img(ImageSource::from(path.clone())).absolute().top(relative(-0.03)).left(relative(-0.03)).w(relative(1.06)).h(relative(1.06)).object_fit(ObjectFit::Cover)
}


impl Render for Console {
    fn render(&mut self, window: &mut Window, cx: &mut Context<Self>) -> impl IntoElement {
        let ui = Ui::for_viewport(window.viewport_size());
        cm_ui::set_device_scale(window.scale_factor());
        // the opening is over everything, whatever is under it
        let stage = self.stage(window, cx);
        stage.children(self.intro_view(&ui, cx))
            .when(!window.is_fullscreen(), |stage| stage.child(Self::window_bar(window)))
    }
}

impl Console {
    fn window_bar(window: &Window) -> Div {
        use gpui::WindowControlArea::{Close, Drag, Max, Min};

        let maximize = if window.is_maximized() { "\u{e923}" } else { "\u{e922}" };
        let button = |id: &'static str, area, icon: &'static str, hover| {
            div().id(id).occlude().window_control_area(area)
                .w(gpui::px(44.)).h_full().flex().items_center().justify_center()
                .text_size(gpui::px(10.)).hover(move |s| s.bg(hover)).child(icon)
        };
        div().absolute().top_0().left_0().right_0().h(gpui::px(28.)).flex()
            .font_family("Segoe MDL2 Assets").text_color(color::ink())
            .child(div().flex_1().h_full().window_control_area(Drag))
            .child(button("window-min", Min, "\u{e921}", color::white(0.15)))
            .child(button("window-max", Max, maximize, color::white(0.15)))
            .child(button("window-close", Close, "\u{e8bb}", gpui::rgb(0xc42b36).into()))
    }

    fn stage(&mut self, window: &mut Window, cx: &mut Context<Self>) -> gpui::Stateful<Div> {
        let ui = Ui::for_viewport(window.viewport_size());
        if !self.painted { self.painted = true; crate::startup_log("first render"); }
        let mut stage = div()
            .id("console")
            .track_focus(&self.focus_handle)
            .on_key_down(cx.listener(|this, e: &KeyDownEvent, _, cx| {
                if this.skip_intro(cx) { return; }
                if this.custom_key(e, cx) { return; }
                let p = match e.keystroke.key.as_str() {
                    "left" => Pad::Left, "right" => Pad::Right, "up" => Pad::Up, "down" => Pad::Down,
                    "enter" | "space" => Pad::A, "escape" | "backspace" => Pad::B, "q" | "pageup" => Pad::LB, "e" | "pagedown" => Pad::RB,
                    "y" => Pad::Y, "x" => Pad::X, "m" => Pad::Menu, _ => return,
                };
                this.press(p, cx);
            }))
            .on_mouse_move(cx.listener(|this, _: &MouseMoveEvent, _, cx| if !this.mouse_mode { this.mouse_mode = true; cx.notify(); }))
            // right click = the controller's options button: the focused card's menu on Home;
            // with a menu open it closes it, like B
            .on_mouse_down(MouseButton::Right, cx.listener(|this, _: &MouseDownEvent, _, cx| {
                if this.view == View::Home && this.tab == 0 { this.open_card_menu(cx) } else { this.press(Pad::B, cx) }
            }))
            .size_full()
            .relative()
            .overflow_hidden()
            .font_family(cm_ui::FONT)
            .text_color(color::ink());

        if !matches!(self.view, View::Guide { .. }) {
            stage = stage.bg(color::stage());
        }
        if self.custom_editor.is_some() { let editor = self.custom_editor_view(&ui, cx); return stage.child(self.background(&ui, true)).child(editor).children(self.toast_view(&ui)); }
        match self.view {
            View::Launch { step } => return stage.child(self.background(&ui, false)).child(self.launch_view(&ui, step)).child(self.hints(&ui, cx)),
            View::Guide { sel } => { let g = self.guide_view(&ui, sel, true, cx); return stage.child(g).child(self.hints(&ui, cx)).children(self.toast_view(&ui)); }
            View::Confirm => return stage.child(self.confirm_view(&ui, cx)),
            _ => {}
        }
        // a tab switch: the content slides in from the new tab's side; the art only fades
        let (anim, dir, d) = (self.tab_anim, self.tab_dir, motion::dist(24.));
        let enter = move |el: Div| -> gpui::AnyElement {
            if anim == 0 { return el.into_any_element(); }
            el.with_animation(("tab-in", anim), motion::out(motion::TAB), move |el, t| el.opacity(t).left(ui.u(dir * d * (1. - t)))).into_any_element()
        };
        if self.tab == 0 {
            let bg = self.background(&ui, true);
            let bg: gpui::AnyElement = if anim == 0 { bg.into_any_element() } else { bg.with_animation(("tab-bg", anim), motion::out(motion::TAB), |el, t| el.opacity(t)).into_any_element() };
            let home = self.home(&ui, cx);
            stage = stage.child(bg).child(enter(home));
            if let View::CardMenu { sel } = self.view { stage = stage.child(self.card_menu_view(&ui, sel, true, cx)); }
            if let (View::Home, Some(View::CardMenu { sel })) = (self.view, self.exiting) { stage = stage.child(self.card_menu_view(&ui, sel, false, cx)); }
        } else {
            let s = self.settings_view(&ui, cx);
            stage = stage.child(enter(s));
        }
        stage = stage.child(self.top_bar(&ui, cx)).child(self.hints(&ui, cx));
        if let Some(rec) = self.recording_view(&ui, cx) { stage = stage.child(rec); }
        if let (View::Home, Some(View::Guide { sel })) = (self.view, self.exiting) { stage = stage.child(self.guide_view(&ui, sel, false, cx)); }
        if let View::Tour { step } = self.view { stage = stage.child(self.tour_view(&ui, step, cx)); }
        if let Some(card) = self.whats_new_view(&ui, cx) { stage = stage.child(card); }
        stage.children(self.toast_view(&ui))
    }
}

impl Console {
    fn background(&self, ui: &Ui, shaded: bool) -> Div {
        let mut layer = div().absolute().top_0().left_0().size_full();
        if let Some(p) = &self.prev_bg { layer = layer.child(p.element(ui)); }
        if let Some(p) = self.items[self.focus].background() {
            layer = layer.child(div().absolute().top_0().left_0().size_full().child(p.element(ui))
                .with_animation(("bg", self.fade), motion::out(motion::BACKDROP), |el, t| el.opacity(t)));
        }
        // a lens vignette: the corners fall off into the dark, like a photo, not a flat picture
        let vignette = ui.vignette();
        if !shaded { return layer.child(vignette); }
        // the mockup's two shades: from the left (text side) and from the bottom (cards)
        layer
            .child(div().absolute().top_0().left_0().size_full().bg(linear_gradient(90., linear_color_stop(color::shade(0.88), 0.), linear_color_stop(color::shade(0.), 0.7))))
            .child(div().absolute().top_0().left_0().size_full().bg(linear_gradient(0., linear_color_stop(color::shade(0.9), 0.), linear_color_stop(color::shade(0.), 0.45))))
            .child(div().absolute().top_0().left_0().size_full().bg(linear_gradient(180., linear_color_stop(color::shade(0.55), 0.), linear_color_stop(color::shade(0.), 0.38))))
            .child(vignette)
    }

    fn top_bar(&self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let ui = *ui;
        // PS5: the tabs as words on the left, the one in use bright; LB/RB as small keycaps
        let key = |label: &'static str, id: &'static str, p: Pad, cx: &mut Context<Self>| {
            div().id(id).text_size(ui.u(13.)).font_weight(FontWeight::BOLD).px(ui.u(8.)).py(ui.u(6.)).rounded(ui.u(8.)).cursor_pointer()
                .text_color(color::white(0.6)).bg(color::white(0.08)).shadow(ui.gloss(0.14)).child(label)
                .hover(|s| s.bg(color::white(0.14)))
                .on_click(cx.listener(move |this, _, _, cx| this.press(p, cx)))
        };
        let mut tabs = div().flex().items_center().gap(ui.u(38.)).child(key("LB", "lb", Pad::LB, cx));
        for (i, name) in TABS.iter().enumerate() {
            let on = i == self.tab;
            let mut l = div().id(("tab", i)).text_size(ui.u(30.)).font_family(cm_ui::FONT).cursor_pointer()
                .on_click(cx.listener(move |this, _, _, cx| this.set_tab(i, cx)));
            l = if on { l.font_weight(FontWeight::SEMIBOLD).text_color(color::ink()) } else { l.font_weight(FontWeight::MEDIUM).text_color(color::white(0.55)).hover(|s| s.text_color(color::white(0.85))) };
            tabs = tabs.child(l.child(ui.lifted(t(name))));
        }
        tabs = tabs.child(key("RB", "rb", Pad::RB, cx));

        // round glass buttons: Sessão, Sistema and the menu (☰)
        let round = |id: &'static str, g: char, cx: &mut Context<Self>, f: fn(&mut Self, &mut Context<Self>)| {
            div().id(id).size(ui.u(52.)).rounded_full().flex().items_center().justify_center().cursor_pointer()
                .bg(color::white(0.07)).shadow(ui.gloss(0.12)).hover(|s| s.bg(color::white(0.16)))
                .child(ui.icon(g, 30., color::white(0.85)))
                .on_click(cx.listener(move |this, _, _, cx| f(this, cx)))
        };
        let mut right = div().ml_auto().flex().items_center().gap(ui.u(14.));
        if self.controller.is_some() || !self.batteries.is_empty() {
            let mut chip = div().flex().items_center().gap(ui.u(10.)).h(ui.u(48.)).px(ui.u(16.)).rounded_full().bg(color::white(0.07)).shadow(ui.gloss(0.12))
                .child(ui.icon(glyph::SPORTS_ESPORTS, 30., color::mute()));
            if let Some(b) = self.battery_chip(&ui, 19., 26.) { chip = chip.child(b); }
            right = right.child(chip);
        }
        right = right
            .child(round("go-session", glyph::TUNE, cx, |this, cx| this.set_tab(1, cx)))
            .child(round("go-system", glyph::SETTINGS, cx, |this, cx| this.set_tab(2, cx)));
        if self.running.is_some() {
            right = right.child(round("go-menu", glyph::POWER, cx, |this, cx| this.open_guide(cx)));
        }
        right = right
            .child(div().ml(ui.u(14.)).text_size(ui.u(38.)).font_family(cm_ui::FONT).font_weight(FontWeight::MEDIUM)
                .child(ui.lifted(format!("{:02}:{:02}", self.clock.0, self.clock.1))));
        div().absolute().left(ui.u(ROW_X)).right(ui.u(70.)).top(ui.u(44.)).h(ui.u(60.)).flex().items_center().child(tabs).child(right)
    }

    fn home(&mut self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let hero = self.hero(ui, cx);
        div().absolute().top_0().left_0().size_full().child(self.row(ui, cx)).child(hero)
    }

    fn hero(&self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let logo_box = div().h(ui.u(170.)).flex().items_end().mb(ui.u(30.));
        let word = |text: &str| div().whitespace_nowrap().text_size(ui.u(86.)).font_family(cm_ui::FONT).font_weight(FontWeight::LIGHT).line_height(ui.u(92.)).child(ui.lifted(text.to_string()));
        let meta_row = |cols: Vec<(String, gpui::AnyElement)>| {
            div().flex().children(cols.into_iter().enumerate().map(|(i, (k, v))| ui.meta(i == 0, &k, v)))
        };
        let hdr = format!("{}{}", if matches!(self.session[1].rows[2].value, Value::Toggle(true)) { " · HDR" } else { "" }, if self.config.vrr_enable { " · VRR" } else { "" });
        let setup_rows = [
            (t("Tela de jogo").to_string(), self.setup_text(0, 0)),
            (t("Imagem").to_string(), if self.setup_raw(1, 0) == crate::hardware::KEEP { format!("{}{hdr}", t("Como está")) } else { format!("{} · {}{hdr}", short_res(&self.setup_raw(1, 0)), self.setup_text(1, 1)) }),
            (t("Áudio").to_string(), self.setup_text(2, 0)),
        ];
        let setup = |n: usize| setup_rows.iter().take(n).map(|(k, v)| (k.clone(), ui.lifted(v.clone()).into_any_element())).collect::<Vec<_>>();
        let sub = |text: &'static str| div().mt(ui.u(24.)).max_w(ui.u(900.)).text_size(ui.u(22.)).line_height(ui.u(31.)).text_color(color::mute()).child(ui.lifted(t(text)));

        // PS5: a white "Jogar" pill and a round "more" button (the card's options)
        let has_menu = self.has_card_menu(self.focus);
        let play_label = if matches!(self.items[self.focus], Item::Add) { t("Adicionar app") } else { t("Jogar") };
        let play_icon = if matches!(self.items[self.focus], Item::Add) { glyph::ADD } else { glyph::PLAY_ARROW };
        let mut buttons = div().flex().items_center().gap(ui.u(16.)).mt(ui.u(34.))
            .child(div().id("play").h(ui.u(64.)).px(ui.u(46.)).rounded_full().flex().items_center().gap(ui.u(10.)).cursor_pointer()
                .bg(ui.focus_fill(1., 0.89)).shadow(ui.raised(ui.lift_md(), 0.9))
                .text_size(ui.u(25.)).font_weight(FontWeight::SEMIBOLD).text_color(color::button_ink())
                .child(ui.icon(play_icon, 32., color::button_ink())).child(play_label)
                .hover(|s| s.bg(color::white(1.)))
                .on_click(cx.listener(|this, _, _, cx| this.press(Pad::A, cx))));
        if has_menu {
            buttons = buttons.child(div().id("more").size(ui.u(64.)).rounded_full().flex().items_center().justify_center().cursor_pointer()
                .bg(color::white(0.10)).shadow(ui.raised(ui.lift_sm(), 0.16)).hover(|s| s.bg(color::white(0.18)))
                .child(ui.icon(glyph::MORE_HORIZ, 32., color::ink()))
                .on_click(cx.listener(|this, _, _, cx| this.press(Pad::Y, cx))));
        }

        let body = match &self.items[self.focus] {
            Item::Game(g, dims) => {
                let logo: gpui::AnyElement = match (&g.logo, dims) {
                    (Some(p), Some((w, h))) => {
                        // contain inside 680×170, anchored bottom-left
                        let k = (680. / *w as f32).min(170. / *h as f32);
                        img(ImageSource::from(p.clone())).w(ui.u(*w as f32 * k)).h(ui.u(*h as f32 * k)).into_any_element()
                    }
                    _ => word(&g.name).into_any_element(),
                };
                let mut steam = div().flex().items_center().gap(ui.u(8.));
                if let Some(i) = &self.steam_icon { steam = steam.child(img(ImageSource::Render(sharp(i, ui, 27.))).size(ui.u(27.))); }
                let mut cols = vec![
                    (t("Última vez").to_string(), ui.lifted(ago(g.last_played, self.now)).into_any_element()),
                    (t("Abre pelo").to_string(), steam.child(ui.lifted("Steam Big Picture")).into_any_element()),
                ];
                cols.extend(setup(2));
                div().child(logo_box.child(logo)).child(meta_row(cols)).child(buttons)
            }
            Item::Launcher(l) => div().child(logo_box.child(word(&l.shown_name()))).child(meta_row(setup(3))).child(buttons).child(sub(l.sub)),
            Item::Add => div().child(logo_box.child(word(t("Adicionar app")))).child(buttons)
                .child(sub(n("Qualquer programa vira um iniciador: emulador, launcher de outra loja, um jogo solto. Ao fechar, suas telas voltam sozinhas."))),
        };
        div().absolute().left(ui.u(ROW_X)).top(ui.u(450.)).w(ui.u(1100.))
            .child(body.with_animation(("hero", self.fade), motion::out(motion::SMALL), |el, t| el.opacity(t)))
    }

    fn row(&mut self, ui: &Ui, cx: &mut Context<Self>) -> gpui::Stateful<Div> {
        self.row_view = ui.design.width - ROW_X - 60.;
        let shift = self.scroll;
        let from = self.prev_scroll;
        let ui2 = *ui;
        let track = div().absolute().top_0().flex().items_start()
            .children((0..self.items.len()).map(|i| self.card(ui, i, cx)).collect::<Vec<_>>());
        let track = track.left(ui.u(ROW_X - shift)).with_animation(("row", self.scroll_anim), motion::out(motion::FOCUS), move |el, t| el.left(ui2.u(ROW_X - (from + (shift - from) * t))));
        let max = (self.row_len() - self.row_view).max(0.);
        let arrow = |id: &'static str, g: char, dir: f32, cx: &mut Context<Self>| {
            // occlude: a click on the arrow must not reach the tile under it
            div().id(id).occlude().absolute().top(ui.u(TILE_F / 2. - 32.)).size(ui.u(64.)).rounded_full().bg(color::white(0.9)).shadow(ui.raised(ui.lift_md(), 0.6)).cursor_pointer()
                .flex().items_center().justify_center().child(ui.icon(g, 40., color::button_ink()))
                .hover(|s| s.bg(color::white(1.)))
                .on_click(cx.listener(move |this, _, _, cx| this.page(dir, cx)))
        };
        let mut row = div().id("row").absolute().left_0().top(ui.u(ROW_Y)).w_full().h(ui.u(TILE_F + 2. * RING)).child(track);
        if self.mouse_mode && self.scroll > 0.5 { row = row.child(arrow("row-left", glyph::CHEVRON_LEFT, -1., cx).left(ui.u(24.))); }
        if self.mouse_mode && self.scroll < max - 0.5 { row = row.child(arrow("row-right", glyph::CHEVRON_RIGHT, 1., cx).right(ui.u(24.))); }
        // mouse wheel scrolls the row, one tile per notch; focus stays where it is
        row
            .on_scroll(cx.listener(|this, e: &ScrollEvent, _, cx| {
                let d = match e.delta {
                    ScrollDelta::Lines(p) => if p.x.abs() > p.y.abs() { p.x } else { -p.y },
                    ScrollDelta::Pixels(p) => { let v = if p.x.abs() > p.y.abs() { p.x } else { -p.y }; f32::from(v) / 40. }
                };
                this.wheel += d;
                this.mouse_mode = true;
                while this.wheel >= 1. { this.wheel -= 1.; let to = this.scroll + STEP; this.set_scroll(to, cx); }
                while this.wheel <= -1. { this.wheel += 1.; let to = this.scroll - STEP; this.set_scroll(to, cx); }
            }))
    }

    fn card(&self, ui: &Ui, i: usize, cx: &mut Context<Self>) -> gpui::AnyElement {
        let it = &self.items[i];
        let focused = i == self.focus;
        let s = if focused { TILE_F } else { TILE };
        // corners grow with the tile: 22 on a plain tile, 30 on the focused one
        let radius = |s: f32| 22. + 8. * (s - TILE) / (TILE_F - TILE);
        let mut tile = div().relative().size(gpui::px(whole(ui, s))).rounded(ui.u(radius(s))).overflow_hidden().bg(color::panel_solid()).flex_none()
            .flex().items_center().justify_center()
            .shadow(if focused { let mut sh = ui.lift_lg(); sh.push(ui.glow(color::white(0.16), 44.)); sh } else { ui.lift_md() });
        tile = match it {
            Item::Game(g, _) => match &g.cover {
                Some(c) => tile.child(img(ImageSource::from(c.clone())).absolute().top_0().left_0().size_full().rounded(ui.u(radius(s))).object_fit(ObjectFit::Cover)),
                None => tile.p(ui.u(14.)).text_size(ui.u(18.)).font_weight(FontWeight::SEMIBOLD).child(SharedString::from(g.name.clone())),
            },
            Item::Launcher(l) => {
                let size = if focused { 96. } else { 64. };
                let icon = match &l.icon {
                    Some(i) => img(ImageSource::Render(sharp(i, ui, 96.))).size(ui.u(size)).into_any_element(),
                    None => ui.icon(glyph::SPORTS_ESPORTS, size * 0.85, color::accent()).into_any_element(),
                };
                tile.child(brand_layer(ui, l.brand, None, 0., 0., 0., 0., ui.u(radius(s)))).child(icon)
            }
            Item::Add => tile.bg(color::white(0.06)).border(ui.u(2.)).border_dashed().border_color(color::white(0.3))
                .child(ui.icon(glyph::ADD, if focused { 72. } else { 52. }, color::mute())),
        };
        // glass: light from above (stronger on the focused tile) and a hairline edge, over the art
        if !matches!(it, Item::Add) {
            tile = tile.child(div().absolute().top_0().left_0().size_full().rounded(ui.u(radius(s)))
                .bg(linear_gradient(180., linear_color_stop(color::white(if focused { 0.16 } else { 0.08 }), 0.), linear_color_stop(color::white(0.), 0.45)))
                .shadow(ui.gloss(if focused { 0.30 } else { 0.18 })));
        }
        // mouse: hovering only lights the tile up; a click focuses it, a second click plays
        let group = SharedString::from(format!("card{i}"));
        let mut slot = div().id(("card", i)).group(group.clone()).relative().flex_none().flex().items_start().cursor_pointer()
            .on_click(cx.listener(move |this, _, _, cx| this.click_card(i, cx)))
            .on_mouse_down(MouseButton::Right, cx.listener(move |this, _: &MouseDownEvent, _, cx| {
                cx.stop_propagation();
                this.right_click_card(i, cx);
            }));
        let ring_room = if focused { RING } else { 0. };
        slot = slot.mt(ui.u(RING)).ml(ui.u(GAP / 2. + ring_room + if i == self.after_add() { 26. } else { 0. })).mr(ui.u(GAP / 2.));
        let ui2 = *ui;
        let grow = if focused && self.prev_focus != i { Some((TILE, TILE_F)) } else if !focused && self.prev_focus == i && self.fade > 0 { Some((TILE_F, TILE)) } else { None };
        let tile: gpui::AnyElement = match grow {
            Some((a, b)) => tile.with_animation(("grow", self.fade * 256 + i), motion::out(motion::FOCUS), move |el, t| {
                // whole pixels, and the shrinking tile takes exactly what the growing one leaves:
                // the tiles after both keep still (fractional sizes rounded apart made them shake)
                let up = TILE + (TILE_F - TILE) * t;
                let w = if b > a { whole(&ui2, up) } else { whole(&ui2, TILE_F) + whole(&ui2, TILE) - whole(&ui2, up) };
                let s = a + (b - a) * t;
                el.size(gpui::px(w)).rounded(ui2.u(radius(s)))
            }).into_any_element(),
            None => tile.into_any_element(),
        };
        slot = slot.child(tile);
        // the focus ring, floating off the tile
        if focused {
            let ring = if self.view == View::Moving { color::accent() } else { color::focus() };
            let ring_el = div().absolute().top(ui.u(-RING)).left(ui.u(-RING)).size(ui.u(TILE_F + 2. * RING)).rounded(ui.u(38.)).border(ui.u(3.5)).border_color(ring);
            let ring_el: gpui::AnyElement = if grow.is_some() {
                ring_el.with_animation(("ring", self.fade), motion::out(motion::FOCUS), move |el, t| {
                    let s = TILE + (TILE_F - TILE) * t;
                    el.size(ui2.u(s + 2. * RING)).rounded(ui2.u(radius(s) + 8.)).opacity(t)
                }).into_any_element()
            } else { ring_el.into_any_element() };
            slot = slot.child(ring_el);
        } else {
            slot = slot.child(div().absolute().top(ui.u(-6.)).left(ui.u(-6.)).size(ui.u(s + 12.)).rounded(ui.u(radius(s) + 6.)).border(ui.u(2.))
                .border_color(gpui::transparent_black()).group_hover(group, |st| st.border_color(color::white(0.55))));
        }
        // the name under the focused tile; it may run past the tile, under the smaller ones
        if focused {
            let (name, note) = self.tile_name(it);
            let block = div().absolute().left(ui.u(2.)).top(ui.u(TILE_F + NAME_GAP)).whitespace_nowrap()
                .child(div().text_size(ui.u(28.)).font_family(cm_ui::FONT).font_weight(FontWeight::SEMIBOLD).child(ui.lifted(name)))
                .child(div().mt(ui.u(2.)).text_size(ui.u(19.)).text_color(color::mute()).child(ui.lifted(note)));
            let block: gpui::AnyElement = if grow.is_some() {
                let d = motion::dist(8.);
                block.with_animation(("name-in", self.fade), motion::out(motion::FOCUS), move |el, t| el.opacity(t).top(ui2.u(TILE_F + NAME_GAP - d * (1. - t)))).into_any_element()
            } else { block.into_any_element() };
            slot = slot.child(block);
        }
        if focused { return slot.into_any_element(); }
        match (self.view, self.exiting) {
            (View::Moving, _) => slot.opacity(0.4).into_any_element(),
            (View::CardMenu { .. }, _) => slot.with_animation(("dim", self.menu_anim * 256 + i), motion::out(motion::MENU_IN), |el, t| el.opacity(1. - 0.6 * t)).into_any_element(),
            (View::Home, Some(View::CardMenu { .. })) => slot.with_animation(("undim", self.exit_anim * 256 + i), motion::out(motion::MENU_OUT), |el, t| el.opacity(0.4 + 0.6 * t)).into_any_element(),
            _ => slot.into_any_element(),
        }
    }

    fn hints(&self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let mk = |v: Vec<(Pad, Glyph, &'static str)>| v.into_iter().map(|(p, g, l)| (p, ui.hint(g, l, None))).collect::<Vec<_>>();
        let (left, right) = if let Some((l, r)) = self.overlay_hints() {
            (mk(l), mk(r))
        } else if self.tab == 0 {
            if self.has_card_menu(self.focus) {
                (vec![(Pad::Menu, ui.hint(Glyph::Icon(glyph::MENU), t("Menu"), None))],
                 vec![(Pad::A, ui.hint(Glyph::Text("A"), t("Jogar"), None)), (Pad::Y, ui.hint(Glyph::Text("Y"), t("Opções"), None))])
            } else {
                (vec![], vec![(Pad::A, ui.hint(Glyph::Text("A"), t("Adicionar app"), None))])
            }
        } else {
            let (l, r) = self.settings_hints();
            (mk(l), mk(r))
        };
        // every hint is also a button for the mouse
        let clickable = |list: Vec<(Pad, Div)>, side: &'static str| list.into_iter().enumerate().map(|(i, (p, h))| {
            h.id((side, i)).cursor_pointer().rounded(ui.u(20.)).px(ui.u(8.)).py(ui.u(4.)).hover(|s| s.bg(color::white(0.08)))
                .on_click(cx.listener(move |this, _, _, cx| this.press(p, cx)))
        }).collect::<Vec<_>>();
        let (left, right) = (clickable(left, "hint-l"), clickable(right, "hint-r"));
        div().absolute().left(ui.u(70.)).right(ui.u(70.)).bottom(ui.u(44.)).flex().justify_between().items_center().text_size(ui.u(22.))
            .child(div().flex().items_center().gap(ui.u(40.)).children(left))
            .child(div().flex().items_center().gap(ui.u(40.)).children(right))
    }
}


/// "3840 × 2160" → "4K" for the meta row.
pub(crate) fn short_res(r: &str) -> String {
    match r { "3840 × 2160" => "4K".into(), "2560 × 1440" => "1440p".into(), "1920 × 1080" => "1080p".into(), other => shown(other).into_owned() }
}

impl Launcher {
    /// The card's name in the language in use ("Modo Xbox"); a custom app keeps its own name.
    pub fn shown_name(&self) -> SharedString { t(&self.name).to_string().into() }
}

/// When a Steam game was last played: "hoje", "ontem", "há 15 dias", "há 2 meses".
pub(crate) fn ago(last_played: u64, now: u64) -> String {
    if last_played == 0 { return t("nunca").into(); }
    let d = now.saturating_sub(last_played) / 86_400;
    match d {
        0 => t("hoje").into(),
        1 => t("ontem").into(),
        2..=30 => tn("há {0} dias", d),
        _ => { let m = (d + 15) / 30; if m == 1 { t("há 1 mês").into() } else { tn("há {0} meses", m) } }
    }
}
