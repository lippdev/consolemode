//! ControlFS and JoyChromium on Windows (1.7 ControlFsService, ControlFsInstaller, JoyChromiumService):
//! finding them, opening them over the game, putting their window on the session screen, handing
//! JoyChromium `--window` when going back to the PC, and installing one when the user asks from the
//! session menu. The rules are `cm_core::helper_apps`. Neither app is ever closed or killed from here.

use std::{
    os::windows::process::CommandExt,
    path::{Path, PathBuf},
    process::Command,
    time::{Duration, Instant},
};

use cm_core::helper_apps::{self as rules, App};
use windows::Win32::{
    Foundation::HWND,
    UI::{
        Input::KeyboardAndMouse::{KEYEVENTF_KEYUP, VK_F11, keybd_event},
        WindowsAndMessaging::{GetForegroundWindow, GetWindowRect, SW_MAXIMIZE, SW_RESTORE, SWP_NOZORDER, SWP_SHOWWINDOW, SetWindowPos, ShowWindow},
    },
};
use winreg::{RegKey, enums::{HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE, KEY_READ, KEY_WOW64_32KEY, KEY_WOW64_64KEY}};

use crate::launch;

const UNINSTALL: &str = r"Software\Microsoft\Windows\CurrentVersion\Uninstall";
const MAX_LIST: usize = 4 << 20;
const MAX_SETUP: u64 = 600 << 20;
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

/// The installed exe, or None.
pub fn find_exe(app: App) -> Option<String> {
    let exists = |p: &str| Path::new(p).is_file();
    match app {
        App::ControlFs => {
            let folders = rules::controlfs_folders(
                controlfs_install_location().as_deref(),
                std::env::var("LOCALAPPDATA").ok().as_deref(),
                std::env::var("ProgramFiles").ok().as_deref(),
            );
            rules::controlfs_exe(&folders, exists)
        }
        App::JoyChromium => {
            let default = std::env::var("LOCALAPPDATA").ok().filter(|l| !l.trim().is_empty()).map(|l| rules::joychromium_default_path(&l));
            let icon = RegKey::predef(HKEY_CURRENT_USER).open_subkey(rules::JOYCHROMIUM_UNINSTALL_KEY).ok().and_then(|k| k.get_value::<String, _>("DisplayIcon").ok());
            rules::joychromium_exe(default.as_deref(), icon.as_deref(), exists)
        }
    }
}

/// ControlFS's InstallLocation from the uninstall entry Windows lists it under (per user first).
fn controlfs_install_location() -> Option<String> {
    let roots = [
        RegKey::predef(HKEY_CURRENT_USER).open_subkey(UNINSTALL),
        RegKey::predef(HKEY_LOCAL_MACHINE).open_subkey_with_flags(UNINSTALL, KEY_READ | KEY_WOW64_64KEY),
        RegKey::predef(HKEY_LOCAL_MACHINE).open_subkey_with_flags(UNINSTALL, KEY_READ | KEY_WOW64_32KEY),
    ];
    for root in roots.into_iter().flatten() {
        for name in root.enum_keys().flatten() {
            let Ok(key) = root.open_subkey(&name) else { continue };
            let display: String = key.get_value("DisplayName").unwrap_or_default();
            if !display.eq_ignore_ascii_case("ControlFS") { continue; }
            let location: String = key.get_value("InstallLocation").unwrap_or_default();
            if !location.trim().is_empty() { return Some(location); }
        }
    }
    None
}

/// Its largest visible window (Windows may count a tooltip as the "main" one), or 0.
fn main_window(app: App) -> isize {
    launch::process_windows(app.process()).into_iter().find(|w| launch::still_visible(w.hwnd) && w.area() > 40_000).map(|w| w.hwnd).unwrap_or(0)
}

fn rect_of(hwnd: isize) -> Option<(i32, i32, i32, i32)> {
    let mut r = windows::Win32::Foundation::RECT::default();
    unsafe { GetWindowRect(HWND(hwnd as *mut _), &mut r).ok()?; }
    Some((r.left, r.top, r.right - r.left, r.bottom - r.top))
}

/// Screen as (left, top, right, bottom) → (x, y, width, height).
fn xywh(screen: (i32, i32, i32, i32)) -> (i32, i32, i32, i32) { (screen.0, screen.1, screen.2 - screen.0, screen.3 - screen.1) }

fn covers(hwnd: isize, screen: (i32, i32, i32, i32)) -> bool { rect_of(hwnd).is_some_and(|w| rules::is_full_screen(w, xywh(screen))) }

