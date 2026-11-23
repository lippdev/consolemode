//! A small notice in the top right corner of the game screen, over the game (1.7 SessionHintWindow):
//! a dark card with an icon, a title and a line of text that slides in from the screen's edge, stays
//! while a thin bar runs out, and slides back. It never takes the focus from the game (no
//! activation, tool window) and closes by itself. Used for the low-battery warning.
//!
//! Drawn with GDI on its own thread: the app's GPUI window is out of the way during a session, and
//! a second GPUI window would only be there for this card.

use std::time::{Duration, Instant};

use windows::{
    Win32::{
        Foundation::{COLORREF, HWND, LPARAM, LRESULT, RECT, WPARAM},
        Graphics::Gdi::{
            BeginPaint, CLEARTYPE_QUALITY, CreateFontW, CreateRoundRectRgn, CreateSolidBrush, DT_END_ELLIPSIS, DT_LEFT, DT_NOPREFIX, DT_SINGLELINE, DT_VCENTER, DT_WORDBREAK,
            DeleteObject, DrawTextW, SetWindowRgn, EndPaint, FF_DONTCARE, FW_NORMAL, FW_SEMIBOLD, FillRect, PAINTSTRUCT, RoundRect, SelectObject, SetBkMode, SetTextColor, TRANSPARENT,
            CreatePen, PS_SOLID, GetStockObject, NULL_BRUSH, InvalidateRect,
        },
        System::LibraryLoader::GetModuleHandleW,
        UI::WindowsAndMessaging::{
            CS_HREDRAW, CS_VREDRAW, CreateWindowExW, DefWindowProcW, DestroyWindow, DispatchMessageW, GWLP_USERDATA, GetMessageW, GetWindowLongPtrW, KillTimer, LWA_ALPHA, MSG,
            PostQuitMessage, RegisterClassW, SW_SHOWNOACTIVATE, SetLayeredWindowAttributes, SetTimer, SetWindowLongPtrW, SetWindowPos, ShowWindow, TranslateMessage,
            WM_DESTROY, WM_ERASEBKGND, WM_NCCREATE, WM_PAINT, WM_TIMER, WNDCLASSW, WS_EX_LAYERED, WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW, WS_EX_TOPMOST, WS_POPUP, HWND_TOPMOST,
            SWP_NOACTIVATE, SWP_NOSIZE, CREATESTRUCTW,
        },
    },
    core::{PCWSTR, w},
};

/// The design size, in units of a screen 1080 tall (everything scales with the screen).
const CARD_W: f32 = 470.;
const CARD_H: f32 = 96.;
const MARGIN: f32 = 28.;
const ENTER: Duration = Duration::from_millis(560);
const LEAVE: Duration = Duration::from_millis(420);
const TIMER: usize = 1;

/// What the card says and how long it stays.
pub struct Hint {
    /// A Segoe Fluent Icons glyph ("\u{e853}").
    pub glyph: String,
    pub title: String,
    pub body: String,
    pub seconds: f32,
    /// The bar's color, 0xRRGGBB.
    pub accent: u32,
    /// With Windows' animations off: no slide, the card just appears and goes.
    pub still: bool,
}

struct State {
    hint: Hint,
    scale: f32,
    start: Instant,
    /// Final position (left, top) and the distance it slides.
    at: (i32, i32),
    travel: i32,
    size: (i32, i32),
}

fn wide(s: &str) -> Vec<u16> { s.encode_utf16().chain(Some(0)).collect() }
fn rgb(c: u32) -> COLORREF { COLORREF(((c & 0xff) << 16) | (c & 0xff00) | ((c >> 16) & 0xff)) }

/// Shows the card on the screen whose rectangle (left, top, right, bottom, in pixels) is given.
/// Returns at once; the card runs on its own thread.
pub fn show(rect: (i32, i32, i32, i32), hint: Hint) {
    let _ = std::thread::Builder::new().name("hint".into()).spawn(move || unsafe { run(rect, hint) });
}

