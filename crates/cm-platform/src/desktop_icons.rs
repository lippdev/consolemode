//! Reads and moves the desktop icons through the shell's own view of the desktop (IShellWindows →
//! the desktop's IShellBrowser → IFolderView): the documented route, instead of poking at
//! Explorer's list view from another process (ported from 1.7's DesktopIcons, issue #30).
//! Each call runs on its own STA thread: the shell objects live in Explorer and want one.
//! The rules for what to put back are in cm_core::desktop_icons.

use std::{sync::mpsc, time::Duration};

use cm_core::desktop_icons::DesktopIcon;
use windows::{
    Win32::{
        Foundation::POINT,
        System::{
            Com::{CLSCTX_ALL, CoCreateInstance, CoInitializeEx, CoTaskMemFree, CoUninitialize, COINIT_APARTMENTTHREADED, IServiceProvider},
            Variant::VARIANT,
        },
        UI::Shell::{
            Common::{ITEMIDLIST, STRRET},
            IFolderView, IFolderView2, IShellBrowser, IShellFolder, IShellWindows, SHGDN_FORPARSING, SID_STopLevelBrowser, ShellWindows, StrRetToStrW, SVGIO_ALLVIEW,
            SVSI_POSITIONITEM, SWC_DESKTOP, SWFO_NEEDDISPATCH,
        },
    },
    core::{Interface, PWSTR},
};

/// Every icon and its position. Err (Portuguese, for the log) when they can't be read or Windows
/// arranges them itself (then there is nothing to put back).
pub fn read() -> Result<Vec<DesktopIcon>, String> {
    on_sta(|| {
        let desktop = DesktopView::open()?;
        if desktop.auto_arrange() { return Err("organização automática ligada; posições não salvas".into()); }
        let mut icons = Vec::new();
        for (name, pidl) in desktop.items() {
            if let Ok(p) = unsafe { desktop.view.GetItemPosition(pidl) } { icons.push(DesktopIcon { name, x: p.x, y: p.y }); }
        }
        Ok(icons)
    })
}

/// Puts the given icons where they were. Returns how many moved.
pub fn move_icons(icons: &[DesktopIcon]) -> Result<usize, String> {
    if icons.is_empty() { return Ok(0); }
    let wanted: Vec<DesktopIcon> = icons.to_vec();
    on_sta(move || {
        let desktop = DesktopView::open()?;
        if desktop.auto_arrange() { return Err("organização automática ligada".into()); }
        let mut moved = 0;
        for (name, pidl) in desktop.items() {
            let Some(icon) = wanted.iter().find(|i| i.name.eq_ignore_ascii_case(&name)) else { continue };
            let point = POINT { x: icon.x, y: icon.y };
            let list = [pidl];
            if unsafe { desktop.view.SelectAndPositionItems(1, list.as_ptr(), Some(&point), SVSI_POSITIONITEM.0 as u32) }.is_ok() { moved += 1; }
        }
        Ok(moved)
    })
}

/// FWF_NOICONS: the switch behind the desktop's View → Show desktop icons.
const FWF_NOICONS: u32 = 0x1000;

/// Whether the desktop icons are showing now (changes nothing; 1.7 `DesktopIcons.IsShowing`).
pub fn showing() -> Result<bool, String> {
    on_sta(|| Ok(DesktopView::open()?.flags()? & FWF_NOICONS == 0))
}

/// Hides or shows the desktop icons (1.7 `DesktopIcons.SetHidden`, issue #210). Ok(true) when it
/// changed them, Ok(false) when they already were that way.
pub fn set_hidden(hide: bool) -> Result<bool, String> {
    on_sta(move || {
        let desktop = DesktopView::open()?;
        if (desktop.flags()? & FWF_NOICONS != 0) == hide { return Ok(false); }
        let view: IFolderView2 = desktop.view.cast().map_err(|e| format!("IFolderView2: {e}"))?;
        unsafe { view.SetCurrentFolderFlags(FWF_NOICONS, if hide { FWF_NOICONS } else { 0 }) }.map_err(|e| format!("SetCurrentFolderFlags: {e}"))?;
        Ok(true)
    })
}

