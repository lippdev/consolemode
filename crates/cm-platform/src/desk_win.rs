//! CCD desk operations. Construction and unit tests never change the desktop.
use crate::displays;
use cm_core::config::DisplayMode;
use std::{
    fs,
    io::Write,
    mem::size_of,
    path::PathBuf,
    thread,
    time::{Duration, Instant, SystemTime, UNIX_EPOCH},
};
use windows::{
    Win32::{
        Devices::Display::*,
        Foundation::{ERROR_INSUFFICIENT_BUFFER, LUID, RPC_E_CHANGED_MODE},
        Graphics::Gdi::*,
        System::Com::*,
    },
    core::{GUID, HRESULT, Interface, PCWSTR},
};

pub struct WinDesk {
    data_dir: PathBuf,
}

#[derive(Clone, Default)]
struct Config {
    paths: Vec<DISPLAYCONFIG_PATH_INFO>,
    modes: Vec<DISPLAYCONFIG_MODE_INFO>,
}
type Key = (i32, u32, u32);
fn key(adapter: LUID, id: u32) -> Key {
    (adapter.HighPart, adapter.LowPart, id)
}
fn target(p: &DISPLAYCONFIG_PATH_INFO) -> Key {
    key(p.targetInfo.adapterId, p.targetInfo.id)
}
fn source(p: &DISPLAYCONFIG_PATH_INFO) -> Key {
    key(p.sourceInfo.adapterId, p.sourceInfo.id)
}
fn active(p: &DISPLAYCONFIG_PATH_INFO) -> bool {
    p.flags & DISPLAYCONFIG_PATH_ACTIVE != 0
}
fn err(context: &str, code: i32) -> Result<(), String> {
    if code == 0 {
        Ok(())
    } else {
        Err(format!("{context}: Windows error {code}"))
    }
}
fn text(s: &[u16]) -> String {
    String::from_utf16_lossy(&s[..s.iter().position(|&v| v == 0).unwrap_or(s.len())])
}
fn wide(s: &str) -> Vec<u16> {
    s.encode_utf16().chain(Some(0)).collect()
}
fn header(
    kind: DISPLAYCONFIG_DEVICE_INFO_TYPE,
    size: usize,
    adapter: LUID,
    id: u32,
) -> DISPLAYCONFIG_DEVICE_INFO_HEADER {
    DISPLAYCONFIG_DEVICE_INFO_HEADER {
        r#type: kind,
        size: size as u32,
        adapterId: adapter,
        id,
    }
}
fn query(flags: QUERY_DISPLAY_CONFIG_FLAGS) -> Result<Config, String> {
    for _ in 0..4 {
        unsafe {
            let (mut np, mut nm) = (0, 0);
            GetDisplayConfigBufferSizes(flags, &mut np, &mut nm)
                .ok()
                .map_err(|e| format!("CCD buffer sizes: {e}"))?;
            let mut c = Config {
                paths: vec![Default::default(); np as usize],
                modes: vec![Default::default(); nm as usize],
            };
            let status = QueryDisplayConfig(
                flags,
                &mut np,
                c.paths.as_mut_ptr(),
                &mut nm,
                c.modes.as_mut_ptr(),
                None,
            );
            if status == ERROR_INSUFFICIENT_BUFFER {
                continue;
            }
            status
                .ok()
                .map_err(|e| format!("QueryDisplayConfig: {e}"))?;
            c.paths.truncate(np as usize);
            c.modes.truncate(nm as usize);
            return Ok(c);
        }
    }
    Err("Display configuration kept changing during query".into())
}
fn apply(c: &Config, allow_changes: bool) -> Result<(), String> {
    validate(c)?;
    let mut flags = SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE;
    if allow_changes {
        flags |= SDC_ALLOW_CHANGES;
    }
    unsafe {
        err(
            "SetDisplayConfig",
            SetDisplayConfig(Some(&c.paths), Some(&c.modes), flags),
        )
    }
}
fn source_mode<'a>(
    c: &'a Config,
    p: &DISPLAYCONFIG_PATH_INFO,
) -> Option<&'a DISPLAYCONFIG_SOURCE_MODE> {
    unsafe {
        let m = c.modes.get(p.sourceInfo.Anonymous.modeInfoIdx as usize)?;
        (m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE && key(m.adapterId, m.id) == source(p))
            .then(|| &m.Anonymous.sourceMode)
    }
}
fn validate(c: &Config) -> Result<(), String> {
    if !c.paths.iter().any(active) {
        return Err("Refusing a configuration with no active screen".into());
    }
    for p in &c.paths {
        unsafe {
            for (idx, kind, expected) in [
                (
                    p.sourceInfo.Anonymous.modeInfoIdx,
                    DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE,
                    source(p),
                ),
                (
                    p.targetInfo.Anonymous.modeInfoIdx,
                    DISPLAYCONFIG_MODE_INFO_TYPE_TARGET,
                    target(p),
                ),
            ] {
                if idx == DISPLAYCONFIG_PATH_MODE_IDX_INVALID {
                    continue;
                }
                let m = c
                    .modes
                    .get(idx as usize)
                    .ok_or("Backup contains an invalid mode index")?;
                if m.infoType != kind || key(m.adapterId, m.id) != expected {
                    return Err("Backup mode does not belong to its CCD path".into());
                }
            }
        }
    }
    Ok(())
}

