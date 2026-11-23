//! Sistema → Sobre → "Relatar um problema" (1.7 Diagnostics + Feedback): gathers a diagnostic text
//! (version, Windows, screens, audio, controllers, the last log lines, with private data stripped),
//! copies it to the clipboard and saves it in the data folder, then opens the GitHub issue page with the
//! environment filled in. Nothing is sent anywhere: the user pastes the report and submits the issue.
//! The format and the redaction are `cm_core::diagnostics`.

use std::path::Path;

use cm_core::{
    config::Config,
    controller_battery::{self as battery, BatteryReading},
    diagnostics::{self as diag, AppInfo, AudioInfo, ControllerInfo, DisplayInfo, Report, SystemInfo},
    update::REPOSITORY,
};
use gpui::{ClipboardItem, Context};

use crate::{home::Console, i18n::{t, tf}, input::PadInfo};

/// The report's file in the data folder.
pub const FILE: &str = "diagnostico.txt";

impl Console {
    pub(crate) fn report_problem(&mut self, cx: &mut Context<Self>) {
        if self.report_busy { return self.show_toast(t("Aguarde, ainda estou trabalhando nisso."), cx); }
        self.report_busy = true;
        self.show_toast(t("Reunindo as informações do relatório…"), cx);
        let (config, data_dir, batteries, pads) = (self.config.clone(), self.paths.data_dir.clone(), self.batteries.clone(), crate::input::connected());
        let language = crate::i18n::code();
        cx.spawn(async move |this, cx| {
            let dir = data_dir.clone();
            let (text, environment) = cx.background_executor().spawn(async move { gather(&config, &dir, &batteries, &pads, language) }).await;
            let _ = this.update(cx, |c, cx| { c.report_busy = false; c.finish_report(text, environment, &data_dir, cx); });
        }).detach();
    }

    /// The report is ready: clipboard, file, the issue page, and a message that says where it went.
    fn finish_report(&mut self, text: String, environment: String, data_dir: &Path, cx: &mut Context<Self>) {
        cx.write_to_clipboard(ClipboardItem::new_string(text.clone()));
        let path = data_dir.join(FILE);
        // Windows' editors want CRLF
        if let Err(e) = std::fs::write(&path, text.replace('\n', "\r\n")) { crate::startup_log(&format!("relatório: não foi possível salvar: {e}")); }
        let body = format!("{environment}\n\n{}\n\n\n{}\n", t("O que aconteceu:"), t("Cole aqui o relatório de diagnóstico (ele já está na área de transferência):"));
        let url = diag::issue_url(REPOSITORY, "[2.0 Alpha] ", &body);
        // tests: CM2_NO_OPEN keeps the browser closed
        if std::env::var_os("CM2_NO_OPEN").is_some() { crate::startup_log(&format!("relatório: página da issue não aberta (teste): {} caracteres", url.len())); } else { cm_platform::launch::open(&url); }
        crate::startup_log(&format!("relatório: copiado e salvo em {}", path.display()));
        self.show_toast(&tf("Relatório de diagnóstico copiado. Cole-o na página que abriu; ele também está salvo em {0}.", &[&path.display()]), cx);
    }
}

