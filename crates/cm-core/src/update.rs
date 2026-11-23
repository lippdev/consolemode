//! Alpha update channel of Console Mode 2 (pure logic; the network lives in cm-platform).
//!
//! Releases come from GitHub (`REPOSITORY`). Only tags `v2.*` that are pre-releases and carry both
//! the setup and its `.sha256` are ever offered, so 2.0 never offers 1.7 and 1.x tags are ignored.
//! The idea is the one of 1.7's `UpdateIntegrity.cs`: nothing is run before its SHA-256 matches.
//! Asset naming and how to cut an alpha: docs/v2/RELEASE.md.

use std::{fs::File, io::Read, path::Path};

use semver::Version;
use serde_json::Value;
use sha2::{Digest, Sha256};

pub const REPOSITORY: &str = "lippdev/consolemode";
/// The installer of every alpha; the checksum is `<SETUP_ASSET>.sha256`.
pub const SETUP_ASSET: &str = "ConsoleMode2Alpha-Setup-x64.exe";
pub const CHECKSUM_ASSET: &str = "ConsoleMode2Alpha-Setup-x64.exe.sha256";
/// Only this major version is ever offered.
pub const MAJOR: u64 = 2;
/// What Inno Setup gets to update in place: silent, closes the running app, no reboot.
pub const INSTALLER_ARGS: [&str; 4] = ["/SILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/CLOSEAPPLICATIONS"];

/// A release that is newer than the running one and safe to install.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Update {
    pub version: String,
    pub tag: String,
    pub notes: String,
    pub page_url: String,
    pub setup_url: String,
    pub checksum_url: String,
    /// GitHub's own `sha256:` digest of the setup, when it publishes one (cross-checked with the `.sha256`).
    pub github_digest: Option<String>,
}

pub fn releases_url() -> String { format!("https://api.github.com/repos/{REPOSITORY}/releases?per_page=30") }

/// Downloads must come from this repository's release assets, never from anywhere else.
pub fn is_trusted_url(url: &str) -> bool { url.starts_with(&format!("https://github.com/{REPOSITORY}/releases/download/")) }

/// `v2.0.0-alpha.3` → 2.0.0-alpha.3; anything that is not a `v2.x` pre-release tag → None.
pub fn parse_tag(tag: &str) -> Option<Version> {
    let v = Version::parse(tag.strip_prefix('v')?).ok()?;
    (v.major == MAJOR && !v.pre.is_empty()).then_some(v)
}

/// The newest alpha above `current` in a GitHub "list releases" response, or None when up to date.
pub fn pick(releases_json: &str, current: &str) -> Result<Option<Update>, String> {
    let current = Version::parse(current).map_err(|e| format!("versão atual inválida '{current}': {e}"))?;
    let list: Value = serde_json::from_str(releases_json).map_err(|e| format!("resposta do GitHub ilegível: {e}"))?;
    let list = list.as_array().ok_or("resposta do GitHub inesperada")?;
    let mut best: Option<(Version, Update)> = None;
    for r in list {
        let text = |k: &str| r.get(k).and_then(Value::as_str).unwrap_or("");
        if r.get("draft").and_then(Value::as_bool).unwrap_or(true) { continue; }
        if !r.get("prerelease").and_then(Value::as_bool).unwrap_or(false) { continue; }
        let Some(version) = parse_tag(text("tag_name")) else { continue };
        if version <= current || best.as_ref().is_some_and(|(b, _)| version <= *b) { continue; }
        let Some((setup, digest)) = asset(r, SETUP_ASSET) else { continue };
        let Some((checksum, _)) = asset(r, CHECKSUM_ASSET) else { continue };
        let page = if text("html_url").is_empty() { format!("https://github.com/{REPOSITORY}/releases") } else { text("html_url").to_string() };
        best = Some((version.clone(), Update {
            version: version.to_string(), tag: text("tag_name").to_string(), notes: text("body").to_string(), page_url: page,
            setup_url: setup, checksum_url: checksum, github_digest: digest,
        }));
    }
    Ok(best.map(|b| b.1))
}

