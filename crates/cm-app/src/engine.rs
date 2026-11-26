//! The console session: switch the desk to the game screen, open the launcher, follow it until it
//! closes, put everything back. Runs on its own thread; the UI gets [`Event`]s and sends [`Command`]s.
//!
//! Screens, audio and HDR go through a [`Desk`]. `SimulatedDesk` only writes down what it would do,
//! so the whole flow is tested without touching the owner's screens; the real one lands after its
//! own careful tests (docs/v2/PLANO.md, passo 3a).

use std::{
    path::{Path, PathBuf},
    sync::mpsc,
    time::{Duration, Instant},
};

use cm_core::{
    config::DisplayMode,
    session::{self, Journal, Launcher},
    tv::{TvControl, TvRestore},
};
use futures::channel::mpsc as fmpsc;

use crate::{i18n::{t, tf}, startup_log as log, video};

// declared here, not in main.rs: the session extras belong to the engine and nothing else uses them
#[path = "session_extras.rs"]
pub mod session_extras;
use session_extras::{Extras, Restore, Side};

/// Screens, audio and HDR. Every call that changes something has its undo in `restore`.
pub trait Desk: Send {
    /// Saves the current display configuration somewhere `restore` can read it; returns where.
    fn backup(&mut self) -> Result<String, String>;
    fn restore(&mut self, backup: &str) -> Result<(), String>;
    /// Turns the game screen on (if needed) and makes it the primary one.
    fn focus(&mut self, id: &str) -> Result<(), String>;
    fn hide(&mut self, ids: &[String]) -> Result<(), String>;
    fn set_mode(&mut self, id: &str, mode: &DisplayMode) -> Result<(), String>;
    /// True when HDR was off and is now on (so the restore turns it off again).
    fn hdr_on(&mut self, id: &str) -> Result<bool, String>;
    fn hdr_off(&mut self, id: &str) -> Result<(), String>;
    fn audio_default(&mut self) -> Option<String>;
    fn set_audio(&mut self, id: &str) -> Result<(), String>;
    /// The game screen's rectangle, to put the launcher's window there.
    fn screen_rect(&mut self, id: &str) -> Option<(i32, i32, i32, i32)>;
    /// The mode / HDR state a screen has now (read only; None = can't tell). A game's own settings
    /// go back to these when it closes.
    fn current_mode(&mut self, _id: &str) -> Option<DisplayMode> { None }
    fn hdr_state(&mut self, _id: &str) -> Option<bool> { None }
    /// Windows' VRR setting text (None: the value is not there), and writing it back (None removes it).
    fn vrr_read(&mut self) -> Option<String> { None }
    fn vrr_write(&mut self, _text: Option<&str>) -> Result<(), String> { Ok(()) }
    /// Turns the TV on and to the PC's input (Sistema → TV); only logged when it fails, never stops the session.
    fn tv_on(&mut self, _tv: &TvControl, _data_dir: &Path) {}
    /// The going-back step the journal holds: the user's command, then standby if on.
    fn tv_back(&mut self, _step: &TvRestore, _data_dir: &Path) {}
    fn simulated(&self) -> bool { false }
}

/// Changes nothing: logs each step. Used until the real desk is tested on the owner's PC.
#[derive(Default)]
pub struct SimulatedDesk { hdr: bool, vrr: Option<String> }