// The format is versioned, little endian, and never reads native padding or unused union bytes.
fn encode(c: &Config) -> Result<Vec<u8>, String> {
    validate(c)?;
    let mut words = vec![0x31444d43, c.paths.len() as u32, c.modes.len() as u32];
    for p in &c.paths {
        unsafe {
            let (s, t) = (p.sourceInfo, p.targetInfo);
            words.extend([
                s.adapterId.LowPart,
                s.adapterId.HighPart as u32,
                s.id,
                s.Anonymous.modeInfoIdx,
                s.statusFlags,
                t.adapterId.LowPart,
                t.adapterId.HighPart as u32,
                t.id,
                t.Anonymous.modeInfoIdx,
                t.outputTechnology.0 as u32,
                t.rotation.0 as u32,
                t.scaling.0 as u32,
                t.refreshRate.Numerator,
                t.refreshRate.Denominator,
                t.scanLineOrdering.0 as u32,
                t.targetAvailable.0 as u32,
                t.statusFlags,
                p.flags,
            ]);
        }
    }
    for m in &c.modes {
        unsafe {
            words.extend([
                m.infoType.0 as u32,
                m.id,
                m.adapterId.LowPart,
                m.adapterId.HighPart as u32,
            ]);
            let mut v = [0; 12];
            if m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE {
                let s = m.Anonymous.sourceMode;
                v[..5].copy_from_slice(&[
                    s.width,
                    s.height,
                    s.pixelFormat.0 as u32,
                    s.position.x as u32,
                    s.position.y as u32,
                ]);
            } else if m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE_TARGET {
                let t = m.Anonymous.targetMode.targetVideoSignalInfo;
                v = [
                    t.pixelRate as u32,
                    (t.pixelRate >> 32) as u32,
                    t.hSyncFreq.Numerator,
                    t.hSyncFreq.Denominator,
                    t.vSyncFreq.Numerator,
                    t.vSyncFreq.Denominator,
                    t.activeSize.cx,
                    t.activeSize.cy,
                    t.totalSize.cx,
                    t.totalSize.cy,
                    t.Anonymous.videoStandard,
                    t.scanLineOrdering.0 as u32,
                ];
            } else if m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE_DESKTOP_IMAGE {
                let d = m.Anonymous.desktopImageInfo;
                v[..10].copy_from_slice(&[
                    d.PathSourceSize.x as u32,
                    d.PathSourceSize.y as u32,
                    d.DesktopImageRegion.left as u32,
                    d.DesktopImageRegion.top as u32,
                    d.DesktopImageRegion.right as u32,
                    d.DesktopImageRegion.bottom as u32,
                    d.DesktopImageClip.left as u32,
                    d.DesktopImageClip.top as u32,
                    d.DesktopImageClip.right as u32,
                    d.DesktopImageClip.bottom as u32,
                ]);
            } else {
                return Err("Unknown CCD mode type".into());
            }
            words.extend(v);
        }
    }
    Ok(words.into_iter().flat_map(u32::to_le_bytes).collect())
}
fn decode(bytes: &[u8]) -> Result<Config, String> {
    if bytes.len() < 12 || bytes.len() % 4 != 0 {
        return Err("Truncated CCD backup".into());
    }
    let words: Vec<u32> = bytes
        .chunks_exact(4)
        .map(|b| u32::from_le_bytes(b.try_into().unwrap()))
        .collect();
    let (np, nm) = (words[1] as usize, words[2] as usize);
    if words[0] != 0x31444d43
        || np == 0
        || np > 4096
        || nm > 16384
        || words.len() != 3 + np * 18 + nm * 16
    {
        return Err("Invalid CCD backup version or array lengths".into());
    }
    let mut c = Config::default();
    // These SDK structs contain only integer fields and integer newtypes; every bit pattern is valid.
    assert_eq!(size_of::<DISPLAYCONFIG_PATH_INFO>(), 72);
    assert_eq!(size_of::<DISPLAYCONFIG_MODE_INFO>(), 64);
    for w in words[3..3 + np * 18].chunks_exact(18) {
        let mut p = DISPLAYCONFIG_PATH_INFO::default();
        unsafe {
            std::ptr::copy_nonoverlapping(
                w.as_ptr().cast::<u8>(),
                (&mut p as *mut DISPLAYCONFIG_PATH_INFO).cast(),
                72,
            );
        }
        c.paths.push(p);
    }
    for w in words[3 + np * 18..].chunks_exact(16) {
        if !(1..=3).contains(&w[0]) {
            return Err("Unknown CCD backup mode type".into());
        }
        let mut m = DISPLAYCONFIG_MODE_INFO::default();
        unsafe {
            std::ptr::copy_nonoverlapping(
                w.as_ptr().cast::<u8>(),
                (&mut m as *mut DISPLAYCONFIG_MODE_INFO).cast(),
                64,
            );
        }
        c.modes.push(m);
    }
    validate(&c)?;
    Ok(c)
}
fn match_target(id: &str, candidates: &[(String, Key)]) -> Result<Key, String> {
    let mut matches = candidates
        .iter()
        .filter(|(s, _)| !id.is_empty() && s.eq_ignore_ascii_case(id))
        .map(|(_, k)| *k);
    let first = matches
        .next()
        .ok_or_else(|| format!("Display not connected: {id}"))?;
    if matches.any(|k| k != first) {
        return Err(format!("Ambiguous display identity: {id}"));
    }
    Ok(first)
}
fn deactivate(c: &Config, targets: &[Key]) -> Result<Config, String> {
    let mut next = c.clone();
    for p in &mut next.paths {
        if targets.contains(&target(p)) {
            p.flags &= !DISPLAYCONFIG_PATH_ACTIVE;
        }
    }
    validate(&next)?;
    Ok(next)
}
fn closest(modes: &[displays::DisplayMode], wanted: &DisplayMode) -> Option<displays::DisplayMode> {
    modes
        .iter()
        .filter(|m| m.width > 0 && m.height > 0)
        .min_by_key(|m| {
            (
                u64::from(m.width.abs_diff(wanted.width))
                    + u64::from(m.height.abs_diff(wanted.height)),
                if wanted.frequency == 0 {
                    0
                } else {
                    m.refresh_hz.abs_diff(wanted.frequency)
                },
                std::cmp::Reverse(m.refresh_hz),
                m.width,
                m.height,
            )
        })
        .copied()
}
fn identities(c: &Config) -> Result<Vec<(String, Key)>, String> {
    let mut result = Vec::new();
    for p in &c.paths {
        unsafe {
            let t = p.targetInfo;
            let mut name = DISPLAYCONFIG_TARGET_DEVICE_NAME {
                header: header(
                    DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                    size_of::<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                    t.adapterId,
                    t.id,
                ),
                ..Default::default()
            };
            if DisplayConfigGetDeviceInfo(&mut name.header) != 0 {
                continue;
            }
            let path = text(&name.monitorDevicePath);
            if let Some((hw, instance)) = displays::parse_device_path(&path) {
                let (driver, _, serial) = displays::registry_identity(&instance);
                let mut s = DISPLAYCONFIG_SOURCE_DEVICE_NAME {
                    header: header(
                        DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size_of::<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                        p.sourceInfo.adapterId,
                        p.sourceInfo.id,
                    ),
                    ..Default::default()
                };
                let _ = DisplayConfigGetDeviceInfo(&mut s.header);
                result.push((
                    displays::stable_id(&hw, &driver, &serial, &text(&s.viewGdiDeviceName)),
                    target(p),
                ));
            }
        }
    }
    Ok(result)
}
fn resolve(c: &Config, id: &str) -> Result<Key, String> {
    match_target(id, &identities(c)?)
}
fn wait(mut check: impl FnMut() -> Result<bool, String>, description: &str) -> Result<(), String> {
    let until = Instant::now() + Duration::from_secs(6);
    loop {
        if check()? {
            return Ok(());
        }
        if Instant::now() >= until {
            return Err(format!("Timed out waiting for {description}"));
        }
        thread::sleep(Duration::from_millis(200));
    }
}
fn make_primary(c: &mut Config, t: Key) -> Result<(), String> {
    let p = c
        .paths
        .iter()
        .find(|p| target(p) == t)
        .ok_or("Focus display disappeared")?;
    let s = source_mode(c, p).ok_or("Focus source mode unavailable")?;
    let (dx, dy) = (s.position.x, s.position.y);
    for m in &mut c.modes {
        if m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE {
            unsafe {
                let s = &mut m.Anonymous.sourceMode;
                s.position.x = s
                    .position
                    .x
                    .checked_sub(dx)
                    .ok_or("Desktop position overflow")?;
                s.position.y = s
                    .position
                    .y
                    .checked_sub(dy)
                    .ok_or("Desktop position overflow")?;
            }
        }
    }
    Ok(())
}

