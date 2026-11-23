//! ControlFS and JoyChromium, the team's two controller-first apps that the session menu opens over
//! the game (1.7 ControlFsLocator, ControlFsRelease, ControlFsService, JoyChromium and JoyChromiumService):
//!
//! * ControlFS, a file explorer: found through its uninstall entry or the usual folders, started with
//!   `--start` (or the open window brought forward), then put in full screen on the session screen;
//! * JoyChromium, a browser for the TV: found in its per-user folder or through its uninstall key's icon,
//!   started with `--tv` (full screen for this run only); going back to the PC hands it `--window`, so it
//!   stays open as a window, tabs and all. One it was not opened from the menu is left alone.
//!
//! Neither is ever closed from Console Mode. When one is missing, pressing its row downloads the setup of
//! its newest release, checks it against GitHub's SHA-256 digest and runs it silently (per user, no admin
//! prompt): only when the user asks, from the menu. A failed install makes the next press open the
//! release page instead. Pure, so it is tested; the OS side is `cm_platform::helper_apps`.

use std::time::Duration;

use serde_json::Value;

/// One of the two apps.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub enum App { ControlFs, JoyChromium }

pub const APPS: [App; 2] = [App::ControlFs, App::JoyChromium];

/// The setup is given this long, then stopped (its child process too), as in 1.7.
pub const SETUP_TIMEOUT: Duration = Duration::from_secs(5 * 60);

/// ControlFS's exe, in the folder it was installed to.
pub const CONTROLFS_EXE: &str = "ControlFS.exe";
/// ControlFS's only setup (x64).
pub const CONTROLFS_SETUP: &str = "ControlFS-Setup-x64.exe";
/// JoyChromium's uninstall key under HKCU: a UUID derived from its appId, the same in every version.
pub const JOYCHROMIUM_UNINSTALL_KEY: &str = r"Software\Microsoft\Windows\CurrentVersion\Uninstall\1fc20e06-e3c1-52c5-9ba1-fcb355e6851d";
/// Back to a window, for this run only; handed to the open instance when going back to the PC.
pub const JOYCHROMIUM_WINDOW_ARG: &str = "--window";

impl App {
    /// The name in logs and in the diagnostic report.
    pub fn name(self) -> &'static str { match self { App::ControlFs => "ControlFS", App::JoyChromium => "JoyChromium" } }

    /// The process to look for (exe name without `.exe`).
    pub fn process(self) -> &'static str { self.name() }

    pub fn repository(self) -> &'static str { match self { App::ControlFs => "nextestudios/ControlFS", App::JoyChromium => "lippdev/joychromium" } }

    /// What opens in the browser when it can't be installed from the menu.
    pub fn releases_page(self) -> String {
        match self {
            App::ControlFs => format!("https://github.com/{}/releases/latest", self.repository()),
            App::JoyChromium => format!("https://github.com/{}/releases", self.repository()),
        }
    }

    /// GitHub's list of its releases, newest first.
    pub fn releases_api(self) -> String { format!("https://api.github.com/repos/{}/releases?per_page=10", self.repository()) }

    /// Downloads come from this repository's release assets only.
    pub fn is_trusted_url(self, url: &str) -> bool { url.starts_with(&format!("https://github.com/{}/releases/download/", self.repository())) }

