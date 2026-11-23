//! One line per display and active audio output: `cargo run -p cm-platform --example screens`
fn main() {
    for d in cm_platform::displays::list_displays().unwrap_or_default() {
        println!(
            "{} | {} | active={} primary={} | {:?} | modes={} | hdr {:?}/{:?} | {}",
            d.friendly_name, d.gdi_name, d.active, d.primary,
            d.current_mode.as_ref().map(|m| format!("{}x{}@{}", m.width, m.height, m.refresh_hz)),
            d.supported_modes.len(), d.hdr_supported, d.hdr_enabled, d.stable_id
        );
    }
    for a in cm_platform::audio::list_outputs().unwrap_or_default().iter().filter(|a| a.is_active) {
        println!("audio: {} default={}", a.friendly_name, a.is_default);
    }
}
