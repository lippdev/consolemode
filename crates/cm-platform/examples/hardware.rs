#[cfg(windows)]
fn main() -> windows::core::Result<()> {
    let displays = cm_platform::displays::list_displays()?;
    println!("Displays: {}", displays.len());
    for d in displays {
        println!("{d:#?}");
    }
    let outputs = cm_platform::audio::list_outputs()?;
    println!("Audio outputs: {}", outputs.len());
    for output in outputs {
        println!("{output:#?}");
    }
    Ok(())
}
#[cfg(not(windows))]
fn main() {
    eprintln!("Hardware enumeration requires Windows.");
}
