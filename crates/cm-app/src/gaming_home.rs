//! Windows Gaming Full Screen Experience requests and state observation.
//! The API set is optional on older Windows versions.
use std::ffi::{c_char, c_void};
use std::sync::{Mutex, OnceLock, atomic::{AtomicBool, AtomicU64, Ordering}};

static TRANSITION: Mutex<()> = Mutex::new(());
static ENTRY_PENDING: AtomicBool = AtomicBool::new(false);
static ENTRY_GENERATION: AtomicU64 = AtomicU64::new(0);
static RETURN_PENDING: AtomicBool = AtomicBool::new(false);

pub fn entry_pending() -> bool { ENTRY_PENDING.load(Ordering::Acquire) }
pub fn entry_generation() -> u64 { ENTRY_GENERATION.load(Ordering::Acquire) }
pub fn complete_entry() { ENTRY_PENDING.store(false, Ordering::Release); }
pub fn return_pending() -> bool { RETURN_PENDING.load(Ordering::Acquire) }
pub fn complete_return() { RETURN_PENDING.store(false, Ordering::Release); }

type Query = unsafe extern "system" fn() -> i32;
type SetState = unsafe extern "system" fn(i32) -> i32;

struct GamingApi {
    active: Query,
    supported: Query,
    can_set: Option<Query>,
    set: Option<SetState>,
}

fn api() -> Option<&'static GamingApi> {
    static API: OnceLock<Option<GamingApi>> = OnceLock::new();
    API.get_or_init(|| {
        #[link(name = "kernel32")]
        unsafe extern "system" {
            fn LoadLibraryExW(name: *const u16, file: *mut c_void, flags: u32) -> *mut c_void;
            fn GetProcAddress(module: *mut c_void, name: *const c_char) -> *mut c_void;
        }
        let dll: Vec<u16> = "api-ms-win-gaming-experience-l1-1-0.dll\0".encode_utf16().collect();
        // Keep this system module loaded for the lifetime of the cached SDK function pointers.
        unsafe {
            let module = LoadLibraryExW(dll.as_ptr(), std::ptr::null_mut(), 0x800); // LOAD_LIBRARY_SEARCH_SYSTEM32
            if module.is_null() { return None; }
            let lookup = |name: &std::ffi::CStr| std::ptr::NonNull::new(GetProcAddress(module, name.as_ptr()));
            Some(GamingApi {
                active: std::mem::transmute::<*mut c_void, Query>(lookup(c"IsGamingFullScreenExperienceActive")?.as_ptr()),
                supported: std::mem::transmute::<*mut c_void, Query>(lookup(c"IsGamingFullScreenExperienceSupported")?.as_ptr()),
                can_set: lookup(c"CanSetGamingFullScreenExperience").map(|p| std::mem::transmute::<*mut c_void, Query>(p.as_ptr())),
                set: lookup(c"SetGamingFullScreenExperience").map(|p| std::mem::transmute::<*mut c_void, SetState>(p.as_ptr())),
            })
        }
    }).as_ref()
}

pub fn active() -> Option<bool> { api().map(|api| unsafe { (api.active)() != 0 }) }
pub fn supported() -> Option<bool> { api().map(|api| unsafe { (api.supported)() != 0 }) }

/// Request a change only when the current state is known and differs. Call off the UI thread:
/// Windows may take several seconds to complete the transition. Never retry the toggle, since
/// its result can arrive late and a second toggle could undo it.
pub fn set_active(target: bool) -> Result<(), &'static str> {
    let _transition = TRANSITION.lock().map_err(|_| "Transição do FSE indisponível")?;
    if supported() != Some(true) { return Err("FSE não é suportado neste dispositivo"); }
    let current = active().ok_or("FSE indisponível nesta versão do Windows")?;
    if current == target {
        if target { ENTRY_GENERATION.fetch_add(1, Ordering::AcqRel); }
        ENTRY_PENDING.store(target, Ordering::Release);
        return Ok(());
    }
    let api = api().ok_or("FSE indisponível nesta versão do Windows")?;
    if let (Some(can_set), Some(set)) = (api.can_set, api.set) {
        if unsafe { can_set() } == 0 { return Err("O Windows não permite alterar o FSE agora"); }
        if unsafe { set(i32::from(target)) } < 0 { return Err("O Windows recusou alterar o FSE"); }
    } else {
        // Older API sets may expose the state but not the explicit setter.
        cm_platform::launch::start_xbox();
    }
    // Windows can present a confirmation dialog. Success means the request was accepted,
    // not that the user has already answered it. The shell observes the eventual state.
    if target { ENTRY_GENERATION.fetch_add(1, Ordering::AcqRel); }
    ENTRY_PENDING.store(target, Ordering::Release);
    Ok(())
}

