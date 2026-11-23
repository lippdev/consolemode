//! Which settings rows are saved, and where in the config they live.
//!
//! Rows are matched by title (titles are unique). Rows not listed here are not saved yet:
//! the screens, modes and audio outputs of Sessão come from the PC in the next step.

use cm_core::config::{Config, DisplayMode, GameProfile};

use crate::{pad_shortcuts as pads, tv, settings::{CHECK_AT_START, HIDE_ICONS_ROW, INTRO_SOUND_ROW, INTRO_STYLE_ROW, SHOW_STEAM_GAMES, Section, Value}};
use crate::settings::gaming_home_settings::ON_OPEN;

/// Sets every bound row from the config.
pub fn load(sections: &mut [Section], c: &Config) {
    let masks = pads::current_masks(c);
    for row in sections.iter_mut().flat_map(|s| s.rows.iter_mut()) {
        if pads::load_row(row.title, &mut row.value, &masks) { continue; }
        match (row.title, &mut row.value) {
            (pads::AUTO_START_ROW, Value::Toggle(on)) => *on = c.auto_start_on_controller,
            (pads::TASK_VIEW_PS_ROW, Value::Toggle(on)) => *on = c.task_view_playstation,
            (pads::MOUSE_ROW, Value::Toggle(on)) => *on = c.controller_mouse,
            (pads::SPEED_ROW, Value::Choice(o, i)) => *i = pick(o, pads::speed_label(c.pointer_speed())),
            ("Iniciar com o Windows", Value::Toggle(on)) => *on = c.start_with_windows,
            ("Começar na bandeja", Value::Toggle(on)) => *on = c.start_in_tray,
            ("Registrar links consolemode2alpha", Value::Toggle(on)) => *on = c.protocol_registered,
            (ON_OPEN, Value::Toggle(on)) => *on = c.gaming_home_on_open,
            ("HDR", Value::Toggle(on)) => *on = c.hdr_enable,
            ("VRR", Value::Toggle(on)) => *on = c.vrr_enable,
            ("Mostrar FPS na tela", Value::Toggle(on)) => *on = matches!(c.fps_overlay.as_str(), "compact" | "stats" | "detailed" | "custom"),
            ("Devolver os ícones da área de trabalho ao lugar", Value::Toggle(on)) => *on = c.restore_desktop_icons,
            (HIDE_ICONS_ROW, Value::Toggle(on)) => *on = c.hide_desktop_icons,
            ("Fechar o iniciador junto", Value::Toggle(on)) => *on = c.close_steam_on_restore,
            ("Sons de navegação", Value::Toggle(on)) => *on = c.interface_sounds,
            ("Estilo", Value::Choice(o, i)) => *i = pick(o, if c.sound_style == "soft" { "Suave" } else { "Console" }),
            ("Volume", Value::Choice(o, i)) => *i = pick(o, &format!("{} %", c.sound_volume)),
            (SHOW_STEAM_GAMES, Value::Toggle(on)) => *on = c.show_steam_games,
            (INTRO_SOUND_ROW, Value::Toggle(on)) => *on = c.intro_sound,
            (INTRO_STYLE_ROW, Value::Choice(o, i)) => *i = pick(o, intro_label(cm_core::boot_intro::picker_value(c.intro_animation, c.intro_style.as_deref()))),
            (CHECK_AT_START, Value::Toggle(on)) => *on = c.check_updates,
            ("Limite de FPS", Value::Choice(o, i)) => *i = pick(o, &if c.fps_limit == 0 { "Sem limite".to_string() } else { c.fps_limit.to_string() }),
            ("Layout dos botões", Value::Choice(o, i)) => *i = pick(o, match c.controller_layout.as_str() { "playstation" => "PlayStation", "xbox" => "Xbox", _ => "Automático" }),
            ("Tema", Value::Choice(o, i)) => *i = pick(o, match c.theme.as_str() { "ambar" => "Âmbar", "jade" => "Jade", _ => "Lua" }),
            ("Idioma", Value::Choice(o, i)) => *i = pick(o, language_label(&c.app_language)),
            (tv::PROVIDER_ROW, Value::Choice(o, i)) => *i = pick(o, tv::provider_label(&c.tv.provider)),
            (tv::HDMI_ROW, Value::Choice(o, i)) => *i = pick(o, tv::hdmi_label(c.tv.hdmi_input)),
            (tv::STANDBY_ROW, Value::Toggle(on)) => *on = c.tv.turn_off_on_restore,
            (t, Value::Link(v)) if tv::TEXT_ROWS.contains(&t) => *v = tv::shown_value(tv::text_of(t, &c.tv)),
            _ => {}
        }
    }
}

