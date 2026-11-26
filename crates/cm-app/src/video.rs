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
        Err(e) => log(&format!("VRR: falha ao ligar: {e}")),
    }
}

/// Puts the VRR setting back as it was before the session (the exact text, or no value at all).
pub fn restore_vrr(desk: &mut dyn Desk, j: &Journal) -> bool {
    let Some(b) = &j.vrr else { return true };
    match desk.vrr_write(b.previous.as_deref()) {
        Ok(()) => { log("VRR: de volta ao que era"); true }
        Err(e) => { log(&format!("VRR: não foi possível restaurar: {e}")); false }
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
    let before = desk.hdr_state(focus)?;
    if before == on { return Some(before); }
    let original = if j.hdr_turned_on == focus { false }
        else if j.hdr_turned_off == focus { true } else { before };
    // When moving away from the original state, persist the undo before touching HDR.
    // When moving back, retain the existing undo until the change has succeeded.
    if on != original {
        let old = j.clone();
        if on { j.hdr_turned_on = focus.to_string(); } else { j.hdr_turned_off = focus.to_string(); }
        if let Err(e) = session::write_journal(journal_path, j) {
            *j = old;
            log(&format!("HDR pelo menu: não deu para gravar o diário ({e}); não mudou"));
            return Some(before);
        }
    }
    let result = if on { desk.hdr_on(focus).map(|_| ()) } else { desk.hdr_off(focus) };
    match result {
        Ok(()) if on == original => {
            j.hdr_turned_on.clear(); j.hdr_turned_off.clear();
            if let Err(e) = session::write_journal(journal_path, j) {
                // The old durable journal still safely restores the original state.
                log(&format!("HDR pelo menu: não deu para atualizar o diário ({e})"));
            }
        }
        Err(e) => log(&format!("HDR pelo menu: falha: {e}")),
        _ => {}
    }
    desk.hdr_state(focus)
}

/// Returns whether each HDR undo succeeded, so failed work remains in the journal.
pub fn restore_hdr(desk: &mut dyn Desk, j: &Journal) -> (bool, bool) {
    let off = j.hdr_turned_off.is_empty() || desk.hdr_on(&j.hdr_turned_off).map(|_| ()).map_err(|e| log(&format!("HDR: falha ao restaurar: {e}"))).is_ok();
    let on = j.hdr_turned_on.is_empty() || desk.hdr_off(&j.hdr_turned_on).map_err(|e| log(&format!("HDR: falha ao restaurar: {e}"))).is_ok();
    (on, off)
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
        fn hdr_off(&mut self, _: &str) -> Result<(), String> { self.calls.push("hdr off".into()); self.hdr = false; Ok(()) }
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
    fn a_vrr_write_that_fails_keeps_the_undo() {
        let (mut desk, mut j, p) = (Fake { vrr_fails: true, ..Default::default() }, Journal::default(), journal_path("vrr-fail"));
        apply_vrr(&mut desk, &mut j, &p);
        assert!(j.vrr.is_some());
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
    fn hdr_is_not_changed_if_its_undo_cannot_be_written() {
        let dir = std::env::temp_dir().join(format!("cm2-hdr-denied-{}", std::process::id()));
        let _ = std::fs::remove_file(&dir);
        std::fs::write(&dir, "not a directory").unwrap();
        let mut desk = Fake::default();
        let mut journal = Journal::default();
        assert_eq!(hdr_from_menu(&mut desk, &mut journal, &dir.join("session.json"), "TV", true), Some(false));
        assert!(desk.calls.is_empty());
        assert!(journal.hdr_turned_on.is_empty());
        let _ = std::fs::remove_file(dir);
    }

    #[test]
    fn failed_vrr_restore_is_reported_for_retry() {
        let mut desk = Fake { vrr_fails: true, ..Default::default() };
        let journal = Journal { vrr: Some(VrrBackup { previous: Some("old".into()) }), ..Default::default() };
        assert!(!restore_vrr(&mut desk, &journal));
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
