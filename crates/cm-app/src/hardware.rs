//! This PC's screens and audio outputs, for Sessão. Read once at start (read only) and turned into
//! the choices of "Tela de jogo", "Outras telas", "Resolução", "Taxa de atualização" and "Saída de
//! áudio", which write the 1.7 config fields (focusMonitor, hideMonitors, monitorModes, audioDevice*).

use cm_core::config::{Config, DisplayMode};

use crate::{i18n::n, settings::{Section, Value}};

pub const KEEP: &str = n("Não mudar");
/// Tacked on to the name of a screen Windows can turn on but that is off now (shown translated).
pub const OFF_NOTE: &str = n(" · desligada");
const OTHERS_OFF: &str = n("Desligar na sessão");
const OTHERS_ON: &str = n("Deixar ligadas");

#[derive(Clone, Debug, Default)]
pub struct Screen {
    pub id: String,
    pub label: &'static str,
    pub modes: Vec<(u32, u32, u32)>,
    /// Where it sits on the desktop (None: off in Windows, so no place right now).
    pub pos: Option<(i32, i32)>,
    /// Its current size, or its biggest mode when it is off.
    pub size: (u32, u32),
    pub hz: Option<u32>,
    pub active: bool,
}

impl Screen {
    /// Its name without the " · desligada" note.
    pub fn name(&self) -> &'static str { self.label.split(" · ").next().unwrap_or(self.label) }
}

/// What a screen does in a session (Sessão → Telas).
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Role { Game, Off, Keep }

pub const ROLE_GAME: &str = n("Tela de jogo");
pub const ROLE_OFF: &str = n("Desligar na sessão");
pub const ROLE_KEEP: &str = n("Deixar ligada");
pub const ROLES: [&str; 3] = [ROLE_GAME, ROLE_OFF, ROLE_KEEP];

impl Role {
    pub fn label(self) -> &'static str { match self { Role::Game => ROLE_GAME, Role::Off => ROLE_OFF, Role::Keep => ROLE_KEEP } }
    pub fn of_index(i: usize) -> Role { [Role::Game, Role::Off, Role::Keep][i.min(2)] }
    pub fn index(self) -> usize { match self { Role::Game => 0, Role::Off => 1, Role::Keep => 2 } }
}

/// A screen on the map, in the units of the box it was fitted into.
#[derive(Clone, Copy, Debug, PartialEq)]
pub struct Tile { pub x: f32, pub y: f32, pub w: f32, pub h: f32 }

/// The screens laid out as in Windows' display settings (1.7 LayoutTiles): each at its desktop
/// place and real proportions (a screen standing up shows standing up), scaled to fit the box.
/// A screen that is off has no place now, or an old one overlapping the live ones: it is parked to
/// the right of the others.
pub fn layout(screens: &[Screen], box_w: f32, box_h: f32) -> Vec<Tile> {
    if screens.is_empty() { return vec![]; }
    type R = (f32, f32, f32, f32);
    let overlaps = |a: R, b: R| a.0 < b.0 + b.2 && b.0 < a.0 + a.2 && a.1 < b.1 + b.3 && b.1 < a.1 + a.3;
    let size = |s: &Screen| (s.size.0.max(1) as f32, s.size.1.max(1) as f32);
    let mut rects: Vec<Option<R>> = vec![None; screens.len()];
    let mut order: Vec<usize> = (0..screens.len()).collect();
    order.sort_by_key(|&i| !screens[i].active);
    for &i in &order {
        let s = &screens[i];
        let (w, h) = size(s);
        if let Some((x, y)) = s.pos {
            let r = (x as f32, y as f32, w, h);
            if s.active || !rects.iter().flatten().any(|o| overlaps(r, *o)) { rects[i] = Some(r); }
        }
    }
    for &i in &order {
        if rects[i].is_some() { continue; }
        let (w, h) = size(&screens[i]);
        let placed: Vec<R> = rects.iter().flatten().copied().collect();
        let r = if placed.is_empty() { (0., 0., w, h) } else {
            let right = placed.iter().map(|r| r.0 + r.2).fold(f32::MIN, f32::max);
            let top = placed.iter().map(|r| r.1).fold(f32::MAX, f32::min);
            (right + 240., top, w, h)
        };
        rects[i] = Some(r);
    }
    let rs: Vec<R> = rects.into_iter().flatten().collect();
    let left = rs.iter().map(|r| r.0).fold(f32::MAX, f32::min);
    let top = rs.iter().map(|r| r.1).fold(f32::MAX, f32::min);
    let width = rs.iter().map(|r| r.0 + r.2).fold(f32::MIN, f32::max) - left;
    let height = rs.iter().map(|r| r.1 + r.3).fold(f32::MIN, f32::max) - top;
    let k = (box_w / width).min(box_h / height);
    rs.iter().map(|r| Tile { x: (r.0 - left) * k, y: (r.1 - top) * k, w: r.2 * k, h: r.3 * k }).collect()
}

