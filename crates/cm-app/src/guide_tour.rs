//! The session menu's guided tour (1.7 SessionMenuTour): offered the first time the menu opens, and
//! then on Y in the menu or from Sistema → Sobre. Each stop selects the button it talks about (the
//! rest of the menu dims) and a card says what that button does. The stops are in
//! `cm_core::session_menu_tour`; the first-run tour of the whole app is in tour.rs.

use cm_core::session_menu_tour::{self as tour, Stop};
use cm_ui::{Glyph, Ui, color, glyph, motion};
use gpui::{AnimationExt, Context, FontWeight, InteractiveElement, IntoElement, ParentElement, SharedString, StatefulInteractiveElement, Styled, div};

use crate::{
    home::{Console, View},
    i18n::{n, t, tf},
    input::Pad,
    overlays::{GUIDE_APPS, GUIDE_FPS, GUIDE_SCREEN, GUIDE_TASK_VIEW, GUIDE_VOLUME, GUIDE_WINDOWS},
    sound::Sound,
};

/// The tour in progress: its stops and where it is.
pub struct GuideTour {
    pub stops: Vec<Stop>,
    pub index: usize,
}

impl GuideTour {
    fn stop(&self) -> Stop { self.stops[self.index] }
    fn is_last(&self) -> bool { self.index + 1 == self.stops.len() }
}

/// The button of the menu a stop talks about (None: the invite, which points at nothing).
fn button_of(stop: Stop) -> Option<usize> {
    Some(match stop {
        Stop::Invite => return None,
        Stop::BackToGame => 0,
        Stop::Windows => GUIDE_WINDOWS,
        Stop::Display => GUIDE_SCREEN,
        Stop::Volume => GUIDE_VOLUME,
        Stop::Fps => GUIDE_FPS,
        Stop::TaskView => GUIDE_TASK_VIEW,
        Stop::Apps => GUIDE_APPS,
        Stop::Exit => crate::overlays::GUIDE.len() - 1,
    })
}

/// What a stop says: its title (a button's own name where there is one) and its text. `preview`: the
/// menu was opened outside a session, where nothing is restored afterwards.
pub(crate) fn texts(stop: Stop, preview: bool) -> (&'static str, &'static str) {
    match stop {
        Stop::Invite => (n("Primeira vez neste menu?"), n("Um tour rápido mostra o que cada botão faz. Para ver de novo depois, aperte Y aqui no menu.")),
        Stop::BackToGame => (n("Voltar ao jogo"), n("Fecha este menu e devolve o controle ao jogo, do jeito que estava. O B faz o mesmo de qualquer ponto do menu.")),
        Stop::Windows => (n("Janelas"), n("Os programas abertos, em miniatura, como no Win + Tab. ↑ e ↓ escolhem a janela, A traz ela para a frente e X pede para ela fechar.")),
        Stop::Display if preview => (n("Tela"), n("A tela de jogo e a resolução. Durante uma sessão, HDR e VRR ligam e desligam aqui; fora dela, como agora, só mostra como está.")),
        Stop::Display => (n("Tela"), n("A tela de jogo e a resolução. ↑ e ↓ escolhem HDR ou VRR e A liga ou desliga na hora; ao encerrar a sessão, tudo volta ao que era antes.")),
        Stop::Volume => (n("Volume"), n("Com este botão em foco, ↑ e ↓ ajustam o volume de 5 em 5 e X troca a saída de áudio; ao encerrar a sessão, o áudio volta ao que era antes.")),
        Stop::Fps => ("FPS", n("O limite de FPS trava a taxa de quadros dos jogos, pelo RivaTuner (RTSS). ↑ e ↓ mudam o limite na hora; ao encerrar a sessão, ele volta ao que era.")),
        Stop::TaskView => (n("Visão de tarefas"), n("Abre a Visão de tarefas do Windows por cima do jogo, para trocar de janela. Com um controle PlayStation, o direcional e o analógico escolhem a janela, A abre e B fecha.")),
        Stop::Apps => (n("Apps"), n("Abre o ControlFS, um explorador de arquivos feito para o controle, em tela cheia. Logo abaixo, o JoyChromium faz o mesmo com um navegador para a TV: abre por cima do jogo e, ao encerrar a sessão, fica aberto em janela. Se um deles ainda não estiver instalado, o primeiro A baixa e instala.")),
        Stop::Exit if preview => (n("Fechar menu"), n("Fora de uma sessão não há nada para restaurar, então este botão só fecha o menu. Durante uma sessão ele encerra a sessão e devolve telas, áudio e resolução ao que eram.")),
        Stop::Exit => (n("Encerrar sessão"), n("Encerra a sessão: telas, resolução, HDR, áudio e limite de FPS voltam ao que eram antes. O Console Mode continua aberto, pronto para a próxima.")),
    }
}

