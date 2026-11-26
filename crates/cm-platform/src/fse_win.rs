//! Current-user Xbox Mode Home deployment for the Alpha.

use std::{
    io::ErrorKind,
    path::{Path, PathBuf},
};

use windows::{
    ApplicationModel::Package,
    Foundation::Uri,
    Management::Deployment::{PackageManager, RegisterPackageOptions},
    Win32::System::WinRT::{RO_INIT_MULTITHREADED, RoInitialize, RoUninitialize},
    core::{HRESULT, HSTRING},
};
use winreg::{
    RegKey,
    enums::{HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE, KEY_READ, KEY_WRITE},
};

const NAME: &str = "lippdev.ConsoleMode2Alpha.Home";
const PUBLISHER: &str = "CN=lippdev";
const FAMILY: &str = "lippdev.ConsoleMode2Alpha.Home_8qknpp3d3v6rw";
pub const AUMID: &str = "lippdev.ConsoleMode2Alpha.Home_8qknpp3d3v6rw!Home";
const MANIFEST: &str = "AppxManifest.xml";
const HOME_SELECTED: &str = "CM2_FSE_HOME_SELECTED";
const FILES: &[&str] = &[
    MANIFEST,
    "ConsoleMode2Home.exe",
    "ConsoleMode2Home.Dev.sccd",
    "FseAssets/StoreLogo.png",
    "FseAssets/Square150x150Logo.png",
    "FseAssets/Square44x44Logo.png",
];

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct Status {
    /// Meets the package's minimum Windows build. Device eligibility and rollout are separate.
    pub compatible_build: bool,
    pub developer_mode: bool,
    pub package_files: bool,
    pub registered: bool,
    pub selected: bool,
    pub can_restore_home: bool,
    pub running_as_home: bool,
}

// Public calls run on a worker thread or before the UI initializes COM.
struct Apartment;

impl Apartment {
    fn enter() -> windows::core::Result<Self> {
        unsafe { RoInitialize(RO_INIT_MULTITHREADED) }?;
        Ok(Self)
    }
}

impl Drop for Apartment {
    fn drop(&mut self) {
        unsafe { RoUninitialize() }
    }
}

// GPUI can dispatch background work on an existing STA.
fn with_mta<T: Send + 'static>(operation: fn() -> Result<T, String>) -> Result<T, String> {
    const CHANGED_MODE: HRESULT = HRESULT(0x80010106_u32 as i32);
    match Apartment::enter() {
        Ok(_apartment) => operation(),
        Err(error) if error.code() == CHANGED_MODE => std::thread::Builder::new()
            .name("cm2-home-deployment".into())
            .spawn(move || {
                let _apartment = Apartment::enter().map_err(|e| e.to_string())?;
                operation()
            })
            .map_err(|e| e.to_string())?
            .join()
            .map_err(|_| "A thread de registro da Home foi interrompida".to_owned())?,
        Err(error) => Err(error.to_string()),
    }
}

fn install_root() -> Result<PathBuf, String> {
    let exe = std::env::current_exe().map_err(|e| e.to_string())?;
    exe.parent()
        .map(Path::to_path_buf)
        .ok_or_else(|| "CM2_FSE_PACKAGE_FILES_MISSING".to_owned())
}

fn package_files(root: &Path) -> bool {
    FILES.iter().all(|name| root.join(name).is_file())
}

fn compatible_build() -> bool {
    let Ok(key) = RegKey::predef(HKEY_LOCAL_MACHINE)
        .open_subkey(r"SOFTWARE\Microsoft\Windows NT\CurrentVersion")
    else {
        return false;
    };
    let build = key
        .get_value::<String, _>("CurrentBuildNumber")
        .ok()
        .and_then(|value| value.parse::<u32>().ok())
        .unwrap_or(0);
    let update = key.get_value::<u32, _>("UBR").unwrap_or(0);
    (build, update) >= (26100, 8039)
}

fn developer_mode() -> bool {
    RegKey::predef(HKEY_LOCAL_MACHINE)
        .open_subkey(r"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock")
        .and_then(|key| key.get_value::<u32, _>("AllowDevelopmentWithoutDevLicense"))
        .is_ok_and(|enabled| enabled == 1)
}

