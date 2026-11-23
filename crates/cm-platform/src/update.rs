//! Alpha updates: look for a newer `v2.*` pre-release, download its setup, verify it, run it.
//! The rules (what counts as newer, which assets, the hash) are in `cm_core::update`; this is the
//! part that touches the network and the disk. Network only on a user action or the startup check.

use std::{os::windows::process::CommandExt, path::{Path, PathBuf}, process::Command};

pub use cm_core::update::Update;
use cm_core::update::{self, INSTALLER_ARGS, SETUP_ASSET};

const MAX_LIST: usize = 4 << 20;
const MAX_CHECKSUM: usize = 4 << 10;
const MAX_SETUP: u64 = 400 << 20;
const DETACHED_PROCESS: u32 = 0x0000_0008;

fn user_agent(current: &str) -> String { format!("ConsoleMode2Alpha/{current}") }

/// The newest alpha above `current`, or None when up to date. Blocking: call off the UI thread.
pub fn check(current: &str) -> Result<Option<Update>, String> {
    let body = crate::http::get(&update::releases_url(), &user_agent(current), "application/vnd.github+json", MAX_LIST)?;
    update::pick(&String::from_utf8_lossy(&body), current)
}

/// Downloads the setup and its `.sha256`, and returns the path only if the hash matches.
/// A file that does not match is deleted; nothing unverified is ever left to run.
pub fn download(u: &Update, current: &str, progress: &mut dyn FnMut(f32)) -> Result<PathBuf, String> {
    let ua = user_agent(current);
    if !update::is_trusted_url(&u.setup_url) || !update::is_trusted_url(&u.checksum_url) { return Err("endereço de download não confiável".into()); }
    let checksum = crate::http::get(&u.checksum_url, &ua, "application/octet-stream", MAX_CHECKSUM)?;
    let expected = update::expected_hash(&String::from_utf8_lossy(&checksum), u.github_digest.as_deref())?;

    let dir = std::env::temp_dir().join("ConsoleMode2Update");
    let _ = std::fs::remove_dir_all(&dir);
    std::fs::create_dir_all(&dir).map_err(|e| format!("pasta temporária: {e}"))?;
    let part = dir.join(format!("{SETUP_ASSET}.part"));
    let file = dir.join(SETUP_ASSET);
    let result = crate::http::download(&u.setup_url, &ua, &part, MAX_SETUP, &mut |done, total| {
        if let Some(t) = total.filter(|t| *t > 0) { progress((done as f64 / t as f64) as f32); }
    });
    if let Err(e) = result { let _ = std::fs::remove_file(&part); return Err(e); }
    let actual = update::sha256_file(&part).map_err(|e| format!("ler o download: {e}"));
    if actual.as_deref() != Ok(expected.as_str()) {
        let _ = std::fs::remove_file(&part);
        return Err("o arquivo baixado não confere com o SHA-256 publicado".into());
    }
    std::fs::rename(&part, &file).map_err(|e| format!("guardar o instalador: {e}"))?;
    Ok(file)
}

/// Starts the verified setup silently. The caller must exit the app right after (the setup also
/// closes it through Restart Manager and opens it again when done).
pub fn launch_installer(setup: &Path) -> Result<(), String> {
    Command::new(setup).args(INSTALLER_ARGS).creation_flags(DETACHED_PROCESS).spawn().map(|_| ()).map_err(|e| format!("abrir o instalador: {e}"))
}
