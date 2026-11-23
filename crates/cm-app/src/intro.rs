//! The opening (1.7 BootIntro): the logo over the interface while it loads, in the style picked in
//! Sistema → Aparência (the app's own, or in the mood of the Xbox 360's or the PS4's), with its
//! sound. Any press, click or key skips it. The rules (styles, which one, how long) are in
//! `cm_core::boot_intro`; this draws them. Each frame is painted from the time since it started,
//! so one animation drives the whole scene; with reduced motion the scene is drawn as it ends.
//!
//! Light is drawn with GPU blurred shapes (drop shadows with nothing on top): real soft glows,
//! no banding. The timings follow the sounds: the 360 flash lands on the last hit of its sound
//! (≈3.8 s), the PS4 logo on the swell of its chime (≈0.6 s), the app's own on the strike at 0.
//!
//! Development: `CM2_INTRO_STYLE=x360|ps4|console` and `CM2_INTRO_AT=<ms>` freeze one frame.

use std::{f32::consts::TAU, time::Duration};

use cm_core::boot_intro::{self as rules, PS1, PS2, PS3, PS4, X360, XBOX, XONE};

#[path = "intro_more.rs"]
mod more;
use cm_ui::{Ui, motion};
use gpui::{
    Animation, AnimationExt, App, BorderStyle, Bounds, BoxShadow, Context, Corners, Edges, FontWeight, Hsla, InteractiveElement, IntoElement, LineCap, LineJoin, MouseButton,
    ParentElement, PathBuilder, PathStyle, SharedString, Styled, StrokeOptions, TextAlign, TextRun, Window, canvas, div, linear_color_stop, linear_gradient, point, px, quad, rgb,
    size, transparent_black,
};

use crate::{home::Console, sound::Sound};

/// The opening on screen.
pub struct Intro {
    pub style: &'static str,
    pub run: usize,
    /// It is fading out (skipped, or its time is up).
    pub leaving: bool,
}

/// How long the fade out over the interface takes (1.7: 380 ms).
const LEAVE_MS: u64 = 380;

/// Development: one frame of the opening, held (`CM2_INTRO_AT`).
fn frozen_at() -> Option<f32> { std::env::var("CM2_INTRO_AT").ok()?.trim().parse().ok() }

impl Console {
    /// The app opened: show the opening if it is on (and the window is for using), with its sound.
    pub(crate) fn begin_intro(&mut self, window_shown: bool, cx: &mut Context<Self>) {
        if !rules::should_show(self.config.intro_animation, window_shown) { return; }
        let roll = (std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).map(|d| d.subsec_nanos()).unwrap_or(0) % 10_000) as f64 / 10_000.;
        let forced = std::env::var("CM2_INTRO_STYLE").ok();
        let style = match forced { Some(s) => rules::normalize(Some(&s)), None => rules::pick_for_opening(self.config.intro_style.as_deref(), roll) };
        self.start_intro(style, self.config.intro_sound && frozen_at().is_none(), cx);
    }

    /// Sistema → Aparência → "Ver a abertura": plays the style picked there now, as if the app had just opened.
    pub(crate) fn preview_intro(&mut self, cx: &mut Context<Self>) {
        if !self.config.intro_animation { return; }
        let style = rules::normalize(self.config.intro_style.as_deref());
        self.start_intro(style, self.config.intro_sound, cx);
    }

    fn start_intro(&mut self, style: &'static str, sound: bool, cx: &mut Context<Self>) {
        self.intro_run += 1;
        let run = self.intro_run;
        self.intro = Some(Intro { style, run, leaving: false });
        crate::startup_log(&format!("abertura: estilo {style}"));
        if rules::should_play_sound(true, sound) { self.sounds.play(Sound::boot_of(style)); }
        if frozen_at().is_some() { cx.notify(); return; }
        let stays = rules::minimum_shown(Some(style));
        cx.spawn(async move |this, cx| {
            cx.background_executor().timer(stays).await;
            let _ = this.update(cx, |c, cx| c.leave_intro(run, cx));
        }).detach();
        cx.notify();
    }

    /// A press, a click or a key on the opening. True when there was one to skip.
    pub(crate) fn skip_intro(&mut self, cx: &mut Context<Self>) -> bool {
        let Some(run) = self.intro.as_ref().filter(|i| !i.leaving).map(|i| i.run) else { return self.intro.is_some() };
        self.sounds.stop_boot();
        self.leave_intro(run, cx);
        true
    }

    /// Fades it out over the interface, which is ready underneath (the first-run tour too).
    fn leave_intro(&mut self, run: usize, cx: &mut Context<Self>) {
        let Some(i) = self.intro.as_mut().filter(|i| i.run == run && !i.leaving) else { return };
        i.leaving = true;
        cx.spawn(async move |this, cx| {
            cx.background_executor().timer(Duration::from_millis(LEAVE_MS)).await;
            let _ = this.update(cx, |c, cx| { if c.intro.as_ref().is_some_and(|i| i.run == run) { c.intro = None; cx.notify(); } });
        }).detach();
        cx.notify();
    }

    /// The overlay, over everything.
    pub(crate) fn intro_view(&self, ui: &Ui, cx: &mut Context<Self>) -> Option<gpui::AnyElement> {
        let i = self.intro.as_ref()?;
        let (style, run) = (i.style, i.run);
        let total = rules::minimum_shown(Some(style)).as_millis() as f32;
        let ui = *ui;
        let scene = move |ms: f32| div().size_full().child(canvas(|_, _, _| (), move |bounds, _, window, cx| paint(window, cx, bounds, &ui, style, ms)).size_full());
        // with Windows' animations off the scene is drawn as it ends
        let scene: gpui::AnyElement = if let Some(at) = frozen_at() { scene(at).into_any_element() } else if motion::reduced() { scene(total + 5000.).into_any_element() } else {
            div().size_full().with_animation(("intro", run), Animation::new(Duration::from_millis(total as u64)), move |el, t| el.child(scene(t * total))).into_any_element()
        };
        let bg = match style { X360 => rgb(0xdfe4e2), PS1 => rgb(0xf6f6f4), PS2 => rgb(0x03060f), XBOX => rgb(0x010401), PS3 | PS4 | XONE => rgb(0x000000), _ => rgb(0x07090d) };
        let root = div().id("intro").absolute().top_0().left_0().size_full().bg(Hsla::from(bg)).occlude().overflow_hidden()
            .on_mouse_down(MouseButton::Left, cx.listener(|this, _, _, cx| { this.skip_intro(cx); }))
            .on_mouse_down(MouseButton::Right, cx.listener(|this, _, _, cx| { this.skip_intro(cx); }))
            .child(scene);
        Some(if i.leaving {
            root.with_animation(("intro-out", run), motion::out(LEAVE_MS), |el, t| el.opacity(1. - t)).into_any_element()
        } else { root.into_any_element() })
    }
}

