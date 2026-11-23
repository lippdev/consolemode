//! Reads the installed 1.7 config (read only) and checks 2.0 carries every field over.
fn main() {
    let path = std::path::Path::new(&std::env::var("LOCALAPPDATA").unwrap()).join(r"ConsoleMode\config.json");
    let text = std::fs::read_to_string(&path).expect("no 1.7 config");
    let c = cm_core::config::Config::from_json(&text).expect("1.7 config did not parse");
    let original: serde_json::Value = serde_json::from_str(text.trim_start_matches('\u{feff}')).unwrap();
    let back: serde_json::Value = serde_json::from_str(&c.to_json()).unwrap();
    let mut lost = vec![];
    for (k, v) in original.as_object().unwrap() {
        if back.get(k) != Some(v) { lost.push(k.clone()); }
    }
    println!("1.7 keys: {}, kept unchanged: {}, changed/lost: {:?}", original.as_object().unwrap().len(), original.as_object().unwrap().len() - lost.len(), lost);
    println!("launcher {}, focus {}, hide {:?}, session ready: {}", c.fullscreen_mode, c.focus_monitor, c.hide_monitors, c.is_session_ready());
}