impl Console {
    /// The menu just opened: start the tour Sistema asked for, or offer it if it was never taken.
    pub(crate) fn on_guide_opened(&mut self, cx: &mut Context<Self>) {
        if std::mem::take(&mut self.tour_on_open) { self.start_guide_tour(false, cx); }
        else if !self.config.session_menu_tour_done { self.start_guide_tour(true, cx); }
    }

    /// Y in the menu: the tour straight away, no invite. `invite`: it is being offered.
    pub(crate) fn start_guide_tour(&mut self, invite: bool, cx: &mut Context<Self>) {
        if !matches!(self.view, View::Guide { .. }) || self.guide_tour.is_some() { return; }
        self.guide_tour = Some(GuideTour { stops: tour::stops(invite), index: 0 });
        crate::startup_log(if invite { "Menu da sessão: tour oferecido" } else { "Menu da sessão: tour iniciado" });
        self.guide_tour_select();
        cx.notify();
    }

    /// The menu's selected button follows the stop.
    fn guide_tour_select(&mut self) {
        let Some(stop) = self.guide_tour.as_ref().map(GuideTour::stop) else { return };
        self.vol_anim += 1;
        self.view = View::Guide { sel: button_of(stop).unwrap_or(0) };
    }

    /// The menu closed under the tour (☰, the session ending): it is put away and still offered next time.
    pub(crate) fn drop_guide_tour(&mut self) {
        self.guide_tour = None;
        self.tour_on_open = false;
    }

    /// B, "Pular" or "Agora não", or the last stop done: not offered again (Y and Sistema still show it).
    fn end_guide_tour(&mut self, cx: &mut Context<Self>) {
        let Some(t) = self.guide_tour.take() else { return };
        crate::startup_log(&match t.stop() {
            Stop::Invite => "Menu da sessão: tour recusado".to_string(),
            _ if t.is_last() => "Menu da sessão: tour concluído".to_string(),
            stop => format!("Menu da sessão: tour encerrado em {stop:?}"),
        });
        self.view = View::Guide { sel: 0 };
        self.vol_anim += 1;
        if !self.config.session_menu_tour_done {
            self.config.session_menu_tour_done = true;
            self.save_config();
        }
        self.sounds.play(Sound::Confirm);
        cx.notify();
    }

    /// True when the tour took the press (it always does while it is open).
    pub(crate) fn guide_tour_press(&mut self, p: Pad, cx: &mut Context<Self>) -> bool {
        let Some(t) = &mut self.guide_tour else { return false };
        match p {
            Pad::A | Pad::Right => {
                if t.is_last() { self.end_guide_tour(cx); return true; }
                if t.stop() == Stop::Invite { crate::startup_log("Menu da sessão: tour aceito"); }
                t.index += 1;
                self.sounds.play(Sound::Move);
            }
            // one stop back, never back to the invite
            Pad::Left if t.index > 0 && t.stops[t.index - 1] != Stop::Invite => { t.index -= 1; self.sounds.play(Sound::Move); }
            Pad::B | Pad::Y => { self.end_guide_tour(cx); return true; }
            _ => {}
        }
        self.guide_tour_select();
        cx.notify();
        true
    }

