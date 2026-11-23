//! What 1.7 does around a session besides the desk and the launcher: the RTSS frame limit and FPS
//! counter, parking the pointer, putting the desktop icons back, Steam's window to the tray, and
//! each Steam game's own settings while it runs. The engine (engine.rs) calls into [`Side`] at the
//! right moments; everything that must be undone is in the [`Journal`], so a crash is undone too.
//!
//! With the simulated desk nothing real is touched: each step only logs what it would do.

use std::{
    path::{Path, PathBuf},
    time::{Duration, Instant},
};

use cm_core::{
    config::{Config, DisplayMode, GameProfile},
    desktop_icons::{self, DesktopIcon},
    game_profiles::{self, GameChange},
    pointer, rtss_overlay,
    session::{self, Journal},
    steam_client,
};
use cm_platform::{
    pointer as os_pointer,
    rtss::{self, Counter, Rtss},
    steam_client as os_steam,
};

use crate::{engine::Desk, startup_log as log};

/// The parts of the config the extras need, copied into the plan when Jogar is pressed.
#[derive(Clone, Debug, Default)]
pub struct Extras {
    /// 0 = no limit.
    pub fps_limit: u32,
    pub fps_overlay: String,
    pub overlay_layout: rtss_overlay::FpsOverlayLayout,
    pub park_pointer: bool,
    pub restore_icons: bool,
    pub hide_icons: bool,
    pub profiles: Vec<GameProfile>,
    /// The session's own mode and HDR for the game screen (not a game's): what a game profile goes
    /// back to when the game closes.
    pub session_mode: Option<DisplayMode>,
    pub session_hdr: bool,
}

impl Extras {
    pub fn from_config(c: &Config) -> Self {
        Extras {
            fps_limit: c.fps_limit,
            fps_overlay: c.fps_overlay.clone(),
            overlay_layout: rtss_overlay::layout_from_config(&c.other),
            park_pointer: c.park_pointer,
            restore_icons: c.restore_desktop_icons,
            hide_icons: c.hide_desktop_icons,
            profiles: c.game_profiles.clone(),
            session_mode: c.monitor_modes.get(&c.focus_monitor).cloned(),
            session_hdr: c.hdr_enable,
        }
    }
}

/// The game screen as it was when a game started: what its profile undoes to.
#[derive(Clone, Debug, Default)]
struct Baseline {
    mode: Option<DisplayMode>,
    hdr: Option<bool>,
    fps: Option<u32>,
    audio: Option<String>,
}

/// One session's extras.
pub struct Side {
    sim: bool,
    cfg: Extras,
    data_dir: PathBuf,
    xbox: bool,
    /// The Steam game the card started: its profile counts as already applied by the plan.
    card_game: Option<u32>,
    launched_at: Option<Instant>,
    // pointer
    parks: u32,
    last_park: Option<Instant>,
    // RTSS
    rtss: Option<Rtss>,
    rtss_backed_up: bool,
    fps_now: u32,
    counter: Counter,
    // per-game profiles
    hdr_at_start: Option<bool>,
    game: u32,
    baseline: Option<Baseline>,
    profile_applied: bool,
}

impl Side {
    pub fn new(cfg: Extras, sim: bool, data_dir: &Path, xbox: bool, card_game: Option<u32>) -> Self {
        let fps_now = cfg.fps_limit;
        Side {
            sim, cfg, data_dir: data_dir.to_path_buf(), xbox, card_game, launched_at: None, parks: 0, last_park: None, rtss: None, rtss_backed_up: false, fps_now,
            counter: Counter::new(), hdr_at_start: None, game: 0, baseline: None, profile_applied: false,
        }
    }

    pub fn parks(&self) -> u32 { self.parks }
    pub fn data_dir(&self) -> &Path { &self.data_dir }
    pub fn counter(&self) -> &Counter { &self.counter }

