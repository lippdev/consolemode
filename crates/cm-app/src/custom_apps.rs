//! Custom-app UI kept separate from the shared Home and overlay renderers.
use std::sync::Arc;
use cm_core::{config::Config, custom_apps::{self as core, CustomAppEntry}};
use cm_ui::{Ui, color};
use gpui::{prelude::*, Context, Div, KeyDownEvent, SharedString, div};
use crate::{home::{Brand, Console, Item, Launcher, View}, i18n::{n, t, tf}, input::Pad, sound::Sound};

pub struct Editor {
    id: String, watch: bool, text: String, key: usize,
    /// A Sistema → TV row (title, description) instead of a custom app's field.
    tv: Option<(&'static str, &'static str)>,
}

impl Editor {
    /// The same keyboard for a text row of Sistema → TV (tv.rs).
    pub fn for_tv(title: &'static str, desc: &'static str, text: String) -> Self { Editor { id: String::new(), watch: false, text, key: 0, tv: Some((title, desc)) } }
}
const KEYS: &str = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .-_/:\\\"=,+*?";

pub fn is_custom(item: &Item) -> bool { matches!(item, Item::Launcher(l) if l.custom_id.is_some()) }

fn card(app: &CustomAppEntry) -> Item {
    let path = core::clean_path(&app.path);
    Item::Launcher(Launcher {
        kind: app.launcher(), custom_id: Some(app.id.clone()),
        name: core::display_name(path, &cm_platform::custom_apps::file_description(path)).into(),
        note: n("app próprio"), sub: n("Abre seu aplicativo. Ao fechar, suas telas voltam sozinhas."),
        icon: cm_platform::exe_icon(path, 256).map(Arc::new), brand: Brand::NEUTRAL, mark: None,
    })
}

pub fn insert_cards(items: &mut Vec<Item>, config: &Config) {
    let at = items.iter().position(|i| matches!(i, Item::Add)).unwrap_or(items.len());
    items.splice(at..at, core::entries(config).iter().map(card));
}

impl Console {
    pub(crate) fn custom_entry(&self) -> Option<&CustomAppEntry> {
        let Item::Launcher(l) = &self.items[self.focus] else { return None };
        self.config.custom_apps.iter().find(|a| Some(&a.id) == l.custom_id.as_ref())
    }

    fn commit_custom(&mut self, mut config: Config, cx: &mut Context<Self>) -> bool {
        core::sync_selected(&mut config);
        match cm_core::config::save(&self.paths, &config) {
            Ok(()) => { self.config = config; true }
            Err(e) => { self.show_toast(&tf("Não deu para salvar os apps: {0}", &[&e]), cx); false }
        }
    }

    pub(crate) fn add_custom_app(&mut self, cx: &mut Context<Self>) {
        if self.picking_app || self.running.is_some() { return; }
        self.picking_app = true;
        let owner = self.window.and_then(|w| w.update(cx, |_, w, _| cm_platform::custom_apps::owner_handle(w)).ok()).unwrap_or(0);
        cx.spawn(async move |this, cx| {
            let result = cx.background_executor().spawn(async move {
                std::thread::spawn(move || cm_platform::custom_apps::pick_exe(owner)).join().unwrap_or_else(|_| Err(t("O seletor de arquivos foi interrompido").into()))
            }).await;
            let _ = this.update(cx, |c, cx| {
                c.picking_app = false;
                match result {
                    Ok(Some(path)) => {
                        if let Err(p) = core::validate(&path) { c.show_toast(problem(p), cx); return; }
                        let mut config = c.config.clone();
                        config.custom_apps = core::entries(&config);
                        let app = CustomAppEntry { id: core::next_id(config.custom_apps.iter().map(|a| a.id.as_str())), path: core::clean_path(&path).into(), ..Default::default() };
                        config.custom_app_id = app.id.clone();
                        config.custom_apps.push(app.clone());
                        if c.commit_custom(config, cx) {
                            let at = c.items.iter().position(|i| matches!(i, Item::Add)).unwrap_or(c.items.len());
                            c.items.insert(at, card(&app));
                            c.focus = at; c.prev_focus = at; c.fade += 1;
                            c.view = View::Home;
                            c.set_scroll(c.row_shift(at, at), cx);
                            c.sounds.play(Sound::Confirm);
                        }
                    }
                    Ok(None) => c.sounds.play(Sound::Back),
                    Err(e) => c.show_toast(&tf("Não deu para abrir o seletor: {0}", &[&e]), cx),
                }
                cx.notify();
            });
        }).detach();
    }

