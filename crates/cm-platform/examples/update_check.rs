// Read-only: asks GitHub for alpha releases above a version (default 2.0.0-alpha.0). Downloads nothing.
fn main() {
    let current = std::env::args().nth(1).unwrap_or_else(|| "2.0.0-alpha.0".into());
    println!("{:?}", cm_platform::update::check(&current));
}