// ───────────── drawing helpers ─────────────

fn c(hex: u32, alpha: f32) -> Hsla { let mut h: Hsla = rgb(hex).into(); h.a = alpha.clamp(0., 1.); h }
fn lerp(a: f32, b: f32, t: f32) -> f32 { a + (b - a) * t }
/// Between two colours.
fn mix(a: u32, b: u32, t: f32) -> u32 {
    let ch = |v: u32, s: u32| ((v >> s) & 0xff) as f32;
    let one = |s: u32| (lerp(ch(a, s), ch(b, s), t.clamp(0., 1.)).round() as u32) << s;
    one(16) | one(8) | one(0)
}
/// 0..1 progress of something that starts at `start` ms and takes `dur` ms.
fn seg(ms: f32, start: f32, dur: f32) -> f32 { ((ms - start) / dur).clamp(0., 1.) }
fn ease_in_out(x: f32) -> f32 { if x < 0.5 { 4. * x * x * x } else { 1. - (-2. * x + 2.).powi(3) / 2. } }
fn ease_out(x: f32) -> f32 { 1. - (1. - x).powi(3) }
fn ease_in(x: f32) -> f32 { x * x * x }
/// 0 → 1 → 0 over the window, peaking at `peak` (0..1 of it).
fn bell(ms: f32, start: f32, dur: f32, peak: f32) -> f32 {
    let x = seg(ms, start, dur);
    if x <= 0. || x >= 1. { 0. } else if x < peak { ease_out(x / peak) } else { 1. - ease_in_out((x - peak) / (1. - peak)) }
}

/// A soft light: a disc blurred on the GPU (a drop shadow with nothing drawn on top).
fn blob(window: &mut Window, cx: f32, cy: f32, r: f32, color: Hsla, blur: f32) {
    if r <= 0.2 || color.a <= 0.003 { return; }
    let b = Bounds::new(point(px(cx - r), px(cy - r)), size(px(2. * r), px(2. * r)));
    window.paint_drop_shadows(b, Corners::all(px(r)), &[BoxShadow { color, offset: point(px(0.), px(0.)), blur_radius: px(blur.max(0.5)), spread_radius: px(0.), inset: false }]);
}

/// A soft rectangle (light streaks, flashes).
fn soft_rect(window: &mut Window, x: f32, y: f32, w: f32, h: f32, color: Hsla, blur: f32) {
    if w <= 0. || h <= 0. || color.a <= 0.003 { return; }
    let b = Bounds::new(point(px(x), px(y)), size(px(w), px(h)));
    window.paint_drop_shadows(b, Corners::all(px(h.min(w) / 2.)), &[BoxShadow { color, offset: point(px(0.), px(0.)), blur_radius: px(blur.max(0.5)), spread_radius: px(0.), inset: false }]);
}

