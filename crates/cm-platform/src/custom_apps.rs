//! Native picker and version-resource metadata. Call the picker on a dedicated STA thread.
use windows::{core::{HSTRING, w}, Win32::{System::Com::*, UI::Shell::*, Storage::FileSystem::*}};

pub fn owner_handle(window: &gpui::Window) -> isize {
    use raw_window_handle::{HasWindowHandle, RawWindowHandle};
    match HasWindowHandle::window_handle(window).ok().map(|h| h.as_raw()) { Some(RawWindowHandle::Win32(h)) => h.hwnd.get(), _ => 0 }
}

pub fn pick_exe(owner: isize) -> Result<Option<String>, String> {
    unsafe {
        CoInitializeEx(None, COINIT_APARTMENTTHREADED).ok().map_err(|e| e.to_string())?;
        let result = pick(owner);
        CoUninitialize();
        result.map_err(|e| e.to_string())
    }
}

unsafe fn pick(owner: isize) -> windows::core::Result<Option<String>> {
    unsafe {
        let dialog: IFileOpenDialog = CoCreateInstance(&FileOpenDialog, None, CLSCTX_INPROC_SERVER)?;
        dialog.SetTitle(w!("Adicionar app"))?;
        dialog.SetOptions(FOS_FILEMUSTEXIST | FOS_PATHMUSTEXIST | FOS_FORCEFILESYSTEM | FOS_NOCHANGEDIR)?;
        dialog.SetFileTypes(&[Common::COMDLG_FILTERSPEC { pszName: w!("Aplicativos (*.exe)"), pszSpec: w!("*.exe") }])?;
        if let Err(e) = dialog.Show(Some(windows::Win32::Foundation::HWND(owner as *mut _))) {
            if e.code().0 == 0x800704c7u32 as i32 { return Ok(None); }
            return Err(e);
        }
        let path = dialog.GetResult()?.GetDisplayName(SIGDN_FILESYSPATH)?;
        let result = path.to_string();
        CoTaskMemFree(Some(path.0.cast()));
        Ok(Some(result?))
    }
}

pub fn file_description(path: &str) -> String {
    unsafe {
        let path = HSTRING::from(path);
        let size = GetFileVersionInfoSizeW(&path, None);
        if size == 0 { return String::new(); }
        let mut data = vec![0u8; size as usize];
        if GetFileVersionInfoW(&path, None, size, data.as_mut_ptr().cast()).is_err() { return String::new(); }
        let mut ptr = std::ptr::null_mut();
        let mut len = 0;
        if !VerQueryValueW(data.as_ptr().cast(), w!("\\VarFileInfo\\Translation"), &mut ptr, &mut len).as_bool() { return String::new(); }
        let translations = std::slice::from_raw_parts(ptr.cast::<u16>(), len as usize / 2).to_vec();
        for pair in translations.chunks_exact(2) {
            let key = HSTRING::from(format!("\\StringFileInfo\\{:04x}{:04x}\\FileDescription", pair[0], pair[1]));
            if VerQueryValueW(data.as_ptr().cast(), &key, &mut ptr, &mut len).as_bool() && len > 1 {
                let text = String::from_utf16_lossy(std::slice::from_raw_parts(ptr.cast::<u16>(), len as usize - 1));
                if !text.trim().is_empty() { return text; }
            }
        }
        String::new()
    }
}