fn same_layout(saved: &Config, now: &Config) -> bool {
    let a: Vec<_> = saved.paths.iter().filter(|p| active(p)).collect();
    let b: Vec<_> = now.paths.iter().filter(|p| active(p)).collect();
    a.len() == b.len()
        && a.iter().all(|p| {
            b.iter().any(|q| {
                target(p) == target(q)
                    && source(p) == source(q)
                    && p.targetInfo.rotation == q.targetInfo.rotation
                    && p.targetInfo.scaling == q.targetInfo.scaling
                    && u64::from(p.targetInfo.refreshRate.Numerator)
                        * u64::from(q.targetInfo.refreshRate.Denominator)
                        == u64::from(q.targetInfo.refreshRate.Numerator)
                            * u64::from(p.targetInfo.refreshRate.Denominator)
                    && source_mode(saved, p)
                        .zip(source_mode(now, q))
                        .is_some_and(|(s, t)| s == t)
            })
        })
}

impl WinDesk {
    pub fn new(data_dir: PathBuf) -> Self {
        Self { data_dir }
    }
    pub fn backup(&mut self) -> Result<String, String> {
        let bytes = encode(&query(QDC_ONLY_ACTIVE_PATHS)?)?;
        fs::create_dir_all(&self.data_dir)
            .map_err(|e| format!("Create desk backup directory: {e}"))?;
        let stamp = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .map_err(|e| e.to_string())?
            .as_nanos();
        let path = fs::canonicalize(&self.data_dir)
            .map_err(|e| format!("Resolve backup directory: {e}"))?
            .join(format!("desk-{}-{stamp}.ccd", std::process::id()));
        let path = path
            .to_str()
            .ok_or("Backup path is not valid UTF-8")?
            .to_owned();
        let mut file = fs::OpenOptions::new()
            .write(true)
            .create_new(true)
            .open(&path)
            .map_err(|e| format!("Create desk backup: {e}"))?;
        file.write_all(&bytes)
            .and_then(|_| file.sync_all())
            .map_err(|e| format!("Persist desk backup: {e}"))?;
        Ok(path)
    }
    pub fn restore(&mut self, backup: &str) -> Result<(), String> {
        let c = decode(&fs::read(backup).map_err(|e| format!("Read desk backup: {e}"))?)?;
        apply(&c, false)?;
        wait(
            || Ok(same_layout(&c, &query(QDC_ONLY_ACTIVE_PATHS)?)),
            "saved display layout, modes and primary origin",
        )
    }
    pub fn focus(&mut self, id: &str) -> Result<(), String> {
        let c = query(QDC_ALL_PATHS)?;
        let t = resolve(&c, id)?;
        if !c.paths.iter().any(|p| active(p) && target(p) == t) {
            let mut next = Config {
                paths: c.paths.iter().filter(|p| active(p)).copied().collect(),
                modes: c.modes.clone(),
            };
            let mut p = *c
                .paths
                .iter()
                .find(|p| {
                    target(p) == t
                        && p.targetInfo.targetAvailable.as_bool()
                        && !next.paths.iter().any(|q| source(q) == source(p))
                })
                .ok_or("No available independent CCD source for display")?;
            p.flags |= DISPLAYCONFIG_PATH_ACTIVE;
            p.sourceInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            p.targetInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            next.paths.push(p);
            apply(&next, true)?;
        }
        wait(
            || {
                Ok(query(QDC_ONLY_ACTIVE_PATHS)?
                    .paths
                    .iter()
                    .any(|p| target(p) == t))
            },
            "focus display to become active",
        )?;
        let mut c = query(QDC_ONLY_ACTIVE_PATHS)?;
        make_primary(&mut c, t)?;
        apply(&c, false)?;
        wait(
            || {
                Ok(displays::list_displays()
                    .map_err(|e| e.to_string())?
                    .iter()
                    .any(|d| d.stable_id.eq_ignore_ascii_case(id) && d.active && d.primary))
            },
            "focus display primary flag",
        )
    }
    pub fn hide(&mut self, ids: &[String]) -> Result<(), String> {
        if ids.is_empty() {
            return Ok(());
        }
        let c = query(QDC_ALL_PATHS)?;
        let targets: Vec<_> = ids
            .iter()
            .map(|id| resolve(&c, id))
            .collect::<Result<_, _>>()?;
        let c = query(QDC_ONLY_ACTIVE_PATHS)?;
        apply(&deactivate(&c, &targets)?, true)?;
        wait(
            || {
                Ok(query(QDC_ONLY_ACTIVE_PATHS)?
                    .paths
                    .iter()
                    .all(|p| !targets.contains(&target(p))))
            },
            "hidden displays to disconnect",
        )
    }
    pub fn set_mode(&mut self, id: &str, mode: &DisplayMode) -> Result<(), String> {
        let d = displays::list_displays()
            .map_err(|e| e.to_string())?
            .into_iter()
            .find(|d| d.stable_id.eq_ignore_ascii_case(id) && d.active)
            .ok_or("Mode target is not active")?;
        let as_mode = |m: displays::DisplayMode| DisplayMode { width: m.width, height: m.height, frequency: m.refresh_hz };
        // already there (a TV at @59 for @60 included): changing it again only blanks the screen
        if d.current_mode.is_some_and(|c| mode.is_met_by(&as_mode(c))) {
            return Ok(());
        }
        let chosen = closest(&d.supported_modes, mode).ok_or("No supported modes for display")?;
        let target = as_mode(chosen);
        let name = wide(&d.gdi_name);
        unsafe {
            let mut dm = DEVMODEW {
                dmSize: size_of::<DEVMODEW>() as u16,
                ..Default::default()
            };
            if !EnumDisplaySettingsW(PCWSTR(name.as_ptr()), ENUM_CURRENT_SETTINGS, &mut dm)
                .as_bool()
            {
                return Err("Cannot query current display mode".into());
            }
            dm.dmPelsWidth = chosen.width;
            dm.dmPelsHeight = chosen.height;
            dm.dmDisplayFrequency = chosen.refresh_hz;
            dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY;
            let status =
                ChangeDisplaySettingsExW(PCWSTR(name.as_ptr()), Some(&dm), None, CDS_TYPE(0), None);
            if status != DISP_CHANGE_SUCCESSFUL {
                return Err(format!("ChangeDisplaySettingsExW: {}", status.0));
            }
        }
        wait(
            || {
                Ok(displays::list_displays()
                    .map_err(|e| e.to_string())?
                    .iter()
                    .any(|d| {
                        d.stable_id.eq_ignore_ascii_case(id)
                            && d.current_mode.is_some_and(|c| target.is_met_by(&as_mode(c)))
                    }))
            },
            "selected display mode",
        )
    }
    pub fn hdr_on(&mut self, id: &str) -> Result<bool, String> {
        let c = query(QDC_ONLY_ACTIVE_PATHS)?;
        let t = resolve(&c, id)?;
        let before = color(t)?;
        if before & 2 != 0 {
            return Ok(false);
        }
        if before & 1 == 0 || before & 8 != 0 {
            return Err("HDR is unsupported or disabled by the display driver".into());
        }
        set_color(t, true)?;
        if let Err(e) = wait(|| Ok(color(t)? & 2 != 0), "HDR enabled") {
            return match set_color(t, false) {
                Ok(()) => Err(e),
                Err(undo) => Err(format!("{e}; HDR rollback failed: {undo}")),
            };
        }
        Ok(true)
    }
    pub fn hdr_off(&mut self, id: &str) {
        let _ = (|| -> Result<(), String> {
            let c = query(QDC_ONLY_ACTIVE_PATHS)?;
            set_color(resolve(&c, id)?, false)
        })();
    }
    pub fn audio_default(&mut self) -> Option<String> {
        crate::audio::list_outputs()
            .ok()?
            .into_iter()
            .find(|d| d.is_default)
            .map(|d| d.endpoint_id)
    }
    pub fn set_audio(&mut self, id: &str) -> Result<(), String> {
        set_audio(id)
    }
    pub fn screen_rect(&mut self, id: &str) -> Option<(i32, i32, i32, i32)> {
        let c = query(QDC_ONLY_ACTIVE_PATHS).ok()?;
        let t = resolve(&c, id).ok()?;
        let p = c.paths.iter().find(|p| target(p) == t)?;
        let s = source_mode(&c, p)?;
        Some((
            s.position.x,
            s.position.y,
            s.position.x.checked_add(i32::try_from(s.width).ok()?)?,
            s.position.y.checked_add(i32::try_from(s.height).ok()?)?,
        ))
    }
    /// The mode a screen is running now (read only), for the per-game profiles' "undo".
    pub fn current_mode(&mut self, id: &str) -> Option<DisplayMode> {
        let d = displays::list_displays().ok()?.into_iter().find(|d| d.stable_id.eq_ignore_ascii_case(id) && d.active)?;
        d.current_mode.map(|m| DisplayMode { width: m.width, height: m.height, frequency: m.refresh_hz })
    }
    /// Whether HDR is on right now (read only); None when Windows can't tell.
    pub fn hdr_state(&mut self, id: &str) -> Option<bool> {
        displays::list_displays().ok()?.into_iter().find(|d| d.stable_id.eq_ignore_ascii_case(id) && d.active)?.hdr_enabled
    }
    pub fn simulated(&self) -> bool {
        false
    }
}
fn color(t: Key) -> Result<u32, String> {
    unsafe {
        let mut c = DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO {
            header: header(
                DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO,
                size_of::<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(),
                LUID {
                    HighPart: t.0,
                    LowPart: t.1,
                },
                t.2,
            ),
            ..Default::default()
        };
        err("Query HDR", DisplayConfigGetDeviceInfo(&mut c.header))?;
        Ok(c.Anonymous.value)
    }
}
fn set_color(t: Key, on: bool) -> Result<(), String> {
    unsafe {
        let mut c = DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE {
            header: header(
                DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE,
                size_of::<DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>(),
                LUID {
                    HighPart: t.0,
                    LowPart: t.1,
                },
                t.2,
            ),
            ..Default::default()
        };
        c.Anonymous.value = u32::from(on);
        err("Set HDR", DisplayConfigSetDeviceInfo(&c.header))
    }
}

