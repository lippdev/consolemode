//! RivaTuner Statistics Server around a session (ported from 1.7's RtssService): the frame limit
//! goes through `rtss-cli.exe` (RTSS's own settings, read back and put back exactly), and our FPS
//! counter is written straight into RTSS's shared memory, in a slot of its own. The counter text is
//! built by cm_core::rtss_overlay.
//!
//! Nothing here runs unless the caller asks; every call that changes something has its undo
//! (`Rtss::restore`, `Counter::clear`).

use std::{
    ffi::c_void,
    io::Read,
    os::windows::process::CommandExt,
    path::{Path, PathBuf},
    process::{Command, Stdio},
    sync::{
        Arc, Mutex,
        atomic::{AtomicBool, AtomicI32, AtomicU32, Ordering},
    },
    time::{Duration, Instant},
};

use cm_core::{
    rtss_overlay::{self, FpsOverlayLayout, HardwareSample},
    session::RtssBackup,
};
use windows::{
    Win32::{
        Foundation::{CloseHandle, FILETIME, HANDLE},
        System::{
            Memory::{FILE_MAP_ALL_ACCESS, MEMORY_MAPPED_VIEW_ADDRESS, MapViewOfFile, OpenFileMappingW, UnmapViewOfFile},
            SystemInformation::{GlobalMemoryStatusEx, MEMORYSTATUSEX},
            Threading::{GetCurrentThread, GetSystemTimes, SetThreadPriority, THREAD_PRIORITY_ABOVE_NORMAL},
        },
    },
    core::w,
};

use crate::launch;

const CREATE_NO_WINDOW: u32 = 0x0800_0000;

/// `RTSS.exe` in the usual install folders.
pub fn install_path() -> Option<PathBuf> {
    ["ProgramFiles(x86)", "ProgramFiles"].iter()
        .filter_map(|v| std::env::var_os(v))
        .map(|p| PathBuf::from(p).join("RivaTuner Statistics Server").join("RTSS.exe"))
        .find(|p| p.is_file())
}

pub fn is_installed() -> bool { install_path().is_some() }

pub fn is_running() -> bool { ["RTSS", "RTSSHooksLoader64", "RTSSHooksLoader"].iter().any(|n| launch::is_running(n)) }

/// Starts RTSS (hidden in its tray) when it is not running. True when it is up.
pub fn ensure_running() -> bool {
    if is_running() { return true; }
    let Some(exe) = install_path() else { return false };
    launch::start_exe(&exe.to_string_lossy(), "");
    std::thread::sleep(Duration::from_millis(1500));
    is_running()
}

/// Where `rtss-cli.exe` is: 2.0's own tools folder, beside the app, or the copy 1.7's installer
/// leaves in its data folder (only run, never changed).
pub fn cli_path(data_dir: &Path) -> Option<PathBuf> {
    let mut candidates = vec![data_dir.join("tools").join("rtss-cli.exe")];
    if let Ok(exe) = std::env::current_exe() {
        if let Some(dir) = exe.parent() { candidates.extend([dir.join("tools").join("rtss-cli.exe"), dir.join("rtss-cli.exe")]); }
    }
    if let Some(local) = std::env::var_os("LOCALAPPDATA") { candidates.push(PathBuf::from(local).join("ConsoleMode").join("tools").join("rtss-cli.exe")); }
    candidates.into_iter().find(|p| p.is_file())
}

fn run_cli(cli: &Path, args: &[&str]) -> Result<String, String> {
    let mut child = Command::new(cli).args(args).stdin(Stdio::null()).stdout(Stdio::piped()).stderr(Stdio::null()).creation_flags(CREATE_NO_WINDOW)
        .spawn().map_err(|e| format!("rtss-cli: {e}"))?;
    let deadline = Instant::now() + Duration::from_secs(8);
    let status = loop {
        match child.try_wait().map_err(|e| e.to_string())? {
            Some(s) => break s,
            None if Instant::now() > deadline => { let _ = child.kill(); return Err(format!("rtss-cli {} não respondeu", args.join(" "))); }
            None => std::thread::sleep(Duration::from_millis(30)),
        }
    };
    let mut out = String::new();
    if let Some(mut so) = child.stdout.take() { let _ = so.read_to_string(&mut out); }
    if !status.success() { return Err(format!("rtss-cli {} saiu com {status}", args.join(" "))); }
    Ok(out.trim().to_string())
}