/// Writes one row back. True when the config changed.
pub fn store(title: &str, value: &Value, c: &mut Config) -> bool {
    if pads::store_row(title, value, c) { return true; }
    match (title, value) {
        (pads::AUTO_START_ROW, Value::Toggle(on)) => c.auto_start_on_controller = *on,
        (pads::TASK_VIEW_PS_ROW, Value::Toggle(on)) => c.task_view_playstation = *on,
        (pads::MOUSE_ROW, Value::Toggle(on)) => c.controller_mouse = *on,
        (pads::SPEED_ROW, Value::Choice(o, i)) => c.controller_mouse_speed = pads::speed_of_label(o[*i]).key().into(),
        ("Iniciar com o Windows", Value::Toggle(on)) => c.start_with_windows = *on,
        ("Começar na bandeja", Value::Toggle(on)) => c.start_in_tray = *on,
        ("Registrar links consolemode2alpha", Value::Toggle(on)) => c.protocol_registered = *on,
        (ON_OPEN, Value::Toggle(on)) => c.gaming_home_on_open = *on,
        ("HDR", Value::Toggle(on)) => c.hdr_enable = *on,
        ("VRR", Value::Toggle(on)) => c.vrr_enable = *on,
        ("Mostrar FPS na tela", Value::Toggle(on)) => c.fps_overlay = if *on { "compact".into() } else { "external".into() },
        ("Devolver os ícones da área de trabalho ao lugar", Value::Toggle(on)) => c.restore_desktop_icons = *on,
        (HIDE_ICONS_ROW, Value::Toggle(on)) => c.hide_desktop_icons = *on,
        ("Fechar o iniciador junto", Value::Toggle(on)) => c.close_steam_on_restore = *on,
        ("Sons de navegação", Value::Toggle(on)) => c.interface_sounds = *on,
        ("Estilo", Value::Choice(o, i)) => c.sound_style = if o[*i] == "Suave" { "soft".into() } else { "console".into() },
        ("Volume", Value::Choice(o, i)) => c.sound_volume = o[*i].trim_end_matches(" %").parse().unwrap_or(60),
        (t, Value::Toggle(on)) if t == SHOW_STEAM_GAMES => c.show_steam_games = *on,
        (t, Value::Toggle(on)) if t == INTRO_SOUND_ROW => c.intro_sound = *on,
        (t, Value::Choice(o, i)) if t == INTRO_STYLE_ROW => (c.intro_animation, c.intro_style) = cm_core::boot_intro::after_choice(intro_value(o[*i]), c.intro_style.as_deref()),
        (t, Value::Toggle(on)) if t == CHECK_AT_START => c.check_updates = *on,
        ("Limite de FPS", Value::Choice(o, i)) => c.fps_limit = o[*i].parse().unwrap_or(0),
        ("Layout dos botões", Value::Choice(o, i)) => c.controller_layout = match o[*i] { "PlayStation" => "playstation", "Xbox" => "xbox", _ => "auto" }.into(),
        ("Tema", Value::Choice(o, i)) => c.theme = match o[*i] { "Âmbar" => "ambar", "Jade" => "jade", _ => "lua" }.into(),
        ("Idioma", Value::Choice(o, i)) => c.app_language = language_code(o[*i]).into(),
        (tv::PROVIDER_ROW, Value::Choice(o, i)) => c.tv.provider = tv::provider_value(o[*i]).into(),
        (tv::HDMI_ROW, Value::Choice(o, i)) => c.tv.hdmi_input = tv::hdmi_value(o[*i]),
        (tv::STANDBY_ROW, Value::Toggle(on)) => c.tv.turn_off_on_restore = *on,
        _ => return false,
    }
    true
}

/// The opening picker's labels (Portuguese sources) and the values they stand for.
const INTRO_CHOICES: [(&str, &str); 9] = [
    ("Console Mode", cm_core::boot_intro::CONSOLE),
    ("Inspirada no PS1", cm_core::boot_intro::PS1), ("Inspirada no PS2", cm_core::boot_intro::PS2), ("Inspirada no PS3", cm_core::boot_intro::PS3), ("Inspirada no PS4", cm_core::boot_intro::PS4),
    ("Inspirada no Xbox", cm_core::boot_intro::XBOX), ("Inspirada no Xbox 360", cm_core::boot_intro::X360), ("Inspirada no Xbox One", cm_core::boot_intro::XONE),
    ("Nenhuma (abre direto)", cm_core::boot_intro::NONE),
];
fn intro_label(value: &str) -> &'static str { INTRO_CHOICES.iter().find(|(_, v)| *v == value).map(|(l, _)| *l).unwrap_or(INTRO_CHOICES[0].0) }
fn intro_value(label: &str) -> &'static str { INTRO_CHOICES.iter().find(|(l, _)| *l == label).map(|(_, v)| *v).unwrap_or(cm_core::boot_intro::CONSOLE) }

