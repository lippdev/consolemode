//! Console Mode 2 Alpha.
//!
//! Lives next to Console Mode 1.x without touching it: no consolemode:// link, no pipe, no mutex
//! or startup entry of 1.x (docs/v2/PLANO.md).
//!
//! Opens as a normal window (minimize, maximize, close). `--fullscreen` opens fullscreen on the
//! primary screen (what a console session will use); `--quit-after <s>` is for tests.

#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod battery;
mod bind;
mod custom_apps;
mod desk;
mod engine;
mod guide_tour;
mod gaming_home;
mod hardware;
mod helper_apps;
mod intro;
mod home;
mod i18n;
mod input;
mod overlays;
mod pad_shortcuts;
mod report;
mod settings;
mod sound;
mod stage;
mod tour;
mod tv;
mod shell;
mod updates;
mod wake;
mod video;
mod whats_new;

use std::{sync::Arc, time::Duration};

use futures::StreamExt;
use gpui::{App, Bounds, WindowBounds, WindowOptions, prelude::*, px, size};

use home::{Brand, Console, Item, Launcher};

fn main() {
    let t0 = std::time::Instant::now();
    let args: Vec<String> = std::env::args().collect();
    if let Some(code) = args.get(1).and_then(|arg| cm_platform::fse_win::cli_exit_code(arg)) {
        std::process::exit(code);
    }
    // Hold the Alpha mutex until GPUI and the shell have stopped.
    let _instance = match cm_platform::shell_win::Instance::acquire() {
        Ok(Some(instance)) => instance,
        Ok(None) => {
            let mut command = cm_core::control::initial_command(&args);
            // A Windows startup invocation should not bring an existing instance forward.
            if command == cm_core::control::Command::Show && args.iter().any(|a| a == "--tray") && !args.iter().any(|a| a == "--show") {
                command = cm_core::control::Command::Status;
            }
            if let Err(e) = cm_platform::shell_win::forward(command) { startup_log(&format!("Controle: {e}")); }
            return;
        }
        Err(e) => { startup_log(&format!("Instância: {e}")); return; }
    };
    // Gaming Home enters fullscreen after opening so GPUI can restore the original bounds.
    let windowed = !args.iter().any(|a| a == "--fullscreen");
    let quit_after: Option<u64> = args.iter().position(|a| a == "--quit-after").and_then(|i| args.get(i + 1)?.parse().ok());

    // controllers start before GPUI: started after it, SDL saw no device at all
    let shared = pad_shortcuts::shared();
    let rx = input::spawn(shared.clone());
    gpui_platform::application().run(move |cx: &mut App| {
        cm_platform::helper_apps::set_logger(startup_log);
        // other brands' centre button through HID (SDL reads Xbox, PlayStation and the pads it has a driver for)
        cm_platform::hid_win::set_logger(startup_log);
        cm_platform::hid_win::start_home_reader();
        cm_ui::init(cx);
        cm_ui::motion::set_reduced(!cm_platform::animations_enabled());
        // 2.0's own config (%LOCALAPPDATA%\ConsoleMode2); the first time, a read-only copy of 1.7's
        let mut paths = cm_core::config::Paths::new(std::path::Path::new(&std::env::var("LOCALAPPDATA").unwrap_or_default()));
        // tests: a separate data folder, so the owner's config is never touched (no 1.7 import there)
        if let Some(dir) = std::env::var_os("CM2_DATA_DIR") {
            let d = std::path::PathBuf::from(dir);
            paths = cm_core::config::Paths { config: d.join("config.json"), data_dir: d, legacy_config: None };
        }
        let (config, how) = cm_core::config::load(&paths);
        let _ = std::fs::create_dir_all(&paths.data_dir);
        let _ = LOG_DIR.set(paths.data_dir.clone());
        startup_log(&format!("Console Mode 2 Alpha {} aberto", env!("CARGO_PKG_VERSION")));
        startup_log(&format!("config: {how:?} ({})", paths.config.display()));
        // the UI language: the saved choice, or (empty) Windows' display language
        i18n::set_language(&config.app_language);
        startup_log(&format!("idioma: {}", i18n::code()));
        cm_ui::set_theme(bind::theme_index(&config));
        // a session that died mid-way (crash, power) is undone before anything else
        let journal = paths.data_dir.join("session.json");
        if engine::recover(desk::pick(&paths.data_dir).as_mut(), &journal) { startup_log("recuperação: feita"); }
        let test_launcher = args.iter().position(|a| a == "--test-launcher").and_then(|i| args.get(i + 1)).cloned();
        let test_args = args.iter().position(|a| a == "--test-args").and_then(|i| args.get(i + 1)).cloned().unwrap_or_default();
        let (mut items, steam_icon) = load_items(&config.playnite_path);
        custom_apps::insert_cards(&mut items, &config);
        if let Some(exe) = test_launcher {
            // development: a custom app as the first card, to test whole sessions with a harmless program
            items.insert(0, Item::Launcher(Launcher {
                kind: cm_core::session::Launcher::Custom { path: exe.clone(), args: test_args, watch: String::new() },
                name: "App de teste".into(), custom_id: None, note: "desenvolvimento", sub: "Abre o programa de teste como iniciador.",
                icon: cm_platform::exe_icon(&exe, 256).map(Arc::new), brand: Brand::NEUTRAL, mark: None,
            }));
        }
        startup_log(&format!("items loaded at {:?}", t0.elapsed()));

        let display = cx.primary_display();
        // tests: `CM2_WINDOW_AT=x,y` opens a 1600×900 window there (off screen, captured with PrintWindow)
        let at = std::env::var("CM2_WINDOW_AT").ok().and_then(|v| { let (x, y) = v.split_once(',')?; Some((x.trim().parse::<f32>().ok()?, y.trim().parse::<f32>().ok()?)) });
        let bounds = match (&display, windowed) {
            (_, true) if at.is_some() => { let (x, y) = at.unwrap(); WindowBounds::Windowed(Bounds::new(gpui::point(px(x), px(y)), size(px(1600.), px(900.)))) }
            (Some(d), false) => WindowBounds::Fullscreen(d.bounds()),
            // 80 % of the screen, 16:9, centered
            (Some(d), true) => {
                let b = d.bounds();
                let w = (b.size.width * 0.8).min(b.size.height * 0.8 * 16. / 9.);
                WindowBounds::Windowed(Bounds::centered(Some(d.id()), size(w, w * 9. / 16.), cx))
            }
            _ => WindowBounds::Windowed(Bounds::centered(None, size(px(1280.), px(720.)), cx)),
        };
        let shell_hidden = shell::starts_hidden(&args, &config);
        let options = WindowOptions {
            show: !shell_hidden,
            window_bounds: Some(bounds),
            display_id: display.map(|d| d.id()),
            titlebar: windowed.then(|| gpui::TitlebarOptions { title: Some("Console Mode 2 Alpha".into()), appears_transparent: true, ..Default::default() }),
            focus: !shell_hidden,
            app_id: Some("ConsoleMode2Alpha".into()),
            ..Default::default()
        };
        let window = cx
            .open_window(options, |window, cx| {
                let console = cx.new(|cx| Console::new(items, steam_icon, config, paths, cx));
                console.read(cx).focus_handle().clone().focus(window, cx);
                console
            })
            .expect("open window");
        startup_log(&format!("window opened at {:?}", t0.elapsed()));
        if !shell_hidden { cx.activate(true); }

        let console = window.update(cx, |_, _, cx| cx.entity()).expect("console");
        console.update(cx, |c, _| c.window = Some(window.into()));
        // Small shell hook: tray, control dispatch and close policy live outside Home.
        shell::attach(console.clone(), window, &args, cx);
        // controller shortcuts, Task View, the pad as a mouse
        pad_shortcuts::attach(console.clone(), shared.clone(), cx);
        console.update(cx, |c, _| c.pads = Some(shared.clone()));
        // alpha updates: one quiet look at startup (silent unless something newer exists)
        console.update(cx, |c, cx| c.check_updates_quietly(cx));
        // the first start after an update: its news wait for the app to be in front
        console.update(cx, |c, _| c.prepare_whats_new());
        // the opening, over the first screen (only when the window opens to be used)
        console.update(cx, |c, cx| c.begin_intro(!shell_hidden, cx));
        // first run of 2.0: the tour (only with the window shown)
        console.update(cx, |c, cx| if !c.config.tour_done && !std::env::args().any(|a| a == "--tray") { c.start_tour(cx) });
        let mut rx = rx;
        let c = console.clone();
        cx.spawn(async move |cx| {
            while let Some(ev) = rx.next().await {
                // in a session the app is out of the way: input only counts once ☰ is held
                let (in_session, guide_open, w) = c.read_with(cx, |con, _| (con.running.is_some(), matches!(con.view, home::View::Guide { .. }), con.window));
                match ev {
                    input::Input::MenuHeld => {
                        c.update(cx, |con, cx| {
                            if matches!(con.view, home::View::Guide { .. }) {
                                con.close_overlay(cx);
                                con.sounds.play(sound::Sound::Back);
                            } else {
                                con.open_guide(cx);
                            }
                        });
                    }
                    input::Input::Press(_) if in_session && !c.read_with(cx, |con, _| con.app_in_front) => {}
                    // hidden in the tray, or the pad is busy as a mouse or driving Task View
                    input::Input::Press(_) if !guide_open && pad_shortcuts::swallow_press(&shared, in_session) => {}
                    input::Input::Press(p) => c.update(cx, |con, cx| con.press(p, cx)),
                    ev @ (input::Input::Shortcut(_) | input::Input::Connected) => pad_shortcuts::on_event(&c, w, &shared, &ev, cx),
                    input::Input::Controller(name) => c.update(cx, |con, cx| { con.controller = name; cx.notify(); }),
                    input::Input::Battery(list) => c.update(cx, |con, cx| con.set_batteries(list, cx)),
                    input::Input::CaptureLive(mask) => c.update(cx, |con, cx| con.capture_live(mask, cx)),
                    input::Input::Captured(mask) => c.update(cx, |con, cx| con.capture_done(mask, cx)),
                }
            }
        }).detach();
        cx.spawn(async move |cx| loop {
            cx.background_executor().timer(Duration::from_secs(5)).await;
            let visible = window.update(cx, |_, w, _| cm_platform::pad_win::window_visible(w)).unwrap_or(true);
            console.update(cx, |con, cx| { con.tick(cx); con.promote_whats_new(visible, cx); con.sounds.close_when_idle(); });
        }).detach();
        if let Some(s) = quit_after {
            cx.spawn(async move |cx| { cx.background_executor().timer(Duration::from_secs(s)).await; cx.update(|cx| cx.quit()); }).detach();
        }
    });
}