fn place(hwnd: isize, x: i32, y: i32, w: i32, h: i32) {
    unsafe { let _ = SetWindowPos(HWND(hwnd as *mut _), None, x, y, w, h, SWP_NOZORDER | SWP_SHOWWINDOW); }
}

fn foreground() -> isize { unsafe { GetForegroundWindow() }.0 as isize }

/// Opens it from the session menu: ControlFS with `--start` (or its open window brought forward,
/// since starting it again can leave a second copy), JoyChromium with `--tv` (a second run hands it
/// to the open one). False when it isn't installed or didn't start.
pub fn open(app: App) -> bool {
    let Some(exe) = find_exe(app) else { return false };
    if app == App::ControlFs {
        let open = main_window(app);
        if open != 0 {
            unsafe { let _ = ShowWindow(HWND(open as *mut _), SW_RESTORE); }
            crate::session_menu_win::activate(open);
            log(app, "já estava aberto, trazido para a frente");
            return true;
        }
    }
    match launch::start_exe(&exe, &app.open_args().join(" ")) {
        Some(_) => { log(app, "aberto pelo menu da sessão"); true }
        None => { log(app, "não foi possível abrir"); false }
    }
}

/// Once its window is up, puts it over the session screen (`screen`: left, top, right, bottom).
/// ControlFS has no start-up flag for full screen, so it gets F11 (its own full-screen key) when it
/// isn't covering the screen, and is maximized if that didn't take. JoyChromium opens full screen
/// wherever Windows puts it, so it is moved over the session screen. Blocking: run off the UI thread.
pub fn ensure_on_screen(app: App, screen: (i32, i32, i32, i32)) {
    let (x, y, w, h) = xywh(screen);
    if w <= 0 || h <= 0 { return; }
    let wait = if app == App::ControlFs { Duration::from_secs(12) } else { Duration::from_secs(15) };
    let deadline = Instant::now() + wait;
    let mut hwnd = 0;
    while Instant::now() < deadline {
        hwnd = main_window(app);
        if hwnd != 0 && (app == App::ControlFs || rect_of(hwnd).is_some_and(|r| r.2 as i64 * r.3 as i64 > launch::BIG_WINDOW_AREA)) { break; }
        hwnd = 0;
        std::thread::sleep(Duration::from_millis(250));
    }
    if hwnd == 0 { return log(app, "a janela não apareceu para levar à tela da sessão"); }
    std::thread::sleep(Duration::from_millis(500));
    if covers(hwnd, screen) { return; }
    match app {
        App::ControlFs => {
            // first onto the session screen, so F11 makes it cover that one
            unsafe { let _ = ShowWindow(HWND(hwnd as *mut _), SW_RESTORE); }
            place(hwnd, x + 80, y + 80, (w - 160).max(400), (h - 160).max(300));
            std::thread::sleep(Duration::from_millis(300));
            if foreground() != hwnd { crate::session_menu_win::activate(hwnd); }
            std::thread::sleep(Duration::from_millis(200));
            if foreground() == hwnd {
                unsafe {
                    keybd_event(VK_F11.0 as u8, 0, Default::default(), 0);
                    keybd_event(VK_F11.0 as u8, 0, KEYEVENTF_KEYUP, 0);
                }
                std::thread::sleep(Duration::from_millis(700));
            } else {
                log(app, "F11 não enviado porque outra janela está em primeiro plano");
            }
            if covers(hwnd, screen) { return log(app, "em tela cheia"); }
            unsafe { let _ = ShowWindow(HWND(hwnd as *mut _), SW_MAXIMIZE); }
            log(app, "F11 não surtiu efeito, janela maximizada");
        }
        App::JoyChromium => {
            place(hwnd, x, y, w, h);
            if foreground() != hwnd { crate::session_menu_win::activate(hwnd); }
            log(app, "levado para a tela da sessão");
        }
    }
}

pub fn is_running(app: App) -> bool { launch::is_running(app.process()) }

/// Going back to the PC: the open JoyChromium leaves full screen (a second run hands `--window` to it
/// and exits at once, so no other browser opens).
pub fn return_joychromium_to_window() {
    let Some(exe) = find_exe(App::JoyChromium) else { return };
    if launch::start_exe(&exe, rules::JOYCHROMIUM_WINDOW_ARG).is_some() { log(App::JoyChromium, "pedido para sair da tela cheia ao voltar ao PC"); }
}

/// Not installed and not installable from the menu: its release page in the browser.
pub fn open_release_page(app: App) {
    if !launch::open(&app.releases_page()) { log(app, "não foi possível abrir a página de download"); }
}

