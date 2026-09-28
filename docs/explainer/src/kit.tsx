import "@fontsource-variable/inter";
import "@fontsource-variable/jetbrains-mono";
import React, { useEffect, useState } from "react";
import { AbsoluteFill, continueRender, delayRender, Easing, interpolate, Sequence, useCurrentFrame } from "remotion";

// Shared look of the Keynote film and the Features GIF: black stage, Inter, mint accents.

export const C = {
  bg: "#000000",
  text: "#F5F5F7",
  muted: "#86868B",
  mint: "#3ECFA0",
  cyan: "#38C6FF",
  violet: "#8B6CFF",
  tile: "#101012",
  line: "rgba(255,255,255,0.10)",
};
export const GRAD = `linear-gradient(90deg, ${C.mint} 0%, ${C.cyan} 35%, ${C.violet} 65%, ${C.mint} 100%)`;
export const WARM = "linear-gradient(90deg, #FFD37A 0%, #FF7AB6 45%, #8B6CFF 80%, #FFD37A 100%)";
export const SANS = "'Inter Variable', Inter, system-ui, sans-serif";
export const MONO = "'JetBrains Mono Variable', ui-monospace, monospace";

export const EXPO = Easing.bezier(0.16, 1, 0.3, 1);
export const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;
export const tw = (f: number, input: [number, number], output: [number, number], easing = EXPO) =>
  interpolate(f, input, output, { ...clamp, easing });

/** Scene timings in frames. */

// ───────────────────────── building blocks ─────────────────────────

/** Gradient-filled text; the gradient slowly drifts so the colour feels alive. */
export const gradText = (f: number, gradient = GRAD): React.CSSProperties => ({
  backgroundImage: gradient,
  backgroundSize: "300% 100%",
  backgroundPosition: `${(f * 0.35) % 300}% 0`,
  WebkitBackgroundClip: "text",
  backgroundClip: "text",
  color: "transparent",
  paddingBottom: "0.1em",
  marginBottom: "-0.1em",
});

export type Word = string | { t: string; grad?: boolean; warm?: boolean; muted?: boolean };

/** A line of type whose words arrive one after another, out of a blur. */
export const Line: React.FC<{
  words: Word[];
  at?: number;
  size: number;
  weight?: number;
  stagger?: number;
  color?: string;
  align?: "center" | "flex-start";
  style?: React.CSSProperties;
}> = ({ words, at = 0, size, weight = 700, stagger = 4, color = C.text, align = "center", style }) => {
  const f = useCurrentFrame();
  return (
    <div
      style={{
        display: "flex",
        flexWrap: "wrap",
        justifyContent: align,
        columnGap: size * 0.26,
        fontFamily: SANS,
        fontSize: size,
        fontWeight: weight,
        letterSpacing: "-0.035em",
        lineHeight: 1.05,
        color,
        ...style,
      }}
    >
      {words.map((w, i) => {
        const word = typeof w === "string" ? { t: w } : w;
        const t = f - at - i * stagger;
        const extra = word.grad ? gradText(f) : word.warm ? gradText(f, WARM) : word.muted ? { color: C.muted } : {};
        return (
          <span
            key={i}
            style={{
              display: "inline-block",
              opacity: tw(t, [0, 12], [0, 1], Easing.linear),
              filter: `blur(${tw(t, [0, 18], [18, 0])}px)`,
              translate: `0 ${tw(t, [0, 22], [size * 0.3, 0])}px`,
              ...extra,
            }}
          >
            {word.t}
          </span>
        );
      })}
    </div>
  );
};

/** Wraps a scene: a soft fade in, then blur + zoom out into black. */
export const Scene: React.FC<{ from: number; dur: number; children: React.ReactNode }> = ({ from, dur, children }) => (
  <Sequence from={from} durationInFrames={dur}>
    <SceneFx dur={dur}>{children}</SceneFx>
  </Sequence>
);

export const SceneFx: React.FC<{ dur: number; children: React.ReactNode }> = ({ dur, children }) => {
  const f = useCurrentFrame();
  const out = tw(f, [dur - 14, dur], [0, 1], Easing.in(Easing.cubic));
  return (
    <AbsoluteFill
      style={{
        opacity: tw(f, [0, 8], [0, 1], Easing.linear) * (1 - out),
        filter: out > 0 ? `blur(${out * 16}px)` : undefined,
        scale: String(1 + out * 0.06),
      }}
    >
      {children}
    </AbsoluteFill>
  );
};

