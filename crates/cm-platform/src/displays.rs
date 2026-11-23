use std::{collections::HashSet, mem::size_of};
use windows::{
    Win32::{
        Devices::Display::*,
        Foundation::{ERROR_INSUFFICIENT_BUFFER, LPARAM, LUID, RECT},
        Graphics::Gdi::*,
    },
    core::{BOOL, PCWSTR, Result},
};
use winreg::{
    RegKey,
    enums::{HKEY_LOCAL_MACHINE, KEY_READ},
};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct DisplayMode {
    pub width: u32,
    pub height: u32,
    pub refresh_hz: u32,
}

#[derive(Debug, Clone)]
pub struct Display {
    pub stable_id: String,
    /// Empty for remembered devices with no current GDI source.
    pub gdi_name: String,
    pub friendly_name: String,
    pub active: bool,
    pub primary: bool,
    pub position: Option<(i32, i32)>,
    pub current_mode: Option<DisplayMode>,
    pub supported_modes: Vec<DisplayMode>,
    /// None means Windows could not query advanced color for this target.
    pub hdr_supported: Option<bool>,
    pub hdr_enabled: Option<bool>,
}

fn text(s: &[u16]) -> String {
    String::from_utf16_lossy(&s[..s.iter().position(|&c| c == 0).unwrap_or(s.len())])
}
pub(crate) fn parse_device_path(path: &str) -> Option<(String, String)> {
    let mut parts = path.strip_prefix(r"\\?\").unwrap_or(path).split('#');
    let (kind, hw, instance) = (parts.next()?, parts.next()?, parts.next()?);
    if kind.is_empty() || hw.is_empty() || instance.is_empty() {
        return None;
    }
    Some((hw.into(), format!(r"{kind}\{hw}\{instance}")))
}
pub(crate) fn stable_id(hw: &str, driver: &str, serial: &str, gdi: &str) -> String {
    if !hw.trim().is_empty() && !driver.trim().is_empty() {
        format!(r"MONITOR\{hw}\{driver}")
    } else if !hw.trim().is_empty() {
        format!("{hw}#{serial}")
    } else {
        gdi.into()
    }
}
fn friendly_name(name: &str, hw: &str, gdi: &str) -> String {
    if !name.trim().is_empty() {
        return name.into();
    }
    if let Some(code) = hw.get(..3) {
        let vendor = match code.to_ascii_uppercase().as_str() {
            "ACR" => "Acer",
            "AOC" => "AOC",
            "AUS" => "ASUS",
            "BNQ" => "BenQ",
            "DEL" => "Dell",
            "GSM" => "LG",
            "HWP" => "HP",
            "LEN" => "Lenovo",
            "MSI" => "MSI",
            "PHL" | "PHI" => "Philips",
            "SAM" => "Samsung",
            "SNY" => "Sony",
            "TCL" => "TCL",
            "VSC" => "ViewSonic",
            "GBT" => "Gigabyte",
            "HSD" => "HannStar",
            "IVM" => "iiyama",
            "XMI" => "Xiaomi",
            "HIS" => "Hisense",
            _ => code,
        };
        return format!("{vendor} ({hw})");
    }
    gdi.replace(r"\\.\", "")
}
fn edid_strings(edid: &[u8]) -> (String, String) {
    let (mut name, mut serial) = (String::new(), String::new());
    if edid.len() < 128 {
        return (name, serial);
    }
    for offset in (54..=108).step_by(18) {
        if edid[offset] != 0 || edid[offset + 1] != 0 {
            continue;
        }
        let bytes = &edid[offset + 5..offset + 18];
        let end = bytes
            .iter()
            .position(|&c| c == 0 || c == 10)
            .unwrap_or(bytes.len());
        let value: String = bytes[..end].iter().map(|&b| char::from(b)).collect();
        match edid[offset + 3] {
            0xfc if name.is_empty() => name = value.trim().into(),
            0xff if serial.is_empty() => serial = value.trim().into(),
            _ => {}
        }
    }
    (name, serial)
}
fn unique_modes(mut modes: Vec<DisplayMode>) -> Vec<DisplayMode> {
    modes.retain(|m| m.width > 0 && m.height > 0);
    modes.sort_by_key(|m| std::cmp::Reverse((m.width, m.height, m.refresh_hz)));
    modes.dedup();
    modes
}
pub(crate) fn registry_identity(instance: &str) -> (String, String, String) {
    if instance.is_empty() {
        return Default::default();
    }
    let key = RegKey::predef(HKEY_LOCAL_MACHINE).open_subkey_with_flags(
        format!(r"SYSTEM\CurrentControlSet\Enum\{instance}"),
        KEY_READ,
    );
    let Ok(key) = key else {
        return Default::default();
    };
    let driver = key.get_value("Driver").unwrap_or_default();
    let edid = key
        .open_subkey_with_flags("Device Parameters", KEY_READ)
        .ok()
        .and_then(|p| p.get_raw_value("EDID").ok());
    let (name, serial) = edid_strings(&edid.map(|v| v.bytes).unwrap_or_default());
    (driver, name, serial)
}
fn mode(dm: &DEVMODEW) -> DisplayMode {
    DisplayMode {
        width: dm.dmPelsWidth,
        height: dm.dmPelsHeight,
        refresh_hz: dm.dmDisplayFrequency,
    }
}
fn modes(gdi: &str) -> (Option<DisplayMode>, Vec<DisplayMode>) {
    if gdi.is_empty() {
        return (None, vec![]);
    }
    let wide: Vec<u16> = gdi.encode_utf16().chain(Some(0)).collect();
    let mut dm = DEVMODEW {
        dmSize: size_of::<DEVMODEW>() as u16,
        ..Default::default()
    };
    unsafe {
        let current = EnumDisplaySettingsW(PCWSTR(wide.as_ptr()), ENUM_CURRENT_SETTINGS, &mut dm)
            .as_bool()
            .then(|| mode(&dm));
        let mut all = vec![];
        for i in 0.. {
            if !EnumDisplaySettingsW(
                PCWSTR(wide.as_ptr()),
                ENUM_DISPLAY_SETTINGS_MODE(i),
                &mut dm,
            )
            .as_bool()
            {
                break;
            }
            all.push(mode(&dm));
        }
        (current, unique_modes(all))
    }
}
unsafe extern "system" fn primary_callback(
    h: HMONITOR,
    _: HDC,
    _: *mut RECT,
    data: LPARAM,
) -> BOOL {
    let mut info = MONITORINFOEXW::default();
    info.monitorInfo.cbSize = size_of::<MONITORINFOEXW>() as u32;
    unsafe {
        if GetMonitorInfoW(h, &mut info as *mut _ as *mut MONITORINFO).as_bool()
            && info.monitorInfo.dwFlags & 1 != 0
        {
            (*(data.0 as *mut HashSet<String>)).insert(text(&info.szDevice));
        }
    }
    BOOL(1)
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
fn query_paths() -> Result<Vec<DISPLAYCONFIG_PATH_INFO>> {
    for _ in 0..3 {
        let (mut np, mut nm) = (0, 0);
        unsafe {
            GetDisplayConfigBufferSizes(QDC_ALL_PATHS, &mut np, &mut nm).ok()?;
            let mut paths = vec![DISPLAYCONFIG_PATH_INFO::default(); np as usize];
            let mut modes = vec![DISPLAYCONFIG_MODE_INFO::default(); nm as usize];
            let status = QueryDisplayConfig(
                QDC_ALL_PATHS,
                &mut np,
                paths.as_mut_ptr(),
                &mut nm,
                modes.as_mut_ptr(),
                None,
            );
            if status == ERROR_INSUFFICIENT_BUFFER {
                continue;
            }
            status.ok()?;
            paths.truncate(np as usize);
            return Ok(paths);
        }
    }
    Err(windows::core::Error::from_hresult(
        ERROR_INSUFFICIENT_BUFFER.to_hresult(),
    ))
}

/// Query CCD plus remembered monitor instances. No display settings are changed.
pub fn list_displays() -> Result<Vec<Display>> {
    let paths = query_paths()?;
    let mut primary = HashSet::new();
    unsafe {
        let _ = EnumDisplayMonitors(
            None,
            None,
            Some(primary_callback),
            LPARAM(&mut primary as *mut HashSet<String> as isize),
        );
    }
    let mut targets = HashSet::new();
    let mut used = HashSet::new();
    let mut result = vec![];
    // Active paths must reserve their sources before inactive candidates are considered.
    let mut paths = paths;
    paths.sort_by_key(|p| p.flags & DISPLAYCONFIG_PATH_ACTIVE == 0);
    for p in &paths {
        let t = p.targetInfo;
        let key = (t.adapterId.HighPart, t.adapterId.LowPart, t.id);
        if !targets.insert(key) {
            continue;
        }
        let active = p.flags & DISPLAYCONFIG_PATH_ACTIVE != 0;
        unsafe {
            let mut name = DISPLAYCONFIG_TARGET_DEVICE_NAME {
                header: header(
                    DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                    size_of::<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                    t.adapterId,
                    t.id,
                ),
                ..Default::default()
            };
            let name_ok = DisplayConfigGetDeviceInfo(&mut name.header) == 0;
            let identity = name_ok
                .then(|| parse_device_path(&text(&name.monitorDevicePath)))
                .flatten();
            // Unavailable CCD targets without an identity are routing candidates, not monitors.
            if identity.is_none() && !active {
                continue;
            }
            let source = paths
                .iter()
                .filter(|c| c.targetInfo.adapterId == t.adapterId && c.targetInfo.id == t.id)
                .find(|c| {
                    active
                        || !used.contains(&(
                            c.sourceInfo.adapterId.HighPart,
                            c.sourceInfo.adapterId.LowPart,
                            c.sourceInfo.id,
                        ))
                });
            let mut gdi = String::new();
            if let Some(source) = source {
                let s = source.sourceInfo;
                used.insert((s.adapterId.HighPart, s.adapterId.LowPart, s.id));
                let mut name = DISPLAYCONFIG_SOURCE_DEVICE_NAME {
                    header: header(
                        DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size_of::<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                        s.adapterId,
                        s.id,
                    ),
                    ..Default::default()
                };
                if DisplayConfigGetDeviceInfo(&mut name.header) == 0 {
                    gdi = text(&name.viewGdiDeviceName);
                }
            }
            let (hw, instance) = identity.unwrap_or_default();
            let (driver, edid_name, serial) = registry_identity(&instance);
            let target_name = text(&name.monitorFriendlyDeviceName);
            let name = if target_name.trim().is_empty() {
                edid_name
            } else {
                target_name
            };
            let (current, supported_modes) = modes(&gdi);
            let mut color = DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO {
                header: header(
                    DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO,
                    size_of::<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(),
                    t.adapterId,
                    t.id,
                ),
                ..Default::default()
            };
            let color =
                (DisplayConfigGetDeviceInfo(&mut color.header) == 0).then(|| color.Anonymous.value);
            let mut position = None;
            if active && !gdi.is_empty() {
                let wide: Vec<u16> = gdi.encode_utf16().chain(Some(0)).collect();
                let mut dm = DEVMODEW {
                    dmSize: size_of::<DEVMODEW>() as u16,
                    ..Default::default()
                };
                if EnumDisplaySettingsW(PCWSTR(wide.as_ptr()), ENUM_CURRENT_SETTINGS, &mut dm)
                    .as_bool()
                {
                    let pt = dm.Anonymous1.Anonymous2.dmPosition;
                    position = Some((pt.x, pt.y));
                }
            }
            result.push(Display {
                stable_id: stable_id(&hw, &driver, &serial, &gdi),
                friendly_name: friendly_name(&name, &hw, &gdi),
                primary: active && primary.contains(&gdi),
                gdi_name: gdi,
                active,
                position,
                current_mode: if active { current } else { None },
                supported_modes,
                hdr_supported: color.map(|v| v & 1 != 0),
                hdr_enabled: color.map(|v| v & 2 != 0),
            });
        }
    }
    // CCD can omit unplugged devices entirely; the Enum hive retains their 1.7 identities.
    if let Ok(root) = RegKey::predef(HKEY_LOCAL_MACHINE)
        .open_subkey_with_flags(r"SYSTEM\CurrentControlSet\Enum\DISPLAY", KEY_READ)
    {
        for hw in root.enum_keys().flatten() {
            let Ok(key) = root.open_subkey_with_flags(&hw, KEY_READ) else {
                continue;
            };
            for instance in key.enum_keys().flatten() {
                let (driver, name, serial) =
                    registry_identity(&format!(r"DISPLAY\{hw}\{instance}"));
                let id = stable_id(&hw, &driver, &serial, "");
                if result.iter().any(|d| d.stable_id == id) {
                    continue;
                }
                result.push(Display {
                    stable_id: id,
                    gdi_name: String::new(),
                    friendly_name: friendly_name(&name, &hw, ""),
                    active: false,
                    primary: false,
                    position: None,
                    current_mode: None,
                    supported_modes: vec![],
                    hdr_supported: None,
                    hdr_enabled: None,
                });
            }
        }
    }
    Ok(result)
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn utf16_strings_stop_at_nul() {
        assert_eq!(text(&[65, 0, 66]), "A");
        assert_eq!(text(&[65, 66]), "AB");
        assert_eq!(text(&[]), "");
    }
    #[test]
    fn identity_matches_legacy() {
        let (hw, instance) =
            parse_device_path(r"\\?\DISPLAY#GSM5B7F#5&abc&0&UID4352#{guid}").unwrap();
        assert_eq!(instance, r"DISPLAY\GSM5B7F\5&abc&0&UID4352");
        assert_eq!(
            stable_id(&hw, r"{guid}\0004", "serial", "gdi"),
            r"MONITOR\GSM5B7F\{guid}\0004"
        );
        assert_eq!(stable_id(&hw, " ", "serial", "gdi"), "GSM5B7F#serial");
        assert_eq!(stable_id("", "driver", "", "gdi"), "gdi");
        assert_eq!(stable_id(&hw, "", "", "gdi"), "GSM5B7F#");
        for path in ["", "DISPLAY##instance", "DISPLAY#HW#", "DISPLAY#HW"] {
            assert!(parse_device_path(path).is_none());
        }
    }
    #[test]
    fn names_match_legacy() {
        assert_eq!(friendly_name("TV", "GSM0001", ""), "TV");
        assert_eq!(friendly_name("", "gsm0001", ""), "LG (gsm0001)");
        assert_eq!(friendly_name("", "ZZZ0001", ""), "ZZZ (ZZZ0001)");
        assert_eq!(friendly_name("", "", r"\\.\DISPLAY1"), "DISPLAY1");
    }
    #[test]
    fn edid_descriptors() {
        assert_eq!(edid_strings(&[0; 127]), (String::new(), String::new()));
        let mut edid = [0; 128];
        edid[57] = 0xfc;
        edid[59..65].copy_from_slice(b"LG TV\n");
        edid[75] = 0xff;
        edid[77..81].copy_from_slice(b"123\n");
        assert_eq!(edid_strings(&edid), ("LG TV".into(), "123".into()));
    }
    #[test]
    fn modes_are_unique_and_descending() {
        let a = DisplayMode {
            width: 1920,
            height: 1080,
            refresh_hz: 60,
        };
        let b = DisplayMode {
            refresh_hz: 120,
            ..a
        };
        let c = DisplayMode {
            width: 3840,
            height: 2160,
            ..a
        };
        assert_eq!(unique_modes(vec![a, b, a, c]), vec![c, b, a]);
    }
}