/// RTSS's global frame limit, through rtss-cli.
pub struct Rtss { cli: PathBuf }

impl Rtss {
    /// Why not, in Portuguese for the log, when RTSS or rtss-cli is missing.
    pub fn open(data_dir: &Path) -> Result<Rtss, String> {
        if !is_installed() { return Err("RTSS: RivaTuner Statistics Server não encontrado; limite de FPS ignorado".into()); }
        let cli = cli_path(data_dir).ok_or("RTSS: rtss-cli.exe não encontrado; limite de FPS ignorado")?;
        Ok(Rtss { cli })
    }

    /// The user's limit and limiter state right now.
    pub fn backup(&self) -> Result<RtssBackup, String> {
        if !ensure_running() { return Err("não foi possível iniciar o RTSS".into()); }
        let limit = run_cli(&self.cli, &["limit:get"])?.parse::<i32>().map_err(|e| format!("limite ilegível: {e}"))?;
        let limiter = run_cli(&self.cli, &["limiter:get"])?;
        Ok(RtssBackup { framerate_limit: limit, limiter_enabled: limiter == "1" || limiter.parse::<i32>().is_ok_and(|n| n != 0) })
    }

    /// 0 = no limit.
    pub fn set_limit(&self, fps: u32) -> Result<(), String> {
        if !ensure_running() { return Err("não foi possível iniciar o RTSS".into()); }
        if fps > 0 {
            run_cli(&self.cli, &["limit:set", &fps.to_string()])?;
            run_cli(&self.cli, &["limiter:set", "1"])?;
        } else {
            run_cli(&self.cli, &["limiter:set", "0"])?;
        }
        Ok(())
    }

    /// Puts the user's settings back; without a backup the limiter is simply turned off.
    pub fn restore(&self, backup: Option<&RtssBackup>) -> Result<(), String> {
        if !ensure_running() { return Err("RTSS fechado; nada a restaurar".into()); }
        match backup {
            Some(b) => {
                run_cli(&self.cli, &["limit:set", &b.framerate_limit.to_string()])?;
                run_cli(&self.cli, &["limiter:set", if b.limiter_enabled { "1" } else { "0" }])?;
            }
            None => { run_cli(&self.cli, &["limiter:set", "0"])?; }
        }
        Ok(())
    }
}

// ───────────── the FPS counter: RTSS shared memory ─────────────

// RTSSSharedMemoryV2 (RTSS SDK, RTSSSharedMemory.h): header offsets and OSD entry layout.
const SIGNATURE: u32 = 0x5254_5353; // 'RTSS'
const OFF_VERSION: usize = 4;
const OFF_OSD_ENTRY_SIZE: usize = 20;
const OFF_OSD_ARR_OFFSET: usize = 24;
const OFF_OSD_ARR_SIZE: usize = 28;
const OFF_OSD_FRAME: usize = 32;
const OFF_BUSY: usize = 36;
const ENTRY_OSD: usize = 0;
const ENTRY_OWNER: usize = 256;
const ENTRY_OSD_EX: usize = 512;
const OSD_SIZE: usize = 256;
const OWNER_SIZE: usize = 256;
const OSD_EX_SIZE: usize = 4096;
const ENTRY_OSD_EX2: usize = ENTRY_OSD_EX + OSD_EX_SIZE + 262_144; // after szOSDEx and the 256 KB object buffer
const AFTERBURNER_OWNER: &str = "MSIAfterburner";

/// The mapped shared memory; unmapped on drop.
struct Shm { handle: HANDLE, base: *mut u8 }