/** A dim coloured light behind the content. */
export const Glow: React.FC<{ color?: string; x?: number; y?: number; size?: number; opacity?: number }> = ({
  color = C.mint,
  x = 960,
  y = 700,
  size = 1300,
  opacity = 0.22,
}) => {
  const f = useCurrentFrame();
  const breathe = 1 + Math.sin(f / 40) * 0.05;
  return (
    <div
      style={{
        position: "absolute",
        left: x - (size * breathe) / 2,
        top: y - (size * breathe * 0.6) / 2,
        width: size * breathe,
        height: size * breathe * 0.6,
        borderRadius: "50%",
        background: `radial-gradient(closest-side, ${color}, transparent)`,
        opacity,
        filter: "blur(40px)",
      }}
    />
  );
};

export const Center: React.FC<{ top?: number; children: React.ReactNode }> = ({ top, children }) => (
  <div
    style={{
      position: "absolute",
      left: 0,
      right: 0,
      top,
      ...(top === undefined ? { top: 0, bottom: 0, display: "flex", flexDirection: "column", justifyContent: "center" } : {}),
    }}
  >
    {children}
  </div>
);

/** The app icon (monitor with a prompt, console base with a power button), drawn in strokes. */
export const Logo: React.FC<{ size: number; draw: number; glow?: number }> = ({ size, draw, glow = 0 }) => {
  const seg = (a: number, b: number) => {
    const p = interpolate(draw, [a, b], [0, 1], clamp);
    return { strokeDasharray: 1, strokeDashoffset: 1 - p, opacity: p > 0 ? 1 : 0 };
  };
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 256 256"
      style={{ overflow: "visible", filter: glow > 0 ? `drop-shadow(0 0 ${30 * glow}px ${C.mint}aa)` : undefined }}
    >
      <rect x="22" y="14" width="212" height="150" rx="24" fill="none" stroke={C.text} strokeWidth="14" pathLength={1} style={seg(0, 0.45)} />
      <path d="M72 62 L112 90 L72 118" fill="none" stroke={C.mint} strokeWidth="16" strokeLinecap="round" strokeLinejoin="round" pathLength={1} style={seg(0.35, 0.6)} />
      <path d="M124 122 L176 122" fill="none" stroke={C.mint} strokeWidth="16" strokeLinecap="round" pathLength={1} style={seg(0.5, 0.7)} />
      <rect x="6" y="182" width="244" height="62" rx="16" fill="#1c1c1f" fillOpacity={interpolate(draw, [0.6, 1], [0, 1], clamp)} stroke={C.text} strokeWidth="12" pathLength={1} style={seg(0.45, 0.85)} />
      <path d="M34 213 L54 213" stroke={C.mint} strokeWidth="12" strokeLinecap="round" pathLength={1} style={seg(0.8, 0.95)} />
      <path d="M200 203 A14 14 0 1 0 220 203" fill="none" stroke={C.mint} strokeWidth="9" strokeLinecap="round" pathLength={1} style={seg(0.82, 1)} />
      <path d="M210 196 L210 212" stroke={C.mint} strokeWidth="9" strokeLinecap="round" pathLength={1} style={seg(0.85, 1)} />
    </svg>
  );
};

/** A stylised game: dusk sky, sun, parallax hills. */
export const Game: React.FC<{ dim?: number }> = ({ dim = 0 }) => {
  const f = useCurrentFrame();
  return (
    <AbsoluteFill style={{ background: "linear-gradient(180deg,#141d3b 0%,#3a4f86 38%,#d9866b 78%,#f3c27a 100%)", overflow: "hidden" }}>
      <div style={{ position: "absolute", left: "62%", top: "30%", width: "13%", aspectRatio: "1", borderRadius: "50%", background: "#ffe3b0", boxShadow: "0 0 120px 40px #ffb86a88" }} />
      {[0, 1, 2].map((i) => (
        <div
          key={i}
          style={{
            position: "absolute",
            left: `${-30 + i * 35 - ((f * (0.08 + i * 0.05)) % 40)}%`,
            bottom: `${-28 + i * 6}%`,
            width: "95%",
            height: `${70 - i * 12}%`,
            borderRadius: "50% 50% 0 0",
            background: ["#1b2a3a", "#223a3c", "#2c4a3e"][i],
          }}
        />
      ))}
      {[0, 1, 2].map((i) => (
        <div
          key={`b${i}`}
          style={{
            position: "absolute",
            left: `${95 + i * 30 - ((f * (0.13 + i * 0.05)) % 140)}%`,
            bottom: `${-22 + i * 4}%`,
            width: "80%",
            height: `${48 - i * 8}%`,
            borderRadius: "50% 50% 0 0",
            background: ["#1b2a3a", "#223a3c", "#2c4a3e"][i],
          }}
        />
      ))}
      <AbsoluteFill style={{ background: `rgba(0,0,0,${dim})` }} />
    </AbsoluteFill>
  );
};

