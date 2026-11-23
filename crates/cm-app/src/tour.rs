//! The first-run tour (1.7 ConsoleTour, texts adapted to 2.0): a card at the bottom that walks
//! through the tabs; each step shows the tab it talks about. Runs once (config.tour_done) and
//! again from Sistema → Sobre → "Ver o tour de novo".

use cm_ui::{Glyph, Ui, color, motion};
use gpui::{AnimationExt, Context, Div, FontWeight, InteractiveElement, IntoElement, ParentElement, SharedString, StatefulInteractiveElement, Styled, div};

use crate::{home::{Console, View}, i18n::{t, tf}, input::Pad, sound::Sound};

/// (tab shown, title, text)
pub(crate) const STEPS: [(usize, &str, &str); 7] = [
    (0, "Bem-vindo ao Console Mode 2", "Ele transforma o PC num console: põe o jogo na TV (ou na tela que você escolher), apaga as outras telas e abre o Steam Big Picture, o Playnite, o modo Xbox ou um app seu. Ao voltar, a mesa fica como estava. Use o controle, o mouse ou o teclado."),
    (0, "Três abas", "Início, Sessão e Sistema. Troque de aba com LB e RB no controle, Q e E no teclado, ou clicando no nome dela."),
    (0, "O que vai abrir", "Cada card é um iniciador. A joga; Y (ou o botão direito do mouse) abre as opções do card, como ajustes só para um jogo ou mover na fileira."),
    (1, "Sua sessão", "Na aba Sessão você escolhe a tela de jogo, quais telas apagam, resolução, HDR, áudio e FPS. Tudo volta ao que era quando a sessão acaba."),
    (0, "Durante o jogo", "Segure ☰ (Menu/Options) para abrir o menu da sessão por cima do jogo: janelas, tela e som, volume, FPS e encerrar a sessão."),
    (2, "Todas as configurações", "A aba Sistema reúne o resto: controles e atalhos, sons, aparência, inicialização, atualizações e idioma."),
    (0, "Tudo pronto", "Confira a tela de jogo na aba Sessão e aperte Jogar. Para ver este tour de novo: Sistema → Sobre."),
];

impl Console {
    pub(crate) fn start_tour(&mut self, cx: &mut Context<Self>) {
        self.view = View::Tour { step: 0 };
        self.tab = STEPS[0].0;
        self.menu_anim += 1;
        cx.notify();
    }

    fn tour_go(&mut self, step: usize, cx: &mut Context<Self>) {
        if step >= STEPS.len() { return self.end_tour(cx); }
        self.view = View::Tour { step };
        self.tab = STEPS[step].0;
        self.vol_anim += 1;
        self.sounds.play(Sound::Move);
        cx.notify();
    }

    fn end_tour(&mut self, cx: &mut Context<Self>) {
        self.view = View::Home;
        self.tab = 0;
        self.config.tour_done = true;
        self.save_config();
        self.sounds.play(Sound::Confirm);
        cx.notify();
    }

    /// True when the tour took the press.
    pub(crate) fn tour_press(&mut self, p: Pad, cx: &mut Context<Self>) -> bool {
        let View::Tour { step } = self.view else { return false };
        match p {
            Pad::A | Pad::Right => self.tour_go(step + 1, cx),
            Pad::Left | Pad::LB if step > 0 => self.tour_go(step - 1, cx),
            Pad::B | Pad::Menu => self.end_tour(cx),
            _ => {}
        }
        true
    }

    pub(crate) fn tour_view(&self, ui: &Ui, step: usize, cx: &mut Context<Self>) -> gpui::AnyElement {
        let (_, title, body) = STEPS[step];
        let last = step + 1 == STEPS.len();
        let btn = |id: &'static str, glyph: Glyph, label: &'static str, p: Pad, cx: &mut Context<Self>| {
            ui.hint(glyph, label, None).id(id).cursor_pointer().px(ui.u(14.)).py(ui.u(8.)).rounded_full().hover(|s| s.bg(color::white(0.1)))
                .on_click(cx.listener(move |this, _, _, cx| { this.tour_press(p, cx); }))
        };
        let mut buttons = div().flex().items_center().gap(ui.u(18.)).text_size(ui.u(20.));
        buttons = buttons.child(btn("tour-skip", Glyph::Text("B"), t("Pular"), Pad::B, cx));
        if step > 0 { buttons = buttons.child(btn("tour-back", Glyph::Text("LB"), t("Voltar"), Pad::LB, cx)); }
        buttons = buttons.child(btn("tour-next", Glyph::Text("A"), if last { t("Começar") } else { t("Próximo") }, Pad::A, cx));
        let w = 980.;
        let card = div().w(ui.u(w)).px(ui.u(36.)).py(ui.u(30.)).rounded(ui.u(18.)).bg(color::panel()).border(ui.u(1.)).border_color(color::white(0.1)).shadow(ui.raised(ui.lift_lg(), 0.14))
            .child(div().flex().justify_between().items_center().mb(ui.u(10.))
                .child(div().text_size(ui.u(34.)).font_family(cm_ui::FONT).font_weight(FontWeight::BOLD).child(t(title)))
                .child(div().text_size(ui.u(18.)).text_color(color::mute()).child(SharedString::from(tf("{0} de {1}", &[&(step + 1), &STEPS.len()])))))
            .child(div().text_size(ui.u(23.)).line_height(ui.u(34.)).text_color(color::mute()).mb(ui.u(22.)).child(t(body)))
            .child(div().flex().justify_end().child(buttons));
        let (ui2, d) = (*ui, motion::dist(14.));
        div().absolute().top_0().left_0().size_full()
            .child(div().id("tour-scrim").absolute().top_0().left_0().size_full().bg(color::shade(0.55)).occlude())
            .child(div().absolute().left(ui.u((ui.design.width - w) / 2.)).bottom(ui.u(120.)).child(card)
                .with_animation(("tour", self.vol_anim * 64 + step), motion::out(motion::SMALL), move |el, t| el.opacity(t).bottom(ui2.u(120. - d * (1. - t)))))
            .into_any_element()
    }
}

/// Not a Div trait import user: keeps `Div` in scope for the closures above.
#[allow(dead_code)]
fn _div(_: Div) {}