impl Desk for SimulatedDesk {
    fn backup(&mut self) -> Result<String, String> { log("sessão (simulada): backup das telas"); Ok("simulated".into()) }
    fn restore(&mut self, _: &str) -> Result<(), String> { log("sessão (simulada): telas restauradas"); Ok(()) }
    fn focus(&mut self, id: &str) -> Result<(), String> { log(&format!("sessão (simulada): tela de jogo {id}")); Ok(()) }
    fn hide(&mut self, ids: &[String]) -> Result<(), String> { log(&format!("sessão (simulada): desligar {ids:?}")); Ok(()) }
    fn set_mode(&mut self, id: &str, m: &DisplayMode) -> Result<(), String> { log(&format!("sessão (simulada): {id} em {}x{}@{}", m.width, m.height, m.frequency)); Ok(()) }
    fn hdr_on(&mut self, id: &str) -> Result<bool, String> { log(&format!("sessão (simulada): HDR em {id}")); let was = std::mem::replace(&mut self.hdr, true); Ok(!was) }
    fn hdr_off(&mut self, id: &str) -> Result<(), String> { log(&format!("sessão (simulada): HDR desligado em {id}")); self.hdr = false; Ok(()) }
    fn hdr_state(&mut self, _: &str) -> Option<bool> { Some(self.hdr) }
    fn vrr_read(&mut self) -> Option<String> { self.vrr.clone() }
    fn vrr_write(&mut self, text: Option<&str>) -> Result<(), String> { log(&format!("sessão (simulada): VRR {}", text.map(|t| format!("= {t}")).unwrap_or_else(|| "sem valor".into()))); self.vrr = text.map(str::to_string); Ok(()) }
    fn audio_default(&mut self) -> Option<String> { None }
    fn set_audio(&mut self, id: &str) -> Result<(), String> { log(&format!("sessão (simulada): áudio {id}")); Ok(()) }
    // never talks to a TV: tests run next to the owner's
    fn tv_on(&mut self, tv: &TvControl, _: &Path) { log(&format!("sessão (simulada): TV ligaria ({}) e trocaria para \"{}\"", tv.provider, tv.input_shell())); }
    fn tv_back(&mut self, step: &TvRestore, _: &Path) { log(&format!("sessão (simulada): TV ao voltar ao PC: {:?}", cm_core::tv::adb::restore_commands(&step.restore_command, step.standby))); }
    fn screen_rect(&mut self, _: &str) -> Option<(i32, i32, i32, i32)> { std::env::var_os("CM2_SIMULATED_CONFIRM").and_then(|_| cm_platform::primary_rect()) }
    fn simulated(&self) -> bool { true }
}

/// Everything a session needs, decided by the UI from the config and the card that was played.
#[derive(Clone, Debug)]
pub struct Plan {
    pub use_fse: bool,
    pub focus: String,
    pub hide: Vec<String>,
    pub mode: Option<DisplayMode>,
    pub hdr: bool,
    /// Windows' variable refresh rate setting on for the session (config `vrrEnable`).
    pub vrr: bool,
    pub audio: String,
    pub launcher: Launcher,
    pub playnite_path: String,
    /// Steam game to open inside Big Picture, if a game card was played.
    pub steam_game: Option<u32>,
    pub close_steam_on_restore: bool,
    /// The game screen/mode was never confirmed: ask before launching, roll back if nobody answers.
    pub needs_confirm: bool,
    /// RTSS, pointer, desktop icons and per-game profiles (session_extras.rs).
    pub extras: Extras,
    /// The card's name, to say "<name> fechou" when a custom app closes (empty: the process name).
    pub card_name: String,
    /// Turning the TV on and to the PC's input (config `tv`).
    pub tv: TvControl,
}

#[derive(Debug)]
pub enum Event {
    /// Step `n` of the opening is done (0 screen, 1 other screens, 2 audio/HDR, 3 launcher).
    Step(usize),
    /// Asks "can you see this?" on the game screen (its rectangle in pixels, to put the window there); answer
    /// with `Command::Confirmed`.
    Confirm(Option<(i32, i32, i32, i32)>),
    /// HDR of the game screen as it is now (None: Windows can't tell), for the session menu.
    Hdr(Option<bool>),
    /// Windows' VRR as it is now, for the session menu.
    Vrr(Option<bool>),
    Running,
    Ended(String),
    Failed(String),
}

/// `SetFps`: the session menu's FPS panel (0 = no limit), applied through RTSS right away.
pub enum Command { Stop, Confirmed(bool), SetFps(u32), /// The session menu's HDR toggle.
    SetHdr(bool), /// The session menu's VRR toggle.
    SetVrr(bool) }

pub const CONFIRM_TIMEOUT: Duration = Duration::from_secs(15);

pub struct Handle { pub events: fmpsc::UnboundedReceiver<Event>, pub commands: mpsc::Sender<Command> }

pub fn start(plan: Plan, desk: Box<dyn Desk>, journal: PathBuf) -> Handle {
    let (etx, erx) = fmpsc::unbounded();
    let (ctx, crx) = mpsc::channel();
    std::thread::Builder::new().name("session".into()).spawn(move || run(plan, desk, journal, etx, crx)).expect("session thread");
    Handle { events: erx, commands: ctx }
}