// Preserve the undocumented 1.7 ABI, including all ten methods before SetDefaultEndpoint.
#[repr(transparent)]
#[derive(Clone)]
struct PolicyConfig(windows::core::IUnknown);
unsafe impl Interface for PolicyConfig {
    type Vtable = PolicyVtable;
    const IID: GUID = GUID::from_u128(0xf8679f50_850a_41cf_9c72_430f290290c8);
}
#[repr(C)]
struct PolicyVtable {
    base: windows::core::IUnknown_Vtbl,
    unused: [unsafe extern "system" fn(); 10],
    set_default: unsafe extern "system" fn(*mut std::ffi::c_void, PCWSTR, i32) -> HRESULT,
    set_visible: unsafe extern "system" fn(*mut std::ffi::c_void, PCWSTR, i32) -> HRESULT,
}
struct Apartment(bool);
impl Drop for Apartment {
    fn drop(&mut self) {
        if self.0 {
            unsafe {
                CoUninitialize();
            }
        }
    }
}
fn set_audio(id: &str) -> Result<(), String> {
    let outputs = crate::audio::list_outputs().map_err(|e| format!("Enumerate audio: {e}"))?;
    let endpoint = outputs
        .iter()
        .find(|d| d.endpoint_id.eq_ignore_ascii_case(id))
        .or_else(|| {
            outputs
                .iter()
                .filter(|d| d.friendly_id.eq_ignore_ascii_case(id))
                .max_by_key(|d| (d.is_active, d.is_default))
        })
        .ok_or_else(|| format!("Audio endpoint not found: {id}"))?;
    let name = wide(&endpoint.endpoint_id);
    unsafe {
        let init = CoInitializeEx(None, COINIT_MULTITHREADED);
        if init != RPC_E_CHANGED_MODE {
            init.ok()
                .map_err(|e| format!("Initialize audio COM: {e}"))?;
        }
        let _apartment = Apartment(init.is_ok());
        let policy: PolicyConfig = CoCreateInstance(
            &GUID::from_u128(0x870af99c_171d_4f9e_af0d_e63df40c2bc9),
            None,
            CLSCTX_ALL,
        )
        .map_err(|e| format!("Create IPolicyConfig: {e}"))?;
        let table = policy.vtable();
        if !endpoint.is_active {
            (table.set_visible)(policy.as_raw(), PCWSTR(name.as_ptr()), 1)
                .ok()
                .map_err(|e| format!("Enable audio endpoint: {e}"))?;
        }
        for role in 0..3 {
            (table.set_default)(policy.as_raw(), PCWSTR(name.as_ptr()), role)
                .ok()
                .map_err(|e| format!("Set default audio role {role}: {e}"))?;
        }
    }
    wait(
        || {
            Ok(crate::audio::list_outputs()
                .map_err(|e| e.to_string())?
                .iter()
                .any(|d| d.is_default && d.endpoint_id == endpoint.endpoint_id))
        },
        "default audio endpoint",
    )
}