/// An uploaded asset by exact name: its download URL (trusted ones only) and GitHub's digest.
fn asset(release: &Value, name: &str) -> Option<(String, Option<String>)> {
    release.get("assets")?.as_array()?.iter().find_map(|a| {
        if a.get("name")?.as_str()? != name { return None; }
        if a.get("state").and_then(Value::as_str).is_some_and(|s| s != "uploaded") { return None; }
        let url = a.get("browser_download_url")?.as_str()?;
        is_trusted_url(url).then(|| (url.to_string(), a.get("digest").and_then(Value::as_str).map(str::to_string)))
    })
}

/// The hash a download must have: from the `.sha256` file (`<hex>  <name>`, `sha256sum` style, or just
/// `<hex>`), and equal to GitHub's own digest when GitHub published one. Lowercase hex.
pub fn expected_hash(checksum_file: &str, github_digest: Option<&str>) -> Result<String, String> {
    let line = checksum_file.lines().map(str::trim).find(|l| !l.is_empty()).ok_or("arquivo .sha256 vazio")?;
    let mut parts = line.split_whitespace();
    let hash = parts.next().unwrap_or("").to_ascii_lowercase();
    if !is_sha256_hex(&hash) { return Err("arquivo .sha256 inválido".into()); }
    if let Some(name) = parts.next() {
        if name.trim_start_matches('*') != SETUP_ASSET { return Err("o .sha256 é de outro arquivo".into()); }
    }
    if let Some(d) = github_digest {
        let theirs = d.strip_prefix("sha256:").unwrap_or("").to_ascii_lowercase();
        if !is_sha256_hex(&theirs) || theirs != hash { return Err("o .sha256 não confere com o resumo do GitHub".into()); }
    }
    Ok(hash)
}

fn is_sha256_hex(s: &str) -> bool { s.len() == 64 && s.bytes().all(|b| b.is_ascii_hexdigit()) }

/// SHA-256 of a file, lowercase hex, read in chunks (the setup is tens of MB).
pub fn sha256_file(path: &Path) -> std::io::Result<String> {
    let mut f = File::open(path)?;
    let mut h = Sha256::new();
    let mut buf = vec![0u8; 1 << 16];
    loop {
        let n = f.read(&mut buf)?;
        if n == 0 { break; }
        h.update(&buf[..n]);
    }
    Ok(h.finalize().iter().map(|b| format!("{b:02x}")).collect())
}

#[cfg(test)]
mod tests {
    use super::*;

    const BASE: &str = "https://github.com/lippdev/consolemode/releases/download";

