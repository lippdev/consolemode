//! The UI language (gettext style): the key of a text is its Portuguese source, the value is the
//! translation. `locales/<code>.json` holds one catalog per language, embedded in the binary;
//! Portuguese (pt-BR) has no file, the source text is the text.
//!
//! * `t("Jogar")`: the translation, or the source itself when a catalog lacks it.
//! * `tf("Baixando a versão {0}…", &[&version])`: a text with values, placeholders `{0}`, `{1}`…
//!   (never build a sentence from fragments: word order differs between languages).
//! * `tn("há {0} dias", n)`: a text with a count; a catalog entry may be a list of plural forms
//!   (Russian: one, few, many), see [`plural_index`].
//! * `n("…")`: marks a text that is rendered later (table data); returns it unchanged, so the
//!   coverage test finds it. The renderer applies `t()`.
//!
//! Logic never compares translated text: rows, choices and hints keep their Portuguese source in
//! the data and are translated only where they are drawn, so every comparison (`title == …`,
//! `"Ligado"`) works in any language. Log lines stay Portuguese and are not translated.

use std::{
    borrow::Cow,
    collections::HashMap,
    fmt::Display,
    sync::{OnceLock, atomic::{AtomicUsize, Ordering}},
};

/// Language codes as 1.7 stores them in `appLanguage`, and how each language names itself in the
/// picker (never translated). The order is the index used everywhere below.
pub const LANGUAGES: [(&str, &str); 6] =
    [("pt-BR", "Português (Brasil)"), ("en-US", "English"), ("es-ES", "Español"), ("de-DE", "Deutsch"), ("fr-FR", "Français"), ("ru-RU", "Русский")];

/// The embedded catalogs, by [`LANGUAGES`] index (Portuguese: none).
const SOURCES: [Option<&str>; 6] = [
    None,
    Some(include_str!("../locales/en-US.json")),
    Some(include_str!("../locales/es-ES.json")),
    Some(include_str!("../locales/de-DE.json")),
    Some(include_str!("../locales/fr-FR.json")),
    Some(include_str!("../locales/ru-RU.json")),
];

enum Entry { One(&'static str), Forms(Vec<&'static str>) }

type Catalog = HashMap<&'static str, Entry>;

static CATALOGS: OnceLock<[Catalog; 6]> = OnceLock::new();
static CURRENT: AtomicUsize = AtomicUsize::new(0);

fn leak(s: &str) -> &'static str { Box::leak(s.to_owned().into_boxed_str()) }

/// Parses a catalog file: `{ "source": "translation" | ["form", …] }`.
fn parse(text: &str) -> Result<Catalog, String> {
    let value: serde_json::Value = serde_json::from_str(text.trim_start_matches('\u{feff}')).map_err(|e| e.to_string())?;
    let object = value.as_object().ok_or("a catalog is a JSON object")?;
    let mut catalog = Catalog::new();
    for (key, v) in object {
        let entry = match v {
            serde_json::Value::String(s) => Entry::One(leak(s)),
            serde_json::Value::Array(a) => Entry::Forms(a.iter().map(|f| f.as_str().map(leak).ok_or(format!("{key}: a form is a string"))).collect::<Result<_, _>>()?),
            _ => return Err(format!("{key}: a text or a list of forms")),
        };
        catalog.insert(leak(key), entry);
    }
    Ok(catalog)
}

fn catalogs() -> &'static [Catalog; 6] {
    CATALOGS.get_or_init(|| std::array::from_fn(|i| match SOURCES[i] {
        // a broken catalog is a bug caught by the tests; at run time it just falls back to Portuguese
        Some(text) => parse(text).unwrap_or_default(),
        None => Catalog::new(),
    }))
}

// ───────────── choosing the language ─────────────

/// The index in [`LANGUAGES`] for a code: exact, else the same language (`en-GB` → en-US).
fn index_of(code: &str) -> Option<usize> {
    let code = code.trim().replace('_', "-");
    if code.is_empty() { return None; }
    LANGUAGES.iter().position(|(c, _)| c.eq_ignore_ascii_case(&code)).or_else(|| {
        let lang = code.split('-').next().unwrap_or("");
        LANGUAGES.iter().position(|(c, _)| c.split('-').next().is_some_and(|l| l.eq_ignore_ascii_case(lang)))
    })
}

