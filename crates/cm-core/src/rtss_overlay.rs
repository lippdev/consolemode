//! Our FPS counter on the game, drawn by RTSS in its own OSD slot (ported from 1.7's RtssOverlay).
//! Pure: it only builds the RTSS hypertext; cm-platform writes it into RTSS's shared memory.
//!
//! Every style except `external` takes the screen: MSI Afterburner's OSD is hidden while it is on,
//! so "off" really means nothing on screen. `external` leaves Afterburner/RTSS alone. FPS and frame
//! time come from RTSS itself; CPU, GPU and memory from a [`HardwareSample`], so the styles that
//! show them are rewritten every second ([`needs_hardware`]).

use serde::{Deserialize, Serialize};

pub const EXTERNAL: &str = "external";
pub const OFF: &str = "off";
/// Only the FPS. Its id is "compact", the name this style had up to 1.6.0, so saved configs keep it.
pub const FPS_ONLY: &str = "compact";
/// FPS plus CPU, GPU and RAM usage on one line.
pub const COMPACT: &str = "stats";
pub const DETAILED: &str = "detailed";
pub const CUSTOM: &str = "custom";

/// szOSDOwner of our slot.
pub const OWNER: &str = "ConsoleMode";

pub const STYLES: [&str; 6] = [EXTERNAL, OFF, FPS_ONLY, COMPACT, DETAILED, CUSTOM];
/// White, blue, green, yellow, orange.
pub const COLORS: [&str; 5] = ["FFFFFF", "4CC2FF", "6FE07A", "FFD54A", "FF8A3D"];
pub const SIZES: [&str; 3] = ["small", "medium", "large"];
pub const POSITIONS: [&str; 5] = ["rtss", "top-left", "top-right", "bottom-left", "bottom-right"];

/// What the counter shows and how (`fpsOverlayLayout` in the 1.7 config).
#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct FpsOverlayLayout {
    pub show_api: bool,
    pub show_fps: bool,
    pub show_frame_time: bool,
    pub show_cpu: bool,
    pub show_cpu_clock: bool,
    pub show_gpu: bool,
    pub show_gpu_clock: bool,
    pub show_gpu_temp: bool,
    pub show_vram: bool,
    pub show_ram: bool,
    /// Everything on one line instead of one row per group.
    pub single_line: bool,
    /// RRGGBB of the numbers; one of [`COLORS`].
    pub color: String,
    pub size: String,
    /// A translucent rounded card behind the numbers.
    pub card: bool,
    pub position: String,
}

impl Default for FpsOverlayLayout {
    fn default() -> Self {
        Self {
            show_api: true, show_fps: true, show_frame_time: true,
            show_cpu: false, show_cpu_clock: false, show_gpu: false, show_gpu_clock: false, show_gpu_temp: false, show_vram: false, show_ram: false,
            single_line: false, color: COLORS[0].into(), size: SIZES[0].into(), card: true, position: POSITIONS[0].into(),
        }
    }
}

/// CPU, GPU and memory readings; `None` is "this PC can't report it" (the row is left out).
#[derive(Clone, Debug, Default, PartialEq)]
pub struct HardwareSample {
    pub cpu_usage: Option<f64>,
    pub cpu_clock_mhz: Option<f64>,
    pub gpu_usage: Option<f64>,
    pub gpu_clock_mhz: Option<f64>,
    pub gpu_temp_c: Option<f64>,
    pub vram_used_mb: Option<f64>,
    pub vram_total_mb: Option<f64>,
    pub ram_used_mb: Option<f64>,
    pub ram_total_mb: Option<f64>,
}

/// The layout saved in the 1.7 config (`fpsOverlayLayout`, kept in `Config::other`), defaults when absent or malformed.
pub fn layout_from_config(other: &serde_json::Map<String, serde_json::Value>) -> FpsOverlayLayout {
    other.get("fpsOverlayLayout").and_then(|v| serde_json::from_value(v.clone()).ok()).unwrap_or_default()
}

pub fn normalize(style: &str) -> &'static str {
    STYLES.iter().copied().find(|s| s.eq_ignore_ascii_case(style)).unwrap_or(EXTERNAL)
}

/// Whether this style keeps Afterburner's OSD off the screen.
pub fn hides_others(style: &str) -> bool { normalize(style) != EXTERNAL }

/// A color from the config, back to white if it isn't plain RRGGBB (it goes inside an RTSS tag).
pub fn normalize_color(color: &str) -> String {
    if color.len() == 6 && color.bytes().all(|b| b.is_ascii_hexdigit()) { color.to_ascii_uppercase() } else { COLORS[0].into() }
}

