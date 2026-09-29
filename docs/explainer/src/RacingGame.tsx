import React, { useId } from "react";
import { SANS } from "./kit";

// A synthwave racing game drawn in SVG, so the TV has something that plays. Everything is a
// function of `t` (a frame at 30 fps), so passing the film's global frame keeps the same race
// going across scenes: on the TV, full screen, behind the session menu.

const VW = 1600;
const VH = 900;
const HORIZON = 380;
/** Depth of the player's car. */
const ZP = 1.25;
const SEGMENTS = 44;

const projY = (z: number) => HORIZON + 560 / z;
const halfW = (z: number) => 760 / z;

/** Road curvature, player lane (-1…1 across the road) and where the camera sits. */
const curve = (t: number) => 0.9 * Math.sin(t / 110) + 0.4 * Math.sin(t / 47);
const player = (t: number) => 0.5 * Math.sin(t / 40) + 0.15 * Math.sin(t / 17);
const camX = (t: number) => 0.6 * player(t);

/** Screen x of a point `lane` half-widths from the road centre at depth z. */
const projX = (t: number, z: number, lane: number) => VW / 2 + curve(t) * z * 14 + (lane - camX(t)) * halfW(z);

/** The rivals: each one is overtaken at `tc`, on the side away from the player. */
const RIVALS = Array.from({ length: 24 }, (_, i) => {
  const tc = 20 + i * 58;
  return { tc, lane: player(tc) >= 0 ? -0.62 : 0.62, color: ["#38c6ff", "#ffd166", "#7cff6b", "#ff8a5c"][i % 4] };
});

const Car: React.FC<{ x: number; y: number; w: number; color: string; tilt?: number; lights?: number }> = ({ x, y, w, color, tilt = 0, lights = 1 }) => (
  <g transform={`translate(${x} ${y}) rotate(${tilt}) scale(${w / 100})`}>
    <ellipse cx="0" cy="1" rx="58" ry="7" fill="#000" opacity="0.55" />
    <rect x="-48" y="-17" width="15" height="17" rx="3" fill="#0b0b10" />
    <rect x="33" y="-17" width="15" height="17" rx="3" fill="#0b0b10" />
    <path d="M-50 -12 L-50 -30 Q-50 -34 -46 -35 L46 -35 Q50 -34 50 -30 L50 -12 Q50 -8 46 -8 L-46 -8 Q-50 -8 -50 -12 Z" fill={color} />
    <path d="M-36 -35 L-28 -52 L28 -52 L36 -35 Z" fill={color} />
    <path d="M-31 -36 L-25 -49 L25 -49 L31 -36 Z" fill="#141028" />
    <path d="M-25 -49 L-17 -49 L-26 -36 L-31 -36 Z" fill="#fff" opacity="0.18" />
    <rect x="-52" y="-58" width="104" height="5" rx="2" fill="#15121f" />
    <rect x="-40" y="-54" width="4" height="4" fill="#15121f" />
    <rect x="36" y="-54" width="4" height="4" fill="#15121f" />
    <rect x="-46" y="-31" width="92" height="6" rx="3" fill="#ff2d55" opacity={0.75 + 0.25 * lights} />
    <rect x="-50" y="-34" width="100" height="12" rx="6" fill="#ff2d55" opacity={0.22 * lights} />
    <rect x="-9" y="-21" width="18" height="8" rx="1.5" fill="#e9e9f2" />
    <rect x="-48" y="-12" width="96" height="3" fill="#000" opacity="0.25" />
  </g>
);

/** Mountain ridge as a polygon; drawn twice so it can scroll forever. */
const ridge = (seed: number, amp: number, base: number) => {
  const pts: string[] = [];
  for (let x = 0; x <= VW; x += 40) {
    const h = amp * (0.55 + 0.25 * Math.sin(x / 97 + seed) + 0.2 * Math.sin(x / 41 + seed * 2.3)) * (0.6 + 0.4 * Math.abs(Math.sin(x / 260 + seed)));
    pts.push(`${x},${base - h}`);
  }
  return `0,${base} ${pts.join(" ")} ${VW},${base}`;
};
const RIDGE_FAR = ridge(1.3, 150, HORIZON + 2);
const RIDGE_NEAR = ridge(4.1, 80, HORIZON + 2);

