//! Sistema → Atualizações: look for a newer alpha, then install it (docs/v2/RELEASE.md).
//!
//! The network is touched only when the user presses "Procurar atualização" (and again to install)
//! or by the quiet check at startup, which says nothing unless it finds something. The rules and
//! the SHA-256 check live in `cm_core::update` / `cm_platform::update`.

use std::sync::Mutex;

use cm_platform::update::{self, Update};
use gpui::Context;

use crate::{home::Console, i18n::{n, t, tf}, settings::Value};

pub const CHECK_ROW: &str = n("Procurar atualização");
/// What the row says once a newer alpha was found (pressing it again installs).
const INSTALL_LABEL: &str = n("Instalar e reiniciar");
const CURRENT: &str = env!("CARGO_PKG_VERSION");

#[derive(Default)]
struct State {
    /// A check or a download is running.
    busy: bool,
    /// The newer alpha found by the last check.
    pending: Option<Update>,
}

static STATE: Mutex<State> = Mutex::new(State { busy: false, pending: None });

fn state() -> std::sync::MutexGuard<'static, State> { STATE.lock().unwrap_or_else(|e| e.into_inner()) }

impl Console {
    /// Startup: if "Procurar atualizações ao abrir" is on, check once; silent when nothing is found.
    pub(crate) fn check_updates_quietly(&mut self, cx: &mut Context<Self>) {
        if !self.config.check_updates || std::env::var_os("CM2_NO_UPDATE_CHECK").is_some() { return; }
        self.start_check(false, cx);
    }

    /// "Procurar atualização" pressed: check, or install the update the last check found.
    pub(crate) fn update_row_pressed(&mut self, cx: &mut Context<Self>) {
        if state().busy { return self.show_toast(t("Aguarde, ainda estou trabalhando nisso."), cx); }
        let pending = state().pending.clone();
        match pending {
            Some(u) => self.install_update(u, cx),
            None => self.start_check(true, cx),
        }
    }

    fn start_check(&mut self, announce: bool, cx: &mut Context<Self>) {
        {
            let mut s = state();
            if s.busy { return; }
            s.busy = true;
        }
        if announce { self.show_toast(t("Procurando atualização…"), cx); }
        cx.spawn(async move |this, cx| {
            let found = cx.background_executor().spawn(async { update::check(CURRENT) }).await;
            state().busy = false;
            let _ = this.update(cx, |c, cx| match found {
                Ok(Some(u)) => {
                    crate::startup_log(&format!("atualização: {} disponível", u.version));
                    c.show_toast(&tf("A versão {0} está disponível. Em Sistema › Atualizações, escolha “{1}”.", &[&u.version, &t(INSTALL_LABEL)]), cx);
                    state().pending = Some(u);
                    c.set_update_row(Some(INSTALL_LABEL));
                    cx.notify();
                }
                Ok(None) => if announce { c.show_toast(&tf("Você já está na versão mais recente ({0}).", &[&CURRENT]), cx) },
                Err(e) => {
                    crate::startup_log(&format!("atualização: não foi possível procurar: {e}"));
                    if announce { c.show_toast(t("Não consegui procurar atualização agora. Tente de novo mais tarde."), cx) }
                }
            });
        }).detach();
    }

    fn install_update(&mut self, u: Update, cx: &mut Context<Self>) {
        // the app closes to install, so never in the middle of a session
        if self.running.is_some() { return self.show_toast(t("Termine a sessão antes de atualizar."), cx); }
        state().busy = true;
        self.show_toast(&tf("Baixando a versão {0}…", &[&u.version]), cx);
        // the next start (the installer reopens the app) shows what this version brings
        crate::whats_new::save_for_next_start(&self.paths.data_dir, &u);
        cx.spawn(async move |this, cx| {
            let done = cx.background_executor().spawn(async move { update::download(&u, CURRENT, &mut |_| {}) }).await;
            state().busy = false;
            let _ = this.update(cx, |c, cx| match done {
                Ok(setup) => match update::launch_installer(&setup) {
                    Ok(()) => { crate::startup_log("atualização: instalador aberto, saindo"); cx.quit(); }
                    Err(e) => { crate::startup_log(&format!("atualização: {e}")); c.show_toast(t("Não consegui abrir o instalador."), cx) }
                },
                Err(e) => {
                    crate::startup_log(&format!("atualização: download recusado: {e}"));
                    c.show_toast(t("A atualização não pôde ser baixada ou não passou na verificação. Nada foi instalado."), cx)
                }
            });
        }).detach();
    }

    /// The text beside "Procurar atualização" (None = plain row).
    fn set_update_row(&mut self, label: Option<&'static str>) {
        let rows = self.system.iter_mut().flat_map(|c| c.sections.iter_mut()).flat_map(|s| s.rows.iter_mut());
        for r in rows.filter(|r| r.title == CHECK_ROW) { r.value = Value::Link(label); }
    }
}