    pub(crate) fn validate_custom_launch(&mut self, cx: &mut Context<Self>) -> bool {
        let Some(app) = self.custom_entry().cloned() else { return true };
        if let Err(p) = core::validate(&app.path) { self.sounds.play(Sound::Back); self.show_toast(problem(p), cx); return false; }
        let mut config = self.config.clone(); config.custom_app_id = app.id; config.fullscreen_mode = "customApp".into();
        self.commit_custom(config, cx)
    }

    pub(crate) fn pick_playnite_exe(&mut self, cx: &mut Context<Self>) {
        if self.picking_app || self.running.is_some() { return; }
        self.picking_app = true;
        let owner = self.window.and_then(|w| w.update(cx, |_, w, _| cm_platform::custom_apps::owner_handle(w)).ok()).unwrap_or(0);
        cx.spawn(async move |this, cx| {
            let result = cx.background_executor().spawn(async move {
                std::thread::spawn(move || cm_platform::custom_apps::pick_exe(owner)).join().unwrap_or_else(|_| Err(t("O seletor de arquivos foi interrompido").into()))
            }).await;
            let _ = this.update(cx, |c, cx| {
                c.picking_app = false;
                match result {
                    Ok(Some(path)) => {
                        if let Err(p) = core::validate(&path) { c.show_toast(problem(p), cx); return; }
                        c.config.playnite_path = core::clean_path(&path).into();
                        c.save_config();
                        c.refresh_playnite_icon();
                        c.sounds.play(Sound::Confirm);
                    }
                    Ok(None) => c.sounds.play(Sound::Back),
                    Err(e) => c.show_toast(&tf("Não deu para abrir o seletor: {0}", &[&e]), cx),
                }
                cx.notify();
            });
        }).detach();
    }

    pub(crate) fn refresh_playnite_icon(&mut self) {
        let icon = cm_platform::launch::playnite_exe(&self.config.playnite_path).and_then(|p| cm_platform::exe_icon(&p, 256)).map(Arc::new);
        if let Some(Item::Launcher(l)) = self.items.iter_mut().find(|i| matches!(i, Item::Launcher(l) if l.kind == cm_core::session::Launcher::Playnite)) {
            if icon.is_some() { l.icon = icon.clone(); l.mark = icon; }
        }
    }

    pub(crate) fn custom_menu_activate(&mut self, sel: usize, cx: &mut Context<Self>) {
        let Some(app) = self.custom_entry().cloned() else { return };
        match sel {
            0 => return self.start_launch(cx),
            4 | 5 => self.custom_editor = Some(Editor { id: app.id, watch: sel == 5, text: if sel == 5 { app.watch_process } else { app.arguments }, key: 0, tv: None }),
            6 => self.view = View::Moving,
            7 => {
                let mut config = self.config.clone();
                config.custom_apps.retain(|a| a.id != app.id);
                if !self.commit_custom(config, cx) { return; }
                self.items.remove(self.focus);
                self.focus = self.focus.saturating_sub(1); self.prev_focus = self.focus;
                self.view = View::Home; self.exiting = None; self.fade += 1;
                self.set_scroll(self.row_shift(self.focus, self.focus), cx);
                self.sounds.play(Sound::Back);
                return;
            }
            _ => return,
        }
        self.sounds.play(Sound::Confirm);
    }

    pub(crate) fn store_custom_order(&mut self) {
        let old = self.config.custom_apps.clone();
        self.config.custom_apps = self.items.iter().filter_map(|item| {
            let Item::Launcher(l) = item else { return None };
            old.iter().find(|app| Some(&app.id) == l.custom_id.as_ref()).cloned()
        }).collect();
    }

    fn finish_custom_editor(&mut self, save: bool, cx: &mut Context<Self>) {
        let tv_row = self.custom_editor.as_ref().and_then(|e| e.tv.map(|(title, _)| (title, e.text.clone())));
        if let Some((title, text)) = tv_row.filter(|_| save) {
            self.tv_text_saved(title, &text);
        } else if save {
            let Some(edit) = &self.custom_editor else { return };
            let mut config = self.config.clone();
            if let Some(app) = config.custom_apps.iter_mut().find(|a| a.id == edit.id) {
                if edit.watch { app.watch_process = edit.text.clone(); } else { app.arguments = edit.text.clone(); }
            }
            if !self.commit_custom(config, cx) { return; }
            if let Some(app) = self.custom_entry() {
                let launcher = app.launcher();
                if let Item::Launcher(l) = &mut self.items[self.focus] { l.kind = launcher; }
            }
        }
        self.custom_editor = None;
        self.sounds.play(if save { Sound::Confirm } else { Sound::Back });
        cx.notify();
    }