/** Windows-like desktop wallpaper, optionally with the Console Mode window on it. */
export const Desktop: React.FC<{ app?: boolean; pressed?: number }> = ({ app, pressed = 0 }) => (
  <AbsoluteFill style={{ background: "radial-gradient(120% 90% at 30% 110%, #3656d9 0%, #1d2a6b 45%, #0b1030 100%)" }}>
    <div style={{ position: "absolute", left: "8%", top: "10%", width: "46%", height: "44%", borderRadius: 6, background: "rgba(255,255,255,0.10)", border: "1px solid rgba(255,255,255,0.16)" }} />
    <div style={{ position: "absolute", left: "44%", top: "32%", width: "44%", height: "40%", borderRadius: 6, background: "rgba(255,255,255,0.08)", border: "1px solid rgba(255,255,255,0.14)" }} />
    {app && (
      <div
        style={{
          position: "absolute",
          left: "22%",
          top: "18%",
          width: "56%",
          height: "62%",
          borderRadius: 8,
          background: "#1d1d21",
          border: "1px solid rgba(255,255,255,0.18)",
          boxShadow: "0 12px 30px rgba(0,0,0,0.5)",
        }}
      >
        <div style={{ height: "18%", display: "flex", alignItems: "center", gap: 6, paddingLeft: 10 }}>
          <Logo size={16} draw={1} />
          <span style={{ fontFamily: SANS, fontSize: 11, color: C.text, fontWeight: 600 }}>Console Mode</span>
        </div>
        <div
          style={{
            position: "absolute",
            left: "50%",
            top: "70%",
            translate: "-50% -50%",
            scale: String(1 - pressed * 0.1),
            padding: "8px 22px",
            borderRadius: 999,
            background: C.mint,
            color: "#04110c",
            fontFamily: SANS,
            fontWeight: 700,
            fontSize: 14,
            whiteSpace: "nowrap",
            boxShadow: `0 0 ${18 + pressed * 20}px ${C.mint}88`,
          }}
        >
          Play now
        </div>
        <div style={{ position: "absolute", left: "12%", top: "28%", width: "76%", height: 8, borderRadius: 4, background: "rgba(255,255,255,0.12)" }} />
        <div style={{ position: "absolute", left: "12%", top: "40%", width: "50%", height: 8, borderRadius: 4, background: "rgba(255,255,255,0.08)" }} />
      </div>
    )}
    <div style={{ position: "absolute", left: 0, right: 0, bottom: 0, height: "8%", background: "rgba(20,20,30,0.75)" }} />
  </AbsoluteFill>
);

/** A screen with a bezel. `power` 0 = off, 1 = on; `tvOn` plays the power-on flash. */
export const Screen: React.FC<{
  x: number;
  y: number;
  w: number;
  h: number;
  power: number;
  kind: "monitor" | "tv";
  children: React.ReactNode;
}> = ({ x, y, w, h, power, kind, children }) => (
  <div style={{ position: "absolute", left: x, top: y, width: w }}>
    <div
      style={{
        width: w,
        height: h,
        padding: kind === "tv" ? 10 : 8,
        boxSizing: "border-box",
        borderRadius: kind === "tv" ? 10 : 14,
        background: "#0a0a0c",
        border: "1.5px solid rgba(255,255,255,0.16)",
        boxShadow: power > 0.5 ? "0 30px 90px rgba(80,140,255,0.18)" : "0 30px 60px rgba(0,0,0,0.6)",
      }}
    >
      <div style={{ position: "relative", width: "100%", height: "100%", overflow: "hidden", borderRadius: 4, background: "#000" }}>
        {children}
        <AbsoluteFill style={{ background: "#000", opacity: 1 - power }} />
      </div>
    </div>
    {kind === "monitor" ? (
      <>
        <div style={{ width: 40, height: 60, margin: "0 auto", background: "linear-gradient(180deg,#2a2a2e,#18181b)" }} />
        <div style={{ width: 170, height: 10, margin: "0 auto", borderRadius: 5, background: "#2a2a2e" }} />
      </>
    ) : (
      <div style={{ display: "flex", justifyContent: "space-between", padding: "0 18%" }}>
        <div style={{ width: 60, height: 26, background: "#2a2a2e", clipPath: "polygon(40% 0,60% 0,100% 100%,0 100%)" }} />
        <div style={{ width: 60, height: 26, background: "#2a2a2e", clipPath: "polygon(40% 0,60% 0,100% 100%,0 100%)" }} />
      </div>
    )}
  </div>
);

