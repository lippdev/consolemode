//! Phase 0 spike for Console Mode 2 Alpha (throwaway).
//! Modes (first argument), each closes itself after N seconds (second argument):
//!   home N      fullscreen Home on the primary display: Steam art background with crossfade,
//!               card row with focus, official icons read from steam.exe / Playnite / Xbox package,
//!               keyboard + controller (gilrs) navigation, UI sound on every move.
//!   fakegame N  a borderless fullscreen window standing in for a game.
//!   overlay N   the session-menu bar as a topmost PopUp window that takes focus and the controller.
//! Every line on stdout starts with "[spike]" so the runs can be read back from a log.

mod icons;
mod input;
mod sound;

use std::{path::PathBuf, sync::Arc, time::{Duration, Instant}};

use futures::StreamExt;
use gpui::{
    Animation, AnimationExt, App, Bounds, Context, FocusHandle, Hsla, ImageSource, KeyDownEvent, RenderImage,
    SharedString, Window, WindowBackgroundAppearance, WindowBounds, WindowKind, WindowOptions, div, img,
    point, prelude::*, px, rgb, rgba, size,
};

use input::Pad;

pub fn log(msg: impl AsRef<str>) {
    println!("[spike] {:>7.3}s {}", START.get().map(|s| s.elapsed().as_secs_f32()).unwrap_or(0.0), msg.as_ref());
}
static START: std::sync::OnceLock<Instant> = std::sync::OnceLock::new();

const CACHE: &str = r"C:\Program Files (x86)\Steam\appcache\librarycache";

struct Item {
    name: &'static str,
    hero: Option<PathBuf>,
    cover: Option<PathBuf>,
    icon: Option<Arc<RenderImage>>,
}

fn items() -> Vec<Item> {
    let c = |p: &str| Some(PathBuf::from(CACHE).join(p));
    let exe_icon = |p: &str| icons::exe_icon(p, 256).map(Arc::new);
    let xbox = icons::xbox_logo().map(Arc::new);
    let local = std::env::var("LOCALAPPDATA").unwrap_or_default();
    vec![
        Item { name: "Steam Big Picture", hero: c(r"4115450\1cd52ca84a3d974c4206e9cb90231956324c5999\library_hero_blur.jpg"), cover: None, icon: exe_icon(r"C:\Program Files (x86)\Steam\steam.exe") },
        Item { name: "Playnite", hero: None, cover: None, icon: exe_icon(&format!(r"{local}\Playnite\Playnite.FullscreenApp.exe")) },
        Item { name: "Modo Xbox", hero: None, cover: None, icon: xbox },
        Item { name: "Phantom Blade Zero", hero: c(r"4115450\1cd52ca84a3d974c4206e9cb90231956324c5999\library_hero.jpg"), cover: c(r"4115450\471e77854910665c03eb9a53ad06f2ac12903acd\library_capsule.jpg"), icon: None },
        Item { name: "Sekiro: Shadows Die Twice", hero: c(r"814380\library_hero.jpg"), cover: c(r"814380\library_600x900.jpg"), icon: None },
        Item { name: "The Witcher 3", hero: c(r"292030\2dbca4868f3a9912aad6966bb0b6ab2d270f6da6\library_hero.jpg"), cover: c(r"292030\dd1c19c4e41b120ddfa35734cc31f79254c25082\library_capsule.jpg"), icon: None },
        Item { name: "CONTROL Resonant", hero: c(r"3669870\6800d1c970b28815caa97e694ce811ca090f82e2\library_hero.jpg"), cover: c(r"3669870\7d4b4f430dc4d07e6562eb445456f9615cb52f77\library_capsule.jpg"), icon: None },
        Item { name: "DARK SOULS III", hero: c(r"374320\library_hero.jpg"), cover: c(r"374320\library_600x900.jpg"), icon: None },
    ]
}

// ───────────────────────── Home ─────────────────────────

struct Home {
    items: Vec<Item>,
    focus: usize,
    prev_bg: Option<PathBuf>,
    fade: usize,
    focus_handle: FocusHandle,
    rendered: bool,
    sound: Option<sound::Ui>,
}

impl Home {
    fn step(&mut self, d: isize, cx: &mut Context<Self>) {
        let n = self.items.len() as isize;
        let next = (self.focus as isize + d).clamp(0, n - 1) as usize;
        if next == self.focus { return; }
        self.prev_bg = self.bg(self.focus);
        self.focus = next;
        self.fade += 1;
        if let Some(s) = &self.sound { s.play_move(); }
        log(format!("focus -> {} ({})", next, self.items[next].name));
        cx.notify();
    }
    fn bg(&self, i: usize) -> Option<PathBuf> { self.items[i].hero.clone() }
    fn pad(&mut self, p: Pad, cx: &mut Context<Self>) {
        match p {
            Pad::Left => self.step(-1, cx),
            Pad::Right => self.step(1, cx),
            Pad::A => { if let Some(s) = &self.sound { s.play_confirm(); } log(format!("A on {}", self.items[self.focus].name)) }
            Pad::B => log("B"),
            other => log(format!("pad {other:?}")),
        }
    }
}

