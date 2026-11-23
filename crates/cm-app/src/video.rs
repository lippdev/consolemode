//! What 1.7's VideoFeaturesService does around a session: VRR on for the session, HDR toggled from
//! the session menu, and the way back for both. Each change goes in the journal first, so the restore
//! (and the recovery after a crash) undoes exactly what the session changed and nothing the user had.

use std::path::Path;

use cm_core::{
    session::{self, Journal, VrrBackup},
    video_features,
};

use crate::{engine::Desk, startup_log as log};

/// VRR for the session (config `vrrEnable`). The text of the setting from before goes in the journal
/// before it changes; a setting that already had VRR on is left alone and not restored.
pub fn apply_vrr(desk: &mut dyn Desk, j: &mut Journal, journal_path: &Path) {
    let current = desk.vrr_read();
    let setting = current.clone().unwrap_or_default();
    if video_features::vrr_enabled(&setting) { return log("VRR: já estava ligado"); }
    j.vrr = Some(VrrBackup { previous: current });
    if let Err(e) = session::write_journal(journal_path, j) {
        j.vrr = None;
        return log(&format!("VRR: não deu para gravar o diário ({e}); não aplicado"));
    }
    match desk.vrr_write(Some(&video_features::with_vrr(&setting, true))) {
        Ok(()) => log("VRR: ligado para a sessão"),
        Err(e) => { j.vrr = None; log(&format!("VRR: falha ao ligar: {e}")); }
    }
}

/// Puts the VRR setting back as it was before the session (the exact text, or no value at all).
pub fn restore_vrr(desk: &mut dyn Desk, j: &Journal) {
    let Some(b) = &j.vrr else { return };
    match desk.vrr_write(b.previous.as_deref()) {
        Ok(()) => log("VRR: de volta ao que era"),
        Err(e) => log(&format!("VRR: não foi possível restaurar: {e}")),
    }
}

/// VRR turned on or off from the session menu. The setting from before the session goes in the
/// journal the first time anything changes it (by the session or the menu), so the restore puts back
/// exactly what the user had. Returns whether VRR is on now.
pub fn vrr_from_menu(desk: &mut dyn Desk, j: &mut Journal, journal_path: &Path, on: bool) -> Option<bool> {
    let current = desk.vrr_read();
    let setting = current.clone().unwrap_or_default();
    if video_features::vrr_enabled(&setting) == on { return Some(on); }
    if j.vrr.is_none() {
        j.vrr = Some(VrrBackup { previous: current });
        if let Err(e) = session::write_journal(journal_path, j) {
            j.vrr = None;
            log(&format!("VRR pelo menu: não deu para gravar o diário ({e}); não mudou"));
            return Some(!on);
        }
    }
    match desk.vrr_write(Some(&video_features::with_vrr(&setting, on))) {
        Ok(()) => log(&format!("VRR pelo menu: {}", if on { "ligado" } else { "desligado" })),
        Err(e) => log(&format!("VRR pelo menu: falha: {e}")),
    }
    Some(video_features::vrr_enabled(&desk.vrr_read().unwrap_or_default()))
}

/// HDR turned on or off from the session menu. Tracks what to undo: HDR the session turned on goes back
/// off; HDR that was on before and the menu turned off goes back on. Returns the state it ended in.
pub fn hdr_from_menu(desk: &mut dyn Desk, j: &mut Journal, journal_path: &Path, focus: &str, on: bool) -> Option<bool> {
    let before = desk.hdr_state(focus);
    if before.is_none() || before == Some(on) { return before; }
    if on {
        match desk.hdr_on(focus) {
            Ok(turned_on) => {
                // back to how it was, or one more thing the restore turns off again
                if j.hdr_turned_off == focus { j.hdr_turned_off.clear(); }
                else if turned_on && j.hdr_turned_on.is_empty() { j.hdr_turned_on = focus.to_string(); }
            }
            Err(e) => log(&format!("HDR pelo menu: não ligou: {e}")),
        }
    } else {
        // the session's own HDR needs no undo any more; one the user had before does
        let ours = j.hdr_turned_on == focus;
        desk.hdr_off(focus);
        if ours { j.hdr_turned_on.clear(); } else { j.hdr_turned_off = focus.to_string(); }
    }
    let _ = session::write_journal(journal_path, j);
    log(&format!("HDR pelo menu: {}", if on { "ligado" } else { "desligado" }));
    desk.hdr_state(focus)
}