const HOME_KEY: &str = r"Software\Microsoft\Windows\CurrentVersion\GamingConfiguration";
const HOME_VALUE: &str = "GamingHomeApp";
const BACKUP_KEY: &str = r"Software\lippdev\ConsoleMode2Alpha";
const BACKUP_VALUE: &str = "PreviousGamingHome";

// A missing registry value and an existing empty string must round-trip separately.
fn encode_previous(previous: Option<&str>) -> String {
    previous.map_or_else(|| "-".to_owned(), |value| format!("+{value}"))
}

fn decode_previous(encoded: &str) -> Result<Option<&str>, String> {
    if encoded == "-" { return Ok(None); }
    encoded.strip_prefix('+').map(Some).ok_or_else(|| "CM2_FSE_INVALID_BACKUP".into())
}

fn saved_home() -> Result<Option<String>, String> {
    match RegKey::predef(HKEY_CURRENT_USER).open_subkey(BACKUP_KEY)
        .and_then(|key| key.get_value(BACKUP_VALUE)) {
        Ok(value) => Ok(Some(value)),
        Err(error) if error.kind() == ErrorKind::NotFound => Ok(None),
        Err(error) => Err(error.to_string()),
    }
}

pub fn select_home() -> Result<(), String> {
    with_mta(|| {
        if !registered_here_in_mta()? { return Err("CM2_FSE_REGISTER_THIS_COPY".into()); }
        let previous = selected_home().map_err(|e| e.to_string())?;
        if previous.as_deref().is_some_and(|value| value.eq_ignore_ascii_case(AUMID)) { return Ok(()); }
        let user = RegKey::predef(HKEY_CURRENT_USER);
        let (backup, _) = user.create_subkey(BACKUP_KEY).map_err(|e| e.to_string())?;
        backup.set_value(BACKUP_VALUE, &encode_previous(previous.as_deref())).map_err(|e| e.to_string())?;
        let (config, _) = user.create_subkey(HOME_KEY).map_err(|e| e.to_string())?;
        config.set_value(HOME_VALUE, &AUMID).map_err(|e| e.to_string())?;
        // Desktop builds can replace this value asynchronously as soon as the write returns.
        // Check again on the deployment worker before confirming selection to the user.
        std::thread::sleep(std::time::Duration::from_millis(600));
        if home_selected()? { return Ok(()); }
        if selected_home().map_err(|e| e.to_string())? == previous {
            backup.delete_value(BACKUP_VALUE).map_err(|e| e.to_string())?;
        }
        Err("CM2_FSE_HOME_SELECTION_CHANGED".into())
    })
}

pub fn restore_home() -> Result<(), String> {
    if !home_selected()? { return Err("CM2_FSE_HOME_SELECTION_CHANGED".into()); }
    let saved = saved_home()?.ok_or("CM2_FSE_NO_PREVIOUS_HOME")?;
    let previous = decode_previous(&saved)?;
    restore_session_home(previous).map_err(|e| e.to_string())?;
    if selected_home().map_err(|e| e.to_string())?.as_deref() != previous {
        return Err("CM2_FSE_HOME_SELECTION_CHANGED".into());
    }
    RegKey::predef(HKEY_CURRENT_USER).open_subkey_with_flags(BACKUP_KEY, KEY_WRITE)
        .and_then(|key| key.delete_value(BACKUP_VALUE)).map_err(|e| e.to_string())
}

pub fn selected_home() -> std::io::Result<Option<String>> {
    let result = RegKey::predef(HKEY_CURRENT_USER).open_subkey(HOME_KEY)
        .and_then(|key| key.get_value(HOME_VALUE));
    match result {
        Ok(selection) => Ok(Some(selection)),
        Err(error) if error.kind() == ErrorKind::NotFound => Ok(None),
        Err(error) => Err(error),
    }
}

fn home_selected() -> Result<bool, String> {
    selected_home().map(|selection| selection.is_some_and(|s| s.eq_ignore_ascii_case(AUMID)))
        .map_err(|error| error.to_string())
}