pub fn normalize_size(size: &str) -> &'static str { SIZES.iter().copied().find(|s| s.eq_ignore_ascii_case(size)).unwrap_or(SIZES[0]) }

pub fn normalize_position(position: &str) -> &'static str { POSITIONS.iter().copied().find(|s| s.eq_ignore_ascii_case(position)).unwrap_or(POSITIONS[0]) }

const FPS: u16 = 1;
const FRAME_TIME: u16 = 2;
const API: u16 = 4;
const CPU: u16 = 8;
const CPU_CLOCK: u16 = 16;
const GPU: u16 = 32;
const GPU_CLOCK: u16 = 64;
const GPU_TEMP: u16 = 128;
const VRAM: u16 = 256;
const RAM: u16 = 512;
const PC_ITEMS: u16 = CPU | CPU_CLOCK | GPU | GPU_CLOCK | GPU_TEMP | VRAM | RAM;

fn items_of(style: &str, layout: &FpsOverlayLayout) -> u16 {
    match style {
        FPS_ONLY => FPS,
        COMPACT => FPS | CPU | GPU | RAM,
        DETAILED => FPS | FRAME_TIME | API | PC_ITEMS,
        CUSTOM => {
            let l = layout;
            let flags = [(l.show_fps, FPS), (l.show_frame_time, FRAME_TIME), (l.show_api, API), (l.show_cpu, CPU), (l.show_cpu_clock, CPU_CLOCK), (l.show_gpu, GPU),
                (l.show_gpu_clock, GPU_CLOCK), (l.show_gpu_temp, GPU_TEMP), (l.show_vram, VRAM), (l.show_ram, RAM)];
            let items = flags.iter().filter(|(on, _)| *on).fold(0, |acc, (_, bit)| acc | bit);
            if items == 0 { FPS } else { items } // nothing ticked still shows something
        }
        _ => 0,
    }
}

/// Whether the style shows CPU/GPU/memory, which RTSS doesn't know: the text then needs a fresh sample every second.
pub fn needs_hardware(style: &str, layout: &FpsOverlayLayout) -> bool { items_of(normalize(style), layout) & PC_ITEMS != 0 }

// RTSS hypertext (SDK sample and OverlayEditor): <Cn=AARRGGBB> / <Sn=size> / <An=width> define a
// variable, <Cn>…<C> / <Sn>…<S> / <An>…<A> use it (<S> and <C> go back to 100% / default color).
// Sizes are percent, negative = sits on the baseline; widths are in characters, negative = right
// aligned, so numbers don't make the text jump. <FR> framerate, <FT> frametime and <APP> 3D API
// are filled in by RTSS. The card is a layer (<L>) with padding (<M>) whose background is a
// rounded bar (<B=0,0,Rn>, 0,0 = the whole layer) drawn in C3; \b puts the cursor back on its origin.
const VALUE: &str = "C0";
const LABEL: &str = "C1";
const ACCENT: &str = "C2";
const CARD_FILL: &str = "C3";
const VALUE_SIZE: &str = "S0";
const UNIT_SIZE: &str = "S1";
const LABEL_SIZE: &str = "S2";
const FPS_SIZE: &str = "S3";
const LABEL_COLOR: &str = "9AA4B2";
const ACCENT_COLOR: &str = "4CC2FF";
const CARD_COLOR: &str = "C0101418";
const MISSING: &str = "--";

fn run(size: &str, color: &str, text: &str) -> String { format!("<{size}><{color}>{text}<C><S>") }
fn gap() -> String { run(LABEL_SIZE, LABEL, "  ") }
fn percent(p: u32, scale: f64) -> i64 { (f64::from(p) * scale).round_ties_even() as i64 }

/// "CPU 45% 4.2 GHz": the label column is fixed-width when stacked so the values line up.
fn add_row(rows: &mut Vec<String>, label: &str, single_line: bool, values: &[String]) {
    let shown: Vec<&String> = values.iter().filter(|v| !v.is_empty()).collect();
    if shown.is_empty() { return; }
    let name = if single_line { run(LABEL_SIZE, LABEL, &format!("{label} ")) } else { format!("<{LABEL_SIZE}><{LABEL}><A2>{label}<A><C><S>") };
    rows.push(name + &shown.iter().map(|s| s.as_str()).collect::<Vec<_>>().join(&gap()));
}