/// Reads everything on a worker thread; each section that fails reads "unavailable" in the report.
/// Returns the report and the environment lines for the issue.
fn gather(config: &Config, data_dir: &Path, batteries: &[BatteryReading], pads: &[PadInfo], language: &str) -> (String, String) {
    let version = env!("CARGO_PKG_VERSION");
    let windows = cm_platform::describe_windows();
    let report = Report {
        system: Some(SystemInfo { app_version: version.into(), channel: "alpha".into(), windows: windows.clone(), runtime: format!("Rust {}", std::env::consts::ARCH), language: language.into() }),
        displays: cm_platform::displays::list_displays().ok().map(|list| list.iter()
            // screens Windows only remembers (no source) are noise, unless they are the ones Console Mode uses
            .filter(|d| d.active || !d.gdi_name.is_empty() || d.stable_id.eq_ignore_ascii_case(&config.focus_monitor) || config.hide_monitors.iter().any(|h| h.eq_ignore_ascii_case(&d.stable_id)))
            .map(|d| DisplayInfo {
            name: format!("{} ({})", d.friendly_name, d.gdi_name.trim_start_matches(r"\\.\")),
            mode: d.current_mode.map(|m| format!("{} × {} @ {} Hz", m.width, m.height, m.refresh_hz)).unwrap_or_default(),
            hdr: match (d.active, d.hdr_supported, d.hdr_enabled) { (false, ..) => "", (_, Some(false), _) => "unsupported", (_, _, Some(true)) => "on", (_, _, Some(false)) => "off", _ => "" }.into(),
            active: d.active,
            primary: d.primary,
            focus: d.stable_id.eq_ignore_ascii_case(&config.focus_monitor),
            hidden: config.hide_monitors.iter().any(|h| h.eq_ignore_ascii_case(&d.stable_id)),
        }).collect()),
        audio: cm_platform::audio::list_outputs().ok().map(|list| list.iter().filter(|a| a.is_active).map(|a| AudioInfo {
            name: a.friendly_name.clone(),
            is_default: a.is_default,
            configured: !config.audio_device_id.is_empty() && (a.friendly_id.eq_ignore_ascii_case(&config.audio_device_id) || a.endpoint_id.eq_ignore_ascii_case(&config.audio_device_id)),
        }).collect()),
        controllers: Some(pads.iter().map(|p| ControllerInfo {
            name: format!("{}{}", p.name, match (p.vendor, p.product) { (Some(v), Some(pr)) => format!(" ({v:04X}:{pr:04X})"), _ => String::new() }),
            kind: p.kind.to_string(),
            detail: batteries.iter().find(|b| b.id == p.id || (b.bluetooth && Some(b.vendor) == p.vendor && Some(b.product) == p.product))
                .map(|b| format!("{}{}", battery::describe(b), if b.bluetooth { " (Windows Bluetooth)" } else { "" })),
        }).chain(
            // other brands' centre button: each HID collection looked at and whether it is read (1.7.1 #226/#227)
            cm_platform::hid_win::describe().into_iter().map(|line| ControllerInfo { name: line, kind: "HID".into(), detail: None })
        ).collect()),
        apps: Some(vec![
            found("Steam", cm_platform::steam::steam_dir().map(|d| d.join("steam.exe")).filter(|p| p.exists()).map(|p| p.display().to_string())),
            found("Playnite", cm_platform::launch::playnite_exe(&config.playnite_path)),
            found("RTSS", cm_platform::rtss::install_path().map(|p| p.display().to_string())),
            found("ControlFS", cm_platform::helper_apps::find_exe(cm_core::helper_apps::App::ControlFs)),
            found("JoyChromium", cm_platform::helper_apps::find_exe(cm_core::helper_apps::App::JoyChromium)),
        ]),
        launches: Some(config.fullscreen_mode.clone()),
        log: read_log(data_dir),
    };
    let environment = diag::environment(version, "alpha", &windows, std::env::consts::ARCH, language);
    (diag::format(&report), environment)
}

fn found(name: &str, path: Option<String>) -> AppInfo { AppInfo { name: name.into(), path, version: None } }

/// The app's log, last lines last. The app may be writing it at the same time.
fn read_log(data_dir: &Path) -> Option<Vec<String>> {
    let path = data_dir.join("consolemode2.log");
    if !path.exists() { return Some(Vec::new()); }
    let text = std::fs::read_to_string(&path).ok()?;
    let lines: Vec<String> = text.lines().map(str::to_string).collect();
    Some(lines[lines.len().saturating_sub(diag::LOG_LINES)..].to_vec())
}
