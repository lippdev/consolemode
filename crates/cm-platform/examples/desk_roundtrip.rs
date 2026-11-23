//! Backs up the current display layout and restores the very same layout (nothing should change).
fn main() {
    let dir = std::env::temp_dir().join("cm2-desk-test");
    let mut d = cm_platform::WinDesk::new(dir);
    let before: Vec<_> = cm_platform::displays::list_displays().unwrap().into_iter().filter(|x| x.active).map(|x| (x.stable_id, x.primary, x.position, x.current_mode)).collect();
    let b = d.backup().expect("backup");
    println!("backup at {b}");
    println!("audio default: {:?}", d.audio_default());
    let t = std::time::Instant::now();
    println!("restore same layout: {:?} in {:?}", d.restore(&b), t.elapsed());
    let after: Vec<_> = cm_platform::displays::list_displays().unwrap().into_iter().filter(|x| x.active).map(|x| (x.stable_id, x.primary, x.position, x.current_mode)).collect();
    println!("unchanged: {}", before == after);
    for x in &after { println!("  {x:?}"); }
}
