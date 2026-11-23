//! Read-only look at what the launch layer sees: `cargo run -p cm-platform --example launchers`
fn main() {
    use cm_platform::launch::*;
    let w = windows();
    println!("visible windows: {}, processes: {}", w.len(), processes().len());
    println!("explorer running: {}, steam running: {}", is_running("explorer.exe"), is_running("steam"));
    println!("playnite exe: {:?}", playnite_exe(""));
    println!("big picture window: {:?}", big_picture_window().map(|w| (w.title, w.class)));
    println!("playnite window: {:?}", playnite_window().map(|w| w.title));
    for x in w.iter().filter(|x| x.area() > 200_000).take(6) { println!("  big window: {:?} [{}] fullscreen={}", x.title, x.class, is_fullscreen(x)); }
}