unsafe fn run(rect: (i32, i32, i32, i32), hint: Hint) {
    let height = (rect.3 - rect.1).max(480) as f32;
    let scale = height / 1080.;
    let (cw, ch) = ((CARD_W * scale) as i32, (CARD_H * scale) as i32);
    let margin = (MARGIN * scale) as i32;
    let at = (rect.2 - margin - cw, rect.1 + margin);
    let travel = if hint.still { 0 } else { cw + margin };
    let state = Box::new(State { hint, scale, start: Instant::now(), at, travel, size: (cw, ch) });
    let class = w!("ConsoleMode2Hint");
    unsafe {
        let hinst = GetModuleHandleW(None).unwrap_or_default();
        let wc = WNDCLASSW { style: CS_HREDRAW | CS_VREDRAW, lpfnWndProc: Some(proc), hInstance: hinst.into(), lpszClassName: class, ..Default::default() };
        RegisterClassW(&wc); // already registered the second time: fine
        let raw = Box::into_raw(state);
        let Ok(hwnd) = CreateWindowExW(
            WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED,
            class, PCWSTR::null(), WS_POPUP, at.0 + travel, at.1, cw, ch, None, None, Some(hinst.into()), Some(raw as *const _),
        ) else { drop(Box::from_raw(raw)); return };
        let _ = SetWindowRgn(hwnd, Some(CreateRoundRectRgn(0, 0, cw + 1, ch + 1, (28. * scale) as i32, (28. * scale) as i32)), false);
        let _ = SetLayeredWindowAttributes(hwnd, COLORREF(0), 0, LWA_ALPHA);
        let _ = ShowWindow(hwnd, SW_SHOWNOACTIVATE);
        SetTimer(Some(hwnd), TIMER, 16, None);
        let mut msg = MSG::default();
        while GetMessageW(&mut msg, None, 0, 0).as_bool() { let _ = TranslateMessage(&msg); DispatchMessageW(&msg); }
    }
}

fn ease_out(t: f32) -> f32 { 1. - (1. - t.clamp(0., 1.)).powi(3) }

