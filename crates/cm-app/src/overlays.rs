//! What opens over the Home (mockup): the card menu (Y), moving a card, the session menu (☰) and
//! the "opening" screen. There is no session engine yet (fase 2): opening a game is a preview that
//! changes nothing, and the session menu says it is outside a session.

use std::{sync::Arc, time::Duration};

use cm_ui::{Glyph, Ui, color, glyph, motion};
use gpui::{
    AnimationExt, AppContext, Context, Div, FontWeight, ImageSource, InteractiveElement, IntoElement, ParentElement, RenderImage, SharedString,
    StatefulInteractiveElement, Styled, div, img,
};

use crate::{
    home::{Console, Item, ROW_X, ROW_Y, View, ago},
    i18n::{shown, t, tf},
    input::Pad,
    settings::Value,
    sound::Sound,
};

/// Per-game settings in the card menu ("Ao abrir este jogo"), in this order.
pub(crate) const GAME_OPTS: [(char, &str); 4] =
    [(glyph::HDR_ON, "HDR"), (glyph::ASPECT_RATIO, "Resolução"), (glyph::SPEED, "Limite de FPS"), (glyph::SKIP_NEXT, "Ir direto para o jogo")];

pub fn game_defaults() -> Vec<Value> {
    vec![
        Value::Choice(vec!["Igual à sessão", "Ligado", "Desligado"], 0),
        Value::Choice(vec!["Igual à sessão", "3840 × 2160", "2560 × 1440", "1920 × 1080"], 0),
        Value::Choice(vec!["Igual à sessão", "30", "60", "120", "Sem limite"], 0),
        Value::Choice(vec!["Ligado", "Desligado"], 0),
    ]
}

// card menu items: 0 Jogar · 1..=4 options · 5 Mover · 6 Fixar · 7 Esconder
const MENU_ITEMS: usize = 8;

pub(crate) const GUIDE: [(char, &str); 8] = [
    (glyph::PLAY_ARROW, "Voltar ao jogo"), (glyph::SELECT_WINDOW, "Janelas"), (glyph::TV, "Tela"), (glyph::VOLUME_UP, "Volume"),
    (glyph::SPEED, "FPS"), (glyph::SCREENSHOT_MONITOR, "Visão de tarefas"), (glyph::APPS, "Apps"), (glyph::POWER, "Encerrar sessão"),
];
pub(crate) const GUIDE_WINDOWS: usize = 1;
pub(crate) const GUIDE_SCREEN: usize = 2;
pub(crate) const GUIDE_VOLUME: usize = 3;
pub(crate) const GUIDE_FPS: usize = 4;
pub(crate) const GUIDE_TASK_VIEW: usize = 5;
pub(crate) const GUIDE_APPS: usize = 6;
const FPS_STEPS: [u32; 5] = [0, 30, 60, 120, 144];

impl Console {
    /// The rows of "Tela": what can be switched during the session, in order (HDR, VRR).
    pub(crate) fn screen_toggles(&self) -> Vec<(&'static str, bool)> {
        let mut v = Vec::new();
        if let Some(on) = self.session_hdr { v.push(("HDR", on)); }
        if let Some(on) = self.session_vrr { v.push(("VRR", on)); }
        v
    }

    /// The panel above the buttons for Tela, FPS and Apps (Volume and Janelas have their own).
    fn guide_panel(&self, ui: &Ui, sel: usize, cx: &mut Context<Self>) -> Option<gpui::AnyElement> {
        let w = 760.;
        let row = |i: usize, label: String, right: gpui::AnyElement, on: bool| {
            let r = div().id(("guide-row", i)).flex().items_center().justify_between().gap(ui.u(20.)).px(ui.u(18.)).h(ui.u(58.)).rounded(ui.u(12.)).text_size(ui.u(22.))
                .child(div().flex_1().overflow_hidden().whitespace_nowrap().child(SharedString::from(label)))
                .child(right);
            if on { r.bg(ui.focus_fill(0.16, 0.09)).shadow(ui.raised(ui.lift_sm(), 0.16)) } else { r }
        };
        let value = |s: String| div().text_color(color::mute()).whitespace_nowrap().child(SharedString::from(s)).into_any_element();
        let (title, rows): (String, Vec<gpui::AnyElement>) = match sel {
            GUIDE_SCREEN => {
                // the game screen as it is, then what can be switched right now
                let mode = if self.setup_raw(1, 0) == crate::hardware::KEEP { t("Como está").to_string() } else { format!("{} · {}", crate::home::short_res(&self.setup_raw(1, 0)), self.setup_text(1, 1)) };
                let mut rows = vec![
                    row(0, t("Tela de jogo").into(), value(self.setup_text(0, 0)), false).into_any_element(),
                    row(1, t("Resolução").into(), value(mode), false).into_any_element(),
                ];
                let toggles = self.screen_toggles();
                if toggles.is_empty() {
                    rows.push(row(2, t("HDR e VRR mudam aqui durante uma sessão").into(), div().into_any_element(), false).text_color(color::mute()).into_any_element());
                }
                for (k, (name, on)) in toggles.into_iter().enumerate() {
                    rows.push(row(2 + k, name.to_string(), ui.toggle(on).into_any_element(), k == self.guide_row)
                        .cursor_pointer().on_click(cx.listener(move |this, _, _, cx| { this.guide_row = k; this.toggle_screen_row(); cx.notify(); })).into_any_element());
                }
                (t("Tela").into(), rows)
            }
            GUIDE_APPS => (t("Apps").into(), vec![self.helper_panel(ui, cx)]),
            GUIDE_FPS => (t("Limite de FPS").into(), vec![
                row(0, t("Agora").into(), value(if self.session_fps == 0 { t("Sem limite").into() } else { format!("{} FPS", self.session_fps) }), true).into_any_element(),
                row(1, t("Precisa do RivaTuner (RTSS) instalado").into(), div().into_any_element(), false).text_color(color::mute()).into_any_element(),
            ]),
            _ => return None,
        };
        let (ui2, dv) = (*ui, motion::dist(12.));
        Some(div().absolute().left(ui.u((ui.design.width - w) / 2.)).bottom(ui.u(260.)).w(ui.u(w)).px(ui.u(22.)).py(ui.u(20.)).rounded(ui.u(22.)).bg(color::panel())
            .shadow(ui.raised(ui.lift_lg(), 0.14))
            .child(div().text_size(ui.u(26.)).font_weight(FontWeight::SEMIBOLD).mb(ui.u(12.)).ml(ui.u(12.)).child(SharedString::from(title)))
            .child(div().flex().flex_col().gap(ui.u(4.)).children(rows))
            .with_animation(("guide-panel", self.vol_anim), motion::out(motion::SMALL), move |el, t| el.opacity(t).bottom(ui2.u(260. - dv * (1. - t))))
            .into_any_element())
    }

