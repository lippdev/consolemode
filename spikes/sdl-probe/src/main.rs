use std::time::{Duration, Instant};
fn main() {
    let secs: u64 = std::env::args().nth(1).and_then(|s| s.parse().ok()).unwrap_or(6);
    // the app reads the pad while a game or launcher has the focus
    sdl3::hint::set("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
    let sdl = sdl3::init().unwrap();
    let gc = sdl.gamepad().unwrap();
    let mut pump = sdl.event_pump().unwrap();
    let mut open = Vec::new();
    let t = Instant::now();
    while t.elapsed() < Duration::from_secs(secs) {
        for e in pump.poll_iter() {
            use sdl3::event::Event::*;
            match e {
                GamepadAdded { which, .. } => {
                    match gc.open(which) { Ok(p) => { println!("[sdl] {:>6.3}s added {:?} \"{}\" type {:?}", t.elapsed().as_secs_f32(), which, p.name().unwrap_or_default(), p.r#type()); open.push(p); } Err(e) => println!("[sdl] open failed {e}") }
                }
                GamepadRemoved { which, .. } => println!("[sdl] {:>6.3}s removed {which:?}", t.elapsed().as_secs_f32()),
                GamepadButtonDown { which, button, .. } => println!("[sdl] {:>6.3}s {which:?} down {button:?}", t.elapsed().as_secs_f32()),
                _ => {}
            }
        }
        std::thread::sleep(Duration::from_millis(5));
    }
}
