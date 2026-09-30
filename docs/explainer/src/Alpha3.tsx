import React from "react";
import { AbsoluteFill, Audio, Easing, interpolate, Sequence, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { C, Center, Check, clamp, Glow, Line, Logo, SANS, tw, useFonts } from "./kit";
import { RacingGame } from "./RacingGame";

// "What's new in 1.6.0-alpha.3": the new session menu (with ControlFS), the redesigned Console
// interface, the Steam covers background, controller shortcuts of your choice, and the fixes.
// The mockups follow the app's XAML (sizes in DIPs on a 1536 × 864 stage, scaled 1.25× to
// 1080p) and use the en-US strings. Music: scripts/soundtrack.py alpha3 (120 BPM).

const A = {
  title: { from: 0, dur: 120 },
  intro: { from: 120, dur: 90 },
  menu: { from: 210, dur: 300 },
  consoleIntro: { from: 510, dur: 60 },
  console: { from: 570, dur: 240 },
  covers: { from: 810, dur: 90 },
  shortcuts: { from: 900, dur: 180 },
  more: { from: 1080, dur: 150 },
  outro: { from: 1230, dur: 150 },
};
export const ALPHA3_DURATION = A.outro.from + A.outro.dur;

const ACCENT = "#4FCFA3";
const SUB = "rgba(255,255,255,0.70)";
const CARD = "linear-gradient(180deg, rgba(255,255,255,0.15), rgba(255,255,255,0.06))";

// ───────────────────────── shared pieces ─────────────────────────

type Glyph =
  | "controller"
  | "folder"
  | "play"
  | "volume"
  | "monitor"
  | "speakers"
  | "fps"
  | "hdr"
  | "video"
  | "back"
  | "power"
  | "vrr"
  | "sound"
  | "steam"
  | "rotate"
  | "flask"
  | "gamepad";

/** Line icons standing in for the Segoe Fluent glyphs the app uses. */
const G: React.FC<{ name: Glyph; size?: number; color?: string }> = ({ name, size = 24, color = "#fff" }) => {
  const p = { fill: "none", stroke: color, strokeWidth: 1.8, strokeLinecap: "round" as const, strokeLinejoin: "round" as const };
  const d: Record<Glyph, React.ReactNode> = {
    controller: (
      <>
        <path d="M7 8h10a4.5 4.5 0 0 1 4.3 5.8l-1 3.4a2.2 2.2 0 0 1-3.8.8L14.6 16H9.4l-1.9 2a2.2 2.2 0 0 1-3.8-.8l-1-3.4A4.5 4.5 0 0 1 7 8z" {...p} />
        <path d="M7.5 11v3M6 12.5h3" {...p} />
        <circle cx="16" cy="11.5" r="0.9" fill={color} />
        <circle cx="17.8" cy="13.3" r="0.9" fill={color} />
      </>
    ),
    folder: <path d="M3 6.5a1.5 1.5 0 0 1 1.5-1.5h4.3l2 2.2h8.7A1.5 1.5 0 0 1 21 8.7v9.8a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 18.5z" {...p} />,
    play: <path d="M8 5.5l10.5 6.5L8 18.5z" fill={color} stroke={color} strokeWidth={1.5} strokeLinejoin="round" />,
    volume: (
      <>
        <path d="M4 9.5h3.5L12 6v12l-4.5-3.5H4z" {...p} />
        <path d="M15.5 9a4 4 0 0 1 0 6M18 6.5a7.5 7.5 0 0 1 0 11" {...p} />
      </>
    ),
    monitor: (
      <>
        <rect x="3" y="4.5" width="18" height="12" rx="1.5" {...p} />
        <path d="M9 20h6M12 16.5V20" {...p} />
      </>
    ),
    speakers: (
      <>
        <rect x="3.5" y="4" width="7" height="16" rx="1.5" {...p} />
        <rect x="13.5" y="4" width="7" height="16" rx="1.5" {...p} />
        <circle cx="7" cy="14" r="2" {...p} />
        <circle cx="17" cy="14" r="2" {...p} />
      </>
    ),
    fps: <path d="M4 17a8 8 0 1 1 16 0M12 17l4-5" {...p} />,
    hdr: (
      <>
        <circle cx="12" cy="12" r="4" {...p} />
        <path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6L7 7M17 17l1.4 1.4M5.6 18.4L7 17M17 7l1.4-1.4" {...p} />
      </>
    ),
    video: (
      <>
        <rect x="3" y="6.5" width="12.5" height="11" rx="1.5" {...p} />
        <path d="M15.5 10.5L21 7.5v9l-5.5-3" {...p} />
      </>
    ),
    back: <path d="M10 7l-5 5 5 5M5 12h14" {...p} />,
    power: <path d="M8 6.5a7 7 0 1 0 8 0M12 3v8" {...p} />,
    vrr: <path d="M3 12c2-5 4-5 6 0s4 5 6 0 4-5 6 0" {...p} />,
    sound: (
      <>
        <path d="M9 17V6l10-2v11" {...p} />
        <circle cx="6.5" cy="17" r="2.5" {...p} />
        <circle cx="16.5" cy="15" r="2.5" {...p} />
      </>
    ),
    steam: (
      <>
        <circle cx="12" cy="12" r="8.5" {...p} />
        <circle cx="15" cy="9.5" r="2.5" {...p} />
        <path d="M4 14l5 2" {...p} />
        <circle cx="9.5" cy="16.2" r="1.6" {...p} />
      </>
    ),
    rotate: (
      <>
        <rect x="7" y="3" width="10" height="16" rx="1.5" {...p} />
        <path d="M3 17a9 9 0 0 0 4 4M3 21v-4h4" {...p} />
      </>
    ),
    flask: <path d="M9.5 3h5M10.5 3v6L5 19a1.5 1.5 0 0 0 1.3 2h11.4a1.5 1.5 0 0 0 1.3-2l-5.5-10V3M7.5 15h9" {...p} />,
    gamepad: (
      <>
        <rect x="3" y="7" width="18" height="10" rx="5" {...p} />
        <path d="M8 10v4M6 12h4" {...p} />
        <circle cx="16" cy="12" r="1" fill={color} />
      </>
    ),
  };
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" style={{ display: "block", flexShrink: 0 }}>
      {d[name]}
    </svg>
  );
};