/// Windows' display language (a LANGID) mapped to the nearest catalog; Portuguese when we have none.
pub fn index_of_langid(langid: u16) -> usize {
    match langid & 0x3ff {
        0x09 => 1, // English
        0x0a => 2, // Spanish
        0x07 => 3, // German
        0x0c => 4, // French
        0x19 => 5, // Russian
        _ => 0,    // Portuguese, and anything we don't have
    }
}

/// The language to use for the saved `appLanguage`: an explicit choice wins; empty means the
/// Windows display language.
pub fn resolve(saved: &str) -> usize {
    index_of(saved).unwrap_or_else(|| index_of_langid(cm_platform::system_ui_language_id()))
}

/// Picks the language from the saved `appLanguage` (startup, and when the picker changes).
pub fn set_language(saved: &str) {
    CURRENT.store(resolve(saved), Ordering::Relaxed);
    apply_tray_labels();
}

pub fn current() -> usize { CURRENT.load(Ordering::Relaxed) }

/// The code the app runs in now ("pt-BR", …).
pub fn code() -> &'static str { LANGUAGES[current()].0 }

/// The tray menu is drawn by Windows from its own thread: it gets its four texts here.
pub fn apply_tray_labels() {
    cm_platform::shell_win::set_tray_labels([t("Abrir").into(), t("Menu da sessão").into(), t("Encerrar sessão").into(), t("Sair").into()]);
}

// ───────────── translating ─────────────

/// Marks a text that is translated where it is drawn (see the module docs). Returns it unchanged.
pub const fn n(src: &'static str) -> &'static str { src }

fn entry(src: &str) -> Option<&'static Entry> {
    let i = current();
    if i == 0 { return None; }
    catalogs()[i].get(src)
}

/// The text in the current language; the source itself when the catalog has no entry for it.
pub fn t(src: &str) -> &str {
    match entry(src) {
        Some(Entry::One(s)) => s,
        Some(Entry::Forms(f)) => f.first().copied().unwrap_or(src),
        None => src,
    }
}

/// Fills `{0}`, `{1}`… of an already translated text.
fn fill(text: &str, args: &[&dyn Display]) -> String {
    let mut out = String::with_capacity(text.len() + 16);
    let mut rest = text;
    while let Some(open) = rest.find('{') {
        out.push_str(&rest[..open]);
        let tail = &rest[open + 1..];
        match tail.find('}').and_then(|close| tail[..close].parse::<usize>().ok().map(|i| (i, close))) {
            Some((i, close)) if i < args.len() => { out.push_str(&args[i].to_string()); rest = &tail[close + 1..]; }
            _ => { out.push('{'); rest = tail; }
        }
    }
    out.push_str(rest);
    out
}

/// A text with values: `tf("Baixando a versão {0}…", &[&version])`.
pub fn tf(src: &str, args: &[&dyn Display]) -> String { fill(t(src), args) }

/// Which plural form a language uses for `n`: Russian has three (1, 21… / 2–4, 22… / 0, 5–20…),
/// French counts 0 and 1 as singular, the rest have two (1 / other).
pub fn plural_index(lang: &str, n: u64) -> usize {
    match lang {
        "ru-RU" => {
            let (m10, m100) = (n % 10, n % 100);
            if m10 == 1 && m100 != 11 { 0 } else if (2..=4).contains(&m10) && !(12..=14).contains(&m100) { 1 } else { 2 }
        }
        "fr-FR" => usize::from(n >= 2),
        _ => usize::from(n != 1),
    }
}

/// A text with a count; `{0}` is the count: `tn("há {0} dias", days)`.
pub fn tn(src: &str, n: u64) -> String {
    let text = match entry(src) {
        Some(Entry::One(s)) => s,
        Some(Entry::Forms(f)) => f[plural_index(code(), n).min(f.len() - 1)],
        None => src,
    };
    fill(text, &[&n])
}