/// The restore: HDR the menu turned off comes back on, HDR the session turned on goes off.
pub fn restore_hdr(desk: &mut dyn Desk, j: &Journal) {
    if !j.hdr_turned_off.is_empty() { let _ = desk.hdr_on(&j.hdr_turned_off); }
    if !j.hdr_turned_on.is_empty() { desk.hdr_off(&j.hdr_turned_on); }
}

#[cfg(test)]
mod tests {
    use super::*;
    use cm_core::config::DisplayMode;

    /// A desk that remembers HDR and the VRR text, and what was done to it.
    #[derive(Default)]
    struct Fake { hdr: bool, vrr: Option<String>, calls: Vec<String>, vrr_fails: bool }

    impl Desk for Fake {
        fn backup(&mut self) -> Result<String, String> { Ok(String::new()) }
        fn restore(&mut self, _: &str) -> Result<(), String> { Ok(()) }
        fn focus(&mut self, _: &str) -> Result<(), String> { Ok(()) }
        fn hide(&mut self, _: &[String]) -> Result<(), String> { Ok(()) }
        fn set_mode(&mut self, _: &str, _: &DisplayMode) -> Result<(), String> { Ok(()) }
        fn hdr_on(&mut self, _: &str) -> Result<bool, String> { self.calls.push("hdr on".into()); let was = self.hdr; self.hdr = true; Ok(!was) }
        fn hdr_off(&mut self, _: &str) { self.calls.push("hdr off".into()); self.hdr = false; }
        fn audio_default(&mut self) -> Option<String> { None }
        fn set_audio(&mut self, _: &str) -> Result<(), String> { Ok(()) }
        fn screen_rect(&mut self, _: &str) -> Option<(i32, i32, i32, i32)> { None }
        fn hdr_state(&mut self, _: &str) -> Option<bool> { Some(self.hdr) }
        fn vrr_read(&mut self) -> Option<String> { self.vrr.clone() }
        fn vrr_write(&mut self, v: Option<&str>) -> Result<(), String> {
            if self.vrr_fails { return Err("sem permissão".into()); }
            self.calls.push(format!("vrr {v:?}"));
            self.vrr = v.map(str::to_string);
            Ok(())
        }
    }

