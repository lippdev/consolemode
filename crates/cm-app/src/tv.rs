//! Sistema → TV (1.7.1/1.8 MainViewModel.Tv, TvControlService): turn a Google TV / Android TV on
//! and switch it to the PC's input when a session starts, and the optional step when going back to
//! the PC. The rows are in settings.rs (bound in bind.rs); the text rows open the on-screen keyboard
//! of custom_apps.rs; "Testar a TV" runs in the background. The session calls [`session_on`] and
//! [`session_back`] through its desk (engine.rs), which a simulated desk only logs.

use std::{collections::HashSet, path::Path, sync::Mutex, time::Instant};

use cm_core::{config::Config, tv::{self as core, TvControl, TvError, TvRestore}};
use gpui::Context;

use crate::{home::Console, i18n::{n, t, tf}, settings::Value, startup_log as log};

pub const PROVIDER_ROW: &str = n("Ligar a TV e trocar a entrada");
pub const HOST_ROW: &str = n("Endereço da TV");
pub const MAC_ROW: &str = n("Endereço MAC (opcional)");
pub const HDMI_ROW: &str = n("Entrada do PC");
pub const INPUT_ROW: &str = n("Comando da entrada (avançado)");
pub const TEST_ROW: &str = n("Testar a TV");
pub const STANDBY_ROW: &str = n("Colocar a TV em espera ao restaurar");
pub const RESTORE_ROW: &str = n("Comando ao voltar ao PC (avançado)");

pub const PROVIDERS: [(&str, &str); 2] = [(n("Não controlar"), core::NONE), (n("Google TV / Android TV (rede)"), core::ANDROID_TV)];
pub const HDMI: [&str; 4] = ["HDMI 1", "HDMI 2", "HDMI 3", "HDMI 4"];

/// The rows typed on the on-screen keyboard.
pub const TEXT_ROWS: [&str; 4] = [HOST_ROW, MAC_ROW, INPUT_ROW, RESTORE_ROW];

pub fn provider_label(provider: &str) -> &'static str { PROVIDERS.iter().find(|p| p.1 == provider).unwrap_or(&PROVIDERS[0]).0 }
pub fn provider_value(label: &str) -> &'static str { PROVIDERS.iter().find(|p| p.0 == label).unwrap_or(&PROVIDERS[0]).1 }
pub fn hdmi_label(input: i32) -> &'static str { HDMI[(input.clamp(1, 4) - 1) as usize] }
pub fn hdmi_value(label: &str) -> i32 { HDMI.iter().position(|h| *h == label).map(|i| i as i32 + 1).unwrap_or(1) }

/// A text row's value in the config.
pub fn text_of<'a>(title: &str, tv: &'a TvControl) -> &'a str {
    match title { HOST_ROW => &tv.host, MAC_ROW => &tv.mac_address, INPUT_ROW => &tv.input_command, RESTORE_ROW => &tv.restore_command, _ => "" }
}

/// Writes a text row typed on the keyboard. True when it is one of the TV's.
pub fn store_text(title: &str, text: &str, c: &mut Config) -> bool {
    let text = text.trim().to_string();
    match title {
        HOST_ROW => c.tv.host = text,
        MAC_ROW => c.tv.mac_address = text,
        INPUT_ROW => c.tv.input_command = text,
        RESTORE_ROW => c.tv.restore_command = text,
        _ => return false,
    }
    true
}

/// What a text row shows beside it: nothing when empty, long commands cut short.
pub fn shown_value(text: &str) -> Option<&'static str> {
    let text = text.trim();
    if text.is_empty() { return None; }
    let short: String = if text.chars().count() > 26 { text.chars().take(25).chain(['…']).collect() } else { text.to_string() };
    Some(intern(&short))
}

/// The rows hold `&'static str`; a typed value is kept once, however many times it is shown.
fn intern(s: &str) -> &'static str {
    static SEEN: Mutex<Option<HashSet<&'static str>>> = Mutex::new(None);
    let mut seen = SEEN.lock().unwrap_or_else(|e| e.into_inner());
    let set = seen.get_or_insert_with(HashSet::new);
    if let Some(v) = set.get(s) { return v; }
    let v: &'static str = Box::leak(s.to_owned().into_boxed_str());
    set.insert(v);
    v
}

/// The message for the user (1.7.1 strings).
pub fn message(e: &TvError) -> String {
    match e {
        TvError::NotConfigured => t("Escolha como controlar a TV.").into(),
        TvError::HostMissing => t("Informe o endereço IP da TV.").into(),
        TvError::Unreachable(host) => tf("A TV em {0} não respondeu. Confira o IP, se a TV está na mesma rede e se a depuração está ativada.", &[host]),
        TvError::NotAllowed => t("A TV não autorizou este PC. Toque em Testar de novo e escolha \"Sempre permitir\" + \"Permitir\" na TV.").into(),
        TvError::TlsUnsupported => t("A TV só aceita \"Depuração sem fio\" (com pareamento por código). Ative \"Depuração USB\" / \"Depuração pela rede\" nas Opções do desenvolvedor.").into(),
        TvError::TimedOut => t("A TV não concluiu a resposta em 90 segundos. Tente novamente.").into(),
        TvError::Other(why) => tf("Não foi possível falar com a TV: {0}", &[why]),
    }
}

// ───────────── the session (TvControlService.TurnOn / Restore) ─────────────

