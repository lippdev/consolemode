// Embeds the exe icon and version info (Windows only). The icon is the 1.7 logo as a multi-size .ico.
fn main() {
    println!("cargo:rerun-if-changed=assets/ConsoleMode2.ico");
    println!("cargo:rerun-if-changed=build.rs");
    if std::env::var("CARGO_CFG_TARGET_OS").as_deref() != Ok("windows") { return; }
    let mut res = winresource::WindowsResource::new();
    res.set_icon("assets/ConsoleMode2.ico");
    // FILEVERSION/PRODUCTVERSION come from the package version; these are the texts Explorer shows.
    res.set("ProductName", "Console Mode 2 Alpha");
    res.set("FileDescription", "Console Mode 2 Alpha");
    res.set("InternalName", "ConsoleMode2");
    res.set("OriginalFilename", "ConsoleMode2.exe");
    res.set("CompanyName", "lippdev");
    res.set("LegalCopyright", "AGPL-3.0-only, lippdev");
    res.compile().expect("could not embed the exe icon and version info");
}
