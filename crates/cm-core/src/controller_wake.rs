//! Waking the PC from sleep with the controller (1.7 ControllerWake.cs, issue #155): what wakes it is
//! the device the controller talks through (the Xbox Wireless Adapter, the Bluetooth radio, or the
//! controller itself on a cable), and Windows only lets it once "Allow this device to wake the
//! computer" is on (WMI `MSPower_DeviceWakeEnable`). Devices are told apart by their instance id,
//! which, unlike the name, isn't translated. Pure logic and the PowerShell scripts; running them is
//! `cm_platform::wake_win`.

use serde_json::Value;

#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord)]
pub enum WakeKind { None, XboxAdapter, Bluetooth, Controller }

/// A device Windows can let wake the PC (WMI MSPower_DeviceWakeEnable) and whether it may now.
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct WakeDevice {
    pub instance_name: String,
    pub name: String,
    pub pnp_class: String,
    pub armed: bool,
}

impl WakeDevice {
    pub fn kind(&self) -> WakeKind { classify(&self.instance_name, &self.pnp_class) }
}

/// Xbox Wireless Adapter for Windows: the original, the slim one, and the newer revisions.
const XBOX_ADAPTER_IDS: [&str; 4] = ["PID_02E6", "PID_02FE", "PID_02F9", "PID_091E"];

/// Sony, Nintendo, 8BitDo, Valve, Logitech gamepads, PowerA, PDP, Hori, Nacon. Xbox pads are found by
/// their class instead: Microsoft also makes the keyboards and mice.
const GAMEPAD_VENDORS: [&str; 9] = ["VID_054C", "VID_057E", "VID_2DC8", "VID_28DE", "VID_046D&PID_C2", "VID_20D6", "VID_0E6F", "VID_0F0D", "VID_146B"];