pub fn theme_index(c: &Config) -> u8 { match c.theme.as_str() { "ambar" => 1, "jade" => 2, _ => 0 } }

fn pick(options: &[&str], label: &str) -> usize { options.iter().position(|o| *o == label).unwrap_or(0) }

/// The picker shows the language in effect: the saved one, or (empty) the Windows display language.
fn language_label(code: &str) -> &'static str { crate::i18n::LANGUAGES[crate::i18n::resolve(code)].1 }
fn language_code(label: &str) -> &'static str { crate::i18n::LANGUAGES.iter().find(|(_, l)| *l == label).map(|(c, _)| *c).unwrap_or("pt-BR") }

// ── the card menu's "Ao abrir este jogo" (1.7 GameProfile) ──

const RESOLUTIONS: [(&str, u32, u32); 3] = [("3840 × 2160", 3840, 2160), ("2560 × 1440", 2560, 1440), ("1920 × 1080", 1920, 1080)];

/// The four values of the card menu from a game's profile.
pub fn game_values(p: Option<&GameProfile>, defaults: &mut [Value]) {
    let Some(p) = p else { return };
    if let Value::Choice(o, i) = &mut defaults[0] { *i = pick(o, match p.hdr { Some(true) => "Ligado", Some(false) => "Desligado", None => "Igual à sessão" }); }
    if let Value::Choice(o, i) = &mut defaults[1] {
        let label = p.mode.as_ref().and_then(|m| RESOLUTIONS.iter().find(|r| r.1 == m.width && r.2 == m.height)).map(|r| r.0).unwrap_or("Igual à sessão");
        *i = pick(o, label);
    }
    if let Value::Choice(o, i) = &mut defaults[2] {
        *i = match p.fps_limit { None => 0, Some(0) => pick(o, "Sem limite"), Some(n) => pick(o, &n.to_string()) };
    }
    if let Value::Choice(o, i) = &mut defaults[3] { *i = pick(o, if p.direct_launch == Some(false) { "Desligado" } else { "Ligado" }); }
}