    fn release(tag: &str, prerelease: bool, draft: bool, with_checksum: bool) -> String {
        let mut assets = vec![format!(r#"{{"name":"{SETUP_ASSET}","state":"uploaded","browser_download_url":"{BASE}/{tag}/{SETUP_ASSET}","digest":"sha256:{}"}}"#, "ab".repeat(32))];
        if with_checksum { assets.push(format!(r#"{{"name":"{CHECKSUM_ASSET}","state":"uploaded","browser_download_url":"{BASE}/{tag}/{CHECKSUM_ASSET}"}}"#)); }
        format!(r#"{{"tag_name":"{tag}","prerelease":{prerelease},"draft":{draft},"html_url":"https://github.com/lippdev/consolemode/releases/tag/{tag}","body":"notes","assets":[{}]}}"#, assets.join(","))
    }
    fn list(rs: &[String]) -> String { format!("[{}]", rs.join(",")) }

    #[test]
    fn offers_a_newer_alpha() {
        let json = list(&[release("v2.0.0-alpha.2", true, false, true)]);
        let u = pick(&json, "2.0.0-alpha.0").unwrap().unwrap();
        assert_eq!((u.version.as_str(), u.tag.as_str()), ("2.0.0-alpha.2", "v2.0.0-alpha.2"));
        assert!(u.setup_url.ends_with(SETUP_ASSET) && u.checksum_url.ends_with(CHECKSUM_ASSET));
    }

    #[test]
    fn nothing_when_up_to_date_or_older() {
        let json = list(&[release("v2.0.0-alpha.1", true, false, true)]);
        assert_eq!(pick(&json, "2.0.0-alpha.1").unwrap(), None);
        assert_eq!(pick(&json, "2.0.0-alpha.3").unwrap(), None);
        assert_eq!(pick("[]", "2.0.0-alpha.0").unwrap(), None);
    }

    #[test]
    fn never_considers_1x_tags() {
        // even a "newer-looking" or mislabelled 1.x release is ignored
        let json = list(&[release("v1.7.0", false, false, true), release("v1.8.0-beta.1", true, false, true), release("v3.0.0-alpha.1", true, false, true)]);
        assert_eq!(pick(&json, "2.0.0-alpha.0").unwrap(), None);
        assert_eq!(parse_tag("v1.7.0"), None);
        assert_eq!(parse_tag("v1.8.0-beta.1"), None);
        assert_eq!(parse_tag("2.0.0-alpha.1"), None);
        assert!(parse_tag("v2.0.0-alpha.1").is_some());
    }

    #[test]
    fn only_prereleases_that_are_not_drafts_and_have_both_assets() {
        let json = list(&[
            release("v2.0.0-alpha.5", false, false, true), // not flagged pre-release
            release("v2.0.0-alpha.6", true, true, true),   // draft
            release("v2.0.0-alpha.7", true, false, false), // no .sha256
        ]);
        assert_eq!(pick(&json, "2.0.0-alpha.0").unwrap(), None);
    }

    #[test]
    fn picks_the_highest_not_the_first() {
        let json = list(&[release("v2.0.0-alpha.2", true, false, true), release("v2.0.0-alpha.10", true, false, true), release("v2.0.0-alpha.9", true, false, true)]);
        assert_eq!(pick(&json, "2.0.0-alpha.0").unwrap().unwrap().version, "2.0.0-alpha.10");
        // alpha < beta
        let json = list(&[release("v2.0.0-beta.1", true, false, true), release("v2.0.0-alpha.9", true, false, true)]);
        assert_eq!(pick(&json, "2.0.0-alpha.0").unwrap().unwrap().version, "2.0.0-beta.1");
    }

    #[test]
    fn rejects_downloads_from_elsewhere() {
        let json = release("v2.0.0-alpha.2", true, false, true).replace(BASE, "https://evil.example/lippdev/consolemode/releases/download");
        assert_eq!(pick(&list(&[json]), "2.0.0-alpha.0").unwrap(), None);
        assert!(!is_trusted_url("https://github.com/someone/else/releases/download/v2.0.0-alpha.1/x.exe"));
        assert!(!is_trusted_url("http://github.com/lippdev/consolemode/releases/download/v2/x.exe"));
    }

    #[test]
    fn bad_input_is_an_error() {
        assert!(pick("not json", "2.0.0-alpha.0").is_err());
        assert!(pick("{}", "2.0.0-alpha.0").is_err());
        assert!(pick("[]", "garbage").is_err());
    }

    #[test]
    fn checksum_file_formats() {
        let h = "ab".repeat(32);
        assert_eq!(expected_hash(&format!("{h}  {SETUP_ASSET}\n"), None).unwrap(), h);
        assert_eq!(expected_hash(&format!("{}  *{SETUP_ASSET}", h.to_uppercase()), None).unwrap(), h);
        assert_eq!(expected_hash(&h, None).unwrap(), h);
        assert!(expected_hash(&format!("{h}  other.exe"), None).is_err());
        assert!(expected_hash("zz", None).is_err());
        assert!(expected_hash("", None).is_err());
    }

    #[test]
    fn github_digest_must_agree() {
        let h = "ab".repeat(32);
        assert!(expected_hash(&h, Some(&format!("sha256:{h}"))).is_ok());
        assert!(expected_hash(&h, Some(&format!("sha256:{}", "cd".repeat(32)))).is_err());
        assert!(expected_hash(&h, Some("md5:abc")).is_err());
    }

    #[test]
    fn hashes_a_file() {
        let p = std::env::temp_dir().join(format!("cm2-sha-test-{}.bin", std::process::id()));
        std::fs::write(&p, b"abc").unwrap();
        let got = sha256_file(&p).unwrap();
        let _ = std::fs::remove_file(&p);
        assert_eq!(got, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }
}
