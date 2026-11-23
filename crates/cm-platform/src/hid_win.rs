//! HID game controllers on Windows (1.7.1 `HidDevices`, `BluetoothBatteryReader`, `HidHomeButtonReader`):
//! the battery Windows' Bluetooth stack keeps for each pad (#225, issue #224) and the centre button
//! of other brands through the standard HID usages (#226, issue #217). The rules are in
//! cm_core::hid_usage and cm_core::controller_battery.
//!
//! Collections are opened "for query" (no read access) to list them, which works while SDL, Steam or
//! a game has them open. Only the collections of a game controller that declare a centre-button usage
//! are opened for reading, shared, so everyone else keeps reading them; each one is read by a thread
//! that blocks on the next report (nothing runs while the pad is idle) and ends when it is unplugged.

use std::{
    collections::{HashMap, HashSet},
    sync::{Mutex, OnceLock},
    time::Duration,
};

use cm_core::{
    controller_battery::{self, BatteryReading},
    hid_usage::{self, Usage},
};
use windows::{
    Win32::{
        Devices::{
            DeviceAndDriverInstallation::{
                CM_Get_DevNode_PropertyW, CM_Get_Device_Interface_PropertyW, CM_Get_Parent, CM_LOCATE_DEVNODE_NORMAL, CM_Locate_DevNodeW, CR_BUFFER_SMALL, CR_SUCCESS,
                DIGCF_DEVICEINTERFACE, DIGCF_PRESENT, SP_DEVICE_INTERFACE_DATA, SP_DEVICE_INTERFACE_DETAIL_DATA_W, SetupDiDestroyDeviceInfoList, SetupDiEnumDeviceInterfaces,
                SetupDiGetClassDevsW, SetupDiGetDeviceInterfaceDetailW,
            },
            HumanInterfaceDevice::{
                HIDD_ATTRIBUTES, HIDP_BUTTON_CAPS, HIDP_CAPS, HIDP_STATUS_SUCCESS, HidD_FreePreparsedData, HidD_GetAttributes, HidD_GetHidGuid, HidD_GetPreparsedData, HidP_GetCaps,
                HidP_GetSpecificButtonCaps, HidP_GetUsagesEx, HidP_Input, PHIDP_PREPARSED_DATA, USAGE_AND_PAGE,
            },
            Properties::{DEVPROP_TYPE_BYTE, DEVPROP_TYPE_STRING, DEVPROPTYPE},
        },
        Foundation::{CloseHandle, DEVPROPKEY, GENERIC_READ, HANDLE},
        Storage::FileSystem::{CreateFileW, FILE_FLAGS_AND_ATTRIBUTES, FILE_SHARE_READ, FILE_SHARE_WRITE, OPEN_EXISTING, ReadFile},
    },
    core::{GUID, PCWSTR},
};

/// What a HID collection says about itself, read without opening it for input.
#[derive(Clone, Debug)]
pub struct HidInfo {
    pub path: String,
    pub vendor: u16,
    pub product: u16,
    pub page: u16,
    pub usage: u16,
    pub input_len: usize,
}

impl HidInfo {
    pub fn is_game_controller(&self) -> bool { hid_usage::is_game_controller(self.page, self.usage) }
}

// ───────────── the log ─────────────

static LOGGER: OnceLock<fn(&str)> = OnceLock::new();

/// The app's log (cm-app sets it at start-up).
pub fn set_logger(f: fn(&str)) { let _ = LOGGER.set(f); }

fn log(line: &str) { if let Some(f) = LOGGER.get() { f(line); } }

// ───────────── listing and querying ─────────────

fn wide(s: &str) -> Vec<u16> { s.encode_utf16().chain(Some(0)).collect() }

struct Handle(HANDLE);
impl Drop for Handle {
    fn drop(&mut self) { unsafe { let _ = CloseHandle(self.0); } }
}

fn open(path: &str, access: u32) -> Option<Handle> {
    let w = wide(path);
    unsafe { CreateFileW(PCWSTR(w.as_ptr()), access, FILE_SHARE_READ | FILE_SHARE_WRITE, None, OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES(0), None) }.ok().map(Handle)
}

struct Preparsed(PHIDP_PREPARSED_DATA);
impl Drop for Preparsed {
    fn drop(&mut self) { unsafe { let _ = HidD_FreePreparsedData(self.0); } }
}