/// A right-aligned value in a box of `width` characters, then its unit. "" = not available here.
/// `sample` None = not measured yet: dashes.
fn number(measured: bool, value: Option<f64>, decimals: usize, width: usize, unit: &str) -> String {
    let text = if !measured { MISSING.to_string() } else { match value { Some(v) if v.is_finite() => format!("{v:.decimals$}"), _ => return String::new() } };
    let boxed = if width >= 4 { "A1" } else { "A0" };
    format!("<{VALUE_SIZE}><{VALUE}><{boxed}>{text}<A><C><S>") + &run(UNIT_SIZE, LABEL, unit)
}

/// "5.2 / 8.0 GB", or a percentage on the compact line.
fn memory(measured: bool, used_mb: Option<f64>, total_mb: Option<f64>, as_percent: bool) -> String {
    if as_percent {
        return number(measured, used_mb.zip(total_mb).filter(|(_, t)| *t > 0.0).map(|(u, t)| u / t * 100.0), 0, 3, "%");
    }
    if !measured { return number(false, None, 1, 3, " GB"); }
    let Some(used) = used_mb else { return String::new() };
    let text = number(true, Some(used / 1024.0), 1, 3, "");
    let total = match total_mb { Some(t) if t > 0.0 => format!(" / {:.0}", t / 1024.0), _ => String::new() };
    text + &run(UNIT_SIZE, LABEL, &format!("{total} GB"))
}

/// The OSD text for a style; empty for off and external (nothing of ours on screen). `sample` None =
/// not measured yet: hardware values show as "--". The text is Latin-1 (°), what RTSS's slot holds.
pub fn text(style: &str, layout: &FpsOverlayLayout, sample: Option<&HardwareSample>) -> String {
    let style = normalize(style);
    let items = items_of(style, layout);
    if items == 0 { return String::new(); }
    let single_line = match style { FPS_ONLY | COMPACT => true, CUSTOM => layout.single_line, _ => false };
    let scale = match normalize_size(&layout.size) { "medium" => 1.5, "large" => 2.0, _ => 1.0 };
    let has = |bit: u16| items & bit != 0;
    let measured = sample.is_some();
    let s = sample.cloned().unwrap_or_default();

    let mut rows = Vec::new();
    let mut game = Vec::new();
    if has(FPS) { game.push(run(FPS_SIZE, VALUE, "<FR>") + &run(UNIT_SIZE, LABEL, " FPS")); }
    if has(FRAME_TIME) { game.push(run(VALUE_SIZE, VALUE, "<FT>") + &run(UNIT_SIZE, LABEL, " ms")); }
    if has(API) { game.push(run(LABEL_SIZE, ACCENT, "<APP>")); }
    if !game.is_empty() { rows.push(game.join(&gap())); }

    // compact is one short line: RAM as a percentage, like the other two
    let ram_as_percent = style == COMPACT;
    let pick = |on: bool, f: &dyn Fn() -> String| if on { f() } else { String::new() };
    add_row(&mut rows, "CPU", single_line, &[
        pick(has(CPU), &|| number(measured, s.cpu_usage, 0, 3, "%")),
        pick(has(CPU_CLOCK), &|| number(measured, s.cpu_clock_mhz.map(|m| m / 1000.0), 1, 3, " GHz")),
    ]);
    add_row(&mut rows, "GPU", single_line, &[
        pick(has(GPU), &|| number(measured, s.gpu_usage, 0, 3, "%")),
        pick(has(GPU_CLOCK), &|| number(measured, s.gpu_clock_mhz, 0, 4, " MHz")),
        pick(has(GPU_TEMP), &|| number(measured, s.gpu_temp_c, 0, 3, "°C")),
    ]);
    add_row(&mut rows, "VRAM", single_line, &[pick(has(VRAM), &|| memory(measured, s.vram_used_mb, s.vram_total_mb, false))]);
    add_row(&mut rows, "RAM", single_line, &[pick(has(RAM), &|| memory(measured, s.ram_used_mb, s.ram_total_mb, ram_as_percent))]);
    if rows.is_empty() { rows.push(run(FPS_SIZE, VALUE, "<FR>") + &run(UNIT_SIZE, LABEL, " FPS")); }

    let mut header = format!("<{VALUE}={}><{LABEL}={LABEL_COLOR}><{ACCENT}={ACCENT_COLOR}><{CARD_FILL}={CARD_COLOR}>", normalize_color(&layout.color));
    header += &format!("<{VALUE_SIZE}={}><{UNIT_SIZE}=-{}><{LABEL_SIZE}=-{}>", percent(100, scale), percent(55, scale), percent(70, scale));
    header += &format!("<{FPS_SIZE}={}>", percent(if single_line { 100 } else { 130 }, scale));
    header += "<A0=-3><A1=-4><A2=5>";
    // A corner is a sticky position (<P0> top left, <P2> top right, <P6> bottom left, <P8> bottom
    // right of RTSS's 3x3 screen grid), which ignores the OSD position set in RTSS; it needs a layer.
    let corner = match normalize_position(&layout.position) { "top-left" => "<P0>", "top-right" => "<P2>", "bottom-left" => "<P6>", "bottom-right" => "<P8>", _ => "" };
    header += corner;
    if layout.card {
        let pad = (6.0 * scale).round_ties_even() as i64;
        header += &format!("<M={},{pad},{},{pad}><L0><{CARD_FILL}><B=0,0,R{}>\u{8}<C>", pad + 2, pad + 2, pad + 2);
    } else if !corner.is_empty() {
        header += "<L0>";
    }
    header + &rows.join(&if single_line { gap() + &gap() } else { "\n".to_string() })
}

