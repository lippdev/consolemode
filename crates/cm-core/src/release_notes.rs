//! Reads the GitHub release body (1.7 ReleaseNotes.cs) and decides when to show "what's new".
//!
//! The body has one section per language, each opened by a fixed `## …` heading; a body without
//! language headings is English-only and shown to everyone. A hidden summary per translated
//! changelog (`<!-- summary pt-BR: first item -->`) wins over the English notes.
//!
//! "What's new" after an update (2.0): the update that gets installed leaves its notes in the data
//! folder ([`Saved`]); the next start, when the version is not the one that ran last time, shows the
//! first item once. Without saved notes (an install done by hand) it only names the version and
//! points to the release page.

use semver::Version;
use serde::{Deserialize, Serialize};

pub const PORTUGUESE_HEADING: &str = "## Português (Brasil)";
pub const ENGLISH_HEADING: &str = "## English (US)";
/// `<!-- summary pt-BR: first item -->`: an HTML comment, so GitHub doesn't show it.
pub const SUMMARY_PREFIX: &str = "<!-- summary ";
/// The longest item shown.
const MAX_CHARS: usize = 140;

/// First real line of the notes in `language` (a catalog code such as "pt-BR"), skipping headings and
/// list markers, or None when the release has no notes in that language.
pub fn first_line(notes: Option<&str>, language: &str) -> Option<String> {
    let notes = notes.filter(|n| !n.trim().is_empty())?;
    let notes = notes.replace("\r\n", "\n");
    let lines: Vec<&str> = notes.split('\n').collect();
    if let Some(summary) = summary(&lines, language) { return Some(summary); }

    // the release body has pt-BR and en-US sections; every other language reads the English one
    let mut english = language != "pt-BR";
    let bilingual = lines.iter().any(|l| is_heading(l, PORTUGUESE_HEADING) || is_heading(l, ENGLISH_HEADING));
    // since 1.5.0-beta.6 the body is English only (Portuguese lives in CHANGELOG.md, linked from the body),
    // so a body without language headings is shown to everyone
    if !bilingual { english = false; }

    let (wanted, other) = if english { (ENGLISH_HEADING, PORTUGUESE_HEADING) } else { (PORTUGUESE_HEADING, ENGLISH_HEADING) };
    let mut in_section = !bilingual;
    for raw in lines {
        if is_heading(raw, wanted) { in_section = true; continue; }
        if is_heading(raw, other) { in_section = false; continue; }
        if !in_section { continue; }
        let trimmed = raw.trim();
        if trimmed.is_empty() || trimmed.starts_with('#') || trimmed == "---" { continue; }
        let line = clean(trimmed);
        if !line.is_empty() { return Some(line); }
    }
    None
}

/// The hidden summary for exactly `language`, if the release has one.
fn summary(lines: &[&str], language: &str) -> Option<String> {
    let prefix = format!("{SUMMARY_PREFIX}{language}:");
    for raw in lines {
        let trimmed = raw.trim();
        if !trimmed.starts_with(&prefix) || !trimmed.ends_with("-->") { continue; }
        let line = clean(&trimmed[prefix.len()..trimmed.len() - 3]);
        if !line.is_empty() { return Some(line); }
    }
    None
}

/// Plain text for the notice: no list marker, bold or link syntax, at most 140 characters.
fn clean(text: &str) -> String {
    let t = text.trim().trim_start_matches(['-', '*', ' ']).replace("**", "");
    let line = strip_links(&t).trim().to_string();
    if line.chars().count() > MAX_CHARS { format!("{}…", line.chars().take(MAX_CHARS).collect::<String>()) } else { line }
}

/// `[text](url)` → `text`.
fn strip_links(s: &str) -> String {
    let mut out = String::with_capacity(s.len());
    let mut rest = s;
    while let Some(open) = rest.find('[') {
        let after = &rest[open + 1..];
        let link = after.find(']').and_then(|close| {
            let tail = &after[close + 1..];
            if !tail.starts_with('(') || after[..close].is_empty() { return None; }
            tail.find(')').map(|end| (close, close + 1 + end + 1))
        });
        match link {
            Some((close, used)) => { out.push_str(&rest[..open]); out.push_str(&after[..close]); rest = &after[used..]; }
            None => { out.push_str(&rest[..=open]); rest = after; }
        }
    }
    out.push_str(rest);
    out
}

fn is_heading(line: &str, heading: &str) -> bool { line.trim().trim_start_matches('\u{feff}') == heading }

// ───────────── what's new after an update ─────────────

/// What an installed update leaves for the next start (`whatsnew.json` in the data folder).
#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct Saved {
    pub version: String,
    pub notes: String,
    pub page_url: String,
}