    /// How it starts from the menu: ControlFS opens (or comes to the front, one instance per user);
    /// JoyChromium opens full screen for this run only, or hands that to the open one.
    pub fn open_args(self) -> &'static [&'static str] { match self { App::ControlFs => &["--start"], App::JoyChromium => &["--tv"] } }

    /// Silent, per user, no reboot: Inno Setup for ControlFS, NSIS one-click for JoyChromium.
    pub fn silent_args(self) -> &'static [&'static str] {
        match self { App::ControlFs => &["/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART"], App::JoyChromium => &["/S"] }
    }

    /// A process of a setup still at work (Inno's child is "ControlFS-Setup-x64.tmp"; JoyChromium's
    /// setup is "JoyChromium-0.1.3-alpha-win-x64", the browser itself has no dash): never a second one.
    pub fn is_setup_process(self, exe: &str) -> bool {
        let prefix = match self { App::ControlFs => "controlfs-setup", App::JoyChromium => "joychromium-" };
        exe.to_ascii_lowercase().starts_with(prefix)
    }

    /// The setup asset for this PC: ControlFS has only x64; JoyChromium's per processor, not the both-in-one "-win.exe".
    pub fn is_setup_asset(self, name: &str, arm64: bool) -> bool {
        let lower = name.to_ascii_lowercase();
        match self {
            App::ControlFs => lower == CONTROLFS_SETUP.to_ascii_lowercase(),
            App::JoyChromium => lower.starts_with("joychromium-") && lower.ends_with(if arm64 { "-win-arm64.exe" } else { "-win-x64.exe" }),
        }
    }
}

/// The setup of one release: what to download and the SHA-256 it must have (`sha256:<hex>`).
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Setup { pub version: String, pub url: String, pub name: String, pub digest: String, pub size: u64 }

/// GitHub's `sha256:<64 hex>` digest of an asset.
pub fn is_valid_sha256_digest(digest: &str) -> bool {
    digest.len() == 71 && digest[..7].eq_ignore_ascii_case("sha256:") && digest[7..].bytes().all(|b| b.is_ascii_hexdigit())
}

/// The expected hash of a digest, lowercase hex.
pub fn digest_hex(digest: &str) -> Option<String> { is_valid_sha256_digest(digest).then(|| digest[7..].to_ascii_lowercase()) }

/// The setup of the newest release (GitHub lists them newest first); drafts don't count, pre-releases do
/// (JoyChromium only has alphas). The newest release with the setup decides: without a trusted URL and
/// GitHub's digest nothing is run, and the release page opens instead.
pub fn pick_setup(app: App, releases_json: &str, arm64: bool) -> Option<Setup> {
    let list: Value = serde_json::from_str(releases_json).ok()?;
    for release in list.as_array()? {
        if !release.is_object() || release.get("draft").and_then(Value::as_bool) == Some(true) { continue; }
        let Some(assets) = release.get("assets").and_then(Value::as_array) else { continue };
        for asset in assets {
            let text = |k: &str| asset.get(k).and_then(Value::as_str).unwrap_or("");
            let name = text("name");
            if !app.is_setup_asset(name, arm64) { continue; }
            let (url, digest) = (text("browser_download_url"), text("digest"));
            if !app.is_trusted_url(url) || !is_valid_sha256_digest(digest) { return None; }
            let tag = release.get("tag_name").and_then(Value::as_str).unwrap_or("");
            return Some(Setup {
                version: tag.trim_start_matches(['v', 'V']).to_string(), url: url.into(), name: name.into(), digest: digest.into(),
                size: asset.get("size").and_then(Value::as_u64).unwrap_or(0),
            });
        }
    }
    None
}

/// A path as typed or stored: no blanks, no quotes around it.
fn clean(path: &str) -> &str { path.trim().trim_matches('"').trim() }

/// Where ControlFS may be, most reliable first (its uninstall entry's InstallLocation, then the per-user
/// and the machine-wide folders), without blanks or repeats.
pub fn controlfs_folders(install_location: Option<&str>, local_app_data: Option<&str>, program_files: Option<&str>) -> Vec<String> {
    let mut folders: Vec<String> = Vec::new();
    let mut add = |path: Option<String>| {
        let Some(path) = path else { return };
        let c = clean(&path).trim_end_matches(['\\', '/']);
        if !c.is_empty() && !folders.iter().any(|f| f.eq_ignore_ascii_case(c)) { folders.push(c.to_string()); }
    };
    add(install_location.map(str::to_string));
    add(local_app_data.filter(|l| !l.trim().is_empty()).map(|l| format!(r"{}\Programs\ControlFS", l.trim_end_matches(['\\', '/']))));
    add(program_files.filter(|p| !p.trim().is_empty()).map(|p| format!(r"{}\ControlFS", p.trim_end_matches(['\\', '/']))));
    folders
}