const STARS = Array.from({ length: 60 }, (_, i) => ({ x: (i * 373) % VW, y: (i * 151) % (HORIZON - 120), r: 1 + (i % 3) * 0.6 }));

const fmtTime = (s: number) => {
  const m = Math.floor(s / 60);
  const sec = s - m * 60;
  return `${m}:${sec.toFixed(2).padStart(5, "0")}`;
};

export const RacingGame: React.FC<{ t: number; hud?: boolean }> = ({ t, hud = true }) => {
  const id = useId().replace(/[^a-zA-Z0-9]/g, "");
  const dist = t * 0.22;
  const frac = dist - Math.floor(dist);
  const scroll = 220 * Math.cos(t / 110) + 70 * Math.cos(t / 47);

  // Road, rumble strips, lane marks and the ground grid, far to near.
  const road: React.ReactNode[] = [];
  for (let k = SEGMENTS; k >= 0; k--) {
    const zn = k + 1 - frac;
    const zf = zn + 1;
    if (zn < 0.4) continue;
    const odd = (k + Math.floor(dist)) % 2 === 1;
    const yn = projY(zn);
    const yf = projY(zf);
    const q = (l1: number, l2: number) =>
      `${projX(t, zn, l1)},${yn} ${projX(t, zn, l2)},${yn} ${projX(t, zf, l2)},${yf} ${projX(t, zf, l1)},${yf}`;
    const fade = Math.min(1, 6 / zn);
    road.push(
      <g key={k}>
        <line x1={0} x2={VW} y1={yn} y2={yn} stroke="#ff3ec9" strokeWidth={Math.max(0.6, 3 / zn)} opacity={0.55 * fade} />
        <polygon points={q(-1, 1)} fill={odd ? "#241741" : "#1e1238"} />
        <polygon points={q(-1.1, -1)} fill={odd ? "#ff3ec9" : "#f4f0ff"} />
        <polygon points={q(1, 1.1)} fill={odd ? "#ff3ec9" : "#f4f0ff"} />
        {!odd && (
          <>
            <polygon points={q(-0.35, -0.32)} fill="#f4f0ff" opacity={0.85} />
            <polygon points={q(0.32, 0.35)} fill="#f4f0ff" opacity={0.85} />
          </>
        )}
      </g>,
    );
  }
  const depths = [0.45, 0.6, 0.8, 1.1, 1.5, 2, 3, 4.5, 7, 11, 18, 30, 46];
  const grid = [1.6, 2.4, 3.6, 5.4, 8, 12].flatMap((m) =>
    [-1, 1].map((s) => (
      <polyline key={`${m}${s}`} points={depths.map((z) => `${projX(t, z, s * m)},${projY(z)}`).join(" ")} fill="none" stroke="#9b4dff" strokeWidth="2" opacity="0.45" />
    )),
  );

  // Rivals, far to near, then the player.
  const rivals = RIVALS.map((r) => ({ ...r, z: ZP + (r.tc - t) * 0.1 }))
    .filter((r) => r.z > 0.5 && r.z < 34)
    .sort((a, b) => b.z - a.z);
  const px = projX(t, ZP, player(t));
  const tilt = (player(t + 1) - player(t - 1)) * 160;
  const passed = RIVALS.filter((r) => r.tc < t).length;
  const pos = Math.max(1, 8 - passed);
  const speed = Math.round(286 + 6 * Math.sin(t / 13) + 4 * Math.sin(t / 5));
  const hudText = { fontFamily: SANS, fill: "#fff", paintOrder: "stroke" as const, stroke: "rgba(10,4,30,0.55)", strokeWidth: 6 };

  return (
    <svg viewBox={`0 0 ${VW} ${VH}`} preserveAspectRatio="xMidYMid slice" style={{ position: "absolute", inset: 0, width: "100%", height: "100%", display: "block" }}>
      <defs>
        <linearGradient id={`${id}sky`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#07041a" />
          <stop offset="0.55" stopColor="#2b0b4a" />
          <stop offset="1" stopColor="#ff4f8b" />
        </linearGradient>
        <linearGradient id={`${id}sun`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#ffe27a" />
          <stop offset="1" stopColor="#ff3ea5" />
        </linearGradient>
        <linearGradient id={`${id}ground`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#2a0c45" />
          <stop offset="1" stopColor="#0c0618" />
        </linearGradient>
        <linearGradient id={`${id}haze`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#ff4f8b" stopOpacity="0.55" />
          <stop offset="1" stopColor="#ff4f8b" stopOpacity="0" />
        </linearGradient>
        <mask id={`${id}bands`}>
          <rect x="0" y="0" width={VW} height={VH} fill="#fff" />
          {[0, 1, 2, 3, 4, 5].map((i) => (
            <rect key={i} x="0" y={HORIZON - 120 + i * 20 + i * i} width={VW} height={3 + i * 2.2} fill="#000" />
          ))}
        </mask>
      </defs>
      <rect x="0" y="0" width={VW} height={HORIZON} fill={`url(#${id}sky)`} />
      {STARS.map((s, i) => (
        <circle key={i} cx={(s.x + scroll * 0.1 + VW) % VW} cy={s.y} r={s.r} fill="#fff" opacity={0.35 + 0.35 * Math.sin(t / 9 + i)} />
      ))}
      <circle cx={VW / 2 + scroll * 0.25} cy={HORIZON - 70} r="190" fill={`url(#${id}sun)`} mask={`url(#${id}bands)`} />
      {[0, 1].map((i) => (
        <polygon key={`f${i}`} points={RIDGE_FAR} fill="#3a1160" transform={`translate(${(((scroll * 0.5) % VW) + VW) % VW - VW * i} 0)`} />
      ))}
      {[0, 1].map((i) => (
        <polygon key={`n${i}`} points={RIDGE_NEAR} fill="#1f0838" transform={`translate(${(((scroll * 0.9) % VW) + VW) % VW - VW * i} 0)`} />
      ))}
      <rect x="0" y={HORIZON} width={VW} height={VH - HORIZON} fill={`url(#${id}ground)`} />
      {grid}
      {road}
      <rect x="0" y={HORIZON} width={VW} height="70" fill={`url(#${id}haze)`} />
      {rivals.map((r) => (
        <Car key={r.tc} x={projX(t, r.z, r.lane)} y={projY(r.z)} w={425 / r.z} color={r.color} lights={0.6} />
      ))}
      <Car x={px} y={projY(ZP) + Math.sin(t / 3) * 1.5} w={340} color="#ff3ec9" tilt={tilt} lights={1} />
      {hud && (
        <g>
          <text x="56" y="92" fontSize="30" fontWeight="700" opacity="0.8" {...hudText}>
            POS
          </text>
          <text x="56" y="166" fontSize="84" fontWeight="800" {...hudText}>
            {pos}
            <tspan fontSize="44" opacity="0.7">
              /8
            </tspan>
          </text>
          <text x="1544" y="92" fontSize="30" fontWeight="700" textAnchor="end" opacity="0.8" {...hudText}>
            LAP 2/3
          </text>
          <text x="1544" y="150" fontSize="54" fontWeight="800" textAnchor="end" {...hudText}>
            {fmtTime(42.18 + t / 30)}
          </text>
          <text x="1544" y="836" fontSize="110" fontWeight="800" textAnchor="end" {...hudText}>
            {speed}
            <tspan fontSize="40" opacity="0.75">
              {" "}
              km/h
            </tspan>
          </text>
          <rect x="1244" y="852" width="300" height="10" rx="5" fill="#fff" opacity="0.2" />
          <rect x="1244" y="852" width={300 * (speed / 300)} height="10" rx="5" fill="#ff3ec9" />
        </g>
      )}
    </svg>
  );
};
