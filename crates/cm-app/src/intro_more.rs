//! More openings in the mood of the consoles (asked for on 2026-11-17): PS1, PS2, PS3, the first
//! Xbox and the Xbox One. Like the PS4 and Xbox 360 ones they borrow the mood only (light, motion,
//! colour), never a console's logo or name, and they end on Console Mode's own logo.

use std::f32::consts::TAU;

use gpui::{App, Bounds, BorderStyle, Corners, Edges, FontWeight, Window, linear_color_stop, linear_gradient, point, px, quad, size, transparent_black};

use super::*;

/// The logo and the name under it, centred on the screen (the block of own() and ps4()).
fn logo_block(s: &Scene, logo: f32, name: f32, gap: f32) -> (f32, f32, f32) {
    let block = logo + gap + name;
    let (lx, ly) = (s.cx(), s.cy() - block / 2. + logo / 2.);
    (lx, ly, ly + logo / 2. + gap)
}


// ── in the mood of the PS1: the screen climbs from black through grey to white; the logo pops in
// big and flat in a warm orange-to-yellow gradient, its screen's prompt cut in a moment later, the
// name fades in under it in deep blue, and it all just stands there with the sound. Then a hard
// cut to black, and the logo again, smaller, in red, yellow and teal, with the name in white.

/// The cut to black, after the sound has had its say (it rings out at ≈6 s).
pub(super) const PS1_CUT: f32 = 5300.;
/// The logo pops in as the screen reaches white, the prompt a moment later, then the name.
const PS1_POP: f32 = 950.;
const PS1_CUT_IN: f32 = 1450.;
const PS1_NAME: f32 = 1850.;

pub(super) fn ps1(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let h = s.h;
    if ms < PS1_CUT {
        // black → grey → white
        s.full(window, c(mix(0x000000, 0xfcfcfc, seg(ms, 0., 900.)), 1.));
        if ms < PS1_POP { return; }
        let (lx, ly) = (s.cx(), s.oy + h * 0.44);
        let logo = Logo::centred(lx, ly, h * 0.4);
        // the first frames are lighter, then the gradient settles
        let shine = 1. - ease_out(seg(ms, PS1_POP, 220.));
        let (yellow, orange) = (mix(0xfdd10f, 0xfff6c8, shine), mix(0xef5a1c, 0xf9b234, shine));
        let warm = linear_gradient(135., linear_color_stop(c(yellow, 1.), 0.), linear_color_stop(c(orange, 1.), 1.));
        let cut = linear_gradient(315., linear_color_stop(c(0xfdd10f, 1.), 0.), linear_color_stop(c(0xe8461b, 1.), 1.));
        let cut_a = if ms >= PS1_CUT_IN { 1. } else { 0. };
        draw_logo_with(window, &logo, warm, c(0xfcfcfc, cut_a).into(), cut.opacity(cut_a));
        let na = ease_out(seg(ms, PS1_NAME, 350.));
        text(window, cx, "Console Mode", h * 0.062, FontWeight::SEMIBOLD, c(0x1b3d93, na), lx, s.oy + h * 0.69, true);
    } else {
        // the hard cut, then the logo again, small, on black
        s.full(window, c(0x000000, 1.));
        let a = ease_out(seg(ms, PS1_CUT + 300., 250.));
        let (lx, ly) = (s.cx(), s.oy + h * 0.4);
        draw_logo(window, &Logo::centred(lx, ly, h * 0.3), Ink { body: 0xe1261c, detail: 0x0f0f0f, prompt: 0xf6b400 }, a);
        // a teal line under the screen, like the third colour of the console's mark
        let l = Logo::centred(lx, ly, h * 0.3);
        stroke(window, &l.pts(&[(30., 214.), (50., 214.)]), 12. * l.k, c(0x1ba39c, a));
        text(window, cx, "Console Mode", h * 0.055, FontWeight::NORMAL, c(0xf2f2f2, a), lx, s.oy + h * 0.575, true);
    }
}