#[cfg(test)]
mod tests {
    use super::*;

    fn pc() -> HardwareSample {
        HardwareSample { cpu_usage: Some(45.4), cpu_clock_mhz: Some(4321.0), gpu_usage: Some(97.6), gpu_clock_mhz: Some(1905.0), gpu_temp_c: Some(66.8),
            vram_used_mb: Some(5324.0), vram_total_mb: Some(8192.0), ram_used_mb: Some(15155.0), ram_total_mb: Some(32768.0) }
    }
    fn layout() -> FpsOverlayLayout { FpsOverlayLayout::default() }

    #[test]
    fn styles_are_listed_from_least_to_most() {
        assert_eq!(STYLES, ["external", "off", "compact", "stats", "detailed", "custom"]);
        assert_eq!(normalize("bogus"), EXTERNAL);
    }

    #[test]
    fn a_corner_pins_the_counter_there_with_or_without_the_card() {
        for (position, tag) in [("top-left", "<P0>"), ("top-right", "<P2>"), ("bottom-left", "<P6>"), ("bottom-right", "<P8>")] {
            for card in [true, false] {
                let t = text(FPS_ONLY, &FpsOverlayLayout { position: position.into(), card, ..layout() }, None);
                assert!(t.contains(&format!("{tag}{}", if card { "<M=" } else { "<L0>" })), "{t}");
            }
        }
    }

    #[test]
    fn otherwise_it_follows_the_rtss_osd_position() {
        for position in ["rtss", "", "middle"] {
            let t = text(FPS_ONLY, &FpsOverlayLayout { position: position.into(), card: false, ..layout() }, None);
            assert!(!t.contains("<P") && !t.contains("<L"), "{t}");
        }
    }

    #[test]
    fn default_leaves_afterburner_alone() {
        assert_eq!(normalize(""), EXTERNAL);
        assert!(!hides_others(""));
        assert!(!hides_others("external"));
        assert!(hides_others("off"));
    }

    // 1.6.0 saved FPS-only as "compact": it must stay FPS-only, not grow into the new compact.
    #[test]
    fn a_saved_compact_from_1_6_0_stays_fps_only() {
        assert_eq!(normalize("compact"), FPS_ONLY);
        let t = text("compact", &layout(), Some(&pc()));
        assert!(t.contains("<FR>") && !t.contains("CPU"));
    }

    #[test]
    fn off_and_external_draw_nothing() {
        assert_eq!(text(OFF, &layout(), None), "");
        assert_eq!(text(EXTERNAL, &layout(), None), "");
    }

    #[test]
    fn styles_show_the_live_framerate_and_fit_the_rtss_slot() {
        for (style, size) in [("compact", "small"), ("stats", "medium"), ("detailed", "large"), ("custom", "large")] {
            let all = FpsOverlayLayout { show_cpu: true, show_cpu_clock: true, show_gpu: true, show_gpu_clock: true, show_gpu_temp: true, show_vram: true, show_ram: true, size: size.into(), ..layout() };
            for sample in [None, Some(pc())] {
                let t = text(style, &all, sample.as_ref());
                assert!(t.contains("<FR>"));
                assert!(t.len() < 4096);
                assert!(t.chars().all(|c| (c as u32) < 256), "the RTSS slot is Latin-1");
            }
        }
    }

