//! Console Mode 2 design system, taken from the approved mockup (docs/v2/mockups/console-mode-2.html).
//!
//! Every size in the mockup is in "design units" on a 1920-wide screen. [`Ui`] turns them into
//! pixels for the real window, so the console looks the same on a 1080p TV, a 4K TV at 150 %
//! or a 16:10 laptop: only the scale changes, never the layout.

use std::{borrow::Cow, sync::atomic::{AtomicU8, Ordering}};

use gpui::{
    App, BoxShadow, Div, FontWeight, Hsla, IntoElement, ParentElement, Pixels, SharedString, Size, Styled, div, px, rgb, rgba,
};

/// Shared text family for the app's headings and body.
pub const FONT: &str = "Source Sans 3";
const FONT_BYTES: &[u8] = include_bytes!("../assets/fonts/SourceSans3-Variable.ttf");
pub const LAUNCHER_TITLE_FONT: &str = "Plus Jakarta Sans";
const LAUNCHER_TITLE_FONT_BYTES: &[u8] = include_bytes!("../assets/fonts/PlusJakartaSans-Variable.ttf");
pub const ICON_FONT: &str = "Material Symbols Rounded Filled";
const ICON_FONT_BYTES: &[u8] = include_bytes!("../assets/fonts/MaterialSymbolsRounded-Filled.ttf");

/// Registers the bundled text and icon fonts. Call once at startup.
pub fn init(cx: &mut App) {
    for (family, bytes) in [
        (FONT, FONT_BYTES),
        (LAUNCHER_TITLE_FONT, LAUNCHER_TITLE_FONT_BYTES),
        (ICON_FONT, ICON_FONT_BYTES),
    ] {
        if let Err(e) = cx.text_system().add_fonts(vec![Cow::Borrowed(bytes)]) {
            eprintln!("font {family}: {e}");
        }
    }
    // grayscale antialiasing, as on macOS: ClearType's colored fringes look rough on a TV
    // (its pixels are not laid out like a monitor's) and on top of art
    cx.set_text_rendering_mode(gpui::TextRenderingMode::Grayscale);
}

/// Design tokens (mockup `:root`), as sRGB hex.
pub mod color {
    use super::*;
    pub fn ink() -> Hsla { rgb(0xf4f6f9).into() }
    pub fn mute() -> Hsla { rgb(0xc2c6cd).into() }
    pub fn dim() -> Hsla { rgb(0x989da6).into() }
    pub fn stage() -> Hsla { rgb(0x0b0d12).into() }
    pub fn panel_solid() -> Hsla { rgb(0x1c2230).into() }
    pub fn focus() -> Hsla { rgb(0xfafafa).into() }
    /// Accent of the current theme (Lua, Âmbar, Jade), and the ink that sits on it.
    pub fn accent() -> Hsla { match super::theme() { 1 => rgb(0xf2bf6b), 2 => rgb(0x79d3ad), _ => rgb(0x9cc6f2) }.into() }
    pub fn accent_ink() -> Hsla { match super::theme() { 1 => rgb(0x2a1d0e), 2 => rgb(0x0f2a20), _ => rgb(0x141c2a) }.into() }
    pub fn accent_alpha(a: f32) -> Hsla { let mut c = accent(); c.a = a; c }
    pub fn panel() -> Hsla { rgba(0x1b2130e6).into() }
    pub fn white(alpha: f32) -> Hsla { let mut c: Hsla = rgb(0xffffff).into(); c.a = alpha; c }
    pub fn black(alpha: f32) -> Hsla { let mut c: Hsla = rgb(0x000000).into(); c.a = alpha; c }
    pub fn shade(alpha: f32) -> Hsla { let mut c: Hsla = rgba(0x0b0d12ff).into(); c.a = alpha; c }
    pub fn button_ink() -> Hsla { rgb(0x111318).into() }
}

