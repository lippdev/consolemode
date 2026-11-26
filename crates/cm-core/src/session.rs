//! Session rules with no OS calls (ported from 1.7: PlayniteExitPolicy, CustomApp, SessionJournal),
//! so they are tested anywhere. The engine in cm-app asks these, the platform layer does the work.

use std::{
    collections::{HashSet, VecDeque},
    fs, io,
    path::Path,
    time::{Duration, SystemTime, UNIX_EPOCH},
};

use serde::{Deserialize, Serialize};

use crate::desktop_icons::DesktopIcon;

/// The launchers, as 1.7 names them in `fullscreenMode`.
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Launcher { BigPicture, Playnite, Xbox, Custom { path: String, args: String, watch: String } }

impl Launcher {
    pub fn from_mode(mode: &str) -> Launcher {
        match mode { "playnite" => Launcher::Playnite, "xboxMode" => Launcher::Xbox, _ => Launcher::BigPicture }
    }
    pub fn mode(&self) -> &'static str {
        match self { Launcher::BigPicture => "bigPicture", Launcher::Playnite => "playnite", Launcher::Xbox => "xboxMode", Launcher::Custom { .. } => "customApp" }
    }
}

// ───────────── Playnite ─────────────

/// How long Playnite may run without a big window before the session ends anyway (it swaps a
/// loading window for the main one on slow starts).
pub const PLAYNITE_WINDOW_GRACE: Duration = Duration::from_secs(20);

/// `window_put_away`: its window still exists but is hidden or minimized, which Playnite does
/// while a game it started runs, however long it lasts.
pub fn playnite_exit(running: bool, showing: bool, missing_for: Option<Duration>, window_put_away: bool) -> bool {
    if !running { return true; }
    if showing || window_put_away { return false; }
    missing_for.is_some_and(|d| d >= PLAYNITE_WINDOW_GRACE)
}

// ───────────── Big Picture ─────────────

/// Big Picture is over when its window, once seen, is missing on two polls in a row (one miss is
/// often just Steam recreating it).
pub fn big_picture_exit(appeared: bool, absent_polls: u32) -> bool { appeared && absent_polls >= 2 }

// ───────────── Custom apps ─────────────

/// How long the watched process may be gone before the session ends (restarts, hand-offs).
pub const CUSTOM_EXIT_GRACE: Duration = Duration::from_secs(8);
/// After this long without the process ever showing up, the session stays until ended by hand.
pub const CUSTOM_APPEAR_TIMEOUT: Duration = Duration::from_secs(90);

pub fn custom_exit(appeared: bool, running: bool, gone_for: Option<Duration>) -> bool {
    appeared && !running && gone_for.is_some_and(|d| d >= CUSTOM_EXIT_GRACE)
}

/// "C:\Games\RetroArch.exe" → "retroarch" (how processes are compared).
pub fn process_name(path_or_name: &str) -> String {
    let s = path_or_name.trim().trim_matches('"');
    let file = s.rsplit(['\\', '/']).next().unwrap_or(s);
    let file = file.strip_suffix(".exe").or_else(|| file.strip_suffix(".EXE")).unwrap_or(file);
    file.to_ascii_lowercase()
}

/// The process to watch: the one named by hand, else the launched exe.
pub fn watched_process(exe: &str, watch: &str) -> String {
    if watch.trim().is_empty() { process_name(exe) } else { process_name(watch) }
}

/// The launched program was only a launcher (a stub, a script) that started the real one and quit:
/// the first descendant that is not a console helper is what the session follows.
pub fn hand_off_target(processes: &[(u32, u32, String)], launched: u32) -> Option<String> {
    const HELPERS: [&str; 3] = ["conhost", "cmd", "werfault"];
    let mut seen = HashSet::from([launched]);
    let mut frontier = VecDeque::from([launched]);
    while let Some(parent) = frontier.pop_front() {
        for (pid, ppid, name) in processes {
            if *ppid != parent || *pid == parent || !seen.insert(*pid) { continue; }
            let n = process_name(name);
            if !HELPERS.contains(&n.as_str()) { return Some(n); }
            frontier.push_back(*pid);
        }
    }
    None
}

// ───────────── journal ─────────────

