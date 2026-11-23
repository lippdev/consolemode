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
const MANAGE: &str = n("Abrir os Ajustes do modo Xbox");
const BUSY: &str = n("Verificando a experiência de tela inteira…");
const UNAVAILABLE: &str = n("Indisponível");

pub(super) fn section() -> Section {
    let mut rows = rows(false, None, None, false);
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
    busy: bool,
) -> Vec<Row> {
    let compatible = registration.is_some_and(|s| s.compatible_build);
    let registered = registration.is_some_and(|s| s.registered);
    let selected = registration.is_some_and(|s| s.selected);
    let can_register =
        registration.is_some_and(|s| compatible && s.developer_mode && s.package_files);
    let status = if busy {
        BUSY
    } else if active.is_some() || compatible {
        n("Consulte o estado após mudar os Ajustes do Windows.")
    } else {
        n("A disponibilidade depende da versão do Windows e da liberação do recurso neste PC.")
    };
    let registration_help = match registration {
        Some(_) if registered && selected => {
            n("Home selecionada. Escolha outra Home no Windows antes de remover o registro.")
        }
        Some(_) if registered => {
            n("Registrada. Se o Windows oferecer o seletor de Home, escolha o Console Mode como padrão.")
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
        r(
            REFRESH,
            status,
            if busy {
                Value::Info("")
            } else {
                Value::Link(None)
            },
        ),
        r(
            ON_OPEN,
            n("Ao abrir a janela normalmente, mostra o Console Mode no modo Xbox."),
            if busy {
                Value::Info(if on_open { n("Ligado") } else { n("Desligado") })
            } else if active.is_some() || on_open {
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
            action(!busy && active.is_some()),
        ),
        r(
            if registered { UNREGISTER } else { REGISTER },
            registration_help,
            action(!busy && if registered { !selected && registration.is_some_and(|s| !s.running_as_home) } else { can_register }),
        ),
        r(
            MANAGE,
            n("Nesta página, você pode escolher a Home padrão se o Windows disponibilizar o seletor."),
            action(!busy && (compatible || active.is_some())),
        ),
    ]
}

#[derive(Clone, Copy, PartialEq, Eq)]
pub(super) enum Action {
    Refresh,
    SetActive(bool),
    Register,
    Unregister,
    Manage,
}

impl Action {
    fn is_available(self, rows: &[Row]) -> bool {
        rows.iter()
            .any(|row| row.value.is_interactive() && Self::for_row(row.title) == Some(self))
    }

    pub(super) fn for_row(title: &str) -> Option<Self> {
        Some(match title {
            REFRESH => Self::Refresh,
            ENTER => Self::SetActive(true),
            EXIT => Self::SetActive(false),
            REGISTER => Self::Register,
            UNREGISTER => Self::Unregister,
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
        *self.gaming_home_rows() = rows(self.config.gaming_home_on_open, None, None, true);
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
                    false,
                );
                if result.is_err() {
                    console.show_toast(
                        t("Não foi possível fazer a alteração. Consulte o log do Console Mode."),
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
        let unavailable = rows(false, None, None, false);
        assert!(!actionable(&unavailable, ON_OPEN));
        assert!(!actionable(&unavailable, ENTER));
        assert!(!actionable(&unavailable, REGISTER));
        assert!(!actionable(&unavailable, MANAGE));
        assert!(actionable(&unavailable, REFRESH));
        assert!(actionable(&rows(true, None, None, false), ON_OPEN));

        let mut registration = Status {
            compatible_build: true,
            developer_mode: true,
            package_files: true,
            registered: false,
            selected: false,
            running_as_home: false,
        };
        let ready = rows(false, Some(false), Some(&registration), false);
        assert!(actionable(&ready, ON_OPEN));
        assert!(actionable(&ready, ENTER));
        assert!(!actionable(&ready, EXIT));
        assert!(actionable(&ready, REGISTER));
        assert!(actionable(&ready, MANAGE));

        registration.registered = true;
        assert!(actionable(
            &rows(false, Some(true), Some(&registration), false),
            UNREGISTER
        ));
        registration.selected = true;
        let selected = rows(false, Some(true), Some(&registration), false);
        assert!(actionable(&selected, EXIT));
        assert!(!actionable(&selected, ENTER));
        assert!(!actionable(&selected, UNREGISTER));
        assert!(!actionable(&selected, REGISTER));
        assert!(actionable(&selected, MANAGE));

        let busy = rows(true, Some(true), Some(&registration), true);
        for title in [ON_OPEN, ENTER, EXIT, REGISTER, UNREGISTER, MANAGE, REFRESH] {
            assert!(!actionable(&busy, title), "{title} was actionable");
        }
    }
}