/// One step with the TV, logged with how long it took. Never fails the session: a TV that doesn't
/// answer is only logged, and the session's own checks do the rest.
fn run(action: &str, provider: &str, timeout: std::time::Duration, work: impl FnOnce() -> Result<(), TvError>) {
    let started = Instant::now();
    match work() {
        Ok(()) => log(&format!("TV: {action} ({provider}) ok em {:.1}s", started.elapsed().as_secs_f32())),
        Err(TvError::TimedOut) => log(&format!("TV: {action} ({provider}) sem resposta em {}s", timeout.as_secs())),
        Err(e) => log(&format!("TV: {action} ({provider}) falhou: {}", e.describe())),
    }
}

/// Session start: wake the TV and switch it to the PC (before the game screen is looked for).
pub fn session_on(tv: &TvControl, data_dir: &Path) {
    run("ligar", &tv.provider, core::START_TIMEOUT, || cm_platform::tv::turn_on(tv, core::START_APPROVAL, core::START_TIMEOUT, data_dir, &log));
}

/// After the restore (and the recovery after a crash): the user's command, then standby if on.
pub fn session_back(step: &TvRestore, data_dir: &Path) {
    run("voltar ao PC", &step.provider, core::STOP_TIMEOUT, || cm_platform::tv::go_back(&step.as_config(), core::STOP_TIMEOUT, data_dir, &log));
}

// ───────────── Sistema → TV ─────────────

impl Console {
    fn show_tv_test_row(&mut self) {
        let label = self.tv_testing.then_some(n("Testando…"));
        let rows = self.system.iter_mut().flat_map(|c| c.sections.iter_mut()).flat_map(|s| s.rows.iter_mut());
        for r in rows.filter(|r| r.title == TEST_ROW) { r.value = Value::Link(label); }
    }

    /// A TV row with a chevron was pressed: a text row opens the keyboard, "Testar a TV" talks to the TV.
    pub(crate) fn tv_row_pressed(&mut self, title: &'static str, desc: &'static str, cx: &mut Context<Self>) {
        if title != TEST_ROW {
            let text = text_of(title, &self.config.tv).to_string();
            self.custom_editor = Some(crate::custom_apps::Editor::for_tv(title, desc, text));
            self.sounds.play(crate::sound::Sound::Confirm);
            return cx.notify();
        }
        if self.tv_testing { return self.show_toast(t("Aguarde, ainda estou trabalhando nisso."), cx); }
        let tv = self.config.tv.clone();
        let data_dir = self.paths.data_dir.clone();
        log(&format!("TV: teste ({})", tv.provider));
        self.tv_testing = true;
        self.show_tv_test_row();
        self.show_toast(t("Falando com a TV… Se ela pedir, aceite na tela da TV."), cx);
        cx.spawn(async move |this, cx| {
            let result = cx.background_executor().spawn(async move { cm_platform::tv::test(&tv, &data_dir, &log) }).await;
            let _ = this.update(cx, |c, cx| {
                c.tv_testing = false;
                c.show_tv_test_row();
                match result {
                    Ok(()) => { log("TV: teste ok"); c.show_toast(t("A TV respondeu: ela deve estar ligada na entrada do PC."), cx); }
                    Err(e) => { log(&format!("TV: teste falhou: {}", e.describe())); c.show_toast(&message(&e), cx); }
                }
            });
        }).detach();
        cx.notify();
    }

    /// The keyboard saved a TV text row.
    pub(crate) fn tv_text_saved(&mut self, title: &str, text: &str) {
        if !store_text(title, text, &mut self.config) { return; }
        for category in &mut self.system { crate::bind::load(&mut category.sections, &self.config); }
        self.save_config();
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn labels_and_values_go_both_ways() {
        for (label, value) in PROVIDERS { assert_eq!((provider_label(value), provider_value(label)), (label, value)); }
        assert_eq!(provider_label("lgWebOs"), "Não controlar", "a provider 2.0 doesn't know shows as off");
        for i in 1..=4 { assert_eq!(hdmi_value(hdmi_label(i)), i); }
        assert_eq!(hdmi_label(9), "HDMI 4");
    }

    #[test]
    fn typed_rows_land_in_the_tv_block() {
        let mut c = Config::default();
        for (row, text) in [(HOST_ROW, " 192.168.0.50 "), (MAC_ROW, "AA:BB:CC:DD:EE:FF"), (INPUT_ROW, "am start -n x/y"), (RESTORE_ROW, "input keyevent 3")] {
            assert!(store_text(row, text, &mut c));
            assert_eq!(text_of(row, &c.tv), text.trim());
        }
        assert!(!store_text("Tema", "x", &mut c));
    }

    #[test]
    fn long_values_are_cut_short_beside_the_row() {
        assert_eq!(shown_value("  "), None);
        assert_eq!(shown_value("192.168.0.50"), Some("192.168.0.50"));
        let long = "am start -n com.smartdevice.livetv/skyworth.LiveTvScreenActivity";
        let v = shown_value(long).unwrap();
        assert_eq!(v.chars().count(), 26);
        assert!(v.ends_with('…'));
        assert!(std::ptr::eq(v, shown_value(long).unwrap()), "kept once");
    }

    #[test]
    fn every_error_has_a_message() {
        for e in [TvError::NotConfigured, TvError::HostMissing, TvError::Unreachable("10.0.0.2".into()), TvError::NotAllowed, TvError::TlsUnsupported, TvError::TimedOut, TvError::Other("x".into())] {
            assert!(!message(&e).is_empty());
        }
        assert!(message(&TvError::Unreachable("10.0.0.2".into())).contains("10.0.0.2"));
    }
}