/// Written before anything on screen changes and deleted only once the desk is fully back. Left
/// behind, it tells the next start that a session died mid-way and must be undone first.
#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct Journal {
    /// Unix seconds.
    pub started_at: u64,
    /// This session entered FSE from the desktop and must put it back.
    pub exit_fse: bool,
    pub fse_home: Option<FseHomeRestore>,
    pub launcher: String,
    /// File (next to the journal) holding the display configuration from before the session.
    pub displays_backup: String,
    /// Default audio output before the session.
    pub audio_before: String,
    /// HDR was switched on by the session on this screen (stable id): switch it back off.
    pub hdr_turned_on: String,
    /// Mouse pointer position before the session.
    pub pointer: Option<(i32, i32)>,
    /// The user's RTSS frame limit from before the session; `Some` means RTSS was touched and the
    /// restore (or the recovery after a crash) puts it back.
    pub rtss: Option<RtssBackup>,
    /// Our FPS counter was written into RTSS's shared memory: the restore clears its slot.
    pub rtss_overlay: bool,
    /// Where the desktop icons were before the screens changed.
    pub desktop_icons: Vec<DesktopIcon>,
    /// The session hides the desktop icons (they were showing): the restore, or the recovery after
    /// a crash, shows them again. Written before they are hidden.
    pub desktop_icons_hidden: bool,
    /// The session menu turned off an HDR that was on before the session (stable id): the restore turns it back on.
    pub hdr_turned_off: String,
    /// VRR was switched on for the session: the setting's text from before, for the restore (1.7 VrrApplied).
    pub vrr: Option<VrrBackup>,
    /// The TV was controlled and has a going-back step (command and/or standby): the restore, or
    /// the recovery after a crash, sends it.
    pub tv: Option<crate::tv::TvRestore>,
}

/// Windows' VRR setting as it was before the session: `None` when the value was not there at all.
#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct VrrBackup {
    pub previous: Option<String>,
}

/// RTSS's global frame limit and whether its limiter was on, as the user had them (1.7 RtssBackup).
#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct RtssBackup {
    pub framerate_limit: i32,
    pub limiter_enabled: bool,
}

/// The first change to RTSS in a session saves the user's settings; later ones (the session menu,
/// a game profile) must not overwrite that backup with our own values (1.7 NeedsRtssBackup).
pub fn needs_rtss_backup(already_applied: bool) -> bool { !already_applied }

#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
pub struct FseHomeRestore {
    pub previous: Option<String>,
}

impl Journal {
    pub fn now() -> u64 { SystemTime::now().duration_since(UNIX_EPOCH).map(|d| d.as_secs()).unwrap_or(0) }
    /// Retain failed restoration steps for the next recovery attempt.
    pub fn remaining_after_restore(&self, screens: bool, audio: bool, fse: bool, home: bool) -> Self {
        let mut rest = self.clone();
        rest.exit_fse = !fse;
        if home { rest.fse_home = None; }
        if screens { rest.displays_backup.clear(); }
        if audio { rest.audio_before.clear(); }
        rest.rtss = None; rest.rtss_overlay = false; rest.desktop_icons.clear();
        rest.desktop_icons_hidden = false; rest.pointer = None; rest.vrr = None;
        rest.hdr_turned_on.clear(); rest.hdr_turned_off.clear(); rest.tv = None;
        rest
    }
}

pub fn write_journal(path: &Path, j: &Journal) -> io::Result<()> {
    if let Some(dir) = path.parent() { fs::create_dir_all(dir)?; }
    let tmp = path.with_extension("json.tmp");
    use std::io::Write;
    let mut file = fs::File::create(&tmp)?;
    file.write_all(serde_json::to_string_pretty(j).expect("journal serializes").as_bytes())?;
    file.sync_all()?;
    drop(file);
    fs::rename(tmp, path)
}

/// A journal left by a session that never got to its restore.
pub fn read_journal(path: &Path) -> Option<Journal> {
    serde_json::from_str(&fs::read_to_string(path).ok()?).ok()
}

