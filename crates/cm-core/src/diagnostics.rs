//! The plain-text diagnostic report people paste into an issue (1.7 DiagnosticsReport.cs and
//! FeedbackService.cs). English on purpose (it is read by whoever answers the issue) and redacted: no
//! user name in paths, no secrets. Pure formatting; gathering what goes in it is cm-app's `report.rs`.
//! Nothing here sends anything anywhere: the report goes to the clipboard and to a file, and the issue
//! page opens in the browser for the user to review and submit.

use regex::Regex;

pub const UNAVAILABLE: &str = "unavailable";
pub const LOG_LINES: usize = 50;

#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct SystemInfo { pub app_version: String, pub channel: String, pub windows: String, pub runtime: String, pub language: String }

/// `mode`: "3840 × 2160 @ 120 Hz", or empty when unknown. `hdr`: "on" | "off" | "unsupported" | "" (unknown).
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct DisplayInfo { pub name: String, pub mode: String, pub hdr: String, pub active: bool, pub primary: bool, pub focus: bool, pub hidden: bool }

#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct AudioInfo { pub name: String, pub is_default: bool, pub configured: bool }

/// One connected controller. `detail` is free text printed after the type (a battery level).
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct ControllerInfo { pub name: String, pub kind: String, pub detail: Option<String> }

/// `path`: None when the program wasn't found.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct AppInfo { pub name: String, pub path: Option<String>, pub version: Option<String> }

/// Everything the report shows. A `None` section is one that failed to load ("unavailable").
#[derive(Clone, Debug, Default)]
pub struct Report {
    pub system: Option<SystemInfo>,
    pub displays: Option<Vec<DisplayInfo>>,
    pub audio: Option<Vec<AudioInfo>>,
    pub controllers: Option<Vec<ControllerInfo>>,
    pub apps: Option<Vec<AppInfo>>,
    /// The launcher the Início opens by default (config `fullscreenMode`).
    pub launches: Option<String>,
    pub log: Option<Vec<String>>,
}

pub fn format(data: &Report) -> String {
    let mut out = String::new();
    section(&mut out, "Console Mode", data.system.as_ref().map(|s| vec![
        format!("Version: {} ({})", s.app_version, s.channel),
        format!("Windows: {}", s.windows),
        format!("Runtime: {}", s.runtime),
        format!("Language: {}", s.language),
    ]));
    section(&mut out, "Displays", data.displays.as_ref().map(|list| list.iter().map(|d| {
        let mut tags = Vec::new();
        if d.focus { tags.push("game screen"); }
        if d.hidden { tags.push("hidden in console mode"); }
        if d.primary { tags.push("primary"); }
        if !d.active { tags.push("off"); }
        let mode = if d.mode.is_empty() { "mode unknown" } else { d.mode.as_str() };
        let hdr = if d.hdr.is_empty() { String::new() } else { format!(", HDR {}", d.hdr) };
        let tag_text = if tags.is_empty() { String::new() } else { format!(" [{}]", tags.join(", ")) };
        format!("- {}: {mode}{hdr}{tag_text}", d.name)
    }).collect()));
    section(&mut out, "Audio outputs", data.audio.as_ref().map(|list| list.iter().map(|a| {
        let mut tags = Vec::new();
        if a.is_default { tags.push("Windows default"); }
        if a.configured { tags.push("chosen in Console Mode"); }
        format!("- {}{}", a.name, if tags.is_empty() { String::new() } else { format!(" [{}]", tags.join(", ")) })
    }).collect()));
    section(&mut out, "Controllers", data.controllers.as_ref().map(|list| list.iter().map(|c| {
        format!("- {}: {}{}", c.name, c.kind, c.detail.as_deref().filter(|d| !d.trim().is_empty()).map(|d| format!(", {d}")).unwrap_or_default())
    }).collect()));
    let apps = data.apps.as_ref().map(|list| {
        let mut lines: Vec<String> = list.iter().map(|a| match &a.path {
            None => format!("- {}: not found", a.name),
            Some(p) => format!("- {}: {p}{}", a.name, a.version.as_deref().filter(|v| !v.trim().is_empty()).map(|v| format!(" ({v})")).unwrap_or_default()),
        }).collect();
        if let Some(l) = data.launches.as_deref().filter(|l| !l.is_empty()) { lines.insert(0, format!("Launches: {l}")); }
        lines
    });
    section(&mut out, "Programs", apps);
    section(&mut out, &format!("Log (last {LOG_LINES} lines)"), data.log.as_ref().map(|l| l[l.len().saturating_sub(LOG_LINES)..].to_vec()));
    redact(&format!("{}\n", out.trim_end()))
}