pub fn restore_session_home(previous: Option<&str>) -> std::io::Result<()> {
    // Never overwrite a different Home set while the launcher was running.
    if !selected_home()?.is_some_and(|s| s.eq_ignore_ascii_case(AUMID)) { return Ok(()); }
    let key = RegKey::predef(HKEY_CURRENT_USER).open_subkey_with_flags(HOME_KEY, KEY_READ | KEY_WRITE)?;
    if let Some(previous) = previous { key.set_value(HOME_VALUE, &previous) }
    else { key.delete_value(HOME_VALUE) }
}

fn running_as_home() -> Result<bool, String> {
    const NO_PACKAGE: HRESULT = HRESULT::from_win32(15700);
    match Package::Current() {
        Ok(package) => package
            .Id()
            .and_then(|id| id.FamilyName())
            .map(|family| family == FAMILY)
            .map_err(|e| e.to_string()),
        Err(e) if e.code() == NO_PACKAGE => Ok(false),
        Err(e) => Err(e.to_string()),
    }
}

fn packages(manager: &PackageManager) -> Result<Vec<Package>, String> {
    // An empty SID identifies the current user.
    let found = manager
        .FindPackagesByUserSecurityIdNamePublisher(
            &HSTRING::new(),
            &HSTRING::from(NAME),
            &HSTRING::from(PUBLISHER),
        )
        .map_err(|e| e.to_string())?;
    let iter = found.First().map_err(|e| e.to_string())?;
    let mut result = Vec::new();
    while iter.HasCurrent().map_err(|e| e.to_string())? {
        result.push(iter.Current().map_err(|e| e.to_string())?);
        iter.MoveNext().map_err(|e| e.to_string())?;
    }
    Ok(result)
}

pub fn status() -> Result<Status, String> { with_mta(status_in_mta) }

fn status_in_mta() -> Result<Status, String> {
    let manager = PackageManager::new().map_err(|e| e.to_string())?;
    let selected = home_selected()?;
    Ok(Status {
        compatible_build: compatible_build(),
        developer_mode: developer_mode(),
        package_files: package_files(&install_root()?),
        registered: !packages(&manager)?.is_empty(),
        selected,
        can_restore_home: selected && saved_home()?.is_some_and(|value| decode_previous(&value).is_ok()),
        running_as_home: running_as_home()?,
    })
}

fn path_uri(path: &Path) -> windows::core::Result<Uri> {
    Uri::CreateUri(&HSTRING::from(path.as_os_str()))
}

pub fn register() -> Result<(), String> { with_mta(register_in_mta) }

fn register_in_mta() -> Result<(), String> {
    if !compatible_build() {
        return Err("CM2_FSE_UNSUPPORTED_BUILD".to_owned());
    }
    if !developer_mode() {
        return Err("CM2_FSE_DEVELOPER_MODE_REQUIRED".to_owned());
    }
    let root = install_root()?;
    if !package_files(&root) {
        return Err("CM2_FSE_PACKAGE_FILES_MISSING".to_owned());
    }

    let manager = PackageManager::new().map_err(|e| e.to_string())?;
    match packages(&manager)?.as_slice() {
        [] => {}
        [package] if installed_at(package, &root) => return Ok(()),
        _ => return Err("CM2_FSE_REGISTERED_AT_ANOTHER_LOCATION".to_owned()),
    }

    let deployment = (|| -> windows::core::Result<_> {
        let options = RegisterPackageOptions::new()?;
        options.SetDeveloperMode(true)?;
        options.SetExternalLocationUri(&path_uri(&root)?)?;
        manager
            .RegisterPackageByUriAsync(&path_uri(&root.join(MANIFEST))?, &options)?
            .join()
    })()
    .map_err(|e| e.to_string())?;
    if deployment.IsRegistered().map_err(|e| e.to_string())? && !packages(&manager)?.is_empty() {
        Ok(())
    } else {
        let detail = deployment
            .ErrorText()
            .map(|t| t.to_string())
            .unwrap_or_default();
        Err(if detail.is_empty() {
            "CM2_FSE_REGISTRATION_INCOMPLETE".to_owned()
        } else {
            detail
        })
    }
}