    /// "Janelas", like Win + Tab: a picture of every open window with its name over it, in rows;
    /// the one ↑↓ picked has the ring.
    fn windows_grid(&self, ui: &Ui, cx: &mut Context<Self>) -> gpui::AnyElement {
        let n = self.guide_windows.len();
        let cols = Self::grid_cols(n);
        let (tw, th) = (if cols <= 2 { 520. } else if cols == 3 { 440. } else { 360. }, 0.);
        let th = if th > 0. { th } else { tw * 9. / 16. };
        let tiles = self.guide_windows.iter().enumerate().map(|(i, w)| {
            let focused = i == self.guide_row;
            // the window's live picture, drawn by Windows exactly over this box; its name shows
            // when Windows can't draw it (minimized)
            let (thumbs, src) = (self.live_thumbs.clone(), w.hwnd);
            let pic = div().size_full().relative().p(ui.u(8.))
                .child(div().absolute().top_0().left_0().size_full().flex().items_center().justify_center().text_size(ui.u(30.)).font_weight(FontWeight::SEMIBOLD).text_color(color::mute())
                    .child(SharedString::from(w.process.clone())))
                .child(gpui::canvas(|_, _, _| (), move |b, _, window, _| {
                    thumbs.borrow_mut().place(window, src, (f32::from(b.origin.x), f32::from(b.origin.y), f32::from(b.size.width), f32::from(b.size.height)));
                }).size_full());
            let frame = div().w(ui.u(tw)).h(ui.u(th)).rounded(ui.u(12.)).bg(color::white(0.05)).flex().items_center().justify_center().overflow_hidden()
                .shadow(if focused { let mut s = ui.lift_lg(); s.push(ui.glow(color::white(0.18), 30.)); s } else { ui.lift_md() })
                .child(pic);
            let ring = div().absolute().top(ui.u(-8.)).left(ui.u(-8.)).w(ui.u(tw + 16.)).h(ui.u(th + 16.)).rounded(ui.u(18.)).border(ui.u(3.5))
                .border_color(if focused { color::focus() } else { gpui::transparent_black() });
            div().id(("win", i)).flex().flex_col().gap(ui.u(10.)).cursor_pointer()
                .child(div().w(ui.u(tw)).flex().gap(ui.u(10.)).items_baseline().text_size(ui.u(20.)).whitespace_nowrap().overflow_hidden()
                    .child(div().font_weight(FontWeight::SEMIBOLD).child(SharedString::from(w.process.clone())))
                    .child(div().text_color(color::mute()).overflow_hidden().child(SharedString::from(w.title.clone()))))
                .child(div().relative().child(frame).child(ring))
                .on_click(cx.listener(move |this, _, _, cx| { this.guide_row = i; this.guide_activate(GUIDE_WINDOWS, cx); cx.notify(); }))
        }).collect::<Vec<_>>();
        let body: gpui::AnyElement = if n == 0 {
            div().text_size(ui.u(24.)).text_color(color::mute()).child(t("Nenhuma outra janela aberta")).into_any_element()
        } else {
            div().flex().flex_wrap().justify_center().gap_x(ui.u(36.)).gap_y(ui.u(30.)).max_w(ui.u(cols as f32 * (tw + 36.))).children(tiles).into_any_element()
        };
        let (ui2, dv) = (*ui, motion::dist(12.));
        div().absolute().left_0().top(ui.u(130.)).w_full().bottom(ui.u(250.)).flex().items_center().justify_center()
            .child(body)
            .with_animation(("guide-grid", self.vol_anim), motion::out(motion::SMALL), move |el, t| el.opacity(t).top(ui2.u(130. + dv * (1. - t))))
            .into_any_element()
    }

    /// Columns of the windows grid for `n` windows.
    pub(crate) fn grid_cols(n: usize) -> usize { match n { 0..=2 => n.max(1), 3 | 5 | 6 | 9 => 3, _ => 4 } }

    /// "Janelas" got focus: the live pictures may show (only for the windows listed now).
    fn show_live_thumbs(&mut self) {
        let hwnds: Vec<isize> = self.guide_windows.iter().map(|w| w.hwnd).collect();
        let mut t = self.live_thumbs.borrow_mut();
        t.retain(&hwnds);
        t.active = true;
    }

    /// A on a row of "Tela": that switch, for the session.
    fn toggle_screen_row(&mut self) {
        let Some(r) = &self.running else { return };
        let Some((name, on)) = self.screen_toggles().get(self.guide_row).copied() else { return };
        let _ = r.commands.send(if name == "HDR" { crate::engine::Command::SetHdr(!on) } else { crate::engine::Command::SetVrr(!on) });
        self.sounds.play(Sound::Move);
    }
}

fn elapsed(d: Duration) -> String {
    let m = d.as_secs() / 60;
    if m < 1 { t("menos de 1 min").into() } else if m < 60 { tf("{0} min", &[&m]) } else { tf("{0} h {1} min", &[&(m / 60), &(m % 60)]) }
}

impl Console {
    pub(crate) fn overlay_press(&mut self, p: Pad, cx: &mut Context<Self>) -> bool {
        match self.view {
            View::Home | View::Tour { .. } => return false,
            View::Confirm => {
                if let Some(r) = &self.running {
                    let yes = p == Pad::A;
                    if matches!(p, Pad::A | Pad::B) {
                        let _ = r.commands.send(crate::engine::Command::Confirmed(yes));
                        if yes { self.config.confirmed_setup = r.setup_key.clone(); self.save_config(); }
                        self.view = View::Launch { step: 3 };
                    }
                }
            }
            // the opening runs to the end (or rolls back by itself); nothing to cancel half-way
            View::Launch { .. } => {}
            View::CardMenu { sel } => match p {
                Pad::Up if sel > 0 => { self.view = View::CardMenu { sel: sel - 1 }; self.sounds.play(Sound::Move); }
                Pad::Down if sel + 1 < self.card_menu_len() => { self.view = View::CardMenu { sel: sel + 1 }; self.sounds.play(Sound::Move); }
                Pad::A => self.menu_activate(sel, cx),
                Pad::B | Pad::Y | Pad::Menu => { self.close_overlay(cx); self.sounds.play(Sound::Back); }
                _ => {}
            },
            View::Moving => match p {
                Pad::Left | Pad::Right => {
                    let to = if p == Pad::Left { self.focus.wrapping_sub(1) } else { self.focus + 1 };
                    // games move among games only; launchers and "add" stay first
                    if to < self.items.len() && ((crate::custom_apps::is_custom(&self.items[self.focus]) && crate::custom_apps::is_custom(&self.items[to])) || (matches!(self.items[self.focus], Item::Game(..)) && matches!(self.items[to], Item::Game(..)))) {
                        self.items.swap(self.focus, to);
                        self.focus = to;
                        self.sounds.play(Sound::Move);
                        let s = self.row_shift(self.focus, self.focus);
                        self.set_scroll(s, cx);
                    }
                }
                Pad::A | Pad::B => {
                    self.view = View::Home;
                    self.sounds.play(Sound::Confirm);
                    // the order the user arranged is kept
                    self.store_custom_order();
                    self.config.game_order = self.items.iter().filter_map(|i| if let Item::Game(g, _) = i { Some(g.app_id) } else { None }).collect();
                    self.save_config();
                }
                _ => {}
            },
            View::Guide { .. } if self.guide_tour.is_some() => { self.guide_tour_press(p, cx); }
            View::Guide { sel } => match p {
                Pad::Y => self.start_guide_tour(false, cx),
                Pad::Left if sel > 0 => self.guide_select(sel - 1),
                Pad::Right if sel + 1 < GUIDE.len() => self.guide_select(sel + 1),
                Pad::Up | Pad::Down if sel == GUIDE_VOLUME => {
                    let v = if p == Pad::Up { (self.volume + 5).min(100) } else { self.volume.saturating_sub(5) };
                    self.set_volume(v);
                }
                Pad::X if sel == GUIDE_VOLUME => self.cycle_output(true),
                // "Janelas": the button keeps focus; ↑↓ pick the window, A brings it up, X asks it to close
                Pad::Up | Pad::Down if sel == GUIDE_WINDOWS && !self.guide_windows.is_empty() => {
                    let n = self.guide_windows.len();
                    self.guide_row = if p == Pad::Up { (self.guide_row + n - 1) % n } else { (self.guide_row + 1) % n };
                    self.sounds.play(Sound::Move);
                }
                Pad::X if sel == GUIDE_WINDOWS => self.close_listed_window(),
                Pad::Up | Pad::Down if sel == GUIDE_SCREEN && !self.screen_toggles().is_empty() => {
                    let n = self.screen_toggles().len();
                    self.guide_row = if p == Pad::Up { (self.guide_row + n - 1) % n } else { (self.guide_row + 1) % n };
                    self.sounds.play(Sound::Move);
                }
                Pad::A if sel == GUIDE_SCREEN => self.toggle_screen_row(),
                // "Apps": ↑↓ pick ControlFS or JoyChromium, A opens it (or installs it)
                Pad::Up | Pad::Down if sel == GUIDE_APPS => {
                    self.guide_row = (self.guide_row + 1) % cm_core::helper_apps::APPS.len();
                    self.sounds.play(Sound::Move);
                }
                Pad::A if sel == GUIDE_APPS => { let i = self.guide_row; self.helper_press(i, cx); }
                Pad::Up | Pad::Down if sel == GUIDE_FPS => {
                    let i = FPS_STEPS.iter().position(|f| *f == self.session_fps).unwrap_or(0);
                    let next = if p == Pad::Up { (i + 1) % FPS_STEPS.len() } else { (i + FPS_STEPS.len() - 1) % FPS_STEPS.len() };
                    self.set_session_fps(FPS_STEPS[next]);
                }
                Pad::A => self.guide_activate(sel, cx),
                Pad::B | Pad::Menu => { self.close_overlay(cx); self.sounds.play(Sound::Back); }
                _ => {}
            },
        }
        cx.notify();
        true
    }