/// The first folder that really has ControlFS.exe, or None (not installed).
pub fn controlfs_exe(folders: &[String], exists: impl Fn(&str) -> bool) -> Option<String> {
    folders.iter().map(|f| format!(r"{f}\{CONTROLFS_EXE}")).find(|exe| exists(exe))
}

/// Where JoyChromium's per-user setup puts it.
pub fn joychromium_default_path(local_app_data: &str) -> String {
    format!(r"{}\Programs\joychromium\JoyChromium.exe", local_app_data.trim_end_matches(['\\', '/']))
}

/// The exe in an uninstall key's DisplayIcon (`"C:\…\JoyChromium.exe",0`): its InstallLocation comes
/// empty, so the icon is the path to trust. None when it isn't an exe.
pub fn exe_from_display_icon(display_icon: &str) -> Option<String> {
    let mut value = display_icon.trim();
    if value.is_empty() { return None; }
    // an icon index after the path (",0", ",-101"), outside the quotes when there are any
    if let Some(comma) = value.rfind(',') {
        if comma > 0 && value[comma + 1..].trim().parse::<i32>().is_ok() { value = &value[..comma]; }
    }
    let value = clean(value);
    value.to_ascii_lowercase().ends_with(".exe").then(|| value.to_string())
}

/// The first JoyChromium that exists: the default folder, then the uninstall key's icon path.
pub fn joychromium_exe(default_path: Option<&str>, display_icon: Option<&str>, exists: impl Fn(&str) -> bool) -> Option<String> {
    let from_icon = display_icon.and_then(exe_from_display_icon);
    [default_path.map(str::to_string), from_icon].into_iter().flatten().find(|c| !c.trim().is_empty() && exists(c))
}

/// Full screen is exactly the screen: a maximized window hangs about 8 px over each edge, so it does
/// not count. A pixel of tolerance for rounding. Rects are (x, y, width, height).
pub fn is_full_screen(window: (i32, i32, i32, i32), screen: (i32, i32, i32, i32)) -> bool {
    screen.2 > 0 && screen.3 > 0
        && (window.0 - screen.0).abs() <= 1 && (window.1 - screen.1).abs() <= 1
        && (window.2 - screen.2).abs() <= 1 && (window.3 - screen.3).abs() <= 1
}

/// Going back to the PC with the JoyChromium the menu opened still running: it leaves full screen and
/// stays (tabs and all). One the user had open on the desk on their own is left alone.
pub fn should_return_to_window(opened_from_menu: bool, running: bool) -> bool { opened_from_menu && running }

/// What a press on the app's row does.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Press {
    /// Installing: nothing, the row shows the progress.
    Busy,
    /// Missing: download and install it (the next press opens it).
    Install,
    /// The menu closes and the app opens over the game (if that fails: its release page).
    Open,
    /// The install failed: its release page opens in the browser.
    ReleasePage,
}

/// What the row says.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Status {
    /// The setup is running (no percentage any more).
    Installing,
    /// Downloading, in percent.
    Downloading(u32),
    /// Installed by the last press: the next one opens it.
    JustInstalled,
    Ready,
    Failed,
    Missing,
}

/// One app's row in the session menu (1.7 MainViewModel.SessionMenu / .JoyChromium).
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct Row {
    pub installed: bool,
    /// The install asked from the row is running (it goes on if the menu closes meanwhile).
    pub installing: bool,
    /// The download, 0 to 1.
    pub progress: f32,
    pub setup_running: bool,
    pub failed: bool,
    pub just_installed: bool,
}

impl Row {
    /// The menu opened: installed or not is read again, and a failed install is offered again.
    pub fn menu_opened(&mut self, installed: bool) {
        self.installed = installed;
        if !self.installing { self.failed = false; }
        self.just_installed = false;
    }

    /// A press, with `installed` read right now.
    pub fn press(&mut self, installed: bool) -> Press {
        if self.installing { return Press::Busy; }
        self.installed = installed;
        if installed { Press::Open } else if self.failed { Press::ReleasePage } else { Press::Install }
    }

    pub fn install_started(&mut self) { self.installing = true; self.progress = 0.; self.setup_running = false; }