    #[test]
    fn compact_is_one_line_of_usages() {
        let t = text(COMPACT, &layout(), Some(&pc()));
        assert!(!t.contains('\n'));
        assert!(t.contains("CPU") && t.contains(">45<") && t.contains(">98<"));
        assert!(t.contains(">46<"), "RAM as a percentage");
        assert!(!t.contains("GHz"));
    }

    #[test]
    fn detailed_has_clocks_temperature_memory_frametime_and_api() {
        let t = text(DETAILED, &layout(), Some(&pc()));
        for needle in ["<FT>", "<APP>", ">4.3<", " GHz", ">1905<", ">67<", "°C", ">5.2<", " / 8 GB", ">14.8<", "\n"] {
            assert!(t.contains(needle), "{needle} in {t}");
        }
    }

    #[test]
    fn before_the_first_sample_values_show_dashes() {
        let t = text(COMPACT, &layout(), None);
        assert!(t.contains(">--<") && t.contains("CPU"));
    }

    #[test]
    fn what_this_pc_cannot_report_is_left_out() {
        let sample = HardwareSample { gpu_temp_c: None, gpu_clock_mhz: None, vram_used_mb: None, ..pc() };
        let t = text(DETAILED, &layout(), Some(&sample));
        assert!(!t.contains("°C") && !t.contains("MHz") && !t.contains("VRAM"));
        assert!(t.contains("GPU"));
    }

    #[test]
    fn card_draws_a_rounded_background_that_can_be_turned_off() {
        let card = text(FPS_ONLY, &layout(), None);
        assert!(card.contains("<B=0,0,R") && card.contains('\u{8}'));
        let plain = text(FPS_ONLY, &FpsOverlayLayout { card: false, ..layout() }, None);
        assert!(!plain.contains("<B=") && !plain.contains("<L"));
    }

    #[test]
    fn size_scales_every_text() {
        for (size, expected) in [("small", "<S0=100>"), ("medium", "<S0=150>"), ("large", "<S0=200>"), ("huge", "<S0=100>")] {
            assert!(text(DETAILED, &FpsOverlayLayout { size: size.into(), ..layout() }, None).contains(expected), "{size}");
        }
    }

    #[test]
    fn only_styles_with_pc_data_need_a_refresh() {
        assert!(!needs_hardware(FPS_ONLY, &layout()));
        assert!(!needs_hardware(OFF, &layout()));
        assert!(needs_hardware(COMPACT, &layout()));
        assert!(needs_hardware(DETAILED, &layout()));
        assert!(!needs_hardware(CUSTOM, &layout()));
        assert!(needs_hardware(CUSTOM, &FpsOverlayLayout { show_gpu_temp: true, ..layout() }));
    }

    #[test]
    fn custom_shows_only_what_is_ticked_on_one_line_in_its_color() {
        let l = FpsOverlayLayout { show_api: false, show_fps: true, show_frame_time: true, show_gpu: true, single_line: true, color: "6fe07a".into(), ..layout() };
        let t = text(CUSTOM, &l, Some(&pc()));
        assert!(!t.contains("<APP>") && t.contains("<FT>") && t.contains("GPU") && !t.contains("CPU") && !t.contains('\n'));
        assert!(t.starts_with("<C0=6FE07A>"));
    }

    #[test]
    fn custom_with_nothing_ticked_still_shows_the_fps() {
        let l = FpsOverlayLayout { show_api: false, show_fps: false, show_frame_time: false, ..layout() };
        assert!(text(CUSTOM, &l, None).contains("<FR>"));
    }

    #[test]
    fn colors_from_the_config_cannot_break_the_tags() {
        assert_eq!(normalize_color("FF0000><C0=000000"), "FFFFFF");
        assert_eq!(normalize_color(""), "FFFFFF");
        assert_eq!(normalize_color("ffd54a"), "FFD54A");
    }

    #[test]
    fn the_layout_comes_out_of_the_config_bag() {
        let c = crate::config::Config::from_json(r#"{"fpsOverlayLayout": {"position": "top-left", "card": false}}"#).unwrap();
        let l = layout_from_config(&c.other);
        assert_eq!((l.position.as_str(), l.card, l.show_fps), ("top-left", false, true));
        assert_eq!(layout_from_config(&crate::config::Config::default().other), FpsOverlayLayout::default());
    }

    #[test]
    fn the_layout_reads_from_the_1_7_config_and_fills_the_gaps() {
        let l: FpsOverlayLayout = serde_json::from_str(r#"{"showGpu": true, "size": "large"}"#).unwrap();
        assert!(l.show_gpu && l.show_fps && l.card);
        assert_eq!(l.size, "large");
    }
}