fn run(plan: Plan, mut desk: Box<dyn Desk>, journal_path: PathBuf, tx: fmpsc::UnboundedSender<Event>, rx: mpsc::Receiver<Command>) {
    let send = |e: Event| { let _ = tx.unbounded_send(e); };
    log(&format!("sessão: início, foco={}, esconder={:?}, iniciador={}", plan.focus, plan.hide, plan.launcher.mode()));
    // the game screen is never among the ones turned off
    let mut plan = plan;
    plan.hide.retain(|h| !h.eq_ignore_ascii_case(&plan.focus));

    // Never overwrite an unfinished (or unreadable) recovery record.
    if journal_path.try_exists().unwrap_or(true) {
        return send(Event::Failed(t("Reabra o Console Mode para concluir a restauração da sessão").into()));
    }

    let needs_fse = plan.use_fse && !desk.simulated() && crate::gaming_home::active() != Some(true);
    if needs_fse && (crate::gaming_home::supported() != Some(true) || crate::gaming_home::active().is_none()) {
        return send(Event::Failed(t("Não foi possível iniciar a sessão com FSE").into()));
    }
    if plan.use_fse && !desk.simulated() && !cm_platform::fse_win::status().is_ok_and(|s| s.registered && s.selected) {
        return send(Event::Failed(t(crate::gaming_home::HOME_REQUIRED).into()));
    }

    // 1. the journal, before anything changes: without it there is no way back after a crash
    let backup = match desk.backup() { Ok(b) => b, Err(e) => return send(Event::Failed(tf("Não deu para guardar o setup das telas: {0}", &[&e]))) };
    let mut journal = Journal { started_at: Journal::now(), launcher: plan.launcher.mode().into(), displays_backup: backup.clone(), audio_before: desk.audio_default().unwrap_or_default(), ..Default::default() };
    // icons and pointer are read before the screens change, and go in the journal with the rest
    let data_dir = journal_path.parent().unwrap_or(std::path::Path::new(".")).to_path_buf();
    let mut side = Side::new(plan.extras.clone(), desk.simulated(), &data_dir, plan.launcher == Launcher::Xbox, plan.steam_game);
    side.snapshot(desk.as_mut(), &plan.focus, &mut journal);
    // the TV's going-back step goes in the journal before the TV is touched, so a crash still does it
    journal.tv = plan.tv.restore_step();
    if let Err(e) = session::write_journal(&journal_path, &journal) {
        return send(Event::Failed(tf("Não deu para gravar o diário da sessão ({0}); nada foi alterado", &[&e])));
    }
    side.hide_icons(&journal);
    // wake the TV and switch it to the PC first, so the game screen is there when it is turned on
    if plan.tv.is_enabled() { desk.tv_on(&plan.tv, &data_dir); }

    // 2. the desk; any failure from here on puts it back
    let undo = |desk: &mut Box<dyn Desk>, journal: &mut Journal, side: &mut Side, why: String| {
        finish_session(desk.as_mut(), side, journal, &journal_path, &plan, false);
        send(Event::Failed(why));
    };
    if let Err(e) = desk.focus(&plan.focus) { return undo(&mut desk, &mut journal, &mut side, tf("A tela de jogo não ligou: {0}", &[&e])); }
    send(Event::Step(0));
    if !plan.hide.is_empty() { if let Err(e) = desk.hide(&plan.hide) { return undo(&mut desk, &mut journal, &mut side, tf("Não deu para desligar as outras telas: {0}", &[&e])); } }
    send(Event::Step(1));
    if let Some(m) = &plan.mode { if let Err(e) = desk.set_mode(&plan.focus, m) { log(&format!("sessão: modo não aplicado: {e}")); } }
    if plan.hdr && desk.hdr_state(&plan.focus) == Some(false) {
        journal.hdr_turned_on = plan.focus.clone();
        if let Err(e) = session::write_journal(&journal_path, &journal) {
            journal.hdr_turned_on.clear();
            log(&format!("HDR: não deu para gravar o diário ({e}); não aplicado"));
        } else if let Err(e) = desk.hdr_on(&plan.focus) {
            // The OS operation may have partially succeeded; keep the undo record.
            log(&format!("sessão: HDR não ligou: {e}"));
        }
    }
    if plan.vrr { video::apply_vrr(desk.as_mut(), &mut journal, &journal_path); }
    send(Event::Hdr(desk.hdr_state(&plan.focus)));
    send(Event::Vrr(Some(cm_core::video_features::vrr_enabled(&desk.vrr_read().unwrap_or_default()))));
    if !plan.audio.is_empty() && !journal.audio_before.is_empty() {
        if let Err(e) = desk.set_audio(&plan.audio) { log(&format!("sessão: áudio não trocou: {e}")); }
    } else if !plan.audio.is_empty() {
        log("sessão: áudio não trocou porque não foi possível guardar a saída anterior");
    }
    send(Event::Step(2));

    // 3. a setup nobody confirmed yet: ask on the game screen, roll back if nobody answers
    // (a simulated desk skips it, unless a test asks for it with CM2_SIMULATED_CONFIRM)
    if plan.needs_confirm && (!desk.simulated() || std::env::var_os("CM2_SIMULATED_CONFIRM").is_some()) {
        send(Event::Confirm(desk.screen_rect(&plan.focus)));
        let answer = rx.recv_timeout(CONFIRM_TIMEOUT);
        if !matches!(answer, Ok(Command::Confirmed(true))) {
            return undo(&mut desk, &mut journal, &mut side, t("A tela de jogo não foi confirmada; tudo voltou como estava").into());
        }
    }

    if needs_fse && crate::gaming_home::active() != Some(true) {
        // Recheck after screen confirmation: the selection may have changed while preparing.
        if !cm_platform::fse_win::status().is_ok_and(|s| s.registered && s.selected) {
            return undo(&mut desk, &mut journal, &mut side, t(crate::gaming_home::HOME_REQUIRED).into());
        }
        journal.exit_fse = true;
        if session::write_journal(&journal_path, &journal).is_err() {
            return undo(&mut desk, &mut journal, &mut side, t("Não foi possível iniciar a sessão com FSE").into());
        }
        log("sessão: solicitando o FSE com a Home do Console Mode");
        if let Err(error) = crate::gaming_home::session_state(true) {
            log(&format!("sessão: FSE não entrou: {error}"));
            return undo(&mut desk, &mut journal, &mut side, t("Não foi possível iniciar a sessão com FSE").into());
        }
        if !cm_platform::fse_win::status().is_ok_and(|s| s.selected) {
            return undo(&mut desk, &mut journal, &mut side, t("O Windows não manteve a Home escolhida. A FSE continua usando a Home definida pelo sistema.").into());
        }
    }
    if matches!(rx.try_recv(), Ok(Command::Stop) | Err(mpsc::TryRecvError::Disconnected)) {
        return undo(&mut desk, &mut journal, &mut side, t("Sessão encerrada").into());
    }

    // RTSS: the frame limit and the counter, once the screen is confirmed
    side.start_fps(&mut journal, &journal_path);

    // 4. the launcher
    let mut watch = Watch::new(&plan.launcher, &plan.card_name);
    match &plan.launcher {
        Launcher::BigPicture => {
            if !cm_platform::launch::start_big_picture() && plan.use_fse {
                return undo(&mut desk, &mut journal, &mut side, tf("Não deu para abrir {0}", &[&plan.card_name]));
            }
            if let Some(id) = plan.steam_game { std::thread::sleep(Duration::from_millis(1500)); cm_platform::launch::open(&format!("steam://rungameid/{id}")); }
        }
        Launcher::Xbox => {
            if crate::gaming_home::active() == Some(true) {
                cm_platform::launch::open("msxbox:");
            } else {
                cm_platform::launch::start_xbox();
            }
        }
        Launcher::Playnite => {
            let started = if let Some(exe) = cm_platform::launch::playnite_exe(&plan.playnite_path) {
                cm_platform::launch::start_exe(&exe, "").is_some()
            } else {
                cm_platform::launch::open("playnite://playnite/start")
            };
            if !started && plan.use_fse {
                return undo(&mut desk, &mut journal, &mut side, tf("Não deu para abrir {0}", &[&plan.card_name]));
            }
        }
        Launcher::Custom { path, args, .. } => {
            watch.custom_pid = cm_platform::launch::start_exe(path, args);
            if watch.custom_pid.is_none() { return undo(&mut desk, &mut journal, &mut side, tf("Não deu para abrir {0}", &[&path])); }
        }
    }
    side.launched();
    send(Event::Step(3));
    send(Event::Running);
    log("sessão: iniciador aberto, acompanhando");

    // 5. follow the launcher until it closes or the user ends the session
    let rect = desk.screen_rect(&plan.focus);
    let handoff_deadline = Instant::now() + Duration::from_secs(15);
    let mut focus_launcher = plan.use_fse && !desk.simulated() && plan.steam_game.is_none();
    let reason = loop {
        match rx.recv_timeout(Duration::from_millis(if watch.appeared { 2000 } else { 1500 })) {
            Ok(Command::Stop) => break t("Sessão encerrada").to_string(),
            Ok(Command::SetFps(fps)) => side.set_fps_live(fps, &mut journal, &journal_path),
            Ok(Command::SetVrr(on)) => { let state = video::vrr_from_menu(desk.as_mut(), &mut journal, &journal_path, on); send(Event::Vrr(state)); }
            Ok(Command::SetHdr(on)) => { let state = video::hdr_from_menu(desk.as_mut(), &mut journal, &journal_path, &plan.focus, on); send(Event::Hdr(state)); }
            Ok(Command::Confirmed(_)) | Err(mpsc::RecvTimeoutError::Timeout) => {}
            Err(mpsc::RecvTimeoutError::Disconnected) => break t("O app fechou").to_string(),
        }
        if focus_launcher && Instant::now() < handoff_deadline {
            if let Some(window) = watch.launcher_window() {
                use cm_platform::launch as l;
                if l::fse_foreground() == l::FseForeground::Xbox {
                    cm_platform::session_menu_win::activate(window.hwnd);
                }
                let ready = plan.launcher != Launcher::BigPicture || l::fullscreen_big_picture(&window);
                if ready {
                    focus_launcher = false;
                }
            }
        }
        if let Some(why) = watch.tick(rect) { break why; }
        side.tick(desk.as_mut(), &mut journal, &journal_path, &plan.focus, watch.appeared);
    };
    log(&format!("sessão: fim ({reason}); restaurando"));
    finish_session(desk.as_mut(), &mut side, &mut journal, &journal_path, &plan, true);
    send(Event::Ended(reason));
}

