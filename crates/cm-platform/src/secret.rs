//! Secrets kept on disk (the TV's ADB key), encrypted for the signed-in Windows user with DPAPI,
//! like 1.7 SecretProtector: `dpapi:` + base64. The data folder may travel (a copied folder, a
//! portable build), and whoever has the key controls the paired TV. DPAPI data opens only for the
//! same Windows user on the same PC; anything else comes back as unreadable so the caller makes a new one.

use cm_core::tv::{base64_decode, base64_encode};
use windows::Win32::{
    Foundation::{HLOCAL, LocalFree},
    Security::Cryptography::{CRYPT_INTEGER_BLOB, CRYPTPROTECT_UI_FORBIDDEN, CryptProtectData, CryptUnprotectData},
};
use windows::core::w;

pub const PREFIX: &str = "dpapi:";

pub fn is_protected(stored: &str) -> bool { stored.starts_with(PREFIX) }

fn take(out: CRYPT_INTEGER_BLOB) -> Vec<u8> {
    let v = unsafe { std::slice::from_raw_parts(out.pbData, out.cbData as usize) }.to_vec();
    unsafe { LocalFree(Some(HLOCAL(out.pbData as _))) };
    v
}

pub fn protect(secret: &[u8]) -> Result<String, String> {
    let input = CRYPT_INTEGER_BLOB { cbData: secret.len() as u32, pbData: secret.as_ptr() as *mut u8 };
    let mut out = CRYPT_INTEGER_BLOB::default();
    unsafe { CryptProtectData(&input, w!("Console Mode"), None, None, None, CRYPTPROTECT_UI_FORBIDDEN, &mut out) }.map_err(|e| format!("DPAPI: {e}"))?;
    Ok(format!("{PREFIX}{}", base64_encode(&take(out))))
}

/// None: protected here by someone else (another Windows user or PC), or damaged.
pub fn unprotect(stored: &str) -> Option<Vec<u8>> {
    let mut data = base64_decode(stored.trim().strip_prefix(PREFIX)?)?;
    let input = CRYPT_INTEGER_BLOB { cbData: data.len() as u32, pbData: data.as_mut_ptr() };
    let mut out = CRYPT_INTEGER_BLOB::default();
    unsafe { CryptUnprotectData(&input, None, None, None, None, CRYPTPROTECT_UI_FORBIDDEN, &mut out) }.ok()?;
    Some(take(out))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_secret_round_trips_for_this_user_and_is_not_plain_text() {
        let stored = protect(b"segredo").unwrap();
        assert!(is_protected(&stored) && !stored.contains("segredo"));
        assert_eq!(unprotect(&stored).as_deref(), Some(&b"segredo"[..]));
        assert_eq!(unprotect("dpapi:AAAA"), None, "damaged");
        assert_eq!(unprotect("plain"), None);
    }
}