// ── in the mood of the PS2: a deep blue void; towers of glass cubes rise out of the fog, block by
// block, while the camera turns slowly around them, small cubes drifting up; a flash of light on
// the sound's hit; the towers sink into the fog and the logo glows in.

/// The PS2 camera: it circles the field, looking down at it.
struct Cam { th: f32, dist: f32, hc: f32, pitch: f32, f: f32, cx: f32, cy: f32 }

impl Cam {
    /// World → screen, and the depth (None behind the camera).
    fn project(&self, x: f32, y: f32, z: f32) -> Option<(f32, f32, f32)> {
        let (s, c) = self.th.sin_cos();
        let xr = x * c - z * s;
        let zr = x * s + z * c + self.dist;
        let yr = y - self.hc;
        let (ps, pc) = self.pitch.sin_cos();
        let zz = zr * pc - yr * ps;
        let yy = yr * pc + zr * ps;
        if zz < 0.3 { return None; }
        Some((self.cx + xr * self.f / zz, self.cy - yy * self.f / zz, zz))
    }
    fn eye(&self) -> (f32, f32, f32) { (-self.dist * self.th.sin(), self.hc, -self.dist * self.th.cos()) }
}

/// One glass cube: the faces that face the camera, translucent, with lit edges.
fn glass_cube(window: &mut Window, cam: &Cam, x: f32, y: f32, z: f32, half: f32, a: f32, top_glow: f32, u: f32) {
    if a <= 0.01 { return; }
    let eye = cam.eye();
    let corner = |dx: f32, dy: f32, dz: f32| cam.project(x + dx * half, y + dy * half, z + dz * half);
    // faces: normal, then its four corners (in units of half)
    let faces: [((f32, f32, f32), [(f32, f32, f32); 4], f32); 5] = [
        ((0., 1., 0.), [(-1., 1., -1.), (1., 1., -1.), (1., 1., 1.), (-1., 1., 1.)], 0.30),
        ((1., 0., 0.), [(1., -1., -1.), (1., 1., -1.), (1., 1., 1.), (1., -1., 1.)], 0.16),
        ((-1., 0., 0.), [(-1., -1., -1.), (-1., 1., -1.), (-1., 1., 1.), (-1., -1., 1.)], 0.16),
        ((0., 0., 1.), [(-1., -1., 1.), (1., -1., 1.), (1., 1., 1.), (-1., 1., 1.)], 0.11),
        ((0., 0., -1.), [(-1., -1., -1.), (1., -1., -1.), (1., 1., -1.), (-1., 1., -1.)], 0.11),
    ];
    for (n, pts, shade) in faces {
        let fc = (x + n.0 * half, y + n.1 * half, z + n.2 * half);
        if n.0 * (eye.0 - fc.0) + n.1 * (eye.1 - fc.1) + n.2 * (eye.2 - fc.2) <= 0. { continue; }
        let Some(poly) = pts.iter().map(|p| corner(p.0, p.1, p.2).map(|q| (q.0, q.1))).collect::<Option<Vec<_>>>() else { return };
        let face_a = a * (shade + if n.1 > 0. { 0.35 * top_glow } else { 0. });
        fill(window, &poly, c(0xb8d2ff, face_a));
        let mut edge = poly.clone();
        edge.push(poly[0]);
        stroke(window, &edge, 1.1 * u, c(0xe6f0ff, a * 0.55));
    }
}