    pub(crate) fn custom_key(&mut self, e: &KeyDownEvent, cx: &mut Context<Self>) -> bool {
        let Some(edit) = &mut self.custom_editor else { return false };
        match e.keystroke.key.as_str() {
            "escape" => self.finish_custom_editor(false, cx),
            "enter" => self.finish_custom_editor(true, cx),
            "backspace" => { edit.text.pop(); }
            "v" if e.keystroke.modifiers.control => { if let Some(text) = cx.read_from_clipboard().and_then(|item| item.text()) { edit.text.extend(text.chars().filter(|c| !c.is_control())); } }
            _ => if let Some(ch) = &e.keystroke.key_char { if !e.keystroke.modifiers.control && !e.keystroke.modifiers.alt { edit.text.push_str(ch); } },
        }
        cx.notify(); true
    }

    pub(crate) fn custom_press(&mut self, p: Pad, cx: &mut Context<Self>) -> bool {
        if self.picking_app { return true; }
        let Some(edit) = &mut self.custom_editor else { return false };
        let n = KEYS.chars().count() + 3;
        match p {
            Pad::Left => edit.key = edit.key.saturating_sub(1),
            Pad::Right => edit.key = (edit.key + 1).min(n - 1),
            Pad::Up => edit.key = edit.key.saturating_sub(10),
            Pad::Down => edit.key = (edit.key + 10).min(n - 1),
            Pad::B => self.finish_custom_editor(false, cx),
            Pad::A => { let key = edit.key; self.editor_button(key, cx); }
            _ => {}
        }
        cx.notify(); true
    }

    fn editor_button(&mut self, key: usize, cx: &mut Context<Self>) {
        let Some(edit) = &mut self.custom_editor else { return };
        let n = KEYS.chars().count();
        if let Some(ch) = KEYS.chars().nth(key) { edit.text.push(ch); }
        else if key == n { edit.text.pop(); }
        else { self.finish_custom_editor(key == n + 1, cx); }
        cx.notify();
    }

    pub(crate) fn custom_editor_view(&self, ui: &Ui, cx: &mut Context<Self>) -> Div {
        let edit = self.custom_editor.as_ref().unwrap();
        let mut labels: Vec<String> = KEYS.chars().map(|c| if c == ' ' { t("Espaço").into() } else { c.to_string() }).collect();
        labels.extend([t("Apagar").into(), t("Salvar").into(), t("Cancelar").into()]);
        let mut grid = div().flex().flex_col().gap(ui.u(8.));
        for (row, keys) in labels.chunks(10).enumerate() {
            grid = grid.child(div().flex().gap(ui.u(8.)).children(keys.iter().enumerate().map(|(col, label)| {
                let i = row * 10 + col;
                div().id(("custom-key", i)).min_w(ui.u(78.)).px(ui.u(6.)).py(ui.u(10.)).text_size(ui.u(18.)).text_center().rounded(ui.u(8.))
                    .bg(if i == edit.key { color::white(0.25) } else { color::white(0.07) }).border(ui.u(2.)).border_color(if i == edit.key { color::focus() } else { gpui::transparent_black() })
                    .cursor_pointer().child(SharedString::from(label.clone())).on_click(cx.listener(move |this, _, _, cx| this.editor_button(i, cx)))
            })));
        }
        div().absolute().size_full().top_0().left_0().bg(color::shade(0.8)).flex().items_center().justify_center()
            .child(div().w(ui.u(940.)).p(ui.u(40.)).rounded(ui.u(18.)).bg(color::panel()).flex().flex_col().gap(ui.u(22.))
                .child(div().text_size(ui.u(32.)).font_family(cm_ui::FONT).child(match edit.tv { Some((title, _)) => t(title), None if edit.watch => t("Processo a acompanhar"), None => t("Argumentos") }))
                .child(div().text_size(ui.u(22.)).child(if let Some((_, desc)) = edit.tv { t(desc) } else if edit.watch { t("Vazio acompanha o executável aberto. Informe o nome, por exemplo: jogo.exe.") } else { t("Argumentos enviados ao aplicativo. Vazio abre sem argumentos.") }))
                .child(div().min_h(ui.u(60.)).p(ui.u(14.)).bg(color::white(0.08)).text_size(ui.u(24.)).child(SharedString::from(format!("{}│", edit.text))))
                .child(grid).child(div().text_size(ui.u(18.)).child(t("Controle: direcional e A · B cancela. Teclado: digite, Enter salva, Esc cancela."))))
    }
}

fn problem(p: core::Problem) -> &'static str {
    match p { core::Problem::Missing => t("Escolha um aplicativo."), core::Problem::NotExe => t("Escolha um arquivo .exe."), core::Problem::NotFound => t("O aplicativo não foi encontrado. Adicione o executável novamente.") }
}
