//! A tiny blocking HTTPS GET over WinHTTP: system TLS and system proxy, no extra crates.
//! Used only by the update check and download (docs/v2/RELEASE.md); call it off the UI thread.

use std::{ffi::c_void, fs::File, io::Write, path::Path, ptr};

use windows::{
    Win32::Networking::WinHttp::*,
    core::{HSTRING, PCWSTR, w},
};

/// A WinHTTP handle, closed on drop.
struct Handle(*mut c_void);
impl Handle {
    fn new(raw: *mut c_void, what: &str) -> Result<Self, String> {
        if raw.is_null() { Err(format!("{what}: {}", windows::core::Error::from_thread())) } else { Ok(Self(raw)) }
    }
}
impl Drop for Handle {
    fn drop(&mut self) { unsafe { let _ = WinHttpCloseHandle(self.0); } }
}

/// An answered request: status 200 and ready to read. Field order = close order (request first).
struct Response {
    request: Handle,
    _connect: Handle,
    _session: Handle,
    length: Option<u64>,
}

/// `https://host[:port]/path?query` → (host, port, "/path?query"). Only https is accepted.
pub fn split_url(url: &str) -> Option<(&str, u16, &str)> {
    let rest = url.strip_prefix("https://")?;
    let (authority, path) = match rest.find('/') { Some(i) => (&rest[..i], &rest[i..]), None => (rest, "/") };
    let (host, port) = match authority.rsplit_once(':') { Some((h, p)) => (h, p.parse().ok()?), None => (authority, 443) };
    (!host.is_empty() && !host.contains('@')).then_some((host, port, path))
}

fn open(url: &str, user_agent: &str, accept: &str) -> Result<Response, String> {
    let (host, port, path) = split_url(url).ok_or_else(|| format!("endereço não permitido: {url}"))?;
    unsafe {
        let session = Handle::new(WinHttpOpen(&HSTRING::from(user_agent), WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, PCWSTR::null(), PCWSTR::null(), 0), "WinHttpOpen")?;
        // resolve, connect, send, receive (ms): a download that stalls for 30 s gives up
        WinHttpSetTimeouts(session.0, 15_000, 15_000, 30_000, 30_000).map_err(|e| e.to_string())?;
        let connect = Handle::new(WinHttpConnect(session.0, &HSTRING::from(host), port, 0), "WinHttpConnect")?;
        let request = Handle::new(
            WinHttpOpenRequest(connect.0, w!("GET"), &HSTRING::from(path), PCWSTR::null(), PCWSTR::null(), ptr::null(), WINHTTP_FLAG_SECURE),
            "WinHttpOpenRequest",
        )?;
        let headers: Vec<u16> = format!("Accept: {accept}\r\n").encode_utf16().collect();
        WinHttpSendRequest(request.0, Some(&headers), None, 0, 0, 0).map_err(|e| format!("envio: {e}"))?;
        WinHttpReceiveResponse(request.0, ptr::null_mut()).map_err(|e| format!("resposta: {e}"))?;

        let mut status = 0u32;
        let mut size = size_of::<u32>() as u32;
        WinHttpQueryHeaders(request.0, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, PCWSTR::null(), Some(&mut status as *mut u32 as *mut c_void), &mut size, &mut 0)
            .map_err(|e| format!("status: {e}"))?;
        if status != 200 { return Err(format!("HTTP {status}")); }
        let mut len = 0u32;
        let mut size = size_of::<u32>() as u32;
        let length = WinHttpQueryHeaders(request.0, WINHTTP_QUERY_CONTENT_LENGTH | WINHTTP_QUERY_FLAG_NUMBER, PCWSTR::null(), Some(&mut len as *mut u32 as *mut c_void), &mut size, &mut 0)
            .ok().map(|_| len as u64);
        Ok(Response { request, _connect: connect, _session: session, length })
    }
}

impl Response {
    /// Next chunk into `buf`; 0 = end.
    fn read(&self, buf: &mut [u8]) -> Result<usize, String> {
        let mut n = 0u32;
        unsafe { WinHttpReadData(self.request.0, buf.as_mut_ptr() as *mut c_void, buf.len() as u32, &mut n) }.map_err(|e| format!("leitura: {e}"))?;
        Ok(n as usize)
    }
}

/// The body of a GET, at most `max` bytes.
pub fn get(url: &str, user_agent: &str, accept: &str, max: usize) -> Result<Vec<u8>, String> {
    let r = open(url, user_agent, accept)?;
    let mut body = Vec::new();
    let mut buf = vec![0u8; 16 * 1024];
    loop {
        let n = r.read(&mut buf)?;
        if n == 0 { return Ok(body); }
        if body.len() + n > max { return Err("resposta grande demais".into()); }
        body.extend_from_slice(&buf[..n]);
    }
}

/// Streams a GET into `dest`, at most `max` bytes; `progress(done, total)` after every chunk.
pub fn download(url: &str, user_agent: &str, dest: &Path, max: u64, progress: &mut dyn FnMut(u64, Option<u64>)) -> Result<(), String> {
    let r = open(url, user_agent, "application/octet-stream")?;
    if r.length.is_some_and(|l| l > max) { return Err("arquivo grande demais".into()); }
    let mut out = File::create(dest).map_err(|e| format!("criar {}: {e}", dest.display()))?;
    let mut buf = vec![0u8; 64 * 1024];
    let mut done = 0u64;
    loop {
        let n = r.read(&mut buf)?;
        if n == 0 { break; }
        done += n as u64;
        if done > max { return Err("arquivo grande demais".into()); }
        out.write_all(&buf[..n]).map_err(|e| format!("gravar: {e}"))?;
        progress(done, r.length);
    }
    out.flush().map_err(|e| format!("gravar: {e}"))?;
    if r.length.is_some_and(|l| l != done) { return Err("download incompleto".into()); }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::split_url;

    #[test]
    fn splits_https_urls() {
        assert_eq!(split_url("https://api.github.com/repos/a/b/releases?per_page=30"), Some(("api.github.com", 443, "/repos/a/b/releases?per_page=30")));
        assert_eq!(split_url("https://host:8443/x"), Some(("host", 8443, "/x")));
        assert_eq!(split_url("https://host"), Some(("host", 443, "/")));
        assert_eq!(split_url("http://github.com/x"), None);
        assert_eq!(split_url("https://user@host/x"), None);
        assert_eq!(split_url("ftp://x"), None);
    }
}