/// Ends the session: a game profile still in effect is undone, then everything is put back.
fn finish_session(desk: &mut dyn Desk, side: &mut Side, journal: &mut Journal, journal_path: &std::path::Path, plan: &Plan, launcher_started: bool) {
    // JoyChromium opened from the menu leaves full screen and stays as a window (1.7: first thing of the restore)
    crate::helper_apps::session_ended();
    side.finish(desk, journal, journal_path, &plan.focus);
    let r = Restore {
        sim: desk.simulated(), data_dir: side.data_dir(), big_picture: launcher_started && plan.launcher == Launcher::BigPicture,
        steam_to_tray: plan.close_steam_on_restore, parks: side.parks(), counter: side.counter(),
    };
    restore(desk, journal, journal_path, &r);
}

/// Puts the desk back as the journal says; the journal is deleted only when that worked.
/// Same order as 1.7: Big Picture and Steam's window, HDR, screens, audio, then icons, pointer, RTSS.
pub fn restore(desk: &mut dyn Desk, j: &Journal, journal_path: &std::path::Path, r: &Restore) {
    if r.big_picture {
        if !cm_platform::launch::close_big_picture(6000) { log("restauração: Big Picture não fechou"); }
        session_extras::steam_to_tray(r.steam_to_tray, r.sim);
    }
    restore_with_fse(desk, j, journal_path, r, |exit, home| {
        let fse_restored = !exit || r.sim || crate::gaming_home::active() == Some(false)
            || crate::gaming_home::session_state(false).map_err(|error| log(&format!("restauração: FSE não saiu: {error}"))).is_ok();
        let home_restored = home.is_none_or(|home| r.sim || (fse_restored
            && cm_platform::fse_win::restore_session_home(home.previous.as_deref())
                .map_err(|error| log(&format!("restauração: a Home anterior não voltou: {error}"))).is_ok()));
        (fse_restored, home_restored)
    });
}