fn preparsed(h: &Handle) -> Option<Preparsed> {
    let mut p = PHIDP_PREPARSED_DATA::default();
    unsafe { HidD_GetPreparsedData(h.0, &mut p) }.then_some(Preparsed(p))
}

/// Every present HID collection, as a device interface path.
pub fn paths() -> Vec<String> {
    let mut out = Vec::new();
    unsafe {
        let guid = HidD_GetHidGuid();
        let Ok(set) = SetupDiGetClassDevsW(Some(&guid), PCWSTR::null(), None, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE) else { return out };
        let mut data = SP_DEVICE_INTERFACE_DATA { cbSize: size_of::<SP_DEVICE_INTERFACE_DATA>() as u32, ..Default::default() };
        let mut i = 0;
        while SetupDiEnumDeviceInterfaces(set, None, &guid, i, &mut data).is_ok() {
            i += 1;
            let mut needed = 0u32;
            let _ = SetupDiGetDeviceInterfaceDetailW(set, &data, None, 0, Some(&mut needed), None);
            if needed < size_of::<SP_DEVICE_INTERFACE_DETAIL_DATA_W>() as u32 { continue; }
            // u64s: the detail wants the alignment of its cbSize
            let mut buf = vec![0u64; (needed as usize).div_ceil(8)];
            let detail = buf.as_mut_ptr() as *mut SP_DEVICE_INTERFACE_DETAIL_DATA_W;
            (*detail).cbSize = size_of::<SP_DEVICE_INTERFACE_DETAIL_DATA_W>() as u32;
            if SetupDiGetDeviceInterfaceDetailW(set, &data, Some(detail), needed, None, None).is_err() { continue; }
            if let Ok(path) = PCWSTR((*detail).DevicePath.as_ptr()).to_string() { if !path.is_empty() { out.push(path); } }
        }
        let _ = SetupDiDestroyDeviceInfoList(set);
    }
    out
}

/// Vendor, product and top-level usage of a collection; None when it can't be queried.
pub fn query(path: &str) -> Option<HidInfo> {
    let h = open(path, 0)?;
    let mut a = HIDD_ATTRIBUTES { Size: size_of::<HIDD_ATTRIBUTES>() as u32, ..Default::default() };
    if !unsafe { HidD_GetAttributes(h.0, &mut a) } { return None; }
    let p = preparsed(&h)?;
    let mut caps = HIDP_CAPS::default();
    if unsafe { HidP_GetCaps(p.0, &mut caps) } != HIDP_STATUS_SUCCESS { return None; }
    Some(HidInfo { path: path.to_string(), vendor: a.VendorID, product: a.ProductID, page: caps.UsagePage, usage: caps.Usage, input_len: caps.InputReportByteLength as usize })
}

/// The first centre-button usage a collection declares as an input button.
fn declared_home(path: &str) -> Option<Usage> {
    let h = open(path, 0)?;
    let p = preparsed(&h)?;
    hid_usage::HOME_USAGES.into_iter().find(|&(page, id)| {
        // SAFETY: plain data, filled in by HidP
        let mut caps: [HIDP_BUTTON_CAPS; 8] = unsafe { std::mem::zeroed() };
        let mut len = caps.len() as u16;
        let status = unsafe { HidP_GetSpecificButtonCaps(HidP_Input, Some(page), Some(0), Some(id), caps.as_mut_ptr(), &mut len, p.0) };
        status == HIDP_STATUS_SUCCESS && len > 0
    })
}

// ───────────── the device tree ─────────────

const INSTANCE_ID: DEVPROPKEY = DEVPROPKEY { fmtid: GUID::from_u128(0x78c34fc8_104a_4aca_9ea4_524d52996e57), pid: 256 };
const FRIENDLY_NAME: DEVPROPKEY = DEVPROPKEY { fmtid: GUID::from_u128(0xa45c254e_df1c_4efd_8020_67d146a850e0), pid: 14 };
const DEVICE_DESC: DEVPROPKEY = DEVPROPKEY { fmtid: GUID::from_u128(0xa45c254e_df1c_4efd_8020_67d146a850e0), pid: 2 };
/// DEVPKEY {104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2: Bluetooth battery level, one byte, 0..100.
const BT_BATTERY: DEVPROPKEY = DEVPROPKEY { fmtid: GUID::from_u128(0x104ea319_6ee2_4701_bd47_8ddbf425bbe5), pid: 2 };