fn installed_at(package: &Package, root: &Path) -> bool {
    let installed = package.InstalledPath();
    installed.is_ok_and(|path| {
        let installed = std::fs::canonicalize(PathBuf::from(path.to_os_string()));
        let root = std::fs::canonicalize(root);
        matches!((installed, root), (Ok(installed), Ok(root)) if installed == root)
    })
}

pub fn unregister() -> Result<(), String> { with_mta(unregister_in_mta) }

fn unregister_in_mta() -> Result<(), String> {
    if running_as_home()? {
        return Err("CM2_FSE_CLOSE_HOME_BEFORE_REMOVAL".to_owned());
    }
    let manager = PackageManager::new().map_err(|e| e.to_string())?;
    let installed = packages(&manager)?;
    if !installed.is_empty() && home_selected()? {
        return Err(HOME_SELECTED.to_owned());
    }
    for package in installed {
        let full_name = package
            .Id()
            .and_then(|id| id.FullName())
            .map_err(|e| e.to_string())?;
        manager
            .RemovePackageAsync(&full_name)
            .and_then(|job| job.join())
            .map_err(|e| e.to_string())?;
    }
    if packages(&manager)?.is_empty() {
        Ok(())
    } else {
        Err("CM2_FSE_REMOVAL_INCOMPLETE".to_owned())
    }
}

pub fn open_settings() -> bool {
    crate::launch::open("ms-settings:gaming-fullscreen")
}

fn registered_here() -> Result<bool, String> { with_mta(registered_here_in_mta) }

fn registered_here_in_mta() -> Result<bool, String> {
    let root = install_root()?;
    let manager = PackageManager::new().map_err(|e| e.to_string())?;
    Ok(packages(&manager)?
        .iter()
        .any(|package| installed_at(package, &root)))
}

/// Commands for local staging tests and installer maintenance.
pub fn cli_exit_code(arg: &str) -> Option<i32> {
    let result = match arg {
        "--fse-status" => status().map(|s| println!("{s:?}")),
        "--fse-register" => register(),
        "--fse-select" => select_home(),
        "--fse-restore-home" => restore_home(),
        "--fse-remove" => unregister(),
        "--fse-uninstall-check" => match registered_here() {
            Ok(registered) => return Some(if registered { 2 } else { 0 }),
            Err(e) => Err(e),
        },
        _ => return None,
    };
    Some(match result {
        Ok(()) => 0,
        Err(e) => {
            eprintln!("{e}");
            if e == HOME_SELECTED { 2 } else { 1 }
        }
    })
}

#[cfg(test)]
mod tests {
    #[test]
    fn sta_callers_dispatch_deployment_to_an_mta() {
        use windows::Win32::System::WinRT::RO_INIT_SINGLETHREADED;
        std::thread::spawn(|| {
            unsafe { super::RoInitialize(RO_INIT_SINGLETHREADED) }.unwrap();
            let _apartment = super::Apartment;
            let current = std::thread::current().id();
            let worker = super::with_mta(|| Ok(std::thread::current().id())).unwrap();
            assert_ne!(current, worker);
        }).join().unwrap();
    }

    use super::*;

    #[test]
    fn previous_home_round_trips_including_missing_and_empty() {
        for previous in [None, Some(""), Some("Xbox!App"), Some("+")] {
            assert_eq!(decode_previous(&encode_previous(previous)).unwrap(), previous);
        }
        assert!(decode_previous("invalid").is_err());
    }

    #[test]
    fn package_and_side_by_side_identities_match() {
        let appx = include_str!("../../../build/FseHome/AppxManifest.xml");
        let sxs = include_str!("../../../build/FseHome/ConsoleMode2.manifest");
        let sccd = include_str!("../../../build/FseHome/ConsoleMode2Home.Dev.sccd");
        let (family, app) = AUMID.split_once('!').unwrap();
        assert!(appx.contains(&format!("Name=\"{NAME}\" Publisher=\"{PUBLISHER}\"")));
        assert!(appx.contains(&format!("<Application Id=\"{app}\"")));
        assert!(sxs.contains(&format!("packageName=\"{NAME}\"")));
        assert!(sxs.contains(&format!(
            "publisher=\"{PUBLISHER}\" applicationId=\"{app}\""
        )));
        assert!(sccd.contains(&format!("AppPackageFamilyName=\"{family}\"")));
    }
}
