//! The session menu's "Apps" button (1.7's pinned ControlFS and JoyChromium rows): a row each, ↑↓ pick,
//! A opens it over the game (the menu closes and the app steps aside, like "Voltar ao jogo"). Missing,
//! the same press downloads and installs it, the row showing the progress, and the next press opens it;
//! if the install failed, the release page opens in the browser. Nothing is downloaded unless the user
//! presses the row. Rules: `cm_core::helper_apps`; Windows side: `cm_platform::helper_apps`.

use std::sync::atomic::{AtomicBool, Ordering};

use cm_core::helper_apps::{APPS, App, Press, Status};
use cm_platform::helper_apps as win;
use cm_ui::{Ui, color, glyph};
use gpui::{Context, FontWeight, InteractiveElement, IntoElement, ParentElement, SharedString, StatefulInteractiveElement, Styled, div};

use crate::{home::Console, i18n::{n, t, tf}, sound::Sound};

/// JoyChromium was opened from the menu during this session: going back to the PC hands it `--window`.
static JOYCHROMIUM_OPENED: AtomicBool = AtomicBool::new(false);

/// The row titles (1.7's "Explorador de arquivos (ControlFS)" and "Navegador (JoyChromium)").
pub const NAMES: [&str; 2] = [n("Explorador de arquivos (ControlFS)"), n("Navegador (JoyChromium)")];

/// What the session engine calls while restoring the desk (1.7 `ReturnJoyChromiumToWindow`, before the screens come back).
pub fn session_ended() {
    let opened = JOYCHROMIUM_OPENED.swap(false, Ordering::SeqCst);
    if cm_core::helper_apps::should_return_to_window(opened, opened && win::is_running(App::JoyChromium)) { win::return_joychromium_to_window(); }
}

fn user_agent() -> String { format!("ConsoleMode2Alpha/{}", env!("CARGO_PKG_VERSION")) }

fn icon(app: App) -> char { match app { App::ControlFs => glyph::FOLDER, App::JoyChromium => glyph::LANGUAGE } }

impl Console {
    /// The button got focus: installed or not is read again, a failed install is offered again.
    pub(crate) fn refresh_helper_rows(&mut self) {
        for (i, app) in APPS.into_iter().enumerate() { self.helper_rows[i].menu_opened(win::find_exe(app).is_some()); }
    }

    /// What the row says under its name.
    fn helper_status(&self, i: usize) -> String {
        match self.helper_rows[i].status() {
            Status::Installing => t("Instalando…").into(),
            Status::Downloading(p) => tf("Baixando… {0}%", &[&p]),
            Status::JustInstalled => t("Instalado · aperte para abrir").into(),
            Status::Ready if APPS[i] == App::ControlFs => t("Navegue pelos seus arquivos com o controle").into(),
            Status::Ready => t("Navegue pela web com o controle").into(),
            Status::Failed => t("Não deu para instalar · aperte para abrir a página de download").into(),
            Status::Missing => t("Não instalado · aperte para baixar e instalar").into(),
        }
    }

    /// A on a row.
    pub(crate) fn helper_press(&mut self, i: usize, cx: &mut Context<Self>) {
        let app = APPS[i];
        match self.helper_rows[i].press(win::find_exe(app).is_some()) {
            Press::Busy => {}
            Press::Install => { self.sounds.play(Sound::Confirm); self.install_helper(i, cx); }
            Press::ReleasePage => {
                self.sounds.play(Sound::Confirm);
                crate::startup_log(&format!("{}: não instalado, abrindo a página de download", app.name()));
                win::open_release_page(app);
            }
            Press::Open => {
                self.sounds.play(Sound::Confirm);
                // the screen it goes to: the session's game screen, else the one chosen for sessions
                let focus = self.running.as_ref().map(|r| r.shell_focus.clone()).unwrap_or_else(|| self.config.focus_monitor.clone());
                let screen = cm_platform::WinDesk::new(self.paths.data_dir.clone()).screen_rect(&focus).or_else(cm_platform::primary_rect);
                self.close_overlay(cx);
                if self.running.is_some() {
                    self.app_in_front = false;
                    if let Some(w) = self.window { cx.defer(move |cx| { let _ = w.update(cx, |_, w, _| crate::stage::step_aside(w)); }); }
                }
                if !win::open(app) {
                    crate::startup_log(&format!("{}: não abriu, abrindo a página de download", app.name()));
                    return win::open_release_page(app);
                }
                if app == App::JoyChromium && self.running.is_some() { JOYCHROMIUM_OPENED.store(true, Ordering::SeqCst); }
                if let Some(s) = screen { std::thread::spawn(move || win::ensure_on_screen(app, s)); }
            }
        }
        cx.notify();
    }