/** Controller button badge like the app's hint pills. */
const Badge: React.FC<{ label: string; lit?: number; h?: number }> = ({ label, lit = 0, h = 32 }) => (
  <span
    style={{
      display: "inline-flex",
      alignItems: "center",
      justifyContent: "center",
      minWidth: h,
      height: h,
      padding: "0 10px",
      boxSizing: "border-box",
      borderRadius: h / 2,
      background: lit > 0.5 ? ACCENT : "rgba(255,255,255,0.08)",
      border: `1px solid ${lit > 0.5 ? ACCENT : "rgba(255,255,255,0.22)"}`,
      color: lit > 0.5 ? "#03130d" : "#fff",
      fontFamily: SANS,
      fontWeight: 700,
      fontSize: h * 0.5,
    }}
  >
    {label}
  </span>
);

const Hints: React.FC<{ items: [string, string][]; style?: React.CSSProperties }> = ({ items, style }) => (
  <div style={{ display: "flex", gap: 26, alignItems: "center", fontFamily: SANS, fontSize: 17, color: "#fff", ...style }}>
    {items.map(([b, t]) => (
      <span key={b + t} style={{ display: "flex", alignItems: "center", gap: 10 }}>
        <Badge label={b} />
        {t}
      </span>
    ))}
  </div>
);

/** The app's UI is laid out on a 1536 × 864 stage and scaled up to 1080p. */
const Stage: React.FC<{ children: React.ReactNode; style?: React.CSSProperties }> = ({ children, style }) => (
  <div style={{ position: "absolute", left: 0, top: 0, width: 1536, height: 864, transformOrigin: "0 0", scale: "1.25", ...style }}>{children}</div>
);

/** A caption pill in screen space, for scenes where the mockup fills the frame. */
const Caption: React.FC<{ text: string; at: number; until: number; pos?: "top" | "bottomLeft" | "windows" }> = ({ text, at, until, pos = "top" }) => {
  const f = useCurrentFrame();
  if (f < at || f >= until) return null;
  const o = tw(f - at, [0, 8], [0, 1], Easing.linear) * (1 - tw(f, [until - 8, until], [0, 1], Easing.linear));
  const place: React.CSSProperties =
    pos === "top"
      ? { top: 44, left: 0, right: 0, justifyContent: "center" }
      : pos === "windows"
        ? { bottom: 110, left: 730, right: 70, justifyContent: "center" }
        : { bottom: 48, left: 70 };
  return (
    <div style={{ position: "absolute", display: "flex", ...place, opacity: o, translate: `0 ${(1 - o) * 10}px`, zIndex: 20 }}>
      <div
        style={{
          padding: "14px 28px",
          borderRadius: 999,
          background: "rgba(8,10,14,0.82)",
          border: `1px solid ${C.mint}66`,
          fontFamily: SANS,
          fontSize: 30,
          fontWeight: 600,
          color: C.text,
          boxShadow: "0 12px 40px rgba(0,0,0,0.5)",
        }}
      >
        {text}
      </div>
    </div>
  );
};

/** Slides and fades in from an offset, like the app's entrance animations. */
const enter = (f: number, at: number, dx: number, dy: number, dur = 9): React.CSSProperties => {
  const p = tw(f - at, [0, dur], [0, 1]);
  return { opacity: p, translate: `${(1 - p) * dx}px ${(1 - p) * dy}px` };
};

/** Focus ring + scale used by the Console interface (ScaleTile). */
const focusStyle = (on: number, scale = 1.07, radius = 16): React.CSSProperties => ({
  scale: String(1 + (scale - 1) * on),
  outline: on > 0.5 ? "3px solid #fff" : "3px solid transparent",
  outlineOffset: 5,
  borderRadius: radius,
});

/** 0…1 closeness of a moving focus position to item i. */
const near = (pos: number, i: number) => Math.max(0, 1 - Math.abs(pos - i));

/** A step function that eases to each new index on its frame. */
const steps = (f: number, marks: [number, number][], start: number) => {
  let v = start;
  let prev = start;
  for (const [at, to] of marks) {
    v += (to - prev) * tw(f, [at, at + 5], [0, 1]);
    prev = to;
  }
  return v;
};

// ───────────────────────── Steam covers ─────────────────────────

/** A made-up game cover (no real artwork): gradient, a shape, a title block. */
const Cover: React.FC<{ i: number; w: number; h: number }> = ({ i, w, h }) => {
  const hue = (i * 47 + 200) % 360;
  const hue2 = (hue + 40 + (i % 3) * 30) % 360;
  const shape = i % 4;
  return (
    <div style={{ width: w, height: h, borderRadius: 6, overflow: "hidden", position: "relative", background: `linear-gradient(${160 + (i % 5) * 20}deg, hsl(${hue} 60% 38%), hsl(${hue2} 70% 14%))` }}>
      {shape === 0 && <div style={{ position: "absolute", left: "20%", top: "18%", width: "60%", aspectRatio: "1", borderRadius: "50%", background: `hsl(${hue2} 90% 70% / 0.55)` }} />}
      {shape === 1 && <div style={{ position: "absolute", left: 0, right: 0, bottom: "28%", height: "40%", background: `hsl(${hue} 40% 8% / 0.6)`, clipPath: "polygon(0 60%,30% 10%,55% 50%,75% 0,100% 45%,100% 100%,0 100%)" }} />}
      {shape === 2 && <div style={{ position: "absolute", left: "30%", top: "12%", width: "40%", height: "55%", borderRadius: "40% 40% 10% 10%", background: `hsl(${hue2} 80% 60% / 0.5)` }} />}
      {shape === 3 && <div style={{ position: "absolute", inset: "15% 15% 40% 15%", border: `${w * 0.05}px solid hsl(${hue2} 90% 70% / 0.6)`, rotate: "45deg" }} />}
      <div style={{ position: "absolute", left: "12%", right: "12%", bottom: "12%", height: h * 0.05, borderRadius: 3, background: "rgba(255,255,255,0.5)" }} />
      <div style={{ position: "absolute", left: "25%", right: "25%", bottom: "20%", height: h * 0.025, borderRadius: 3, background: "rgba(255,255,255,0.28)" }} />
    </div>
  );
};

/** The Console interface background: covers grid at 55% under the app's gradient. */
const CoversBackground: React.FC<{ drift?: number; gradientOnly?: number; veil?: number }> = ({ drift = 0, gradientOnly = 0, veil = 1 }) => (
  <AbsoluteFill style={{ background: "#0F1418", overflow: "hidden" }}>
    <div style={{ position: "absolute", left: -60, top: -drift - 60, display: "grid", gridTemplateColumns: "repeat(8, 240px)", opacity: 0.55 * (1 - gradientOnly) }}>
      {Array.from({ length: 40 }, (_, i) => (
        <div key={i} style={{ padding: 6 }}>
          <Cover i={i} w={228} h={342} />
        </div>
      ))}
    </div>
    {/* The app's #D9123A30 → #E60F1B1F → #F20F1418 (WinUI colours are #AARRGGBB). */}
    <AbsoluteFill
      style={{
        background: `linear-gradient(135deg, rgba(18,58,48,${0.85 * veil}) 0%, rgba(15,27,31,${0.9 * veil}) 45%, rgba(15,20,24,${0.95 * veil}) 100%)`,
      }}
    />
  </AbsoluteFill>
);