fn restore_with_fse(
    desk: &mut dyn Desk, j: &Journal, journal_path: &Path, r: &Restore,
    restore_fse: impl FnOnce(bool, Option<&session::FseHomeRestore>) -> (bool, bool),
) {
    let (fse_restored, home_restored) = restore_fse(j.exit_fse, j.fse_home.as_ref());
    let (hdr_on_restored, hdr_off_restored) = video::restore_hdr(desk, j);
    let vrr_restored = video::restore_vrr(desk, j);
    let screens = if j.displays_backup.is_empty() { Ok(()) } else { desk.restore(&j.displays_backup) };
    let audio = if j.audio_before.is_empty() { Ok(()) } else { desk.set_audio(&j.audio_before) };
    session_extras::restore_extras(r, j);
    if let Some(step) = &j.tv { desk.tv_back(step, r.data_dir); }
    if let Err(error) = &screens { log(&format!("restauração: as telas não voltaram por completo ({error})")); }
    if let Err(error) = &audio { log(&format!("restauração: o áudio não voltou ({error})")); }

    let mut rest = j.remaining_after_restore(screens.is_ok(), audio.is_ok(), fse_restored, home_restored);
    if !hdr_on_restored { rest.hdr_turned_on = j.hdr_turned_on.clone(); }
    if !hdr_off_restored { rest.hdr_turned_off = j.hdr_turned_off.clone(); }
    if !vrr_restored { rest.vrr = j.vrr.clone(); }
    let complete = fse_restored && home_restored && screens.is_ok() && audio.is_ok()
        && hdr_on_restored && hdr_off_restored && vrr_restored;
    let persisted = if complete { session::delete_journal(journal_path) }
        else { session::write_journal(journal_path, &rest) };
    match persisted {
        Ok(()) => {
            // Keep the backup if the recovery record could not be updated or removed.
            if screens.is_ok() && j.displays_backup.ends_with(".ccd") {
                let _ = std::fs::remove_file(&j.displays_backup);
            }
            if complete { log("restauração: concluída"); }
            else { log("restauração: o diário fica para a próxima abertura"); }
        }
        Err(error) => log(&format!("restauração: não foi possível atualizar o diário ({error}); preservando o backup")),
    }
}