    /// Before any screen changes: Windows may reshuffle the icons once the primary moves, and the
    /// pointer's spot is where it goes back to. Both go in the journal.
    pub fn snapshot(&mut self, desk: &mut dyn Desk, focus: &str, j: &mut Journal) {
        if self.cfg.restore_icons {
            if self.sim { log("ícones (simulado): posições seriam salvas"); } else {
                match cm_platform::desktop_icons::read() {
                    Ok(icons) => { log(&format!("Ícones da área de trabalho: {} posições salvas", icons.len())); j.desktop_icons = icons; }
                    Err(e) => log(&format!("Ícones da área de trabalho: {e}")),
                }
            }
        }
        // the clean look of a console (1.7.1 #210): only icons that are showing get hidden, so only
        // those come back. Read here, changing nothing; hidden once the journal says so (hide_icons).
        if self.cfg.hide_icons {
            if self.sim { log("ícones (simulado): seriam escondidos durante a sessão"); } else {
                match cm_platform::desktop_icons::showing() {
                    Ok(showing) => j.desktop_icons_hidden = showing,
                    Err(e) => log(&format!("Ícones da área de trabalho: não deu para ver se aparecem: {e}")),
                }
            }
        }
        if self.cfg.park_pointer && !self.sim { j.pointer = os_pointer::position(); }
        self.hdr_at_start = desk.hdr_state(focus);
    }

    /// After the journal is written: hides the desktop icons it says to hide, so a crash, a journal
    /// that failed to write or an Explorer call that hides them late can't leave them hidden.
    pub fn hide_icons(&self, j: &Journal) {
        if !j.desktop_icons_hidden || self.sim { return; }
        match cm_platform::desktop_icons::set_hidden(true) {
            Ok(_) => log("Ícones da área de trabalho: escondidos durante a sessão"),
            Err(e) => log(&format!("Ícones da área de trabalho: não deu para esconder: {e}")),
        }
    }

    /// After the screen is confirmed, before the launcher: the global frame limit and the counter.
    pub fn start_fps(&mut self, j: &mut Journal, journal_path: &Path) {
        if self.cfg.fps_limit > 0 { self.set_limit(self.cfg.fps_limit, j, journal_path); }
        if rtss_overlay::hides_others(&self.cfg.fps_overlay) {
            if self.sim { log("RTSS (simulado): contador de FPS seria exibido"); } else if rtss::is_installed() {
                // in the journal first: a crash while it shows must still clear it
                j.rtss_overlay = true;
                let _ = session::write_journal(journal_path, j);
                if !self.counter.apply(&self.cfg.fps_overlay, &self.cfg.overlay_layout) { log("RTSS: o contador de FPS não pôde ser exibido"); }
            }
        }
    }

    /// Sets RTSS's global limit (0 = none). The user's own settings are saved in the journal the
    /// first time only, so the restore puts back what they had, not what we set last.
    fn set_limit(&mut self, fps: u32, j: &mut Journal, journal_path: &Path) {
        if self.sim { log(&format!("RTSS (simulado): limite de {fps} FPS")); self.fps_now = fps; return; }
        if self.rtss.is_none() {
            match Rtss::open(&self.data_dir) { Ok(r) => self.rtss = Some(r), Err(e) => return log(&e) }
        }
        let rtss = self.rtss.as_ref().expect("opened above");
        if session::needs_rtss_backup(self.rtss_backed_up) {
            match rtss.backup() {
                Ok(b) => {
                    j.rtss = Some(b);
                    if session::write_journal(journal_path, j).is_err() { return log("RTSS: não deu para gravar o diário; limite de FPS não aplicado"); }
                    self.rtss_backed_up = true;
                }
                Err(e) => return log(&format!("RTSS: não foi possível ler as configurações atuais ({e}); limite de FPS não aplicado")),
            }
        }
        match rtss.set_limit(fps) {
            Ok(()) => { self.fps_now = fps; log(&if fps > 0 { format!("RTSS: limite de FPS global em {fps}") } else { "RTSS: sem limite de FPS".into() }); }
            Err(e) => log(&format!("RTSS: falha ao aplicar o limite: {e}")),
        }
    }

    /// The session menu changed the limit while the game runs (restored with the rest at the end).
    pub fn set_fps_live(&mut self, fps: u32, j: &mut Journal, journal_path: &Path) { self.set_limit(fps, j, journal_path); }

    pub fn launched(&mut self) { self.launched_at = Some(Instant::now()); }

    /// One poll of the running session: parks the pointer once the launcher is up and follows
    /// Steam's running game. `launcher_up`: the launcher's window has appeared.
    pub fn tick(&mut self, desk: &mut dyn Desk, j: &mut Journal, journal_path: &Path, focus: &str, launcher_up: bool) {
        self.tick_pointer(desk, focus, launcher_up);
        self.tick_game(desk, j, journal_path, focus);
    }

