//! Custom-app compatibility with 1.7, independent of Windows metadata and UI.
use std::{collections::HashSet, path::Path};
use serde::{Deserialize, Serialize};
use crate::{config::Config, session::Launcher};

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct CustomAppEntry {
    pub id: String,
    pub path: String,
    pub arguments: String,
    pub watch_process: String,
    #[serde(flatten)]
    pub other: serde_json::Map<String, serde_json::Value>,
}

impl CustomAppEntry {
    pub fn launcher(&self) -> Launcher {
        Launcher::Custom { path: clean_path(&self.path).into(), args: self.arguments.clone(), watch: self.watch_process.clone() }
    }
}

pub fn clean_path(path: &str) -> &str { path.trim().trim_matches('"').trim() }

pub fn display_name(path: &str, description: &str) -> String {
    if !description.trim().is_empty() { return description.trim().into(); }
    let file = clean_path(path).rsplit(['\\', '/']).next().unwrap_or("");
    if file.to_ascii_lowercase().ends_with(".exe") { file[..file.len() - 4].trim().into() } else { file.trim().into() }
}

#[derive(Debug, PartialEq, Eq)]
pub enum Problem { Missing, NotExe, NotFound }

pub fn validate(path: &str) -> Result<(), Problem> {
    let p = clean_path(path);
    if p.is_empty() { return Err(Problem::Missing); }
    if !p.to_ascii_lowercase().ends_with(".exe") { return Err(Problem::NotExe); }
    if !Path::new(p).is_file() { return Err(Problem::NotFound); }
    Ok(())
}

pub fn next_id<'a>(ids: impl IntoIterator<Item = &'a str>) -> String {
    let used: HashSet<_> = ids.into_iter().collect();
    (1u64..).map(|n| n.to_string()).find(|n| !used.contains(n.as_str())).unwrap()
}

pub fn entries(config: &Config) -> Vec<CustomAppEntry> {
    let mut list: Vec<_> = config.custom_apps.iter().filter(|e| !e.id.is_empty() && !clean_path(&e.path).is_empty()).cloned().collect();
    if list.is_empty() && !clean_path(&config.custom_app_path).is_empty() {
        list.push(CustomAppEntry { id: "1".into(), path: clean_path(&config.custom_app_path).into(), arguments: config.custom_app_arguments.clone(), watch_process: config.custom_app_watch_process.clone(), ..Default::default() });
    }
    list
}

pub fn selected<'a>(entries: &'a [CustomAppEntry], id: &str) -> Option<&'a CustomAppEntry> {
    entries.iter().find(|e| e.id == id).or_else(|| entries.first())
}

/// Keep the old single-app fields aligned, including when the last app is removed.
pub fn sync_selected(config: &mut Config) {
    let app = selected(&config.custom_apps, &config.custom_app_id).cloned().unwrap_or_default();
    config.custom_app_id = app.id;
    config.custom_app_path = app.path;
    config.custom_app_arguments = app.arguments;
    config.custom_app_watch_process = app.watch_process;
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn names_and_ids_match_17() {
        assert_eq!(display_name(r"C:\Emu\Emu.EXE", " RetroArch "), "RetroArch");
        assert_eq!(display_name(r#" "C:\Emu\Emu.eXe" "#, ""), "Emu");
        for (ids, expected) in [(vec![], "1"), (vec!["1", "2"], "3"), (vec!["2", "3"], "1"), (vec!["1", "3"], "2")] { assert_eq!(next_id(ids), expected); }
    }
    #[test]
    fn list_selection_and_legacy_fallback() {
        let mut c = Config::from_json(r#"{"customAppPath":"\"C:\\Emu\\retroarch.exe\"","customAppArguments":"-f","customAppWatchProcess":"retroarch"}"#).unwrap();
        let list = entries(&c);
        assert_eq!(list[0].id, "1");
        assert_eq!(list[0].path, r"C:\Emu\retroarch.exe");
        assert_eq!(list[0].arguments, "-f");
        assert_eq!(list[0].watch_process, "retroarch");
        c.custom_apps = vec![CustomAppEntry { id: "2".into(), path: "a.exe".into(), ..Default::default() }, CustomAppEntry { id: "4".into(), path: " ".into(), ..Default::default() }, CustomAppEntry { id: "5".into(), path: "b.exe".into(), ..Default::default() }];
        let list = entries(&c);
        assert_eq!(list.iter().map(|e| e.id.as_str()).collect::<Vec<_>>(), ["2", "5"]);
        assert_eq!(selected(&list, "5").unwrap().id, "5");
        assert_eq!(selected(&list, "9").unwrap().id, "2");
        c.custom_apps.clear(); sync_selected(&mut c);
        assert!(entries(&c).is_empty());
        assert!(selected(&[], "1").is_none());
    }
    #[test]
    fn exact_17_shape_and_unknown_entry_fields_round_trip() {
        let raw = r#"{"customAppId":"2","customApps":[{"id":"2","path":"C:\\app.exe","arguments":"--fullscreen","watchProcess":"game.exe","future":42}]}"#;
        let c = Config::from_json(raw).unwrap();
        let back: serde_json::Value = serde_json::from_str(&c.to_json()).unwrap();
        let original: serde_json::Value = serde_json::from_str(raw).unwrap();
        assert_eq!(back["customApps"], original["customApps"]);
        assert_eq!(back["customAppId"], "2");
        assert_eq!(Config::from_json(&c.to_json()).unwrap(), c);
        assert_eq!(c.custom_apps[0].launcher(), Launcher::Custom { path: r"C:\app.exe".into(), args: "--fullscreen".into(), watch: "game.exe".into() });
    }
    #[test]
    fn validation_matches_17() {
        let dir = std::env::temp_dir().join(format!("cm-custom-validation-{}", std::process::id()));
        std::fs::create_dir_all(&dir).unwrap();
        let exe = dir.join("app.EXE"); std::fs::write(&exe, "").unwrap();
        assert_eq!(validate(&format!(" \"{}\" ", exe.display())), Ok(()));
        assert_eq!(validate(""), Err(Problem::Missing));
        assert_eq!(validate("notes.txt"), Err(Problem::NotExe));
        assert_eq!(validate(&dir.join("gone.exe").to_string_lossy()), Err(Problem::NotFound));
        std::fs::create_dir_all(dir.join("folder.exe")).unwrap();
        assert_eq!(validate(&dir.join("folder.exe").to_string_lossy()), Err(Problem::NotFound));
        std::fs::remove_dir_all(dir).unwrap();
    }
}