    pub(crate) fn has_card_menu(&self, i: usize) -> bool {
        !matches!(self.items.get(i), None | Some(Item::Add))
    }

    pub(crate) fn item_profile_id(&self, i: usize) -> Option<(u32, String)> {
        match self.items.get(i)? {
            Item::Game(g, _) => Some((g.app_id, g.name.clone())),
            Item::Launcher(l) => {
                let id = if let Some(cid) = &l.custom_id {
                    let mut h: u32 = 2166136261;
                    for b in cid.as_bytes() { h = (h ^ (*b as u32)).wrapping_mul(16777619); }
                    0xF100_0000 | (h & 0x00FF_FFFF)
                } else {
                    match l.kind {
                        cm_core::session::Launcher::BigPicture => 0xF000_0001,
                        cm_core::session::Launcher::Playnite => 0xF000_0002,
                        cm_core::session::Launcher::Xbox => 0xF000_0003,
                        cm_core::session::Launcher::Custom { .. } => 0xF000_0004,
                    }
                };
                Some((id, l.name.to_string()))
            }
            Item::Add => None,
        }
    }

    pub(crate) fn card_menu_len(&self) -> usize {
        if crate::custom_apps::is_custom(&self.items[self.focus]) { return 8; }
        match &self.items[self.focus] {
            Item::Game(..) => MENU_ITEMS,
            Item::Launcher(l) => match l.kind {
                cm_core::session::Launcher::BigPicture => 6,
                cm_core::session::Launcher::Playnite => if self.config.playnite_path.is_empty() { 5 } else { 6 },
                cm_core::session::Launcher::Xbox | cm_core::session::Launcher::Custom { .. } => 4,
            },
            Item::Add => 0,
        }
    }

    fn menu_activate(&mut self, sel: usize, cx: &mut Context<Self>) {
        if sel == 0 { return self.start_launch(cx); }
        let is_game = matches!(self.items[self.focus], Item::Game(..));
        if sel >= 1 && sel <= if is_game { 4 } else { 3 } {
            if let Some((id, name)) = self.item_profile_id(self.focus) {
                let values = self.game_opts.entry(id).or_insert_with(game_defaults);
                values[sel - 1].activate();
                let values = values.clone();
                crate::bind::store_game(&values, self.config.profile_mut(id, &name));
                self.save_config();
                self.sounds.play(Sound::Confirm);
            }
            return;
        }
        if crate::custom_apps::is_custom(&self.items[self.focus]) { return self.custom_menu_activate(sel, cx); }
        if let Item::Launcher(l) = &self.items[self.focus] {
            match (l.kind.clone(), sel) {
                (cm_core::session::Launcher::BigPicture, 4) => {
                    self.config.close_steam_on_restore = !self.config.close_steam_on_restore;
                    crate::bind::load(&mut self.session, &self.config);
                    self.save_config();
                }
                (cm_core::session::Launcher::BigPicture, 5) => {
                    let on = !self.config.show_steam_games;
                    self.config.show_steam_games = on;
                    self.show_steam_games(on);
                    for cat in &mut self.system { crate::bind::load(&mut cat.sections, &self.config); }
                    self.save_config();
                }
                (cm_core::session::Launcher::Playnite, 4) => return self.pick_playnite_exe(cx),
                (cm_core::session::Launcher::Playnite, 5) => {
                    self.config.playnite_path.clear();
                    self.save_config();
                    self.refresh_playnite_icon();
                    self.view = View::CardMenu { sel: 4 };
                }
                _ => return,
            }
            self.sounds.play(Sound::Confirm);
            return;
        }
        let Item::Game(g, _) = &self.items[self.focus] else { return };
        let id = g.app_id;
        match sel {
            5 => { self.view = View::Moving; }
            6 => {
                if !self.pinned.remove(&id) { self.pinned.insert(id); }
                self.config.pinned_games = self.pinned.iter().copied().collect();
                self.save_config();
            }
            7 => {
                if !self.config.hidden_games.contains(&id) { self.config.hidden_games.push(id); }
                self.save_config();
                self.items.remove(self.focus);
                self.focus = self.focus.min(self.items.len() - 1);
                self.prev_focus = self.focus;
                self.view = View::Home;
                self.fade += 1;
            }
            _ => {}
        }
        self.sounds.play(Sound::Confirm);
    }

    /// A button of the session menu got focus: its panel reads what it shows, fresh.
    pub(crate) fn guide_select(&mut self, sel: usize) {
        self.view = View::Guide { sel };
        self.vol_anim += 1;
        self.guide_row = 0;
        self.live_thumbs.borrow_mut().clear();
        match sel {
            GUIDE_WINDOWS => { self.guide_windows = cm_platform::session_menu_win::list_windows(); self.show_live_thumbs(); }
            GUIDE_VOLUME => { if let Some(v) = cm_platform::session_menu_win::volume() { self.volume = v; } }
            GUIDE_APPS => self.refresh_helper_rows(),
            _ => {}
        }
        self.sounds.play(Sound::Move);
    }

    fn set_volume(&mut self, v: u8) {
        self.volume = v;
        cm_platform::session_menu_win::set_volume(v);
        self.sounds.play(Sound::Move);
    }

    fn set_session_fps(&mut self, fps: u32) {
        self.session_fps = fps;
        if let Some(r) = &self.running { let _ = r.commands.send(crate::engine::Command::SetFps(fps)); }
        self.sounds.play(Sound::Move);
    }

    /// X on "Volume": the next audio output, right away (the session's restore puts the old one back).
    fn cycle_output(&mut self, up: bool) {
        let outs = &self.hardware.outputs;
        if outs.is_empty() { return; }
        let cur = cm_platform::WinDesk::new(self.paths.data_dir.clone()).audio_default();
        let i = cur.and_then(|c| outs.iter().position(|o| o.id == c)).unwrap_or(0);
        let next = if up { (i + 1) % outs.len() } else { (i + outs.len() - 1) % outs.len() };
        let _ = cm_platform::WinDesk::new(self.paths.data_dir.clone()).set_audio(&outs[next].id);
        self.sounds.play(Sound::Move);
    }

