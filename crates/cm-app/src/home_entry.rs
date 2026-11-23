//! Windows activates this packaged entry point to open the desktop Alpha as its Home.
#![windows_subsystem = "windows"]

use std::{
    io,
    os::windows::process::CommandExt,
    process::{Command, ExitCode},
};

const CREATE_NO_WINDOW: u32 = 0x0800_0000;

fn open_home() -> io::Result<()> {
    let desktop = std::env::current_exe()?.with_file_name("ConsoleMode2.exe");
    Command::new(desktop)
        .arg("--gaming-home")
        .creation_flags(CREATE_NO_WINDOW)
        .spawn()?;
    Ok(())
}

fn main() -> ExitCode {
    if open_home().is_ok() {
        ExitCode::SUCCESS
    } else {
        ExitCode::FAILURE
    }
}