pub fn delete_journal(path: &Path) -> io::Result<()> {
    match fs::remove_file(path) { Err(e) if e.kind() != io::ErrorKind::NotFound => Err(e), _ => Ok(()) }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn playnite() {
        assert!(playnite_exit(false, true, None, false), "process gone = over");
        assert!(!playnite_exit(true, true, None, false));
        assert!(!playnite_exit(true, false, Some(Duration::from_secs(60)), true), "window put away while a game runs");
        assert!(!playnite_exit(true, false, Some(Duration::from_secs(5)), false), "swapping windows");
        assert!(playnite_exit(true, false, Some(PLAYNITE_WINDOW_GRACE), false));
    }

    #[test]
    fn big_picture() {
        assert!(!big_picture_exit(false, 9), "never seen: still starting");
        assert!(!big_picture_exit(true, 1));
        assert!(big_picture_exit(true, 2));
    }

    #[test]
    fn custom_apps() {
        assert_eq!(process_name(r#""C:\Games\RetroArch.exe""#), "retroarch");
        assert_eq!(watched_process(r"C:\x\launcher.exe", ""), "launcher");
        assert_eq!(watched_process(r"C:\x\launcher.exe", "Game.exe"), "game");
        assert!(!custom_exit(false, false, Some(Duration::from_secs(99))), "never appeared");
        assert!(!custom_exit(true, false, Some(Duration::from_secs(3))));
        assert!(custom_exit(true, false, Some(CUSTOM_EXIT_GRACE)));
    }

    #[test]
    fn hand_off() {
        let tree = vec![
            (10, 1, "launcher.exe".to_string()),
            (11, 10, "conhost.exe".to_string()),
            (12, 10, "cmd.exe".to_string()),
            (13, 12, "Game.exe".to_string()),
            (14, 99, "other.exe".to_string()),
        ];
        assert_eq!(hand_off_target(&tree, 10).as_deref(), Some("game"));
        assert_eq!(hand_off_target(&tree, 14), None);
    }

    #[test]
    fn a_journal_from_before_the_extras_still_reads() {
        // alpha journals had no rtss / icons fields: they must not stop the recovery
        let j: Journal = serde_json::from_str(r#"{"startedAt": 1, "launcher": "bigPicture", "displaysBackup": "x.ccd", "audioBefore": "", "hdrTurnedOn": "", "pointer": null}"#).unwrap();
        assert_eq!((j.rtss, j.rtss_overlay, j.desktop_icons.len()), (None, false, 0));
        assert_eq!((j.vrr, j.hdr_turned_off.as_str(), j.tv), (None, "", None));
        assert!(!j.desktop_icons_hidden);
    }

    #[test]
    fn recovery_keeps_each_failed_step_without_repeating_successes() {
        let original = Journal { displays_backup: "display.ccd".into(), audio_before: "audio".into(), exit_fse: true,
            fse_home: Some(FseHomeRestore { previous: Some("previous".into()) }), ..Default::default() };
        let pending = original.remaining_after_restore(true, false, false, false);
        assert!(pending.displays_backup.is_empty());
        assert_eq!(pending.audio_before, "audio");
        assert!(pending.exit_fse && pending.fse_home.is_some());
        let pending = pending.remaining_after_restore(true, true, true, false);
        assert!(pending.audio_before.is_empty());
        assert!(!pending.exit_fse && pending.fse_home.is_some());
        assert!(pending.remaining_after_restore(true, true, true, true).fse_home.is_none());
        let failed_display = original.remaining_after_restore(false, true, true, true);
        assert_eq!(failed_display.displays_backup, "display.ccd");
        assert!(failed_display.audio_before.is_empty());
    }

    #[test]
    fn rtss_is_backed_up_only_once_per_session() {
        assert!(needs_rtss_backup(false));
        assert!(!needs_rtss_backup(true));
    }

    #[test]
    fn journal_round_trip() {
        let dir = std::env::temp_dir().join(format!("cm2-journal-{}", std::process::id()));
        let p = dir.join("session.json");
        let j = Journal {
            started_at: 5, launcher: "playnite".into(), audio_before: "{0.0.0}".into(), pointer: Some((10, 20)),
            rtss: Some(RtssBackup { framerate_limit: 60, limiter_enabled: true }), rtss_overlay: true,
            desktop_icons: vec![DesktopIcon { name: "::{x}".into(), x: 1, y: 2 }], desktop_icons_hidden: true,
            hdr_turned_off: r"MONITOR\X".into(), vrr: Some(VrrBackup { previous: Some("A=1;".into()) }),
            tv: Some(crate::tv::TvRestore { provider: "androidTv".into(), host: "10.0.0.2".into(), restore_command: "input keyevent 3".into(), standby: true }), ..Default::default()
        };
        write_journal(&p, &j).unwrap();
        assert_eq!(read_journal(&p), Some(j.clone()));
        let updated = j.remaining_after_restore(true, false, true, true);
        write_journal(&p, &updated).unwrap();
        assert_eq!(read_journal(&p), Some(updated));
        delete_journal(&p).unwrap();
        assert_eq!(read_journal(&p), None);
        delete_journal(&p).unwrap(); // twice is fine
        let _ = fs::remove_dir_all(dir);
    }
}