fn layer(path: &PathBuf) -> gpui::Img {
    img(ImageSource::from(path.clone())).absolute().top_0().left_0().size_full().object_fit(gpui::ObjectFit::Cover)
}

impl Render for Home {
    fn render(&mut self, window: &mut Window, cx: &mut Context<Self>) -> impl IntoElement {
        if !self.rendered {
            self.rendered = true;
            log(format!("first render: viewport {:?} scale {}", window.viewport_size(), window.scale_factor()));
        }
        let cur = self.items.get(self.focus).unwrap();
        let shade = Hsla::from(rgba(0x0b0d12e0));
        let mut stage = div().track_focus(&self.focus_handle).on_key_down(cx.listener(|h: &mut Home, e: &KeyDownEvent, _, cx| if let Some(p) = key_to_pad(e) { h.pad(p, cx) })).size_full().relative().bg(rgb(0x0b0d12)).font_family("Segoe UI Variable Display").text_color(rgb(0xf4f6fa));
        if let Some(p) = &self.prev_bg { stage = stage.child(layer(p)); }
        if let Some(p) = self.bg(self.focus) {
            stage = stage.child(layer(&p).with_animation(("fade", self.fade), Animation::new(Duration::from_millis(420)), |el, t| el.opacity(t)));
        }
        stage = stage.child(div().absolute().top_0().left_0().size_full().bg(gpui::linear_gradient(90., gpui::linear_color_stop(shade, 0.), gpui::linear_color_stop(Hsla::from(rgba(0x0b0d1200)), 0.7))));

        let mut row = div().absolute().left(px(110.)).top(px(590.)).flex().gap(px(18.)).items_center();
        for (i, it) in self.items.iter().enumerate() {
            let focused = i == self.focus;
            let mut card = div().w(px(168.)).h(px(252.)).rounded(px(10.)).overflow_hidden().bg(rgb(0x1c2230)).flex().flex_col().justify_between().p(px(18.));
            if let Some(c) = &it.cover { card = card.p_0().child(img(ImageSource::from(c.clone())).size_full().object_fit(gpui::ObjectFit::Cover)); }
            else {
                if let Some(icon) = &it.icon { card = card.child(img(ImageSource::Render(icon.clone())).size(px(64.))); }
                card = card.child(div().text_size(px(21.)).font_weight(gpui::FontWeight::SEMIBOLD).child(it.name));
            }
            if focused { card = card.border(px(4.)).border_color(rgb(0xffffff)); }
            row = row.child(card);
        }

        stage
            .child(div().absolute().left(px(110.)).top(px(330.)).flex().items_center().gap(px(24.))
                .children(cur.icon.clone().map(|i| img(ImageSource::Render(i)).size(px(96.))))
                .child(div().text_size(px(88.)).font_weight(gpui::FontWeight::BOLD).child(SharedString::from(cur.name))))
            .child(row)
            .child(div().absolute().right(px(70.)).top(px(46.)).text_size(px(38.)).child("Console Mode 2 Alpha · spike"))
    }
}

// ───────────────────────── fake game + overlay ─────────────────────────

struct Fake;
impl Render for Fake {
    fn render(&mut self, _w: &mut Window, _cx: &mut Context<Self>) -> impl IntoElement {
        div().size_full().bg(rgb(0x1e5a3a)).flex().items_center().justify_center().text_size(px(64.)).text_color(rgb(0xffffff)).child("JOGO FALSO (tela cheia)")
    }
}

struct Overlay { focus: usize, focus_handle: FocusHandle, sound: Option<sound::Ui> }
const GUIDE: [&str; 5] = ["Voltar ao jogo", "Janelas", "Tela e som", "Volume", "Encerrar sessão"];
impl Overlay {
    fn pad(&mut self, p: Pad, cx: &mut Context<Self>) {
        let n = GUIDE.len();
        match p {
            Pad::Left if self.focus > 0 => self.focus -= 1,
            Pad::Right if self.focus + 1 < n => self.focus += 1,
            Pad::A => { log(format!("overlay A on {}", GUIDE[self.focus])); return; }
            _ => return,
        }
        if let Some(s) = &self.sound { s.play_move(); }
        log(format!("overlay focus -> {}", GUIDE[self.focus]));
        cx.notify();
    }
}
impl Render for Overlay {
    fn render(&mut self, _w: &mut Window, cx: &mut Context<Self>) -> impl IntoElement {
        div().track_focus(&self.focus_handle).on_key_down(cx.listener(|o: &mut Overlay, e: &KeyDownEvent, _, cx| if let Some(p) = key_to_pad(e) { o.pad(p, cx) })).size_full().rounded(px(24.)).bg(rgba(0x141a24f0)).flex().items_center().justify_center().gap(px(26.))
            .font_family("Segoe UI Variable Display").text_color(rgb(0xffffff))
            .children(GUIDE.iter().enumerate().map(|(i, g)| {
                let d = div().px(px(22.)).py(px(14.)).rounded(px(40.)).text_size(px(24.)).child(*g);
                if i == self.focus { d.border(px(3.)).border_color(rgb(0xffffff)).bg(rgba(0xffffff22)) } else { d }
            }))
    }
}