    /// The scrim over the menu (everything but the buttons) and the card.
    pub(crate) fn guide_tour_card(&self, ui: &Ui, cx: &mut Context<Self>) -> Option<gpui::AnyElement> {
        let gt = self.guide_tour.as_ref()?;
        let stop = gt.stop();
        let preview = self.running.is_none();
        let (title, body) = texts(stop, preview);
        let invite = stop == Stop::Invite;
        let btn = |id: &'static str, glyph: Glyph, label: &'static str, p: Pad, cx: &mut Context<Self>| {
            ui.hint(glyph, t(label), None).id(id).cursor_pointer().px(ui.u(14.)).py(ui.u(8.)).rounded_full().hover(|s| s.bg(color::white(0.1)))
                .on_click(cx.listener(move |this, _, _, cx| { this.guide_tour_press(p, cx); }))
        };
        let mut buttons = div().flex().items_center().gap(ui.u(18.)).text_size(ui.u(20.));
        buttons = buttons.child(btn("gt-skip", Glyph::Text("B"), if invite { n("Agora não") } else { n("Pular") }, Pad::B, cx));
        if gt.index > 0 && gt.stops[gt.index - 1] != Stop::Invite { buttons = buttons.child(btn("gt-back", Glyph::Icon(glyph::CHEVRON_LEFT), n("Voltar"), Pad::Left, cx)); }
        let primary = if invite { n("Fazer o tour") } else if gt.is_last() { n("Concluir") } else { n("Próximo") };
        buttons = buttons.child(btn("gt-next", Glyph::Text("A"), primary, Pad::A, cx));

        let mut head = div().flex().justify_between().items_center().mb(ui.u(10.))
            .child(div().text_size(ui.u(32.)).font_family(cm_ui::FONT).font_weight(FontWeight::BOLD).child(t(title)));
        if let Some((number, total)) = tour::progress(&gt.stops, gt.index) {
            head = head.child(div().text_size(ui.u(18.)).text_color(color::mute()).child(SharedString::from(tf("{0} de {1}", &[&number, &total]))));
        }
        let w = 860.;
        let card = div().w(ui.u(w)).px(ui.u(36.)).py(ui.u(28.)).rounded(ui.u(18.)).bg(color::panel()).border(ui.u(1.)).border_color(color::white(0.1)).shadow(ui.raised(ui.lift_lg(), 0.14))
            .child(head)
            .child(div().text_size(ui.u(23.)).line_height(ui.u(34.)).text_color(color::mute()).mb(ui.u(22.)).child(t(body)))
            .child(div().flex().justify_end().child(buttons));
        let (ui2, d) = (*ui, motion::dist(14.));
        let key = self.vol_anim;
        Some(div().absolute().left(ui.u((ui.design.width - w) / 2.)).bottom(ui.u(310.)).child(card)
            .with_animation(("guide-tour", key), motion::out(motion::SMALL), move |el, t| el.opacity(t).bottom(ui2.u(310. - d * (1. - t))))
            .into_any_element())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn every_stop_has_a_title_and_a_text() {
        for stop in tour::stops(true) {
            for preview in [false, true] {
                let (title, body) = texts(stop, preview);
                assert!(!title.is_empty() && body.len() > 20, "{stop:?}");
            }
        }
    }

    #[test]
    fn what_changes_outside_a_session_is_said_differently() {
        // in a session the changes are undone at the end; in the preview nothing is, and the way out only closes the menu
        assert_ne!(texts(Stop::Display, false).1, texts(Stop::Display, true).1);
        assert_ne!(texts(Stop::Exit, false), texts(Stop::Exit, true));
        assert_eq!(texts(Stop::Volume, false), texts(Stop::Volume, true));
    }

    #[test]
    fn each_stop_points_at_its_own_button() {
        let buttons: Vec<_> = tour::stops(false).into_iter().filter_map(button_of).collect();
        assert_eq!(buttons.len(), 8);
        let mut sorted = buttons.clone();
        sorted.sort();
        sorted.dedup();
        assert_eq!(sorted.len(), 8, "one button each");
        assert_eq!(button_of(Stop::Invite), None);
        // the labels the tour borrows as titles are the buttons' own
        for stop in tour::stops(false) {
            let b = button_of(stop).unwrap();
            assert_eq!(texts(stop, false).0, crate::overlays::GUIDE[b].1, "{stop:?}");
        }
    }
}