/// Launchers present on this PC, the "add app" card, then the recent Steam games.
fn load_items(playnite_path: &str) -> (Vec<Item>, Option<Arc<gpui::RenderImage>>) {
    let steam_dir = cm_platform::steam::steam_dir();
    let games = steam_dir.as_deref().map(|d| cm_platform::steam::recent_games(d, 12)).unwrap_or_default();
    let steam_icon = steam_dir.as_ref().and_then(|d| cm_platform::exe_icon(&d.join("steam.exe").to_string_lossy(), 256)).map(Arc::new);

    let mut items = vec![Item::Launcher(Launcher {
        kind: cm_core::session::Launcher::BigPicture,
        name: "Steam Big Picture".into(), custom_id: None, note: i18n::n("padrão"), sub: i18n::n("Abre a Steam em tela cheia. Ao fechar, suas telas voltam sozinhas."),
        icon: steam_icon.clone(), brand: Brand::STEAM, mark: steam_icon.clone(),
    })];
    if let Some(playnite) = cm_platform::launch::playnite_exe(playnite_path) {
        let playnite_icon = cm_platform::exe_icon(&playnite, 256).map(Arc::new);
        items.push(Item::Launcher(Launcher { kind: cm_core::session::Launcher::Playnite, name: "Playnite".into(), custom_id: None, note: i18n::n("tela cheia"), sub: i18n::n("Abre o Playnite em tela cheia. Ao fechar, suas telas voltam sozinhas."), icon: playnite_icon.clone(), brand: Brand::PLAYNITE, mark: playnite_icon }));
    }
    if let Some(logo) = cm_platform::xbox_logo() {
        items.push(Item::Launcher(Launcher { kind: cm_core::session::Launcher::Xbox, name: i18n::n("Modo Xbox").into(), custom_id: None, note: i18n::n("experimental"), sub: i18n::n("A experiência de tela cheia do Xbox no Windows."), icon: Some(Arc::new(logo)), brand: Brand::XBOX, mark: cm_platform::xbox_logo_mono().map(Arc::new) }));
    }
    items.push(Item::Add);
    for g in games {
        let dims = g.logo.as_ref().and_then(|p| image_size(p));
        items.push(Item::Game(g, dims));
    }
    (items, steam_icon)
}