    fn tick_pointer(&mut self, desk: &mut dyn Desk, focus: &str, launcher_up: bool) {
        // Xbox mode shows no window to watch: it goes by the time since the launch
        let ready = if self.xbox { self.launched_at.is_some_and(|t| t.elapsed() >= pointer::XBOX_DELAY) } else { launcher_up };
        if !pointer::is_due(self.cfg.park_pointer, self.parks, ready, self.last_park.map(|t| t.elapsed())) { return; }
        let Some(rect) = desk.screen_rect(focus) else { return };
        let (x, y) = pointer::bottom_right(rect);
        if self.sim { log(&format!("Ponteiro (simulado): estacionaria em {x},{y}")); }
        else if os_pointer::move_to(x, y) { log(&format!("Ponteiro: estacionado em {x},{y}")); }
        else { log(&format!("Ponteiro: não foi possível mover para {x},{y}")); }
        self.parks += 1;
        self.last_park = Some(Instant::now());
    }

    fn tick_game(&mut self, desk: &mut dyn Desk, j: &mut Journal, journal_path: &Path, focus: &str) {
        if focus.is_empty() { return; }
        let previous = self.game;
        let current = os_steam::running_app_id();
        let change = game_profiles::change(previous, current);
        if change == GameChange::None { return; }
        if matches!(change, GameChange::Ended | GameChange::Switched) { self.end_game(desk, j, journal_path, focus, previous); }
        self.game = current.unwrap_or(0);
        if matches!(change, GameChange::Started | GameChange::Switched) { self.begin_game(desk, j, journal_path, focus, self.game); }
    }

    fn read_baseline(&self, desk: &mut dyn Desk, focus: &str, app_id: u32) -> Baseline {
        if self.card_game == Some(app_id) {
            // the plan already put this game's screen settings on; the session's own are the way back
            return Baseline { mode: self.cfg.session_mode.clone(), hdr: Some(self.cfg.session_hdr), fps: Some(self.cfg.fps_limit), audio: desk.audio_default() };
        }
        Baseline { mode: desk.current_mode(focus), hdr: desk.hdr_state(focus), fps: Some(self.fps_now), audio: desk.audio_default() }
    }

    fn begin_game(&mut self, desk: &mut dyn Desk, j: &mut Journal, jp: &Path, focus: &str, app_id: u32) {
        self.baseline = Some(self.read_baseline(desk, focus, app_id));
        self.profile_applied = false;
        let Some(p) = self.cfg.profiles.iter().find(|p| p.steam_app_id == app_id && !game_profiles::is_empty(p)).cloned() else {
            return log(&format!("Perfil por jogo: {app_id} começou, sem perfil"));
        };
        log(&format!("Perfil por jogo: aplicando o perfil de {} ({app_id})", p.name));
        self.apply(desk, j, jp, focus, p.mode.as_ref(), p.hdr, p.fps_limit, p.audio_device_id.as_deref());
        self.profile_applied = true;
    }

    fn end_game(&mut self, desk: &mut dyn Desk, j: &mut Journal, jp: &Path, focus: &str, app_id: u32) {
        if self.profile_applied {
            if let Some(b) = self.baseline.take() {
                log(&format!("Perfil por jogo: {app_id} fechou; voltando a tela de antes"));
                self.apply(desk, j, jp, focus, b.mode.as_ref(), b.hdr, b.fps, b.audio.as_deref());
            }
        }
        self.baseline = None;
        self.profile_applied = false;
    }

    /// The session is ending: a game profile still in effect is undone before the desk is restored.
    pub fn finish(&mut self, desk: &mut dyn Desk, j: &mut Journal, jp: &Path, focus: &str) {
        if self.game != 0 { let g = self.game; self.end_game(desk, j, jp, focus, g); self.game = 0; }
    }

