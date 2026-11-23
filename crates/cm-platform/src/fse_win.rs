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
    enums::{HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE},
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
    pub running_as_home: bool,
}

// Public calls run on a worker thread or before the UI initializes COM.
struct Apartment;

impl Apartment {
    fn enter() -> Result<Self, String> {
        unsafe { RoInitialize(RO_INIT_MULTITHREADED) }.map_err(|e| e.to_string())?;
        Ok(Self)
    }
}

impl Drop for Apartment {
    fn drop(&mut self) {
        unsafe { RoUninitialize() }
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

fn home_selected() -> Result<bool, String> {
    let selection = RegKey::predef(HKEY_CURRENT_USER)
        .open_subkey(r"Software\Microsoft\Windows\CurrentVersion\GamingConfiguration")
        .and_then(|key| key.get_value::<String, _>("GamingHomeApp"));
    match selection {
        Ok(selection) => Ok(selection.eq_ignore_ascii_case(AUMID)),
        Err(e) if e.kind() == ErrorKind::NotFound => Ok(false),
        Err(e) => Err(e.to_string()),
    }
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

pub fn status() -> Result<Status, String> {
    let _apartment = Apartment::enter()?;
    let manager = PackageManager::new().map_err(|e| e.to_string())?;
    Ok(Status {
        compatible_build: compatible_build(),
        developer_mode: developer_mode(),
        package_files: package_files(&install_root()?),
        registered: !packages(&manager)?.is_empty(),
        selected: home_selected()?,
        running_as_home: running_as_home()?,
    })
}

fn path_uri(path: &Path) -> windows::core::Result<Uri> {
    Uri::CreateUri(&HSTRING::from(path.as_os_str()))
}

pub fn register() -> Result<(), String> {
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

    let _apartment = Apartment::enter()?;
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

pub fn unregister() -> Result<(), String> {
    let _apartment = Apartment::enter()?;
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

fn registered_here() -> Result<bool, String> {
    let root = install_root()?;
    let _apartment = Apartment::enter()?;
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
    use super::*;

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