fn image_size(p: &std::path::Path) -> Option<(u32, u32)> {
    // PNG header: width/height are big-endian at bytes 16..24
    let b = std::fs::read(p).ok()?;
    (b.len() > 24 && &b[1..4] == b"PNG").then(|| (u32::from_be_bytes(b[16..20].try_into().unwrap()), u32::from_be_bytes(b[20..24].try_into().unwrap())))
}

/// Startup timings go to %TEMP%ConsoleMode2-startup.log (no console in release builds).
/// The app's log: `consolemode2.log` in its data folder once known (temp before that), with a
/// time stamp; kept under ~512 KB by dropping the older half. `ConsoleMode2-startup.log` in temp
/// stays as a mirror for development scripts.
fn startup_log(line: &str) {
    use std::io::Write;
    let stamp = cm_platform::local_time();
    let entry = format!("{:02}:{:02}  {line}", stamp.0, stamp.1);
    let mut targets = vec![std::env::temp_dir().join("ConsoleMode2-startup.log")];
    if let Some(dir) = LOG_DIR.get() { targets.push(dir.join("consolemode2.log")); }
    for (i, path) in targets.iter().enumerate() {
        if i > 0 { if let Ok(m) = std::fs::metadata(path) { if m.len() > 512 * 1024 { if let Ok(t) = std::fs::read_to_string(path) { let _ = std::fs::write(path, &t[t.len() / 2..]); } } } }
        if let Ok(mut f) = std::fs::OpenOptions::new().create(true).append(true).open(path) { let _ = writeln!(f, "{}", if i == 0 { line } else { &entry }); }
    }
}

pub static LOG_DIR: std::sync::OnceLock<std::path::PathBuf> = std::sync::OnceLock::new();
