//! Alpha-only control protocol and registry command construction.
use serde_json::{Value, json};

pub const MUTEX: &str = r"Local\ConsoleMode2Alpha.Instance";
pub const PIPE: &str = r"\\.\pipe\ConsoleMode2Alpha.Control";
pub const SCHEME: &str = "consolemode2alpha";
pub const RUN_VALUE: &str = "ConsoleMode2Alpha";
pub const RUN_KEY: &str = r"Software\Microsoft\Windows\CurrentVersion\Run";
pub const CLASS_KEY: &str = r"Software\Classes\consolemode2alpha";

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Command {
    Show,
    GamingHome,
    Start,
    Stop,
    Menu,
    Status,
    Exit,
}
impl Command {
    pub fn name(self) -> &'static str {
        match self {
            Self::Show => "show",
            Self::GamingHome => "gaming-home",
            Self::Start => "start",
            Self::Stop => "stop",
            Self::Menu => "menu",
            Self::Status => "status",
            Self::Exit => "exit",
        }
    }
    fn parse(s: &str) -> Option<Self> {
        match s.trim().to_ascii_lowercase().as_str() {
            "show" => Some(Self::Show),
            "gaming-home" => Some(Self::GamingHome),
            "start" => Some(Self::Start),
            "stop" => Some(Self::Stop),
            "menu" => Some(Self::Menu),
            "status" => Some(Self::Status),
            _ => None,
        }
    }
}
pub fn parse_request(line: &str) -> Option<Command> {
    let v: Value = serde_json::from_str(line).ok()?;
    Command::parse(v.get("cmd")?.as_str()?)
}
pub fn parse_uri(s: &str) -> Option<Command> {
    let (scheme, rest) = s.split_once(':')?;
    if !scheme.eq_ignore_ascii_case(SCHEME) {
        return None;
    }
    let action = rest.trim_start_matches('/').split(['/', '?', '#']).next()?;
    Command::parse(action).filter(|c| *c != Command::Status)
}
pub fn initial_command(args: &[String]) -> Command {
    args.iter()
        .find_map(|a| parse_uri(a).or_else(|| a.strip_prefix("--").and_then(Command::parse)))
        .unwrap_or(Command::Show)
}
// Executable paths cannot contain quotes on Windows; reject malformed input rather than register it.
pub fn quoted_exe(exe: &str) -> Result<String, &'static str> {
    if exe.is_empty() || exe.contains(['"', '\0', '\r', '\n']) {
        return Err("Caminho do executável inválido");
    }
    Ok(format!("\"{exe}\""))
}
pub fn startup_command(exe: &str) -> Result<String, &'static str> {
    Ok(format!("{} --tray", quoted_exe(exe)?))
}
pub fn protocol_command(exe: &str) -> Result<String, &'static str> {
    Ok(format!("{} \"%1\"", quoted_exe(exe)?))
}
pub fn reply(ok: bool, active: bool, launcher: &str, focus: &str, error: Option<&str>) -> String {
    let mut v = json!({"ok":ok,"active":active,"launcher":launcher,"focusScreenId":focus});
    if let Some(e) = error {
        v["error"] = e.into();
    }
    v.to_string()
}
#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn wire_commands() {
        for c in [
            Command::Show,
            Command::GamingHome,
            Command::Start,
            Command::Stop,
            Command::Menu,
            Command::Status,
        ] {
            assert_eq!(
                parse_request(&format!(r#"{{"cmd":" {} "}}"#, c.name().to_uppercase())),
                Some(c)
            );
        }
        for bad in [
            "",
            "[]",
            "null",
            r#"{"cmd":2}"#,
            r#"{"cmd":"exit"}"#,
            "show",
            "{invalid",
        ] {
            assert_eq!(parse_request(bad), None);
        }
    }
    #[test]
    fn links_are_alpha_only() {
        for uri in [
            "consolemode2alpha://START/",
            "CONSOLEMODE2ALPHA:start?x=1",
            "consolemode2alpha:///start#x",
        ] {
            assert_eq!(parse_uri(uri), Some(Command::Start));
        }
        for uri in [
            "consolemode://start",
            "consolemode2alpha://status",
            "consolemode2alpha://exit",
            "consolemode2alpha://bad",
        ] {
            assert_eq!(parse_uri(uri), None);
        }
    }
    #[test]
    fn gaming_home_activation_is_forwardable() {
        let args = vec!["ConsoleMode2.exe".into(), "--gaming-home".into()];
        assert_eq!(initial_command(&args), Command::GamingHome);
        assert_eq!(parse_request(&format!(r#"{{"cmd":"{}"}}"#, Command::GamingHome.name())), Some(Command::GamingHome));
    }
    #[test]
    fn registry_commands_are_quoted_and_isolated() {
        let exe = r"C:\Program Files\Console Mode\ConsoleMode2.exe";
        assert_eq!(startup_command(exe).unwrap(), format!("\"{exe}\" --tray"));
        assert_eq!(protocol_command(exe).unwrap(), format!("\"{exe}\" \"%1\""));
        for bad in ["", "a\"b", "a\0b", "a\nb"] {
            assert!(quoted_exe(bad).is_err());
        }
        assert_ne!(RUN_VALUE, "ConsoleMode");
        assert_ne!(CLASS_KEY, r"Software\Classes\consolemode");
    }
    #[test]
    fn alpha_settings_default_off_and_round_trip() {
        let mut c = crate::config::Config::from_json(r#"{"minimizeToTray":true}"#).unwrap();
        assert!(!c.start_with_windows && !c.start_in_tray && !c.protocol_registered);
        c.start_with_windows = true;
        c.start_in_tray = true;
        c.protocol_registered = true;
        assert_eq!(crate::config::Config::from_json(&c.to_json()).unwrap(), c);
    }
    #[test]
    fn status_escapes_ids() {
        let v: Value =
            serde_json::from_str(&reply(true, true, "customApp", r"MONITOR\x", None)).unwrap();
        assert_eq!(v["active"], true);
        assert_eq!(v["focusScreenId"], r"MONITOR\x");
    }
}