impl crate::home::Console {
    /// One fallback after a direct foreground attempt, only if Xbox still owns focus.
    pub(crate) fn rescue_home_foreground(&mut self, cx: &mut gpui::Context<Self>) {
        cx.spawn(async move |this, cx| {
            cx.background_executor().timer(std::time::Duration::from_millis(600)).await;
            let deadline = std::time::Instant::now() + std::time::Duration::from_secs(60);
            loop {
                if this.read_with(cx, |c, _| c.running.is_some()).unwrap_or(true) { return; }
                let foreground = cx.background_executor().spawn(async move { cm_platform::launch::fse_foreground() }).await;
                match foreground {
                    cm_platform::launch::FseForeground::Xbox => break,
                    cm_platform::launch::FseForeground::Confirmation if std::time::Instant::now() < deadline => {
                        cx.background_executor().timer(std::time::Duration::from_millis(500)).await;
                    }
                    _ => return,
                }
            }
            let registered = cx.background_executor().spawn(async move {
                active() == Some(true) && cm_platform::launch::fse_foreground() == cm_platform::launch::FseForeground::Xbox
                    && cm_platform::fse_win::status().is_ok_and(|s| s.registered)
            }).await;
            if !registered || this.read_with(cx, |c, _| c.running.is_some()).unwrap_or(true) { return; }
            cx.background_executor().spawn(async move {
                // The package lookup can take time: check foreground again before invoking Shell.
                if active() != Some(true) || cm_platform::launch::fse_foreground() != cm_platform::launch::FseForeground::Xbox { return; }
                match cm_platform::launch::activate_gaming_home() {
                    Ok(_) => crate::startup_log("FSE: ativação alternativa da Home enviada ao Shell"),
                    Err(error) => crate::startup_log(&format!("FSE: falha ao ativar Home pelo Shell: {error}")),
                }
            }).await;
        }).detach();
    }

    /// An explicit return to the desktop waits for session restoration before leaving FSE.
    pub(crate) fn return_to_pc(&mut self, cx: &mut gpui::Context<Self>) {
        if active() != Some(true) && !crate::stage::is_gaming_home() {
            complete_entry();
            if let Some(r) = &self.running { let _ = r.commands.send(crate::engine::Command::Stop); }
            return;
        }
        if let Some(r) = &self.running {
            self.return_to_pc_pending = true;
            let _ = r.commands.send(crate::engine::Command::Stop);
            return;
        }
        self.return_to_pc_pending = false;
        cx.spawn(async move |this, cx| {
            let result = cx.background_executor().spawn(async move {
                if supported() == Some(true) { set_active(false).map(|_| true) } else { Ok(false) }
            }).await;
            let _ = this.update(cx, |c, cx| {
                match result {
                    Ok(_) => {
                        if active() == Some(true) {
                            RETURN_PENDING.store(true, Ordering::Release);
                            return;
                        }
                        c.app_in_front = false;
                        if let Some(window) = c.window {
                            cx.defer(move |cx| { let _ = window.update(cx, |_, w, _| {
                                cm_platform::shell_win::hide_window(w);
                                crate::stage::leave_gaming_home(w);
                                cm_platform::shell_win::hide_window(w);
                            }); });
                        }
                    }
                    Err(error) => {
                        crate::startup_log(&format!("Voltar ao PC: {error}"));
                        if let Some(window) = c.window {
                            cx.defer(move |cx| { let _ = window.update(cx, |_, w, _| crate::stage::back_home_front(w)); });
                        }
                    }
                }
            });
        }).detach();
    }
}