/// Writes the four card-menu values into the game's profile.
pub fn store_game(values: &[Value], p: &mut GameProfile) {
    let label = |k: usize| values[k].text().unwrap_or("");
    p.hdr = match label(0) { "Ligado" => Some(true), "Desligado" => Some(false), _ => None };
    p.mode = RESOLUTIONS.iter().find(|r| r.0 == label(1)).map(|r| DisplayMode { width: r.1, height: r.2, frequency: p.mode.as_ref().map(|m| m.frequency).unwrap_or(0) });
    p.fps_limit = match label(2) { "Igual à sessão" => None, "Sem limite" => Some(0), n => n.parse().ok() };
    p.direct_launch = if label(3) == "Desligado" { Some(false) } else { None };
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::{overlays::game_defaults, settings};

    #[test]
    fn rows_round_trip_through_the_config() {
        let mut c = Config::default();
        c.theme = "jade".into();
        c.fps_limit = 120;
        c.controller_layout = "playstation".into();
        c.show_steam_games = true;
        c.start_with_windows = true;
        c.start_in_tray = true;
        c.protocol_registered = true;
        c.gaming_home_on_open = true;
        c.check_updates = false;
        let mut sys = settings::system();
        for cat in sys.iter_mut() { load(&mut cat.sections, &c); }
        let mut c2 = Config::default();
        for row in sys.iter().flat_map(|cat| cat.sections.iter()).flat_map(|s| s.rows.iter()) { store(row.title, &row.value, &mut c2); }
        assert_eq!((c2.theme.as_str(), c2.controller_layout.as_str(), c2.show_steam_games, c2.check_updates), ("jade", "playstation", true, false));

        assert!(c2.start_with_windows && c2.start_in_tray && c2.protocol_registered);
        assert!(c2.gaming_home_on_open);

        let mut ses = settings::session();
        load(&mut ses, &c);
        let mut c3 = Config::default();
        for row in ses.iter().flat_map(|s| s.rows.iter()) { store(row.title, &row.value, &mut c3); }
        assert_eq!(c3.fps_limit, 120);
    }

    #[test]
    fn controller_rows_round_trip_through_the_config() {
        use cm_core::shortcuts::{BACK, GUIDE, START, Y};
        let mut c = Config::default();
        c.home_shortcut = GUIDE as i32;
        c.menu_shortcut = (BACK | Y) as i32;
        c.exit_shortcut = (START | BACK) as i32;
        c.task_view_shortcut = 0;
        c.task_view_playstation = false;
        c.auto_start_on_controller = true;
        c.controller_mouse = true;
        c.controller_mouse_speed = "fast".into();
        let mut sys = settings::system();
        for cat in sys.iter_mut() { load(&mut cat.sections, &c); }
        let mut c2 = Config::default();
        c2.task_view_shortcut = 0x0001; // junk bits: the row overwrites it
        for row in sys.iter().flat_map(|cat| cat.sections.iter()).flat_map(|s| s.rows.iter()) { store(row.title, &row.value, &mut c2); }
        assert_eq!((c2.home_shortcut, c2.menu_shortcut, c2.exit_shortcut, c2.task_view_shortcut), (c.home_shortcut, c.menu_shortcut, c.exit_shortcut, 0));
        assert!(!c2.task_view_playstation && c2.auto_start_on_controller && c2.controller_mouse);
        assert_eq!(c2.controller_mouse_speed, "fast");
        // the defaults show as the 1.7 ones: Task View on (Select + X), the rest off
        let mut fresh = settings::system();
        for cat in fresh.iter_mut() { load(&mut cat.sections, &Config::default()); }
        let texts: Vec<_> = fresh.iter().flat_map(|cat| cat.sections.iter()).flat_map(|s| s.rows.iter()).filter(|r| crate::pad_shortcuts::slot_of_title(r.title).is_some()).map(|r| r.value.text().unwrap()).collect();
        assert_eq!(texts, ["Desligado", "Segurar ☰ (padrão)", "Desligado", "Select + X (Create + □)"]);
    }

    #[test]
    fn the_opening_rows_round_trip_through_the_config() {
        let mut c = Config::default();
        let rows = |c: &Config| { let mut sys = settings::system(); for cat in sys.iter_mut() { load(&mut cat.sections, c); } sys };
        let texts = |sys: &[settings::Category]| sys.iter().flat_map(|cat| cat.sections.iter()).flat_map(|s| s.rows.iter()).filter(|r| r.title == INTRO_STYLE_ROW).map(|r| r.value.text().unwrap()).collect::<Vec<_>>();
        // the default: the app's own, with its sound
        assert_eq!(texts(&rows(&c)), ["Console Mode"]);
        // pick the PS4 one, then "none": off, but the style stays
        let mut value = settings::Value::Choice(INTRO_CHOICES.iter().map(|x| x.0).collect(), 4);
        assert!(store(INTRO_STYLE_ROW, &value, &mut c));
        assert_eq!((c.intro_animation, c.intro_style.as_deref()), (true, Some("ps4")));
        if let settings::Value::Choice(_, i) = &mut value { *i = INTRO_CHOICES.len() - 1; }
        store(INTRO_STYLE_ROW, &value, &mut c);
        assert_eq!((c.intro_animation, c.intro_style.as_deref()), (false, Some("ps4")));
        assert_eq!(texts(&rows(&c)), ["Nenhuma (abre direto)"]);
        c.intro_sound = false;
        let sys = rows(&c);
        let sound: Vec<_> = sys.iter().flat_map(|cat| cat.sections.iter()).flat_map(|s| s.rows.iter()).filter(|r| r.title == INTRO_SOUND_ROW).map(|r| matches!(r.value, settings::Value::Toggle(false))).collect();
        assert_eq!(sound, [true]);
    }

    #[test]
    fn the_tv_rows_round_trip_through_the_config() {
        let mut c = Config::default();
        c.tv.provider = cm_core::tv::ANDROID_TV.into();
        c.tv.hdmi_input = 3;
        c.tv.turn_off_on_restore = true;
        c.tv.host = "192.168.0.50".into();
        let mut sys = settings::system();
        for cat in sys.iter_mut() { load(&mut cat.sections, &c); }
        let rows: Vec<_> = sys.iter().flat_map(|cat| cat.sections.iter()).flat_map(|s| s.rows.iter()).collect();
        let host = rows.iter().find(|r| r.title == tv::HOST_ROW).unwrap();
        assert_eq!(host.value.text(), Some("192.168.0.50"), "the typed value shows beside the row");
        let mut c2 = Config::default();
        for row in &rows { store(row.title, &row.value, &mut c2); }
        assert_eq!((c2.tv.provider.as_str(), c2.tv.hdmi_input, c2.tv.turn_off_on_restore), ("androidTv", 3, true));
        assert_eq!(c2.tv.host, "", "text rows are saved by the keyboard, not by the row");
    }

    #[test]
    fn game_profile_round_trip() {
        let mut v = game_defaults();
        v[0].activate(); // HDR → Ligado
        v[2].activate(); v[2].activate(); // FPS → 60
        v[3].activate(); // direct → Desligado
        let mut p = GameProfile::default();
        store_game(&v, &mut p);
        assert_eq!((p.hdr, p.fps_limit, p.direct_launch), (Some(true), Some(60), Some(false)));
        let mut back = game_defaults();
        game_values(Some(&p), &mut back);
        assert_eq!(back.iter().map(|x| x.text().unwrap()).collect::<Vec<_>>(), v.iter().map(|x| x.text().unwrap()).collect::<Vec<_>>());
    }
}