export const Cursor: React.FC<{ x: number; y: number; scale?: number }> = ({ x, y, scale = 1 }) => (
  <svg width="34" height="44" viewBox="0 0 17 22" style={{ position: "absolute", left: x, top: y, scale: String(scale), transformOrigin: "0 0", filter: "drop-shadow(0 4px 6px rgba(0,0,0,0.6))" }}>
    <path d="M1 1 L1 17 L5 13 L8 20 L11 19 L8 12 L14 12 Z" fill="#fff" stroke="#000" strokeWidth="1.2" strokeLinejoin="round" />
  </svg>
);

export const Check: React.FC<{ p: number; size?: number }> = ({ p, size = 56 }) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: size / 2,
      background: `rgba(62,207,160,${0.15 + p * 0.85})`,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      scale: String(0.6 + 0.4 * p),
      boxShadow: p > 0.9 ? `0 0 30px ${C.mint}88` : undefined,
    }}
  >
    <svg width={size * 0.55} height={size * 0.55} viewBox="0 0 24 24">
      <path d="M4 12.5 L10 18 L20 6" fill="none" stroke="#03130d" strokeWidth="3.4" strokeLinecap="round" strokeLinejoin="round" pathLength={1} strokeDasharray={1} strokeDashoffset={1 - p} />
    </svg>
  </div>
);

export type IconName = "screen" | "speaker" | "resize" | "volume" | "tv" | "fps" | "hdr" | "exit" | "play" | "menu" | "stop";

export const Icon: React.FC<{ name: IconName; size?: number; color?: string }> = ({ name, size = 48, color = C.text }) => {
  const p = { fill: "none", stroke: color, strokeWidth: 2, strokeLinecap: "round" as const, strokeLinejoin: "round" as const };
  const paths: Record<IconName, React.ReactNode> = {
    screen: (
      <>
        <rect x="3" y="4" width="18" height="12" rx="2" {...p} />
        <path d="M9 20h6M12 16v4" {...p} />
      </>
    ),
    speaker: (
      <>
        <path d="M4 9h4l5-4v14l-5-4H4z" {...p} />
        <path d="M16.5 8.5a5 5 0 0 1 0 7M19 6a8.5 8.5 0 0 1 0 12" {...p} />
      </>
    ),
    resize: (
      <>
        <path d="M4 9V4h5M20 15v5h-5M4 4l6 6M20 20l-6-6" {...p} />
      </>
    ),
    volume: (
      <>
        <path d="M4 9h4l5-4v14l-5-4H4z" {...p} />
        <path d="M17 9.5a3.5 3.5 0 0 1 0 5" {...p} />
      </>
    ),
    tv: (
      <>
        <rect x="2.5" y="5" width="19" height="12" rx="1.5" {...p} />
        <path d="M7 20h10" {...p} />
      </>
    ),
    fps: <path d="M4 18a8 8 0 1 1 16 0M12 18l4-6" {...p} />,
    hdr: (
      <>
        <circle cx="12" cy="12" r="4.5" {...p} />
        <path d="M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.3 5.3l1.4 1.4M17.3 17.3l1.4 1.4M5.3 18.7l1.4-1.4M17.3 6.7l1.4-1.4" {...p} />
      </>
    ),
    exit: <path d="M14 4h5v16h-5M10 8l-4 4 4 4M6 12h10" {...p} />,
    play: <path d="M8 5l11 7-11 7z" fill={color} stroke={color} strokeWidth={2} strokeLinejoin="round" />,
    menu: <path d="M5 7h14M5 12h14M5 17h14" {...p} strokeWidth={2.4} />,
    stop: <rect x="6" y="6" width="12" height="12" rx="2" fill={color} />,
  };
  return (
    <svg width={size} height={size} viewBox="0 0 24 24">
      {paths[name]}
    </svg>
  );
};

