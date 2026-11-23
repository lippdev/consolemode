//! Sistema → Controles → "Acordar o PC pelo controle" (1.7 MainViewModel.ControllerWake): shows whether
//! the controller's adapters may wake the PC from sleep, and turns them on (Windows asks for
//! administrator rights, as in 1.7). Reading and turning on run in the background; the classification and
//! the checks are `cm_core::controller_wake`.

use cm_core::controller_wake::{self as wake, Status, WakeDevice};
use cm_platform::wake_win::{self, Enabled};
use gpui::Context;

use crate::{home::Console, i18n::{n, t, tf}, settings::Value};

pub const WAKE_ROW: &str = n("Acordar o PC pelo controle");

#[derive(Default)]
pub struct WakeState {
    /// The devices read; None when they couldn't be.
    pub devices: Option<Vec<WakeDevice>>,
    pub checked: bool,
    /// A reading or turning on is running.
    pub busy: bool,
}

impl WakeState {
    /// The value beside the row (None: nothing yet).
    fn label(&self) -> Option<&'static str> {
        if self.busy { return Some(n("Verificando os dispositivos…")); }
        if !self.checked { return None; }
        match wake::status(self.devices.as_deref()) {
            Status::Unknown | Status::None => Some(n("Indisponível")),
            Status::Ready => Some(n("Ligado")),
            Status::Off => Some(n("Ativar")),
        }
    }
}

impl Console {
    fn show_wake_row(&mut self) {
        let label = self.wake.label();
        let rows = self.system.iter_mut().flat_map(|c| c.sections.iter_mut()).flat_map(|s| s.rows.iter_mut());
        for r in rows.filter(|r| r.title == WAKE_ROW) { r.value = Value::Link(label); }
    }

    /// The message for what was read (the row only has a word).
    fn wake_message(&self) -> String {
        let devices = self.wake.devices.as_deref();
        match wake::status(devices) {
            Status::Unknown => t("Não foi possível verificar os dispositivos.").into(),
            Status::None => t("Nenhum adaptador de controle ou Bluetooth deste PC pode acordá-lo. Conecte o adaptador ou o controle e tente de novo.").into(),
            Status::Ready => tf("Pronto: {0} pode acordar o PC.", &[&wake::names(devices.unwrap_or_default())]),
            Status::Off => tf("Ainda não pode acordar o PC: {0}.", &[&wake::names(devices.unwrap_or_default().iter().filter(|d| !d.armed))]),
        }
    }

    /// The Controles category opened: read the devices once, quietly (no rights needed).
    pub(crate) fn check_wake(&mut self, cx: &mut Context<Self>) {
        if self.wake.checked || self.wake.busy { return; }
        self.wake.busy = true;
        self.show_wake_row();
        cx.spawn(async move |this, cx| {
            let read = cx.background_executor().spawn(async { wake_win::read() }).await;
            let _ = this.update(cx, |c, cx| {
                match &read { Some(d) => crate::startup_log(&format!("Acordar pelo controle: {} dispositivo(s) lido(s)", d.len())), None => crate::startup_log("Acordar pelo controle: leitura falhou") }
                c.wake = WakeState { devices: read, checked: true, busy: false };
                c.show_wake_row();
                cx.notify();
            });
        }).detach();
        cx.notify();
    }

    /// The row was pressed: read again (an adapter may have been plugged in since) and turn on the ones still off.
    pub(crate) fn wake_row_pressed(&mut self, cx: &mut Context<Self>) {
        if self.wake.busy { return self.show_toast(t("Aguarde, ainda estou trabalhando nisso."), cx); }
        self.wake.busy = true;
        self.show_wake_row();
        cx.spawn(async move |this, cx| {
            let first = cx.background_executor().spawn(async { wake_win::read() }).await;
            let off: Vec<WakeDevice> = first.iter().flatten().filter(|d| !d.armed).cloned().collect();
            if off.is_empty() {
                // nothing to turn on: say why
                let _ = this.update(cx, |c, cx| {
                    c.wake = WakeState { devices: first, checked: true, busy: false };
                    c.show_wake_row();
                    let message = c.wake_message();
                    c.show_toast(&message, cx);
                });
                return;
            }
            let to_turn_on = off.clone();
            let (result, detail) = cx.background_executor().spawn(async move { wake_win::enable(&to_turn_on) }).await;
            crate::startup_log(&format!("Acordar pelo controle: {detail}"));
            // PowerShell saying it is done is not proof: read the devices again and check they can wake the PC
            let after = cx.background_executor().spawn(async { wake_win::read() }).await;
            let mut result = result;
            if result == Enabled::Done {
                let still = wake::still_off(&off, after.as_deref());
                if !still.is_empty() {
                    crate::startup_log(&format!("Acordar pelo controle: ainda desligado depois de ativar: {}", wake::names(&still)));
                    result = if after.is_none() { Enabled::Failed } else { Enabled::NotFound };
                }
            }
            let _ = this.update(cx, |c, cx| {
                c.wake = WakeState { devices: after, checked: true, busy: false };
                c.show_wake_row();
                let message = match result {
                    Enabled::Done => t("Pronto. Suspenda o PC e aperte o botão central do controle para testar."),
                    Enabled::Canceled => t("O Windows não recebeu a permissão; nada foi alterado."),
                    Enabled::NotFound => t("O dispositivo não foi encontrado na hora de ativar (foi desconectado?). Conecte-o de novo e tente outra vez."),
                    Enabled::Failed => t("Não foi possível ativar. Os detalhes estão no log."),
                };
                c.show_toast(message, cx);
            });
        }).detach();
        cx.notify();
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn device(armed: bool) -> WakeDevice { WakeDevice { instance_name: r"USB\VID_045E&PID_02FE\6_0".into(), name: "Xbox Wireless Adapter".into(), pnp_class: "XboxWirelessAdapter".into(), armed } }

    #[test]
    fn the_row_says_what_was_found() {
        let mut s = WakeState::default();
        assert_eq!(s.label(), None, "nothing read yet");
        s.busy = true;
        assert_eq!(s.label(), Some("Verificando os dispositivos…"));
        s = WakeState { checked: true, ..Default::default() };
        assert_eq!(s.label(), Some("Indisponível"), "could not read");
        s.devices = Some(vec![]);
        assert_eq!(s.label(), Some("Indisponível"), "nothing can wake this PC");
        s.devices = Some(vec![device(false)]);
        assert_eq!(s.label(), Some("Ativar"));
        s.devices = Some(vec![device(true)]);
        assert_eq!(s.label(), Some("Ligado"));
    }
}