// ───────────────────────── scenes ─────────────────────────

const Title: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const pop = spring({ frame: f, fps, config: { damping: 18, stiffness: 100 } });
  return (
    <AbsoluteFill>
      <Glow color={C.mint} y={560} opacity={0.18} size={1500} />
      <Center>
        <div style={{ display: "flex", justifyContent: "center", scale: String(0.8 + pop * 0.2) }}>
          <Logo size={170} draw={tw(f, [0, 34], [0, 1], Easing.inOut(Easing.cubic))} glow={tw(f, [24, 50], [0, 1])} />
        </div>
        <div style={{ height: 40 }} />
        <Line words={["What’s", "new", "in"]} at={14} size={72} weight={600} stagger={4} color={C.muted} />
        <div style={{ height: 8 }} />
        <Line words={["Console", "Mode", { t: "1.6", grad: true }, { t: "alpha.3", grad: true }]} at={24} size={150} weight={800} stagger={5} />
      </Center>
    </AbsoluteFill>
  );
};

/** 120–210: the race, and the promise of the new menu. */
const MenuIntro: React.FC = () => {
  const f = useCurrentFrame();
  const inOut = tw(f, [4, 14], [0, 1], Easing.linear);
  return (
    <AbsoluteFill>
      <RacingGame t={A.intro.from + f} />
      <AbsoluteFill style={{ background: `rgba(5,2,15,${0.35 * inOut})` }} />
      <Center>
        <Line words={["A", "new", "session", { t: "menu.", grad: true }]} at={6} size={150} stagger={5} style={{ textShadow: "0 10px 60px rgba(0,0,0,0.55)" }} />
        <div style={{ height: 24 }} />
        <Line words={["Steam", "overlay", "style,", "over", "any", "game."]} at={30} size={52} weight={600} stagger={3} color="rgba(245,245,247,0.85)" style={{ textShadow: "0 6px 30px rgba(0,0,0,0.6)" }} />
      </Center>
    </AbsoluteFill>
  );
};

type Win = { proc: string; title: string; color: string; letter: string };
const WINDOWS: Win[] = [
  { proc: "NeonDrift", title: "Neon Drift", color: "linear-gradient(135deg,#ff3ec9,#7a2cff)", letter: "N" },
  { proc: "steam", title: "Steam Big Picture Mode", color: "linear-gradient(135deg,#2a475e,#171a21)", letter: "S" },
  { proc: "Discord", title: "#general – Friends", color: "linear-gradient(135deg,#6b7cff,#4450c9)", letter: "D" },
  { proc: "firefox", title: "Best graphics settings – Firefox", color: "linear-gradient(135deg,#ff9a3c,#c2279a)", letter: "F" },
  { proc: "explorer", title: "Downloads", color: "linear-gradient(135deg,#f7c948,#c99312)", letter: "E" },
];

/** A row of the side panel. */
const SideRow: React.FC<{ glyph: Glyph; label: string; value?: string; focus: number; bg?: string; style?: React.CSSProperties; children?: React.ReactNode }> = ({
  glyph,
  label,
  value,
  focus,
  bg,
  style,
  children,
}) => (
  <div
    style={{
      height: 74,
      borderRadius: 16,
      padding: "0 16px",
      display: "flex",
      alignItems: "center",
      gap: 16,
      background: bg ?? "rgba(255,255,255,0.04)",
      boxShadow: focus > 0.5 ? "inset 0 0 0 2px #fff" : undefined,
      boxSizing: "border-box",
      position: "relative",
      ...style,
    }}
  >
    <div style={{ width: 44, height: 44, borderRadius: 22, background: "rgba(255,255,255,0.15)", display: "flex", alignItems: "center", justifyContent: "center" }}>
      <G name={glyph} size={21} />
    </div>
    <div style={{ flex: 1, fontFamily: SANS }}>
      {value !== undefined ? (
        <>
          <div style={{ fontSize: 16, color: SUB }}>{label}</div>
          <div style={{ fontSize: 19, fontWeight: 600, color: "#fff" }}>{value}</div>
        </>
      ) : (
        <div style={{ fontSize: 19, fontWeight: 600, color: "#fff" }}>{label}</div>
      )}
    </div>
    {children}
  </div>
);

