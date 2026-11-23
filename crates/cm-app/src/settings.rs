//! Sessão and Sistema, PS4/PS5 style (mockup): Sistema opens on a list of categories; a category,
//! and Sessão itself, are two panes: sections on the left, that section's options on the right.
//!
//! Values live in memory for now: the config file and the session engine come in fase 2.

use cm_ui::{Glyph, Ui, color, glyph, motion};
use gpui::{AnimationExt, Context, Div, FontWeight, InteractiveElement, IntoElement, ParentElement, SharedString, StatefulInteractiveElement, Styled, div, linear_color_stop, linear_gradient, prelude::*};

use crate::{home::Console, i18n::{shown, t, tf}, input::Pad, pad_shortcuts as pads, sound::Sound, tv};
use cm_core::shortcuts::Slot;

#[path = "gaming_home_settings.rs"]
pub(crate) mod gaming_home_settings;

#[derive(Clone)]
pub enum Value {
    Toggle(bool),
    Choice(Vec<&'static str>, usize),
    /// Opens something (chevron); `Some` shows a value before the chevron.
    Link(Option<&'static str>),
    Info(&'static str),
}

impl Value {
    pub(crate) fn is_interactive(&self) -> bool { !matches!(self, Self::Info(_)) }

    pub fn activate(&mut self) {
        match self {
            Value::Toggle(on) => *on = !*on,
            Value::Choice(opts, i) => *i = (*i + 1) % opts.len(),
            Value::Link(_) | Value::Info(_) => {}
        }
    }
    pub fn text(&self) -> Option<&'static str> {
        match self { Value::Choice(o, i) => Some(o[*i]), Value::Link(v) => *v, Value::Info(v) => Some(v), Value::Toggle(_) => None }
    }
}

/// Aparência → Início: the recent Steam games in the Home row. Off by default (owner's call).
pub const TOUR_AGAIN: &str = "Ver o tour de novo";
/// Sobre: the session menu's own tour, in a preview of the menu (1.7 "Tour do menu da sessão").
pub const GUIDE_TOUR_ROW: &str = "Tour do menu da sessão";
/// Aparência → Abertura (1.7 introStyle, introSound): the opening when the app starts.
pub const INTRO_STYLE_ROW: &str = "Estilo da abertura";
pub const INTRO_SOUND_ROW: &str = "Som da abertura";
pub const INTRO_PREVIEW_ROW: &str = "Ver a abertura";
pub const SHOW_STEAM_GAMES: &str = "Mostrar jogos da Steam na Início";

/// Atualizações: look for a newer alpha when the app opens (config `check_updates`).
/// Hide the desktop icons during the session (1.7.1 #210).
pub const HIDE_ICONS_ROW: &str = "Esconder os ícones da área de trabalho na sessão";
pub const CHECK_AT_START: &str = "Procurar atualizações ao abrir";

/// The list of choices open beside a row (PS5 style): `sel` is the choice under the focus.
/// `screen`: the list is a screen's role on the Telas map (`row` is then the screen's index).
pub struct Dropdown { pub section: usize, pub row: usize, pub sel: usize, pub screen: Option<String> }

/// Telas rows that the map of the screens stands in for.
const MAP_ROWS: [&str; 2] = ["Tela de jogo", "Outras telas"];
/// The box the screens are fitted into (design units).
const MAP_W: f32 = 1060.;
const MAP_H: f32 = 330.;

pub struct Row { pub title: &'static str, pub desc: &'static str, pub value: Value }
pub struct Section { pub title: &'static str, pub rows: Vec<Row> }
pub struct Category { pub icon: char, pub title: &'static str, pub desc: &'static str, pub sections: Vec<Section> }

fn r(title: &'static str, desc: &'static str, value: Value) -> Row { Row { title, desc, value } }
fn choice(o: &[&'static str]) -> Value { Value::Choice(o.to_vec(), 0) }

pub fn session() -> Vec<Section> {
    vec![
        Section { title: "Telas", rows: vec![
            r("Tela de jogo", "Onde o jogo aparece.", choice(&["TV da sala", "Monitor 27\"", "Notebook"])),
            r("Outras telas", "Monitor 27\" e notebook.", choice(&["Desligar na sessão", "Deixar ligadas"])),
            r("Confirmar a imagem ao iniciar", "Se você não confirmar, tudo volta sozinho.", Value::Toggle(true)),
        ]},
        Section { title: "Imagem", rows: vec![
            r("Resolução", "Aplicada ao iniciar, desfeita ao encerrar.", choice(&["3840 × 2160", "2560 × 1440", "1920 × 1080", "Não mudar"])),
            r("Taxa de atualização", "", choice(&["120 Hz", "60 Hz", "Não mudar"])),
            r("HDR", "Liga o HDR do Windows só na sessão.", Value::Toggle(true)),
            r("VRR", "Liga o ajuste do Windows de taxa de atualização variável só na sessão. Ative também no painel da NVIDIA/AMD.", Value::Toggle(false)),
        ]},
        Section { title: "Som", rows: vec![
            r("Saída de áudio", "", choice(&["TV (HDMI)", "Fone", "Não mudar"])),
            r("Voltar para a saída anterior no fim", "", Value::Toggle(true)),
        ]},
        Section { title: "Desempenho", rows: vec![
            r("Limite de FPS", "Via RTSS, quando instalado.", choice(&["60", "30", "120", "Sem limite"])),
            r("Mostrar FPS na tela", "", Value::Toggle(false)),
        ]},
        Section { title: "Ao encerrar", rows: vec![
            r(HIDE_ICONS_ROW, "Para a TV ficar limpa como num console. Se eles já estavam escondidos, continuam assim.", Value::Toggle(false)),
            r("Devolver os ícones da área de trabalho ao lugar", "", Value::Toggle(true)),
            r("Fechar o iniciador junto", "", Value::Toggle(true)),
        ]},
    ]
}

pub fn system() -> Vec<Category> {
    vec![
        Category { icon: glyph::SPORTS_ESPORTS, title: "Controles", desc: "Layout dos botões, vibração e o controle como mouse.", sections: vec![
            Section { title: "Controle", rows: vec![
                r("Layout dos botões", "PlayStation ou Xbox, conforme o controle ligado.", choice(&["Automático", "PlayStation", "Xbox"])),
                r("Vibrar ao navegar", "", Value::Toggle(true)),
                r(pads::AUTO_START_ROW, "Com o Console Mode na bandeja, ligar um controle traz o app para a frente.", Value::Toggle(false)),
                r(crate::wake::WAKE_ROW, "Deixa o adaptador sem fio Xbox, o Bluetooth ou o controle no cabo acordarem o PC da suspensão quando você aperta o botão central do controle. O Windows pede permissão de administrador. Não acorda o PC desligado ou em hibernação, e alguns PCs cortam a energia do Bluetooth ao suspender. Com “Entrar ao conectar o controle” ligado, o PC já acorda no modo console.", Value::Link(None)),
            ]},
            Section { title: "Mouse pelo controle", rows: vec![
                r(pads::MOUSE_ROW, "Fora das sessões e com o app em segundo plano: o analógico esquerdo move o ponteiro, A clica, B clica com o botão direito e o analógico direito rola.", Value::Toggle(false)),
                r(pads::SPEED_ROW, "Quão rápido o ponteiro anda.", choice(&["Média", "Rápida", "Lenta"])),
            ]},
        ]},
        Category { icon: glyph::GAMEPAD, title: "Atalhos", desc: "Combinações que funcionam mesmo com o Console Mode na bandeja.", sections: vec![
            Section { title: "Atalhos do controle", rows: vec![
                r(pads::HOME_ROW, "Segure por 1 s, com o Console Mode na bandeja ou atrás de outras janelas.", Value::Choice(pads::choices(Slot::Home), 0)),
                r(pads::MENU_ROW, "Em uma sessão, abre o menu por cima do jogo. Sem combinação, segure ☰.", Value::Choice(pads::choices(Slot::Menu), 0)),
                r(pads::EXIT_ROW, "Em uma sessão, segure por 1 s para devolver as telas e voltar ao PC.", Value::Choice(pads::choices(Slot::Exit), 0)),
                r(pads::TASK_VIEW_ROW, "Abre a Visão de tarefas do Windows, também durante um jogo.", Value::Choice(pads::choices(Slot::TaskView), 0)),
                r(pads::TASK_VIEW_PS_ROW, "Com um controle PlayStation, o direcional e o analógico escolhem a janela, A abre e B fecha. Desligue se o controle já aparece como Xbox (DS4Windows, Steam).", Value::Toggle(true)),
            ]},
        ]},
        Category { icon: glyph::VOLUME_UP, title: "Som", desc: "Sons de navegação e de início de sessão.", sections: vec![
            Section { title: "Sons da interface", rows: vec![
                r("Sons de navegação", "", Value::Toggle(true)),
                r("Estilo", "", choice(&["Console", "Suave"])),
                r("Volume", "", choice(&["60 %", "80 %", "100 %", "40 %", "20 %"])),
            ]},
        ]},
        Category { icon: glyph::PALETTE, title: "Aparência", desc: "Tema, fundo da Início e tamanho da interface na TV.", sections: vec![
            Section { title: "Tema", rows: vec![
                r("Tema", "", Value::Choice(cm_ui::THEMES.to_vec(), cm_ui::theme() as usize)),
            ]},
            Section { title: "Início", rows: vec![
                r(SHOW_STEAM_GAMES, "Os jogos instalados aparecem depois dos iniciadores, com a arte da sua biblioteca.", Value::Toggle(false)),
                r("Fundo da Início", "", choice(&["Arte do item em foco", "Só a cor do tema"])),
            ]},
            Section { title: "Abertura", rows: vec![
                r(INTRO_STYLE_ROW, "A do Console Mode é a oficial e, de vez em quando, o app abre de surpresa com uma inspirada em um console. Escolher um estilo aqui fixa esse estilo e acaba com as surpresas.", choice(&["Console Mode", "Inspirada no PS1", "Inspirada no PS2", "Inspirada no PS3", "Inspirada no PS4", "Inspirada no Xbox", "Inspirada no Xbox 360", "Inspirada no Xbox One", "Nenhuma (abre direto)"])),
                r(INTRO_SOUND_ROW, "Toca um som de entrada junto com a abertura.", Value::Toggle(true)),
                r(INTRO_PREVIEW_ROW, "Mostra agora a abertura escolhida, com o som.", Value::Link(None)),
            ]},
            Section { title: "Tamanho", rows: vec![
                r("Escala da interface", "", choice(&["100 %", "110 %", "125 %", "90 %"])),
            ]},
        ]},
        Category { icon: glyph::TV, title: "TV", desc: "Ligar a TV e trocar para a entrada do PC ao entrar no modo console.", sections: vec![
            Section { title: "Controle da TV", rows: vec![
                r(tv::PROVIDER_ROW, "Pela rede, sem instalar nada. Na TV: Configurações → Sistema → Opções do desenvolvedor → ative \"Depuração USB\" (ou \"Depuração pela rede\"). Na primeira vez, aceite \"Permitir depuração\" na TV.", Value::Choice(tv::PROVIDERS.map(|p| p.0).to_vec(), 0)),
                r(tv::HOST_ROW, "IP da TV na sua rede (Configurações → Rede). Dica: reserve esse IP no roteador para ele não mudar.", Value::Link(None)),
                r(tv::MAC_ROW, "Para acordar a TV em espera profunda (Wake-on-LAN). Deixe vazio se a TV responde mesmo desligada.", Value::Link(None)),
                r(tv::HDMI_ROW, "Porta HDMI da TV onde o PC está ligado.", choice(&tv::HDMI)),
                r(tv::INPUT_ROW, "Só se a TV ignorar a tecla HDMI: um comando de shell do Android que troca para a entrada do PC. Vazio = tecla HDMI escolhida acima.", Value::Link(None)),
                r(tv::TEST_ROW, "Liga a TV e troca para a entrada do PC agora, sem mexer nas telas. Na primeira vez, pareia com a TV.", Value::Link(None)),
            ]},
            Section { title: "Ao voltar ao PC", rows: vec![
                r(tv::STANDBY_ROW, "Depois que as telas da mesa voltam.", Value::Toggle(false)),
                r(tv::RESTORE_ROW, "Um comando de shell do Android enviado quando as telas da mesa voltam, antes da espera. Ex.: input keyevent 3 (tela inicial). Vazio = nenhum.", Value::Link(None)),
            ]},
        ]},
        Category { icon: glyph::POWER, title: "Inicialização", desc: "Como o Console Mode abre junto com o Windows.", sections: vec![
            Section { title: "Inicialização", rows: vec![
                r("Iniciar com o Windows", "", Value::Toggle(false)),
                r("Começar na bandeja", "Pronto para o atalho do controle.", Value::Toggle(false)),
                r("Registrar links consolemode2alpha", "Permite abrir e controlar esta alpha por links.", Value::Toggle(false)),
            ]},
            gaming_home_settings::section(),
        ]},
        Category { icon: glyph::SYSTEM_UPDATE, title: "Atualizações", desc: "Versão instalada e canal alpha da 2.0.", sections: vec![
            Section { title: "Software", rows: vec![
                r("Versão", "", Value::Link(Some(env!("CARGO_PKG_VERSION")))),
                r("Canal", "", choice(&["Alpha"])),
                r("Procurar atualização", "Só olha o GitHub quando você pede, e uma vez ao abrir o app se a opção abaixo estiver ligada.", Value::Link(None)),
                r(CHECK_AT_START, "Avisa se existir uma versão alpha mais nova. Não instala nada sozinho.", Value::Toggle(true)),
            ]},
        ]},
        Category { icon: glyph::TRANSLATE, title: "Idioma", desc: "Idioma da interface.", sections: vec![
            Section { title: "Idioma", rows: vec![
                r("Idioma", "", choice(&crate::i18n::LANGUAGES.map(|l| l.1))),
            ]},
        ]},
        Category { icon: glyph::INFO, title: "Sobre", desc: "Licenças, registros e relatar um problema.", sections: vec![
            Section { title: "Sobre", rows: vec![
                r("Relatar um problema", "", Value::Link(None)),
                r(TOUR_AGAIN, "", Value::Link(None)),
                r(GUIDE_TOUR_ROW, "Explica cada botão do menu que abre sobre o jogo. Abre o menu numa prévia, sem iniciar uma sessão.", Value::Link(None)),
                r("Abrir a pasta de registros", "", Value::Link(None)),
                r("Licenças", "", Value::Link(None)),
            ]},
        ]},
    ]
}

/// Where the controller is inside Sessão/Sistema.
#[derive(Default)]
pub struct Nav {
    focus_glide: Option<FocusGlide>,
    list_scroll: gpui::ScrollHandle,
    options_scroll: gpui::ScrollHandle,
    selection_dir: f32,
    /// Sistema: None = the category list; Some(i) = inside category i.
    pub category: Option<usize>,
    pub category_sel: usize,
    pub section: usize,
    pub row: usize,
    /// Focus is on the options (right pane) instead of the sections.
    pub right: bool,
}

struct FocusGlide {
    start: std::time::Instant,
    generation: usize,
    top: gpui::Pixels,
    bottom: gpui::Pixels,
}

// Keep settings content above the fixed controller hints.
const SETTINGS_BOTTOM: f32 = 120.;
const SETTINGS_EDGE_FADE: f32 = 28.;

impl Console {
    fn sections_mut(&mut self) -> Option<&mut Vec<Section>> {
        match (self.tab, self.nav.category) {
            (1, _) => Some(&mut self.session),
            (2, Some(c)) => Some(&mut self.system[c].sections),
            _ => None,
        }
    }

    pub(crate) fn settings_press(&mut self, p: Pad, cx: &mut Context<Self>) {
        // recording a combo: only B (the keyboard's; the pad's goes to the recording) does something
        if self.shortcut_capture.is_some() { if p == Pad::B { self.cancel_recording(cx); } return; }
        if self.tab == 2 && self.nav.category.is_none() {
            let sel = self.nav.category_sel;
            let n = self.system.len();
            match p {
                Pad::Up if sel > 0 => self.nav.category_sel -= 1,
                Pad::Down if sel + 1 < n => self.nav.category_sel += 1,
                Pad::A | Pad::Right => return self.open_category(self.nav.category_sel, cx),
                Pad::B => return self.back_home(cx),
                _ => return,
            }
            self.animate_settings_selection(sel, self.nav.category_sel);
            self.nav.focus_glide = focus_glide(&self.nav.list_scroll, sel, self.nav.category_sel, self.nav.focus_glide.as_ref());
            self.nav.list_scroll.scroll_to_item(self.nav.category_sel);
            self.sounds.play(Sound::Move);
            return cx.notify();
        }
        if self.dropdown.is_some() { return self.dropdown_press(p, cx); }
        if self.nav.right && self.map_on() { return self.map_press(p, cx); }
        let right = self.nav.right;
        let (section, row) = (self.nav.section, self.nav.row);
        let Some(sections) = self.sections_mut() else { return };
        let (n_sections, n_rows) = (sections.len(), sections[section].rows.len());
        let mut sound = Sound::Move;
        match (right, p) {
            (false, Pad::Up) if section > 0 => self.select_settings_section(section - 1),
            (false, Pad::Down) if section + 1 < n_sections => self.select_settings_section(section + 1),
            (false, Pad::A | Pad::Right) => { self.nav.right = true; self.nav.row = 0; sound = Sound::Confirm; }
            (false, Pad::B | Pad::Left) => {
                if self.tab == 2 { return self.system_root(cx); } else { return self.back_home(cx); }
            }
            (true, Pad::Up) if row > 0 => self.nav.row -= 1,
            (true, Pad::Down) if row + 1 < n_rows => self.nav.row += 1,
            (true, Pad::A) => { self.activate_row(section, row, cx); sound = Sound::Confirm; }
            (true, Pad::B | Pad::Left) => { self.nav.right = false; sound = Sound::Back; }
            _ => return,
        }
        if self.nav.right != right {
            self.nav.focus_glide = None;
        } else if right && self.nav.row != row {
            self.nav.focus_glide = focus_glide(&self.nav.options_scroll, 1 + row, 1 + self.nav.row, self.nav.focus_glide.as_ref());
        }
        if self.nav.right && (!right || self.nav.row != row) {
            self.reveal_settings_option();
        }
        self.sounds.play(sound);
        cx.notify();
    }

    fn reveal_settings_option(&self) {
        let item = if self.map_on() {
            let screens = self.hardware.screens.len();
            if self.nav.row < screens { 2 } else { 3 + self.nav.row - screens }
        } else {
            1 + self.nav.row
        };
        self.nav.options_scroll.scroll_to_item(item);
    }

    fn animate_settings_selection(&mut self, previous: usize, next: usize) {
        if previous == next { return; }
        self.settings_selection_anim += 1;
        self.nav.selection_dir = if next > previous { 1. } else { -1. };
    }

    fn select_settings_section(&mut self, section: usize) {
        self.animate_settings_selection(self.nav.section, section);
        if self.nav.right {
            self.nav.focus_glide = None;
        } else if self.nav.section != section {
            self.nav.focus_glide = focus_glide(&self.nav.list_scroll, self.nav.section, section, self.nav.focus_glide.as_ref());
        }
        self.nav.list_scroll.scroll_to_item(section);
        self.nav.options_scroll = gpui::ScrollHandle::new();
        self.nav.section = section;
        self.nav.row = 0;
        self.nav.right = false;
        self.dropdown = None;
    }

    fn settings_focus_row(&self, ui: &Ui, row: gpui::Stateful<Div>, focused: bool) -> gpui::Stateful<Div> {
        let Some(glide) = self.nav.focus_glide.as_ref().filter(|_| focused && !motion::reduced()) else {
            return focusable(ui, row, focused);
        };
        let (start, top, bottom) = (glide.start, glide.top, glide.bottom);
        let inset = ui.u(-3.);
        let highlight = div().absolute().left(ui.u(-3.)).right(ui.u(-3.))
            .rounded(ui.u(18.)).border(ui.u(3.)).border_color(color::white(0.95))
            .bg(ui.focus_fill(0.19, 0.11)).shadow(settings_focus_shadow(ui))
            .with_animation(("settings-focus-glide", glide.generation), motion::out(motion::FOCUS), move |el, _| {
                let remaining = glide_remaining(start);
                el.top(inset + top * remaining).bottom(inset - bottom * remaining)
            });
        row.relative().rounded(ui.u(18.)).border(ui.u(3.)).border_color(gpui::transparent_black())
            .cursor_pointer().child(highlight)
    }

    fn settings_selection_content(&self, ui: &Ui, id: &'static str, content: Div) -> gpui::AnyElement {
        if self.nav.selection_dir == 0. || motion::reduced() {
            return content.into_any_element();
        }
        let offset = ui.u(14. * self.nav.selection_dir);
        content.with_animation((id, self.settings_selection_anim), motion::out(motion::SMALL), move |el, t| {
            el.top(offset * (1. - t)).opacity(0.55 + 0.45 * t)
        }).into_any_element()
    }

    /// A row pressed: a list of choices opens beside it (PS5 style); a switch flips; a link opens.
    fn activate_row(&mut self, section: usize, row: usize, cx: &mut Context<Self>) {
        let Some(sections) = self.sections_mut() else { return };
        let r = &mut sections[section].rows[row];
        if !r.value.is_interactive() { return; }
        if let Value::Choice(_, i) = r.value { return self.open_dropdown(Dropdown { section, row, sel: i, screen: None }, cx); }
        r.value.activate();
        self.row_changed(section, row, cx);
    }

    // ───────────── the list of choices (dropdown) ─────────────

    fn open_dropdown(&mut self, d: Dropdown, cx: &mut Context<Self>) {
        let sel = d.sel;
        self.dropdown = Some(d);
        self.dropdown_anim += 1;
        self.dropdown_scroll.scroll_to_item(sel);
        cx.notify();
    }

    /// The open list's choices, which one is in use, and which can't be picked (a combo another
    /// shortcut has; the game screen can't just be turned off).
    pub(crate) fn dropdown_options(&self) -> Option<(Vec<&'static str>, usize, Vec<bool>)> {
        let d = self.dropdown.as_ref()?;
        if let Some(id) = &d.screen {
            let role = self.hardware.role(id, &self.config);
            let game = role == crate::hardware::Role::Game;
            return Some((crate::hardware::ROLES.to_vec(), role.index(), vec![false, game, game]));
        }
        let sections = match (self.tab, self.nav.category) { (1, _) => &self.session, (2, Some(c)) => &self.system[c].sections, _ => return None };
        let r = sections.get(d.section)?.rows.get(d.row)?;
        let Value::Choice(o, i) = &r.value else { return None };
        let masks = pads::current_masks(&self.config);
        let blocked = o.iter().enumerate().map(|(k, l)| k != *i && pads::blocked(r.title, l, &masks)).collect();
        Some((o.clone(), *i, blocked))
    }

    fn dropdown_press(&mut self, p: Pad, cx: &mut Context<Self>) {
        let Some((options, _, blocked)) = self.dropdown_options() else { self.dropdown = None; return cx.notify() };
        let Some(d) = self.dropdown.as_mut() else { return };
        let step = |from: usize, dir: isize| -> Option<usize> {
            let mut k = from as isize + dir;
            while k >= 0 && (k as usize) < options.len() { if !blocked[k as usize] { return Some(k as usize); } k += dir; }
            None
        };
        match p {
            Pad::Up | Pad::Down => {
                let Some(k) = step(d.sel, if p == Pad::Up { -1 } else { 1 }) else { return };
                d.sel = k;
                self.dropdown_scroll.scroll_to_item(k);
                self.sounds.play(Sound::Move);
            }
            Pad::A => { let k = d.sel; return self.pick(k, cx); }
            Pad::B | Pad::Left => { self.dropdown = None; self.sounds.play(Sound::Back); }
            _ => return,
        }
        cx.notify();
    }

    /// A choice picked in the open list.
    fn pick(&mut self, k: usize, cx: &mut Context<Self>) {
        let Some((_, _, blocked)) = self.dropdown_options() else { return };
        if blocked.get(k).copied().unwrap_or(true) { return; }
        let Some(d) = self.dropdown.take() else { return };
        self.sounds.play(Sound::Confirm);
        if let Some(id) = d.screen {
            let hw = self.hardware.clone();
            hw.set_role(&id, crate::hardware::Role::of_index(k), &mut self.config);
            hw.fill(&mut self.session, &self.config);
            self.map_anim += 1;
            self.map_changed = Some(id);
            self.save_config();
            return cx.notify();
        }
        let Some(sections) = self.sections_mut() else { return };
        // "Gravar combinação…": the row keeps its combo until a new one is recorded
        let row = &sections[d.section].rows[d.row];
        if let Value::Choice(o, _) = &row.value { if pads::is_record(o[k]) { let title = row.title; return self.start_recording(title, cx); } }
        if let Value::Choice(_, i) = &mut sections[d.section].rows[d.row].value { *i = k; }
        self.row_changed(d.section, d.row, cx);
        cx.notify();
    }

    // ───────────── Sessão → Telas: the map of the screens ─────────────

    /// The Telas section shows the screens as they sit on the desk (1.7), not as rows.
    pub(crate) fn map_on(&self) -> bool { self.tab == 1 && self.nav.section == 0 && !self.hardware.screens.is_empty() }

    /// Rows of Telas still shown as rows under the map (the screen choices live on the map).
    fn telas_rows(&self) -> Vec<usize> {
        self.session.first().map(|s| s.rows.iter().enumerate().filter(|(_, r)| !MAP_ROWS.contains(&r.title)).map(|(i, _)| i).collect()).unwrap_or_default()
    }

    fn map_tiles(&self) -> Vec<crate::hardware::Tile> { crate::hardware::layout(&self.hardware.screens, MAP_W, MAP_H) }

    /// From tile `from`, the nearest tile that way (dx, dy), if any.
    fn tile_toward(&self, from: usize, dx: f32, dy: f32) -> Option<usize> {
        let t = self.map_tiles();
        let c = |r: &crate::hardware::Tile| (r.x + r.w / 2., r.y + r.h / 2.);
        let (fx, fy) = c(&t[from]);
        t.iter().enumerate().filter(|(i, _)| *i != from).filter_map(|(i, r)| {
            let (x, y) = c(r);
            let (ax, ay) = (x - fx, y - fy);
            let along = ax * dx + ay * dy;
            if along <= 1. { return None; }
            let across = (ax * dy - ay * dx).abs();
            Some((i, along + across * 2.))
        }).min_by(|a, b| a.1.total_cmp(&b.1)).map(|(i, _)| i)
    }

    fn map_press(&mut self, p: Pad, cx: &mut Context<Self>) {
        let n = self.hardware.screens.len();
        let rows = self.telas_rows();
        let item = self.nav.row.min(n + rows.len() - 1);
        let mut sound = Sound::Move;
        if item < n {
            let to = match p {
                Pad::Left => self.tile_toward(item, -1., 0.),
                Pad::Right => self.tile_toward(item, 1., 0.),
                Pad::Up => self.tile_toward(item, 0., -1.),
                Pad::Down => self.tile_toward(item, 0., 1.).or(if rows.is_empty() { None } else { Some(n) }),
                Pad::A => {
                    let id = self.hardware.screens[item].id.clone();
                    let sel = self.hardware.role(&id, &self.config).index();
                    self.sounds.play(Sound::Confirm);
                    return self.open_dropdown(Dropdown { section: 0, row: item, sel, screen: Some(id) }, cx);
                }
                Pad::B => { self.nav.right = false; self.nav.focus_glide = None; self.sounds.play(Sound::Back); return cx.notify(); }
                _ => return,
            };
            match (to, p) {
                (Some(k), _) => self.nav.row = k,
                (None, Pad::Left) => { self.nav.right = false; sound = Sound::Back; }
                _ => return,
            }
        } else {
            match p {
                Pad::Up => {
                    // back up to the map: the game screen, or the first one
                    self.nav.row = if item > n { item - 1 } else { self.hardware.screens.iter().position(|s| self.hardware.role(&s.id, &self.config) == crate::hardware::Role::Game).unwrap_or(0) };
                }
                Pad::Down if item + 1 < n + rows.len() => self.nav.row = item + 1,
                Pad::A => { self.activate_row(0, rows[item - n], cx); sound = Sound::Confirm; }
                Pad::B | Pad::Left => { self.nav.right = false; sound = Sound::Back; }
                _ => return,
            }
        }
        if self.nav.right && self.nav.row != item {
            self.nav.focus_glide = if item >= n && self.nav.row >= n {
                focus_glide(&self.nav.options_scroll, 3 + item - n, 3 + self.nav.row - n, self.nav.focus_glide.as_ref())
            } else {
                None
            };
            self.reveal_settings_option();
        } else if !self.nav.right {
            self.nav.focus_glide = None;
        }
        self.sounds.play(sound);
        cx.notify();
    }

    /// A row's value changed (switch flipped, choice picked): save it and apply what depends on it.
    fn row_changed(&mut self, section: usize, row: usize, cx: &mut Context<Self>) {
        let Some(sections) = self.sections_mut() else { return };
        let r = &sections[section].rows[row];
        let (title, value) = (r.title, r.value.clone());
        if let Some(action) = gaming_home_settings::Action::for_row(title) { return self.change_gaming_home(action, cx); }
        if tv::TEXT_ROWS.contains(&title) || title == tv::TEST_ROW { let desc = r.desc; return self.tv_row_pressed(title, desc, cx); }
        if title == TOUR_AGAIN { self.nav = Nav::default(); return self.start_tour(cx); }
        if title == INTRO_PREVIEW_ROW { return self.preview_intro(cx); }
        if title == crate::wake::WAKE_ROW { return self.wake_row_pressed(cx); }
        if title == GUIDE_TOUR_ROW {
            // the menu opens (a preview outside a session) with its tour running
            self.nav = Nav::default();
            self.tab = 0;
            self.tour_on_open = true;
            return self.open_guide(cx);
        }
        match title {
            // gathers a diagnostic report, copies and saves it, and opens a new issue on GitHub (the owner triages there)
            "Relatar um problema" => return self.report_problem(cx),
            "Abrir a pasta de registros" => { cm_platform::launch::open(&self.paths.data_dir.to_string_lossy()); return; }
            "Licenças" => {
                // installed: next to the exe; development: the repository's copy
                let exe = std::env::current_exe().unwrap_or_default();
                let found = exe.ancestors().skip(1).take(4).flat_map(|d| [d.join("THIRD_PARTY_NOTICES.md"), d.join("docs/v2/THIRD_PARTY_NOTICES.md")]).find(|p| p.exists());
                if let Some(p) = found { cm_platform::launch::open(&p.to_string_lossy()); }
                return;
            }
            _ => {}
        }
        if title == crate::updates::CHECK_ROW { return self.update_row_pressed(cx); }
        // Aparência → Tema changes the accent everywhere right away
        if title == "Tema" { if let Value::Choice(_, i) = value { cm_ui::set_theme(i as u8); } }
        if title == SHOW_STEAM_GAMES { if let Value::Toggle(on) = value { self.show_steam_games(on); } }
        if self.hardware.store(title, &value, &mut self.config) {
            // the other hardware rows depend on this one (a screen's own modes)
            let hw = self.hardware.clone();
            hw.fill(&mut self.session, &self.config);
            self.save_config();
        } else if {
            // Registry changes happen only on explicit settings activation.
            let previous = self.config.clone();
            let bound = crate::bind::store(title, &value, &mut self.config);
            if bound {
                if let Err(e) = crate::shell::settings_change(title, &self.config) {
                    crate::startup_log(&format!("Inicialização: {e}"));
                    self.config = previous;
                    for category in &mut self.system { crate::bind::load(&mut category.sections, &self.config); }
                    self.toast = Some((tf("Não foi possível alterar a inicialização: {0}", &[&e]), std::time::Instant::now()));
                    return;
                }
            }
            bound
        } {
            // Idioma: the whole UI is drawn again in the new language, no restart
            if title == "Idioma" { crate::i18n::set_language(&self.config.app_language); }
            self.sounds.enabled = self.config.interface_sounds;
            self.sounds.style = crate::sound::Style::from_config(&self.config.sound_style);
            self.sounds.volume = self.config.sound_volume;
            // hear the change right away
            if matches!(title, "Estilo" | "Volume") { self.sounds.play(Sound::Confirm); }
            // turning the opening's sound on lets you hear it
            if title == INTRO_SOUND_ROW && self.config.intro_sound { self.sounds.play(Sound::boot_of(cm_core::boot_intro::normalize(self.config.intro_style.as_deref()))); }
            self.save_config();
        }
    }

    fn open_category(&mut self, i: usize, cx: &mut Context<Self>) {
        // the controller wake row reads the adapters the first time Controles opens
        if self.system[i].sections.iter().flat_map(|s| s.rows.iter()).any(|r| r.title == crate::wake::WAKE_ROW) { self.check_wake(cx); }
        if self.system[i].sections.iter().any(|s| s.title == gaming_home_settings::SECTION) {
            self.change_gaming_home(gaming_home_settings::Action::Refresh, cx);
        }
        self.nav.list_scroll = gpui::ScrollHandle::new();
        self.nav.options_scroll = gpui::ScrollHandle::new();
        self.nav.focus_glide = None;
        self.nav.category = Some(i);
        self.nav.selection_dir = 0.;
        self.nav.category_sel = i;
        self.nav.section = 0;
        self.nav.row = 0;
        self.nav.right = false;
        self.pane_anim += 1;
        self.pane_dir = 1.;
        self.sounds.play(Sound::Confirm);
        cx.notify();
    }

    fn back_home(&mut self, cx: &mut Context<Self>) {
        self.tab = 0;
        self.nav = Nav::default();
        self.sounds.play(Sound::Back);
        cx.notify();
    }

    // ───────────── views ─────────────

    pub(crate) fn settings_view(&self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let page = match (self.tab, self.nav.category) {
            (1, _) => self.two_pane(ui, cx, glyph::TUNE, None, t("Sessão"), &self.session),
            (2, None) => self.category_list(ui, cx),
            (2, Some(c)) => { let cat = &self.system[c]; self.two_pane(ui, cx, cat.icon, Some(t("Sistema")), t(cat.title), &cat.sections) }
            _ => div(),
        };
        let page = div().absolute().top_0().left_0().size_full().child(page).child(settings_footer_fade(ui));
        let (ui2, dir, d) = (*ui, self.pane_dir, motion::dist(24.));
        let page: gpui::AnyElement = if self.pane_anim == 0 { page.into_any_element() } else {
            page.with_animation(("pane", self.pane_anim), motion::out(motion::TAB), move |el, t| el.opacity(t).left(ui2.u(dir * d * (1. - t)))).into_any_element()
        };
        div().absolute().top_0().left_0().size_full().child(backdrop(ui)).child(page)
    }

    fn category_list(&self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let sel = self.nav.category_sel;
        let list = div().absolute().left(ui.u(crate::home::ROW_X)).top(ui.u(250.)).bottom(ui.u(SETTINGS_BOTTOM)).w(ui.u(820.))
            .id("settings-categories").pb(ui.u(SETTINGS_EDGE_FADE)).overflow_y_scroll().track_scroll(&self.nav.list_scroll).flex().flex_col().gap(ui.u(4.))
            .children(self.system.iter().enumerate().map(|(i, c)| {
                self.settings_focus_row(ui, div().id(("cat", i)).flex_shrink_0().flex().items_center().gap(ui.u(18.)).px(ui.u(26.)).py(ui.u(15.)).text_size(ui.u(28.)), i == sel)
                    .child(div().w(ui.u(52.)).child(ui.icon(c.icon, 34., color::accent())))
                    .child(div().flex_1().child(t(c.title)))
                    .child(ui.icon(glyph::CHEVRON_RIGHT, 30., color::dim()))
                    .on_click(cx.listener(move |this, _, _, cx| this.open_category(i, cx)))
            }));
        let c = &self.system[sel];
        let desc = div().relative().flex().flex_col()
            .child(div().size(ui.u(200.)).rounded(ui.u(44.)).flex().items_center().justify_center()
                .bg(linear_gradient(160., linear_color_stop(color::accent_alpha(0.30), 0.), linear_color_stop(color::accent_alpha(0.08), 1.)))
                .shadow(ui.raised(ui.lift_lg(), 0.2))
                .child(ui.icon(c.icon, 110., color::ink())))
            .child(div().mt(ui.u(34.)).mb(ui.u(14.)).text_size(ui.u(40.)).font_family(cm_ui::FONT).font_weight(FontWeight::SEMIBOLD).child(t(c.title)))
            .child(div().max_w(ui.u(640.)).text_size(ui.u(25.)).line_height(ui.u(36.)).text_color(color::mute()).child(t(c.desc)));
        let preview = div().absolute().left(ui.u(1040.)).right(ui.u(110.)).top(ui.u(280.)).bottom(ui.u(SETTINGS_BOTTOM)).overflow_hidden()
            .child(self.settings_selection_content(ui, "category-preview", desc));
        div().size_full().child(title(ui, glyph::SETTINGS, None, t("Sistema"))).child(list).child(preview)
    }

    /// Sistema: out of a category, back to the list (B, the "Sistema ›" crumb, or the Sistema tab again).
    pub(crate) fn system_root(&mut self, cx: &mut Context<Self>) {
        if self.nav.category.is_none() { return; }
        self.nav.list_scroll = gpui::ScrollHandle::new();
        self.nav.list_scroll.scroll_to_item(self.nav.category_sel);
        self.nav.focus_glide = None;
        self.nav.category = None;
        self.nav.selection_dir = 0.;
        self.nav.right = false;
        self.dropdown = None;
        self.pane_anim += 1;
        self.pane_dir = -1.;
        self.sounds.play(Sound::Back);
        cx.notify();
    }

    fn two_pane(&self, ui: &Ui, cx: &mut Context<Self>, icon: char, crumb: Option<&str>, name: &str, sections: &[Section]) -> Div {
        let (sel, row_sel, right) = (self.nav.section.min(sections.len() - 1), self.nav.row, self.nav.right);
        let left = div().absolute().left(ui.u(crate::home::ROW_X)).top(ui.u(250.)).bottom(ui.u(SETTINGS_BOTTOM)).w(ui.u(470.))
            .id("settings-sections").pb(ui.u(SETTINGS_EDGE_FADE)).overflow_y_scroll().track_scroll(&self.nav.list_scroll).flex().flex_col().gap(ui.u(6.))
            .children(sections.iter().enumerate().map(|(i, s)| {
                let mut d = self.settings_focus_row(ui, div().id(("section", i)).flex_shrink_0().px(ui.u(26.)).py(ui.u(18.)).text_size(ui.u(27.)), !right && i == sel)
                    .on_click(cx.listener(move |this, _, _, cx| {
                        this.select_settings_section(i);
                        this.sounds.play(Sound::Move);
                        cx.notify();
                    }))
                    .child(t(s.title));
                d = if i == sel { d.text_color(color::ink()).when(right, |d| d.bg(color::white(0.06))) } else { d.text_color(color::mute()) };
                d
            }));
        let sec = &sections[sel];
        let map = self.map_on() && sel == 0;
        let mut rows = div().id("settings-options").pb(ui.u(SETTINGS_EDGE_FADE)).size_full()
            .overflow_y_scroll().track_scroll(&self.nav.options_scroll).flex().flex_col().gap(ui.u(4.))
            .child(div().text_size(ui.u(18.)).font_weight(FontWeight::SEMIBOLD).text_color(color::mute()).mt(ui.u(6.)).mb(ui.u(12.)).ml(ui.u(26.))
                .child(SharedString::from(t(sec.title).to_uppercase())));
        // the list of choices of the focused row (or screen), if open
        let mut dropdown = self.dropdown_view(ui, cx);
        let dd_at = self.dropdown.as_ref().filter(|d| d.section == sel).map(|d| (d.row, d.screen.is_some()));
        // Telas: the map of the screens, then the rows it doesn't stand in for (nav: tiles first)
        let shown_rows: Vec<usize> = if map { self.telas_rows() } else { (0..sec.rows.len()).collect() };
        let offset = if map { self.hardware.screens.len() } else { 0 };
        if map {
            rows = rows.child(div().ml(ui.u(26.)).mb(ui.u(18.)).text_size(ui.u(21.)).text_color(color::mute()).child(t("Escolha o que cada tela faz durante a sessão.")));
            let tile_dd = if dd_at.is_some_and(|(_, s)| s) { dropdown.take() } else { None };
            rows = rows.child(self.screen_map(ui, cx, right.then_some(row_sel), dd_at.map(|(r, _)| r), tile_dd));
        }
        for (k, &i) in shown_rows.iter().enumerate() {
            let r = &sec.rows[i];
            let mut text = div().flex().flex_col().flex_1().min_w_0().child(div().text_size(ui.u(27.)).child(t(r.title)));
            if !r.desc.is_empty() { text = text.child(div().text_size(ui.u(19.)).text_color(color::mute()).mt(ui.u(4.)).child(t(r.desc))); }
            let open = dd_at == Some((i, false));
            let value: gpui::AnyElement = match &r.value {
                Value::Toggle(on) => ui.toggle(*on).into_any_element(),
                v => {
                    let mut d = div().flex().items_center().gap(ui.u(6.)).text_size(ui.u(24.)).whitespace_nowrap()
                        .when(matches!(v, Value::Info(_)), |d| d.text_color(color::mute()));
                    if let Some(text) = v.text() { d = d.child(SharedString::from(shown(text).into_owned())); }
                    match v {
                        Value::Link(_) => d = d.child(ui.icon(glyph::CHEVRON_RIGHT, 26., color::mute())),
                        // a list opens from here
                        Value::Choice(_, _) => d = d.child(ui.icon(if open { glyph::EXPAND_LESS } else { glyph::EXPAND_MORE }, 28., color::mute())),
                        _ => {}
                    }
                    d.into_any_element()
                }
            };
            let item = k + offset;
            let mut el = self.settings_focus_row(ui, div().id(("row", i)).relative().flex().items_center().gap(ui.u(24.)).min_h(ui.u(88.)).px(ui.u(26.)).py(ui.u(16.)), right && item == row_sel)
                .child(text).child(value)
                .on_click(cx.listener(move |this, _, _, cx| {
                    this.nav.focus_glide = None;
                    this.nav.right = true; this.nav.row = item;
                    let s = this.nav.section;
                    this.activate_row(s, i, cx);
                    this.sounds.play(Sound::Confirm);
                    cx.notify();
                }));
            if open { if let Some(d) = dropdown.take() { el = el.child(d); } }
            rows = rows.child(el);
        }
        let pane = div().absolute().left(ui.u(640.)).right(ui.u(110.)).top(ui.u(250.)).bottom(ui.u(SETTINGS_BOTTOM)).overflow_hidden()
            .border_l(ui.u(1.)).border_color(color::white(0.1)).pl(ui.u(46.))
            .child(self.settings_selection_content(ui, "section-content", div().relative().size_full().child(rows)));
        // the crumb ("Sistema ›") is a link back to the list
        let crumb = crumb.map(|c| div().id("crumb").cursor_pointer().text_color(color::mute()).font_weight(FontWeight::NORMAL).rounded(ui.u(10.)).px(ui.u(6.)).mx(ui.u(-6.))
            .hover(|s| s.text_color(color::ink()).bg(color::white(0.08)))
            .on_click(cx.listener(|this, _, _, cx| this.system_root(cx)))
            .child(SharedString::from(format!("{c}  ›"))).into_any_element());
        div().size_full().child(title(ui, icon, crumb, name)).child(left).child(pane)
    }

    /// The open list of choices, hung from the bottom right corner of its row (or screen): it
    /// opens downwards, or upwards when there is no room, and never leaves the window.
    fn dropdown_view(&self, ui: &Ui, cx: &mut Context<Self>) -> Option<gpui::AnyElement> {
        let (options, current, blocked) = self.dropdown_options()?;
        let sel = self.dropdown.as_ref()?.sel;
        let items = options.iter().enumerate().map(|(k, o)| {
            let (focused, chosen, off) = (k == sel, k == current, blocked[k]);
            let ink = if focused { color::button_ink() } else if off { color::dim() } else { color::ink() };
            let mut it = div().id(("choice", k)).flex().items_center().gap(ui.u(12.)).px(ui.u(18.)).py(ui.u(13.)).rounded(ui.u(10.))
                .text_size(ui.u(24.)).whitespace_nowrap().text_color(ink)
                .child(div().w(ui.u(30.)).flex().items_center().children(chosen.then(|| ui.icon(glyph::CHECK, 26., if focused { color::button_ink() } else { color::accent() }))))
                .child(SharedString::from(shown(o).into_owned()));
            if focused { it = it.bg(ui.focus_fill(1., 0.88)).shadow(ui.lift_sm()); } else if !off { it = it.hover(|s| s.bg(color::white(0.08))).cursor_pointer(); }
            it.on_click(cx.listener(move |this, _, _, cx| this.pick(k, cx)))
        }).collect::<Vec<_>>();
        let margin = 16.;
        let height = (ui.design.height - SETTINGS_BOTTOM - margin).clamp(0., 560.);
        let menu = div().id("dropdown").occlude().min_w(ui.u(340.)).max_w(ui.u(720.)).max_h(ui.u(height)).overflow_y_scroll().track_scroll(&self.dropdown_scroll)
            .p(ui.u(8.)).rounded(ui.u(16.)).bg(color::panel_solid()).border(ui.u(1.)).border_color(color::white(0.14)).shadow(ui.raised(ui.lift_lg(), 0.14))
            .flex().flex_col().gap(ui.u(2.))
            .on_mouse_down_out(cx.listener(|this, _, _, cx| { this.dropdown = None; cx.notify(); }))
            .children(items);
        let (ui2, d) = (*ui, motion::dist(10.));
        let menu = menu.with_animation(("dropdown", self.dropdown_anim), motion::out(motion::MENU_IN), move |el, t| el.opacity(t).mt(ui2.u(-d * (1. - t))));
        Some(div().absolute().right(ui.u(8.)).bottom(ui.u(-4.)).size_0()
            .child(gpui::deferred(gpui::anchored().anchor(gpui::Anchor::TopRight).snap_to_window_with_margin(gpui::Edges { top: ui.u(margin), right: ui.u(margin), bottom: ui.u(SETTINGS_BOTTOM), left: ui.u(margin) }).child(menu)).with_priority(2))
            .into_any_element())
    }

    /// The screens where they sit on the desk, at their real proportions, each lit by what the
    /// session does to it: the game screen in the accent, a screen to turn off dimmed and dashed.
    fn screen_map(&self, ui: &Ui, cx: &mut Context<Self>, focus: Option<usize>, dd_tile: Option<usize>, mut dropdown: Option<gpui::AnyElement>) -> gpui::AnyElement {
        use crate::hardware::Role;
        let tiles = self.map_tiles();
        let (w, h) = tiles.iter().fold((0f32, 0f32), |(w, h), r| (w.max(r.x + r.w), h.max(r.y + r.h)));
        const GAP: f32 = 7.;
        let mut map = div().relative().flex_shrink_0().ml(ui.u(26.)).mb(ui.u(22.)).w(ui.u(w)).h(ui.u(h));
        for (i, (s, r)) in self.hardware.screens.iter().zip(tiles.iter()).enumerate() {
            let role = self.hardware.role(&s.id, &self.config);
            let focused = focus == Some(i);
            // small and standing screens shrink their content
            let k = ((r.w - 2. * GAP) / 300.).min((r.h - 2. * GAP) / 190.).clamp(0.45, 1.);
            let (bg, border, icon, icon_bg, icon_ink, pill_bg, pill_ink) = match role {
                Role::Game => (color::accent_alpha(0.22), color::accent(), glyph::SPORTS_ESPORTS, color::accent(), color::button_ink(), color::accent(), color::button_ink()),
                Role::Off => (color::white(0.02), color::white(0.26), glyph::DESKTOP_OFF, color::white(0.06), color::white(0.55), color::white(0.08), color::mute()),
                Role::Keep => (color::white(0.07), color::white(0.14), glyph::DESKTOP, color::white(0.12), color::white(0.9), color::white(0.14), color::ink()),
            };
            let details = if s.active {
                SharedString::from(format!("{} × {}{}", s.size.0, s.size.1, s.hz.map(|z| format!(" · {z} Hz")).unwrap_or_default()))
            } else { SharedString::from(t("Desligada no Windows")) };
            let content = div().flex().flex_col().items_center().gap(ui.u(7. * k)).px(ui.u(10.))
                .child(div().size(ui.u(52. * k)).rounded_full().bg(icon_bg).flex().items_center().justify_center().child(ui.icon(icon, 28. * k, icon_ink)))
                .child(div().text_size(ui.u(23. * k)).font_weight(FontWeight::SEMIBOLD).text_color(if role == Role::Off { color::mute() } else { color::ink() }).max_w_full().truncate().child(s.name()))
                .child(div().px(ui.u(12. * k)).py(ui.u(3. * k)).rounded_full().bg(pill_bg).text_color(pill_ink).text_size(ui.u(16. * k)).font_weight(FontWeight::SEMIBOLD).child(t(role.label())))
                .child(div().text_size(ui.u(16. * k)).text_color(color::mute()).child(details));
            let mut tile = div().id(("screen", i)).absolute().left(ui.u(r.x + GAP)).top(ui.u(r.y + GAP)).w(ui.u(r.w - 2. * GAP)).h(ui.u(r.h - 2. * GAP))
                .rounded(ui.u(14.)).bg(bg).border(ui.u(2.)).border_color(border)
                .when(role != Role::Off, |d| d.shadow(ui.raised(ui.lift_md(), 0.14))).flex().items_center().justify_center().overflow_hidden().cursor_pointer()
                .child(content)
                .on_click(cx.listener(move |this, _, _, cx| {
                    this.nav.right = true; this.nav.row = i;
                    let id = this.hardware.screens[i].id.clone();
                    let sel = this.hardware.role(&id, &this.config).index();
                    this.sounds.play(Sound::Confirm);
                    this.open_dropdown(Dropdown { section: 0, row: i, sel, screen: Some(id) }, cx);
                }));
            if role == Role::Off { tile = tile.border_dashed(); }
            if !focused { tile = tile.hover(|st| st.bg(color::white(0.1))); }
            // the screen just changed: it settles into its new look
            let changed = self.map_changed.as_deref().is_some_and(|c| c.eq_ignore_ascii_case(&s.id));
            let tile: gpui::AnyElement = if changed {
                tile.with_animation(("tile", self.map_anim), motion::out(motion::FOCUS), |el, t| el.opacity(0.4 + 0.6 * t)).into_any_element()
            } else { tile.into_any_element() };
            map = map.child(tile);
            if focused {
                map = map.child(div().absolute().left(ui.u(r.x + GAP - 7.)).top(ui.u(r.y + GAP - 7.)).w(ui.u(r.w - 2. * GAP + 14.)).h(ui.u(r.h - 2. * GAP + 14.))
                    .rounded(ui.u(20.)).border(ui.u(4.)).border_color(color::focus()));
            }
            if dd_tile == Some(i) {
                if let Some(d) = dropdown.take() {
                    map = map.child(div().absolute().left(ui.u(r.x + GAP)).top(ui.u(r.y + GAP)).w(ui.u(r.w - 2. * GAP)).h(ui.u(r.h - 2. * GAP)).child(d));
                }
            }
        }
        map.into_any_element()
    }

    pub(crate) fn settings_hints(&self) -> (Vec<(Pad, Glyph, &'static str)>, Vec<(Pad, Glyph, &'static str)>) {
        if self.shortcut_capture.is_some() { return (vec![(Pad::B, Glyph::Text("B"), t("Cancelar"))], vec![]); }
        if self.dropdown.is_some() { return (vec![(Pad::B, Glyph::Text("B"), t("Voltar"))], vec![(Pad::A, Glyph::Text("A"), t("Selecionar"))]); }
        let a = if self.tab == 2 && self.nav.category.is_none() { t("Abrir") } else if self.nav.right { t("Mudar") } else { t("Entrar") };
        (vec![(Pad::B, Glyph::Text("B"), t("Voltar"))], vec![(Pad::A, Glyph::Text("A"), a)])
    }
}

fn glide_remaining(start: std::time::Instant) -> f32 {
    1. - motion::ease_out((start.elapsed().as_secs_f32() * 1000. / motion::FOCUS as f32).min(1.))
}

fn focus_glide(scroll: &gpui::ScrollHandle, from: usize, to: usize, active: Option<&FocusGlide>) -> Option<FocusGlide> {
    if motion::reduced() { return None; }
    let previous = scroll.bounds_for_item(from)?;
    let next = scroll.bounds_for_item(to)?;
    let (top, bottom, generation) = active.map_or((gpui::px(0.), gpui::px(0.), 0), |g| {
        let remaining = glide_remaining(g.start);
        (g.top * remaining, g.bottom * remaining, g.generation + 1)
    });
    Some(FocusGlide {
        start: std::time::Instant::now(),
        generation,
        top: previous.top() - next.top() + top,
        bottom: previous.bottom() - next.bottom() + bottom,
    })
}

/// Settings rows (PS5 style): rounded, glass fill, a white ring and a soft glow when focused, a
/// hint of fill on hover.
pub(crate) fn focusable(ui: &Ui, d: gpui::Stateful<Div>, focused: bool) -> gpui::Stateful<Div> {
    let d = d.rounded(ui.u(18.)).border(ui.u(3.)).cursor_pointer();
    if focused {
        d.bg(ui.focus_fill(0.19, 0.11)).border_color(color::white(0.95)).shadow(settings_focus_shadow(ui))
    } else { d.border_color(gpui::transparent_black()).hover(|s| s.bg(color::white(0.07))) }
}

fn settings_focus_shadow(ui: &Ui) -> Vec<gpui::BoxShadow> {
    let mut shadow = ui.raised(ui.lift_md(), 0.2);
    shadow.push(ui.glow(color::white(0.22), 22.));
    shadow
}

fn title(ui: &Ui, icon: char, crumb: Option<gpui::AnyElement>, name: &str) -> Div {
    let mut t = div().absolute().left(ui.u(crate::home::ROW_X)).top(ui.u(150.)).flex().items_center().gap(ui.u(18.)).text_size(ui.u(44.)).font_family(cm_ui::FONT).font_weight(FontWeight::SEMIBOLD)
        .child(ui.icon(icon, 46., color::accent()));
    if let Some(c) = crumb { t = t.child(c); }
    t.child(SharedString::from(name.to_string()))
}

/// Feather both sides of the content cutoff into the fixed footer.
fn settings_footer_fade(ui: &Ui) -> Div {
    let clear = gpui::Hsla::from(gpui::rgba(0x0e121900));
    let edge = gpui::Hsla::from(gpui::rgb(0x0e1219));
    div().absolute().left_0().right_0().bottom_0().flex().flex_col()
        .child(div().h(ui.u(SETTINGS_EDGE_FADE)).bg(linear_gradient(180.,
            linear_color_stop(clear, 0.), linear_color_stop(edge, 1.))))
        .child(div().h(ui.u(SETTINGS_BOTTOM)).bg(linear_gradient(180.,
            linear_color_stop(edge, 0.), linear_color_stop(clear, 1.))))
}

/// Near-black lit by two soft pools of the theme accent (top left, and fainter at the bottom
/// right), with the corners falling off into the dark.
fn backdrop(ui: &Ui) -> Div {
    div().absolute().top_0().left_0().size_full().overflow_hidden()
        .bg(linear_gradient(180., linear_color_stop(gpui::Hsla::from(gpui::rgb(0x161b25)), 0.), linear_color_stop(gpui::Hsla::from(gpui::rgb(0x0a0d13)), 1.)))
        .child(ui.light(240., 80., 1200., color::accent_alpha(0.34)))
        .child(ui.light(1780., 1080., 1000., color::accent_alpha(0.14)))
        .child(ui.vignette())
}