/// A setup of this app still at work (from an earlier attempt).
fn setup_running(app: App) -> bool { launch::processes().iter().any(|(_, _, exe)| app.is_setup_process(exe)) }

fn arm64() -> bool {
    ["PROCESSOR_ARCHITEW6432", "PROCESSOR_ARCHITECTURE"].iter().any(|v| std::env::var(v).is_ok_and(|a| a.eq_ignore_ascii_case("ARM64")))
}

/// Downloads the setup of its newest release, checks it against GitHub's SHA-256 and runs it silently
/// (per user, no admin prompt), giving it [`rules::SETUP_TIMEOUT`]. Only on the user's press in the menu.
/// `progress` gets the download from 0 to 1, then None once the setup runs. Returns the installed exe,
/// or the reason (Portuguese, for the log). Blocking: run off the UI thread.
pub fn install(app: App, user_agent: &str, progress: &mut dyn FnMut(Option<f32>)) -> Result<String, String> {
    if setup_running(app) { return Err(format!("outro instalador do {} ainda está rodando", app.name())); }
    let list = crate::http::get(&app.releases_api(), user_agent, "application/vnd.github+json", MAX_LIST)?;
    let setup = rules::pick_setup(app, &String::from_utf8_lossy(&list), arm64()).ok_or("nenhuma release com instalador verificável")?;
    let expected = rules::digest_hex(&setup.digest).ok_or("resumo SHA-256 inválido")?;
    if !app.is_trusted_url(&setup.url) { return Err("endereço de download não confiável".into()); }

    let dir = std::env::temp_dir().join("ConsoleMode2Apps");
    std::fs::create_dir_all(&dir).map_err(|e| format!("pasta temporária: {e}"))?;
    let part = dir.join(format!("{}.part", setup.name));
    let file: PathBuf = dir.join(&setup.name);
    let _ = std::fs::remove_file(&file);
    let result = (|| {
        crate::http::download(&setup.url, user_agent, &part, MAX_SETUP, &mut |done, total| {
            if let Some(t) = total.filter(|t| *t > 0) { progress(Some((done as f64 / t as f64) as f32)); }
        })?;
        let actual = cm_core::update::sha256_file(&part).map_err(|e| format!("ler o download: {e}"))?;
        if actual != expected { return Err("o SHA-256 do instalador não confere".to_string()); }
        std::fs::rename(&part, &file).map_err(|e| format!("guardar o instalador: {e}"))?;
        log(app, &format!("instalador {} baixado e verificado", setup.version));
        progress(None);
        run_setup(app, &file)
    })();
    let _ = std::fs::remove_file(&part);
    let _ = std::fs::remove_file(&file);
    result?;
    find_exe(app).ok_or_else(|| format!("instalado, mas o {}.exe não foi encontrado", app.name()))
}

/// Runs the verified setup and waits for it; a setup that doesn't finish in time is stopped with its
/// child process (Inno runs two), so a later attempt never starts a second one beside it.
fn run_setup(app: App, file: &Path) -> Result<(), String> {
    let mut child = Command::new(file).args(app.silent_args()).spawn().map_err(|e| format!("o instalador não iniciou: {e}"))?;
    let deadline = Instant::now() + rules::SETUP_TIMEOUT;
    loop {
        match child.try_wait() {
            Ok(Some(status)) if status.success() => return Ok(()),
            Ok(Some(status)) => return Err(format!("o instalador saiu com o código {}", status.code().unwrap_or(-1))),
            Ok(None) if Instant::now() >= deadline => {
                let _ = Command::new("taskkill").args(["/T", "/F", "/PID", &child.id().to_string()]).creation_flags(CREATE_NO_WINDOW).status();
                let _ = child.kill();
                let _ = child.wait();
                return Err(format!("o instalador não terminou em {} min e foi encerrado", rules::SETUP_TIMEOUT.as_secs() / 60));
            }
            Ok(None) => std::thread::sleep(Duration::from_millis(250)),
            Err(e) => return Err(format!("esperar o instalador: {e}")),
        }
    }
}

/// The app's log (cm-app sets it at start-up; nothing is written before that).
static LOGGER: std::sync::OnceLock<fn(&str)> = std::sync::OnceLock::new();

pub fn set_logger(f: fn(&str)) { let _ = LOGGER.set(f); }

fn log(app: App, what: &str) { if let Some(f) = LOGGER.get() { f(&format!("{}: {what}", app.name())); } }
