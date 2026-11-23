//! The controller battery in the interface (1.7 MainViewModel.Battery): the emptiest pad's level
//! beside the clock in the top bar and in the session menu, and a notice over the game when a pad
//! runs low during a session. The numbers and the warning rules are in `cm_core::controller_battery`;
//! the reading is the controller thread's (input.rs).

use cm_core::controller_battery::{self as battery, BatteryReading, LowBatteryAlarm};
use cm_ui::{Ui, color};
use gpui::{Context, Div, ParentElement, SharedString, Styled, div};

use crate::{home::Console, i18n::{t, tf}};

/// Material Symbols battery_0_bar … battery_6_bar, then battery_full; and battery_charging_full.
const BARS: [char; 8] = ['\u{f0a3}', '\u{f0a4}', '\u{f0a5}', '\u{f0a6}', '\u{f0a7}', '\u{f0a0}', '\u{f0a1}', '\u{e1a4}'];
const CHARGING: char = '\u{e1a3}';

/// The icon for a level.
pub fn glyph(percent: u8, charging: bool) -> char { if charging { CHARGING } else { BARS[battery::icon_step(percent) as usize] } }

/// The Segoe Fluent Icons battery glyph for the notice over the game (Battery0..9 = E850..E859,
/// Battery10 = E83F; charging: BatteryCharging0..8 = E85A..E862, 9 = E83E).
fn segoe_glyph(percent: u8, charging: bool) -> String {
    let step = ((percent as u32 + 5) / 10).min(10);
    let code = match (charging, step) { (false, 10) => 0xE83F, (false, s) => 0xE850 + s, (true, s) if s >= 9 => 0xE83E, (true, s) => 0xE85A + s };
    char::from_u32(code).map(String::from).unwrap_or_default()
}

impl Console {
    /// The chip shown beside the clock: an icon and the level of the emptiest pad (nothing without one).
    pub(crate) fn battery_chip(&self, ui: &Ui, text_size: f32, icon_size: f32) -> Option<Div> {
        let r = battery::lowest(&self.batteries)?;
        // a low pad that is not charging is the one to notice
        let warn = !r.charging && r.percent <= LowBatteryAlarm::LOW;
        let tint = if warn { gpui::rgb(0xff8a80).into() } else { color::mute() };
        Some(div().flex().items_center().gap(ui.u(6.)).text_size(ui.u(text_size)).text_color(tint)
            .child(ui.icon(glyph(r.percent, r.charging), icon_size, tint))
            .child(SharedString::from(battery::format(r))))
    }

    /// The controller thread read the batteries: show the emptiest, and warn (during a session) about any
    /// that just ran low, like 1.7.
    pub(crate) fn set_batteries(&mut self, list: Vec<BatteryReading>, cx: &mut Context<Self>) {
        self.battery_alarms.retain(|id, _| list.iter().any(|r| &r.id == id));
        if self.running.is_some() {
            for r in &list {
                let alarm = self.battery_alarms.entry(r.id.clone()).or_default();
                if alarm.update(r.percent, r.charging).is_none() { continue; }
                crate::startup_log(&format!("Controle: bateria fraca ({}, {})", r.name, battery::format(r)));
                self.warn_low_battery(r);
            }
        } else {
            // outside a session the warning waits; a pad that charges still re-arms it
            for r in &list { self.battery_alarms.entry(r.id.clone()).or_default().update(r.percent, r.charging); }
        }
        if list != self.batteries {
            self.batteries = list;
            cx.notify();
        }
    }

    /// The notice over the game, top right of the game screen.
    fn warn_low_battery(&self, r: &BatteryReading) {
        let Some(running) = &self.running else { return };
        let mut desk = cm_platform::WinDesk::new(self.paths.data_dir.clone());
        let Some(rect) = desk.screen_rect(&running.shell_focus).or_else(primary_rect) else { return };
        let a: gpui::Rgba = color::accent().into();
        let accent = ((a.r * 255.) as u32) << 16 | ((a.g * 255.) as u32) << 8 | (a.b * 255.) as u32;
        cm_platform::hint_win::show(rect, cm_platform::hint_win::Hint {
            glyph: segoe_glyph(r.percent, false),
            title: t("Bateria do controle fraca").into(),
            body: tf("Restam {0} de bateria. Conecte o cabo ou carregue o controle.", &[&battery::format(r)]),
            seconds: 7.,
            accent,
            still: cm_ui::motion::reduced(),
        });
    }
}

/// The primary screen, when the game screen's rectangle can't be read (a simulated desk).
fn primary_rect() -> Option<(i32, i32, i32, i32)> { cm_platform::primary_rect() }

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn the_icon_steps_from_empty_to_full_and_charging_has_its_own() {
        assert_eq!(glyph(0, false), BARS[0]);
        assert_eq!(glyph(100, false), BARS[7]);
        assert_eq!(glyph(40, true), CHARGING);
        let mut all: Vec<char> = BARS.to_vec();
        all.push(CHARGING);
        all.sort();
        all.dedup();
        assert_eq!(all.len(), 9);
    }

    #[test]
    fn the_notice_icon_follows_segoe_fluent() {
        let code = |p, c| segoe_glyph(p, c).chars().next().unwrap() as u32;
        assert_eq!((code(0, false), code(54, false), code(100, false)), (0xE850, 0xE855, 0xE83F));
        assert_eq!((code(0, true), code(80, true), code(100, true)), (0xE85A, 0xE862, 0xE83E));
    }
}