pub const FILE: &str = "whatsnew.json";

/// What to show once after an update.
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Notice {
    pub version: String,
    /// The first item of the notes, when there are notes for this version.
    pub line: Option<String>,
    pub url: String,
}

/// The release page of a version, when nothing saved knows better.
pub fn release_url(repository: &str, version: &str) -> String { format!("https://github.com/{repository}/releases/tag/v{version}") }

/// At start: `(the notice to show, the version to record as the last one that ran)`.
/// `last_run` empty is a first run (or a config from before this existed): nothing to announce, just
/// record the version. A version that is not newer than the last one (a downgrade) announces nothing.
pub fn decide(last_run: &str, current: &str, saved: Option<&Saved>, language: &str, repository: &str) -> (Option<Notice>, String) {
    if last_run.is_empty() || last_run == current { return (None, current.to_string()); }
    let newer = match (Version::parse(last_run), Version::parse(current)) { (Ok(a), Ok(b)) => b > a, _ => true };
    if !newer { return (None, current.to_string()); }
    let mine = saved.filter(|s| s.version == current);
    let notice = Notice {
        version: current.to_string(),
        line: mine.and_then(|s| first_line(Some(&s.notes), language)),
        url: mine.map(|s| s.page_url.clone()).filter(|u| u.starts_with("https://")).unwrap_or_else(|| release_url(repository, current)),
    };
    (Some(notice), current.to_string())
}

#[cfg(test)]
mod tests {
    use super::*;

    const BILINGUAL: &str = "## Português (Brasil)\n\n### Novidades\n- **Escolha** do idioma no instalador.\n\n### Arquivos para baixar\n- Instalador\n\n---\n\n## English (US)\n\n### What's new\n- **Language** choice in the installer.\n\n### Downloads\n- Installer";
    const LEGACY_PT: &str = "### Novidades\n- Interface em inglês (Estados Unidos).\n\n## Arquivos para baixar\n- Instalador";
    const ENGLISH_ONLY: &str = "### What's new\n- **Session menu** over the game. (#46)\n\n### Downloads\n- Installer\n\n---\nNotas em português: CHANGELOG.md";
    const WITH_SUMMARIES: &str = "### What's new\n- **Minimize to the system tray:** a new option in Settings. (#150)\n\n---\n\n🇧🇷 Notas em português: [CHANGELOG.md](https://example.com/CHANGELOG.md)\n\n<!-- summary pt-BR: **Minimizar para a bandeja:** traduzida por [u/Skinner99](https://www.reddit.com/user/Skinner99/). (#150) -->";

    #[test]
    fn picks_the_portuguese_and_the_english_sections() {
        assert_eq!(first_line(Some(BILINGUAL), "pt-BR").as_deref(), Some("Escolha do idioma no instalador."));
        assert_eq!(first_line(Some(BILINGUAL), "en-US").as_deref(), Some("Language choice in the installer."));
        // every other language reads the English one
        assert_eq!(first_line(Some(BILINGUAL), "ru-RU").as_deref(), Some("Language choice in the installer."));
    }

    #[test]
    fn handles_crlf() {
        assert_eq!(first_line(Some(&BILINGUAL.replace('\n', "\r\n")), "en-US").as_deref(), Some("Language choice in the installer."));
        assert_eq!(first_line(Some(&WITH_SUMMARIES.replace('\n', "\r\n")), "pt-BR").as_deref(), Some("Minimizar para a bandeja: traduzida por u/Skinner99. (#150)"));
    }

    #[test]
    fn a_body_without_language_headings_is_shown_to_everyone() {
        assert_eq!(first_line(Some(LEGACY_PT), "pt-BR").as_deref(), Some("Interface em inglês (Estados Unidos)."));
        assert_eq!(first_line(Some(LEGACY_PT), "en-US").as_deref(), Some("Interface em inglês (Estados Unidos)."));
        assert_eq!(first_line(Some(ENGLISH_ONLY), "pt-BR").as_deref(), Some("Session menu over the game. (#46)"));
        assert_eq!(first_line(Some(ENGLISH_ONLY), "es-ES").as_deref(), Some("Session menu over the game. (#46)"));
    }

    #[test]
    fn the_hidden_summary_wins_for_its_language_and_the_others_read_english() {
        assert_eq!(first_line(Some(WITH_SUMMARIES), "pt-BR").as_deref(), Some("Minimizar para a bandeja: traduzida por u/Skinner99. (#150)"));
        assert_eq!(first_line(Some(WITH_SUMMARIES), "en-US").as_deref(), Some("Minimize to the system tray: a new option in Settings. (#150)"));
        assert_eq!(first_line(Some(WITH_SUMMARIES), "de-DE").as_deref(), Some("Minimize to the system tray: a new option in Settings. (#150)"));
    }

