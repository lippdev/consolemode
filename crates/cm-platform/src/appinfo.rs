//! Steam's `appcache/appinfo.vdf` (binary, versions 28 and 29): only what the Home needs, each
//! app's `common/type` ("Game", "Tool", "Application", …), so tools like Wallpaper Engine or
//! Lossless Scaling don't show up as games.

use std::collections::HashMap;

const V28: u32 = 0x0756_4428;
const V29: u32 = 0x0756_4429;

/// app id → type, lower-case ("game", "tool", "application", "demo", …).
pub fn app_types(bytes: &[u8]) -> HashMap<u32, String> {
    let mut out = HashMap::new();
    let mut r = Reader { b: bytes, i: 0 };
    let Some(magic) = r.u32() else { return out };
    if magic != V28 && magic != V29 { return out; }
    r.u32(); // universe
    let strings = if magic == V29 {
        let Some(off) = r.u64() else { return out };
        string_table(bytes, off as usize)
    } else {
        Vec::new()
    };
    let v29 = magic == V29;
    loop {
        let Some(app_id) = r.u32() else { break };
        if app_id == 0 { break; }
        let Some(size) = r.u32() else { break };
        let end = r.i + size as usize;
        if end > bytes.len() { break; }
        // info state, last updated, PICS token, sha1, change number, binary sha1
        r.i += 4 + 4 + 8 + 20 + 4 + 20;
        let mut kv = Reader { b: &bytes[..end], i: r.i };
        if let Some(t) = find_type(&mut kv, &strings, v29) { out.insert(app_id, t.to_ascii_lowercase()); }
        r.i = end;
    }
    out
}

fn string_table(b: &[u8], off: usize) -> Vec<String> {
    let mut r = Reader { b, i: off };
    let Some(n) = r.u32() else { return Vec::new() };
    (0..n).map_while(|_| r.cstr().map(str::to_string)).collect()
}

/// Walks the app's key/values and returns appinfo → common → type.
fn find_type(r: &mut Reader, strings: &[String], v29: bool) -> Option<String> {
    fn walk(r: &mut Reader, s: &[String], v29: bool, depth: usize, in_common: bool) -> Option<Option<String>> {
        let mut found = None;
        loop {
            let t = r.u8()?;
            if t == 0x08 { return Some(found); }
            let key = if v29 { s.get(r.u32()? as usize)?.as_str().to_string() } else { r.cstr()?.to_string() };
            match t {
                0x00 => {
                    let common = depth == 1 && key.eq_ignore_ascii_case("common");
                    let inner = walk(r, s, v29, depth + 1, common)?;
                    if found.is_none() { found = inner; }
                }
                0x01 => { let v = r.cstr()?; if in_common && key.eq_ignore_ascii_case("type") { found = Some(v.to_string()); } }
                0x02 | 0x03 | 0x04 | 0x06 => { r.u32()?; }
                0x07 | 0x0A => { r.u64()?; }
                0x05 => { r.wstr()?; }
                _ => return None,
            }
        }
    }
    walk(r, strings, v29, 0, false).flatten()
}

struct Reader<'a> { b: &'a [u8], i: usize }

impl<'a> Reader<'a> {
    fn take(&mut self, n: usize) -> Option<&'a [u8]> { let s = self.b.get(self.i..self.i + n)?; self.i += n; Some(s) }
    fn u8(&mut self) -> Option<u8> { Some(self.take(1)?[0]) }
    fn u32(&mut self) -> Option<u32> { Some(u32::from_le_bytes(self.take(4)?.try_into().ok()?)) }
    fn u64(&mut self) -> Option<u64> { Some(u64::from_le_bytes(self.take(8)?.try_into().ok()?)) }
    fn cstr(&mut self) -> Option<&'a str> {
        let rest = self.b.get(self.i..)?;
        let n = rest.iter().position(|&c| c == 0)?;
        self.i += n + 1;
        std::str::from_utf8(&rest[..n]).ok().or(Some(""))
    }
    fn wstr(&mut self) -> Option<()> {
        loop { let c = self.take(2)?; if c == [0, 0] { return Some(()); } }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    /// A tiny v28 file with one app: appinfo { common { type "Game" } }.
    #[test]
    fn reads_type_v28() {
        let mut kv = vec![0x00]; kv.extend(b"appinfo\0");
        kv.push(0x00); kv.extend(b"common\0");
        kv.push(0x01); kv.extend(b"name\0Sekiro\0");
        kv.push(0x01); kv.extend(b"type\0Game\0");
        kv.extend([0x08, 0x08, 0x08]);
        let mut f = Vec::new();
        f.extend(V28.to_le_bytes()); f.extend(1u32.to_le_bytes());
        f.extend(814380u32.to_le_bytes()); f.extend(((60 + kv.len()) as u32).to_le_bytes());
        f.extend([0u8; 60]); f.extend(&kv);
        f.extend(0u32.to_le_bytes());
        assert_eq!(app_types(&f).get(&814380).map(String::as_str), Some("game"));
    }
}
