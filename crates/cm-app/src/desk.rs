//! Which [`Desk`] the session uses: the real one (cm_platform::WinDesk: CCD screens, modes, HDR,
//! Core Audio), or the simulated one when `CM2_SIMULATED_DESK` is set (development).

use cm_core::config::DisplayMode;
use cm_platform::WinDesk;

use crate::engine::{Desk, SimulatedDesk};

pub fn pick(data_dir: &std::path::Path) -> Box<dyn Desk> {
    if std::env::var_os("CM2_SIMULATED_DESK").is_some() { return Box::new(SimulatedDesk::default()); }
    Box::new(WinDesk::new(data_dir.to_path_buf()))
}

impl Desk for WinDesk {
    fn backup(&mut self) -> Result<String, String> { WinDesk::backup(self) }
    fn restore(&mut self, b: &str) -> Result<(), String> { WinDesk::restore(self, b) }
    fn focus(&mut self, id: &str) -> Result<(), String> { WinDesk::focus(self, id) }
    fn hide(&mut self, ids: &[String]) -> Result<(), String> { WinDesk::hide(self, ids) }
    fn set_mode(&mut self, id: &str, m: &DisplayMode) -> Result<(), String> { WinDesk::set_mode(self, id, m) }
    fn hdr_on(&mut self, id: &str) -> Result<bool, String> { WinDesk::hdr_on(self, id) }
    fn hdr_off(&mut self, id: &str) -> Result<(), String> { WinDesk::hdr_off(self, id) }
    fn audio_default(&mut self) -> Option<String> { WinDesk::audio_default(self) }
    fn set_audio(&mut self, id: &str) -> Result<(), String> { WinDesk::set_audio(self, id) }
    fn screen_rect(&mut self, id: &str) -> Option<(i32, i32, i32, i32)> { WinDesk::screen_rect(self, id) }
    fn current_mode(&mut self, id: &str) -> Option<DisplayMode> { WinDesk::current_mode(self, id) }
    fn hdr_state(&mut self, id: &str) -> Option<bool> { WinDesk::hdr_state(self, id) }
    fn vrr_read(&mut self) -> Option<String> { cm_platform::video_win::vrr_setting() }
    fn vrr_write(&mut self, text: Option<&str>) -> Result<(), String> { cm_platform::video_win::set_vrr_setting(text) }
    fn tv_on(&mut self, tv: &cm_core::tv::TvControl, data_dir: &std::path::Path) { crate::tv::session_on(tv, data_dir) }
    fn tv_back(&mut self, step: &cm_core::tv::TvRestore, data_dir: &std::path::Path) { crate::tv::session_back(step, data_dir) }
}