    /// Download progress (0..1), or None once the setup itself runs. A late report after the end is ignored.
    pub fn install_progress(&mut self, progress: Option<f32>) {
        if !self.installing { return; }
        match progress { Some(p) => self.progress = p.clamp(0., 1.), None => self.setup_running = true }
    }

    pub fn install_finished(&mut self, ok: bool) {
        self.installing = false;
        self.setup_running = false;
        if ok { self.installed = true; self.just_installed = true; self.failed = false; } else { self.failed = true; }
    }

    pub fn status(&self) -> Status {
        if self.setup_running { Status::Installing }
        else if self.installing { Status::Downloading((self.progress * 100.) as u32) }
        else if self.just_installed { Status::JustInstalled }
        else if self.installed { Status::Ready }
        else if self.failed { Status::Failed }
        else { Status::Missing }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn digest() -> String { format!("sha256:{}", "Ab".repeat(32)) }

    fn release(tag: &str, draft: bool, assets: &[(&str, &str, Option<String>)]) -> String {
        let assets: Vec<String> = assets.iter().map(|(name, url, d)| match d {
            Some(d) => format!(r#"{{"name":"{name}","browser_download_url":"{url}","digest":"{d}","size":1234}}"#),
            None => format!(r#"{{"name":"{name}","browser_download_url":"{url}"}}"#),
        }).collect();
        format!(r#"{{"tag_name":"{tag}","draft":{draft},"prerelease":true,"assets":[{}]}}"#, assets.join(","))
    }

    const CFS: &str = "https://github.com/nextestudios/ControlFS/releases/download/v1.2.0/ControlFS-Setup-x64.exe";
    const JOY_X64: &str = "https://github.com/lippdev/joychromium/releases/download/v0.1.3-alpha/JoyChromium-0.1.3-alpha-win-x64.exe";
    const JOY_ARM: &str = "https://github.com/lippdev/joychromium/releases/download/v0.1.3-alpha/JoyChromium-0.1.3-alpha-win-arm64.exe";
    const JOY_BOTH: &str = "https://github.com/lippdev/joychromium/releases/download/v0.1.3-alpha/JoyChromium-0.1.3-alpha-win.exe";

    #[test]
    fn the_newest_published_controlfs_setup_is_picked() {
        let json = format!("[{},{}]",
            release("v1.3.0", true, &[("ControlFS-Setup-x64.exe", CFS, Some(digest()))]),
            release("v1.2.0", false, &[("ControlFS-Portable.zip", CFS, Some(digest())), ("ControlFS-Setup-x64.exe", CFS, Some(digest()))]));
        let s = pick_setup(App::ControlFs, &json, false).unwrap();
        assert_eq!((s.version.as_str(), s.url.as_str(), s.name.as_str(), s.size), ("1.2.0", CFS, CONTROLFS_SETUP, 1234));
        assert_eq!(digest_hex(&s.digest).unwrap(), "ab".repeat(32));
    }

    #[test]
    fn without_a_digest_or_from_elsewhere_nothing_is_installed() {
        let no_digest = format!("[{},{}]", release("v1.3.0", false, &[("ControlFS-Setup-x64.exe", CFS, None)]), release("v1.2.0", false, &[("ControlFS-Setup-x64.exe", CFS, Some(digest()))]));
        assert_eq!(pick_setup(App::ControlFs, &no_digest, false), None, "the newest release decides");
        let elsewhere = format!("[{}]", release("v1.3.0", false, &[("ControlFS-Setup-x64.exe", "https://example.com/ControlFS-Setup-x64.exe", Some(digest()))]));
        assert_eq!(pick_setup(App::ControlFs, &elsewhere, false), None);
        // the right repository's name in another one's path
        let fork = format!("[{}]", release("v1", false, &[("ControlFS-Setup-x64.exe", "https://github.com/evil/ControlFS/releases/download/nextestudios/ControlFS/releases/download/x.exe", Some(digest()))]));
        assert_eq!(pick_setup(App::ControlFs, &fork, false), None);
        let bad_digest = format!("[{}]", release("v1", false, &[("ControlFS-Setup-x64.exe", CFS, Some("sha256:xyz".into()))]));
        assert_eq!(pick_setup(App::ControlFs, &bad_digest, false), None);
        assert_eq!(pick_setup(App::ControlFs, "not json", false), None);
        assert_eq!(pick_setup(App::ControlFs, "{}", false), None);
    }

    #[test]
    fn joychromium_takes_the_setup_for_this_processor() {
        let json = format!("[{}]", release("v0.1.3-alpha", false, &[
            ("JoyChromium-0.1.3-alpha-win.exe", JOY_BOTH, Some(digest())),
            ("JoyChromium-0.1.3-alpha-win-arm64.exe", JOY_ARM, Some(digest())),
            ("JoyChromium-0.1.3-alpha-win-x64.exe", JOY_X64, Some(digest())),
        ]));
        assert_eq!(pick_setup(App::JoyChromium, &json, false).unwrap().url, JOY_X64);
        let arm = pick_setup(App::JoyChromium, &json, true).unwrap();
        assert_eq!((arm.url.as_str(), arm.version.as_str()), (JOY_ARM, "0.1.3-alpha"));
        // ControlFS's rules don't take JoyChromium's assets, nor the other way round
        assert_eq!(pick_setup(App::ControlFs, &json, false), None);
        assert!(!App::JoyChromium.is_trusted_url(CFS));
    }

    #[test]
    fn digests_are_sha256_with_64_hex_digits() {
        assert!(is_valid_sha256_digest(&digest()));
        assert!(is_valid_sha256_digest(&format!("SHA256:{}", "0".repeat(64))));
        assert!(!is_valid_sha256_digest(&"0".repeat(64)));
        assert!(!is_valid_sha256_digest(&format!("sha256:{}", "0".repeat(63))));
        assert!(!is_valid_sha256_digest(&format!("sha512:{}", "0".repeat(64))));
        assert!(!is_valid_sha256_digest(""));
    }

    #[test]
    fn controlfs_is_looked_for_in_its_uninstall_entry_first_without_repeats() {
        let f = controlfs_folders(Some(r#" "C:\Users\a\AppData\Local\Programs\ControlFS\" "#), Some(r"C:\Users\a\AppData\Local"), Some(r"C:\Program Files"));
        assert_eq!(f, vec![r"C:\Users\a\AppData\Local\Programs\ControlFS".to_string(), r"C:\Program Files\ControlFS".to_string()]);
        assert_eq!(controlfs_folders(None, None, Some("  ")), Vec::<String>::new());
        assert_eq!(controlfs_folders(Some(r"D:\Apps\CFS"), None, None), vec![r"D:\Apps\CFS".to_string()]);
    }

    #[test]
    fn the_first_folder_with_the_exe_wins() {
        let folders = vec![r"D:\Old".to_string(), r"C:\New".to_string()];
        assert_eq!(controlfs_exe(&folders, |p| p == r"C:\New\ControlFS.exe").as_deref(), Some(r"C:\New\ControlFS.exe"));
        assert_eq!(controlfs_exe(&folders, |_| false), None);
    }

    #[test]
    fn joychromium_is_found_by_its_folder_or_its_icon() {
        assert_eq!(joychromium_default_path(r"C:\Users\a\AppData\Local\"), r"C:\Users\a\AppData\Local\Programs\joychromium\JoyChromium.exe");
        assert_eq!(exe_from_display_icon(r#""C:\J\JoyChromium.exe",0"#).as_deref(), Some(r"C:\J\JoyChromium.exe"));
        assert_eq!(exe_from_display_icon(r"C:\J\JoyChromium.exe,-101").as_deref(), Some(r"C:\J\JoyChromium.exe"));
        assert_eq!(exe_from_display_icon(r"C:\J, the folder\JoyChromium.exe").as_deref(), Some(r"C:\J, the folder\JoyChromium.exe"));
        assert_eq!(exe_from_display_icon(r"C:\J\icon.ico,0"), None);
        assert_eq!(exe_from_display_icon("  "), None);
        let default = r"C:\L\Programs\joychromium\JoyChromium.exe";
        assert_eq!(joychromium_exe(Some(default), Some(r#""D:\J\JoyChromium.exe",0"#), |p| p == default).as_deref(), Some(default));
        assert_eq!(joychromium_exe(Some(default), Some(r#""D:\J\JoyChromium.exe",0"#), |p| p.starts_with('D')).as_deref(), Some(r"D:\J\JoyChromium.exe"));
        assert_eq!(joychromium_exe(None, None, |_| true), None);
    }

    #[test]
    fn full_screen_is_exactly_the_screen() {
        let screen = (1920, 0, 3840, 2160);
        assert!(is_full_screen((1920, 0, 3840, 2160), screen));
        assert!(is_full_screen((1921, -1, 3839, 2161), screen), "a pixel of rounding");
        assert!(!is_full_screen((1912, -8, 3856, 2176), screen), "maximized hangs over the edges");
        assert!(!is_full_screen((0, 0, 1920, 1080), screen));
        assert!(!is_full_screen((0, 0, 0, 0), (0, 0, 0, 0)));
    }

    #[test]
    fn only_the_joychromium_opened_from_the_menu_goes_back_to_a_window() {
        assert!(should_return_to_window(true, true));
        assert!(!should_return_to_window(true, false));
        assert!(!should_return_to_window(false, true));
    }

    #[test]
    fn setups_and_arguments_follow_each_app() {
        assert!(App::ControlFs.is_setup_process("ControlFS-Setup-x64.tmp") && !App::ControlFs.is_setup_process("ControlFS.exe"));
        assert!(App::JoyChromium.is_setup_process("JoyChromium-0.1.3-alpha-win-x64.exe") && !App::JoyChromium.is_setup_process("JoyChromium.exe"));
        assert_eq!(App::ControlFs.open_args(), ["--start"]);
        assert_eq!(App::JoyChromium.open_args(), ["--tv"]);
        assert_eq!(App::JoyChromium.silent_args(), ["/S"]);
        assert!(App::ControlFs.silent_args().contains(&"/VERYSILENT"));
        assert_eq!(App::ControlFs.releases_page(), "https://github.com/nextestudios/ControlFS/releases/latest");
        assert_eq!(App::JoyChromium.releases_api(), "https://api.github.com/repos/lippdev/joychromium/releases?per_page=10");
    }

    #[test]
    fn a_missing_app_installs_on_the_first_press_and_opens_on_the_next() {
        let mut row = Row::default();
        row.menu_opened(false);
        assert_eq!(row.status(), Status::Missing);
        assert_eq!(row.press(false), Press::Install);
        row.install_started();
        assert_eq!(row.press(false), Press::Busy, "a second press while it installs does nothing");
        row.install_progress(Some(0.42));
        assert_eq!(row.status(), Status::Downloading(42));
        row.install_progress(None);
        assert_eq!(row.status(), Status::Installing);
        row.install_finished(true);
        assert_eq!(row.status(), Status::JustInstalled);
        assert_eq!(row.press(true), Press::Open);
        row.menu_opened(true);
        assert_eq!(row.status(), Status::Ready);
    }

    #[test]
    fn a_failed_install_opens_the_release_page_until_the_menu_opens_again() {
        let mut row = Row::default();
        row.install_started();
        row.install_finished(false);
        row.install_progress(Some(0.9));
        assert_eq!(row.status(), Status::Failed, "a late report after the end is ignored");
        assert_eq!(row.press(false), Press::ReleasePage);
        row.menu_opened(false);
        assert_eq!(row.press(false), Press::Install, "offered again");
        // installed by hand meanwhile: it just opens
        let mut row = Row { failed: true, ..Row::default() };
        assert_eq!(row.press(true), Press::Open);
    }

    #[test]
    fn the_menu_opening_keeps_an_install_that_is_still_running() {
        let mut row = Row::default();
        row.install_started();
        row.failed = true;
        row.menu_opened(false);
        assert!(row.installing && row.failed, "the failure flag is left to the running install");
    }
}