    /// The name of the default audio output.
    fn current_output_label(&self) -> String {
        let cur = cm_platform::WinDesk::new(self.paths.data_dir.clone()).audio_default();
        self.hardware.outputs.iter().find(|o| Some(&o.id) == cur.as_ref()).map(|o| o.label.to_string()).unwrap_or_else(|| "—".into())
    }

    fn close_listed_window(&mut self) {
        if let Some(w) = self.guide_windows.get(self.guide_row) {
            cm_platform::session_menu_win::request_close(w.hwnd);
            self.sounds.play(Sound::Back);
            std::thread::sleep(std::time::Duration::from_millis(250));
            self.guide_windows = cm_platform::session_menu_win::list_windows();
            self.guide_row = self.guide_row.min(self.guide_windows.len().saturating_sub(1));
            self.show_live_thumbs();
        }
    }

    fn guide_activate(&mut self, sel: usize, cx: &mut Context<Self>) {
        self.sounds.play(Sound::Confirm);
        if sel == GUIDE_WINDOWS {
            // a window to the front: the app steps aside like "Voltar ao jogo"
            if let Some(w) = self.guide_windows.get(self.guide_row).map(|w| w.hwnd) {
                self.close_overlay(cx);
                self.app_in_front = false;
                if let Some(win) = self.window { cx.defer(move |cx| { let _ = win.update(cx, |_, w, _| { w.set_background_appearance(gpui::WindowBackgroundAppearance::Opaque); crate::stage::step_aside(w); }); cm_platform::session_menu_win::activate(w); }); }
            }
            return;
        }
        if sel == GUIDE_TASK_VIEW {
            self.close_overlay(cx);
            self.app_in_front = false;
            if let Some(win) = self.window {
                cx.defer(move |cx| { let _ = win.update(cx, |_, w, _| { w.set_background_appearance(gpui::WindowBackgroundAppearance::Opaque); crate::stage::step_aside(w); }); });
                // a moment for the app to step aside, then the controller thread opens Task View and, with a PlayStation
                // pad, steers it (the pad stays with Task View until it closes), like the global shortcut
                let pads = self.pads.clone();
                cx.spawn(async move |_, cx| {
                    cx.background_executor().timer(Duration::from_millis(300)).await;
                    match pads { Some(hub) => hub.request_task_view(), None => cm_platform::session_menu_win::open_task_view() }
                }).detach();
            }
            return;
        }
        if sel == GUIDE.len() - 1 {
            // Encerrar sessão: the engine puts the desk back and sends Ended
            let leaving_home = crate::stage::is_gaming_home() || crate::gaming_home::active() == Some(true);
            self.return_to_pc(cx);
            return self.dismiss_overlay(leaving_home, cx);
        }
        if sel == 0 {
            self.close_overlay(cx);
        }
    }

    /// Back to the Home (or back to the game / desktop when closing the full-screen session menu).
    pub(crate) fn close_overlay(&mut self, cx: &mut Context<Self>) {
        self.dismiss_overlay(false, cx);
    }

    fn dismiss_overlay(&mut self, returning_to_desktop: bool, cx: &mut Context<Self>) {
        let old = self.view;
        self.view = View::Home;
        if matches!(old, View::Guide { .. }) {
            self.drop_guide_tour();
            self.live_thumbs.borrow_mut().clear();
            let in_session = self.running.is_some();
            let from_tray = std::mem::take(&mut self.guide_from_tray);
            if in_session || from_tray {
                self.app_in_front = false;
            }
            if let Some(w) = self.window {
                cx.spawn(async move |_, cx| {
                    let _ = cx.update_window(w, |_, w, _| {
                        w.set_background_appearance(gpui::WindowBackgroundAppearance::Opaque);
                        if in_session {
                            crate::stage::step_aside(w);
                        } else {
                            crate::stage::back_home(w);
                            if from_tray {
                                cm_platform::shell_win::hide_window(w);
                            }
                        }
                    });
                    if !in_session && !from_tray && !returning_to_desktop {
                        cx.background_executor().timer(Duration::from_millis(250)).await;
                        let _ = cx.update_window(w, |_, w, _| crate::stage::back_home_front(w));
                    }
                }).detach();
            }
            return cx.notify();
        }
        if !matches!(old, View::CardMenu { .. }) { return cx.notify(); }
        self.exiting = Some(old);
        self.exit_anim += 1;
        self.exit_task = Some(cx.spawn(async move |this, cx| {
            cx.background_executor().timer(Duration::from_millis(motion::MENU_OUT)).await;
            let _ = this.update(cx, |c, cx| { c.exiting = None; cx.notify(); });
        }));
        cx.notify();
    }

    pub(crate) fn open_card_menu(&mut self, cx: &mut Context<Self>) {
        if self.has_card_menu(self.focus) {
            self.view = View::CardMenu { sel: 0 };
            self.exiting = None;
            self.menu_anim += 1;
            self.sounds.play(Sound::Confirm);
            cx.notify();
        }
    }

    pub(crate) fn open_guide(&mut self, cx: &mut Context<Self>) {
        if let Some(w) = self.window {
            if !matches!(self.view, View::Guide { .. }) {
                self.guide_from_tray = w.update(cx, |_, win, _| !cm_platform::pad_win::window_visible(win)).unwrap_or(false);
            }
        }
        self.app_in_front = true;
        self.view = View::Guide { sel: 0 };
        self.exiting = None;
        self.menu_anim += 1;
        self.vol_anim += 1;
        self.sounds.play(Sound::Confirm);
        self.on_guide_opened(cx);
        if let Some(w) = self.window {
            cx.spawn(async move |this, cx| {
                let _ = cx.update_window(w, |_, w, _| {
                    cm_platform::shell_win::show_window(w);
                    w.set_background_appearance(gpui::WindowBackgroundAppearance::Blurred);
                    crate::stage::over_game(w);
                });
                cx.background_executor().timer(Duration::from_millis(150)).await;
                let still_guide = this.read_with(cx, |c, _| matches!(c.view, View::Guide { .. })).unwrap_or(false);
                if still_guide {
                    let _ = cx.update_window(w, |_, w, _| {
                        w.set_background_appearance(gpui::WindowBackgroundAppearance::Blurred);
                        crate::stage::over_game_fullscreen(w);
                    });
                }
            }).detach();
        }
        cx.notify();
    }

