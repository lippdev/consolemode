//! Read-only look at what the session extras would work with on this PC: RTSS, the pointer, the
//! desktop icons, Steam's client, the Game Bar setting. Changes nothing (no limit set, no pointer
//! move, no icon moved, no window closed, RTSS is not started).

#[cfg(windows)]
fn main() {
    use cm_platform::{desktop_icons, gamebar, pointer, rtss, steam_client};

    println!("RTSS instalado: {:?}", rtss::install_path());
    println!("RTSS rodando: {}", rtss::is_running());
    let data = std::env::var_os("LOCALAPPDATA").map(|l| std::path::PathBuf::from(l).join("ConsoleMode2")).unwrap_or_default();
    println!("rtss-cli: {:?}", rtss::cli_path(&data));
    if rtss::is_running() {
        match rtss::Rtss::open(&data).and_then(|r| r.backup()) {
            Ok(b) => println!("RTSS agora: limite {} limitador {}", b.framerate_limit, b.limiter_enabled),
            Err(e) => println!("RTSS agora: {e}"),
        }
    }
    println!("Ponteiro: {:?}", pointer::position());
    if let Some((x, y)) = pointer::position() { println!("  numa tela ligada: {}", pointer::is_on_screen(x, y)); }
    match desktop_icons::read() {
        Ok(icons) => {
            println!("Ícones: {}", icons.len());
            for i in icons.iter().take(8) { println!("  {:?}", i); }
        }
        Err(e) => println!("Ícones: {e}"),
    }
    println!("Steam rodando: {}", steam_client::is_running());
    println!("Steam RunningAppID: {:?}", steam_client::running_app_id());
    println!("Game Bar: botão Xbox abre a Game Bar: {}", gamebar::guide_opens_game_bar());
}

#[cfg(not(windows))]
fn main() {
    eprintln!("Requires Windows.");
}