    fn journal_path(name: &str) -> std::path::PathBuf {
        let d = std::env::temp_dir().join(format!("cm2-video-{name}-{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&d);
        d.join("session.json")
    }

    #[test]
    fn vrr_is_turned_on_for_the_session_and_put_back_exactly() {
        for before in [None, Some("SwapEffectUpgradeEnable=1;".to_string()), Some("A=1;VRROptimizeEnable=0;".to_string())] {
            let (mut desk, mut j, p) = (Fake { vrr: before.clone(), ..Default::default() }, Journal::default(), journal_path("vrr"));
            apply_vrr(&mut desk, &mut j, &p);
            assert!(video_features::vrr_enabled(desk.vrr.as_deref().unwrap()), "{before:?}");
            // in the journal before it changed, so a crash is undone too
            assert_eq!(session::read_journal(&p).unwrap().vrr, Some(VrrBackup { previous: before.clone() }));
            restore_vrr(&mut desk, &j);
            assert_eq!(desk.vrr, before, "the exact text, or no value at all");
        }
    }

    #[test]
    fn vrr_from_the_menu_goes_both_ways_and_the_restore_puts_back_what_the_user_had() {
        for before in [None, Some("VRROptimizeEnable=1;".to_string())] {
            let (mut desk, mut j, p) = (Fake { vrr: before.clone(), ..Default::default() }, Journal::default(), journal_path("vrr-menu"));
            let first = !video_features::vrr_enabled(before.as_deref().unwrap_or_default());
            assert_eq!(vrr_from_menu(&mut desk, &mut j, &p, first), Some(first));
            assert_eq!(session::read_journal(&p).unwrap().vrr, Some(VrrBackup { previous: before.clone() }));
            assert_eq!(vrr_from_menu(&mut desk, &mut j, &p, !first), Some(!first));
            // the first backup stays: it is what the user had
            assert_eq!(j.vrr, Some(VrrBackup { previous: before.clone() }));
            restore_vrr(&mut desk, &j);
            assert_eq!(desk.vrr, before);
        }
    }

    #[test]
    fn vrr_that_was_already_on_is_left_alone_and_not_undone() {
        let (mut desk, mut j, p) = (Fake { vrr: Some("VRROptimizeEnable=1;".into()), ..Default::default() }, Journal::default(), journal_path("vrr-on"));
        apply_vrr(&mut desk, &mut j, &p);
        assert!(desk.calls.is_empty() && j.vrr.is_none());
        restore_vrr(&mut desk, &j);
        assert!(desk.calls.is_empty());
        assert_eq!(desk.vrr.as_deref(), Some("VRROptimizeEnable=1;"));
    }

    #[test]
    fn a_vrr_write_that_fails_leaves_nothing_to_undo() {
        let (mut desk, mut j, p) = (Fake { vrr_fails: true, ..Default::default() }, Journal::default(), journal_path("vrr-fail"));
        apply_vrr(&mut desk, &mut j, &p);
        assert!(j.vrr.is_none());
    }

    #[test]
    fn a_crash_recovery_undoes_vrr_from_the_journal_alone() {
        let p = journal_path("vrr-crash");
        let mut desk = Fake { vrr: Some("A=1;".into()), ..Default::default() };
        let mut j = Journal::default();
        apply_vrr(&mut desk, &mut j, &p);
        // the app died: a new desk reads only the file
        let recovered = session::read_journal(&p).unwrap();
        restore_vrr(&mut desk, &recovered);
        assert_eq!(desk.vrr.as_deref(), Some("A=1;"));
    }

    #[test]
    fn hdr_the_menu_turns_on_is_turned_off_again_at_the_end() {
        let (mut desk, mut j, p) = (Fake::default(), Journal::default(), journal_path("hdr-on"));
        assert_eq!(hdr_from_menu(&mut desk, &mut j, &p, "TV", true), Some(true));
        assert_eq!(j.hdr_turned_on, "TV");
        restore_hdr(&mut desk, &j);
        assert!(!desk.hdr);
    }

    #[test]
    fn hdr_that_was_on_before_and_the_menu_turned_off_comes_back_on() {
        let (mut desk, mut j, p) = (Fake { hdr: true, ..Default::default() }, Journal::default(), journal_path("hdr-off"));
        assert_eq!(hdr_from_menu(&mut desk, &mut j, &p, "TV", false), Some(false));
        assert_eq!((j.hdr_turned_off.as_str(), j.hdr_turned_on.as_str()), ("TV", ""));
        restore_hdr(&mut desk, &j);
        assert!(desk.hdr, "back on, as the user had it");
        // off and on again from the menu: nothing left to undo
        let (mut desk, mut j) = (Fake { hdr: true, ..Default::default() }, Journal::default());
        hdr_from_menu(&mut desk, &mut j, &p, "TV", false);
        hdr_from_menu(&mut desk, &mut j, &p, "TV", true);
        assert_eq!((j.hdr_turned_off.as_str(), j.hdr_turned_on.as_str()), ("", ""));
    }

    #[test]
    fn hdr_the_session_turned_on_and_the_menu_turned_off_needs_no_undo() {
        let (mut desk, mut j, p) = (Fake { hdr: true, ..Default::default() }, Journal { hdr_turned_on: "TV".into(), ..Default::default() }, journal_path("hdr-ours"));
        assert_eq!(hdr_from_menu(&mut desk, &mut j, &p, "TV", false), Some(false));
        assert_eq!((j.hdr_turned_off.as_str(), j.hdr_turned_on.as_str()), ("", ""));
    }

    #[test]
    fn asking_for_the_state_it_already_has_changes_nothing() {
        let (mut desk, mut j, p) = (Fake::default(), Journal::default(), journal_path("hdr-same"));
        assert_eq!(hdr_from_menu(&mut desk, &mut j, &p, "TV", false), Some(false));
        assert!(desk.calls.is_empty() && j.hdr_turned_off.is_empty());
    }
}