    /// The "opening" screen: the steps check off one by one, then the session menu shows.
    /// Jogar: builds the session plan from the config and the card, and starts the engine.
    pub(crate) fn start_launch(&mut self, cx: &mut Context<Self>) {
        if matches!(self.items[self.focus], Item::Add) { return self.add_custom_app(cx); }
        if !self.validate_custom_launch(cx) { return; }
        // a session is already running: Jogar shows its menu
        if self.running.is_some() { return self.open_guide(cx); }
        if !self.config.is_session_ready() {
            self.sounds.play(Sound::Back);
            return self.show_toast(t("Escolha a tela de jogo em Sessão antes de jogar"), cx);
        }
        let profile = self.item_profile_id(self.focus).and_then(|(id, _)| self.config.profile(id).cloned());
        let c = &self.config;
        let (launcher, steam_game) = match &self.items[self.focus] {
            Item::Launcher(l) => (l.kind.clone(), None),
            Item::Game(g, _) => {
                let direct = profile.as_ref().and_then(|p| p.direct_launch) != Some(false);
                (cm_core::session::Launcher::BigPicture, direct.then_some(g.app_id))
            }
            Item::Add => return,
        };
        let mode = profile.as_ref().and_then(|p| p.mode.clone()).or_else(|| c.monitor_modes.get(&c.focus_monitor).cloned());
        let setup_key = format!("{}|{}", c.focus_monitor, mode.as_ref().map(|m| format!("{}x{}@{}", m.width, m.height, m.frequency)).unwrap_or("current".into()));
        let mut extras = crate::engine::session_extras::Extras::from_config(c);
        if let Some(fps) = profile.as_ref().and_then(|p| p.fps_limit) {
            extras.fps_limit = fps;
        }
        let plan = crate::engine::Plan {
            focus: c.focus_monitor.clone(),
            hide: c.hide_monitors.clone(),
            mode,
            hdr: profile.as_ref().and_then(|p| p.hdr).unwrap_or(c.hdr_enable),
            vrr: c.vrr_enable,
            audio: if c.audio_auto_switch { String::new() } else { c.audio_device_id.clone() },
            launcher,
            playnite_path: c.playnite_path.clone(),
            steam_game,
            close_steam_on_restore: c.close_steam_on_restore,
            needs_confirm: c.confirmed_setup != setup_key,
            card_name: match &self.items[self.focus] { Item::Launcher(l) => l.name.to_string(), Item::Game(g, _) => g.name.clone(), Item::Add => String::new() },
            extras,
            tv: c.tv.clone(),
        };
        let desk = crate::desk::pick(&self.paths.data_dir);
        let simulated = desk.simulated();
        let journal = self.paths.data_dir.join("session.json");
        // Capture the immutable session identity for shell status.
        let (shell_focus, shell_launcher) = (plan.focus.clone(), plan.launcher.clone());
        let handle = crate::engine::start(plan, desk, journal);
        self.running = Some(crate::home::Running { started: std::time::Instant::now(), commands: handle.commands, simulated, setup_key, shell_focus, shell_launcher });
        self.view = View::Launch { step: 0 };
        self.menu_anim += 1;
        self.sounds.play(Sound::Confirm);
        let window = self.window;
        let mut events = handle.events;
        self.launch_task = Some(cx.spawn(async move |this, cx| {
            use futures::StreamExt;
            use crate::engine::Event;
            while let Some(ev) = events.next().await {
                let ended = matches!(ev, Event::Ended(_) | Event::Failed(_));
                let running = matches!(ev, Event::Running);
                let confirm = if let Event::Confirm(rect) = &ev { Some(*rect) } else { None };
                let mut return_to_pc = false;
                let _ = this.update(cx, |c, cx| {
                    match ev {
                        Event::Step(n) => c.view = View::Launch { step: n + 1 },
                        Event::Confirm(_) => { c.view = View::Confirm; c.app_in_front = true; }
                        Event::Running => {}
                        Event::Hdr(state) => c.session_hdr = state,
                        Event::Vrr(state) => c.session_vrr = state,
                        Event::Ended(why) | Event::Failed(why) => {
                            // the engine already worded it in the language in use (a custom app by its card's name)
                            c.running = None;
                            return_to_pc = c.return_to_pc_pending;
                            c.session_hdr = None;
                            c.session_vrr = None;
                            c.app_in_front = true;
                            c.view = View::Home;
                            c.toast = Some((why, std::time::Instant::now()));
                        }
                    }
                    cx.notify();
                });
                if let (Some(rect), Some(w)) = (confirm, window) {
                    // "Está vendo esta tela?" belongs on the game screen, over everything: the window goes there first, full
                    // screen a moment later (see stage.rs); the launcher stepping aside puts it away afterwards, as before
                    let _ = cx.update_window(w, |_, w, _| crate::stage::confirm_on_game_screen(w, rect));
                    cx.background_executor().timer(Duration::from_millis(200)).await;
                    let _ = cx.update_window(w, |_, w, _| crate::stage::over_game_fullscreen(w));
                }
                if running {
                    // the launcher takes the screen: the app steps aside (the session menu brings it back)
                    cx.background_executor().timer(Duration::from_millis(900)).await;
                    let _ = this.update(cx, |c, cx| { if matches!(c.view, View::Launch { .. }) { c.view = View::Home; } c.app_in_front = false; cx.notify(); });
                    if let Some(w) = window { let _ = cx.update_window(w, |_, w, _| crate::stage::step_aside(w)); }
                }
                if ended {
                    if return_to_pc {
                        let _ = this.update(cx, |c, cx| c.return_to_pc(cx));
                        break;
                    }
                    if let Some(w) = window {
                        let _ = cx.update_window(w, |_, w, _| crate::stage::back_home(w));
                        cx.background_executor().timer(Duration::from_millis(250)).await;
                        let _ = cx.update_window(w, |_, w, _| crate::stage::back_home_front(w));
                        crate::startup_log("Sessão: tentativa direta de trazer Home para frente");
                        let _ = this.update(cx, |c, cx| c.rescue_home_foreground(cx));
                    }
                    break;
                }
            }
        }));
        cx.notify();
    }

    pub(crate) fn show_toast(&mut self, text: &str, cx: &mut Context<Self>) {
        self.toast = Some((text.to_string(), std::time::Instant::now()));
        cx.notify();
        cx.spawn(async move |this, cx| {
            cx.background_executor().timer(Duration::from_secs(5)).await;
            let _ = this.update(cx, |c, cx| { if c.toast.as_ref().is_some_and(|t| t.1.elapsed() >= Duration::from_secs(5)) { c.toast = None; cx.notify(); } });
        }).detach();
    }

    pub(crate) fn toast_view(&self, ui: &Ui) -> Option<Div> {
        let (text, at) = self.toast.as_ref()?;
        if at.elapsed() > Duration::from_secs(6) { return None; }
        Some(div().absolute().top(ui.u(124.)).left_0().w_full().flex().justify_center()
            .child(div().px(ui.u(28.)).py(ui.u(14.)).rounded_full().bg(color::panel()).border(ui.u(1.)).border_color(color::white(0.1)).text_size(ui.u(22.))
                .child(SharedString::from(text.clone())))
            .with_animation(("toast", at.elapsed().as_millis() as usize / 100_000), motion::out(motion::SMALL), |el, t| el.opacity(t)))
            .map(|d| div().child(d))
    }