fn utf16(buf: &[u8]) -> String {
    let units: Vec<u16> = buf.chunks_exact(2).map(|c| u16::from_le_bytes([c[0], c[1]])).collect();
    String::from_utf16_lossy(&units).trim_end_matches('\0').to_string()
}

fn interface_string(path: &str, key: &DEVPROPKEY) -> Option<String> {
    let w = wide(path);
    let (mut ty, mut size) = (DEVPROPTYPE(0), 0u32);
    unsafe {
        if CM_Get_Device_Interface_PropertyW(PCWSTR(w.as_ptr()), key, &mut ty, None, &mut size, 0) != CR_BUFFER_SMALL || size == 0 { return None; }
        let mut buf = vec![0u8; size as usize];
        if CM_Get_Device_Interface_PropertyW(PCWSTR(w.as_ptr()), key, &mut ty, Some(buf.as_mut_ptr()), &mut size, 0) != CR_SUCCESS || ty != DEVPROP_TYPE_STRING { return None; }
        Some(utf16(&buf[..size as usize]))
    }
}

fn node_string(node: u32, key: &DEVPROPKEY) -> Option<String> {
    let (mut ty, mut size) = (DEVPROPTYPE(0), 0u32);
    unsafe {
        if CM_Get_DevNode_PropertyW(node, key, &mut ty, None, &mut size, 0) != CR_BUFFER_SMALL || size == 0 { return None; }
        let mut buf = vec![0u8; size as usize];
        if CM_Get_DevNode_PropertyW(node, key, &mut ty, Some(buf.as_mut_ptr()), &mut size, 0) != CR_SUCCESS || ty != DEVPROP_TYPE_STRING { return None; }
        Some(utf16(&buf[..size as usize])).filter(|s| !s.trim().is_empty())
    }
}

fn node_byte(node: u32, key: &DEVPROPKEY) -> Option<u8> {
    let (mut ty, mut size, mut b) = (DEVPROPTYPE(0), 1u32, 0u8);
    let r = unsafe { CM_Get_DevNode_PropertyW(node, key, &mut ty, Some(&mut b), &mut size, 0) };
    (r == CR_SUCCESS && ty == DEVPROP_TYPE_BYTE && size == 1).then_some(b)
}

/// The device node behind a collection's interface path (0 when Windows doesn't know it).
fn dev_node_of(path: &str) -> u32 {
    let Some(id) = interface_string(path, &INSTANCE_ID) else { return 0 };
    let w = wide(&id);
    let mut node = 0u32;
    if unsafe { CM_Locate_DevNodeW(&mut node, PCWSTR(w.as_ptr()), CM_LOCATE_DEVNODE_NORMAL) } == CR_SUCCESS { node } else { 0 }
}

fn parent_of(node: u32) -> u32 {
    let mut parent = 0u32;
    if unsafe { CM_Get_Parent(&mut parent, node, 0) } == CR_SUCCESS { parent } else { 0 }
}

// ───────────── Bluetooth battery (#225) ─────────────

/// HID collection → Bluetooth service → Bluetooth device, with room to spare.
const MAX_DEPTH: usize = 6;

/// The battery Windows keeps for each Bluetooth game controller: the number Settings › Bluetooth
/// shows. It sits on the pad's Bluetooth device node, above the HID collection the input comes from.
/// Pads on a cable or a dongle have no such node. Blocks briefly: call it off the UI thread.
pub fn bluetooth_batteries() -> Vec<BatteryReading> {
    let mut out = Vec::new();
    // a pad can expose several collections; each Bluetooth device is listed once
    let mut seen = HashSet::new();
    for path in paths() {
        let Some(info) = query(&path).filter(HidInfo::is_game_controller) else { continue };
        let mut node = dev_node_of(&path);
        for _ in 0..MAX_DEPTH {
            if node == 0 { break; }
            if let Some(level) = node_byte(node, &BT_BATTERY) {
                if !seen.insert(node) { break; }
                if let Some(percent) = controller_battery::from_bluetooth(level) {
                    let id = node_string(node, &INSTANCE_ID).unwrap_or_else(|| path.clone());
                    let name = node_string(node, &FRIENDLY_NAME).or_else(|| node_string(node, &DEVICE_DESC)).unwrap_or_else(|| "Bluetooth".into());
                    out.push(BatteryReading { id: format!("bt:{id}"), name, percent, charging: false, vendor: info.vendor, product: info.product, bluetooth: true });
                }
                break;
            }
            node = parent_of(node);
        }
    }
    out
}