// ───────────────────────── plumbing ─────────────────────────

fn quit_after(secs: u64, cx: &mut App) {
    cx.spawn(async move |cx| {
        cx.background_executor().timer(Duration::from_secs(secs)).await;
        log("time is up, quitting");
        cx.update(|cx| cx.quit());
    }).detach();
}

fn key_to_pad(e: &KeyDownEvent) -> Option<Pad> {
    Some(match e.keystroke.key.as_str() { "left" => Pad::Left, "right" => Pad::Right, "up" => Pad::Up, "down" => Pad::Down, "enter" => Pad::A, "escape" => Pad::B, _ => return None })
}

fn fullscreen(cx: &App) -> (WindowBounds, Option<gpui::DisplayId>) {
    let d = cx.primary_display().expect("no display");
    for disp in cx.displays() { log(format!("display {:?} bounds {:?}", disp.id(), disp.bounds())); }
    (WindowBounds::Fullscreen(d.bounds()), Some(d.id()))
}

fn main() {
    START.set(Instant::now()).ok();
    let args: Vec<String> = std::env::args().collect();
    let mode = args.get(1).cloned().unwrap_or_else(|| "home".into());
    let secs: u64 = args.get(2).and_then(|s| s.parse().ok()).unwrap_or(8);
    log(format!("mode {mode}, {secs}s"));

    gpui_platform::application().run(move |cx: &mut App| {
        quit_after(secs, cx);
        match mode.as_str() {
            "fakegame" => {
                let (b, d) = fullscreen(cx);
                let win = cx.open_window(WindowOptions { window_bounds: Some(b), display_id: d, titlebar: None, ..Default::default() }, |_, cx| cx.new(|_| Fake)).unwrap();
                cx.activate(true);
                win.update(cx, |_, window, _| icons::force_foreground(window)).ok();
            }
            "overlay" => {
                let d = cx.primary_display().unwrap();
                let db = d.bounds();
                let w = px(1100.); let h = px(120.);
                let origin = point(db.origin.x + (db.size.width - w) / 2., db.origin.y + db.size.height - h - px(80.));
                let t0 = Instant::now();
                let win = cx.open_window(
                    WindowOptions { window_bounds: Some(WindowBounds::Windowed(Bounds { origin, size: size(w, h) })), kind: WindowKind::PopUp, titlebar: None, focus: true, display_id: Some(d.id()), window_background: WindowBackgroundAppearance::Transparent, ..Default::default() },
                    |window, cx| {
                        let fh = cx.focus_handle();
                        fh.focus(window, cx);
                        cx.new(|_| Overlay { focus: 0, focus_handle: fh, sound: sound::Ui::open() })
                    },
                ).unwrap();
                log(format!("overlay window opened in {:?}", t0.elapsed()));
                win.update(cx, |_, window, _| icons::force_foreground(window)).ok();
                let rx = input::spawn();
                let ent = win.update(cx, |_, _, cx| cx.entity()).unwrap();
                cx.spawn(async move |cx| {
                    let mut rx = rx;
                    while let Some(p) = rx.next().await { ent.update(cx, |o, cx| o.pad(p, cx)); }
                }).detach();
            }
            _ => {
                let (b, d) = fullscreen(cx);
                let t0 = Instant::now();
                let items = items();
                log(format!("items + icons loaded in {:?} (icons: {})", t0.elapsed(), items.iter().filter(|i| i.icon.is_some()).count()));
                let win = cx.open_window(WindowOptions { window_bounds: Some(b), display_id: d, titlebar: None, focus: true, ..Default::default() }, |window, cx| {
                    let fh = cx.focus_handle();
                    fh.focus(window, cx);
                    cx.new(|_| Home { items, focus: 0, prev_bg: None, fade: 0, focus_handle: fh, rendered: false, sound: sound::Ui::open() })
                }).unwrap();
                cx.activate(true);
                win.update(cx, |_, window, _| icons::force_foreground(window)).ok();
                let rx = input::spawn();
                let ent = win.update(cx, |_, _, cx| cx.entity()).unwrap();
                cx.spawn(async move |cx| {
                    let mut rx = rx;
                    while let Some(p) = rx.next().await { ent.update(cx, |h, cx| h.pad(p, cx)); }
                }).detach();
            }
        }
    });
}