/// Following one launcher (1.7's Tick, the exit rules are in cm_core::session).
struct Watch {
    launcher: Launcher,
    appeared: bool,
    absent: u32,
    moved: u32,
    window: isize,
    missing_since: Option<Instant>,
    custom_pid: Option<u32>,
    custom_name: String,
    /// The card's name for the "closed" message (empty: the process name).
    card_name: String,
    handed_off: bool,
    gone_since: Option<Instant>,
    launched: Instant,
    timeout_logged: bool,
}

impl Watch {
    fn new(l: &Launcher, card_name: &str) -> Self {
        let custom_name = match l { Launcher::Custom { path, watch, .. } => session::watched_process(path, watch), _ => String::new() };
        Watch { launcher: l.clone(), appeared: false, absent: 0, moved: 0, window: 0, missing_since: None, custom_pid: None, custom_name, card_name: card_name.to_string(), handed_off: false, gone_since: None, launched: Instant::now(), timeout_logged: false }
    }

    /// Some(reason) when the session is over.
    fn launcher_window(&self) -> Option<cm_platform::launch::Win> {
        use cm_platform::launch as l;
        match &self.launcher {
            Launcher::BigPicture => l::big_picture_window(),
            Launcher::Playnite => l::playnite_window(),
            Launcher::Custom { .. } => l::process_windows(&self.custom_name).into_iter().max_by_key(|w| w.area()),
            Launcher::Xbox => None,
        }
    }

