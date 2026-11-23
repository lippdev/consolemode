//! The user's Steam library, read from files Steam already keeps on this PC: install folders
//! (libraryfolders.vdf), each game's manifest (name, last played) and the art in Steam's own cache.
//! Nothing is downloaded or copied.

use std::{fs, path::{Path, PathBuf}};

#[derive(Clone, Debug)]
pub struct Game {
    pub app_id: u32,
    pub name: String,
    /// Unix seconds; 0 when never played.
    pub last_played: u64,
    pub hero: Option<PathBuf>,
    pub hero_blur: Option<PathBuf>,
    pub logo: Option<PathBuf>,
    pub cover: Option<PathBuf>,
}

/// Steam's install folder (registry, then the default path).
pub fn steam_dir() -> Option<PathBuf> {
    #[cfg(windows)]
    {
        use winreg::{RegKey, enums::HKEY_CURRENT_USER};
        if let Ok(key) = RegKey::predef(HKEY_CURRENT_USER).open_subkey(r"Software\Valve\Steam") {
            if let Ok(p) = key.get_value::<String, _>("SteamPath") {
                let p = PathBuf::from(p.replace('/', "\\"));
                if p.exists() { return Some(p); }
            }
        }
        let d = PathBuf::from(r"C:\Program Files (x86)\Steam");
        d.exists().then_some(d)
    }
    #[cfg(not(windows))]
    { None }
}

/// Installed games with art, most recently played first.
pub fn recent_games(steam: &Path, limit: usize) -> Vec<Game> {
    let cache = steam.join(r"appcache\librarycache");
    // only games: Steam also installs tools and apps (Wallpaper Engine, Lossless Scaling, redistributables)
    let types = fs::read(steam.join(r"appcache\appinfo.vdf")).map(|b| crate::appinfo::app_types(&b)).unwrap_or_default();
    let is_game = |id: u32| types.is_empty() || types.get(&id).is_some_and(|t| t == "game");
    let mut games: Vec<Game> = library_folders(steam)
        .into_iter()
        .flat_map(|lib| fs::read_dir(lib.join("steamapps")).into_iter().flatten().flatten())
        .filter(|e| e.file_name().to_string_lossy().starts_with("appmanifest_"))
        .filter_map(|e| parse_manifest(&fs::read_to_string(e.path()).ok()?))
        .filter(|(app_id, _, _)| is_game(*app_id))
        .filter_map(|(app_id, name, last_played)| {
            let art = Art::find(&cache.join(app_id.to_string()));
            // tools and redistributables have no hero art: only games show up
            art.hero.as_ref()?;
            Some(Game { app_id, name: clean_name(&name), last_played, hero: art.hero, hero_blur: art.hero_blur, logo: art.logo, cover: art.cover })
        })
        .collect();
    games.sort_by(|a, b| b.last_played.cmp(&a.last_played));
    games.dedup_by_key(|g| g.app_id);
    games.truncate(limit);
    games
}

fn library_folders(steam: &Path) -> Vec<PathBuf> {
    let mut out = vec![steam.to_path_buf()];
    if let Ok(vdf) = fs::read_to_string(steam.join(r"steamapps\libraryfolders.vdf")) {
        for line in vdf.lines() {
            if let Some(v) = vdf_value(line, "path") {
                let p = PathBuf::from(v.replace("\\\\", "\\"));
                if !out.contains(&p) { out.push(p); }
            }
        }
    }
    out
}

/// `"key"   "value"` → value.
fn vdf_value<'a>(line: &'a str, key: &str) -> Option<&'a str> {
    let mut parts = line.trim().split('"').filter(|s| !s.trim().is_empty());
    (parts.next()? == key).then(|| parts.next()).flatten()
}

fn parse_manifest(text: &str) -> Option<(u32, String, u64)> {
    let (mut id, mut name, mut last) = (None, None, 0);
    for line in text.lines() {
        if id.is_none() { if let Some(v) = vdf_value(line, "appid") { id = v.parse().ok(); } }
        if name.is_none() { if let Some(v) = vdf_value(line, "name") { name = Some(v.to_string()); } }
        if let Some(v) = vdf_value(line, "LastPlayed") { last = v.parse().unwrap_or(0); }
    }
    Some((id?, name?, last))
}

fn clean_name(n: &str) -> String { n.replace(['™', '®'], "").trim().to_string() }

#[derive(Default)]
struct Art { hero: Option<PathBuf>, hero_blur: Option<PathBuf>, logo: Option<PathBuf>, cover: Option<PathBuf> }

impl Art {
    /// Steam keeps art either right in the app's folder or in hashed subfolders.
    fn find(dir: &Path) -> Art {
        let mut art = Art::default();
        let mut visit = |p: PathBuf| {
            let slot = match p.file_name().and_then(|n| n.to_str()) {
                Some("library_hero.jpg") => &mut art.hero,
                Some("library_hero_blur.jpg") => &mut art.hero_blur,
                Some("logo.png") => &mut art.logo,
                Some("library_600x900.jpg") | Some("library_capsule.jpg") => &mut art.cover,
                _ => return,
            };
            if slot.is_none() { *slot = Some(p); }
        };
        for e in fs::read_dir(dir).into_iter().flatten().flatten() {
            let p = e.path();
            if p.is_dir() { for f in fs::read_dir(&p).into_iter().flatten().flatten() { visit(f.path()); } } else { visit(p); }
        }
        art
    }
}

/// "hoje", "ontem", "há 15 dias", "há 2 meses".
pub fn ago_pt(last_played: u64, now: u64) -> String {
    if last_played == 0 { return "nunca".into(); }
    let d = now.saturating_sub(last_played) / 86_400;
    match d { 0 => "hoje".into(), 1 => "ontem".into(), 2..=30 => format!("há {d} dias"), _ => { let m = (d + 15) / 30; if m == 1 { "há 1 mês".into() } else { format!("há {m} meses") } } }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn manifest() {
        let t = "\"AppState\"\n{\n\t\"appid\"\t\t\"814380\"\n\t\"name\"\t\t\"Sekiro™: Shadows Die Twice\"\n\t\"LastPlayed\"\t\t\"1792885204\"\n}";
        let (id, name, last) = parse_manifest(t).unwrap();
        assert_eq!((id, clean_name(&name).as_str(), last), (814380, "Sekiro: Shadows Die Twice", 1792885204));
    }
    #[test]
    fn ago() {
        assert_eq!(ago_pt(1000, 1000), "hoje");
        assert_eq!(ago_pt(0, 1000), "nunca");
        assert_eq!(ago_pt(0 + 1, 15 * 86_400 + 1), "há 15 dias");
        assert_eq!(ago_pt(1, 60 * 86_400), "há 2 meses");
    }
}