fn disc(window: &mut Window, cx: f32, cy: f32, r: f32, color: Hsla) {
    if r <= 0.3 || color.a <= 0.003 { return; }
    let bounds = Bounds::new(point(px(cx - r), px(cy - r)), size(px(2. * r), px(2. * r)));
    window.paint_quad(quad(bounds, Corners::all(px(r)), color, Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
}

fn round_rect(window: &mut Window, x: f32, y: f32, w: f32, h: f32, r: f32, color: Hsla) {
    if color.a <= 0.003 { return; }
    window.paint_quad(quad(Bounds::new(point(px(x), px(y)), size(px(w), px(h))), Corners::all(px(r)), color, Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
}

fn stroke(window: &mut Window, pts: &[(f32, f32)], width: f32, color: Hsla) {
    if color.a <= 0.003 { return; }
    stroke_with(window, pts, width, color);
}

/// A stroke in any paint (a gradient too).
fn stroke_with(window: &mut Window, pts: &[(f32, f32)], width: f32, color: impl Into<gpui::Background>) {
    if pts.len() < 2 || width <= 0.05 { return; }
    let mut b = PathBuilder::stroke(px(width)).with_style(PathStyle::Stroke(StrokeOptions { line_width: width, line_cap: LineCap::Round, line_join: LineJoin::Round, miter_limit: 4. }));
    b.move_to(point(px(pts[0].0), px(pts[0].1)));
    for p in &pts[1..] { b.line_to(point(px(p.0), px(p.1))); }
    if let Ok(path) = b.build() { window.paint_path(path, color); }
}

fn fill(window: &mut Window, pts: &[(f32, f32)], color: impl Into<gpui::Background>) {
    if pts.len() < 3 { return; }
    let mut b = PathBuilder::fill();
    b.move_to(point(px(pts[0].0), px(pts[0].1)));
    for p in &pts[1..] { b.line_to(point(px(p.0), px(p.1))); }
    b.close();
    if let Ok(path) = b.build() { window.paint_path(path, color); }
}

/// Text drawn in the scene, so it fades and moves with it. Returns its width.
fn text(window: &mut Window, cx: &mut App, s: &str, size_px: f32, weight: FontWeight, color: Hsla, x: f32, y: f32, centred: bool) -> f32 {
    let mut font = gpui::font(cm_ui::FONT);
    font.weight = weight;
    let run = TextRun { len: s.len(), font, color, background_color: None, underline: None, strikethrough: None };
    let line = window.text_system().shape_line(SharedString::from(s.to_string()), px(size_px), &[run], None);
    let w = f32::from(line.width);
    if color.a > 0.003 {
        let x = if centred { x - w / 2. } else { x };
        let _ = line.paint(point(px(x), px(y)), px(size_px * 1.2), TextAlign::Left, None, window, cx);
    }
    w
}

fn text_width(window: &mut Window, s: &str, size_px: f32, weight: FontWeight) -> f32 {
    let mut font = gpui::font(cm_ui::FONT);
    font.weight = weight;
    let run = TextRun { len: s.len(), font, color: c(0, 0.), background_color: None, underline: None, strikethrough: None };
    f32::from(window.text_system().shape_line(SharedString::from(s.to_string()), px(size_px), &[run], None).width)
}

/// The outline of a rounded rectangle, clockwise from the end of its top-left corner.
fn rounded_rect_points(x: f32, y: f32, w: f32, h: f32, r: f32) -> Vec<(f32, f32)> {
    let mut pts = vec![(x + r, y), (x + w - r, y)];
    let arc = |pts: &mut Vec<(f32, f32)>, cx: f32, cy: f32, from: f32| {
        for i in 1..=10 { let a = (from + 90. * i as f32 / 10.).to_radians(); pts.push((cx + r * a.cos(), cy + r * a.sin())); }
    };
    arc(&mut pts, x + w - r, y + r, -90.);
    pts.push((x + w, y + h - r));
    arc(&mut pts, x + w - r, y + h - r, 0.);
    pts.push((x + r, y + h));
    arc(&mut pts, x + r, y + h - r, 90.);
    pts.push((x, y + r));
    arc(&mut pts, x + r, y + r, 180.);
    pts
}

/// The first `fraction` of a polyline's length: a line drawing itself.
fn partial(pts: &[(f32, f32)], fraction: f32) -> Vec<(f32, f32)> {
    let len = |a: (f32, f32), b: (f32, f32)| ((b.0 - a.0).powi(2) + (b.1 - a.1).powi(2)).sqrt();
    let total: f32 = pts.windows(2).map(|w| len(w[0], w[1])).sum();
    let mut left = total * fraction.clamp(0., 1.);
    let mut out = vec![pts[0]];
    for w in pts.windows(2) {
        let l = len(w[0], w[1]);
        if left >= l { out.push(w[1]); left -= l; } else {
            if l > 0. && left > 0. { let t = left / l; out.push((lerp(w[0].0, w[1].0, t), lerp(w[0].1, w[1].1, t))); }
            break;
        }
    }
    out
}

/// Small deterministic random numbers, so every opening looks the same.
struct Rand(u32);
impl Rand {
    fn next(&mut self) -> f32 { self.0 = self.0.wrapping_mul(1_664_525).wrapping_add(1_013_904_223); (self.0 >> 8) as f32 / (1u32 << 24) as f32 }
}

// ───────────── the logo ─────────────

/// What the logo is made of, in its own 256 × 256 box (assets/icon.ico).
/// `sx` squeezes it across, as when it turns on a sphere.
struct Logo { x: f32, y: f32, k: f32, sx: f32 }

impl Logo {
    /// The logo `size` px wide, centred on (cx, cy).
    fn centred(cx: f32, cy: f32, size: f32) -> Logo { Logo { x: cx - size / 2., y: cy - size / 2., k: size / 256., sx: 1. } }
    /// The same, squeezed across to `sx` of its width around its centre.
    fn squeezed(self, sx: f32) -> Logo { let cx = self.x + 128. * self.k; Logo { x: cx - 128. * self.k * sx, sx, ..self } }
    fn at(&self, vx: f32, vy: f32) -> (f32, f32) { (self.x + vx * self.k * self.sx, self.y + vy * self.k) }
    fn pts(&self, v: &[(f32, f32)]) -> Vec<(f32, f32)> { v.iter().map(|p| self.at(p.0, p.1)).collect() }
}

const GREEN: u32 = 0x33d69f;

/// The colours of the logo's parts.
#[derive(Clone, Copy)]
struct Ink {
    /// The base under the screen, and the screen's frame.
    body: u32,
    /// The dash and the power sign on the base.
    detail: u32,
    /// The prompt and cursor inside the screen.
    prompt: u32,
}

fn power_arc(logo: &Logo) -> Vec<(f32, f32)> {
    (0..=24).map(|i| { let t = lerp(236.3, -56.3, i as f32 / 24.).to_radians(); logo.at(216. + 18. * t.cos(), 214.97 + 18. * t.sin()) }).collect()
}

/// The finished logo at some opacity.
fn draw_logo(window: &mut Window, logo: &Logo, ink: Ink, a: f32) {
    if a <= 0.003 { return; }
    draw_logo_with(window, logo, c(ink.body, a).into(), c(ink.detail, a).into(), c(ink.prompt, a).into());
}

/// The logo in any paint (gradients too): its body (base and screen frame), the details on the
/// base, and the prompt in the screen.
fn draw_logo_with(window: &mut Window, logo: &Logo, body: gpui::Background, detail: gpui::Background, prompt: gpui::Background) {
    let k = logo.k;
    let (bx, by) = logo.at(8., 182.);
    window.paint_quad(quad(Bounds::new(point(px(bx), px(by)), size(px(240. * k * logo.sx), px(64. * k))), Corners::all(px(16. * k * logo.sx.min(1.))), body, Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
    stroke_with(window, &logo.pts(&[(30., 214.), (50., 214.)]), 12. * k, detail);
    let screen = logo.pts(&rounded_rect_points(22., 14., 212., 156., 30.));
    let mut closed = screen.clone();
    closed.push(screen[0]);
    stroke_with(window, &closed, 18. * k, body);
    stroke_with(window, &power_arc(logo), 10. * k, detail);
    stroke_with(window, &logo.pts(&[(216., 190.), (216., 212.)]), 10. * k, detail);
    stroke_with(window, &logo.pts(&[(64., 62.), (104., 88.), (64., 114.)]), 14. * k, prompt);
    stroke_with(window, &logo.pts(&[(118., 124.), (168., 124.)]), 14. * k, prompt);
}

// ───────────── the scenes ─────────────

/// The scene, by style, `ms` after the opening started.
fn paint(window: &mut Window, cx: &mut App, bounds: Bounds<gpui::Pixels>, ui: &Ui, style: &str, ms: f32) {
    let s = Scene {
        w: f32::from(bounds.size.width), h: f32::from(bounds.size.height),
        ox: f32::from(bounds.origin.x), oy: f32::from(bounds.origin.y),
        u: f32::from(ui.u(1.)),
    };
    let ms = scene_ms(style, ms);
    match style {
        X360 => x360(window, cx, &s, ms),
        PS4 => ps4(window, cx, &s, ms),
        PS1 => more::ps1(window, cx, &s, ms),
        PS2 => more::ps2(window, cx, &s, ms),
        PS3 => more::ps3(window, cx, &s, ms),
        XBOX => more::xbox(window, cx, &s, ms),
        XONE => more::xone(window, cx, &s, ms),
        _ => own(window, cx, &s, ms),
    }
}

/// The scenes were drawn on their own clock; this keeps their big moment on the sound's
/// (Xbox: the burst at 6 s). The others are drawn on the sound's clock.
fn scene_ms(style: &str, ms: f32) -> f32 {
    match style {
        XBOX => remap(ms, &[(0., 0.), (6000., 3300.), (8000., 5300.)]),
        _ => ms,
    }
}

/// Piecewise linear: real time → the scene's time, through the given points (past the last, 1:1).
fn remap(ms: f32, pts: &[(f32, f32)]) -> f32 {
    for w in pts.windows(2) {
        let ((a, va), (b, vb)) = (w[0], w[1]);
        if ms <= b { return va + (vb - va) * ((ms - a) / (b - a)).clamp(0., 1.); }
    }
    let (a, va) = *pts.last().unwrap();
    va + (ms - a)
}

struct Scene { w: f32, h: f32, ox: f32, oy: f32, u: f32 }
impl Scene {
    fn cx(&self) -> f32 { self.ox + self.w / 2. }
    fn cy(&self) -> f32 { self.oy + self.h / 2. }
    fn diag(&self) -> f32 { (self.w * self.w + self.h * self.h).sqrt() }
    fn full(&self, window: &mut Window, color: Hsla) { round_rect(window, self.ox, self.oy, self.w, self.h, 0., color); }
}

// ── in the mood of the Xbox 360: green ribbons of light swirl in 3D, collapse into a glowing orb,
// a white flash on the sound's last hit, and the logo in a glossy orb with the name beside it.

const X_GREEN: u32 = 0x7ad12a;
const X_DEEP: u32 = 0x4aa516;
const X_FLASH: f32 = 3720.;
/// The sound comes in (≈0.67 s): the bands sweep in with it.
const X_IN: f32 = 650.;

struct Ribbon { inc: f32, node: f32, phase: f32, span: f32, radius: f32, speed: f32, width: f32, silver: bool }

fn ribbons() -> Vec<Ribbon> {
    let mut r = Rand(360);
    (0..11).map(|i| Ribbon {
        inc: 0.35 + r.next() * 1.0,
        node: r.next() * TAU,
        phase: r.next() * TAU,
        span: 1.1 + r.next() * 1.5,
        radius: 0.7 + r.next() * 0.6,
        speed: 0.75 + r.next() * 0.55,
        width: 9. + r.next() * 13.,
        silver: i % 4 == 3,
    }).collect()
}

/// A point of a ribbon's orbit: screen offset from the centre and its depth (-1 back … 1 front).
fn orbit(rb: &Ribbon, angle: f32, radius: f32) -> (f32, f32, f32) {
    let (x, y0) = (angle.cos(), angle.sin());
    let (y, z) = (y0 * rb.inc.cos(), y0 * rb.inc.sin());
    let (xs, ys) = (x * rb.node.cos() - y * rb.node.sin(), x * rb.node.sin() + y * rb.node.cos());
    // perspective: the near side is larger
    let p = 1. / (1. - z * 0.32);
    (xs * radius * p, ys * radius * p, z)
}

/// One wide band of light in 3D: a strip on a tilted orbit around the orb, twisting as it goes, so
/// it is wide where it faces the camera and a thin bright line where it turns edge-on. Each run of
/// it on one side of the orb is filled as one smooth shape (no seams), in layers: a green glow, the
/// translucent band, a lighter middle and a white-hot core, with fine rims along its edges.
fn light_band(window: &mut Window, s: &Scene, rb: &Ribbon, ms: f32, alpha: f32, front: bool, centre: (f32, f32), radius: f32, width: f32) {
    if alpha <= 0.01 { return; }
    const N: usize = 54;
    let t = ms / 1000.;
    let run_t = seg(ms, X_IN, X_FLASH - X_IN);
    // faster and faster as it closes in
    let head = rb.phase + rb.speed * TAU * (0.3 * run_t + 1.45 * run_t.powf(2.3)) + t * 0.15;
    let span = rb.span * lerp(1.8, 1.1, run_t);
    let (si, ci) = rb.inc.sin_cos();
    let (sn, cn) = (rb.node + 0.22 * t).sin_cos();
    let rot = |v: (f32, f32, f32)| -> (f32, f32, f32) {
        let (y, z) = (v.1 * ci - v.2 * si, v.1 * si + v.2 * ci);
        (v.0 * cn - y * sn, v.0 * sn + y * cn, z)
    };
    // the camera: a little perspective, the near side larger
    let cam = s.h * 2.4;
    let project = |p: (f32, f32, f32)| -> Option<(f32, f32)> {
        let k = cam / (cam - p.2);
        if !(0.2..=4.0).contains(&k) { return None; }
        Some((centre.0 + p.0 * k, centre.1 + p.1 * k))
    };
    // per point: centre, the two edges on screen, and which side of the orb it is on
    let mut pts: Vec<((f32, f32), (f32, f32), (f32, f32), bool)> = Vec::with_capacity(N + 1);
    for i in 0..=N {
        let u = i as f32 / N as f32;
        let a = head - span * (1. - u);
        let (sa, ca) = a.sin_cos();
        let r = radius * rb.radius * (1. + 0.14 * (a * 1.6 + rb.phase).sin());
        let rad = rot((ca, sa, 0.));
        let nrm = rot((0., 0., 1.));
        let tw = rb.phase * 2. + a * 1.15 + t * 1.4;
        let (st, ct) = tw.sin_cos();
        let d = (nrm.0 * ct + rad.0 * st, nrm.1 * ct + rad.1 * st, nrm.2 * ct + rad.2 * st);
        // thin at the tail, full past the middle, rounded off at the head
        let taper = u.powf(0.8) * (1. - ((u - 0.88) / 0.12).max(0.).powi(2));
        let hw = width * taper;
        let c3 = (rad.0 * r, rad.1 * r, rad.2 * r);
        let (Some(c2), Some(e1), Some(e2)) = (project(c3), project((c3.0 + d.0 * hw, c3.1 + d.1 * hw, c3.2 + d.2 * hw)), project((c3.0 - d.0 * hw, c3.1 - d.1 * hw, c3.2 - d.2 * hw))) else { continue };
        pts.push((c2, e1, e2, c3.2 >= 0.));
    }
    // each band its own green, from deep to yellow-green; a few are white light
    let hue = (rb.phase * 7.).rem_euclid(1.);
    let (col, core) = if rb.silver { (0xc9d6cc, 0xffffff) } else if hue > 0.8 { (0xd6e01a, 0xfdffe0) } else { (mix(0x2f8f12, 0x9fd615, hue / 0.8), 0xf4ffd6) };
    let mut i = 0;
    while i < pts.len() {
        if pts[i].3 != front { i += 1; continue; }
        let start = i;
        while i < pts.len() && pts[i].3 == front { i += 1; }
        let run = &pts[start.saturating_sub(1)..(i + 1).min(pts.len())];
        if run.len() < 2 { continue; }
        // the glow around it
        for p in run.iter().step_by(4) {
            let w = ((p.1.0 - p.2.0).powi(2) + (p.1.1 - p.2.1).powi(2)).sqrt() / 2.;
            blob(window, p.0.0, p.0.1, w * 1.3 + 4. * s.u, c(if rb.silver { 0xffffff } else { X_GREEN }, 0.16 * alpha), w * 1.6 + 8. * s.u);
        }
        // nested layers, lighter toward the middle: a soft gradient across the band
        const LAYERS: usize = 7;
        for l in 0..LAYERS {
            let f = l as f32 / (LAYERS - 1) as f32;
            let k = lerp(1., 0.12, f);
            let side = |e: usize| -> Vec<(f32, f32)> { run.iter().map(|p| { let q = if e == 1 { p.1 } else { p.2 }; (lerp(p.0.0, q.0, k), lerp(p.0.1, q.1, k)) }).collect() };
            let mut poly = side(1);
            poly.extend(side(2).into_iter().rev());
            fill(window, &poly, c(mix(col, core, f.powf(1.6)), (if l == 0 { 0.62 } else { 0.34 }) * alpha));
        }
        // the fine rims
        let rim = if rb.silver { 0xa9b8ad } else { mix(col, 0x1f6a0a, 0.5) };
        stroke(window, &run.iter().map(|p| p.1).collect::<Vec<_>>(), 1.2 * s.u, c(rim, 0.3 * alpha));
        stroke(window, &run.iter().map(|p| p.2).collect::<Vec<_>>(), 1.2 * s.u, c(rim, 0.3 * alpha));
    }
}

/// A glossy silver orb, lit from the top left.
fn silver_orb(window: &mut Window, x: f32, y: f32, r: f32, a: f32) {
    if a <= 0.003 || r <= 0.5 { return; }
    let b = Bounds::new(point(px(x - r), px(y - r)), size(px(2. * r), px(2. * r)));
    window.paint_quad(quad(b, Corners::all(px(r)), linear_gradient(160., linear_color_stop(c(0xfdfefd, a), 0.), linear_color_stop(c(0x868f8a, a), 1.)), Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
    blob(window, x + r * 0.25, y + r * 0.32, r * 0.55, c(0x6d7671, 0.35 * a), r * 0.4);
    blob(window, x - r * 0.3, y - r * 0.38, r * 0.3, c(0xffffff, 0.95 * a), r * 0.18);
    // a thin rim of light
    let rim: Vec<(f32, f32)> = (0..=48).map(|i| { let t = i as f32 / 48. * TAU; (x + r * 0.985 * t.cos(), y + r * 0.985 * t.sin()) }).collect();
    stroke(window, &rim, (r * 0.03).max(1.), c(0xffffff, 0.3 * a));
}

fn x360(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let (mx, my, u) = (s.cx(), s.cy(), s.u);
    // the light room: grey at the edges, white in the middle, out of a dim grey before the sound
    s.full(window, c(0xc3cbc6, 1.));
    blob(window, mx, my, s.diag() * 0.36, c(0xffffff, 1.), s.diag() * 0.24);
    let close = ease_in_out(seg(ms, X_IN, X_FLASH - X_IN));
    // a green light gathering at the centre as they close in
    blob(window, mx, my, s.h * lerp(0.6, 0.25, close), c(X_GREEN, 0.18 * seg(ms, X_IN, 1500.) + 0.25 * close), s.h * 0.3);
    s.full(window, c(0x2a2e2c, 0.7 * (1. - ease_out(seg(ms, 0., X_IN)))));

    let bands_a = seg(ms, X_IN - 120., 260.) * (1. - seg(ms, X_FLASH - 140., 180.));
    let rbs = ribbons();
    // they sweep in close to the camera, wide, and wind in onto the orb
    let radius = s.h * lerp(1.15, 0.16, close);
    let width = s.h * lerp(0.09, 0.035, close);
    let orb_r = s.h * lerp(0.05, 0.085, ease_in(seg(ms, X_IN, X_FLASH - X_IN)));
    if bands_a > 0. { for rb in &rbs { light_band(window, s, rb, ms, bands_a, false, (mx, my), radius, width * rb.width / 16.); } }
    let orb_a = seg(ms, X_IN, 500.) * (1. - seg(ms, X_FLASH, 120.));
    blob(window, mx, my, orb_r * 1.7, c(X_GREEN, (0.25 + 0.5 * close) * orb_a), orb_r * 1.2);
    silver_orb(window, mx, my, orb_r, orb_a);
    blob(window, mx, my, orb_r * 0.9, c(0xf4ffe0, 0.8 * orb_a * ease_in(seg(ms, X_FLASH - 900., 900.))), orb_r * 0.6);
    if bands_a > 0. { for rb in &rbs { light_band(window, s, rb, ms, bands_a, true, (mx, my), radius, width * rb.width / 16.); } }
    // the burst just before the flash
    let burst = seg(ms, X_FLASH - 380., 380.);
    blob(window, mx, my, s.h * lerp(0.05, 0.7, ease_in(burst)), c(0xffffff, burst * (1. - seg(ms, X_FLASH, 150.))), s.h * 0.22);

    // after the flash: the grey-green room with a warm glow in the middle, the logo in a silver orb,
    // the name under it, and a ring of light running out from it
    if ms >= X_FLASH {
        let room = ease_in_out(seg(ms, X_FLASH + 150., 1100.));
        s.full(window, c(0xe9eee6, 1.));
        s.full(window, c(0x98aa88, room));
        blob(window, mx, my, s.h * 0.4, c(0xf2fcd0, 1.), s.h * 0.3);
        blob(window, mx, my - s.h * 0.02, s.h * 0.2, c(0xd9f59a, 0.7), s.h * 0.2);
        blob(window, mx, my - s.h * 0.06, s.h * 0.12, c(0xffffff, 0.85), s.h * 0.12);
        let settle = ease_out(seg(ms, X_FLASH, 1100.));
        let scale = lerp(1.1, 1., settle);
        let d = s.h * 0.21 * scale;
        let (ocx, ocy) = (mx, my - s.h * 0.08);
        // the ring
        let ring = seg(ms, X_FLASH + 60., 1300.);
        if ring > 0. && ring < 1. {
            let rr = s.h * lerp(0.1, 0.62, ease_out(ring));
            let pts: Vec<(f32, f32)> = (0..=72).map(|i| { let t = i as f32 / 72. * TAU; (ocx + rr * t.cos(), ocy + s.h * 0.04 + rr * 0.86 * t.sin()) }).collect();
            let ra = (1. - ring).powf(0.7);
            stroke(window, &pts, 22. * u, c(X_GREEN, 0.3 * ra));
            stroke(window, &pts, 6. * u, c(0xf2ffd8, 0.95 * ra));
        }
        blob(window, ocx, ocy, d * 0.7, c(X_GREEN, 0.4), d * 0.5);
        silver_orb(window, ocx, ocy, d / 2., 1.);
        draw_logo(window, &Logo::centred(ocx, ocy + d * 0.02, d * 0.58), Ink { body: X_DEEP, detail: 0xeef3ec, prompt: X_DEEP }, 1.);
        blob(window, ocx - d * 0.13, ocy - d * 0.3, d * 0.13, c(0xffffff, 0.7), d * 0.06);
        // the name: the first word green, the second grey, thin
        let size_px = s.h * 0.095 * scale;
        let (w1, w2) = (text_width(window, "Console", size_px, FontWeight::NORMAL), text_width(window, " Mode", size_px, FontWeight::LIGHT));
        let x0 = mx - (w1 + w2) / 2.;
        let ny = ocy + d / 2. + s.h * 0.025;
        text(window, cx, "Console", size_px, FontWeight::NORMAL, c(0x7cc21f, 1.), x0, ny, false);
        text(window, cx, " Mode", size_px, FontWeight::LIGHT, c(0x8f9790, 1.), x0 + w1, ny, false);
    }
    // the flash
    let flash = if ms < X_FLASH { ease_in(seg(ms, X_FLASH - 200., 200.)) } else { 1. - ease_out(seg(ms, X_FLASH + 90., 700.)) };
    s.full(window, c(0xffffff, flash));
}

fn ps4(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let (mx, my) = (s.cx(), s.cy());
    let (w, h, ox, oy, u) = (s.w, s.h, s.ox, s.oy, s.u);
    window.paint_quad(quad(Bounds::new(point(px(ox), px(oy)), size(px(w), px(h))), Corners::all(px(0.)),
        linear_gradient(170., linear_color_stop(c(0x0b3a9c, 1.), 0.), linear_color_stop(c(0x020c2e, 1.), 1.)), Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
    // the haze of light
    blob(window, ox + w * 0.42, oy + h * 0.42, w * 0.32, c(0x2f73e0, 0.55), w * 0.3);
    blob(window, ox + w * 0.75, oy + h * 0.7, w * 0.2, c(0x1b4fb8, 0.4), w * 0.22);

    let t = ms / 1000.;
    // the waves: glossy bands, a bright edge on each
    for k in 0..5 {
        let kf = k as f32;
        let base = h * (0.5 + 0.065 * kf);
        let amp = h * (0.07 + 0.025 * (k % 2) as f32);
        let lambda = w * (1.15 + 0.22 * kf);
        let phase = kf * 1.7 + t * (0.45 + 0.12 * kf);
        let top = |x: f32| oy + base + amp * (TAU * x / lambda + phase).sin() + 0.35 * amp * (TAU * x / (0.47 * lambda) - phase * 1.3 + kf).sin();
        let thick = |x: f32| h * (0.018 + 0.06 * (0.5 + 0.5 * (TAU * x / (0.8 * lambda) + kf * 2.1 + t * 0.6).sin()));
        let xs: Vec<f32> = (0..=64).map(|i| i as f32 / 64. * w).collect();
        let upper: Vec<(f32, f32)> = xs.iter().map(|&x| (ox + x, top(x))).collect();
        let mut band = upper.clone();
        band.extend(xs.iter().rev().map(|&x| (ox + x, top(x) + thick(x))));
        fill(window, &band, linear_gradient(180., linear_color_stop(c(0x9cc6ff, 0.13 + 0.02 * kf), 0.), linear_color_stop(c(0x5a96ff, 0.02), 1.)));
        stroke(window, &upper, 7. * u, c(0x8dbbff, 0.06));
        stroke(window, &upper, 1.4 * u, c(0xd2e6ff, 0.38 - 0.04 * kf));
        // a light running along the crest
        if k % 2 == 1 {
            let x = ((t * 0.09 + kf * 0.31) % 1.) * w;
            blob(window, ox + x, top(x), 5. * u, c(0xffffff, 0.55), 10. * u);
        }
    }
    // bokeh drifting up
    let mut r = Rand(4);
    for _ in 0..36 {
        let (x0, y0, size_r, a, sp) = (r.next() * w, r.next() * h, r.next(), 0.05 + r.next() * 0.25, 0.3 + r.next());
        let rad = (2.5 + 26. * size_r * size_r) * u;
        let x = ox + (x0 + t * 14. * u * sp) % w;
        let y = oy + (y0 - t * 22. * u * sp).rem_euclid(h);
        let twinkle = 0.65 + 0.35 * (t * 2.2 + x0).sin();
        blob(window, x, y, rad, c(0xd6e8ff, a * twinkle), rad * 0.7 + 2. * u);
    }
    // the logo with the chime, glowing white, and the streak of light across it
    let logo_size = 210. * u;
    let name_px = 64. * u;
    let block = logo_size + 36. * u + name_px;
    let (lx, ly) = (mx, my - block / 2. + logo_size / 2.);
    let la = ease_out(seg(ms, 380., 900.));
    blob(window, lx, ly, logo_size * 0.55, c(0xcfe2ff, 0.28 * la), logo_size * 0.5);
    let scale = lerp(0.94, 1., ease_out(seg(ms, 380., 1400.)));
    draw_logo(window, &Logo::centred(lx, ly, logo_size * scale), Ink { body: 0xffffff, detail: 0x1546a8, prompt: 0xffffff }, la);
    let streak = bell(ms, 300., 1500., 0.35);
    let len = w * lerp(0.15, 1.1, ease_out(seg(ms, 300., 1100.)));
    soft_rect(window, mx - len / 2., ly - 1.5 * u, len, 3. * u, c(0xbfdcff, 0.55 * streak), 14. * u);
    soft_rect(window, mx - len * 0.3, ly - 0.6 * u, len * 0.6, 1.2 * u, c(0xffffff, 0.9 * streak), 2. * u);
    blob(window, mx, ly, 26. * u, c(0xffffff, 0.6 * streak), 30. * u);
    let na = ease_out(seg(ms, 700., 800.));
    text(window, cx, "Console Mode", name_px, FontWeight::NORMAL, c(0xffffff, na), mx, ly + logo_size / 2. + 36. * u - 4. * u * (1. - na), true);
    // out of black
    s.full(window, c(0x000000, 1. - ease_out(seg(ms, 0., 650.))));
}

// ── Console Mode's own: in the dark, the power light comes on, the screen draws itself and
// powers up like a CRT, the prompt is typed, and the name rises under a green glow.

fn own(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let (mx, my, u) = (s.cx(), s.cy(), s.u);
    s.full(window, c(0x07090d, 1.));
    let logo_size = 200. * u;
    let name_px = 66. * u;
    let block = logo_size + 34. * u + name_px;
    let (lx, ly) = (mx, my - block / 2. + logo_size / 2.);
    let logo = Logo::centred(lx, ly, logo_size);
    let k = logo.k;
    // the room lights up green behind it
    let room = ease_out(seg(ms, 1300., 1400.));
    blob(window, mx, my, s.h * 0.42, c(0x1fa57c, 0.42 * room), s.h * 0.36);
    blob(window, lx, ly, logo_size * 0.7, c(GREEN, 0.18 * room), logo_size * 0.6);
    let ink = 0x2a3036;

    // the power light: on at once (the strike of the sound), then breathing
    let (pcx, pcy) = logo.at(216., 214.97);
    let power_a = seg(ms, 60., 140.);
    let breathe = if ms < 2400. { 0.55 + 0.45 * (ms / 260.).sin().abs() } else { 0.5 };
    blob(window, pcx, pcy, 30. * k, c(GREEN, 0.75 * power_a * breathe), 34. * k);
    // the base
    let (bx, by) = logo.at(8., 182.);
    let base_a = ease_out(seg(ms, 380., 420.));
    round_rect(window, bx, by + (1. - base_a) * 10. * k, 240. * k, 64. * k, 16. * k, c(ink, base_a));
    stroke(window, &logo.pts(&[(30., 214.), (50., 214.)]), 12. * k, c(GREEN, seg(ms, 650., 200.)));
    stroke(window, &power_arc(&logo), 10. * k, c(GREEN, power_a));
    stroke(window, &logo.pts(&[(216., 190.), (216., 212.)]), 10. * k, c(GREEN, power_a));

    // the screen draws itself, a light running at its tip
    let screen = logo.pts(&rounded_rect_points(22., 14., 212., 156., 30.));
    let mut closed = screen.clone();
    closed.push(screen[0]);
    let draw = ease_in_out(seg(ms, 600., 650.));
    if draw > 0. {
        let drawn = partial(&closed, draw);
        stroke(window, &drawn, 18. * k, c(ink, 1.));
        if draw < 1. { let tip = *drawn.last().unwrap(); blob(window, tip.0, tip.1, 9. * k, c(GREEN, 0.9), 14. * k); }
    }
    // it powers up like a CRT: a line across the middle, opening to a green flash, then dark
    let (sx, sy) = logo.at(31., 23.);
    let (sw, sh) = (194. * k, 138. * k);
    let open = ease_out(seg(ms, 1280., 260.));
    let crt = bell(ms, 1220., 900., 0.25);
    if crt > 0. {
        let line_w = sw * ease_out(seg(ms, 1220., 120.));
        let hh = lerp(2. * k, sh, open);
        round_rect(window, sx + (sw - line_w) / 2., sy + (sh - hh) / 2., line_w, hh, 18. * k * open, c(GREEN, 0.55 * crt));
        soft_rect(window, sx + (sw - line_w) / 2., sy + sh / 2. - 2. * k, line_w, 4. * k, c(0xeafff6, crt * (1. - open * 0.7)), 6. * k);
    }
    // the prompt is typed, the cursor blinks
    let chevron = logo.pts(&[(64., 62.), (104., 88.), (64., 114.)]);
    let typed = ease_in_out(seg(ms, 1750., 260.));
    if typed > 0. {
        stroke(window, &partial(&chevron, typed), 14. * k, c(GREEN, 1.));
        blob(window, logo.at(84., 88.).0, logo.at(84., 88.).1, 26. * k, c(GREEN, 0.22 * typed), 24. * k);
    }
    let cursor_drawn = ease_in_out(seg(ms, 2010., 160.));
    let blink_on = ms < 2250. || ms >= 3300. || ((ms - 2250.) % 480.) < 260.;
    if cursor_drawn > 0. && blink_on {
        stroke(window, &partial(&logo.pts(&[(118., 124.), (168., 124.)]), cursor_drawn), 14. * k, c(GREEN, 1.));
        blob(window, logo.at(143., 124.).0, logo.at(143., 124.).1, 20. * k, c(GREEN, 0.25 * cursor_drawn), 18. * k);
    }
    // the name rises in
    let na = ease_out(seg(ms, 2250., 650.));
    text(window, cx, "Console Mode", name_px, FontWeight::SEMIBOLD, c(0xf4f6f9, na), mx, ly + logo_size / 2. + 34. * u + 14. * u * (1. - na), true);
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_line_draws_itself_along_its_length() {
        let line = [(0., 0.), (10., 0.), (10., 10.)];
        assert_eq!(partial(&line, 0.), vec![(0., 0.)]);
        assert_eq!(partial(&line, 0.5), vec![(0., 0.), (10., 0.)]);
        assert_eq!(partial(&line, 0.75), vec![(0., 0.), (10., 0.), (10., 5.)]);
        assert_eq!(partial(&line, 1.).len(), 3);
    }

    #[test]
    fn the_rounded_screen_closes_where_it_starts() {
        let p = rounded_rect_points(22., 14., 212., 156., 30.);
        assert_eq!(p[0], (52., 14.));
        let (lx, ly) = *p.last().unwrap();
        assert!((lx - 22. - 30.).abs() < 1.0 && (ly - 14.).abs() < 0.01, "{lx},{ly}");
    }

    #[test]
    fn the_360_flash_lands_on_the_last_hit_of_its_sound_and_before_the_end() {
        // the sound's last hit is at ≈3.8 s; the logo has 1.1 s to settle before the fade out
        assert!((3600. ..=3900.).contains(&X_FLASH));
        assert!(X_FLASH + 1100. <= cm_core::boot_intro::minimum_shown(Some(X360)).as_millis() as f32);
    }

    #[test]
    fn a_bell_rises_and_falls_back() {
        assert_eq!(bell(0., 100., 1000., 0.3), 0.);
        assert!((bell(400., 100., 1000., 0.3) - 1.).abs() < 1e-4);
        assert_eq!(bell(1200., 100., 1000., 0.3), 0.);
    }

    #[test]
    fn ribbons_orbit_the_centre_and_have_depth() {
        let rb = &ribbons()[0];
        for i in 0..32 {
            let (x, y, z) = orbit(rb, i as f32 / 32. * TAU, 100.);
            assert!((-1.0..=1.0).contains(&z));
            assert!((x * x + y * y).sqrt() <= 100. / (1. - 0.32) + 0.01);
        }
    }
}
