//! Read-only look at what the session menu would act on.
fn main() {
    use cm_platform::session_menu_win::*;
    for w in list_windows() { println!("{:<20} {}", w.process, w.title); }
    println!("volume: {:?}", volume());
}