    /// Downloads and installs it off the UI thread; the row follows. It goes on if the menu closes;
    /// it is not opened when done (the menu may have lost the foreground by then): the next A opens it.
    fn install_helper(&mut self, i: usize, cx: &mut Context<Self>) {
        let app = APPS[i];
        self.helper_rows[i].install_started();
        crate::startup_log(&format!("{}: não instalado, baixando e instalando a pedido do menu", app.name()));
        let (tx, mut rx) = futures::channel::mpsc::unbounded::<Result<Option<f32>, Result<String, String>>>();
        std::thread::spawn(move || {
            let progress_tx = tx.clone();
            let result = win::install(app, &user_agent(), &mut |p| { let _ = progress_tx.unbounded_send(Ok(p)); });
            let _ = tx.unbounded_send(Err(result));
        });
        cx.spawn(async move |this, cx| {
            use futures::StreamExt;
            while let Some(msg) = rx.next().await {
                let done = msg.is_err();
                let _ = this.update(cx, |c, cx| {
                    match msg {
                        Ok(p) => c.helper_rows[i].install_progress(p),
                        Err(Ok(exe)) => { crate::startup_log(&format!("{}: instalado em {exe}", app.name())); c.helper_rows[i].install_finished(true); }
                        Err(Err(why)) => { crate::startup_log(&format!("{}: instalar: {why}", app.name())); c.helper_rows[i].install_finished(false); }
                    }
                    cx.notify();
                });
                if done { break; }
            }
        }).detach();
        cx.notify();
    }

    /// The panel above the buttons: one row per app, the picked one lit.
    pub(crate) fn helper_panel(&self, ui: &Ui, cx: &mut Context<Self>) -> gpui::AnyElement {
        let rows = APPS.into_iter().enumerate().map(|(i, app)| {
            let on = i == self.guide_row;
            let installing = self.helper_rows[i].installing;
            let mut r = div().id(("helper-row", i)).flex().items_center().gap(ui.u(18.)).px(ui.u(18.)).py(ui.u(12.)).rounded(ui.u(12.)).cursor_pointer()
                .child(div().size(ui.u(52.)).rounded(ui.u(12.)).bg(color::white(0.08)).flex().items_center().justify_center().child(ui.icon(icon(app), 30., color::ink())))
                .child(div().flex().flex_col().flex_1().min_w_0()
                    .child(div().text_size(ui.u(22.)).font_weight(FontWeight::SEMIBOLD).whitespace_nowrap().child(t(NAMES[i])))
                    .child(div().text_size(ui.u(18.)).text_color(color::mute()).whitespace_nowrap().overflow_hidden().child(SharedString::from(self.helper_status(i)))))
                .on_click(cx.listener(move |this, _, _, cx| { this.guide_row = i; this.helper_press(i, cx); }));
            if installing {
                let p = self.helper_rows[i].progress;
                r = r.child(div().w(ui.u(120.)).h(ui.u(6.)).rounded(ui.u(3.)).bg(color::white(0.2)).relative()
                    .child(div().absolute().top_0().left_0().h_full().w(ui.u(120. * p)).rounded(ui.u(3.)).bg(color::accent())));
            }
            if on { r.bg(ui.focus_fill(0.16, 0.09)).shadow(ui.raised(ui.lift_sm(), 0.16)) } else { r }
        }).collect::<Vec<_>>();
        div().flex().flex_col().gap(ui.u(4.)).children(rows).into_any_element()
    }
}