pub(super) fn ps2(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let (u, t) = (s.u, ms / 1000.);
    // the void: black above, a band of deep blue fog where the field fades out, dark below
    window.paint_quad(quad(Bounds::new(point(px(s.ox), px(s.oy)), size(px(s.w), px(s.h))), Corners::all(px(0.)),
        linear_gradient(180., linear_color_stop(c(0x000105, 1.), 0.), linear_color_stop(c(0x04102c, 1.), 1.)), Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
    blob(window, s.cx(), s.oy + s.h * 0.36, s.w * 0.45, c(0x123a8c, 0.45), s.w * 0.3);

    let sink = ease_in_out(seg(ms, 2900., 1100.));
    let cam = Cam { th: 0.55 + t * 0.16, dist: 15., hc: 7.5 - 1.5 * ease_out(seg(ms, 0., 4000.)), pitch: 0.36, f: s.h * 1.05, cx: s.cx(), cy: s.oy + s.h * 0.42 };
    // the field: a grid of towers, each so many cubes high, built cube by cube
    const N: i32 = 4;
    let (half, step) = (0.3, 0.7);
    let mut towers = Vec::new();
    let mut r = Rand(2);
    for gx in -N..=N { for gz in -N..=N {
        let (hgt, delay) = (1 + (r.next() * r.next() * 12.) as usize, r.next());
        let (x, z) = (gx as f32 * 2.2, gz as f32 * 2.2);
        let Some((_, _, depth)) = cam.project(x, 0., z) else { continue };
        towers.push((depth, x, z, hgt, delay));
    } }
    towers.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    let field_a = seg(ms, 150., 700.) * (1. - 0.8 * sink);
    for (depth, x, z, hgt, delay) in towers {
        let fog = (1. - (depth - 8.) / 18.).clamp(0., 1.).powf(1.3);
        let a = field_a * fog;
        if a < 0.02 { continue; }
        // built from the ground up, one cube after the other
        let built = ((ms - 300. - delay * 900.) / 110.).max(0.);
        let shown = (built as usize).min(hgt);
        for k in 0..shown {
            let pop = if k + 1 == shown && (built as usize) < hgt { built.fract() } else { 1. };
            let y = half + k as f32 * step - (1. - ease_out(pop)) * 0.5;
            let top = k + 1 == hgt;
            glass_cube(window, &cam, x, y, z, half, a * ease_out(pop), if top { 1. } else { 0. }, u);
            if top && pop >= 1. {
                if let Some((px_, py_, d)) = cam.project(x, y + half, z) { blob(window, px_, py_, s.h * 0.05 / d * 6., c(0xcfe0ff, 0.35 * a), s.h * 0.06 / d * 6.); }
            }
        }
    }
    // small cubes drifting up through the void, turning
    let mut r = Rand(9);
    for _ in 0..40 {
        let (x0, y0, sp, sz, ph) = (r.next(), r.next(), 0.3 + r.next(), r.next(), r.next() * TAU);
        let x = s.ox + x0 * s.w + (t * 0.7 + ph).sin() * 10. * u;
        let y = s.oy + (y0 * s.h - t * 40. * u * sp).rem_euclid(s.h);
        let half_px = (2. + 5. * sz) * u;
        let ang = ph + t * (0.8 + sp);
        let sq: Vec<(f32, f32)> = (0..4).map(|i| { let a = ang + i as f32 * TAU / 4.; (x + half_px * a.cos(), y + half_px * 0.75 * a.sin()) }).collect();
        fill(window, &sq, c(0xbcd4ff, (0.12 + 0.3 * sz) * seg(ms, 300., 900.)));
    }
    // the hit of the sound: a light sweeping out from the middle
    let flash = bell(ms, 950., 900., 0.15);
    blob(window, s.cx(), s.oy + s.h * 0.4, s.h * lerp(0.08, 0.45, seg(ms, 950., 900.)), c(0xd9e6ff, 0.55 * flash), s.h * 0.25);
    // the logo glows in as the field sinks into the fog
    let (lx, ly, ny) = logo_block(s, 200. * u, 56. * u, 34. * u);
    let la = ease_out(seg(ms, 3100., 1000.));
    blob(window, lx, ly, 200. * u * 0.7, c(0x5d8fff, 0.35 * la), 200. * u * 0.6);
    draw_logo(window, &Logo::centred(lx, ly, 200. * u * lerp(1.06, 1., la)), Ink { body: 0xe9f0ff, detail: 0x0a1a46, prompt: 0x9ec3ff }, la);
    let na = ease_out(seg(ms, 3450., 900.));
    text(window, cx, "Console Mode", 56. * u, FontWeight::LIGHT, c(0xe9f0ff, na), lx, ny, true);
}

// ── in the mood of the PS3: on black, a wave of fine silk lines drifts across the screen, twisting
// so its strands gather into bright pinches and fan out again; a deep purple haze rises behind it,
// and the logo and the name come in small, right of the middle, resting on the wave.

/// Where the wave's middle is, 0..1 down the screen, at `x` (0..1 across).
fn ps3_wave_y(x: f32, t: f32) -> f32 { 0.475 + 0.025 * (TAU * (0.6 * x - 0.05) + 0.35 * t).sin() + 0.2 * x * x - 0.04 * x }

/// The silk: many fine lines around the wave's middle, crossing over as the ribbon twists.
fn ps3_wave(window: &mut Window, s: &Scene, t: f32, a: f32) {
    if a <= 0.01 { return; }
    const LINES: usize = 34;
    const STEPS: usize = 96;
    let twist = |x: f32| TAU * (0.42 * x + 0.1) + 0.45 * t;
    let half = |x: f32| s.h * (0.055 + 0.022 * (TAU * (0.8 * x + 0.3) - 0.3 * t).sin());
    // the soft body of the ribbon and the haze around it
    let edge = |side: f32| -> Vec<(f32, f32)> { (0..=STEPS).map(|j| { let x = j as f32 / STEPS as f32; (s.ox + x * s.w, s.oy + s.h * ps3_wave_y(x, t) + side * half(x) * twist(x).cos().abs().max(0.25)) }).collect() };
    let mut body = edge(-1.);
    body.extend(edge(1.).into_iter().rev());
    fill(window, &body, c(0xb58cc4, 0.12 * a));
    // smoke: a few soft strands of light, blurred, brightest where the ribbon folds
    for k in 0..4 {
        let o = [-0.7, -0.2, 0.25, 0.65][k];
        for j in 0..=40 {
            let x = j as f32 / 40.;
            let tw = twist(x);
            let fold = 1. - tw.cos().abs();
            let y = s.oy + s.h * ps3_wave_y(x, t) + half(x) * (o * tw.cos() + 0.15 * (TAU * (1.1 * x + k as f32 * 0.3) + 0.6 * t).sin());
            blob(window, s.ox + x * s.w, y, s.h * 0.014, c(0xd8b8e4, (0.09 + 0.2 * fold) * a), s.h * 0.022);
        }
    }
    for j in 0..12 {
        let x = (j as f32 + 0.5) / 12.;
        let pinch = 1. - twist(x).cos().abs();
        blob(window, s.ox + x * s.w, s.oy + s.h * ps3_wave_y(x, t), half(x) * 0.8, c(0xd9bfe3, (0.05 + 0.1 * pinch) * a), s.h * 0.05);
    }
    for i in 0..LINES {
        let o = i as f32 / (LINES - 1) as f32 * 2. - 1.;
        let ph = i as f32 * 0.37;
        let pts: Vec<(f32, f32)> = (0..=STEPS).map(|j| {
            let x = j as f32 / STEPS as f32;
            let off = o * twist(x).cos() + 0.18 * (TAU * (1.3 * x + ph) + 0.7 * t).sin() * (1. - o.abs() * 0.5);
            (s.ox + x * s.w, s.oy + s.h * ps3_wave_y(x, t) + half(x) * off)
        }).collect();
        // the outer strands are fainter
        let la = (0.08 + 0.14 * (1. - o.abs())) * a;
        stroke(window, &pts, 1.1 * s.u, c(0xeedcf5, la));
    }
}

pub(super) fn ps3(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let (t, h) = (ms / 1000., s.h);
    s.full(window, c(0x000000, 1.));
    // the haze: up from black to its full purple by ≈4 s, then settling a little
    let glow = ease_in_out(seg(ms, 500., 3800.)) * (1. - 0.18 * ease_in_out(seg(ms, 5400., 900.)));
    s.full(window, c(0x4a2350, glow));
    blob(window, s.ox + s.w * 0.12, s.oy + h * 0.95, s.w * 0.35, c(0x8a5694, 0.9 * glow), s.w * 0.3);
    blob(window, s.ox + s.w * 0.88, s.oy + h * 0.1, s.w * 0.3, c(0x7a4a88, 0.75 * glow), s.w * 0.28);
    blob(window, s.ox + s.w * 0.5, s.oy + h * 0.45, s.w * 0.28, c(0x24092a, 0.75 * glow), s.w * 0.25);
    blob(window, s.ox + s.w * 0.95, s.oy + h * 0.95, s.w * 0.2, c(0x1c0620, 0.7 * glow), s.w * 0.2);
    // the wave comes first, dim on black
    ps3_wave(window, s, t, seg(ms, 150., 900.) * (0.55 + 0.45 * glow));
    // the logo and the name, small, resting on the wave right of the middle
    let a = ease_in_out(seg(ms, 600., 800.));
    let x0 = s.ox + s.w * 0.565;
    let wy = s.oy + h * ps3_wave_y(0.6, t);
    let lsize = h * 0.058;
    draw_logo(window, &Logo::centred(x0 + lsize / 2., wy - lsize * 0.12, lsize), Ink { body: 0xf6f2f7, detail: 0x2c1230, prompt: 0xf6f2f7 }, a);
    let size_px = h * 0.036;
    blob(window, x0 + lsize * 1.4 + size_px * 3., wy - size_px * 0.1, size_px * 2., c(0xffffff, 0.06 * a), size_px * 2.);
    text(window, cx, "Console Mode", size_px, FontWeight::NORMAL, c(0xf6f2f7, a), x0 + lsize * 1.35, wy - lsize * 0.12 - size_px * 0.62, false);
}

// ── in the mood of the first Xbox: in the dark, thick ribbons of green energy, like a glowing
// liquid, swirl in 3D around a black orb, faster and closer, until they plunge into it and it
// bursts; the logo glows green inside the orb and the name comes in under it, wide and green.

pub(super) const XBOX_BURST: f32 = 3300.;

/// One green ribbon: thick and wobbly, a white-hot core, drawn behind or in front of the orb.
fn goo(window: &mut Window, s: &Scene, rb: &Ribbon, ms: f32, alpha: f32, front: bool, orb_r: f32) {
    let tau = (ms / XBOX_BURST).clamp(0., 1.);
    let head = rb.phase + rb.speed * TAU * (0.35 * tau + 1.1 * tau.powf(2.2));
    let close = ease_in(seg(ms, 400., XBOX_BURST - 400.));
    let radius = s.h * lerp(0.5 * rb.radius, 0.0, close).max(0.) + orb_r * 0.9;
    let span = rb.span * lerp(1.15, 0.6, close);
    const N: usize = 30;
    let pts: Vec<(f32, f32, f32)> = (0..=N).map(|i| orbit(rb, head - span * (1. - i as f32 / N as f32), radius)).collect();
    let (cx, cy) = (s.cx(), s.cy() - s.h * 0.05);
    // each point: screen position, half width, strength; a liquid swells and thins along its
    // length, fattest near the head
    let at: Vec<(f32, f32, f32, f32, bool)> = pts.iter().enumerate().map(|(i, p)| {
        let t = i as f32 / N as f32;
        let wob = 0.75 + 0.25 * (t * 9. + ms / 160. + rb.phase).sin();
        let taper = (t.powf(0.9) * (1. - (t - 0.9).max(0.) * 8.) * wob).max(0.);
        let depth = 0.6 + 0.4 * (p.2 + 1.) / 2.;
        (cx + p.0, cy + p.1, rb.width * 1.8 * s.u * taper * depth, alpha * taper * depth, p.2 >= 0.)
    }).collect();
    // the runs of points on this side of the orb, each filled as one smooth band (no seams)
    let mut i = 0;
    while i < at.len() {
        if at[i].4 != front { i += 1; continue; }
        let start = i;
        while i < at.len() && at[i].4 == front { i += 1; }
        let run = &at[start.saturating_sub(1)..(i + 1).min(at.len())];
        if run.len() < 2 { continue; }
        let strength = run.iter().map(|p| p.3).fold(0., f32::max);
        for p in run.iter().step_by(2) { blob(window, p.0, p.1, p.2 * 2.2, c(0x39ff14, p.3 * 0.3), p.2 * 3.); }
        for (k, col, a) in [(1., 0x4fd61a, 0.85), (0.55, 0x9dff5c, 0.9), (0.18, 0xf2ffe8, 0.9)] {
            let band = |side: f32| -> Vec<(f32, f32)> {
                (0..run.len()).map(|j| {
                    let (p, q) = (run[j.saturating_sub(1)], run[(j + 1).min(run.len() - 1)]);
                    let (dx, dy) = (q.0 - p.0, q.1 - p.1);
                    let len = (dx * dx + dy * dy).sqrt().max(1e-3);
                    let w = run[j].2 * k * side;
                    (run[j].0 - dy / len * w, run[j].1 + dx / len * w)
                }).collect()
            };
            let mut poly = band(1.);
            poly.extend(band(-1.).into_iter().rev());
            fill(window, &poly, c(col, a * strength));
        }
    }
}

pub(super) fn xbox(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let (u, mx) = (s.u, s.cx());
    let my = s.cy() - s.h * 0.05;
    s.full(window, c(0x000000, 1.));
    // a dim green haze that thickens toward the burst
    let haze = seg(ms, 0., 1500.) * (0.6 + 0.4 * seg(ms, 1500., XBOX_BURST - 1500.));
    blob(window, mx, my, s.diag() * 0.33, c(0x0c3a06, 0.75 * haze), s.diag() * 0.28);
    // the orb: black glass, a green rim of light, growing as the ribbons close in
    let orb_in = ease_out(seg(ms, 600., XBOX_BURST - 600.));
    let d = s.h * 0.26 * lerp(0.55, 1., orb_in);
    let r = d / 2.;
    let rbs: Vec<Ribbon> = {
        let mut rr = Rand(2001);
        (0..7).map(|_| Ribbon { inc: 0.3 + rr.next() * 1.1, node: rr.next() * TAU, phase: rr.next() * TAU, span: 1.3 + rr.next() * 1.4, radius: 0.8 + rr.next() * 0.5, speed: 0.7 + rr.next() * 0.5, width: 12. + rr.next() * 12., silver: false }).collect()
    };
    let rib_a = seg(ms, 100., 800.) * (1. - seg(ms, XBOX_BURST - 120., 160.));
    if rib_a > 0. { for rb in &rbs { goo(window, s, rb, ms, rib_a, false, r); } }
    let orb_a = seg(ms, 500., 700.);
    blob(window, mx, my, r * 1.5, c(0x39ff14, 0.5 * orb_a * (0.5 + 0.5 * orb_in)), r * 0.8);
    disc(window, mx, my, r, c(0x020602, orb_a));
    disc(window, mx, my, r * 0.97, c(0x07140a, orb_a));
    // the light inside: a green glow from below, and the gloss on top
    blob(window, mx, my + r * 0.35, r * 0.6, c(0x2fbf12, 0.55 * orb_a), r * 0.45);
    blob(window, mx - r * 0.2, my - r * 0.55, r * 0.32, c(0xffffff, 0.22 * orb_a), r * 0.16);
    if rib_a > 0. { for rb in &rbs { goo(window, s, rb, ms, rib_a, true, r); } }
    // the burst
    let burst = bell(ms, XBOX_BURST - 120., 1000., 0.18);
    blob(window, mx, my, s.h * lerp(0.08, 0.75, seg(ms, XBOX_BURST - 120., 1000.)), c(0xb6ff8a, 0.9 * burst), s.h * 0.25);
    // after: the logo glowing green in the orb, the name under it, wide-set like the console's
    let la = ease_out(seg(ms, XBOX_BURST + 100., 700.));
    if la > 0. {
        blob(window, mx, my, r * 0.75, c(0x5cff22, 0.35 * la), r * 0.5);
        draw_logo(window, &Logo::centred(mx, my + r * 0.04, d * 0.56), Ink { body: 0x8dff4f, detail: 0x07140a, prompt: 0xeaffdc }, la);
        let na = ease_out(seg(ms, XBOX_BURST + 500., 700.));
        let name = "CONSOLE MODE";
        let size_px = 46. * u;
        let track = 14. * u;
        let widths: Vec<f32> = name.chars().map(|ch| text_width(window, &ch.to_string(), size_px, FontWeight::BOLD)).collect();
        let total: f32 = widths.iter().sum::<f32>() + track * (widths.len() as f32 - 1.);
        let mut x = mx - total / 2.;
        let y = my + r + 40. * u;
        blob(window, mx, y + size_px * 0.6, total * 0.45, c(0x39ff14, 0.18 * na), total * 0.3);
        for (ch, w) in name.chars().zip(widths) {
            text(window, cx, &ch.to_string(), size_px, FontWeight::BOLD, c(0x9bf00b, na), x, y, false);
            x += w + track;
        }
    }
}

// ── in the mood of the Xbox One: on black, a ring of white light draws itself around the centre,
// a green glow behind it; the logo comes into the ring and the name under it.

// ── in the mood of the Xbox One: black, then the camera pulls back from a white sphere so close
// it fills the screen; it shrinks onto a green room, rolls across the floor line uncovering the
// name, turns to show the logo on its face and rises into place above it.

/// The sound's whoosh (≈0.9 s): the sphere pulls back with it.
pub(super) const XONE_IN: f32 = 560.;
const XONE_BACK: f32 = 800.;
const XONE_ROLL: f32 = 1060.;
const XONE_SET: f32 = 1380.;

/// A white sphere lit from the top left, with a soft grey shade on the far side.
fn white_sphere(window: &mut Window, x: f32, y: f32, r: f32) {
    let b = Bounds::new(point(px(x - r), px(y - r)), size(px(2. * r), px(2. * r)));
    window.paint_quad(quad(b, Corners::all(px(r)), linear_gradient(150., linear_color_stop(c(0xffffff, 1.), 0.25), linear_color_stop(c(0xc9cfd2, 1.), 1.)), Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
    blob(window, x + r * 0.3, y + r * 0.35, r * 0.55, c(0xa9b0b4, 0.35), r * 0.45);
    blob(window, x - r * 0.25, y - r * 0.3, r * 0.42, c(0xffffff, 0.95), r * 0.3);
}

pub(super) fn xone(window: &mut Window, cx: &mut App, s: &Scene, ms: f32) {
    let (mx, h) = (s.cx(), s.h);
    s.full(window, c(0x000000, 1.));
    if ms < XONE_IN { return; }
    if ms < XONE_BACK {
        // so close to the sphere that it is all there is: white, a little shade, its edge far off
        let k = seg(ms, XONE_IN, XONE_BACK - XONE_IN);
        s.full(window, c(0xdfe3e7, 1.));
        blob(window, s.ox + s.w * 0.72, s.oy + h * 0.15, s.w * 0.45, c(0xffffff, 1.), s.w * 0.3);
        blob(window, s.ox + s.w * 0.1, s.oy + h * 0.95, s.w * 0.3, c(0xb4bbc2, 0.6 * (0.4 + 0.6 * k)), s.w * 0.25);
        return;
    }
    // the green room: darker above, a lighter floor below a soft horizon
    window.paint_quad(quad(Bounds::new(point(px(s.ox), px(s.oy)), size(px(s.w), px(h))), Corners::all(px(0.)),
        linear_gradient(180., linear_color_stop(c(0x0a630d, 1.), 0.), linear_color_stop(c(0x0f720e, 1.), 0.66)), Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
    window.paint_quad(quad(Bounds::new(point(px(s.ox), px(s.oy + h * 0.66)), size(px(s.w), px(h * 0.34))), Corners::all(px(0.)),
        linear_gradient(180., linear_color_stop(c(0x0f720e, 1.), 0.), linear_color_stop(c(0x22850c, 1.), 0.3)), Edges::all(px(0.)), transparent_black(), BorderStyle::default()));
    blob(window, mx, s.oy + h * 0.35, s.w * 0.3, c(0x17801a, 0.5), s.w * 0.25);

    let (fx, fy, fr) = (mx, s.oy + h * 0.415, h * 0.088);
    let name_y = s.oy + h * 0.53;
    // where the sphere is, how big, and how far it has turned (a quarter turn → facing us)
    let (x, y, r, turn) = if ms < XONE_ROLL {
        let k = seg(ms, XONE_BACK, XONE_ROLL - XONE_BACK);
        let near = 1. / (1. + 9. * k * k);
        (lerp(mx - s.w * 0.2, mx + s.w * 0.04, near), s.oy + h * 0.5, lerp(fr * 1.15, h * 0.95, near), 0.)
    } else {
        let k = ease_in_out(seg(ms, XONE_ROLL, XONE_SET - XONE_ROLL));
        let arc = (k * std::f32::consts::PI).sin();
        (lerp(mx - s.w * 0.2, fx, k), lerp(s.oy + h * 0.5, fy, k) + arc * h * 0.03, lerp(fr * 1.15, fr, k), k)
    };
    // its shadow on the floor
    let settle = seg(ms, XONE_ROLL, XONE_SET - XONE_ROLL);
    soft_rect(window, x - r * 1.1, s.oy + h * 0.735, r * 2.2, h * 0.018, c(0x063d06, 0.35 * settle), h * 0.02);

    // the name, uncovered letter by letter as the sphere rolls past it
    let name = "Console Mode";
    let size_px = h * 0.068;
    let widths: Vec<f32> = name.chars().map(|ch| text_width(window, &ch.to_string(), size_px, FontWeight::NORMAL)).collect();
    let total: f32 = widths.iter().sum();
    let mut lx = mx - total / 2.;
    for (ch, w) in name.chars().zip(widths) {
        let shown = if ms < XONE_ROLL { 0. } else { ((x - (lx + w / 2.)) / (r * 0.8) + 0.5).clamp(0., 1.).max(seg(ms, XONE_SET, 300.)) };
        text(window, cx, &ch.to_string(), size_px, FontWeight::NORMAL, c(0xffffff, shown), lx, name_y, false);
        lx += w;
    }

    white_sphere(window, x, y, r);
    // the logo on its face, coming round from the right as it turns
    if turn > 0. {
        let lon = (1. - turn) * std::f32::consts::FRAC_PI_2;
        let sx = lon.cos();
        let logo = Logo::centred(x + r * 0.52 * lon.sin(), y + r * 0.03, r * 1.2).squeezed(sx);
        draw_logo(window, &logo, Ink { body: 0x3c9b1e, detail: 0xf2f4f3, prompt: 0x5cb82c }, seg(turn, 0., 0.25).min(1.));
        // the gloss back over it
        blob(window, x - r * 0.3, y - r * 0.4, r * 0.25, c(0xffffff, 0.55), r * 0.18);
    }
}