impl Shm {
    /// None when RTSS is not running (nothing on screen to show or clear).
    fn open() -> Option<Shm> {
        unsafe {
            let handle = OpenFileMappingW(FILE_MAP_ALL_ACCESS.0, false, w!("RTSSSharedMemoryV2")).ok()?;
            let view = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, 0);
            if view.Value.is_null() { let _ = CloseHandle(handle); return None; }
            Some(Shm { handle, base: view.Value as *mut u8 })
        }
    }
    fn u32_at(&self, off: usize) -> u32 { unsafe { (self.base.add(off) as *const u32).read_volatile() } }
    fn valid(&self) -> bool { self.u32_at(0) == SIGNATURE }
    fn version(&self) -> u32 { self.u32_at(OFF_VERSION) }
    fn entry_size(&self) -> usize { self.u32_at(OFF_OSD_ENTRY_SIZE) as usize }
    fn arr_size(&self) -> usize { self.u32_at(OFF_OSD_ARR_SIZE) as usize }
    fn entry(&self, i: usize) -> *mut u8 { unsafe { self.base.add(self.u32_at(OFF_OSD_ARR_OFFSET) as usize + i * self.entry_size()) } }
    fn atomic(&self, off: usize) -> &AtomicI32 { unsafe { AtomicI32::from_ptr(self.base.add(off) as *mut i32) } }
    fn bump_frame(&self) { self.atomic(OFF_OSD_FRAME).fetch_add(1, Ordering::SeqCst); }
    /// A header that doesn't look like RTSS's is not trusted (offsets come from it).
    fn sane(&self) -> bool { self.valid() && self.version() >= 0x0002_0000 && (OSD_SIZE..=1 << 20).contains(&self.entry_size()) && (1..=64).contains(&self.arr_size()) }
    fn is_owner(&self, entry: *mut u8, owner: &[u8]) -> bool {
        let field = unsafe { std::slice::from_raw_parts(entry.add(ENTRY_OWNER), owner.len() + 1) };
        &field[..owner.len()] == owner && field[owner.len()] == 0
    }
}

impl Drop for Shm {
    fn drop(&mut self) {
        unsafe {
            let _ = UnmapViewOfFile(MEMORY_MAPPED_VIEW_ADDRESS { Value: self.base as *mut c_void });
            let _ = CloseHandle(self.handle);
        }
    }
}

/// RTSS's font is Latin-1 (° is 0xB0).
fn latin1(text: &str) -> Vec<u8> { text.chars().map(|c| if (c as u32) < 256 { c as u8 } else { b'?' }).collect() }

/// `None` releases our slot. Mirrors UpdateOSD/ReleaseOSD from the RTSS SDK sample. False when RTSS
/// isn't there, has no free slot, or its header looks wrong.
fn write_slot(text: Option<&str>) -> bool {
    let Some(shm) = Shm::open() else { return false };
    if !shm.sane() { return false; }
    let owner = rtss_overlay::OWNER.as_bytes();
    let entry_size = shm.entry_size();

    let Some(text) = text else {
        // slot 0 belongs to the primary client (Afterburner); third parties start at 1
        for i in 1..shm.arr_size() {
            let e = shm.entry(i);
            if !shm.is_owner(e, owner) { continue; }
            unsafe { std::ptr::write_bytes(e, 0, entry_size); }
            shm.bump_frame();
        }
        return true;
    };

    // the slot we hold already, else the first free one
    let mut slot = (1..shm.arr_size()).map(|i| shm.entry(i)).find(|&e| shm.is_owner(e, owner));
    if slot.is_none() {
        for i in 1..shm.arr_size() {
            let e = shm.entry(i);
            if unsafe { *e.add(ENTRY_OWNER) } != 0 { continue; }
            unsafe {
                let field = std::slice::from_raw_parts_mut(e.add(ENTRY_OWNER), OWNER_SIZE);
                field.fill(0);
                field[..owner.len()].copy_from_slice(owner);
            }
            slot = Some(e);
            break;
        }
    }
    let Some(slot) = slot else { return false };

    let bytes = latin1(text);
    let extended = shm.version() >= 0x0002_0007 && entry_size >= ENTRY_OSD_EX + OSD_EX_SIZE;
    let (off, len) = if extended { (ENTRY_OSD_EX, OSD_EX_SIZE) } else { (ENTRY_OSD, OSD_SIZE) };
    // v2.14+: bit 0 of dwBusy locks the OSD while the renderer reads it
    let lockable = shm.version() >= 0x0002_000e;
    let busy = shm.atomic(OFF_BUSY);
    let mut spin = 0;
    while lockable && busy.fetch_or(1, Ordering::SeqCst) & 1 != 0 {
        spin += 1;
        if spin > 200 { return false; }
        std::thread::sleep(Duration::from_millis(1));
    }
    unsafe {
        let target = std::slice::from_raw_parts_mut(slot.add(off), len);
        target.fill(0);
        let n = bytes.len().min(len - 1);
        target[..n].copy_from_slice(&bytes[..n]);
    }
    if lockable { busy.fetch_and(!1, Ordering::SeqCst); }
    shm.bump_frame();
    true
}