fn section(out: &mut String, title: &str, lines: Option<Vec<String>>) {
    if !out.is_empty() { out.push('\n'); }
    out.push_str(&format!("== {title} ==\n"));
    match lines {
        None => out.push_str(&format!("{UNAVAILABLE}\n")),
        Some(l) if l.is_empty() => out.push_str("(none)\n"),
        Some(l) => for line in l { out.push_str(&line); out.push('\n'); },
    }
}

/// Hides the Windows user name in profile paths and anything that looks like a key or token.
pub fn redact(text: &str) -> String {
    let user_path = Regex::new(r#"(?i)\b([a-z]:[\\/]+(?:users|documents and settings)[\\/]+)[^\\/\r\n"']+"#).expect("valid");
    let pem = Regex::new(r"(?s)-----BEGIN [A-Z ]+-----.*?(?:-----END [A-Z ]+-----|$)").expect("valid");
    let dpapi = Regex::new(r"dpapi:[A-Za-z0-9+/=]+").expect("valid");
    let named = Regex::new(r#"(?i)\b(token|password|passwd|secret|api[_-]?key|private[_-]?key|pairing[_-]?key|auth[_-]?key|authorization)("?\s*[:=]\s*"?)(?:bearer\s+)?[^\s",;]+"#).expect("valid");
    let text = pem.replace_all(text, "[redacted key]");
    let text = dpapi.replace_all(&text, "dpapi:[redacted]");
    let text = named.replace_all(&text, "${1}${2}[redacted]");
    user_path.replace_all(&text, "${1}…").into_owned()
}

// ───────────── the issue page ─────────────

/// Only non-personal details: app version and channel, Windows version and the UI language.
pub fn environment(app_version: &str, channel: &str, os_version: &str, architecture: &str, language: &str) -> String {
    format!("Console Mode {app_version} ({channel})\nWindows {os_version} ({architecture})\nLanguage: {language}")
}

fn percent_encode(s: &str) -> String {
    s.bytes().map(|b| if b.is_ascii_alphanumeric() || matches!(b, b'-' | b'_' | b'.' | b'~') { (b as char).to_string() } else { format!("%{b:02X}") }).collect()
}

/// A new GitHub issue with a title and a body already filled in. No token or API call is involved: the
/// user reviews and submits the issue in the browser.
pub fn issue_url(repository: &str, title: &str, body: &str) -> String {
    format!("https://github.com/{repository}/issues/new?title={}&body={}", percent_encode(title), percent_encode(body))
}

#[cfg(test)]
mod tests {
    use super::*;

    fn full() -> Report {
        Report {
            system: Some(SystemInfo { app_version: "2.0.0-alpha.1".into(), channel: "alpha".into(), windows: "Windows 11 23H2 (build 22631.4317)".into(), runtime: "Rust x86_64".into(), language: "pt-BR".into() }),
            displays: Some(vec![
                DisplayInfo { name: "LG TV".into(), mode: "3840 × 2160 @ 120 Hz".into(), hdr: "on".into(), active: true, focus: true, ..Default::default() },
                DisplayInfo { name: "Dell (DEL4321)".into(), primary: true, hidden: true, ..Default::default() },
            ]),
            audio: Some(vec![AudioInfo { name: "Speakers".into(), is_default: true, configured: false }, AudioInfo { name: "LG TV".into(), is_default: false, configured: true }]),
            controllers: Some(vec![ControllerInfo { name: "Xbox Wireless Controller".into(), kind: "Xbox".into(), detail: None }, ControllerInfo { name: "DualSense".into(), kind: "PlayStation".into(), detail: Some("battery 80%".into()) }]),
            apps: Some(vec![AppInfo { name: "Steam".into(), path: Some(r"D:\Steam\steam.exe".into()), version: Some("9.0".into()) }, AppInfo { name: "RTSS".into(), path: None, version: None }]),
            launches: Some("bigPicture".into()),
            log: Some(vec!["line one".into(), "line two".into()]),
        }
    }

    #[test]
    fn the_full_report_lists_every_section_with_its_marks() {
        let r = format(&full());
        for want in [
            "== Console Mode ==", "Version: 2.0.0-alpha.1 (alpha)", "Runtime: Rust x86_64", "Language: pt-BR",
            "- LG TV: 3840 × 2160 @ 120 Hz, HDR on [game screen]", "- Dell (DEL4321): mode unknown [hidden in console mode, primary, off]",
            "- Speakers [Windows default]", "- LG TV [chosen in Console Mode]", "- Xbox Wireless Controller: Xbox\n", "- DualSense: PlayStation, battery 80%",
            "Launches: bigPicture", r"- Steam: D:\Steam\steam.exe (9.0)", "- RTSS: not found", "line one\nline two",
        ] { assert!(r.contains(want), "{want}\n{r}"); }
        assert!(!r.contains(UNAVAILABLE));
    }

    #[test]
    fn failed_sections_read_unavailable_and_empty_ones_none() {
        let r = format(&Report { controllers: Some(vec![]), log: Some(vec![]), ..Default::default() });
        // system, displays, audio and programs failed; controllers and log are just empty
        assert_eq!(r.matches(UNAVAILABLE).count(), 4);
        assert_eq!(r.matches("(none)").count(), 2);
        assert!(r.contains("== Controllers ==\n(none)") && r.contains(&format!("== Displays ==\n{UNAVAILABLE}")));
    }

    #[test]
    fn the_log_keeps_only_the_last_lines() {
        let log: Vec<String> = (1..=80).map(|i| format!("entry {i:03}")).collect();
        let r = format(&Report { log: Some(log), ..Default::default() });
        assert!(!r.contains("entry 030") && r.contains("entry 031") && r.contains("entry 080"));
    }

    #[test]
    fn the_user_name_is_removed_from_paths() {
        for (input, want) in [
            (r"C:\Users\Filipe\AppData\Local\Playnite\Playnite.FullscreenApp.exe", r"C:\Users\…\AppData\Local\Playnite\Playnite.FullscreenApp.exe"),
            (r"c:\users\joão silva\Desktop", r"c:\users\…\Desktop"),
            ("C:/Users/bob/Documents", "C:/Users/…/Documents"),
            (r"perfil em C:\Users\maria", r"perfil em C:\Users\…"),
            (r"D:\Games\Steam\steam.exe", r"D:\Games\Steam\steam.exe"),
        ] { assert_eq!(redact(input), want); }
    }

    #[test]
    fn secrets_are_redacted() {
        for (input, want) in [
            ("chave: dpapi:AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA==", "chave: dpapi:[redacted]"),
            ("token=abc123XYZ fim", "token=[redacted] fim"),
            ("\"pairingKey\": \"s3cr3t\"", "\"pairingKey\": \"[redacted]\""),
            ("Authorization: Bearer abc.def", "Authorization: [redacted]"),
            ("password=hunter2;", "password=[redacted];"),
        ] { assert_eq!(redact(input), want); }
    }

    #[test]
    fn private_keys_are_redacted_in_the_report() {
        let r = format(&Report { log: Some(vec!["-----BEGIN PRIVATE KEY-----".into(), "MIIEvQIBADANBgkqhkiG9w0BAQEFAASC".into(), "-----END PRIVATE KEY-----".into(), r"lido de C:\Users\ana\x".into()]), ..Default::default() });
        assert!(!r.contains("MIIEvQ") && r.contains("[redacted key]") && !r.contains(r"\ana\"), "{r}");
    }

    #[test]
    fn the_issue_page_gets_the_title_and_the_body_escaped() {
        let env = environment("2.0.0-alpha.1", "alpha", "10.0.26300", "X64", "pt-BR");
        assert_eq!(env, "Console Mode 2.0.0-alpha.1 (alpha)\nWindows 10.0.26300 (X64)\nLanguage: pt-BR");
        let url = issue_url("lippdev/consolemode", "[2.0 Alpha] ", &env);
        assert!(url.starts_with("https://github.com/lippdev/consolemode/issues/new?title=%5B2.0%20Alpha%5D%20&body=Console%20Mode%202.0.0-alpha.1"));
        // characters that would break the query
        let tricky = issue_url("a/b", "a&b=c #d", "a&b=c #d\né");
        assert!(!tricky.contains(' ') && !tricky.contains('#') && tricky.matches('&').count() == 1, "{tricky}");
        assert!(tricky.ends_with("%0A%C3%A9"));
        assert!(url.len() < 8000);
    }
}