#[cfg(test)]
mod tests {
    use super::*;
    fn fixture() -> Config {
        let mut c = Config::default();
        for i in 0..2 {
            let mut p = DISPLAYCONFIG_PATH_INFO::default();
            p.flags = DISPLAYCONFIG_PATH_ACTIVE;
            p.sourceInfo.id = i;
            p.targetInfo.id = i;
            p.sourceInfo.Anonymous.modeInfoIdx = i;
            p.targetInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            p.targetInfo.refreshRate = DISPLAYCONFIG_RATIONAL {
                Numerator: 60000,
                Denominator: 1000,
            };
            let mut m = DISPLAYCONFIG_MODE_INFO {
                infoType: DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE,
                id: i,
                ..Default::default()
            };
            m.Anonymous.sourceMode = DISPLAYCONFIG_SOURCE_MODE {
                width: 1920,
                height: 1080,
                position: windows::Win32::Foundation::POINTL {
                    x: (i as i32 - 1) * 1920,
                    y: 0,
                },
                pixelFormat: DISPLAYCONFIG_PIXELFORMAT_32BPP,
            };
            c.paths.push(p);
            c.modes.push(m);
        }
        c
    }
    #[test]
    fn backup_roundtrip_including_target_and_desktop_union() {
        let mut c = fixture();
        let mut m = DISPLAYCONFIG_MODE_INFO {
            infoType: DISPLAYCONFIG_MODE_INFO_TYPE_TARGET,
            ..Default::default()
        };
        m.Anonymous.targetMode.targetVideoSignalInfo.pixelRate = 123456789012;
        m.Anonymous
            .targetMode
            .targetVideoSignalInfo
            .Anonymous
            .videoStandard = 0xaabbccdd;
        c.modes.push(m);
        let mut m = DISPLAYCONFIG_MODE_INFO {
            infoType: DISPLAYCONFIG_MODE_INFO_TYPE_DESKTOP_IMAGE,
            ..Default::default()
        };
        m.Anonymous.desktopImageInfo.DesktopImageClip.left = -400;
        c.modes.push(m);
        let bytes = encode(&c).unwrap();
        let parsed = decode(&bytes).unwrap();
        assert_eq!(encode(&parsed).unwrap(), bytes);
        assert!(same_layout(&c, &parsed));
        assert_eq!(
            source_mode(&parsed, &parsed.paths[0]).unwrap().position.x,
            -1920
        );
    }
    #[test]
    fn backup_rejects_corrupt_lengths_versions_indices_and_types() {
        let bytes = encode(&fixture()).unwrap();
        for length in 0..bytes.len() {
            assert!(decode(&bytes[..length]).is_err());
        }
        for (offset, value) in [
            (0, 0),
            (4, u32::MAX),
            (8, u32::MAX),
            (24, 99),
            (3 * 4 + 2 * 72, 99),
        ] {
            let mut bad = bytes.clone();
            bad[offset..offset + 4].copy_from_slice(&value.to_le_bytes());
            assert!(decode(&bad).is_err(), "offset {offset}");
        }
        let mut c = fixture();
        c.modes[0].id = 88;
        assert!(encode(&c).is_err());
        let mut extra = bytes;
        extra.extend([0; 4]);
        assert!(decode(&extra).is_err());
    }
    #[test]
    fn stable_ids_match_exactly_case_insensitively_and_reject_ambiguity() {
        let ids = vec![
            (r"MONITOR\GSM0001\{guid}\0002".into(), (1, 2, 3)),
            (r"MONITOR\GSM0001\{guid}\0003".into(), (1, 2, 4)),
        ];
        assert_eq!(
            match_target(r"monitor\gsm0001\{GUID}\0002", &ids).unwrap(),
            (1, 2, 3)
        );
        assert!(match_target("GSM0001", &ids).is_err());
        assert!(match_target("", &ids).is_err());
        let mut ambiguous = ids.clone();
        ambiguous.push((ids[0].0.clone(), (5, 2, 3)));
        assert!(match_target(&ids[0].0, &ambiguous).is_err());
    }
    #[test]
    fn disconnect_never_removes_last_active_screen_or_another_adapters_target() {
        let c = fixture();
        assert!(deactivate(&c, &[(0, 0, 0), (0, 0, 1)]).is_err());
        let next = deactivate(&c, &[(0, 0, 0)]).unwrap();
        assert!(!active(&next.paths[0]));
        assert!(active(&next.paths[1]));
        assert!(active(&c.paths[0]));
        assert!(
            deactivate(&c, &[(1, 0, 0)])
                .unwrap()
                .paths
                .iter()
                .all(active)
        );
    }
    #[test]
    fn mode_selection_prefers_resolution_then_refresh_and_handles_extremes() {
        let modes = [
            displays::DisplayMode {
                width: 1920,
                height: 1080,
                refresh_hz: 60,
            },
            displays::DisplayMode {
                width: 1920,
                height: 1080,
                refresh_hz: 120,
            },
            displays::DisplayMode {
                width: 3840,
                height: 2160,
                refresh_hz: 144,
            },
        ];
        let mut request = DisplayMode {
            width: 1920,
            height: 1080,
            frequency: 119,
        };
        assert_eq!(closest(&modes, &request), Some(modes[1]));
        request.frequency = 144;
        assert_eq!(closest(&modes, &request), Some(modes[1]));
        request.frequency = 0;
        assert_eq!(closest(&modes, &request), Some(modes[1]));
        assert_eq!(closest(&[], &request), None);
        request.width = u32::MAX;
        request.height = u32::MAX;
        assert_eq!(closest(&modes, &request), Some(modes[2]));
    }
    #[test]
    fn primary_translation_preserves_relative_positions_and_rejects_overflow() {
        let mut c = fixture();
        make_primary(&mut c, (0, 0, 0)).unwrap();
        assert_eq!(source_mode(&c, &c.paths[0]).unwrap().position.x, 0);
        assert_eq!(source_mode(&c, &c.paths[1]).unwrap().position.x, 1920);
        c.modes[0].Anonymous.sourceMode.position.x = i32::MIN;
        assert!(make_primary(&mut c, (0, 0, 0)).is_err());
    }
    #[test]
    fn restore_verification_detects_missing_extra_moved_rotated_and_changed_modes() {
        let c = fixture();
        let mut now = c.clone();
        now.paths.reverse();
        assert!(same_layout(&c, &now));
        now.paths.pop();
        assert!(!same_layout(&c, &now));
        let mut now = c.clone();
        now.modes[0].Anonymous.sourceMode.position.x = 20;
        assert!(!same_layout(&c, &now));
        let mut now = c.clone();
        now.modes[0].Anonymous.sourceMode.width = 1280;
        assert!(!same_layout(&c, &now));
        let mut now = c.clone();
        now.paths[0].targetInfo.rotation = DISPLAYCONFIG_ROTATION_ROTATE90;
        assert!(!same_layout(&c, &now));
        let mut now = c.clone();
        now.paths[0].targetInfo.refreshRate.Numerator = 120000;
        assert!(!same_layout(&c, &now));
    }
}
