//! Which windows the session menu's "Janelas" lists: the ones Alt + Tab would (1.7 AltTabRules).

/// What is known about a top-level window.
#[derive(Clone, Debug, Default)]
pub struct WindowFacts {
    pub title: String,
    pub class: String,
    pub visible: bool,
    pub cloaked: bool,
    pub has_owner: bool,
    pub tool_window: bool,
    pub app_window: bool,
    pub own_process: bool,
}

const SHELL_CLASSES: [&str; 5] = ["Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Windows.UI.Core.CoreWindow"];

pub fn should_list(w: &WindowFacts) -> bool {
    if !w.visible || w.cloaked || w.own_process || w.title.trim().is_empty() { return false; }
    if SHELL_CLASSES.contains(&w.class.as_str()) { return false; }
    w.app_window || (!w.tool_window && !w.has_owner)
}

/// "steamwebhelper.exe" → "Steamwebhelper".
pub fn process_display_name(name: &str) -> String {
    let n = name.trim();
    let n = n.strip_suffix(".exe").or_else(|| n.strip_suffix(".EXE")).unwrap_or(n);
    let mut c = n.chars();
    match c.next() { Some(f) => f.to_uppercase().collect::<String>() + c.as_str(), None => String::new() }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn w() -> WindowFacts { WindowFacts { title: "Notepad".into(), class: "Notepad".into(), visible: true, ..Default::default() } }

    #[test]
    fn rules() {
        assert!(should_list(&w()));
        assert!(!should_list(&WindowFacts { cloaked: true, ..w() }));
        assert!(!should_list(&WindowFacts { own_process: true, ..w() }));
        assert!(!should_list(&WindowFacts { title: " ".into(), ..w() }));
        assert!(!should_list(&WindowFacts { class: "Progman".into(), ..w() }));
        assert!(!should_list(&WindowFacts { tool_window: true, ..w() }));
        assert!(should_list(&WindowFacts { tool_window: true, app_window: true, ..w() }), "app window wins");
        assert!(!should_list(&WindowFacts { has_owner: true, ..w() }));
        assert_eq!(process_display_name("steamwebhelper.exe"), "Steamwebhelper");
        assert_eq!(process_display_name(""), "");
    }
}