static BLUETOOTH: Mutex<Vec<BatteryReading>> = Mutex::new(Vec::new());
static BLUETOOTH_BUSY: std::sync::atomic::AtomicBool = std::sync::atomic::AtomicBool::new(false);

/// Reads the Bluetooth batteries again on a thread of its own (the device queries take a moment and
/// the controller thread must not wait); `bluetooth_cached` has the answer once it is in.
pub fn refresh_bluetooth() {
    use std::sync::atomic::Ordering;
    if BLUETOOTH_BUSY.swap(true, Ordering::AcqRel) { return; }
    let spawned = std::thread::Builder::new().name("bt-battery".into()).spawn(|| {
        let list = bluetooth_batteries();
        let mut cached = BLUETOOTH.lock().unwrap_or_else(|e| e.into_inner());
        for r in list.iter().filter(|r| !cached.iter().any(|c| c.id == r.id)) {
            log(&format!("Controle: bateria pelo Bluetooth do Windows: {} ({:04X}:{:04X}) {}%", r.name, r.vendor, r.product, r.percent));
        }
        *cached = list;
        BLUETOOTH_BUSY.store(false, Ordering::Release);
    });
    if spawned.is_err() { BLUETOOTH_BUSY.store(false, Ordering::Release); }
}

/// The Bluetooth batteries from the last `refresh_bluetooth`.
pub fn bluetooth_cached() -> Vec<BatteryReading> { BLUETOOTH.lock().unwrap_or_else(|e| e.into_inner()).clone() }

// ───────────── centre button (#226) ─────────────

/// One collection of a controller: what it declares and what it is doing.
struct Collection {
    info: HidInfo,
    /// Looked at for a centre button already (Some(None): it has none).
    home: Option<Option<Usage>>,
    reading: bool,
    unreadable: bool,
    held: bool,
}

static COLLECTIONS: Mutex<Option<HashMap<String, Collection>>> = Mutex::new(None);
static STARTED: OnceLock<()> = OnceLock::new();
/// How often new collections are looked for (a pad plugged in or paired).
const SCAN_EVERY: Duration = Duration::from_secs(3);

/// Starts reading the centre button of other brands' controllers (once; it runs for the app's life).
pub fn start_home_reader() {
    STARTED.get_or_init(|| {
        let _ = std::thread::Builder::new().name("hid-home".into()).spawn(|| loop {
            scan();
            std::thread::sleep(SCAN_EVERY);
        });
    });
}

/// For each collection whose centre button is read: whether it is held right now.
pub fn home_held() -> Vec<bool> {
    let st = COLLECTIONS.lock().unwrap_or_else(|e| e.into_inner());
    st.as_ref().map(|m| m.values().filter(|c| c.reading).map(|c| c.held).collect()).unwrap_or_default()
}

/// For the diagnostic report: every collection of the controllers (not Xbox or PlayStation) with its
/// centre-button usage and whether it is read (1.7.1's controller test, #227).
pub fn describe() -> Vec<String> {
    let st = COLLECTIONS.lock().unwrap_or_else(|e| e.into_inner());
    let Some(m) = st.as_ref() else { return Vec::new() };
    let mut rows: Vec<&Collection> = m.values().filter(|c| c.home.is_some()).collect();
    rows.sort_by_key(|c| (c.info.vendor, c.info.product, c.info.page, c.info.usage));
    rows.iter().map(|c| {
        let i = &c.info;
        let home = match c.home.flatten() { Some(u) => format!("centre button = {}", hid_usage::describe(u)), None => "no standard centre button".into() };
        let state = if c.unreadable { "Windows won't let it be read" } else if c.reading { "read" } else { "not read" };
        format!("HID {:04X}:{:04X} {}: {home}; {state}{}", i.vendor, i.product, hid_usage::describe((i.page, i.usage)), if c.held { "; held" } else { "" })
    }).collect()
}