/// Device pixels per window pixel (Windows display scale), set by the window each frame.
static DEVICE_SCALE: std::sync::atomic::AtomicU32 = std::sync::atomic::AtomicU32::new(0x3f80_0000);
pub fn set_device_scale(s: f32) { DEVICE_SCALE.store(s.max(0.5).to_bits(), Ordering::Relaxed) }
pub fn device_scale() -> f32 { f32::from_bits(DEVICE_SCALE.load(Ordering::Relaxed)) }

/// Color themes, as in the mockup: 0 Lua (light blue), 1 Âmbar, 2 Jade.
pub const THEMES: [&str; 3] = ["Lua", "Âmbar", "Jade"];
static THEME: AtomicU8 = AtomicU8::new(0);
pub fn theme() -> u8 { THEME.load(Ordering::Relaxed) }
pub fn set_theme(t: u8) { THEME.store(t.min(2), Ordering::Relaxed) }

/// The console scale: design units → pixels for this window.
#[derive(Clone, Copy, Debug)]
pub struct Ui {
    pub scale: f32,
    /// Window size in design units (width is always 1920; height follows the screen's shape).
    pub design: Size<f32>,
}

impl Ui {
    pub const DESIGN_WIDTH: f32 = 1920.0;

    pub fn for_viewport(viewport: Size<Pixels>) -> Self {
        let w = f32::from(viewport.width).max(1.0);
        let h = f32::from(viewport.height).max(1.0);
        let scale = w / Self::DESIGN_WIDTH;
        Self { scale, design: Size { width: Self::DESIGN_WIDTH, height: h / scale } }
    }

    /// Design units → pixels.
    pub fn u(&self, v: f32) -> Pixels { px(v * self.scale) }

    /// A shadow in design units (offset y, blur, black alpha).
    fn drop(&self, y: f32, blur: f32, a: f32) -> BoxShadow {
        BoxShadow::new(px(0.), self.u(y), color::black(a)).blur_radius(self.u(blur))
    }

    /// Depth, Apple style: a tight contact shadow plus a wide ambient one.
    /// Small things that sit on the surface (buttons, chips, pills).
    pub fn lift_sm(&self) -> Vec<BoxShadow> { vec![self.drop(1., 2., 0.28), self.drop(4., 12., 0.22)] }
    /// Cards.
    pub fn lift_md(&self) -> Vec<BoxShadow> { vec![self.drop(2., 4., 0.30), self.drop(14., 30., 0.34)] }
    /// Floating things: the focused card, menus, panels, dialogs.
    pub fn lift_lg(&self) -> Vec<BoxShadow> { vec![self.drop(4., 10., 0.32), self.drop(30., 70., 0.50)] }

    /// The glass edge of a surface: a light hairline on top and a faint one all around, drawn
    /// inside. Add to a lift: `ui.lift_md().into_iter().chain(ui.gloss(0.14)).collect()`.
    pub fn gloss(&self, top: f32) -> Vec<BoxShadow> {
        vec![
            BoxShadow::new(px(0.), self.u(1.), color::white(top)).inset(),
            BoxShadow::new(px(0.), px(0.), color::white(top * 0.5)).spread_radius(self.u(1.)).inset(),
        ]
    }

    /// A lift with the glass edge.
    pub fn raised(&self, lift: Vec<BoxShadow>, top: f32) -> Vec<BoxShadow> { lift.into_iter().chain(self.gloss(top)).collect() }

    /// A soft light around something focused.
    pub fn glow(&self, c: Hsla, blur: f32) -> BoxShadow { BoxShadow::new(px(0.), px(0.), c).blur_radius(self.u(blur)) }

