//! "What's new" after an update (1.7 ReleaseNotes, in 2.0): the first start on a version that is not
//! the one that ran last shows a card once, with the first item of that release's notes. The update
//! that was installed left its notes in the data folder (`updates.rs`); without them (an install done
//! by hand) the card names the version and points to the release page. The rules are in
//! `cm_core::release_notes`.

use cm_core::{release_notes::{self, Saved}, update::REPOSITORY};
use cm_ui::{Glyph, Ui, color, motion};
use gpui::{AnimationExt, Context, FontWeight, InteractiveElement, IntoElement, ParentElement, SharedString, StatefulInteractiveElement, Styled, div};

use crate::{
    home::{Console, View},
    i18n::{n, t, tf},
    input::Pad,
    sound::Sound,
};

const CURRENT: &str = env!("CARGO_PKG_VERSION");

/// Leaves the notes of an update about to be installed, for the next start to show.
pub fn save_for_next_start(data_dir: &std::path::Path, u: &cm_core::update::Update) {
    let saved = Saved { version: u.version.clone(), notes: u.notes.clone(), page_url: u.page_url.clone() };
    if let Ok(text) = serde_json::to_string_pretty(&saved) { let _ = std::fs::write(data_dir.join(release_notes::FILE), text); }
}

impl Console {
    /// At start: decide whether this start is the first on a new version. Nothing to announce records the
    /// version at once; a notice waits until the app is in front and idle (see `promote_whats_new`).
    pub(crate) fn prepare_whats_new(&mut self) {
        let file = self.paths.data_dir.join(release_notes::FILE);
        let saved = std::fs::read_to_string(&file).ok().and_then(|t| serde_json::from_str::<Saved>(t.trim_start_matches('﻿')).ok());
        let (notice, record) = release_notes::decide(&self.config.last_run_version, CURRENT, saved.as_ref(), crate::i18n::code(), REPOSITORY);
        match notice {
            Some(n) => { crate::startup_log(&format!("novidades: versão {} (antes {})", n.version, self.config.last_run_version)); self.pending_whats_new = Some(n); }
            None => {
                if self.config.last_run_version != record { self.config.last_run_version = record; self.save_config(); }
                // notes left for a version that is not this one are of no use
                if saved.is_some() { let _ = std::fs::remove_file(&file); }
            }
        }
    }

    /// Shows the waiting notice once the app is in front, on the Início, with no opening playing.
    pub(crate) fn promote_whats_new(&mut self, window_visible: bool, cx: &mut Context<Self>) {
        if self.pending_whats_new.is_none() || !window_visible || self.intro.is_some() || self.running.is_some() || self.custom_editor.is_some() { return; }
        if !matches!(self.view, View::Home) || self.tab != 0 { return; }
        let Some(notice) = self.pending_whats_new.take() else { return };
        // once: recorded as soon as it is shown
        self.config.last_run_version = CURRENT.to_string();
        self.save_config();
        let _ = std::fs::remove_file(self.paths.data_dir.join(release_notes::FILE));
        self.whats_new = Some(notice);
        self.vol_anim += 1;
        self.sounds.play(Sound::Confirm);
        cx.notify();
    }

    /// True when the card took the press.
    pub(crate) fn whats_new_press(&mut self, p: Pad, cx: &mut Context<Self>) -> bool {
        let Some(notice) = &self.whats_new else { return false };
        match p {
            Pad::A => { cm_platform::launch::open(&notice.url); self.whats_new = None; self.sounds.play(Sound::Confirm); }
            Pad::B | Pad::Menu => { self.whats_new = None; self.sounds.play(Sound::Back); }
            _ => {}
        }
        cx.notify();
        true
    }

    pub(crate) fn whats_new_view(&self, ui: &Ui, cx: &mut Context<Self>) -> Option<gpui::AnyElement> {
        let notice = self.whats_new.as_ref()?;
        let btn = |id: &'static str, glyph: Glyph, label: &'static str, p: Pad, cx: &mut Context<Self>| {
            ui.hint(glyph, t(label), None).id(id).cursor_pointer().px(ui.u(14.)).py(ui.u(8.)).rounded_full().hover(|s| s.bg(color::white(0.1)))
                .on_click(cx.listener(move |this, _, _, cx| { this.whats_new_press(p, cx); }))
        };
        let body = match &notice.line { Some(l) => l.clone(), None => t(n("Veja o que mudou nas notas desta versão.")).to_string() };
        let buttons = div().flex().items_center().gap(ui.u(18.)).text_size(ui.u(20.))
            .child(btn("wn-close", Glyph::Text("B"), n("Fechar"), Pad::B, cx))
            .child(btn("wn-open", Glyph::Text("A"), n("Ver as novidades"), Pad::A, cx));
        let w = 860.;
        let card = div().w(ui.u(w)).px(ui.u(36.)).py(ui.u(28.)).rounded(ui.u(18.)).bg(color::panel()).border(ui.u(1.)).border_color(color::white(0.1)).shadow(ui.raised(ui.lift_lg(), 0.14))
            .child(div().text_size(ui.u(32.)).font_family(cm_ui::FONT).font_weight(FontWeight::BOLD).mb(ui.u(10.)).child(SharedString::from(tf("Atualizado para a versão {0}", &[&notice.version]))))
            .child(div().text_size(ui.u(23.)).line_height(ui.u(34.)).text_color(color::mute()).mb(ui.u(22.)).child(SharedString::from(body)))
            .child(div().flex().justify_end().child(buttons));
        let (ui2, d) = (*ui, motion::dist(14.));
        Some(div().absolute().top_0().left_0().size_full()
            .child(div().id("wn-scrim").absolute().top_0().left_0().size_full().bg(color::shade(0.45)).occlude())
            .child(div().absolute().left(ui.u((ui.design.width - w) / 2.)).bottom(ui.u(120.)).child(card)
                .with_animation(("whats-new", self.vol_anim), motion::out(motion::SMALL), move |el, t| el.opacity(t).bottom(ui2.u(120. - d * (1. - t)))))
            .into_any_element())
    }
}