/// A value shown in a row or a menu (a choice, a screen's name…): translated when it has an entry,
/// and a screen turned off keeps its name with the note translated.
pub fn shown(text: &str) -> Cow<'_, str> {
    if let Some(name) = text.strip_suffix(crate::hardware::OFF_NOTE) { return Cow::Owned(format!("{name}{}", t(crate::hardware::OFF_NOTE))); }
    Cow::Borrowed(t(text))
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::collections::{BTreeMap, BTreeSet};

    /// The tests that switch the language share one global.
    static LANGUAGE: std::sync::Mutex<()> = std::sync::Mutex::new(());

    fn files() -> Vec<std::path::PathBuf> {
        let dir = std::path::Path::new(env!("CARGO_MANIFEST_DIR")).join("src");
        let mut v: Vec<_> = std::fs::read_dir(dir).unwrap().flatten().map(|e| e.path()).filter(|p| p.extension().is_some_and(|x| x == "rs")).collect();
        v.sort();
        v
    }

    /// Source texts found in `t("…")`, `tf("…"`, `tn("…"` and `n("…")` calls (code after `#[cfg(test)]`
    /// is skipped), with the file they are in.
    fn extract() -> BTreeMap<String, String> {
        let mut found = BTreeMap::new();
        for path in files() {
            let name = path.file_name().unwrap().to_string_lossy().into_owned();
            let text = std::fs::read_to_string(&path).unwrap();
            let code = text.split("#[cfg(test)]").next().unwrap();
            // comments may show examples
            let code: String = code.lines().filter(|l| !l.trim_start().starts_with("//")).collect::<Vec<_>>().join("\n");
            let code = code.as_str();
            let b = code.as_bytes();
            let mut i = 0;
            while i < b.len() {
                if !code.is_char_boundary(i) { i += 1; continue; }
                for call in ["tf(\"", "tn(\"", "t(\"", "n(\""] {
                    if code[i..].starts_with(call) && (i == 0 || !(b[i - 1].is_ascii_alphanumeric() || b[i - 1] == b'_')) {
                        let (s, _) = unescape(&code[i + call.len()..]);
                        found.entry(s).or_insert_with(|| name.clone());
                    }
                }
                i += 1;
            }
        }
        found
    }

    /// The Rust string literal that starts right after its opening quote → (text, bytes used).
    fn unescape(src: &str) -> (String, usize) {
        let (mut out, mut chars) = (String::new(), src.char_indices());
        while let Some((i, c)) = chars.next() {
            match c {
                '"' => return (out, i + 1),
                '\\' => match chars.next().map(|x| x.1) {
                    Some('n') => out.push('\n'),
                    Some('"') => out.push('"'),
                    Some('\\') => out.push('\\'),
                    Some('u') => {
                        let rest = &src[chars.next().unwrap().0 + 1..];
                        let end = rest.find('}').unwrap();
                        out.push(char::from_u32(u32::from_str_radix(&rest[..end], 16).unwrap()).unwrap());
                        for _ in 0..end + 1 { chars.next(); }
                    }
                    other => panic!("unsupported escape {other:?} in a translated text"),
                },
                c => out.push(c),
            }
        }
        panic!("unterminated literal");
    }

    fn load(i: usize) -> Catalog { parse(SOURCES[i].unwrap()).unwrap_or_else(|e| panic!("{}: {e}", LANGUAGES[i].0)) }

    /// Every user-facing text that lives in tables rather than in `t("…")` calls.
    fn table_texts() -> BTreeSet<String> {
        use crate::{overlays, settings, tour};
        let mut all = BTreeSet::new();
        let mut add = |s: &str| { if !is_symbolic(s) { all.insert(s.to_string()); } };
        let sections = |list: &[settings::Section], add: &mut dyn FnMut(&str)| {
            for s in list {
                add(s.title);
                for r in &s.rows {
                    add(r.title);
                    add(r.desc);
                    // the language names are never translated
                    if r.title == "Idioma" { continue; }
                    match &r.value {
                        settings::Value::Choice(o, _) => o.iter().for_each(|x| add(x)),
                        settings::Value::Link(Some(v)) if *v != env!("CARGO_PKG_VERSION") => add(v),
                        _ => {}
                    }
                }
            }
        };
        sections(&settings::session(), &mut add);
        for c in settings::system() { add(c.title); add(c.desc); sections(&c.sections, &mut add); }
        for v in overlays::game_defaults() { if let settings::Value::Choice(o, _) = v { o.iter().for_each(|x| add(x)); } }
        for (_, l) in overlays::GAME_OPTS { add(l); }
        for (_, l) in overlays::GUIDE { add(l); }
        for (_, title, body) in tour::STEPS { add(title); add(body); }
        for s in cm_core::shortcuts::SLOTS { for l in crate::pad_shortcuts::choices(s) { add(l); } }
        all
    }

    /// Numbers, resolutions, rates and the like read the same everywhere.
    fn is_symbolic(s: &str) -> bool {
        let rest = s.replace(['×', '%', '.', ',', ' '], "").replace("Hz", "").replace("FPS", "").replace("HDMI", "");
        rest.chars().all(|c| c.is_ascii_digit() || matches!(c, 'K' | 'p'))
            // button names: "Select + Y (Create + △)", "A + B + X (✕ + ○ + □)"
            || s.contains(" + ")
    }

    /// A helper, not a check: `CM2_DUMP_KEYS=keys.json cargo test dump_keys -- --ignored` writes every
    /// text the code asks for (the skeleton of a new catalog, or the list to diff an old one against).
    #[test]
    #[ignore]
    fn dump_keys() {
        let Some(out) = std::env::var_os("CM2_DUMP_KEYS") else { return };
        let mut wanted: BTreeSet<String> = extract().into_keys().collect();
        wanted.extend(table_texts());
        wanted.insert(crate::hardware::OFF_NOTE.to_string());
        let object: serde_json::Map<String, serde_json::Value> = wanted.into_iter().map(|k| (k, serde_json::Value::String(String::new()))).collect();
        std::fs::write(out, serde_json::to_string_pretty(&object).unwrap()).unwrap();
    }

    #[test]
    fn every_catalog_has_the_same_keys() {
        let en: BTreeSet<_> = load(1).keys().copied().collect();
        for i in 2..6 {
            let keys: BTreeSet<_> = load(i).keys().copied().collect();
            let missing: Vec<_> = en.difference(&keys).collect();
            let extra: Vec<_> = keys.difference(&en).collect();
            assert!(missing.is_empty() && extra.is_empty(), "{}: missing {missing:?}, extra {extra:?}", LANGUAGES[i].0);
        }
    }

    #[test]
    fn every_text_in_the_source_has_an_entry_in_every_catalog() {
        let used = extract();
        assert!(used.len() > 100, "the extraction found only {} texts", used.len());
        let mut wanted: BTreeSet<String> = used.keys().cloned().collect();
        wanted.extend(table_texts());
        for i in 1..6 {
            let cat = load(i);
            let missing: Vec<_> = wanted.iter().filter(|k| !cat.contains_key(k.as_str())).collect();
            assert!(missing.is_empty(), "{} lacks {} texts: {missing:#?}", LANGUAGES[i].0, missing.len());
        }
    }

    #[test]
    fn no_catalog_keeps_a_text_nobody_asks_for() {
        let mut wanted: BTreeSet<String> = extract().into_keys().collect();
        wanted.extend(table_texts());
        // texts the code builds from the pieces it has (hardware notes, the tray, plural forms)
        let extra = [crate::hardware::OFF_NOTE];
        wanted.extend(extra.iter().map(|s| s.to_string()));
        let unused: Vec<_> = load(1).keys().filter(|k| !wanted.contains(**k)).copied().collect();
        assert!(unused.is_empty(), "catalog entries no code uses: {unused:#?}");
    }

    fn placeholders(s: &str) -> Vec<String> {
        let mut found: Vec<String> = Vec::new();
        let mut rest = s;
        while let Some(open) = rest.find('{') {
            let tail = &rest[open + 1..];
            let Some(close) = tail.find('}') else { break };
            found.push(tail[..close].to_string());
            rest = &tail[close + 1..];
        }
        found.sort();
        found.dedup();
        found
    }

    fn forms(e: &Entry) -> Vec<&'static str> {
        match e { Entry::One(s) => vec![*s], Entry::Forms(f) => f.clone() }
    }

    #[test]
    fn placeholders_match_the_source() {
        for i in 1..6 {
            for (key, e) in load(i) {
                for text in forms(&e) {
                    assert_eq!(placeholders(key), placeholders(text), "{} · {key:?} → {text:?}", LANGUAGES[i].0);
                }
            }
        }
    }

    #[test]
    fn russian_has_three_plural_forms_and_the_others_one_or_two() {
        for i in 1..6 {
            for (key, e) in load(i) {
                if let Entry::Forms(f) = e {
                    let want = if LANGUAGES[i].0 == "ru-RU" { 3 } else { 2 };
                    assert_eq!(f.len(), want, "{} · {key:?}", LANGUAGES[i].0);
                    assert!(key.contains("{0}"), "{key:?}: only a count has forms");
                }
            }
        }
        // 1 / 21 → one, 2–4 / 22 → few, 5–20 / 25 / 11–14 → many
        let pick = |n| plural_index("ru-RU", n);
        assert_eq!([1, 21, 2, 4, 22, 5, 11, 12, 14, 20, 25, 30].map(pick), [0, 0, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2]);
        assert_eq!((plural_index("fr-FR", 1), plural_index("fr-FR", 2), plural_index("en-US", 1), plural_index("en-US", 2)), (0, 1, 0, 1));
    }

    /// Per language, the texts that truly read the same as the Portuguese source (brands, units,
    /// cognates). Anything else left as the source in a catalog is an untranslated sentence.
    const SAME_AS_SOURCE: &[(&str, &[&str])] = &[
        ("de-DE", &["Alpha", "Apps", "Console Mode", "HDR", "Jade", "Notebook", "PlayStation", "Software", "VRR", "Xbox"]),
        ("en-US", &["Alpha", "Apps", "Console", "Console Mode", "HDR", "Jade", "Menu", "PlayStation", "Software", "TV", "TV (HDMI)", "Tour", "VRR", "Volume", "Volume ↑↓", "Xbox", "experimental", "{0} h {1} min", "{0} min"]),
        ("es-ES", &[
            "Abrir", "Argumentos", "Automático", "Canal", "Cancelar", "Console Mode", "Controles", "Entrar", "Estado", "Estilo", "HDR", "Idioma", "Instalando…", "Jade", "Lenta", "Modo Xbox", "Mover", "Navegador (JoyChromium)", "PlayStation",
            "Rápida", "Sistema", "Software", "Suave", "TV", "TV (HDMI)", "Tema", "VRR", "Xbox", "experimental", "menos de 1 min", "nunca", "Última vez", "{0} de {1}", "{0} h {1} min", "{0} min",
        ]),
        ("fr-FR", &["Alpha", "Canal", "Console", "Console Mode", "HDR", "Jade", "Limite de FPS", "Limite ↑↓", "Menu", "PlayStation", "TV", "TV (HDMI)", "VRR", "Volume", "Volume ↑↓", "Xbox", "{0} h {1} min", "{0} min"]),
        ("ru-RU", &["Console Mode", "HDR", "PlayStation", "VRR", "Xbox"]),
    ];

    #[test]
    fn no_translation_is_the_portuguese_source_left_as_is() {
        let mut left = Vec::new();
        for i in 1..6 {
            for (key, e) in load(i) {
                if SAME_AS_SOURCE.iter().any(|(l, keys)| *l == LANGUAGES[i].0 && keys.contains(&key)) || is_symbolic(key) { continue; }
                if forms(&e).iter().any(|text| *text == key) { left.push(format!("{} · {key}", LANGUAGES[i].0)); }
            }
        }
        left.sort();
        assert!(left.is_empty(), "left in Portuguese (translate, or list it in SAME_AS_SOURCE if it truly reads the same): {left:#?}");
    }

    #[test]
    fn a_missing_entry_falls_back_to_the_source() {
        let _guard = LANGUAGE.lock().unwrap_or_else(|e| e.into_inner());
        CURRENT.store(1, Ordering::Relaxed);
        assert_eq!(t("texto que não existe em nenhum catálogo"), "texto que não existe em nenhum catálogo");
        assert_eq!(tf("{1} e {0} {2}", &[&"a", &"b"]), "b e a {2}");
        CURRENT.store(0, Ordering::Relaxed);
    }

    #[test]
    fn the_language_follows_the_saved_code_then_windows() {
        assert_eq!(index_of("en-US"), Some(1));
        assert_eq!(index_of("de_DE"), Some(3));
        assert_eq!(index_of("en-GB"), Some(1));
        assert_eq!(index_of("pt-pt"), Some(0));
        assert_eq!(index_of(""), None);
        assert_eq!(index_of("ja-JP"), None);
        // Windows' LANGIDs: pt-BR 0x416, en-US 0x409, es-MX 0x80a, de-AT 0xc07, fr-CA 0xc0c, ru 0x419, ja 0x411
        assert_eq!([0x416, 0x409, 0x80a, 0xc07, 0xc0c, 0x419, 0x411].map(index_of_langid), [0, 1, 2, 3, 4, 5, 0]);
        assert_eq!(resolve("ru-RU"), 5);
    }

    #[test]
    fn the_texts_render_in_every_language() {
        let _guard = LANGUAGE.lock().unwrap_or_else(|e| e.into_inner());
        for i in 0..6 {
            CURRENT.store(i, Ordering::Relaxed);
            assert!(!t("Jogar").is_empty() && !t("Sistema").is_empty());
            assert!(tn("há {0} dias", 21).contains("21"), "{}", LANGUAGES[i].0);
            assert!(shown("TV LG · desligada").starts_with("TV LG"));
        }
        CURRENT.store(0, Ordering::Relaxed);
    }
}