    /// "Can you see this screen?" on the game screen, before the launcher opens.
    pub(crate) fn confirm_view(&self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let btn = |label: &'static str, glyph: Glyph, p: Pad, cx: &mut Context<Self>| {
            ui.hint(glyph, label, None).id(label).cursor_pointer().px(ui.u(26.)).py(ui.u(14.)).rounded_full().bg(color::white(0.08)).hover(|s| s.bg(color::white(0.16)))
                .on_click(cx.listener(move |this, _, _, cx| this.press(p, cx)))
        };
        div().absolute().top_0().left_0().size_full().bg(color::stage()).flex().flex_col().items_center().justify_center().gap(ui.u(28.))
            .child(div().text_size(ui.u(56.)).font_family(cm_ui::FONT).font_weight(FontWeight::BOLD).child(t("Está vendo esta tela?")))
            .child(div().text_size(ui.u(26.)).text_color(color::mute()).child(SharedString::from(tf(
                "Se ninguém responder em {0} s, as telas voltam como estavam.", &[&crate::engine::CONFIRM_TIMEOUT.as_secs()]))))
            .child(div().flex().gap(ui.u(30.)).text_size(ui.u(24.))
                .child(btn(t("Sim, estou vendo"), Glyph::Text("A"), Pad::A, cx))
                .child(btn(t("Não, voltar"), Glyph::Text("B"), Pad::B, cx)))
    }

    // ───────────── views ─────────────

    pub(crate) fn card_menu_view(&self, ui: &Ui, sel: usize, entering: bool, cx: &mut Context<Self>) -> Div {
        let game = if let Item::Game(g, _) = &self.items[self.focus] { Some(g) } else { None };
        let app = self.custom_entry();
        let defaults = game_defaults();
        let opts = self.item_profile_id(self.focus).and_then(|(id, _)| self.game_opts.get(&id)).unwrap_or(&defaults);
        let item = |i: usize, icon: char, label: &'static str, right: gpui::AnyElement, cx: &mut Context<Self>| {
            let mut d = div().id(("menu", i)).flex().items_center().gap(ui.u(14.)).px(ui.u(18.)).py(ui.u(14.)).rounded(ui.u(12.))
                .border(ui.u(2.)).text_size(ui.u(24.)).cursor_pointer()
                .child(div().w(ui.u(40.)).child(ui.icon(icon, 28., color::accent())))
                .child(div().flex_1().child(label))
                .child(right)
                .on_click(cx.listener(move |this, _, _, cx| { if matches!(this.view, View::CardMenu { .. }) { this.view = View::CardMenu { sel: i }; this.menu_activate(i, cx); cx.notify(); } }));
            d = if i == sel { d.bg(ui.focus_fill(0.19, 0.11)).border_color(color::white(0.92)).shadow(ui.raised(ui.lift_md(), 0.2)) } else { d.border_color(gpui::transparent_black()).hover(|s| s.bg(color::white(0.07))) };
            d
        };
        let small = |t: String| div().text_size(ui.u(19.)).text_color(color::mute()).flex().items_center().child(SharedString::from(t)).into_any_element();
        let sep = || div().h(ui.u(1.)).mx(ui.u(12.)).my(ui.u(6.)).bg(color::white(0.1));

        let mut steam = div().flex().items_center().gap(ui.u(8.)).text_size(ui.u(18.)).text_color(color::mute()).mt(ui.u(4.));
        if let Some(i) = self.steam_icon.as_ref().filter(|_| game.is_some()) { steam = steam.child(img(ImageSource::Render(crate::home::sharp(i, ui, 19.))).size(ui.u(19.))); }
        let subtitle = if let Some(g) = game {
            tf("Steam · última vez {0}", &[&ago(g.last_played, self.now)])
        } else if app.is_some() {
            t("App próprio").into()
        } else if let Item::Launcher(l) = &self.items[self.focus] {
            t(l.note).into()
        } else {
            String::new()
        };
        let head = div().px(ui.u(18.)).pt(ui.u(14.)).pb(ui.u(16.)).mb(ui.u(8.)).border_b(ui.u(1.)).border_color(color::white(0.1))
            .child(div().text_size(ui.u(28.)).font_weight(FontWeight::SEMIBOLD).child(SharedString::from(match &self.items[self.focus] { Item::Launcher(l) => l.shown_name().to_string(), Item::Game(g, _) => g.name.clone(), _ => String::new() })))
            .child(steam.child(SharedString::from(subtitle)));

        let mut menu = div().flex().flex_col().child(head)
            .child(item(0, glyph::PLAY_ARROW, t("Jogar"), div().into_any_element(), cx));
        let opt_count = if game.is_some() { GAME_OPTS.len() } else { 3 };
        menu = menu.child(sep());
        if game.is_some() {
            menu = menu.child(div().text_size(ui.u(15.)).font_weight(FontWeight::SEMIBOLD).text_color(color::mute()).px(ui.u(18.)).pt(ui.u(6.)).pb(ui.u(4.)).child(SharedString::from(t("Ao abrir este jogo").to_uppercase())));
        }
        for (k, (icon, label)) in GAME_OPTS.iter().take(opt_count).enumerate() {
            let v = &opts[k];
            let text = shown(v.text().unwrap_or("")).into_owned();
            let changed = matches!(v, Value::Choice(_, i) if *i != 0);
            let r = div().text_size(ui.u(19.)).text_color(if changed { color::accent() } else { color::mute() }).child(SharedString::from(text)).into_any_element();
            menu = menu.child(item(1 + k, *icon, t(label), r, cx));
        }
        if let Some(app) = app {
            menu = menu.child(sep())
                .child(item(4, glyph::SETTINGS, t("Argumentos"), small(app.arguments.clone()), cx))
                .child(item(5, glyph::SETTINGS, t("Processo a acompanhar"), small(app.watch_process.clone()), cx))
                .child(sep())
                .child(item(6, glyph::SWAP_HORIZ, t("Mover na fileira"), div().into_any_element(), cx))
                .child(item(7, glyph::VISIBILITY_OFF, t("Remover da Início"), div().into_any_element(), cx));
        } else if let Some(g) = game {
            let pin = if self.pinned.contains(&g.app_id) { t("Ligado") } else { t("Desligado") };
            menu = menu.child(sep())
                .child(item(5, glyph::SWAP_HORIZ, t("Mover na fileira"), div().into_any_element(), cx))
                .child(item(6, glyph::PUSH_PIN, t("Fixar na Início"), small(pin.into()), cx))
                .child(item(7, glyph::VISIBILITY_OFF, t("Esconder da Início"), div().into_any_element(), cx));
        } else if let Item::Launcher(l) = &self.items[self.focus] {
            match l.kind {
                cm_core::session::Launcher::BigPicture => {
                    let close_lbl = if self.config.close_steam_on_restore { t("Ligado") } else { t("Desligado") };
                    let games_lbl = if self.config.show_steam_games { t("Ligado") } else { t("Desligado") };
                    menu = menu.child(sep())
                        .child(item(4, glyph::POWER, t("Fechar o iniciador junto"), small(close_lbl.into()), cx))
                        .child(item(5, glyph::SPORTS_ESPORTS, t(crate::settings::SHOW_STEAM_GAMES), small(games_lbl.into()), cx));
                }
                cm_core::session::Launcher::Playnite => {
                    let path_lbl = if self.config.playnite_path.is_empty() {
                        t("Automático").to_string()
                    } else {
                        self.config.playnite_path.rsplit(['\\', '/']).next().unwrap_or(&self.config.playnite_path).to_string()
                    };
                    menu = menu.child(sep())
                        .child(item(4, glyph::FOLDER, t("Escolher app"), small(path_lbl), cx));
                    if !self.config.playnite_path.is_empty() {
                        menu = menu.child(item(5, glyph::CHECK_CIRCLE, t("Automático"), div().into_any_element(), cx));
                    }
                }
                cm_core::session::Launcher::Xbox | cm_core::session::Launcher::Custom { .. } => {}
            }
        }

        // beside the held tile and its name, at the row's top
        let card_right = ROW_X - self.scroll + self.focus_right();
        let left = (card_right + 30.).min(ui.design.width - 560. - 70.);
        let panel = div().id("card-menu").occlude().absolute().left(ui.u(left)).top(ui.u(ROW_Y)).w(ui.u(560.)).p(ui.u(12.))
            .rounded(ui.u(18.)).bg(color::panel()).border(ui.u(1.)).border_color(color::white(0.08)).shadow(ui.raised(ui.lift_lg(), 0.14))
            .child(menu);
        let (ui2, d) = (*ui, motion::dist(16.));
        let panel: gpui::AnyElement = if entering {
            panel.with_animation(("menu-in", self.menu_anim), motion::out(motion::MENU_IN), move |el, t| el.opacity(t).left(ui2.u(left - d * (1. - t)))).into_any_element()
        } else {
            panel.with_animation(("menu-out", self.exit_anim), motion::out(motion::MENU_OUT), move |el, t| el.opacity(1. - t).left(ui2.u(left - d * 0.5 * t))).into_any_element()
        };
        let mut layer = div().absolute().top_0().left_0().size_full();
        // a click anywhere else closes the menu
        if entering {
            layer = layer.child(div().id("card-menu-away").absolute().top_0().left_0().size_full()
                .on_click(cx.listener(|this, _, _, cx| { this.close_overlay(cx); this.sounds.play(Sound::Back); })));
        }
        layer.child(panel)
    }

    /// The item being opened/played: its logo (games) or icon and name (launchers).
    fn item_logo(&self, ui: &Ui, max_w: f32, max_h: f32) -> gpui::AnyElement {
        match &self.items[self.focus] {
            Item::Game(g, Some((w, h))) if g.logo.is_some() => {
                let k = (max_w / *w as f32).min(max_h / *h as f32);
                img(ImageSource::from(g.logo.clone().unwrap())).w(ui.u(*w as f32 * k)).h(ui.u(*h as f32 * k)).into_any_element()
            }
            Item::Game(g, _) => div().text_size(ui.u(64.)).font_family(cm_ui::FONT).font_weight(FontWeight::BOLD).child(SharedString::from(g.name.clone())).into_any_element(),
            Item::Launcher(l) => div().flex().items_center().gap(ui.u(20.)).text_size(ui.u(64.)).font_family(cm_ui::FONT).font_weight(FontWeight::BOLD)
                .children(l.icon.clone().map(|i: Arc<RenderImage>| img(ImageSource::Render(crate::home::sharp(&i, ui, 72.))).size(ui.u(72.))))
                .child(l.shown_name()).into_any_element(),
            Item::Add => div().into_any_element(),
        }
    }

    fn item_name(&self) -> String {
        match &self.items[self.focus] { Item::Game(g, _) => g.name.clone(), Item::Launcher(l) => l.shown_name().to_string(), Item::Add => String::new() }
    }

    /// Current session setup from Sessão (so the screens tell the truth about it), in Portuguese
    /// as the rows hold it: for comparisons. To show it, use [`Self::setup_text`].
    pub(crate) fn setup_raw(&self, section: usize, row: usize) -> String {
        // the game screen without its "· desligada" note
        if (section, row) == (0, 0) { return self.hardware.focus_label(&self.config); }
        match &self.session[section].rows[row].value {
            Value::Choice(o, i) => o[*i].to_string(),
            Value::Toggle(on) => if *on { "Ligado".into() } else { "Desligado".into() },
            value => value.text().unwrap_or("").to_string(),
        }
    }

    /// The same, in the language in use.
    pub(crate) fn setup_text(&self, section: usize, row: usize) -> String { shown(&self.setup_raw(section, row)).into_owned() }

    pub(crate) fn launch_view(&self, ui: &Ui, step: usize) -> Div {
        let hdr = if matches!(self.session[1].rows[2].value, Value::Toggle(true)) { format!(" · {}", t("HDR ligado")) } else { String::new() };
        let opening = if matches!(self.items[self.focus], Item::Game(..)) { tf("Abrindo {0} pelo Steam Big Picture", &[&self.item_name()]) } else { tf("Abrindo {0}", &[&self.item_name()]) };
        let steps = [
            tf("{0} como tela de jogo · {1} · {2}", &[&self.setup_text(0, 0), &crate::home::short_res(&self.setup_raw(1, 0)), &self.setup_text(1, 1)]),
            tf("Outras telas: {0}", &[&self.setup_text(0, 1).to_lowercase()]),
            format!("{}{hdr}", tf("Áudio: {0}", &[&self.setup_text(2, 0)])),
            opening,
        ];
        let list = div().absolute().left(ui.u((ui.design.width - 620.) / 2.)).top(ui.u(610.)).w(ui.u(620.)).flex().flex_col().gap(ui.u(14.)).text_size(ui.u(24.))
            .children(steps.into_iter().enumerate().map(|(i, t)| {
                let done = i < step;
                let row = div().flex().items_center().gap(ui.u(16.)).text_color(if done { color::ink() } else { color::dim() })
                    .child(ui.icon(glyph::CHECK_CIRCLE, 28., if done { color::accent() } else { color::dim() }))
                    .child(SharedString::from(t));
                if done && i + 1 == step {
                    row.with_animation(("step", self.menu_anim * 8 + i), motion::out(motion::SMALL), |el, t| el.opacity(0.4 + 0.6 * t)).into_any_element()
                } else { row.into_any_element() }
            }));
        let (ui2, d) = (*ui, motion::dist(16.));
        div().absolute().top_0().left_0().size_full()
            .child(div().absolute().top(ui.u(330.)).left_0().w_full().h(ui.u(200.)).flex().items_end().justify_center().child(self.item_logo(ui, 760., 200.))
                .with_animation(("launch-logo", self.menu_anim), motion::out(motion::LAUNCH), move |el, t| el.opacity(t).top(ui2.u(330. + d * (1. - t)))))
            .child(list)
            .child(div().absolute().top(ui.u(60.)).left_0().w_full().flex().justify_center()
                .child(div().px(ui.u(24.)).py(ui.u(12.)).rounded_full().bg(color::panel()).text_size(ui.u(20.)).text_color(color::mute())
                    .child(if self.running.as_ref().is_some_and(|r| r.simulated) { t("Simulação: nesta alpha as telas, o áudio e o HDR ainda não mudam") } else { t("Preparando a sessão") })))
    }

    pub(crate) fn guide_view(&self, ui: &Ui, sel: usize, entering: bool, cx: &mut Context<Self>) -> gpui::AnyElement {
        let via = match &self.items[self.focus] { Item::Game(..) => "Steam Big Picture".to_string(), Item::Launcher(l) => l.shown_name().to_string(), Item::Add => String::new() };
        let hdr = if matches!(self.session[1].rows[2].value, Value::Toggle(true)) { " · HDR" } else { "" };
        let meta = div().flex().mt(ui.u(26.))
            .child(match &self.running {
                Some(r) => ui.meta(true, t("Jogando há"), SharedString::from(elapsed(r.started.elapsed()))),
                None => ui.meta(true, t("Estado"), SharedString::from(t("Fora da sessão"))),
            })
            .child(ui.meta(false, t("Pelo"), SharedString::from(via)))
            .child(ui.meta(false, t("Tela"), SharedString::from(format!("{}{hdr}", self.setup_text(0, 0)))));
        let now = div().absolute().left(ui.u(crate::home::ROW_X)).top(ui.u(150.)).child(self.item_logo(ui, 760., 160.)).child(meta);

        let (ui2, d) = (*ui, motion::dist(24.));
        let buttons = div().absolute().left_0().bottom(ui.u(120.)).w_full().flex().justify_center().gap(ui.u(26.))
            .children(GUIDE.iter().enumerate().map(|(i, (icon, label))| {
                let danger = i == GUIDE.len() - 1;
                let mut b = div().id(("guide", i)).relative().size(ui.u(84.)).rounded_full().flex().items_center().justify_center().cursor_pointer()
                    .border(ui.u(3.)).child(ui.icon(*icon, 40., if danger { color::accent() } else { color::ink() }))
                    .on_click(cx.listener(move |this, _, _, cx| { if matches!(this.view, View::Guide { .. }) && this.guide_tour.is_none() { if this.view != (View::Guide { sel: i }) { this.guide_select(i); } else { this.guide_activate(i, cx); } cx.notify(); } }));
                if i == sel {
                    b = b.border_color(color::focus()).bg(ui.focus_fill(0.17, 0.09)).shadow(ui.raised(ui.lift_md(), 0.2))
                        .child(div().absolute().bottom(ui.u(104.)).left(ui.u(-120.)).w(ui.u(324.)).flex().justify_center()
                            .text_size(ui.u(22.)).font_weight(FontWeight::SEMIBOLD).whitespace_nowrap().child(t(label)));
                } else {
                    // round glass buttons, as the Início top bar
                    b = b.border_color(gpui::transparent_black()).bg(color::white(0.07)).shadow(ui.raised(ui.lift_sm(), 0.12)).hover(|s| s.bg(color::white(0.14)));
                }
                b
            }));

        let menu_anim = self.menu_anim;
        let fade_in = move |el: Div, key: &'static str| -> gpui::AnyElement {
            if entering { el.with_animation((key, menu_anim), motion::out(motion::MODAL_IN), |el, t| el.opacity(t)).into_any_element() } else { el.into_any_element() }
        };
        let buttons: gpui::AnyElement = if entering {
            buttons.with_animation(("guide-buttons", menu_anim), motion::out(motion::MODAL_IN), move |el, t| el.opacity(t).bottom(ui2.u(120. - d * (1. - t)))).into_any_element()
        } else { buttons.into_any_element() };
        let mut layer = div().absolute().top_0().left_0().size_full()
            .child(fade_in(div().absolute().top_0().left_0().size_full().bg(color::shade(0.72)), "guide-scrim"))
            // the windows grid takes the screen: what is running steps back
            .children((sel != GUIDE_WINDOWS || self.guide_tour.is_some()).then(|| fade_in(now, "guide-now")));
        // the tour dims all but the buttons
        if self.guide_tour.is_some() { layer = layer.child(div().absolute().top_0().left_0().size_full().bg(color::shade(0.5))); }
        layer = layer.child(buttons)
            .child(div().absolute().right(ui.u(70.)).top(ui.u(40.)).h(ui.u(64.)).flex().items_center().gap(ui.u(26.)).text_size(ui.u(38.)).font_family(cm_ui::FONT)
                .children(self.battery_chip(ui, 22., 30.))
                .child(SharedString::from(format!("{:02}:{:02}", self.clock.0, self.clock.1))));
        if sel == GUIDE_VOLUME && self.guide_tour.is_none() {
            let w = 760.;
            let fill = self.volume as f32 / 100.;
            let bar = div().id("volume-bar").flex_1().h(ui.u(8.)).rounded(ui.u(4.)).bg(color::white(0.2)).relative().cursor_pointer()
                .child(div().absolute().top_0().left_0().h_full().w(ui.u((w - 60. - 200.) * fill)).rounded(ui.u(4.)).bg(color::accent()));
            let dv = motion::dist(12.);
            layer = layer.child(div().absolute().left(ui.u((ui.design.width - w) / 2.)).bottom(ui.u(260.)).w(ui.u(w)).px(ui.u(30.)).py(ui.u(26.))
                .rounded(ui.u(14.)).bg(color::panel())
                .child(div().text_size(ui.u(26.)).font_weight(FontWeight::SEMIBOLD).mb(ui.u(14.)).child(t("Volume")))
                .child(div().flex().items_center().gap(ui.u(18.)).text_size(ui.u(22.))
                    .child(div().id("vol-down").cursor_pointer().child(ui.icon(glyph::VOLUME_DOWN, 30., color::ink()))
                        .on_click(cx.listener(|this, _, _, cx| { let v = this.volume.saturating_sub(5); this.set_volume(v); cx.notify(); })))
                    .child(bar)
                    .child(div().id("vol-up").cursor_pointer().child(ui.icon(glyph::VOLUME_UP, 30., color::ink()))
                        .on_click(cx.listener(|this, _, _, cx| { let v = (this.volume + 5).min(100); this.set_volume(v); cx.notify(); })))
                    .child(div().w(ui.u(44.)).child(SharedString::from(self.volume.to_string()))))
                // the output, switched with X (the session's restore puts the old one back)
                .child(div().id("vol-out").mt(ui.u(18.)).pt(ui.u(16.)).border_t(ui.u(1.)).border_color(color::white(0.1)).flex().items_center().justify_between().gap(ui.u(20.)).text_size(ui.u(22.)).cursor_pointer()
                    .child(div().flex().items_center().gap(ui.u(12.)).child(ui.icon(glyph::SPEAKER, 28., color::mute())).child(t("Saída de áudio")))
                    .child(div().flex().items_center().gap(ui.u(12.)).text_color(color::mute()).whitespace_nowrap()
                        .child(SharedString::from(self.current_output_label()))
                        .child(ui.pad_button(Glyph::Text("X"))))
                    .on_click(cx.listener(|this, _, _, cx| { this.cycle_output(true); cx.notify(); })))
                .with_animation(("volume", self.vol_anim), motion::out(motion::SMALL), move |el, t| el.opacity(t).bottom(ui2.u(260. - dv * (1. - t)))));
        }
        if self.guide_tour.is_none() { if let Some(panel) = self.guide_panel(ui, sel, cx) { layer = layer.child(panel); } }
        if sel == GUIDE_WINDOWS && self.guide_tour.is_none() { layer = layer.child(self.windows_grid(ui, cx)); }
        if let Some(card) = self.guide_tour_card(ui, cx) { layer = layer.child(card); }
        if entering { layer.into_any_element() } else {
            layer.with_animation(("guide-out", self.exit_anim), motion::out(motion::MODAL_OUT), |el, t| el.opacity(1. - t)).into_any_element()
        }
    }

    pub(crate) fn overlay_hints(&self) -> Option<(Vec<(Pad, Glyph, &'static str)>, Vec<(Pad, Glyph, &'static str)>)> {
        Some(match self.view {
            View::Home | View::Tour { .. } => return None,
            View::Launch { .. } => (vec![], vec![]),
            View::Confirm => (vec![], vec![]),
            View::CardMenu { .. } => (vec![(Pad::B, Glyph::Text("B"), t("Fechar"))], vec![(Pad::A, Glyph::Text("A"), t("Selecionar"))]),
            View::Moving => (vec![(Pad::B, Glyph::Text("B"), t("Concluir"))], vec![(Pad::Left, Glyph::Icon(glyph::SWAP_HORIZ), t("Mover"))]),
            View::Guide { .. } if self.guide_tour.is_some() => (vec![], vec![]),
            View::Guide { sel } => (
                vec![(Pad::B, Glyph::Text("B"), t("Fechar")), (Pad::Y, Glyph::Text("Y"), t("Tour"))],
                match sel {
                    GUIDE_VOLUME => vec![(Pad::Up, Glyph::Icon(glyph::VOLUME_UP), t("Volume ↑↓")), (Pad::X, Glyph::Text("X"), t("Saída de áudio"))],
                    GUIDE_WINDOWS if !self.guide_windows.is_empty() => vec![(Pad::Up, Glyph::Icon(glyph::SELECT_WINDOW), t("Escolher janela")), (Pad::X, Glyph::Text("X"), t("Fechar janela")), (Pad::A, Glyph::Text("A"), t("Ir para a janela"))],
                    GUIDE_WINDOWS => vec![],
                    GUIDE_FPS => vec![(Pad::Up, Glyph::Icon(glyph::SPEED), t("Limite ↑↓"))],
                    GUIDE_SCREEN if !self.screen_toggles().is_empty() => vec![(Pad::A, Glyph::Text("A"), t("Mudar"))],
                    GUIDE_SCREEN => vec![],
                    GUIDE_APPS => vec![(Pad::Up, Glyph::Icon(glyph::APPS), t("Escolher app")), (Pad::A, Glyph::Text("A"), t("Abrir"))],
                    _ => vec![(Pad::A, Glyph::Text("A"), t("Selecionar"))],
                },
            ),
        })
    }
}