    /// Text with a soft shadow under it, so it reads on any art (CSS `text-shadow`, which GPUI
    /// has not): a dark copy one unit lower and a fainter one spread around it. It takes the
    /// size, weight and width of its parent.
    pub fn lifted(&self, text: impl Into<SharedString>) -> Div {
        let text: SharedString = text.into();
        let copy = |dx: f32, dy: f32, a: f32| div().absolute().top(self.u(dy)).left(self.u(dx)).w_full().text_color(color::black(a)).child(text.clone());
        div().relative()
            .child(copy(0., 2., 0.22)).child(copy(0., 1., 0.38))
            .child(div().relative().child(text.clone()))
    }

    /// A lens vignette over a full-screen layer: the corners fall off into the dark.
    pub fn vignette(&self) -> Div {
        div().absolute().top_0().left_0().size_full()
            .shadow(vec![BoxShadow::new(px(0.), px(0.), color::black(0.55)).blur_radius(self.u(320.)).spread_radius(self.u(-40.)).inset()])
    }

    /// A soft pool of light (a blurred disc), `d` design units wide, centered at (x, y).
    pub fn light(&self, x: f32, y: f32, d: f32, c: Hsla) -> Div {
        div().absolute().left(self.u(x - d / 2.)).top(self.u(y - d / 2.)).size(self.u(d)).rounded_full()
            .shadow(vec![BoxShadow::new(px(0.), px(0.), c).blur_radius(self.u(d * 0.45))])
    }

    /// A focused or pressed surface: white glass, brighter at the top.
    pub fn focus_fill(&self, top: f32, bottom: f32) -> gpui::Background {
        gpui::linear_gradient(180., gpui::linear_color_stop(color::white(top), 0.), gpui::linear_color_stop(color::white(bottom), 1.))
    }

    /// A Material Symbols glyph (filled, rounded), `size` in design units.
    pub fn icon(&self, glyph: char, size: f32, color: Hsla) -> Div {
        div().font_family(ICON_FONT).text_size(self.u(size)).line_height(self.u(size)).text_color(color).child(SharedString::from(glyph.to_string()))
    }

    /// The round white controller button used in hints ("A", "B", or an icon glyph).
    pub fn pad_button(&self, label: Glyph) -> Div {
        let d = div().size(self.u(34.)).rounded_full().flex().items_center().justify_center().flex_none()
            .bg(gpui::linear_gradient(180., gpui::linear_color_stop(color::white(0.98), 0.), gpui::linear_color_stop(color::white(0.84), 1.)))
            .shadow(self.lift_sm());
        match label {
            Glyph::Text(t) => d.text_color(color::button_ink()).text_size(self.u(16.)).font_weight(FontWeight::BOLD).child(t),
            Glyph::Icon(g) => d.child(self.icon(g, 22., color::button_ink())),
        }
    }

    /// One hint of the bottom bar: button, label and an optional second line ("Segure: Sistema").
    pub fn hint(&self, button: Glyph, label: &'static str, sub: Option<&'static str>) -> Div {
        // medium weight: thin strokes break up at TV distance and at small sizes
        let mut text = div().flex().flex_col().font_weight(FontWeight::MEDIUM).child(self.lifted(label));
        if let Some(s) = sub { text = text.child(div().text_size(self.u(16.)).font_weight(FontWeight::NORMAL).text_color(color::mute()).mt(self.u(2.)).child(self.lifted(s))); }
        div().flex().items_center().gap(self.u(12.)).child(self.pad_button(button)).child(text)
    }

    /// On/off switch (54×30 pill).
    pub fn toggle(&self, on: bool) -> Div {
        let knob = div().absolute().top(self.u(4.)).left(self.u(if on { 28. } else { 4. })).size(self.u(22.)).rounded_full()
            .bg(if on { color::accent_ink() } else { color::ink() }).shadow(vec![self.drop(1., 3., 0.35)]);
        // the track is sunk: a shadow inside along its top
        div().relative().w(self.u(54.)).h(self.u(30.)).rounded_full().flex_none()
            .bg(if on { color::accent() } else { color::white(0.22) })
            .shadow(vec![BoxShadow::new(px(0.), self.u(1.), color::black(0.30)).blur_radius(self.u(3.)).inset()]).child(knob)
    }

