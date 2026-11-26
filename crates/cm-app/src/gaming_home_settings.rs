//! The optional Windows gaming full-screen experience and Home registration.

use cm_platform::fse_win::{self, Status};
use gpui::Context;

use super::{Row, Section, Value, r};
use crate::{
    gaming_home,
    home::Console,
    i18n::{n, t},
};

pub const SECTION: &str = n("Experiência de tela inteira para jogos");
pub const ON_OPEN: &str = n("Entrar ao abrir o Console Mode");
const REFRESH: &str = n("Verificar disponibilidade");
const ENTER: &str = n("Entrar na experiência");
const EXIT: &str = n("Sair da experiência");
const REGISTER: &str = n("Registrar o Console Mode como opção de Home");
const UNREGISTER: &str = n("Remover o registro da Home");
const SELECT: &str = n("Selecionar o Console Mode como Home");
const RESTORE: &str = n("Restaurar a Home anterior");
const MANAGE: &str = n("Abrir os Ajustes do modo Xbox");
const BUSY: &str = n("Em andamento…");
const UNAVAILABLE: &str = n("Indisponível");

pub(crate) fn experimental_tag(ui: &cm_ui::Ui) -> gpui::Div {
    use gpui::{ParentElement, SharedString, Styled, div};
    div().flex_none().rounded_full().px(ui.u(10.)).py(ui.u(4.))
        .bg(cm_ui::color::accent_alpha(0.12)).text_color(cm_ui::color::accent())
        .text_size(ui.u(15.)).font_weight(gpui::FontWeight::SEMIBOLD)
        .child(SharedString::from(t("experimental").to_uppercase()))
}

pub(super) fn pending_indicator(ui: &cm_ui::Ui, row: usize) -> gpui::AnyElement {
    use gpui::{Animation, AnimationExt, IntoElement, ParentElement, Styled, div};
    use std::time::Duration;

    let size = ui.u(5.);
    let dot = || div().size(size).rounded_full().bg(cm_ui::color::accent());
    let mut indicator = div().flex_none().flex().items_center().gap(ui.u(4.)).ml(ui.u(6.));
    if cm_ui::motion::reduced() {
        return indicator.child(dot()).child(dot()).child(dot()).into_any_element();
    }
    for index in 0..3 {
        indicator = indicator.child(dot().with_animation(
            ("fse-pending-dot", row * 3 + index),
            Animation::new(Duration::from_millis(900)).repeat().with_max_fps(30.),
            move |el, t| {
                let phase = t - index as f32 / 3.;
                el.opacity(0.6 + 0.4 * (std::f32::consts::TAU * phase).cos())
            },
        ));
    }
    indicator.into_any_element()
}

pub(super) fn section() -> Section {
    let mut rows = rows(false, None, None);
    // Binding loads the stored switch before the first Windows capability check.
    rows[1].value = Value::Toggle(false);
    Section {
        title: SECTION,
        rows,
    }
}

fn action(available: bool) -> Value {
    if available {
        Value::Link(None)
    } else {
        Value::Info(UNAVAILABLE)
    }
}

fn rows(
    on_open: bool,
    active: Option<bool>,
    registration: Option<&Status>,
) -> Vec<Row> {
    let compatible = registration.is_some_and(|s| s.compatible_build);
    let registered = registration.is_some_and(|s| s.registered);
    let selected = registration.is_some_and(|s| s.selected);
    let can_restore = registration.is_some_and(|s| s.can_restore_home);
    let can_register =
        registration.is_some_and(|s| compatible && s.developer_mode && s.package_files);
    let status = if active.is_some() || compatible {
        n("Consulte o estado após mudar os Ajustes do Windows.")
    } else {
        n("A disponibilidade depende da versão do Windows e da liberação do recurso neste PC.")
    };
    let registration_help = match registration {
        Some(_) if registered && selected && can_restore => {
            n("Home selecionada. Restaure a Home anterior antes de remover o registro.")
        }
        Some(_) if registered && selected => {
            n("Home selecionada. Escolha outra Home no Windows antes de remover o registro.")
        }
        Some(_) if registered => {
            n("Registrada. Selecione o Console Mode como Home abaixo ou nos Ajustes do modo Xbox.")
        }
        Some(s) if compatible && !s.package_files => {
            n("A instalação atual não inclui os arquivos de registro da Home.")
        }
        Some(s) if compatible && !s.developer_mode => {
            n("O registro exige o Modo de Desenvolvedor do Windows.")
        }
        _ => n("O Console Mode aparece como opção de Home se o Windows oferecer o seletor."),
    };
    vec![
        r(REFRESH, status, Value::Link(None)),
        r(
            ON_OPEN,
            n("Ao abrir a janela normalmente, mostra o Console Mode no modo Xbox."),
            if active.is_some() || on_open {
                Value::Toggle(on_open)
            } else {
                Value::Info(UNAVAILABLE)
            },
        ),
        r(
            if active == Some(true) { EXIT } else { ENTER },
            if active == Some(true) {
                n("Volta para a área de trabalho do Windows.")
            } else {
                n("Mostra o Console Mode no modo Xbox. O Windows pode pedir confirmação.")
            },
            action(active.is_some()),
        ),
        r(
            if registered { UNREGISTER } else { REGISTER },
            registration_help,
            action(if registered { !selected && registration.is_some_and(|s| !s.running_as_home) } else { can_register }),
        ),
        r(
            if selected || can_restore { RESTORE } else { SELECT },
            if selected && !can_restore {
                n("Escolha outra Home nos Ajustes do modo Xbox.")
            } else if can_restore {
                n("Devolve ao Windows a seleção de Home que estava salva antes.")
            } else {
                n("Define o Console Mode como a Home do modo Xbox para este usuário do Windows.")
            },
            action(can_restore || (registered && !selected)),
        ),
        r(
            MANAGE,
            n("Nesta página, você pode escolher a Home padrão se o Windows disponibilizar o seletor."),
            action(compatible || active.is_some()),
        ),
    ]
}