/** The Desktop interface: the list of screens and Play now. */
export const DesktopWindow: React.FC<{ t?: { screens: string[]; play: string } }> = ({
  t = { screens: ["Living room TV", "Monitor 1", "Monitor 2"], play: "Play now" },
}) => {
  const W = 760;
  const H = 440;
  return (
    <div style={{ width: W, height: H, borderRadius: 18, background: "#1c1c1f", border: `1px solid ${C.line}`, overflow: "hidden", position: "relative" }}>
      <div style={{ height: 52, display: "flex", alignItems: "center", gap: 12, padding: "0 20px", borderBottom: `1px solid ${C.line}` }}>
        <Logo size={24} draw={1} />
        <span style={{ fontFamily: SANS, fontSize: 20, fontWeight: 600, color: C.text, flex: 1 }}>Console Mode</span>
        <span style={{ fontFamily: SANS, fontSize: 18, color: C.muted, letterSpacing: 18 }}>–□×</span>
      </div>
      <div style={{ padding: "26px 32px", display: "flex", flexDirection: "column", gap: 14 }}>
        {t.screens.map((s, i) => (
          <div key={s} style={{ display: "flex", alignItems: "center", gap: 16, padding: "14px 18px", borderRadius: 12, background: i === 0 ? "rgba(62,207,160,0.12)" : "rgba(255,255,255,0.04)", border: `1px solid ${i === 0 ? C.mint + "88" : C.line}` }}>
            <div style={{ width: 20, height: 20, borderRadius: 10, border: `2px solid ${i === 0 ? C.mint : C.muted}`, background: i === 0 ? `radial-gradient(${C.mint} 45%, transparent 50%)` : undefined }} />
            <span style={{ fontFamily: SANS, fontSize: 24, fontWeight: 600, color: C.text, flex: 1 }}>{s}</span>
            <span style={{ fontFamily: SANS, fontSize: 20, color: C.muted }}>{i === 0 ? "4K · 120 Hz" : "1440p"}</span>
          </div>
        ))}
      </div>
      <div style={{ position: "absolute", right: 32, bottom: 28, padding: "14px 36px", borderRadius: 999, background: C.mint, color: "#04110c", fontFamily: SANS, fontSize: 24, fontWeight: 700 }}>{t.play}</div>
    </div>
  );
};

/** The Console interface: big tiles for the controller, focus ring on Play. */
export const ConsoleWindow: React.FC<{ focus: number; t?: { tiles: string[]; select: string; back: string } }> = ({
  focus,
  t = { tiles: ["Play", "Screens", "Settings"], select: "Select", back: "Back" },
}) => {
  const W = 760;
  const H = 440;
  return (
    <div style={{ width: W, height: H, borderRadius: 18, background: "radial-gradient(120% 100% at 20% 0%, #1b2e2a, #050607)", border: `1px solid ${C.line}`, overflow: "hidden", position: "relative", padding: 40, boxSizing: "border-box" }}>
      <div style={{ fontFamily: SANS, fontSize: 40, fontWeight: 800, color: C.text, letterSpacing: "-0.03em" }}>Console Mode</div>
      <div style={{ display: "flex", gap: 22, marginTop: 36 }}>
        {(["play", "screen", "menu"] as IconName[]).map((ic, i) => (
          <div
            key={ic}
            style={{
              width: 206,
              height: 190,
              borderRadius: 22,
              background: i === 0 ? "linear-gradient(160deg,#3ecfa0,#1f9a76)" : "rgba(255,255,255,0.06)",
              border: i === 0 ? `3px solid rgba(255,255,255,${focus})` : `1px solid ${C.line}`,
              boxShadow: i === 0 ? `0 0 ${40 * focus}px ${C.mint}88` : undefined,
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
              padding: 22,
              boxSizing: "border-box",
            }}
          >
            <Icon name={ic} size={48} color={i === 0 ? "#03130d" : C.text} />
            <span style={{ fontFamily: SANS, fontSize: 28, fontWeight: 700, color: i === 0 ? "#03130d" : C.text }}>{t.tiles[i]}</span>
          </div>
        ))}
      </div>
      <div style={{ position: "absolute", left: 40, bottom: 30, display: "flex", gap: 30, fontFamily: SANS, fontSize: 22, color: C.muted, alignItems: "center" }}>
        {[
          ["A", "#5fd36f", t.select],
          ["B", "#ff6b6b", t.back],
        ].map(([b, col, t]) => (
          <span key={b} style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <span style={{ width: 34, height: 34, borderRadius: 17, border: `2px solid ${col}`, color: col, fontWeight: 800, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 18 }}>{b}</span>
            {t}
          </span>
        ))}
      </div>
    </div>
  );
};

/** Holds the render until Inter and JetBrains Mono are loaded. */
export const useFonts = () => {
  const [handle] = useState(() => delayRender("Loading fonts"));
  useEffect(() => {
    Promise.all([
      document.fonts.load(`800 100px 'Inter Variable'`),
      document.fonts.load(`500 40px 'JetBrains Mono Variable'`),
    ]).then(
      () => continueRender(handle),
      () => continueRender(handle),
    );
  }, [handle]);
};