/// RTSS draws every slot and has no way to hide one, and Afterburner's own OSD hotkey turns off the
/// whole OSD (ours too). So, while our counter is on, Afterburner's slot text is emptied right after
/// each rewrite (it does one per polling period, ~1 s): at worst it shows for a frame. Once we
/// stop, its next rewrite brings it back by itself.
fn hide_afterburner_loop(hide: Arc<AtomicBool>, alive: Arc<AtomicBool>) {
    unsafe { let _ = SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_ABOVE_NORMAL); }
    if let Some(shm) = Shm::open().filter(Shm::sane) {
        let owner = AFTERBURNER_OWNER.as_bytes();
        let slot = shm.entry(0); // Afterburner keeps slot 0
        let has_ex2 = shm.version() >= 0x0002_0014 && shm.entry_size() > ENTRY_OSD_EX2;
        let byte = |o: usize| unsafe { slot.add(o) };
        while hide.load(Ordering::Relaxed) && shm.valid() {
            let shown = unsafe { *byte(ENTRY_OSD) != 0 || *byte(ENTRY_OSD_EX) != 0 || (has_ex2 && *byte(ENTRY_OSD_EX2) != 0) };
            if shm.is_owner(slot, owner) && shown {
                unsafe {
                    *byte(ENTRY_OSD) = 0;
                    *byte(ENTRY_OSD_EX) = 0;
                    if has_ex2 { *byte(ENTRY_OSD_EX2) = 0; }
                }
                shm.bump_frame();
            }
            std::thread::sleep(Duration::from_millis(1));
        }
    }
    alive.store(false, Ordering::SeqCst);
}

/// CPU and memory for the styles that show them (1.7 reads GPU from the display driver as well;
/// 2.0 leaves GPU, clocks and temperature out for now, see docs/TESTING.md). Wants a call about once
/// a second: the usage is measured between two calls.
struct Sampler { last: Option<(u64, u64, u64)> }

fn ticks(f: FILETIME) -> u64 { (u64::from(f.dwHighDateTime) << 32) | u64::from(f.dwLowDateTime) }

impl Sampler {
    fn new() -> Self { let mut s = Sampler { last: None }; s.cpu(); s }

    fn times() -> Option<(u64, u64, u64)> {
        let (mut idle, mut kernel, mut user) = (FILETIME::default(), FILETIME::default(), FILETIME::default());
        unsafe { GetSystemTimes(Some(&mut idle), Some(&mut kernel), Some(&mut user)).ok()?; }
        Some((ticks(idle), ticks(kernel), ticks(user)))
    }

    /// Percent busy since the previous call; the kernel time includes the idle time.
    fn cpu(&mut self) -> Option<f64> {
        let now = Self::times()?;
        let before = self.last.replace(now)?;
        let (idle, total) = (now.0.saturating_sub(before.0), (now.1 + now.2).saturating_sub(before.1 + before.2));
        (total > 0).then(|| ((1.0 - idle as f64 / total as f64) * 100.0).clamp(0.0, 100.0))
    }

