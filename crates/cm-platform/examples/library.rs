//! Prints what the Home row would show: `cargo run -p cm-platform --example library`
fn main() {
    let t = std::time::Instant::now();
    let Some(dir) = cm_platform::steam::steam_dir() else { println!("no Steam"); return };
    for g in cm_platform::steam::recent_games(&dir, 12) { println!("{:>8} {:<40} last {}", g.app_id, g.name, g.last_played); }
    println!("({:?})", t.elapsed());
}