    /// LB/RB bumper chip next to the tabs.
    pub fn bumper(&self, label: &'static str) -> Div {
        div().text_size(self.u(15.)).font_weight(FontWeight::BOLD).px(self.u(10.)).py(self.u(5.)).rounded(self.u(8.)).text_color(color::button_ink()).child(label)
            .bg(gpui::linear_gradient(180., gpui::linear_color_stop(color::white(0.96), 0.), gpui::linear_color_stop(color::white(0.80), 1.)))
            .shadow(self.lift_sm())
    }

    /// Small caps label + value, one column of the hero's meta row.
    pub fn meta(&self, first: bool, label: &str, value: impl IntoElement) -> Div {
        let mut col = div().flex().flex_col().px(self.u(30.));
        if first { col = col.pl_0(); } else { col = col.border_l(self.u(1.)).border_color(color::white(0.22)); }
        col.child(div().text_size(self.u(15.)).font_weight(FontWeight::SEMIBOLD).text_color(color::mute()).mb(self.u(6.)).child(self.lifted(label.to_uppercase())))
            .child(div().text_size(self.u(26.)).font_weight(FontWeight::SEMIBOLD).whitespace_nowrap().child(value))
    }
}

pub enum Glyph { Text(&'static str), Icon(char) }

/// Material Symbols codepoints used by the app (names as in the mockup).
pub mod glyph {
    pub const MENU: char = '\u{e5d2}';
    pub const SWAP_HORIZ: char = '\u{e8d4}';
    pub const ADD: char = '\u{e145}';
    pub const ADD_CIRCLE: char = '\u{e990}';
    pub const SPORTS_ESPORTS: char = '\u{ea28}';
    pub const BATTERY_5_BAR: char = '\u{f0a0}';
    pub const PLAY_ARROW: char = '\u{e037}';
    pub const CHEVRON_RIGHT: char = '\u{e5cc}';
    pub const CHEVRON_LEFT: char = '\u{e5cb}';
    pub const CHECK_CIRCLE: char = '\u{f0be}';
    pub const VOLUME_DOWN: char = '\u{e04d}';
    pub const DESKTOP_OFF: char = '\u{e99d}';
    pub const SPEAKER: char = '\u{e32d}';
    pub const MUSIC_NOTE: char = '\u{e405}';
    pub const SETTINGS: char = '\u{e8b8}';
    pub const TUNE: char = '\u{e429}';
    pub const TV: char = '\u{e63b}';
    pub const HDR_ON: char = '\u{e3ee}';
    pub const SPEED: char = '\u{e9e4}';
    pub const VOLUME_UP: char = '\u{e050}';
    pub const POWER: char = '\u{f8c7}';
    pub const SELECT_WINDOW: char = '\u{e6fa}';
    pub const SCREENSHOT_MONITOR: char = '\u{ec08}';
    pub const PUSH_PIN: char = '\u{f10d}';
    pub const VISIBILITY_OFF: char = '\u{e8f5}';
    pub const ASPECT_RATIO: char = '\u{e85b}';
    pub const SKIP_NEXT: char = '\u{e044}';
    pub const GAMEPAD: char = '\u{e30f}';
    pub const PALETTE: char = '\u{e40a}';
    pub const SYSTEM_UPDATE: char = '\u{f2cd}';
    pub const TRANSLATE: char = '\u{e8e2}';
    pub const INFO: char = '\u{e88e}';
    pub const CHECK: char = '\u{e5ca}';
    pub const EXPAND_MORE: char = '\u{e5cf}';
    pub const EXPAND_LESS: char = '\u{e5ce}';
    pub const DESKTOP: char = '\u{e30c}';
    pub const MORE_HORIZ: char = '\u{e5d3}';
    pub const APPS: char = '\u{e5c3}';
    pub const FOLDER: char = '\u{e2c7}';
    pub const LANGUAGE: char = '\u{e894}';
    pub const FIBER_RECORD: char = '\u{e061}';
}

/// Motion tokens (lippstack design-skills `animate` / `choreograph`): strong ease-out curves,
/// UI under 300 ms, exits shorter than entrances, and slides turned into fades when Windows
/// has its animations off.
pub mod motion {
    use std::{sync::atomic::{AtomicBool, Ordering}, time::Duration};

    use gpui::Animation;

    pub const TAB: u64 = 200;
    pub const MENU_IN: u64 = 200;
    pub const MENU_OUT: u64 = 140;
    pub const MODAL_IN: u64 = 250;
    pub const MODAL_OUT: u64 = 160;
    pub const SMALL: u64 = 160;
    pub const FOCUS: u64 = 240;
    pub const BACKDROP: u64 = 480;
    pub const LAUNCH: u64 = 300;

    static REDUCED: AtomicBool = AtomicBool::new(false);
    pub fn set_reduced(on: bool) { REDUCED.store(on, Ordering::Relaxed) }
    pub fn reduced() -> bool { REDUCED.load(Ordering::Relaxed) }

    /// A slide distance (design units), or none with reduced motion.
    pub fn dist(d: f32) -> f32 { if reduced() { 0. } else { d } }

    /// `cubic-bezier(0.23, 1, 0.32, 1)`: entering and exiting.
    pub fn ease_out(t: f32) -> f32 { bezier(0.23, 1., 0.32, 1., t) }
    /// `cubic-bezier(0.77, 0, 0.175, 1)`: something moving across the screen.
    pub fn ease_in_out(t: f32) -> f32 { bezier(0.77, 0., 0.175, 1., t) }

    pub fn out(ms: u64) -> Animation { Animation::new(Duration::from_millis(ms)).with_easing(ease_out) }
    pub fn in_out(ms: u64) -> Animation { Animation::new(Duration::from_millis(ms)).with_easing(ease_in_out) }

    /// y of a CSS cubic-bezier at x = t (Newton, then bisection when the slope is flat).
    fn bezier(x1: f32, y1: f32, x2: f32, y2: f32, t: f32) -> f32 {
        let t = t.clamp(0., 1.);
        let f = |a: f32, b: f32, s: f32| { let u = 1. - s; 3. * u * u * s * a + 3. * u * s * s * b + s * s * s };
        let df = |a: f32, b: f32, s: f32| { let u = 1. - s; 3. * u * u * a + 6. * u * s * (b - a) + 3. * s * s * (1. - b) };
        let mut s = t;
        for _ in 0..8 {
            let d = df(x1, x2, s);
            if d.abs() < 1e-6 { break; }
            s = (s - (f(x1, x2, s) - t) / d).clamp(0., 1.);
        }
        if (f(x1, x2, s) - t).abs() > 1e-4 {
            let (mut lo, mut hi) = (0., 1.);
            for _ in 0..30 { s = (lo + hi) / 2.; if f(x1, x2, s) < t { lo = s } else { hi = s } }
        }
        f(y1, y2, s)
    }

    #[cfg(test)]
    mod tests {
        use super::*;
        #[test]
        fn curves() {
            for e in [ease_out as fn(f32) -> f32, ease_in_out] {
                assert!(e(0.).abs() < 1e-3 && (e(1.) - 1.).abs() < 1e-3);
                let mut last = 0.;
                for i in 1..=20 { let v = e(i as f32 / 20.); assert!(v + 1e-4 >= last); last = v; }
            }
            // strong ease-out: most of the way there early
            assert!(ease_out(0.3) > 0.75);
            // reference values of the two CSS curves
            assert!((ease_out(0.1) - 0.398).abs() < 0.01 && (ease_in_out(0.5) - 0.596).abs() < 0.01);
        }
    }
}