/** 210–510: the session menu over the race, then the zoom into ControlFS. */
const SessionMenu: React.FC = () => {
  const f = useCurrentFrame();
  const open = 4;
  // Focus: side rows 0 ControlFS, 1 Back to the game, 2 Volume, … ; windows are 10 + i.
  const pos = steps(
    f,
    [
      [30, 2],
      [90, 10],
      [105, 11],
      [120, 12],
      [150, 11],
      [180, 0],
    ],
    1,
  );
  const adjusting = f >= 45 && f < 84;
  const vol = Math.round(interpolate(f, [52, 76], [65, 80], clamp));
  const closing = tw(f, [135, 145], [0, 1], Easing.in(Easing.cubic));
  const closed = f >= 145;
  const wins = WINDOWS.filter((_, i) => !(closed && i === 2));
  const zoom = tw(f, [192, 232], [0, 1], Easing.inOut(Easing.cubic));
  const press = interpolate(f, [248, 252, 260], [0, 1, 0], clamp);
  const burst = tw(f, [262, 292], [0, 1], Easing.inOut(Easing.cubic));
  const Z = 1.6;
  // Zoom towards the ControlFS row (stage 56…556, 118…192).
  const zs = 1 + (Z - 1) * zoom;
  const ax = 70;
  const ay = 150;

  const rows: { glyph: Glyph; label: string; value?: string; bg?: string }[] = [
    { glyph: "play", label: "Back to the game", bg: "linear-gradient(135deg,#2A8F69,#17362F)" },
    { glyph: "volume", label: "Volume", value: "" },
    { glyph: "monitor", label: "Resolution and refresh rate", value: "3840 × 2160 · 120 Hz" },
    { glyph: "speakers", label: "Audio output", value: "LG TV" },
    { glyph: "fps", label: "FPS limit", value: "60 FPS" },
    { glyph: "hdr", label: "HDR on the gaming display", value: "On" },
  ];

  return (
    <AbsoluteFill>
      <div style={{ position: "absolute", inset: -40, filter: `blur(${tw(f, [0, 8], [0, 18])}px)` }}>
        <RacingGame t={A.menu.from + f} hud={false} />
      </div>
      <AbsoluteFill style={{ background: "rgba(7,10,14,0.6)", opacity: tw(f, [0, 8], [0, 1], Easing.linear) }} />
      <AbsoluteFill style={{ transformOrigin: `${ax * 1.25}px ${ay * 1.25}px`, scale: String(zs) }}>
        <Stage>
          {/* Header */}
          <div style={{ position: "absolute", left: 56, top: 36, right: 56, display: "flex", alignItems: "center", gap: 16, ...enter(f, open, 0, -16) }}>
            <div style={{ width: 54, height: 54, borderRadius: 27, background: ACCENT, display: "flex", alignItems: "center", justifyContent: "center" }}>
              <G name="controller" size={28} color="#03130d" />
            </div>
            <div style={{ flex: 1, fontFamily: SANS }}>
              <div style={{ fontSize: 28, fontWeight: 600, color: "#fff" }}>Session menu</div>
              <div style={{ fontSize: 16, color: SUB }}>Playing for 1:12:40 · Xbox controller detected</div>
            </div>
            <div style={{ fontFamily: SANS, fontSize: 40, fontWeight: 600, color: "#fff" }}>21:48</div>
          </div>
          {/* Side panel */}
          <div
            style={{
              position: "absolute",
              left: 56,
              top: 118,
              width: 500,
              height: 662,
              borderRadius: 24,
              padding: 14,
              boxSizing: "border-box",
              background: "rgba(17,24,32,0.70)",
              overflow: "hidden",
              ...enter(f, open + 1, -56, 0, 10),
            }}
          >
            <SideRow
              glyph="folder"
              label="File explorer (ControlFS)"
              focus={near(pos, 0)}
              bg="linear-gradient(135deg,#2B5C8F,#16263A)"
              style={{ height: 74, scale: String(1 - press * 0.04), ...enter(f, open + 3, 0, 14) }}
            >
              <div style={{ position: "absolute", left: 76, bottom: 10, fontFamily: SANS, fontSize: 14, color: SUB }}>Browse your files with the controller</div>
            </SideRow>
            <div style={{ height: 10 }} />
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {rows.map((r, i) => (
                <div key={r.label} style={enter(f, open + 4 + i * 1.2, 0, 14)}>
                  {r.glyph === "volume" ? (
                    <SideRow glyph="volume" label="Volume" focus={near(pos, 2)}>
                      <div style={{ position: "absolute", left: 76, right: 16, bottom: 14, height: 6, borderRadius: 3, background: "rgba(255,255,255,0.15)" }}>
                        <div style={{ width: `${vol}%`, height: "100%", borderRadius: 3, background: ACCENT }} />
                      </div>
                      <div style={{ display: "flex", alignItems: "center", gap: 10, fontFamily: SANS, fontSize: 17, fontWeight: 600, color: "#fff", marginTop: -14 }}>
                        {adjusting && <span style={{ color: ACCENT }}>◀</span>}
                        {vol}%
                        {adjusting && <span style={{ color: ACCENT }}>▶</span>}
                      </div>
                    </SideRow>
                  ) : (
                    <SideRow glyph={r.glyph} label={r.label} value={r.value} focus={near(pos, i + 1)} bg={r.bg} />
                  )}
                </div>
              ))}
            </div>
          </div>
          {/* Windows */}
          <div style={{ position: "absolute", left: 584, top: 118, right: 56, ...enter(f, open + 5, 0, 26, 10) }}>
            <div style={{ display: "flex", alignItems: "baseline", gap: 14, fontFamily: SANS }}>
              <span style={{ fontSize: 26, fontWeight: 600, color: "#fff" }}>Windows</span>
              <span style={{ fontSize: 18, color: SUB }}>{wins.length} open</span>
            </div>
            <div style={{ display: "flex", flexWrap: "wrap", gap: 16, marginTop: 18 }}>
              {WINDOWS.map((w, i) => {
                if (closed && i === 2) return null;
                const slot = closed && i > 2 ? i - 1 : i;
                const on = near(pos, 10 + slot);
                const isClosing = i === 2 ? closing : 0;
                return (
                  <div
                    key={w.proc}
                    style={{
                      width: 250,
                      height: 178,
                      borderRadius: 16,
                      padding: "16px 18px",
                      boxSizing: "border-box",
                      background: CARD,
                      boxShadow: on > 0.5 ? "inset 0 0 0 2px #fff" : undefined,
                      scale: String((1 + 0.04 * on) * (1 - 0.1 * isClosing)),
                      opacity: 1 - isClosing,
                      translate: `0 ${isClosing * 22}px`,
                      display: "flex",
                      flexDirection: "column",
                      justifyContent: "space-between",
                    }}
                  >
                    <div style={{ width: 52, height: 52, borderRadius: 26, background: "rgba(255,255,255,0.15)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                      <div style={{ width: 34, height: 34, borderRadius: 9, background: w.color, display: "flex", alignItems: "center", justifyContent: "center", fontFamily: SANS, fontWeight: 800, fontSize: 18, color: "#fff" }}>{w.letter}</div>
                    </div>
                    <div style={{ fontFamily: SANS }}>
                      <div style={{ fontSize: 16, color: SUB }}>{w.proc}</div>
                      <div style={{ fontSize: 18, fontWeight: 600, color: "#fff", lineHeight: 1.2 }}>{w.title}</div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
          {/* Hints */}
          <div style={{ position: "absolute", left: 0, right: 0, bottom: 30, display: "flex", justifyContent: "center", ...enter(f, open + 10, 0, 10) }}>
            <Hints
              items={[
                ["A", "Select"],
                ["B", "Back to the game"],
                ["X", "Close window"],
                ["◀ ▶", "Volume"],
              ]}
            />
          </div>
        </Stage>
      </AbsoluteFill>
      {/* ControlFS opening: the folder grows from the row until it fills the screen. */}
      {burst > 0 && (
        <div
          style={{
            position: "absolute",
            left: 0,
            top: 0,
            width: 1920,
            height: 1080,
            background: "radial-gradient(circle at 20% 20%, #2B5C8F, #0d1826 70%)",
            clipPath: `circle(${burst * 2300}px at ${150}px ${200}px)`,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <div style={{ opacity: tw(f, [274, 286], [0, 1], Easing.linear), scale: String(tw(f, [274, 292], [0.8, 1])) }}>
            <G name="folder" size={220} color="#fff" />
          </div>
        </div>
      )}
      <Caption pos="windows" text="Volume, resolution, audio, FPS and HDR on the side." at={20} until={86} />
      <Caption pos="windows" text="Every open window, one press away. X closes it." at={88} until={176} />
      <AbsoluteFill style={{ background: "linear-gradient(90deg, transparent 44%, rgb(7,10,14) 56%)", opacity: zoom * (1 - burst) }} />
      {/* ControlFS copy, over the zoomed panel. */}
      <div style={{ position: "absolute", left: 1110, top: 440, width: 770, opacity: zoom * (1 - burst), fontFamily: SANS }}>
        <Line words={["File", "explorer,", { t: "built", grad: true }, { t: "in.", grad: true }]} at={200} size={74} stagger={4} align="flex-start" />
        <div style={{ height: 18 }} />
        <Line words={["ControlFS,", "the", "team’s", "open-source,", "controller-first", "file", "manager.", "Opens", "full", "screen", "on", "the", "TV."]} at={214} size={36} weight={500} stagger={1} color={C.muted} align="flex-start" />
      </div>
    </AbsoluteFill>
  );
};

/** 510–570: announce the Console interface. */
const ConsoleIntro: React.FC = () => (
  <AbsoluteFill>
    <Glow color={C.mint} y={560} opacity={0.14} />
    <Center>
      <Line words={["The", "Console", "interface,", { t: "redesigned.", grad: true }]} at={2} size={110} stagger={4} />
      <div style={{ height: 22 }} />
      <Line words={["Home,", "Session", "and", "System.", "LB", "/", "RB", "to", "switch."]} at={16} size={46} weight={600} stagger={2} color={C.muted} />
    </Center>
  </AbsoluteFill>
);

const ConsoleCard: React.FC<{ w: number; h: number; focus: number; children: React.ReactNode; style?: React.CSSProperties }> = ({ w, h, focus, children, style }) => (
  <div style={{ width: w, height: h, borderRadius: 16, background: CARD, padding: 18, boxSizing: "border-box", flexShrink: 0, ...focusStyle(focus), ...style }}>{children}</div>
);

const Circle: React.FC<{ glyph: Glyph; size?: number }> = ({ glyph, size = 46 }) => (
  <div style={{ width: size, height: size, borderRadius: size / 2, background: "rgba(255,255,255,0.15)", display: "flex", alignItems: "center", justifyContent: "center" }}>
    <G name={glyph} size={size * 0.46} />
  </div>
);

/** Monitor art from the Session tab: what will happen to that screen. */
const MonitorArt: React.FC<{ role: "play" | "off" }> = ({ role }) => (
  <div style={{ width: 124, display: "flex", flexDirection: "column", alignItems: "center" }}>
    <div style={{ width: 124, height: 72, border: "3px solid #E6EBEF", borderRadius: 9, padding: 3, boxSizing: "border-box" }}>
      <div
        style={{
          width: "100%",
          height: "100%",
          borderRadius: 5,
          background: role === "play" ? "linear-gradient(135deg,#7DF0CB,#23A67F)" : "#0A0D10",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <G name={role === "play" ? "play" : "power"} size={24} color={role === "play" ? "#0B1F18" : "rgba(255,255,255,0.6)"} />
      </div>
    </div>
    <div style={{ width: 46, height: 6, background: "#E6EBEF", marginTop: 6, borderRadius: 2 }} />
  </div>
);

/** 570–810: the Console interface, walking the three tabs. */
const ConsoleUi: React.FC = () => {
  const f = useCurrentFrame();
  const tab = f < 100 ? 0 : f < 180 ? 1 : 2;
  const tabAt = [0, 100, 180][tab];
  const slide = tw(f - tabAt, [0, 8], [1, 0]);
  const fade = tw(f - tabAt, [0, 7], [0, 1], Easing.linear);
  const pageStyle: React.CSSProperties = tab === 0 ? {} : { translate: `${slide * 56}px 0`, opacity: fade };
  const homePos = steps(
    f,
    [
      [40, 1],
      [55, 2],
      [70, 3],
    ],
    0,
  );
  const sessPos = steps(f, [[140, 1]], 0);
  const bg = f >= 205 && f < 228 ? 1 : 0;
  const gradOnly = tw(f, [205, 213], [0, 1], Easing.linear) * (1 - tw(f, [228, 236], [0, 1], Easing.linear));
  const rb = (at: number) => (f >= at - 2 && f < at + 6 ? 1 : 0);

  const cards: { glyph: Glyph; label: string; value: React.ReactNode }[] = [
    { glyph: "monitor", label: "Gaming display", value: "Living room TV" },
    { glyph: "volume", label: "Audio output", value: "LG TV" },
    { glyph: "play", label: "Open", value: "Steam Big Picture" },
    {
      glyph: "hdr",
      label: "Quick settings",
      value: (
        <>
          HDR On
          <br />
          VRR Off
        </>
      ),
    },
  ];

  return (
    <AbsoluteFill>
      <CoversBackground gradientOnly={gradOnly} drift={f * 0.15} />
      <Stage>
        {/* Top bar */}
        <div style={{ position: "absolute", left: 56, right: 56, top: 18, height: 56, display: "flex", alignItems: "center", fontFamily: SANS, color: "#fff" }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12, flex: 1 }}>
            <div style={{ width: 40, height: 40, borderRadius: 20, background: ACCENT, display: "flex", alignItems: "center", justifyContent: "center" }}>
              <G name="controller" size={22} color="#03130d" />
            </div>
            <span style={{ fontSize: 20, fontWeight: 600 }}>Console Mode</span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
            <Badge label="LB" />
            {["Home", "Session", "System"].map((t, i) => (
              <div key={t} style={{ padding: "10px 20px", fontSize: 21, fontWeight: 600, opacity: i === tab ? 1 : 0.55, position: "relative" }}>
                {t}
                {i === tab && <div style={{ position: "absolute", left: 20, right: 20, bottom: -2, height: 3, borderRadius: 2, background: ACCENT }} />}
              </div>
            ))}
            <Badge label="RB" lit={rb(100) || rb(180)} />
          </div>
          <div style={{ flex: 1, display: "flex", justifyContent: "flex-end", alignItems: "center", gap: 16 }}>
            <div style={{ width: 52, height: 52, borderRadius: 26, background: "rgba(255,255,255,0.1)", display: "flex", alignItems: "center", justifyContent: "center" }}>
              <G name="monitor" size={22} />
            </div>
            <G name="controller" size={24} />
            <span style={{ fontSize: 22, fontWeight: 600 }}>21:52</span>
          </div>
        </div>
        {/* Pages */}
        <div style={{ position: "absolute", left: 56, right: 56, top: 104, ...pageStyle }}>
          {tab === 0 && (
            <>
              <div
                style={{
                  minHeight: 220,
                  borderRadius: 20,
                  padding: "28px 40px",
                  boxSizing: "border-box",
                  background: "linear-gradient(135deg,#1F6B4A 0%,#17362F 55%,#10191D 100%)",
                  display: "flex",
                  alignItems: "center",
                  fontFamily: SANS,
                  color: "#fff",
                }}
              >
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 20, color: SUB }}>Where do you want to play?</div>
                  <div style={{ fontSize: 34, fontWeight: 600, marginTop: 8 }}>Play on Living room TV and turn off 2 screens</div>
                  <div style={{ display: "flex", alignItems: "center", gap: 10, fontSize: 20, marginTop: 14 }}>
                    <G name="play" size={20} /> Steam Big Picture
                  </div>
                </div>
                <div
                  style={{
                    minWidth: 280,
                    height: 64,
                    borderRadius: 14,
                    background: ACCENT,
                    color: "#03130d",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    gap: 12,
                    fontSize: 24,
                    fontWeight: 600,
                    outline: near(homePos, 0) > 0.5 ? "4px solid #fff" : "4px solid transparent",
                    outlineOffset: 4,
                  }}
                >
                  <G name="play" size={22} color="#03130d" /> Play now
                </div>
              </div>
              <div style={{ fontFamily: SANS, fontSize: 22, fontWeight: 600, color: SUB, margin: "34px 0 16px" }}>Current setup</div>
              <div style={{ display: "flex", gap: 16 }}>
                {cards.map((c, i) => (
                  <ConsoleCard key={c.label} w={300} h={132} focus={near(homePos, i + 1)} style={{ display: "flex", gap: 16, alignItems: "center" }}>
                    <Circle glyph={c.glyph} />
                    <div style={{ fontFamily: SANS, color: "#fff" }}>
                      <div style={{ fontSize: 14, color: SUB }}>{c.label}</div>
                      <div style={{ fontSize: 20, fontWeight: 600, lineHeight: 1.2 }}>{c.value}</div>
                    </div>
                  </ConsoleCard>
                ))}
              </div>
            </>
          )}
          {tab === 1 && (
            <>
              <div style={{ fontFamily: SANS, fontSize: 22, fontWeight: 600, color: SUB, marginBottom: 16 }}>Screens</div>
              <div style={{ display: "flex", gap: 16 }}>
                {[
                  { name: "Living room TV", role: "play" as const, pill: "Play here", detail: "3840 × 2160 · 120 Hz" },
                  { name: "Monitor 1", role: "off" as const, pill: "Turn off", detail: "2560 × 1440" },
                  { name: "Monitor 2", role: "off" as const, pill: "Turn off", detail: "2560 × 1440" },
                ].map((m, i) => (
                  <div
                    key={m.name}
                    style={{
                      width: 300,
                      height: 204,
                      padding: 18,
                      boxSizing: "border-box",
                      background: m.role === "play" ? "linear-gradient(135deg,#1F6B4A,#10362B)" : "linear-gradient(135deg,#262D34,#151A1F)",
                      opacity: m.role === "off" ? 0.55 + 0.45 * near(sessPos, i) : 1,
                      display: "flex",
                      flexDirection: "column",
                      justifyContent: "space-between",
                      fontFamily: SANS,
                      color: "#fff",
                      ...focusStyle(near(sessPos, i)),
                    }}
                  >
                    <MonitorArt role={m.role} />
                    <div>
                      <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                        <span style={{ fontSize: 22, fontWeight: 600 }}>{m.name}</span>
                        <span
                          style={{
                            padding: "2px 10px",
                            borderRadius: 11,
                            fontSize: 14,
                            fontWeight: 600,
                            background: m.role === "play" ? "#6FDDB9" : "rgba(255,255,255,0.15)",
                            color: m.role === "play" ? "#0B1F18" : "#DDE3E8",
                          }}
                        >
                          {m.pill}
                        </span>
                      </div>
                      <div style={{ fontSize: 14, color: SUB, marginTop: 4 }}>{m.detail}</div>
                    </div>
                  </div>
                ))}
              </div>
              <div style={{ fontFamily: SANS, fontSize: 22, fontWeight: 600, color: SUB, margin: "30px 0 16px" }}>Quick settings</div>
              <div style={{ display: "flex", gap: 16 }}>
                {(
                  [
                    ["play", "Open", "Big Picture"],
                    ["volume", "Audio", "LG TV"],
                    ["monitor", "Resolution", "4K · 120 Hz"],
                    ["hdr", "HDR", "On"],
                    ["vrr", "VRR", "On"],
                  ] as [Glyph, string, string][]
                ).map(([g, l, v]) => (
                  <ConsoleCard key={l} w={252} h={176} focus={0} style={{ display: "flex", flexDirection: "column", justifyContent: "space-between" }}>
                    <G name={g} size={28} />
                    <div style={{ fontFamily: SANS, color: "#fff" }}>
                      <div style={{ fontSize: 15, color: SUB }}>{l}</div>
                      <div style={{ fontSize: 22, fontWeight: 600 }}>{v}</div>
                    </div>
                  </ConsoleCard>
                ))}
              </div>
            </>
          )}
          {tab === 2 && (
            <div style={{ display: "flex", flexDirection: "column", gap: 10, maxWidth: 1000 }}>
              <div style={{ fontFamily: SANS, fontSize: 22, fontWeight: 600, color: SUB, marginBottom: 6 }}>Optional extras</div>
              {[
                ["Background", bg ? "Gradient only" : "Steam covers"],
                ["Interface sounds", "On"],
                ["Close Steam when going back to the PC", "On"],
              ].map(([l, v], i) => (
                <div
                  key={l}
                  style={{
                    height: 76,
                    borderRadius: 16,
                    padding: "0 24px",
                    background: CARD,
                    display: "flex",
                    alignItems: "center",
                    fontFamily: SANS,
                    fontSize: 20,
                    color: "#fff",
                    outline: i === 0 ? "3px solid #fff" : "3px solid transparent",
                    outlineOffset: 5,
                  }}
                >
                  <span style={{ flex: 1 }}>{l}</span>
                  <span style={{ fontWeight: 600, color: ACCENT }}>{v}</span>
                </div>
              ))}
              <div style={{ fontFamily: SANS, fontSize: 22, fontWeight: 600, color: SUB, margin: "18px 0 6px" }}>App</div>
              <div style={{ height: 76, borderRadius: 16, padding: "0 24px", background: CARD, display: "flex", alignItems: "center", fontFamily: SANS, fontSize: 20, color: "#fff" }}>
                <span style={{ flex: 1 }}>Receive test versions (alpha and beta)</span>
                <span style={{ fontWeight: 600, color: ACCENT }}>On</span>
              </div>
            </div>
          )}
        </div>
        {/* Hints */}
        <div style={{ position: "absolute", right: 56, bottom: 32 }}>
          <Hints
            items={[
              ["LB RB", "Tabs"],
              ["A", "Select"],
              ["B", "Back"],
              ["☰", "Play now"],
              ["Y", "Settings"],
            ]}
          />
        </div>
      </Stage>
      <Caption pos="bottomLeft" text="Home: where you play and how." at={6} until={98} />
      <Caption pos="bottomLeft" text="Session: what happens to each screen." at={104} until={178} />
      <Caption pos="bottomLeft" text="System: background, sounds and more." at={184} until={240} />
    </AbsoluteFill>
  );
};

/** 810–900: the covers background on its own. */
const Covers: React.FC = () => {
  const f = useCurrentFrame();
  return (
    <AbsoluteFill>
      <div style={{ position: "absolute", inset: 0, scale: String(1.1 - f * 0.0008) }}>
        <CoversBackground drift={f * 0.8} veil={0.55} />
      </div>
      <AbsoluteFill style={{ background: "radial-gradient(ellipse at center, rgba(10,14,18,0.55), rgba(10,14,18,0.2))" }} />
      <Center>
        <Line words={["Your", "Steam", "library,", "as", "the", { t: "backdrop.", grad: true }]} at={4} size={110} stagger={4} style={{ textShadow: "0 10px 50px rgba(0,0,0,0.6)" }} />
        <div style={{ height: 22 }} />
        <Line words={["Covers", "of", "the", "games", "you", "installed.", "Nothing", "bundled."]} at={20} size={44} weight={600} stagger={2} color="rgba(245,245,247,0.85)" />
        <div style={{ height: 34 }} />
        <div style={{ display: "flex", justifyContent: "center", gap: 14 }}>
          {["Steam covers", "Gradient only", "My picture"].map((c, i) => (
            <span
              key={c}
              style={{
                padding: "12px 24px",
                borderRadius: 999,
                fontFamily: SANS,
                fontSize: 28,
                fontWeight: 600,
                color: i === 0 ? "#03130d" : C.text,
                background: i === 0 ? ACCENT : "rgba(12,16,20,0.7)",
                border: `1px solid ${i === 0 ? ACCENT : "rgba(255,255,255,0.2)"}`,
                ...enter(f, 34 + i * 5, 0, 20),
              }}
            >
              {c}
            </span>
          ))}
        </div>
      </Center>
    </AbsoluteFill>
  );
};

/** 900–1080: the first-run shortcut setup, capturing each combination. */
const Shortcuts: React.FC = () => {
  const f = useCurrentFrame();
  const rows = [
    { name: "Open Console Mode", start: 26, parts: ["Xbox"] },
    { name: "Session menu", start: 70, parts: ["Select", "Select + Y"] },
    { name: "Back to the PC", start: 116, parts: ["Start", "Start + Select"] },
  ];
  const dialogIn = tw(f, [4, 20], [0, 1]);
  return (
    <AbsoluteFill>
      <Glow color={C.mint} y={640} opacity={0.12} />
      <Center top={70}>
        <Line words={["Your", "shortcuts,", { t: "your", grad: true }, { t: "buttons.", grad: true }]} at={2} size={96} stagger={4} />
      </Center>
      <div
        style={{
          position: "absolute",
          left: 960 - 470,
          top: 250,
          width: 940,
          borderRadius: 12,
          background: "#2B2B2B",
          border: "1px solid rgba(255,255,255,0.1)",
          boxShadow: "0 40px 120px rgba(0,0,0,0.7)",
          fontFamily: SANS,
          color: "#fff",
          overflow: "hidden",
          opacity: dialogIn,
          scale: String(0.96 + dialogIn * 0.04),
        }}
      >
        <div style={{ padding: "34px 40px 26px" }}>
          <div style={{ fontSize: 34, fontWeight: 600 }}>Set up your controller shortcuts</div>
          <div style={{ fontSize: 22, color: SUB, marginTop: 12, lineHeight: 1.4 }}>
            No shortcut is on until you choose it. Press Set, hold the buttons and let go. The same combination can’t be used for two actions.
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: 22, marginTop: 30 }}>
            {rows.map((r) => {
              const t = f - r.start;
              const capturing = t >= 0 && t < 36;
              const saved = t >= 36;
              let value = "Not set";
              if (capturing) value = t < 10 ? "Hold the buttons, then let go…" : r.parts[t < 22 ? 0 : r.parts.length - 1];
              if (saved) value = r.parts[r.parts.length - 1];
              return (
                <div key={r.name}>
                  <div style={{ fontSize: 24, fontWeight: 600 }}>{r.name}</div>
                  <div style={{ display: "flex", alignItems: "center", gap: 12, marginTop: 10 }}>
                    <div
                      style={{
                        minWidth: 300,
                        fontSize: 24,
                        fontWeight: saved || (capturing && t >= 10) ? 700 : 400,
                        color: value === "Not set" ? "rgba(255,255,255,0.6)" : capturing && t < 10 ? SUB : "#fff",
                      }}
                    >
                      {value}
                    </div>
                    <div
                      style={{
                        padding: "8px 22px",
                        borderRadius: 6,
                        fontSize: 20,
                        background: capturing ? "rgba(255,255,255,0.14)" : "rgba(255,255,255,0.08)",
                        border: "1px solid rgba(255,255,255,0.14)",
                      }}
                    >
                      {capturing ? "Cancel" : saved ? "Change" : "Set"}
                    </div>
                    <div style={{ padding: "8px 22px", borderRadius: 6, fontSize: 20, background: "rgba(255,255,255,0.08)", border: "1px solid rgba(255,255,255,0.14)", opacity: 0.8 }}>Remove</div>
                    {saved && (
                      <div style={{ marginLeft: 12 }}>
                        <Check p={tw(t, [36, 46], [0, 1])} size={40} />
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
        <div style={{ background: "#202020", padding: "20px 40px", display: "flex", gap: 14, justifyContent: "flex-end" }}>
          <div style={{ padding: "10px 24px", borderRadius: 6, fontSize: 20, background: "rgba(255,255,255,0.08)", border: "1px solid rgba(255,255,255,0.14)" }}>Use the suggested shortcuts</div>
          <div style={{ padding: "10px 40px", borderRadius: 6, fontSize: 20, fontWeight: 600, background: ACCENT, color: "#03130d" }}>Done</div>
        </div>
      </div>
    </AbsoluteFill>
  );
};

/** 1080–1230: the rest, as a grid of tiles. */
const More: React.FC = () => {
  const f = useCurrentFrame();
  const items: { glyph: Glyph; title: string; text: string }[] = [
    { glyph: "sound", title: "Interface sounds", text: "Soft sounds as you move, pick and go back." },
    { glyph: "steam", title: "Steam closes cleanly", text: "Its own Exit when you go back to the PC. Never forced." },
    { glyph: "rotate", title: "Portrait screens", text: "Rotated monitors come back in portrait." },
    { glyph: "play", title: "Slow Playnite starts", text: "No more early trip back to the desk." },
    { glyph: "gamepad", title: "Steadier controllers", text: "Pads sending impossible readings are ignored." },
    { glyph: "flask", title: "Test versions", text: "Opt in from Settings. Off by default." },
  ];
  const W = 520;
  const H = 250;
  const GAP = 24;
  const left = (1920 - (3 * W + 2 * GAP)) / 2;
  return (
    <AbsoluteFill>
      <Glow color={C.cyan} y={640} opacity={0.12} size={1800} />
      <Center top={90}>
        <Line words={["And", "a", "lot", "of", { t: "polish.", grad: true }]} at={2} size={100} stagger={4} />
      </Center>
      {items.map((it, i) => {
        const t = f - 12 - i * 5;
        return (
          <div
            key={it.title}
            style={{
              position: "absolute",
              left: left + (i % 3) * (W + GAP),
              top: 300 + Math.floor(i / 3) * (H + GAP),
              width: W,
              height: H,
              borderRadius: 32,
              padding: 36,
              boxSizing: "border-box",
              background: "linear-gradient(180deg,#161618,#0c0c0e)",
              border: `1px solid ${C.line}`,
              opacity: tw(t, [0, 10], [0, 1], Easing.linear),
              scale: String(tw(t, [0, 20], [0.93, 1])),
              filter: `blur(${tw(t, [0, 14], [10, 0])}px)`,
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
            }}
          >
            <div style={{ width: 64, height: 64, borderRadius: 32, background: `${C.mint}22`, display: "flex", alignItems: "center", justifyContent: "center" }}>
              <G name={it.glyph} size={32} color={C.mint} />
            </div>
            <div style={{ fontFamily: SANS }}>
              <div style={{ fontSize: 38, fontWeight: 700, color: C.text, letterSpacing: "-0.02em" }}>{it.title}</div>
              <div style={{ fontSize: 24, fontWeight: 500, color: C.muted, marginTop: 6 }}>{it.text}</div>
            </div>
          </div>
        );
      })}
    </AbsoluteFill>
  );
};

const Outro: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const pop = spring({ frame: f, fps, config: { damping: 14, stiffness: 110 } });
  const end = tw(f, [118, 150], [0, 1], Easing.inOut(Easing.cubic));
  return (
    <AbsoluteFill style={{ opacity: 1 - end }}>
      <Glow color={C.mint} y={470} opacity={0.2 + pop * 0.08} size={1600} />
      <Center top={170}>
        <div style={{ display: "flex", justifyContent: "center", scale: String(0.6 + pop * 0.4) }}>
          <Logo size={180} draw={1} glow={pop} />
        </div>
        <div style={{ height: 36 }} />
        <Line words={["Console", "Mode", { t: "1.6", grad: true }, { t: "alpha.3", grad: true }]} at={4} size={140} weight={800} stagger={4} />
        <div style={{ height: 26 }} />
        <Line words={["Try", "it:", "Settings", "→", "Receive", "test", "versions", "(alpha", "and", "beta)."]} at={24} size={44} weight={600} stagger={2} color={C.muted} />
      </Center>
      <div style={{ position: "absolute", top: 880, left: 0, right: 0, textAlign: "center", fontFamily: SANS, fontSize: 34, fontWeight: 600, color: C.mint, opacity: tw(f, [50, 62], [0, 1], Easing.linear) }}>
        github.com/lippdev/consolemode
      </div>
    </AbsoluteFill>
  );
};

// ───────────────────────── film ─────────────────────────

/** Blurs and zooms out into black over the last frames; fades in from black unless `cut`. */
const Part: React.FC<{ t: { from: number; dur: number }; cutIn?: boolean; cutOut?: boolean; children: React.ReactNode }> = ({ t, cutIn, cutOut, children }) => (
  <Sequence from={t.from} durationInFrames={t.dur}>
    <PartFx dur={t.dur} cutIn={!!cutIn} cutOut={!!cutOut}>
      {children}
    </PartFx>
  </Sequence>
);

const PartFx: React.FC<{ dur: number; cutIn: boolean; cutOut: boolean; children: React.ReactNode }> = ({ dur, cutIn, cutOut, children }) => {
  const f = useCurrentFrame();
  const out = cutOut ? 0 : tw(f, [dur - 12, dur], [0, 1], Easing.in(Easing.cubic));
  return (
    <AbsoluteFill
      style={{
        opacity: (cutIn ? 1 : tw(f, [0, 8], [0, 1], Easing.linear)) * (1 - out),
        filter: out > 0 ? `blur(${out * 16}px)` : undefined,
        scale: String(1 + out * 0.06),
      }}
    >
      {children}
    </AbsoluteFill>
  );
};

export const Alpha3: React.FC = () => {
  useFonts();
  return (
    <AbsoluteFill style={{ background: C.bg, overflow: "hidden" }}>
      <Audio src={staticFile("keynote/alpha3.mp3")} />
      <Part t={A.title}>
        <Title />
      </Part>
      <Part t={A.intro} cutOut>
        <MenuIntro />
      </Part>
      <Part t={A.menu} cutIn>
        <SessionMenu />
      </Part>
      <Part t={A.consoleIntro}>
        <ConsoleIntro />
      </Part>
      <Part t={A.console}>
        <ConsoleUi />
      </Part>
      <Part t={A.covers}>
        <Covers />
      </Part>
      <Part t={A.shortcuts}>
        <Shortcuts />
      </Part>
      <Part t={A.more}>
        <More />
      </Part>
      <Part t={A.outro} cutOut>
        <Outro />
      </Part>
    </AbsoluteFill>
  );
};

