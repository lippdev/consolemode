//! Where the app's window goes during a session: out of the way while the launcher plays, above the
//! game (full screen, topmost) when ☰ is held, and back to how it was when the session ends.

use std::sync::atomic::{AtomicBool, Ordering};

use gpui::Window;

/// The window went full screen for the session menu (it was a normal window before).
static MADE_FULLSCREEN: AtomicBool = AtomicBool::new(false);
/// It was full screen already (`--fullscreen`) when the confirm screen had to move it to the game screen.
static WAS_FULLSCREEN: AtomicBool = AtomicBool::new(false);
static HOME_FULLSCREEN: AtomicBool = AtomicBool::new(false);
pub fn is_gaming_home() -> bool { HOME_FULLSCREEN.load(Ordering::Relaxed) }

/// A Gaming Home activation makes fullscreen the new home state, including when the
/// window had previously entered fullscreen temporarily for the session menu.
pub fn gaming_home(w: &mut Window) {
    if !w.is_fullscreen() || MADE_FULLSCREEN.load(Ordering::Relaxed) {
        HOME_FULLSCREEN.store(true, Ordering::Relaxed);
    }
    MADE_FULLSCREEN.store(false, Ordering::Relaxed);
    WAS_FULLSCREEN.store(false, Ordering::Relaxed);
    if !w.is_fullscreen() { w.toggle_fullscreen(); }
}

/// Undo only fullscreen entered by Gaming Home, returning to the saved window bounds.
pub fn leave_gaming_home(w: &mut Window) {
    if HOME_FULLSCREEN.swap(false, Ordering::Relaxed) && w.is_fullscreen() {
        w.toggle_fullscreen();
    }
}

/// The session menu over the game. First out of the minimized state, then full screen: going full
/// screen while minimized kept the old window size.
pub fn over_game(w: &mut Window) {
    cm_platform::restore_window(w);
    cm_platform::set_topmost(w, true);
    cm_platform::force_foreground(w);
}

/// Second half of `over_game`, a moment later (once Windows has restored the window).
pub fn over_game_fullscreen(w: &mut Window) {
    // a window that was full screen to begin with stays so at the end (back_home only undoes what was made here)
    if !w.is_fullscreen() { w.toggle_fullscreen(); MADE_FULLSCREEN.store(!WAS_FULLSCREEN.load(Ordering::Relaxed), Ordering::Relaxed); }
    cm_platform::set_topmost(w, true);
    cm_platform::force_foreground(w);
}

/// "Está vendo esta tela?": the window goes to the game screen (its rectangle in pixels) above everything, in front.
/// Full screen comes a moment later (`over_game_fullscreen`): a full screen window stays on the screen it was on,
/// so one that already is full screen leaves it first and comes back on the right screen.
pub fn confirm_on_game_screen(w: &mut Window, rect: Option<(i32, i32, i32, i32)>) {
    cm_platform::restore_window(w);
    if w.is_fullscreen() {
        if !MADE_FULLSCREEN.load(Ordering::Relaxed) { WAS_FULLSCREEN.store(true, Ordering::Relaxed); }
        w.toggle_fullscreen();
        MADE_FULLSCREEN.store(false, Ordering::Relaxed);
    }
    if let Some(r) = rect { cm_platform::move_window_to(w, r); }
    cm_platform::set_topmost(w, true);
    cm_platform::force_foreground(w);
}

/// Back to the game: out of the way.
pub fn step_aside(w: &mut Window) {
    cm_platform::set_topmost(w, false);
    w.minimize_window();
}

/// The session ended: the window as it was before it. Leaving full screen goes back to the state
/// saved when it entered, which was "minimized" (it entered from the game), so `back_home_front`
/// follows a moment later.
pub fn back_home(w: &mut Window) {
    cm_platform::restore_window(w);
    if MADE_FULLSCREEN.swap(false, Ordering::Relaxed) && w.is_fullscreen() { w.toggle_fullscreen(); }
    WAS_FULLSCREEN.store(false, Ordering::Relaxed);
}

/// In front: topmost for a moment so it lands above a full-screen game left open, then a normal window.
pub fn back_home_front(w: &mut Window) {
    cm_platform::restore_window(w);
    cm_platform::set_topmost(w, true);
    cm_platform::force_foreground(w);
    cm_platform::set_topmost(w, false);
}