pub fn classify(instance_name: &str, pnp_class: &str) -> WakeKind {
    let id = instance_name.to_uppercase();
    if id.is_empty() { return WakeKind::None; }
    if id.starts_with(r"USB\VID_045E&") && XBOX_ADAPTER_IDS.iter().any(|p| id.contains(p)) { return WakeKind::XboxAdapter; }
    // the radio itself, not the devices paired through it (BTHENUM\…, BTHLE\…)
    if pnp_class.eq_ignore_ascii_case("Bluetooth") && (id.starts_with(r"USB\") || id.starts_with(r"PCI\")) { return WakeKind::Bluetooth; }
    // XInput pads show up as HID collections marked IG_; Xbox pads on a cable as their own class
    if id.contains("&IG_") || pnp_class.eq_ignore_ascii_case("XnaComposite") || pnp_class.eq_ignore_ascii_case("XboxComposite") { return WakeKind::Controller; }
    if (id.starts_with(r"USB\") || id.starts_with(r"HID\")) && GAMEPAD_VENDORS.iter().any(|v| id.contains(v)) { return WakeKind::Controller; }
    WakeKind::None
}

/// The controller-related devices, the adapter first, then Bluetooth, then controllers (by name).
pub fn relevant(devices: impl IntoIterator<Item = WakeDevice>) -> Vec<WakeDevice> {
    let mut list: Vec<WakeDevice> = devices.into_iter().filter(|d| d.kind() != WakeKind::None).collect();
    list.sort_by(|a, b| a.kind().cmp(&b.kind()).then_with(|| a.name.to_lowercase().cmp(&b.name.to_lowercase())));
    list
}

/// Reads the JSON the query script prints (one object or an array).
pub fn parse(json: &str) -> Vec<WakeDevice> {
    let json = json.trim().trim_start_matches('\u{feff}');
    if json.is_empty() { return Vec::new(); }
    let Ok(value) = serde_json::from_str::<Value>(json) else { return Vec::new() };
    let items = match value { Value::Array(a) => a, other => vec![other] };
    let text = |item: &Value, key: &str| item.get(key).and_then(Value::as_str).unwrap_or("").to_string();
    items.iter().filter(|i| i.is_object()).filter_map(|item| {
        let instance = text(item, "InstanceName");
        if instance.trim().is_empty() { return None; }
        let name = text(item, "Name");
        Some(WakeDevice {
            name: if name.trim().is_empty() { device_id(&instance).to_string() } else { name },
            armed: item.get("Enable").and_then(Value::as_bool) == Some(true),
            pnp_class: text(item, "PnpClass"),
            instance_name: instance,
        })
    }).collect()
}

/// WMI names wake entries after the device with an "_N" suffix.
pub fn device_id(instance_name: &str) -> &str {
    match instance_name.rfind('_') {
        Some(cut) if cut > 0 && cut < instance_name.len() - 1 && instance_name[cut + 1..].bytes().all(|b| b.is_ascii_digit()) => &instance_name[..cut],
        _ => instance_name,
    }
}

/// Lists every device that can wake the PC, with its name and class, as JSON (needs no rights).
pub const QUERY_SCRIPT: &str = "[Console]::OutputEncoding = [Text.Encoding]::UTF8\n\
$ErrorActionPreference = 'Stop'\n\
$pnp = @{}\n\
Get-CimInstance Win32_PnPEntity -Property DeviceID,Name,PNPClass | ForEach-Object { $pnp[$_.DeviceID] = $_ }\n\
$rows = @(Get-CimInstance -Namespace root\\wmi -ClassName MSPower_DeviceWakeEnable | ForEach-Object {\n\
    $d = $pnp[($_.InstanceName -replace '_\\d+$', '')]\n\
    [pscustomobject]@{ InstanceName = $_.InstanceName; Name = $d.Name; PnpClass = $d.PNPClass; Enable = [bool]$_.Enable }\n\
})\n\
ConvertTo-Json -InputObject $rows -Compress\n";

/// Exit code of [`enable_script`] when none of the devices is there any more (unplugged meanwhile).
pub const NOT_FOUND_EXIT: i32 = 2;

/// Turns on "may wake the computer" for the given WMI entries (needs an elevated PowerShell). Exits with
/// [`NOT_FOUND_EXIT`] when none of them is found, instead of succeeding without changing anything.
pub fn enable_script<'a>(instance_names: impl IntoIterator<Item = &'a str>) -> String {
    let quoted = instance_names.into_iter().map(|n| format!("'{}'", n.replace('\'', "''"))).collect::<Vec<_>>().join(",");
    format!(
        "$ErrorActionPreference = 'Stop'\n\
$ids = @({quoted})\n\
$found = @(Get-CimInstance -Namespace root\\wmi -ClassName MSPower_DeviceWakeEnable | Where-Object {{ $ids -contains $_.InstanceName }})\n\
if ($found.Count -eq 0) {{ exit {NOT_FOUND_EXIT} }}\n\
$found | Set-CimInstance -Property @{{ Enable = $true }}\n"
    )
}

/// The devices that were meant to be turned on and, read again afterwards, still can't wake the PC (or are
/// gone). Empty means the change really took. `after` None: the second reading failed, nothing is confirmed.
pub fn still_off(attempted: &[WakeDevice], after: Option<&[WakeDevice]>) -> Vec<WakeDevice> {
    let Some(after) = after else { return attempted.to_vec() };
    let armed: Vec<String> = after.iter().filter(|d| d.armed).map(|d| d.instance_name.to_lowercase()).collect();
    attempted.iter().filter(|d| !armed.contains(&d.instance_name.to_lowercase())).cloned().collect()
}

/// The argument for `powershell -EncodedCommand` (UTF-16LE in Base64).
pub fn encode(script: &str) -> String {
    const TABLE: &[u8; 64] = b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    let bytes: Vec<u8> = script.encode_utf16().flat_map(u16::to_le_bytes).collect();
    let mut out = String::with_capacity(bytes.len().div_ceil(3) * 4);
    for chunk in bytes.chunks(3) {
        let n = (chunk[0] as u32) << 16 | (*chunk.get(1).unwrap_or(&0) as u32) << 8 | *chunk.get(2).unwrap_or(&0) as u32;
        for (i, shift) in [18, 12, 6, 0].into_iter().enumerate() {
            if i <= chunk.len() { out.push(TABLE[(n >> shift & 63) as usize] as char); } else { out.push('='); }
        }
    }
    out
}

/// What the Sistema row says: its value and the message behind it, from the devices read.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Status {
    /// The devices couldn't be read.
    Unknown,
    /// Nothing in this PC can wake it.
    None,
    /// Every relevant device may wake the PC.
    Ready,
    /// Some may not yet: the row offers to turn them on.
    Off,
}

pub fn status(devices: Option<&[WakeDevice]>) -> Status {
    match devices {
        None => Status::Unknown,
        Some([]) => Status::None,
        Some(list) if list.iter().all(|d| d.armed) => Status::Ready,
        Some(_) => Status::Off,
    }
}

/// Up to three distinct names, for the messages.
pub fn names<'a>(devices: impl IntoIterator<Item = &'a WakeDevice>) -> String {
    let mut seen: Vec<String> = Vec::new();
    for d in devices { if !seen.iter().any(|n| n.eq_ignore_ascii_case(&d.name)) { seen.push(d.name.clone()); } }
    seen.truncate(3);
    seen.join(", ")
}

#[cfg(test)]
mod tests {
    use super::*;

    fn dev(id: &str, name: &str, class: &str, armed: bool) -> WakeDevice { WakeDevice { instance_name: id.into(), name: name.into(), pnp_class: class.into(), armed } }

    #[test]
    fn devices_are_told_apart_by_instance_id_and_class() {
        let cases = [
            (r"USB\VID_045E&PID_02FE\6&1A2B3C&0&3_0", "XboxWirelessAdapter", WakeKind::XboxAdapter),
            (r"USB\VID_045E&PID_02E6\5&ABC&0&2_0", "Net", WakeKind::XboxAdapter),
            (r"USB\VID_8087&PID_0033\5&2A&0&10_0", "Bluetooth", WakeKind::Bluetooth),
            (r"PCI\VEN_8086&DEV_51F1\3&11583659&1&A3_0", "Bluetooth", WakeKind::Bluetooth),
            (r"BTHENUM\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002045E_PID&0B13\8&1_0", "Bluetooth", WakeKind::None),
            (r"HID\VID_045E&PID_0B12&IG_00\8&2A&0&0000_0", "HIDClass", WakeKind::Controller),
            (r"USB\VID_045E&PID_0B12\3032363030303536_0", "XnaComposite", WakeKind::Controller),
            (r"USB\VID_054C&PID_0CE6&MI_03\7&1_0", "HIDClass", WakeKind::Controller),
            (r"USB\VID_2DC8&PID_3106\8&1_0", "HIDClass", WakeKind::Controller),
            (r"HID\VID_046D&PID_C547&MI_00\A&26C66045&0&0000_0", "Mouse", WakeKind::None),
            (r"HID\VID_045E&PID_07B2&MI_00\8&1_0", "Keyboard", WakeKind::None),
            (r"PCI\VEN_10EC&DEV_8168&SUBSYS_E0001458&REV_16\1CF_0", "Net", WakeKind::None),
            ("", "Bluetooth", WakeKind::None),
        ];
        for (id, class, want) in cases { assert_eq!(classify(id, class), want, "{id}"); }
    }

    #[test]
    fn only_controller_devices_are_kept_adapter_first() {
        let list = relevant([
            dev(r"HID\VID_046D&PID_C547&MI_00\A_0", "Mouse", "Mouse", true),
            dev(r"USB\VID_8087&PID_0033\5_0", "Intel Bluetooth", "Bluetooth", false),
            dev(r"USB\VID_045E&PID_02FE\6_0", "Xbox Wireless Adapter", "XboxWirelessAdapter", false),
        ]);
        assert_eq!(list.iter().map(|d| d.name.as_str()).collect::<Vec<_>>(), ["Xbox Wireless Adapter", "Intel Bluetooth"]);
    }

    #[test]
    fn the_query_output_is_read_as_a_list_or_a_single_object() {
        let many = r#"[{"InstanceName":"USB\\VID_8087&PID_0033\\5_0","Name":"Intel Bluetooth","PnpClass":"Bluetooth","Enable":true},{"InstanceName":"PCI\\VEN_10EC\\1_0","Name":null,"PnpClass":null,"Enable":false}]"#;
        let list = parse(many);
        assert_eq!(list.len(), 2);
        assert!(list[0].armed);
        assert_eq!(list[1].name, r"PCI\VEN_10EC\1");
        assert!(!list[1].armed);
        let one = r#"{"InstanceName":"USB\\VID_045E&PID_02FE\\6_0","Name":"Xbox Wireless Adapter","PnpClass":"XboxWirelessAdapter","Enable":false}"#;
        assert_eq!(parse(one).len(), 1);
        assert!(parse("").is_empty() && parse("[]").is_empty() && parse("not json").is_empty());
        // a byte order mark from a pipe
        assert_eq!(parse(&format!("\u{feff}{one}")).len(), 1);
    }

    #[test]
    fn the_wmi_suffix_is_dropped_from_the_device_id() {
        for (instance, want) in [(r"USB\VID_8087&PID_0033\5&2A&0&10_0", r"USB\VID_8087&PID_0033\5&2A&0&10"), (r"USB\X\Y_12", r"USB\X\Y"), (r"USB\X\Y_ab", r"USB\X\Y_ab"), (r"USB\X\Y", r"USB\X\Y")] {
            assert_eq!(device_id(instance), want);
        }
    }

    #[test]
    fn the_enable_script_quotes_every_id() {
        let script = enable_script([r"USB\VID_045E&PID_02FE\6_0", "it's"]);
        assert!(script.contains(r"$ids = @('USB\VID_045E&PID_02FE\6_0','it''s')"), "{script}");
        assert!(script.contains("Set-CimInstance -Property @{ Enable = $true }"));
    }

    #[test]
    fn the_enable_script_fails_when_no_device_matches() {
        // unplugged while the UAC prompt was open: it must not exit 0 with nothing changed
        let script = enable_script([r"USB\VID_045E&PID_02FE\6_0"]);
        assert!(script.contains(&format!("if ($found.Count -eq 0) {{ exit {NOT_FOUND_EXIT} }}")));
        assert!(script.find("exit").unwrap() < script.find("Set-CimInstance").unwrap());
    }

    #[test]
    fn devices_still_off_after_turning_on_are_reported() {
        let adapter = dev(r"USB\VID_045E&PID_02FE\6_0", "Xbox Wireless Adapter", "XboxWirelessAdapter", false);
        let radio = dev(r"USB\VID_8087&PID_0033\5_0", "Intel Bluetooth", "Bluetooth", false);
        let on = |d: &WakeDevice| WakeDevice { armed: true, ..d.clone() };
        assert!(still_off(&[adapter.clone(), radio.clone()], Some(&[on(&adapter), on(&radio)])).is_empty());
        assert_eq!(still_off(&[adapter.clone(), radio.clone()], Some(&[on(&adapter), radio.clone()])), vec![radio.clone()]);
        // gone from the list (unplugged), or the list couldn't be read: not confirmed
        assert_eq!(still_off(&[adapter.clone(), radio.clone()], Some(&[on(&adapter)])), vec![radio.clone()]);
        assert_eq!(still_off(&[adapter.clone(), radio.clone()], None), vec![adapter, radio]);
    }

    #[test]
    fn scripts_are_encoded_for_powershell_as_utf16() {
        assert_eq!(encode("whoami"), "dwBoAG8AYQBtAGkA");
        // padding, and a character outside ASCII
        assert_eq!(encode("a"), "YQA=");
        assert_eq!(encode("é"), "6QA=");
        assert_eq!(encode(""), "");
    }

    #[test]
    fn the_row_follows_what_was_read() {
        let on = dev("USB\\VID_8087&PID_0033\\5_0", "Intel Bluetooth", "Bluetooth", true);
        let off = dev("USB\\VID_045E&PID_02FE\\6_0", "Xbox Wireless Adapter", "XboxWirelessAdapter", false);
        assert_eq!(status(None), Status::Unknown);
        assert_eq!(status(Some(&[])), Status::None);
        assert_eq!(status(Some(&[on.clone()])), Status::Ready);
        assert_eq!(status(Some(&[on.clone(), off.clone()])), Status::Off);
        // distinct names, at most three
        assert_eq!(names([&on, &on, &off]), "Intel Bluetooth, Xbox Wireless Adapter");
        let many: Vec<_> = (0..5).map(|i| dev("x", &format!("D{i}"), "", false)).collect();
        assert_eq!(names(&many), "D0, D1, D2");
    }
}