#[derive(Clone, Copy, PartialEq, Eq)]
pub(super) enum Action {
    Refresh,
    SetActive(bool),
    Register,
    Unregister,
    Select,
    Restore,
    Manage,
}

impl Action {
    fn is_available(self, rows: &[Row]) -> bool {
        rows.iter()
            .any(|row| row.value.is_interactive() && Self::for_row(row.title) == Some(self))
    }

    fn mark_pending(self, rows: &mut [Row]) {
        for row in rows.iter_mut().filter(|row| row.value.is_interactive()) {
            match Self::for_row(row.title) {
                Some(action) if action == self => row.value = Value::Pending(BUSY),
                Some(action) if action != Self::Manage => row.value = Value::DisabledLink,
                _ => {}
            }
        }
    }

    pub(super) fn for_row(title: &str) -> Option<Self> {
        Some(match title {
            REFRESH => Self::Refresh,
            ENTER => Self::SetActive(true),
            EXIT => Self::SetActive(false),
            REGISTER => Self::Register,
            UNREGISTER => Self::Unregister,
            SELECT => Self::Select,
            RESTORE => Self::Restore,
            MANAGE => Self::Manage,
            _ => return None,
        })
    }
}

impl Console {
    fn gaming_home_rows(&mut self) -> &mut Vec<Row> {
        &mut self
            .system
            .iter_mut()
            .flat_map(|category| &mut category.sections)
            .find(|section| section.title == SECTION)
            .expect("gaming home section")
            .rows
    }

