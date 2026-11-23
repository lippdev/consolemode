//! Reads and turns on "Allow this device to wake the computer" for the controller's adapters (1.7
//! ControllerWakeService). Reading needs no rights; turning it on runs an elevated PowerShell, so
//! Windows asks first (the UAC prompt), exactly as 1.7 does. The classification, the scripts and the
//! checks are `cm_core::controller_wake`. Both calls block: run them off the UI thread.

use std::{
    io::Read,
    os::windows::process::CommandExt,
    process::{Command, Stdio},
    time::{Duration, Instant},
};

use cm_core::controller_wake::{self as wake, WakeDevice};
use windows::{
    Win32::{
        Foundation::{CloseHandle, WAIT_OBJECT_0},
        System::Threading::{GetExitCodeProcess, TerminateProcess, WaitForSingleObject},
        UI::{
            Shell::{SEE_MASK_NOCLOSEPROCESS, SHELLEXECUTEINFOW, ShellExecuteExW},
            WindowsAndMessaging::SW_HIDE,
        },
    },
    core::{PCWSTR, w},
};

const CREATE_NO_WINDOW: u32 = 0x0800_0000;
/// HRESULT_FROM_WIN32(ERROR_CANCELLED): the user said no to the UAC prompt.
const CANCELLED: u32 = 0x8007_04C7;

/// The controller-related devices that can wake the PC, or None when they couldn't be read.
pub fn read() -> Option<Vec<WakeDevice>> {
    let mut child = Command::new("powershell.exe")
        .args(["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", &wake::encode(wake::QUERY_SCRIPT)])
        .stdin(Stdio::null()).stdout(Stdio::piped()).stderr(Stdio::null()).creation_flags(CREATE_NO_WINDOW)
        .spawn().ok()?;
    let mut out = child.stdout.take()?;
    let reader = std::thread::spawn(move || { let mut bytes = Vec::new(); let _ = out.read_to_end(&mut bytes); bytes });
    let end = Instant::now() + Duration::from_secs(20);
    loop {
        match child.try_wait() {
            Ok(Some(_)) => break,
            Ok(None) if Instant::now() < end => std::thread::sleep(Duration::from_millis(60)),
            _ => { let _ = child.kill(); return None; }
        }
    }
    let text = String::from_utf8_lossy(&reader.join().ok()?).into_owned();
    Some(wake::relevant(wake::parse(&text)))
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Enabled { Done, Canceled, Failed, NotFound }

/// Lets the given devices wake the PC. Shows the UAC prompt; blocks until PowerShell exits.
pub fn enable(devices: &[WakeDevice]) -> (Enabled, String) {
    if devices.is_empty() { return (Enabled::Done, "nada a ativar".into()); }
    let script = wake::enable_script(devices.iter().map(|d| d.instance_name.as_str()));
    let params: Vec<u16> = format!("-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand {}", wake::encode(&script)).encode_utf16().chain(Some(0)).collect();
    let mut info = SHELLEXECUTEINFOW {
        cbSize: std::mem::size_of::<SHELLEXECUTEINFOW>() as u32,
        fMask: SEE_MASK_NOCLOSEPROCESS,
        lpVerb: w!("runas"),
        lpFile: w!("powershell.exe"),
        lpParameters: PCWSTR(params.as_ptr()),
        nShow: SW_HIDE.0,
        ..Default::default()
    };
    unsafe {
        if let Err(e) = ShellExecuteExW(&mut info as *mut SHELLEXECUTEINFOW) {
            return if e.code().0 as u32 == CANCELLED { (Enabled::Canceled, "permissão de administrador recusada".into()) } else { (Enabled::Failed, format!("{e}")) };
        }
        let process = info.hProcess;
        if process.is_invalid() { return (Enabled::Failed, "o PowerShell não abriu".into()); }
        if WaitForSingleObject(process, 30_000) != WAIT_OBJECT_0 {
            let _ = TerminateProcess(process, 1);
            let _ = CloseHandle(process);
            return (Enabled::Failed, "o PowerShell não terminou em 30 s".into());
        }
        let mut code = 0u32;
        let _ = GetExitCodeProcess(process, &mut code);
        let _ = CloseHandle(process);
        let result = match code as i32 { 0 => Enabled::Done, wake::NOT_FOUND_EXIT => Enabled::NotFound, _ => Enabled::Failed };
        (result, format!("{} => {code}", wake::names(devices)))
    }
}
