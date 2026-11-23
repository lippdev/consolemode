//! Lists the HID collections, the Bluetooth batteries Windows keeps and what the centre-button
//! reader sees (1.7.1 #225/#226): `cargo run -p cm-platform --example hid_probe`.
fn main() {
    #[cfg(windows)]
    {
        use cm_platform::hid_win;
        let paths = hid_win::paths();
        println!("{} HID collections", paths.len());
        for p in &paths {
            if let Some(i) = hid_win::query(p) {
                println!("  {:04X}:{:04X} page 0x{:02X} usage 0x{:02X} in {}{}", i.vendor, i.product, i.page, i.usage, i.input_len, if i.is_game_controller() { "  <- game controller" } else { "" });
            }
        }
        let t = std::time::Instant::now();
        println!("Bluetooth batteries ({:?}): {:?}", { let b = hid_win::bluetooth_batteries(); (t.elapsed(), b) }.0, hid_win::bluetooth_batteries());
        hid_win::set_logger(|l| println!("log: {l}"));
        hid_win::start_home_reader();
        std::thread::sleep(std::time::Duration::from_secs(4));
        for line in hid_win::describe() { println!("{line}"); }
        println!("held: {:?}", hid_win::home_held());
    }
}