    /// Each part on its own: one that fails is logged and the others still apply.
    #[allow(clippy::too_many_arguments)]
    fn apply(&mut self, desk: &mut dyn Desk, j: &mut Journal, jp: &Path, focus: &str, mode: Option<&DisplayMode>, hdr: Option<bool>, fps: Option<u32>, audio: Option<&str>) {
        if let Some(m) = mode.filter(|m| m.width > 0 && m.height > 0) {
            if !desk.current_mode(focus).is_some_and(|c| m.is_met_by(&c)) {
                if let Err(e) = desk.set_mode(focus, m) { log(&format!("Perfil por jogo: resolução: {e}")); }
            }
        }
        if let Some(on) = hdr {
            if desk.hdr_state(focus) != Some(on) {
                if on {
                    match desk.hdr_on(focus) {
                        // HDR was off before the session: the restore must turn it off again
                        Ok(true) if self.hdr_at_start == Some(false) && j.hdr_turned_on.is_empty() => { j.hdr_turned_on = focus.to_string(); let _ = session::write_journal(jp, j); }
                        Ok(_) => {}
                        Err(e) => log(&format!("Perfil por jogo: HDR: {e}")),
                    }
                } else {
                    desk.hdr_off(focus);
                }
            }
        }
        if let Some(f) = fps.filter(|f| *f != self.fps_now) { self.set_limit(f, j, jp); }
        if let Some(a) = audio.filter(|a| !a.trim().is_empty()) {
            if desk.audio_default().as_deref() != Some(a) {
                if let Err(e) = desk.set_audio(a) { log(&format!("Perfil por jogo: áudio: {e}")); }
            }
        }
    }
}

/// What the restore needs besides the journal.
pub struct Restore<'a> {
    pub sim: bool,
    pub data_dir: &'a Path,
    /// Closes Big Picture first (it is the launcher).
    pub big_picture: bool,
    /// And puts Steam's client window in the tray (the setting) unless a game runs.
    pub steam_to_tray: bool,
    /// Times the pointer was parked: it only goes back after at least one.
    pub parks: u32,
    pub counter: &'a Counter,
}

/// After Big Picture is closed: Steam's window to the tray, unless a game runs (1.7 CloseSteamForRestore).
pub fn steam_to_tray(setting: bool, sim: bool) {
    if sim { return log("Steam (simulado): janela iria para a bandeja se nenhum jogo rodasse"); }
    let id = os_steam::running_app_id();
    let decision = steam_client::decide("bigPicture", setting, os_steam::is_running(), id);
    log(&steam_client::describe(decision, id));
    if decision == steam_client::Decision::CloseToTray && !os_steam::close_window_to_tray() { log("Steam: não achei a janela do cliente para fechar"); }
}

/// Puts back what the extras changed, from the journal alone (so a crash is undone too): the
/// pointer, the icons (in the background: Explorer reshuffles them a moment after the screens
/// return, sometimes more than once) and RTSS.
pub fn restore_extras(r: &Restore, j: &Journal) {
    if j.desktop_icons_hidden {
        if r.sim { log("ícones (simulado): apareceriam de novo"); } else {
            log(match cm_platform::desktop_icons::set_hidden(false) {
                Ok(_) => "Ícones da área de trabalho: visíveis de novo".into(),
                Err(e) => format!("Ícones da área de trabalho: não foi possível mostrar de novo: {e}"),
            }.as_str());
        }
    }
    if !j.desktop_icons.is_empty() {
        if r.sim { log("ícones (simulado): voltariam ao lugar"); } else { restore_icons_later(j.desktop_icons.clone()); }
    }
    if let Some((x, y)) = j.pointer {
        if r.sim { log("Ponteiro (simulado): voltaria ao lugar de antes"); }
        else if pointer::should_return(Some((x, y)), r.parks, os_pointer::is_on_screen(x, y)) && os_pointer::move_to(x, y) { log(&format!("Ponteiro: de volta a {x},{y}")); }
    }
    if let Some(b) = &j.rtss {
        if r.sim { log("RTSS (simulado): limite voltaria ao de antes"); } else {
            match Rtss::open(r.data_dir).and_then(|rtss| rtss.restore(Some(b))) {
                Ok(()) => log(&format!("RTSS: limite de volta a {} (limitador {})", b.framerate_limit, if b.limiter_enabled { "ligado" } else { "desligado" })),
                Err(e) => log(&format!("RTSS: não foi possível restaurar: {e}")),
            }
        }
    }
    if j.rtss_overlay && !r.sim { r.counter.clear(); }
}

fn restore_icons_later(saved: Vec<DesktopIcon>) {
    let _ = std::thread::Builder::new().name("desktop-icons-restore".into()).spawn(move || {
        let mut total = 0;
        for delay in [1500, 2500, 4000] {
            std::thread::sleep(Duration::from_millis(delay));
            let Ok(current) = cm_platform::desktop_icons::read() else { break };
            let moves = desktop_icons::moves(&saved, &current);
            if moves.is_empty() { continue; }
            match cm_platform::desktop_icons::move_icons(&moves) { Ok(n) => total += n, Err(_) => break }
        }
        log(&format!("Ícones da área de trabalho: {total} reposicionados"));
    });
}