fn on_sta<T: Send + 'static>(work: impl FnOnce() -> Result<T, String> + Send + 'static) -> Result<T, String> {
    let (tx, rx) = mpsc::channel();
    std::thread::Builder::new().name("desktop-icons".into()).spawn(move || {
        let init = unsafe { CoInitializeEx(None, COINIT_APARTMENTTHREADED) };
        let result = work();
        if init.is_ok() { unsafe { CoUninitialize(); } }
        let _ = tx.send(result);
    }).map_err(|e| e.to_string())?;
    rx.recv_timeout(Duration::from_secs(10)).unwrap_or_else(|_| Err("o Explorer não respondeu".into()))
}

/// The desktop's folder view and folder; frees every item id it hands out.
struct DesktopView {
    view: IFolderView,
    folder: IShellFolder,
    pidls: std::cell::RefCell<Vec<*mut ITEMIDLIST>>,
}

impl DesktopView {
    fn open() -> Result<DesktopView, String> {
        let e = |what: &str| { let what = what.to_string(); move |err: windows::core::Error| format!("{what}: {err}") };
        unsafe {
            let windows_: IShellWindows = CoCreateInstance(&ShellWindows, None, CLSCTX_ALL).map_err(e("ShellWindows"))?;
            let location = VARIANT::from(0i32); // CSIDL_DESKTOP
            let root = VARIANT::default();
            let mut hwnd = 0i32;
            let dispatch = windows_.FindWindowSW(&location, &root, SWC_DESKTOP, &mut hwnd, SWFO_NEEDDISPATCH)
                .map_err(|_| "a área de trabalho do Explorer não foi encontrada".to_string())?;
            let provider: IServiceProvider = dispatch.cast().map_err(e("IServiceProvider"))?;
            let browser: IShellBrowser = provider.QueryService(&SID_STopLevelBrowser).map_err(e("IShellBrowser"))?;
            let shell_view = browser.QueryActiveShellView().map_err(e("QueryActiveShellView"))?;
            let view: IFolderView = shell_view.cast().map_err(e("IFolderView"))?;
            let folder: IShellFolder = view.GetFolder().map_err(e("GetFolder"))?;
            Ok(DesktopView { view, folder, pidls: Default::default() })
        }
    }

    fn flags(&self) -> Result<u32, String> {
        let view: IFolderView2 = self.view.cast().map_err(|e| format!("IFolderView2: {e}"))?;
        unsafe { view.GetCurrentFolderFlags() }.map_err(|e| format!("GetCurrentFolderFlags: {e}"))
    }

    /// GetAutoArrange answers S_OK (0) when the icons are arranged automatically and S_FALSE (1)
    /// when not; the wrapper turns both into Ok, so the raw result is read.
    fn auto_arrange(&self) -> bool {
        unsafe { (Interface::vtable(&self.view).GetAutoArrange)(Interface::as_raw(&self.view)).0 == 0 }
    }

    /// Each icon's parsing name with its item id (valid until drop).
    fn items(&self) -> Vec<(String, *const ITEMIDLIST)> {
        let mut out = Vec::new();
        let Ok(count) = (unsafe { self.view.ItemCount(SVGIO_ALLVIEW) }) else { return out };
        for i in 0..count {
            let Ok(pidl) = (unsafe { self.view.Item(i) }) else { continue };
            if pidl.is_null() { continue; }
            self.pidls.borrow_mut().push(pidl);
            if let Some(name) = self.parsing_name(pidl).filter(|n| !n.trim().is_empty()) { out.push((name, pidl as *const ITEMIDLIST)); }
        }
        out
    }

    fn parsing_name(&self, pidl: *const ITEMIDLIST) -> Option<String> {
        unsafe {
            let mut strret = STRRET::default();
            self.folder.GetDisplayNameOf(pidl, SHGDN_FORPARSING, &mut strret).ok()?;
            let mut text = PWSTR::null();
            StrRetToStrW(&mut strret, Some(pidl), &mut text).ok()?;
            let name = text.to_string().ok();
            CoTaskMemFree(Some(text.0 as *const _));
            name
        }
    }
}

impl Drop for DesktopView {
    fn drop(&mut self) {
        for pidl in self.pidls.borrow_mut().drain(..) { unsafe { CoTaskMemFree(Some(pidl as *const _)); } }
    }
}
