//! UI-thread dispatch for the Alpha shell. Worker threads never touch GPUI state.
use crate::home::Console;
use cm_core::control::{self, Command};
use cm_platform::shell_win::{self as native, Request};
use gpui::{App, AppContext, Entity, WindowHandle};
use std::{
    sync::{atomic::Ordering, mpsc},
    time::Duration,
};

pub fn settings_change(title: &str, config: &cm_core::config::Config) -> Result<(), String> {
    match title {
        "Iniciar com o Windows" => native::set_startup(config.start_with_windows),
        "Registrar links consolemode2alpha" => native::set_protocol(config.protocol_registered),
        _ => Ok(()),
    }
}
fn snapshot(c: &Console, ok: bool, error: Option<&str>) -> String {
    let launcher = c
        .running
        .as_ref()
        .map(|r| r.shell_launcher.mode())
        .unwrap_or("");
    let focus = c
        .running
        .as_ref()
        .map(|r| r.shell_focus.as_str())
        .unwrap_or(&c.config.focus_monitor);
    control::reply(ok, c.running.is_some(), launcher, focus, error)
}
pub fn starts_hidden(args: &[String], config: &cm_core::config::Config) -> bool {
    (args.iter().any(|a| a == "--tray") || config.start_in_tray)
        && !args
            .iter()
            .any(|a| a == "--show" || control::parse_uri(a).is_some())
        && control::initial_command(args) == Command::Show
}
fn enter_fse_on_open(command: Option<Command>, enabled: bool, xbox_running: bool) -> bool {
    command == Some(Command::Show) && enabled && !xbox_running
}
pub fn attach(
    console: Entity<Console>,
    window: WindowHandle<Console>,
    args: &[String],
    cx: &mut App,
) {
    let (tx, tray_rx) = mpsc::channel();
    let tray = match native::Tray::start(tx.clone()) {
        Ok(t) => Some(t),
        Err(e) => {
            crate::startup_log(&format!("Bandeja: {e}"));
            None
        }
    };
    let tray_ok = tray.is_some();
    let (request_tx, requests) = mpsc::channel::<Request>();
    let pipe = match native::Pipe::start(request_tx) {
        Ok(p) => Some(p),
        Err(e) => {
            crate::startup_log(&format!("Controle: {e}"));
            None
        }
    };
    let close_console = console.clone();
    window
        .update(cx, |_, w, cx| {
            native::set_window_icon(w);
            w.on_window_should_close(cx, move |w, cx| {
                let c = close_console.read(cx);
                if tray_ok && c.config.start_in_tray {
                    close_console.update(cx, |c, _| c.app_in_front = c.running.is_none());
                    native::hide_window(w);
                } else {
                    let _ = tx.send(Command::Exit);
                }
                false
            });
        })
        .ok();
    let command = control::initial_command(args);
    let hidden = tray_ok && starts_hidden(args, &console.read(cx).config);
    if hidden {
        window.update(cx, |_, w, _| native::hide_window(w)).ok();
    } else {
        window.update(cx, |_, w, _| native::show_window(w)).ok();
    }
    cx.spawn(async move |cx| {
        let (_pipe, tray) = (pipe, tray);
        let mut initial = (command != Command::Show || !hidden).then_some(command);
        let mut exiting = false;
        let mut menu_fullscreen = None;
        let mut fse_seen_active = false;
        let mut home_due: Option<std::time::Instant> = None;
        let mut restore_home = false;
        let mut entry_generation = 0;
        let mut entry_deadline: Option<std::time::Instant> = None;
        let mut return_deadline: Option<std::time::Instant> = None;
        loop {
            // quick while something is due (the menu going full screen, quitting), slower at rest:
            // a tray click or a command through the pipe still answers within a quarter of a second
            let pace = if menu_fullscreen.is_some() || exiting || initial.is_some() { 30 } else { 250 };
            cx.background_executor()
                .timer(Duration::from_millis(pace))
                .await;
            if crate::gaming_home::entry_pending() {
                let generation = crate::gaming_home::entry_generation();
                if generation != entry_generation {
                    entry_generation = generation;
                    entry_deadline = Some(std::time::Instant::now() + Duration::from_secs(60));
                }
                if entry_deadline.is_some_and(|due| std::time::Instant::now() >= due)
                    && crate::gaming_home::active() != Some(true)
                {
                    crate::gaming_home::complete_entry();
                    entry_deadline = None;
                    crate::startup_log("FSE: confirmação ainda não recebida; é possível tentar novamente");
                }
            }
            if crate::gaming_home::return_pending() {
                let deadline = *return_deadline.get_or_insert_with(|| std::time::Instant::now() + Duration::from_secs(60));
                if std::time::Instant::now() >= deadline && crate::gaming_home::active() != Some(false) {
                    crate::gaming_home::complete_return();
                    return_deadline = None;
                    crate::startup_log("FSE: saída não confirmada; é possível tentar novamente");
                }
            } else { return_deadline = None; }
            if crate::gaming_home::entry_pending() || crate::gaming_home::return_pending() || crate::stage::is_gaming_home() || fse_seen_active || restore_home {
                match crate::gaming_home::active() {
                    Some(true) => {
                        fse_seen_active = true;
                        restore_home = false;
                        if crate::gaming_home::entry_pending() && home_due.is_none() {
                            // Let Windows finish bringing up its selected Home first.
                            home_due = Some(std::time::Instant::now() + Duration::from_millis(1500));
                        }
                        if home_due.is_some_and(|due| std::time::Instant::now() >= due)
                            && !console.read_with(cx, |c, _| c.running.is_some())
                        {
                            crate::gaming_home::complete_entry();
                            home_due = None;
                            let foreground = cm_platform::launch::fse_foreground();
                            if foreground == cm_platform::launch::FseForeground::Xbox || cm_platform::pad_win::app_is_foreground() {
                                console.update(cx, |c, _| c.app_in_front = true);
                                let _ = cx.update_window(window.into(), |_, w, _| {
                                    native::show_window(w);
                                    crate::stage::gaming_home(w);
                                    crate::stage::back_home_front(w);
                                });
                                crate::startup_log("FSE: tentativa direta de trazer Home para frente");
                            }
                            if foreground != cm_platform::launch::FseForeground::Other {
                                console.update(cx, |c, cx| c.rescue_home_foreground(cx));
                            }
                        }
                    }
                    Some(false) if fse_seen_active || crate::gaming_home::return_pending() => {
                        fse_seen_active = false;
                        home_due = None;
                        crate::gaming_home::complete_entry();
                        // Do not disturb the session menu or take focus from another app.
                        restore_home = true;
                    }
                    _ => {}
                }
                if restore_home && !console.read_with(cx, |c, _| c.running.is_some()) {
                    let return_to_pc = crate::gaming_home::return_pending();
                    crate::gaming_home::complete_return();
                    if return_to_pc { console.update(cx, |c, _| c.app_in_front = false); }
                    let _ = cx.update_window(window.into(), |_, w, _| {
                        if return_to_pc { native::hide_window(w); }
                        crate::stage::leave_gaming_home(w);
                        if return_to_pc { native::hide_window(w); }
                    });
                    restore_home = false;
                }
            }
            if menu_fullscreen.is_some_and(|due| std::time::Instant::now() >= due) {
                if console.read_with(cx, |c, _| {
                    matches!(c.view, crate::home::View::Guide { .. })
                }) {
                    let _ = cx.update_window(window.into(), |_, w, _| {
                        crate::stage::over_game_fullscreen(w)
                    });
                }
                menu_fullscreen = None;
            }
            let mut pending: Vec<(Option<Command>, Option<mpsc::Sender<String>>)> = requests
                .try_iter()
                .map(|r| (r.command, Some(r.response)))
                .collect();
            pending.extend(tray_rx.try_iter().map(|c| (Some(c), None)));
            if let Some(c) = initial.take() {
                pending.push((Some(c), None));
            }
            for (command, response) in pending {
                // A normal opening honors the saved preference, including a second process
                // forwarded to the existing instance. Tray startup and explicit commands do not.
                let enter_fse = console.read_with(cx, |c, _| {
                    enter_fse_on_open(command, c.config.gaming_home_on_open, c.running.as_ref().is_some_and(|r| r.shell_launcher == cm_core::session::Launcher::Xbox))
                });
                if enter_fse {
                    let target = console.clone();
                    cx.spawn(async move |cx| {
                        let result = cx.background_executor().spawn(async move { crate::gaming_home::set_active(true) }).await;
                        if let Err(error) = result {
                            crate::startup_log(&format!("Abrir experiência de tela inteira: {error}"));
                            let _ = target.update(cx, |c, cx| c.show_toast(crate::i18n::t(error), cx));
                        }
                    }).detach();
                }
                let launcher_owns_home = command == Some(Command::GamingHome)
                    && console.read_with(cx, |c, _| c.running.as_ref().is_some_and(|run| run.use_fse));
                let needs_front = !launcher_owns_home && matches!(
                    command,
                    Some(Command::Show | Command::GamingHome | Command::Start | Command::Menu)
                );
                console.update(cx, |c, cx| {
                    match command {
                        Some(Command::Start) if !exiting => c.start_from_shell(cx),
                        Some(Command::GamingHome) if !exiting && c.running.is_none() => c.start_from_gaming_home(cx),
                        Some(Command::Stop | Command::Exit) => {
                            if command == Some(Command::Exit) {
                                exiting = true;
                            }
                            if let Some(r) = &c.running {
                                let _ = r.commands.send(crate::engine::Command::Stop);
                            }
                        }
                        Some(Command::Menu) => {
                            c.open_guide(cx);
                            menu_fullscreen =
                                Some(std::time::Instant::now() + Duration::from_millis(150));
                        }
                        _ => {}
                    }
                    if needs_front {
                        c.app_in_front = true;
                    }
                    // Mutation replies acknowledge dispatch; status reports the current engine state.
                    if let Some(tx) = response {
                        let _ = tx.send(snapshot(
                            c,
                            command.is_some(),
                            command.is_none().then_some("Comando desconhecido"),
                        ));
                    }
                });
                if needs_front {
                    let _ = cx.update_window(window.into(), |_, w, _| {
                        native::show_window(w);
                        if command == Some(Command::GamingHome) { crate::stage::gaming_home(w); }
                        if menu_fullscreen.is_some() {
                            crate::stage::over_game(w);
                        }
                    });
                }
            }
            let active = console.read_with(cx, |c, _| c.running.is_some());
            if let Some(t) = &tray {
                t.active.store(active, Ordering::Relaxed);
            }
            if exiting && !active {
                cx.update(|cx| cx.quit());
                break;
            }
        }
    })
    .detach();
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn preference_applies_only_to_normal_open_and_not_xbox_session() {
        assert!(enter_fse_on_open(Some(Command::Show), true, false));
        assert!(!enter_fse_on_open(Some(Command::Show), false, false));
        assert!(!enter_fse_on_open(Some(Command::Show), true, true));
        assert!(!enter_fse_on_open(Some(Command::Start), true, false));
        assert!(!enter_fse_on_open(Some(Command::GamingHome), true, false));
    }
    #[test]
    fn tray_start_respects_explicit_links() {
        let mut config = cm_core::config::Config::default();
        let args = |s: &str| vec!["ConsoleMode2.exe".to_string(), s.to_string()];
        assert!(starts_hidden(&args("--tray"), &config));
        assert!(!starts_hidden(&args(""), &config));
        config.start_in_tray = true;
        assert!(starts_hidden(&args(""), &config));
        for action in [
            "--show",
            "--start",
            "consolemode2alpha://show",
            "consolemode2alpha://menu",
        ] {
            assert!(!starts_hidden(&args(action), &config));
        }
    }
}