    pub(super) fn change_gaming_home(&mut self, action: Action, cx: &mut Context<Self>) {
        if !action.is_available(self.gaming_home_rows()) {
            return;
        }
        if matches!(action, Action::Manage) {
            if !fse_win::open_settings() {
                self.show_toast(t("Não foi possível abrir os Ajustes do Windows."), cx);
            }
            return;
        }
        action.mark_pending(self.gaming_home_rows());
        cx.spawn(async move |this, cx| {
            let (active, registration, result) = cx
                .background_executor()
                .spawn(async move {
                    let result = match action {
                        Action::SetActive(target) => {
                            gaming_home::set_active(target).map_err(str::to_owned)
                        }
                        Action::Register => fse_win::register(),
                        Action::Unregister => fse_win::unregister(),
                        Action::Select => fse_win::select_home(),
                        Action::Restore => fse_win::restore_home(),
                        _ => Ok(()),
                    };
                    let registration = fse_win::status();
                    let active = if gaming_home::supported() == Some(true) {
                        gaming_home::active()
                    } else {
                        None
                    };
                    (active, registration, result)
                })
                .await;
            if let Err(error) = &registration {
                crate::startup_log(&format!("Estado do registro da Home: {error}"));
            }
            if let Err(error) = &result {
                crate::startup_log(&format!(
                    "Alteração da experiência de tela inteira: {error}"
                ));
            }
            let _ = this.update(cx, |console, cx| {
                *console.gaming_home_rows() = rows(
                    console.config.gaming_home_on_open,
                    active,
                    registration.as_ref().ok(),
                );
                if let Err(error) = &result {
                    let message = if error == gaming_home::HOME_REQUIRED {
                        t(gaming_home::HOME_REQUIRED)
                    } else if error == "CM2_FSE_HOME_SELECTION_CHANGED" {
                        t("O Windows não manteve a Home escolhida. A FSE continua usando a Home definida pelo sistema.")
                    } else {
                        t("Não foi possível fazer a alteração. Consulte o log do Console Mode.")
                    };
                    console.show_toast(message, cx);
                } else if action == Action::Select && registration.as_ref().is_ok_and(|s| s.selected) {
                    console.show_toast(t("Console Mode selecionado como Home."), cx);
                } else if action == Action::Restore && registration.as_ref().is_ok_and(|s| !s.selected) {
                    console.show_toast(t("Home anterior restaurada."), cx);
                } else if action == Action::Register && registration.as_ref().is_ok_and(|s| s.registered) {
                    console.show_toast(
                        t("Home registrada. Selecione o Console Mode como Home nesta página ou nos Ajustes do modo Xbox."),
                        cx,
                    );
                }
                cx.notify();
            });
        })
        .detach();
        cx.notify();
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn actionable(rows: &[Row], title: &str) -> bool {
        if let Some(action) = Action::for_row(title) {
            return action.is_available(rows);
        }
        rows.iter()
            .any(|row| row.title == title && row.value.is_interactive())
    }

    #[test]
    fn controls_follow_capabilities_and_selection() {
        let unavailable = rows(false, None, None);
        assert!(!actionable(&unavailable, ON_OPEN));
        assert!(!actionable(&unavailable, ENTER));
        assert!(!actionable(&unavailable, REGISTER));
        assert!(!actionable(&unavailable, MANAGE));
        assert!(actionable(&unavailable, REFRESH));
        assert!(actionable(&rows(true, None, None), ON_OPEN));

        let mut registration = Status {
            compatible_build: true,
            developer_mode: true,
            package_files: true,
            registered: false,
            selected: false,
            running_as_home: false,
            can_restore_home: false,
        };
        let ready = rows(false, Some(false), Some(&registration));
        assert!(actionable(&ready, ON_OPEN));
        assert!(actionable(&ready, ENTER));
        assert!(!actionable(&ready, EXIT));
        assert!(actionable(&ready, REGISTER));
        assert!(!actionable(&ready, SELECT));
        assert!(actionable(&ready, MANAGE));

        registration.registered = true;
        let registered = rows(false, Some(true), Some(&registration));
        assert!(actionable(&registered, UNREGISTER));
        assert!(actionable(&registered, SELECT));
        assert!(!actionable(&registered, RESTORE));
        registration.selected = true;
        let selected = rows(false, Some(true), Some(&registration));
        assert!(actionable(&selected, EXIT));
        assert!(!actionable(&selected, ENTER));
        assert!(!actionable(&selected, UNREGISTER));
        assert!(!actionable(&selected, REGISTER));
        assert!(!actionable(&selected, SELECT));
        assert!(!actionable(&selected, RESTORE));
        assert!(actionable(&selected, MANAGE));
        registration.can_restore_home = true;
        let restorable = rows(false, Some(true), Some(&registration));
        assert!(actionable(&restorable, RESTORE));
        assert!(!actionable(&restorable, UNREGISTER));
    }

    #[test]
    fn pending_actions_keep_known_rows_and_independent_controls() {
        let mut registration = Status {
            compatible_build: true,
            developer_mode: true,
            package_files: true,
            registered: true,
            selected: false,
            running_as_home: false,
            can_restore_home: false,
        };
        for pending in [
            Action::SetActive(false),
            Action::Unregister,
            Action::Select,
            Action::Restore,
            Action::Refresh,
        ] {
            registration.selected = pending == Action::Restore;
            registration.can_restore_home = registration.selected;
            let known = rows(true, Some(true), Some(&registration));
            let mut busy = rows(true, Some(true), Some(&registration));
            assert!(pending.is_available(&busy));
            pending.mark_pending(&mut busy);
            for (row, previous) in busy.iter().zip(&known) {
                assert_eq!(row.title, previous.title);
                assert_eq!(row.desc, previous.desc);
                assert_eq!(
                    matches!(row.value, Value::Pending(_)),
                    Action::for_row(row.title) == Some(pending),
                );
            }
            for title in [EXIT, UNREGISTER, SELECT, RESTORE, REFRESH] {
                assert!(!actionable(&busy, title), "{title} was actionable");
            }
            assert!(actionable(&busy, ON_OPEN));
            assert!(actionable(&busy, MANAGE));
        }
    }
}