fn scan() {
    let paths = paths();
    let known: HashSet<String> = {
        let mut st = COLLECTIONS.lock().unwrap_or_else(|e| e.into_inner());
        let m = st.get_or_insert_with(HashMap::new);
        // gone: forgotten (a reader still running ends on its own when its read fails)
        m.retain(|p, c| c.reading || paths.contains(p));
        m.keys().cloned().collect()
    };
    // only new collections are opened to be queried
    let fresh: Vec<HidInfo> = paths.iter().filter(|p| !known.contains(*p)).filter_map(|p| query(p)).collect();
    let mut st = COLLECTIONS.lock().unwrap_or_else(|e| e.into_inner());
    let m = st.get_or_insert_with(HashMap::new);
    for info in fresh { m.insert(info.path.clone(), Collection { info, home: None, reading: false, unreadable: false, held: false }); }
    // the devices that are game controllers, by vendor and product: their other collections count too
    // (the button may sit on a sibling "consumer control" collection)
    let pads: HashSet<(u16, u16)> = m.values()
        .filter(|c| c.info.is_game_controller() && hid_usage::reads_home_from_hid(c.info.vendor))
        .map(|c| (c.info.vendor, c.info.product)).collect();
    let todo: Vec<String> = m.values().filter(|c| c.home.is_none() && pads.contains(&(c.info.vendor, c.info.product))).map(|c| c.info.path.clone()).collect();
    drop(st);
    let looked: Vec<(String, Option<Usage>)> = todo.into_iter().map(|p| { let h = declared_home(&p); (p, h) }).collect();
    let mut st = COLLECTIONS.lock().unwrap_or_else(|e| e.into_inner());
    let m = st.get_or_insert_with(HashMap::new);
    for (path, home) in looked {
        let Some(c) = m.get_mut(&path) else { continue };
        c.home = Some(home);
        let Some(usage) = home else { continue };
        if c.reading || c.unreadable { continue; }
        let Some(reader) = Reader::open(&c.info) else {
            c.unreadable = true;
            log(&format!("Controle: botão central em {:04X}:{:04X} ({}), mas o Windows não deixa ler esse dispositivo", c.info.vendor, c.info.product, hid_usage::describe(usage)));
            continue;
        };
        c.reading = true;
        log(&format!("Controle: lendo o botão central pelo HID ({:04X}:{:04X}, {})", c.info.vendor, c.info.product, hid_usage::describe(usage)));
        let _ = std::thread::Builder::new().name("hid-home-read".into()).spawn(move || reader.run());
    }
}

/// A collection open to read its input reports, shared, with its parsed descriptor.
struct Reader {
    handle: Handle,
    preparsed: Preparsed,
    path: String,
    len: usize,
}

// SAFETY: the handle and the preparsed data belong to this reader alone; it moves to its thread once.
unsafe impl Send for Reader {}

impl Reader {
    fn open(info: &HidInfo) -> Option<Reader> {
        if info.input_len == 0 { return None; }
        let handle = open(&info.path, GENERIC_READ.0)?;
        let preparsed = preparsed(&handle)?;
        Some(Reader { handle, preparsed, path: info.path.clone(), len: info.input_len })
    }

    /// Reads reports until the pad is unplugged; then the next scan looks at it again.
    fn run(self) {
        let mut buf = vec![0u8; self.len];
        loop {
            let mut n = 0u32;
            if unsafe { ReadFile(self.handle.0, Some(&mut buf), Some(&mut n), None) }.is_err() || n == 0 { break; }
            let mut list = [USAGE_AND_PAGE::default(); 64];
            let mut count = list.len() as u32;
            // a report of another id says nothing about the buttons: keep what they were
            if unsafe { HidP_GetUsagesEx(HidP_Input, Some(0), list.as_mut_ptr(), &mut count, self.preparsed.0, &buf[..n as usize]) } != HIDP_STATUS_SUCCESS { continue; }
            let pressed: Vec<Usage> = list[..count as usize].iter().map(|u| (u.UsagePage, u.Usage)).collect();
            let held = hid_usage::holds_home(&pressed);
            let mut st = COLLECTIONS.lock().unwrap_or_else(|e| e.into_inner());
            if let Some(c) = st.as_mut().and_then(|m| m.get_mut(&self.path)) { c.held = held; }
        }
        let mut st = COLLECTIONS.lock().unwrap_or_else(|e| e.into_inner());
        if let Some(m) = st.as_mut() { m.remove(&self.path); }
    }
}