    fn tick(&mut self, rect: Option<(i32, i32, i32, i32)>) -> Option<String> {
        use cm_platform::launch as l;
        match &self.launcher {
            // Xbox mode has no window to follow: it ends from the session menu
            Launcher::Xbox => None,
            Launcher::BigPicture => {
                match l::big_picture_window() {
                    Some(w) => {
                        if !self.appeared { log("sessão: Big Picture apareceu"); }
                        self.appeared = true;
                        self.absent = 0;
                        if let Some(r) = rect { if self.moved < 8 && !centered_on(&w, r) { l::move_to(w.hwnd, r); self.moved += 1; } }
                        None
                    }
                    None => { if self.appeared { self.absent += 1; } session::big_picture_exit(self.appeared, self.absent).then(|| t("O Big Picture fechou").into()) }
                }
            }
            Launcher::Playnite => {
                let showing = l::playnite_window();
                if let Some(w) = &showing { if !self.appeared { log("sessão: Playnite apareceu"); } self.appeared = true; self.window = w.hwnd; }
                if !self.appeared { return None; }
                let put_away = showing.is_none() && l::is_put_away(self.window);
                if showing.is_some() || put_away { self.missing_since = None; } else if self.missing_since.is_none() { self.missing_since = Some(Instant::now()); }
                let running = l::is_running("Playnite.FullscreenApp");
                session::playnite_exit(running, showing.is_some(), self.missing_since.map(|t| t.elapsed()), put_away).then(|| t("O Playnite fechou").into())
            }
            Launcher::Custom { watch, .. } => {
                let running = l::is_running(&self.custom_name);
                if !running && !self.handed_off && watch.trim().is_empty() {
                    if let Some(pid) = self.custom_pid {
                        if let Some(target) = session::hand_off_target(&l::processes(), pid) {
                            if target != self.custom_name {
                                log(&format!("sessão: {} abriu {target} e fechou; acompanhando {target}", self.custom_name));
                                self.custom_name = target;
                                self.handed_off = true;
                                self.appeared = false;
                                return None;
                            }
                        }
                    }
                }
                if running {
                    if !self.appeared { log(&format!("sessão: processo {} apareceu", self.custom_name)); }
                    self.appeared = true;
                    self.gone_since = None;
                } else if self.appeared {
                    self.gone_since.get_or_insert_with(Instant::now);
                } else if !self.timeout_logged && self.launched.elapsed() > session::CUSTOM_APPEAR_TIMEOUT {
                    self.timeout_logged = true;
                    log(&format!("sessão: {} não apareceu; a sessão continua até ser encerrada pelo menu", self.custom_name));
                }
                session::custom_exit(self.appeared, running, self.gone_since.map(|t| t.elapsed())).then(|| tf("{0} fechou", &[if self.card_name.is_empty() { &self.custom_name } else { &self.card_name }]))
            }
        }
    }
}

fn centered_on(w: &cm_platform::launch::Win, r: (i32, i32, i32, i32)) -> bool {
    let (cx, cy) = ((w.rect.0 + w.rect.2) / 2, (w.rect.1 + w.rect.3) / 2);
    cx >= r.0 && cx < r.2 && cy >= r.1 && cy < r.3
}

/// A session that died mid-way (crash, power): undo it before anything else (1.7's recovery).
pub fn recover(desk: &mut dyn Desk, journal_path: &std::path::Path) -> bool {
    let Some(j) = session::read_journal(journal_path) else {
        if journal_path.try_exists().unwrap_or(true) { log("recuperação: diário ilegível; preservado, novas sessões bloqueadas"); }
        return false;
    };
    log(&format!("recuperação: sessão de {} ficou aberta; restaurando", j.launcher));
    // it was parked before the crash, or may have been: the pointer goes back if its old spot is on a screen
    let r = Restore { sim: desk.simulated(), data_dir: journal_path.parent().unwrap_or(std::path::Path::new(".")), big_picture: false, steam_to_tray: false, parks: u32::from(j.pointer.is_some()), counter: &cm_platform::rtss::Counter::new() };
    restore(desk, &j, journal_path, &r);
    true
}

#[cfg(test)]
mod tests {
    use super::*;
    use cm_core::tv::TvRestore;

    /// Writes down what the restore asks of it; `screens_ok`: whether the screens come back.
    struct Recorder { calls: Vec<String>, screens_ok: bool, audio_ok: bool }