#[derive(Clone, Debug, Default)]
pub struct Output { pub id: String, pub label: &'static str }

#[derive(Clone, Debug, Default)]
pub struct Hardware { pub screens: Vec<Screen>, pub outputs: Vec<Output> }

/// Labels live as long as the app (choices hold `&'static str`); a handful of short strings.
fn leak(s: String) -> &'static str { Box::leak(s.into_boxed_str()) }

impl Hardware {
    #[cfg(windows)]
    pub fn read() -> Self {
        // screens Windows can turn on: they have a GDI source (the others are old registry entries)
        let displays: Vec<_> = cm_platform::displays::list_displays().unwrap_or_default().into_iter().filter(|d| !d.gdi_name.is_empty()).collect();
        let mut screens = Vec::new();
        for d in &displays {
            let twins = displays.iter().filter(|o| o.friendly_name == d.friendly_name).count();
            let name = if twins > 1 { format!("{} ({})", d.friendly_name, d.gdi_name.trim_start_matches(r"\\.\")) } else { d.friendly_name.clone() };
            let label = if d.active { name } else { format!("{name}{OFF_NOTE}") };
            let biggest = d.supported_modes.iter().max_by_key(|m| m.width * m.height).map(|m| (m.width, m.height)).unwrap_or((1920, 1080));
            let current = d.current_mode.as_ref().filter(|_| d.active);
            screens.push(Screen {
                id: d.stable_id.clone(), label: leak(label), modes: d.supported_modes.iter().map(|m| (m.width, m.height, m.refresh_hz)).collect(),
                pos: d.position.filter(|_| d.active), size: current.map(|m| (m.width, m.height)).unwrap_or(biggest),
                hz: current.map(|m| m.refresh_hz), active: d.active,
            });
        }
        let outputs = cm_platform::audio::list_outputs().unwrap_or_default().into_iter().filter(|a| a.is_active)
            .map(|a| Output { id: a.endpoint_id, label: leak(a.friendly_name) }).collect();
        Hardware { screens, outputs }
    }

    fn screen(&self, id: &str) -> Option<&Screen> { self.screens.iter().find(|s| s.id.eq_ignore_ascii_case(id)) }

    /// The role of a screen in the config.
    pub fn role(&self, id: &str, c: &Config) -> Role {
        if !c.focus_monitor.is_empty() && c.focus_monitor.eq_ignore_ascii_case(id) { Role::Game }
        else if c.hide_monitors.iter().any(|h| h.eq_ignore_ascii_case(id)) { Role::Off }
        else { Role::Keep }
    }

    /// Gives a screen a role. A new game screen swaps roles with the old one (the old game screen
    /// gets what the new one had: turned off or kept on). The game screen itself is never turned
    /// off or merely kept: another one has to become the game screen first.
    pub fn set_role(&self, id: &str, role: Role, c: &mut Config) {
        let Some(s) = self.screen(id) else { return };
        let (id, label) = (s.id.clone(), s.label);
        let before = self.role(&id, c);
        if before == role || before == Role::Game { return; }
        match role {
            Role::Game => {
                let old = self.screen(&c.focus_monitor).map(|o| o.id.clone());
                // same rules as picking it in a list (drops a mode the new screen lacks)
                let hide = c.hide_monitors.clone();
                self.store("Tela de jogo", &Value::Choice(vec![label], 0), c);
                c.hide_monitors = hide;
                c.hide_monitors.retain(|h| !h.eq_ignore_ascii_case(&id));
                if let (Some(old), Role::Off) = (old, before) { c.hide_monitors.push(old); }
            }
            Role::Off => c.hide_monitors.push(id),
            Role::Keep => c.hide_monitors.retain(|h| !h.eq_ignore_ascii_case(&id)),
        }
    }

    /// Resolutions of a screen, big → small.
    fn resolutions(&self, id: &str) -> Vec<(u32, u32)> {
        let mut r: Vec<(u32, u32)> = self.screen(id).map(|s| s.modes.iter().map(|m| (m.0, m.1)).collect()).unwrap_or_default();
        r.sort_by(|a, b| (b.0 * b.1).cmp(&(a.0 * a.1)));
        r.dedup();
        r
    }

    fn rates(&self, id: &str, w: u32, h: u32) -> Vec<u32> {
        let mut r: Vec<u32> = self.screen(id).map(|s| s.modes.iter().filter(|m| m.0 == w && m.1 == h).map(|m| m.2).collect()).unwrap_or_default();
        r.sort_by(|a, b| b.cmp(a));
        r.dedup();
        r
    }

    /// The Telas, Imagem and Som rows from this PC and the config (rebuilt whenever one changes).
    pub fn fill(&self, sections: &mut [Section], c: &Config) {
        if self.screens.is_empty() { return; }
        let focus = self.screen(&c.focus_monitor).map(|s| s.id.clone()).unwrap_or_default();
        let mode = c.monitor_modes.get(&focus).cloned();
        for row in sections.iter_mut().flat_map(|s| s.rows.iter_mut()) {
            row.value = match row.title {
                "Tela de jogo" => {
                    let mut o: Vec<&'static str> = if focus.is_empty() { vec![n("Escolha a tela")] } else { vec![] };
                    o.extend(self.screens.iter().map(|s| s.label));
                    let i = o.iter().position(|l| self.screen(&focus).is_some_and(|s| s.label == *l)).unwrap_or(0);
                    Value::Choice(o, i)
                }
                "Outras telas" => {
                    let off = !c.hide_monitors.is_empty() || (focus.is_empty() && self.screens.len() > 1);
                    Value::Choice(vec![OTHERS_OFF, OTHERS_ON], if off { 0 } else { 1 })
                }
                "Resolução" => {
                    let mut o = vec![KEEP];
                    o.extend(self.resolutions(&focus).into_iter().map(|(w, h)| leak(format!("{w} × {h}"))));
                    let i = mode.as_ref().and_then(|m| o.iter().position(|l| *l == format!("{} × {}", m.width, m.height))).unwrap_or(0);
                    Value::Choice(o, i)
                }
                "Taxa de atualização" => {
                    let mut o = vec![KEEP];
                    if let Some(m) = &mode { o.extend(self.rates(&focus, m.width, m.height).into_iter().map(|r| leak(format!("{r} Hz")))); }
                    let i = mode.as_ref().and_then(|m| o.iter().position(|l| *l == format!("{} Hz", m.frequency))).unwrap_or(0);
                    Value::Choice(o, i)
                }
                "Saída de áudio" => {
                    let mut o = vec![KEEP];
                    o.extend(self.outputs.iter().map(|a| a.label));
                    let i = self.outputs.iter().position(|a| a.id == c.audio_device_id).map(|i| i + 1).unwrap_or(0);
                    Value::Choice(o, i)
                }
                _ => continue,
            };
            row.desc = match row.title {
                "Outras telas" => {
                    // the screens the config really turns off (1.7 may have left one out on purpose)
                    let off = |s: &&Screen| if c.hide_monitors.is_empty() { s.id != focus } else { c.hide_monitors.iter().any(|h| h.eq_ignore_ascii_case(&s.id)) };
                    let others: Vec<&str> = self.screens.iter().filter(off).map(|s| s.label.split(" · ").next().unwrap_or(s.label)).collect();
                    if others.is_empty() { n("Não há outras telas.") } else { leak(others.join(", ")) }
                }
                _ => row.desc,
            };
        }
    }

    /// One of the hardware rows changed: write it into the config. True when it was one of them.
    pub fn store(&self, title: &str, value: &Value, c: &mut Config) -> bool {
        let Value::Choice(o, i) = value else { return false };
        let label = o[*i];
        match title {
            "Tela de jogo" => {
                let Some(s) = self.screens.iter().find(|s| s.label == label) else { return true };
                c.focus_monitor = s.id.clone();
                if !c.hide_monitors.is_empty() { c.hide_monitors = self.others(&s.id); }
                // the mode chosen for another screen may not exist on this one
                if let Some(m) = c.monitor_modes.get(&s.id) {
                    if !self.screen(&s.id).is_some_and(|sc| sc.modes.iter().any(|x| (x.0, x.1, x.2) == (m.width, m.height, m.frequency))) { c.monitor_modes.remove(&s.id); }
                }
            }
            "Outras telas" => c.hide_monitors = if label == OTHERS_OFF { self.others(&c.focus_monitor) } else { vec![] },
            "Resolução" => {
                let focus = c.focus_monitor.clone();
                if label == KEEP { c.monitor_modes.remove(&focus); return true; }
                let Some((w, h)) = parse_res(label) else { return true };
                // keep the refresh rate if this resolution has it, else its highest
                let rates = self.rates(&focus, w, h);
                let keep = c.monitor_modes.get(&focus).map(|m| m.frequency).filter(|f| rates.contains(f));
                c.monitor_modes.insert(focus, DisplayMode { width: w, height: h, frequency: keep.or(rates.first().copied()).unwrap_or(60) });
            }
            "Taxa de atualização" => {
                let focus = c.focus_monitor.clone();
                if label == KEEP { c.monitor_modes.remove(&focus); return true; }
                if let (Some(m), Ok(hz)) = (c.monitor_modes.get_mut(&focus), label.trim_end_matches(" Hz").parse()) { m.frequency = hz; }
            }
            "Saída de áudio" => match self.outputs.iter().find(|a| a.label == label) {
                Some(a) => { c.audio_device_id = a.id.clone(); c.audio_device_name = a.label.to_string(); c.audio_auto_switch = false; }
                None => { c.audio_device_id.clear(); c.audio_device_name = "Don't change".into(); }
            },
            _ => return false,
        }
        true
    }

    fn others(&self, focus: &str) -> Vec<String> { self.screens.iter().filter(|s| !s.id.eq_ignore_ascii_case(focus)).map(|s| s.id.clone()).collect() }

    /// The name shown for the game screen ("TV LG"), or a hint when none is chosen.
    pub fn focus_label(&self, c: &Config) -> String {
        self.screen(&c.focus_monitor).map(|s| s.label.split(" · ").next().unwrap_or(s.label).to_string()).unwrap_or_else(|| n("Escolha em Sessão").into())
    }

}

fn parse_res(l: &str) -> Option<(u32, u32)> {
    let (w, h) = l.split_once(" × ")?;
    Some((w.parse().ok()?, h.parse().ok()?))
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::settings;

    fn hw() -> Hardware {
        Hardware {
            screens: vec![
                Screen { id: "MONITOR\\TV".into(), label: "TV LG · desligada", modes: vec![(3840, 2160, 120), (3840, 2160, 60), (1920, 1080, 120)], pos: None, size: (3840, 2160), hz: None, active: false },
                Screen { id: "MONITOR\\HP".into(), label: "HP V225hz", modes: vec![(1920, 1080, 75), (1920, 1080, 60)], pos: Some((0, 0)), size: (1920, 1080), hz: Some(75), active: true },
            ],
            outputs: vec![Output { id: "{0.0.0}.{tv}".into(), label: "TV (HDMI)" }],
        }
    }

    fn row<'a>(s: &'a mut [Section], title: &str) -> &'a mut Value { &mut s.iter_mut().flat_map(|x| x.rows.iter_mut()).find(|r| r.title == title).unwrap().value }

    fn three() -> Hardware {
        let sc = |id: &str, pos: Option<(i32, i32)>, size: (u32, u32), active: bool| Screen { id: id.into(), label: "x", modes: vec![], pos, size, hz: None, active };
        Hardware { screens: vec![
            sc("A", Some((0, 0)), (2560, 1440), true),
            sc("B", Some((2560, 0)), (1080, 1920), true),   // standing up, to the right
            sc("TV", None, (3840, 2160), false),             // off: parked to the right
        ], outputs: vec![] }
    }

    #[test]
    fn the_map_keeps_places_and_proportions_and_parks_screens_that_are_off() {
        let t = layout(&three().screens, 1000., 400.);
        assert_eq!(t[0].x, 0.);
        assert!((t[1].x - t[0].w).abs() < 0.01, "B sits right of A");
        assert!(t[1].h > t[1].w, "standing up shows standing up");
        assert!(t[2].x > t[1].x + t[1].w, "the TV that is off is parked to the right");
        let right = t.iter().map(|r| r.x + r.w).fold(0., f32::max);
        let bottom = t.iter().map(|r| r.y + r.h).fold(0., f32::max);
        assert!(right <= 1000.01 && bottom <= 400.01);
    }

    #[test]
    fn roles_swap_when_the_game_screen_moves_and_the_game_screen_stays_on() {
        let mut h = three();
        // set_role finds screens by label too (store): give them distinct ones
        for (s, l) in h.screens.iter_mut().zip(["A", "B", "TV"]) { s.label = l; }
        let mut c = Config::default();
        c.focus_monitor = "TV".into();
        c.hide_monitors = vec!["A".into()];
        assert_eq!((h.role("TV", &c), h.role("A", &c), h.role("B", &c)), (Role::Game, Role::Off, Role::Keep));
        // the game screen cannot just be turned off
        h.set_role("TV", Role::Off, &mut c);
        assert_eq!(h.role("TV", &c), Role::Game);
        // A (off) becomes the game screen: the TV takes its place among the screens turned off
        h.set_role("A", Role::Game, &mut c);
        assert_eq!((h.role("A", &c), h.role("TV", &c), h.role("B", &c)), (Role::Game, Role::Off, Role::Keep));
        // B (kept on) becomes the game screen: A is kept on
        h.set_role("B", Role::Game, &mut c);
        assert_eq!((h.role("B", &c), h.role("A", &c), h.role("TV", &c)), (Role::Game, Role::Keep, Role::Off));
        h.set_role("TV", Role::Keep, &mut c);
        assert!(c.hide_monitors.is_empty());
        h.set_role("A", Role::Off, &mut c);
        assert_eq!(c.hide_monitors, vec!["A".to_string()]);
    }

    #[test]
    fn choosing_the_tv_and_a_mode_writes_the_17_fields() {
        let h = hw();
        let mut c = Config::default();
        let mut s = settings::session();
        h.fill(&mut s, &c);
        // no screen yet: the first choice asks for one
        assert_eq!(row(&mut s, "Tela de jogo").text(), Some("Escolha a tela"));
        let v = row(&mut s, "Tela de jogo"); v.activate();
        h.store("Tela de jogo", &v.clone(), &mut c);
        assert_eq!(c.focus_monitor, "MONITOR\\TV");
        h.store("Outras telas", &Value::Choice(vec![OTHERS_OFF, OTHERS_ON], 0), &mut c);
        assert_eq!(c.hide_monitors, vec!["MONITOR\\HP".to_string()]);
        h.fill(&mut s, &c);
        let v = row(&mut s, "Resolução"); v.activate(); // Não mudar → 3840 × 2160
        h.store("Resolução", &v.clone(), &mut c);
        assert_eq!(c.monitor_modes["MONITOR\\TV"], DisplayMode { width: 3840, height: 2160, frequency: 120 });
        h.fill(&mut s, &c);
        assert_eq!(row(&mut s, "Taxa de atualização").text(), Some("120 Hz"));
        let v = row(&mut s, "Saída de áudio"); v.activate();
        h.store("Saída de áudio", &v.clone(), &mut c);
        assert_eq!(c.audio_device_id, "{0.0.0}.{tv}");
        assert_eq!(h.focus_label(&c), "TV LG");
    }

    #[test]
    fn switching_screens_drops_a_mode_the_new_one_lacks() {
        let h = hw();
        let mut c = Config::default();
        c.focus_monitor = "MONITOR\\HP".into();
        c.monitor_modes.insert("MONITOR\\HP".into(), DisplayMode { width: 3840, height: 2160, frequency: 120 });
        h.store("Tela de jogo", &Value::Choice(vec!["HP V225hz"], 0), &mut c);
        assert!(c.monitor_modes.get("MONITOR\\HP").is_none());
    }
}
