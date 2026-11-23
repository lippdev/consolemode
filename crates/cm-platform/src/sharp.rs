//! Icons drawn at the size they are shown. The GPU samples a 256 px icon shown at 70 px with no
//! mipmaps, so its edges come out jagged; scaling it down first, with a good filter and with the
//! colors weighted by alpha (so edges do not darken), gives the smooth edge of a native icon.

use gpui::RenderImage;
use image::{Frame, RgbaImage, imageops::FilterType};

/// `src` (B,G,R,A, straight alpha) scaled to `px` × `px`, or None when it is not bigger.
pub fn downscale(src: &RenderImage, px: u32) -> Option<RenderImage> {
    let size = src.size(0);
    let (w, h) = (size.width.0 as u32, size.height.0 as u32);
    if px == 0 || (px >= w && px >= h) { return None; }
    let mut buf = RgbaImage::from_raw(w, h, src.as_bytes(0)?.to_vec())?;
    for p in buf.pixels_mut() { let a = p[3] as u32; for c in 0..3 { p[c] = ((p[c] as u32 * a + 127) / 255) as u8; } }
    let (tw, th) = if w >= h { (px, (px * h / w).max(1)) } else { ((px * w / h).max(1), px) };
    let mut out = image::imageops::resize(&buf, tw, th, FilterType::CatmullRom);
    for p in out.pixels_mut() { let a = p[3] as u32; if a > 0 { for c in 0..3 { p[c] = ((p[c] as u32 * 255 + a / 2) / a).min(255) as u8; } } }
    Some(RenderImage::new(vec![Frame::new(out)]))
}



#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn an_icon_is_shrunk_keeping_its_shape_and_its_edge_does_not_darken() {
        // 256 × 128, white, the right half transparent
        let mut buf = RgbaImage::new(256, 128);
        for (x, _, p) in buf.enumerate_pixels_mut() { *p = if x < 128 { image::Rgba([255, 255, 255, 255]) } else { image::Rgba([0, 0, 0, 0]) }; }
        let src = RenderImage::new(vec![Frame::new(buf)]);
        let out = downscale(&src, 64).unwrap();
        let s = out.size(0);
        assert_eq!((s.width.0, s.height.0), (64, 32));
        // every pixel with any coverage is still white
        for p in out.as_bytes(0).unwrap().chunks_exact(4) { if p[3] > 8 { assert!(p[0] > 240 && p[1] > 240 && p[2] > 240, "{p:?}"); } }
        assert!(downscale(&src, 300).is_none());
    }
}