unsafe extern "system" fn proc(hwnd: HWND, msg: u32, wparam: WPARAM, lparam: LPARAM) -> LRESULT {
    unsafe {
        if msg == WM_NCCREATE {
            let cs = &*(lparam.0 as *const CREATESTRUCTW);
            SetWindowLongPtrW(hwnd, GWLP_USERDATA, cs.lpCreateParams as isize);
            return DefWindowProcW(hwnd, msg, wparam, lparam);
        }
        let p = GetWindowLongPtrW(hwnd, GWLP_USERDATA) as *mut State;
        if p.is_null() { return DefWindowProcW(hwnd, msg, wparam, lparam); }
        let st = &mut *p;
        match msg {
            WM_TIMER => {
                let t = st.start.elapsed();
                let stay = Duration::from_secs_f32(st.hint.seconds);
                let total = ENTER + stay + LEAVE;
                if t >= total { let _ = KillTimer(Some(hwnd), TIMER); let _ = DestroyWindow(hwnd); return LRESULT(0); }
                // 0 = out of sight, 1 = in place
                let shown = if t < ENTER { ease_out(t.as_secs_f32() / ENTER.as_secs_f32()) }
                    else if t < ENTER + stay { 1. }
                    else { 1. - ease_out((t - ENTER - stay).as_secs_f32() / LEAVE.as_secs_f32()) };
                let x = st.at.0 + ((1. - shown) * st.travel as f32) as i32;
                let _ = SetWindowPos(hwnd, Some(HWND_TOPMOST), x, st.at.1, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
                let _ = SetLayeredWindowAttributes(hwnd, COLORREF(0), (shown * 255.) as u8, LWA_ALPHA);
                // the bar runs out while it stays
                let _ = InvalidateRect(Some(hwnd), None, false);
                LRESULT(0)
            }
            WM_ERASEBKGND => LRESULT(1),
            WM_PAINT => { paint(hwnd, st); LRESULT(0) }
            WM_DESTROY => {
                SetWindowLongPtrW(hwnd, GWLP_USERDATA, 0);
                drop(Box::from_raw(p));
                PostQuitMessage(0);
                LRESULT(0)
            }
            _ => DefWindowProcW(hwnd, msg, wparam, lparam),
        }
    }
}

unsafe fn font(height: i32, weight: i32, face: &str) -> windows::Win32::Graphics::Gdi::HFONT {
    let name = wide(face);
    unsafe { CreateFontW(-height, 0, 0, 0, weight, 0, 0, 0, windows::Win32::Graphics::Gdi::FONT_CHARSET(1), windows::Win32::Graphics::Gdi::FONT_OUTPUT_PRECISION(0), windows::Win32::Graphics::Gdi::FONT_CLIP_PRECISION(0), CLEARTYPE_QUALITY, FF_DONTCARE.0 as u32, PCWSTR(name.as_ptr())) }
}

unsafe fn paint(hwnd: HWND, st: &mut State) {
    unsafe {
        let mut ps = PAINTSTRUCT::default();
        let dc = BeginPaint(hwnd, &mut ps);
        let (w, h) = st.size;
        let u = st.scale;
        let n = |v: f32| (v * u) as i32;
        // the card: dark, with a faint border
        let bg = CreateSolidBrush(rgb(0x0f1418));
        FillRect(dc, &RECT { left: 0, top: 0, right: w, bottom: h }, bg);
        let _ = DeleteObject(bg.into());
        let pen = CreatePen(PS_SOLID, 1.max(n(1.)), rgb(0x3a4046));
        let old_pen = SelectObject(dc, pen.into());
        let old_brush = SelectObject(dc, GetStockObject(NULL_BRUSH));
        let _ = RoundRect(dc, 0, 0, w, h, n(28.), n(28.));
        SelectObject(dc, old_pen);
        SelectObject(dc, old_brush);
        let _ = DeleteObject(pen.into());
        SetBkMode(dc, TRANSPARENT);

        // icon
        let icon_font = font(n(30.), FW_NORMAL.0 as i32, "Segoe Fluent Icons");
        let old = SelectObject(dc, icon_font.into());
        SetTextColor(dc, rgb(0xf4f6f9));
        let mut glyph = wide(&st.hint.glyph);
        let n_glyph = glyph.len() - 1;
        let mut icon_box = RECT { left: n(20.), top: 0, right: n(20.) + n(44.), bottom: h };
        DrawTextW(dc, &mut glyph[..n_glyph], &mut icon_box, DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_NOPREFIX);
        SelectObject(dc, old);
        let _ = DeleteObject(icon_font.into());

        // title and body
        let left = n(20. + 44. + 16.);
        let title_font = font(n(17.), FW_SEMIBOLD.0 as i32, "Segoe UI Variable Text");
        let old = SelectObject(dc, title_font.into());
        SetTextColor(dc, rgb(0xf4f6f9));
        let mut title = wide(&st.hint.title);
        let n_title = title.len() - 1;
        let mut tb = RECT { left, top: n(14.), right: w - n(20.), bottom: n(40.) };
        DrawTextW(dc, &mut title[..n_title], &mut tb, DT_LEFT | DT_SINGLELINE | DT_END_ELLIPSIS | DT_NOPREFIX);
        SelectObject(dc, old);
        let _ = DeleteObject(title_font.into());
        let body_font = font(n(15.), FW_NORMAL.0 as i32, "Segoe UI Variable Text");
        let old = SelectObject(dc, body_font.into());
        SetTextColor(dc, rgb(0xc2c6cd));
        let mut body = wide(&st.hint.body);
        let n_body = body.len() - 1;
        let mut bb = RECT { left, top: n(42.), right: w - n(20.), bottom: h - n(12.) };
        DrawTextW(dc, &mut body[..n_body], &mut bb, DT_LEFT | DT_WORDBREAK | DT_END_ELLIPSIS | DT_NOPREFIX);
        SelectObject(dc, old);
        let _ = DeleteObject(body_font.into());

        // how long it stays: a thin bar that runs out
        let stay = st.hint.seconds;
        let into = (st.start.elapsed().as_secs_f32() - ENTER.as_secs_f32()).clamp(0., stay);
        let left_over = 1. - into / stay.max(0.1);
        let bar = CreateSolidBrush(rgb(st.hint.accent));
        let bar_w = ((w - n(40.)) as f32 * left_over) as i32;
        FillRect(dc, &RECT { left: n(20.), top: h - n(10.), right: n(20.) + bar_w, bottom: h - n(7.) }, bar);
        let _ = DeleteObject(bar.into());
        let _ = EndPaint(hwnd, &ps);
    }
}