    #[test]
    fn an_empty_or_unclosed_summary_falls_back_to_the_notes() {
        assert_eq!(first_line(Some("- Item.\n<!-- summary pt-BR:   -->"), "pt-BR").as_deref(), Some("Item."));
        assert_eq!(first_line(Some("- Item.\n<!-- summary pt-BR: sem fim"), "pt-BR").as_deref(), Some("Item."));
    }

    #[test]
    fn the_summaries_stay_after_the_notes_for_older_clients() {
        let first = WITH_SUMMARIES.lines().map(str::trim).find(|l| !l.is_empty() && !l.starts_with('#') && *l != "---").unwrap();
        assert!(first.starts_with("- **Minimize"));
    }

    #[test]
    fn an_empty_body_has_no_line() {
        assert_eq!(first_line(Some(""), "pt-BR"), None);
        assert_eq!(first_line(None, "en-US"), None);
        assert_eq!(first_line(Some("   \n  "), "en-US"), None);
    }

    #[test]
    fn a_long_item_is_cut_and_links_become_their_text() {
        let long = format!("- {}", "a".repeat(200));
        let line = first_line(Some(&long), "en-US").unwrap();
        assert_eq!(line.chars().count(), 141);
        assert!(line.ends_with('…'));
        assert_eq!(first_line(Some("- See [the notes](https://x.y/z) now [not a link"), "en-US").as_deref(), Some("See the notes now [not a link"));
    }

    fn saved(version: &str) -> Saved { Saved { version: version.into(), notes: "### What's new\n- **Battery** in the top bar. (#9)\n".into(), page_url: format!("https://github.com/lippdev/consolemode/releases/tag/v{version}") } }

    #[test]
    fn a_first_run_announces_nothing_and_records_the_version() {
        assert_eq!(decide("", "2.0.0-alpha.1", None, "en-US", "lippdev/consolemode"), (None, "2.0.0-alpha.1".into()));
        assert_eq!(decide("2.0.0-alpha.1", "2.0.0-alpha.1", Some(&saved("2.0.0-alpha.1")), "en-US", "lippdev/consolemode").0, None);
    }

    #[test]
    fn an_update_shows_the_first_item_of_its_notes_once() {
        let (notice, record) = decide("2.0.0-alpha.1", "2.0.0-alpha.2", Some(&saved("2.0.0-alpha.2")), "en-US", "lippdev/consolemode");
        let n = notice.unwrap();
        assert_eq!((n.version.as_str(), n.line.as_deref()), ("2.0.0-alpha.2", Some("Battery in the top bar. (#9)")));
        assert_eq!(n.url, "https://github.com/lippdev/consolemode/releases/tag/v2.0.0-alpha.2");
        assert_eq!(record, "2.0.0-alpha.2");
        // alpha.10 is newer than alpha.9 (not a text comparison)
        assert!(decide("2.0.0-alpha.9", "2.0.0-alpha.10", None, "en-US", "r/r").0.is_some());
    }

    #[test]
    fn without_notes_for_this_version_it_names_the_version_and_links_the_release() {
        // installed by hand, or the saved notes are of another version
        for s in [None, Some(saved("2.0.0-alpha.9"))] {
            let n = decide("2.0.0-alpha.1", "2.0.0-alpha.2", s.as_ref(), "pt-BR", "lippdev/consolemode").0.unwrap();
            assert_eq!(n.line, None);
            assert_eq!(n.url, "https://github.com/lippdev/consolemode/releases/tag/v2.0.0-alpha.2");
        }
        // a link that is not https is not followed
        let mut s = saved("2.0.0-alpha.2");
        s.page_url = "file:///C:/x".into();
        assert!(decide("2.0.0-alpha.1", "2.0.0-alpha.2", Some(&s), "pt-BR", "r/r").0.unwrap().url.starts_with("https://github.com/r/r/"));
    }

    #[test]
    fn a_downgrade_announces_nothing() {
        let (notice, record) = decide("2.0.0-alpha.5", "2.0.0-alpha.2", None, "pt-BR", "r/r");
        assert_eq!((notice, record.as_str()), (None, "2.0.0-alpha.2"));
    }

    #[test]
    fn the_saved_notes_round_trip() {
        let s = saved("2.0.0-alpha.2");
        assert_eq!(serde_json::from_str::<Saved>(&serde_json::to_string(&s).unwrap()).unwrap(), s);
        assert_eq!(serde_json::from_str::<Saved>("{}").unwrap(), Saved::default());
    }
}