    fn sample(&mut self) -> HardwareSample {
        let mut mem = MEMORYSTATUSEX { dwLength: size_of::<MEMORYSTATUSEX>() as u32, ..Default::default() };
        let ram = unsafe { GlobalMemoryStatusEx(&mut mem) }.is_ok().then(|| (((mem.ullTotalPhys - mem.ullAvailPhys) as f64) / 1_048_576.0, mem.ullTotalPhys as f64 / 1_048_576.0));
        HardwareSample { cpu_usage: self.cpu(), ram_used_mb: ram.map(|r| r.0), ram_total_mb: ram.map(|r| r.1), ..Default::default() }
    }
}

/// Our FPS counter on the game. Cheap to clone; clones share the same counter.
#[derive(Clone, Default)]
pub struct Counter {
    /// Bumped by every apply/clear: a ticker whose generation is old stops.
    generation: Arc<AtomicU32>,
    hide_afterburner: Arc<AtomicBool>,
    hider_alive: Arc<AtomicBool>,
    slot_lock: Arc<Mutex<()>>,
}

impl Counter {
    pub fn new() -> Self { Self::default() }

    fn next_generation(&self) -> u32 {
        let _guard = self.slot_lock.lock();
        self.generation.fetch_add(1, Ordering::SeqCst) + 1
    }

    /// Shows a style (CPU/GPU/memory styles are rewritten every second until the next apply/clear).
    /// False when RTSS isn't there to draw it.
    pub fn apply(&self, style: &str, layout: &FpsOverlayLayout) -> bool {
        let generation = self.next_generation();
        let text = rtss_overlay::text(style, layout, None);
        if !self.show(&text, rtss_overlay::hides_others(style)) { return false; }
        if !text.is_empty() && rtss_overlay::needs_hardware(style, layout) {
            let (me, style, layout) = (self.clone(), style.to_string(), layout.clone());
            let _ = std::thread::Builder::new().name("rtss-counter".into()).spawn(move || me.tick(generation, &style, &layout));
        }
        true
    }

    /// Takes our counter off the screen and lets Afterburner's come back.
    pub fn clear(&self) {
        self.next_generation();
        self.show("", false);
    }

    fn tick(&self, generation: u32, style: &str, layout: &FpsOverlayLayout) {
        let mut sampler = Sampler::new();
        while self.generation.load(Ordering::SeqCst) == generation {
            std::thread::sleep(Duration::from_secs(1));
            let text = rtss_overlay::text(style, layout, Some(&sampler.sample()));
            // checked under the lock so a session that just ended can't get the counter back
            let _guard = self.slot_lock.lock();
            if self.generation.load(Ordering::SeqCst) != generation { return; }
            write_slot(Some(&text));
        }
    }

    fn show(&self, text: &str, hide_others: bool) -> bool {
        if text.is_empty() {
            write_slot(None);
            // off doesn't start RTSS just to hide something that isn't there
            if hide_others && is_running() { self.hide_afterburner(); } else { self.hide_afterburner.store(false, Ordering::SeqCst); }
            return true;
        }
        if !ensure_running() { return false; }
        // right after RTSS starts its shared memory may not be there yet
        for _ in 0..10 {
            if write_slot(Some(text)) {
                if hide_others { self.hide_afterburner(); } else { self.hide_afterburner.store(false, Ordering::SeqCst); }
                return true;
            }
            std::thread::sleep(Duration::from_millis(300));
        }
        false
    }

    fn hide_afterburner(&self) {
        self.hide_afterburner.store(true, Ordering::SeqCst);
        if self.hider_alive.swap(true, Ordering::SeqCst) { return; }
        let (hide, alive) = (self.hide_afterburner.clone(), self.hider_alive.clone());
        if std::thread::Builder::new().name("rtss-afterburner".into()).spawn({ let alive = alive.clone(); move || hide_afterburner_loop(hide, alive) }).is_err() {
            alive.store(false, Ordering::SeqCst);
        }
    }
}