    impl Desk for Recorder {
        fn backup(&mut self) -> Result<String, String> { Ok("b".into()) }
        fn restore(&mut self, _: &str) -> Result<(), String> { self.calls.push("telas".into()); if self.screens_ok { Ok(()) } else { Err("x".into()) } }
        fn focus(&mut self, _: &str) -> Result<(), String> { Ok(()) }
        fn hide(&mut self, _: &[String]) -> Result<(), String> { Ok(()) }
        fn set_mode(&mut self, _: &str, _: &DisplayMode) -> Result<(), String> { Ok(()) }
        fn hdr_on(&mut self, _: &str) -> Result<bool, String> { Ok(false) }
        fn hdr_off(&mut self, _: &str) -> Result<(), String> { Ok(()) }
        fn audio_default(&mut self) -> Option<String> { None }
        fn set_audio(&mut self, _: &str) -> Result<(), String> { self.calls.push("áudio".into()); if self.audio_ok { Ok(()) } else { Err("x".into()) } }
        fn screen_rect(&mut self, _: &str) -> Option<(i32, i32, i32, i32)> { None }
        fn tv_back(&mut self, step: &TvRestore, _: &Path) { self.calls.push(format!("tv {}", step.host)); }
        fn simulated(&self) -> bool { true }
    }

    fn journal_with_tv(dir: &Path) -> (PathBuf, Journal) {
        let _ = std::fs::create_dir_all(dir);
        let path = dir.join("session.json");
        let j = Journal { displays_backup: "backup".into(), launcher: "bigPicture".into(), tv: Some(TvRestore { provider: "androidTv".into(), host: "10.0.0.2".into(), restore_command: String::new(), standby: true }), ..Default::default() };
        session::write_journal(&path, &j).unwrap();
        (path, j)
    }

    #[test]
    fn the_tv_goes_back_after_the_screens_also_when_recovering_from_a_crash() {
        let dir = std::env::temp_dir().join(format!("cm2-engine-tv-{}", std::process::id()));
        let (path, _) = journal_with_tv(&dir);
        let mut desk = Recorder { calls: Vec::new(), screens_ok: true, audio_ok: true };
        assert!(recover(&mut desk, &path));
        assert_eq!(desk.calls, ["telas", "tv 10.0.0.2"]);
        assert_eq!(session::read_journal(&path), None, "done: the journal is gone");
        let _ = std::fs::remove_dir_all(dir);
    }

    #[test]
    fn failed_audio_and_fse_are_retried_without_repeating_successful_display_restore() {
        let dir = std::env::temp_dir().join(format!("cm2-engine-retry-{}", std::process::id()));
        let _ = std::fs::create_dir_all(&dir);
        let path = dir.join("session.json");
        let journal = Journal { displays_backup: "backup".into(), audio_before: "output".into(), exit_fse: true,
            fse_home: Some(session::FseHomeRestore { previous: Some("other".into()) }), ..Default::default() };
        session::write_journal(&path, &journal).unwrap();
        let mut desk = Recorder { calls: Vec::new(), screens_ok: true, audio_ok: false };
        let counter = cm_platform::rtss::Counter::new();
        let r = Restore { sim: true, data_dir: &dir, big_picture: false, steam_to_tray: false, parks: 0, counter: &counter };
        restore_with_fse(&mut desk, &journal, &path, &r, |exit, home| {
            assert!(exit && home.is_some()); (false, false)
        });
        let pending = session::read_journal(&path).unwrap();
        assert!(pending.displays_backup.is_empty());
        assert_eq!(pending.audio_before, "output");
        assert!(pending.exit_fse && pending.fse_home.is_some());
        assert_eq!(desk.calls, ["telas", "áudio"]);
        desk.audio_ok = true;
        desk.calls.clear();
        restore_with_fse(&mut desk, &pending, &path, &r, |exit, home| {
            assert!(exit && home.is_some()); (true, true)
        });
        assert_eq!(desk.calls, ["áudio"]);
        assert!(!path.exists());
        let _ = std::fs::remove_dir_all(dir);
    }

    #[test]
    fn screens_that_did_not_come_back_keep_the_journal_without_the_tv_step() {
        let dir = std::env::temp_dir().join(format!("cm2-engine-tv-fail-{}", std::process::id()));
        let (path, _) = journal_with_tv(&dir);
        let mut desk = Recorder { calls: Vec::new(), screens_ok: false, audio_ok: true };
        assert!(recover(&mut desk, &path));
        assert_eq!(desk.calls, ["telas", "tv 10.0.0.2"]);
        let left = session::read_journal(&path).expect("kept for the next start");
        assert_eq!(left.tv, None, "the TV was already sent back: not twice");
        let _ = std::fs::remove_dir_all(dir);
    }
}
