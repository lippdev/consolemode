import React from "react";
import {
  AbsoluteFill,
  Easing,
  Img,
  interpolate,
  Sequence,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { theme } from "./theme";

// "What's new in 1.5": the session menu (Select + Y) with horizontal tiles, the start-of-session
// toast, PlayStation pads, controller navigation in Settings, working updates, and the other apps.

const font = "Segoe UI, system-ui, sans-serif";

const ease = {
  extrapolateLeft: "clamp" as const,
  extrapolateRight: "clamp" as const,
  easing: Easing.bezier(0.22, 1, 0.36, 1),
};
const linear = { extrapolateLeft: "clamp" as const, extrapolateRight: "clamp" as const };

/** Scene timings in frames (30 fps). */
const S = {
  intro: { from: 0, dur: 60 },
  toast: { from: 60, dur: 105 },
  menu: { from: 165, dur: 165 },
  playstation: { from: 330, dur: 105 },
  settings: { from: 435, dur: 105 },
  update: { from: 540, dur: 75 },
  outro: { from: 615, dur: 90 },
};
export const WHATS_NEW_DURATION = S.outro.from + S.outro.dur;

// ───────────────────────── shared pieces ─────────────────────────

/** Fades a scene in and out over its own length. */
const SceneFade: React.FC<{ dur: number; children: React.ReactNode }> = ({ dur, children }) => {
  const f = useCurrentFrame();
  const o = interpolate(f, [0, 10, dur - 10, dur], [0, 1, 1, 0], linear);
  return <AbsoluteFill style={{ opacity: o }}>{children}</AbsoluteFill>;
};

const Caption: React.FC<{ kicker: string; text: string; dur: number }> = ({ kicker, text, dur }) => {
  const f = useCurrentFrame();
  const o = interpolate(f, [6, 18, dur - 14, dur - 4], [0, 1, 1, 0], linear);
  const y = interpolate(f, [6, 22], [14, 0], ease);
  return (
    <div
      style={{
        position: "absolute",
        left: 0,
        right: 0,
        bottom: 34,
        display: "flex",
        justifyContent: "center",
        opacity: o,
        translate: `0 ${y}px`,
        zIndex: 20,
      }}
    >
      <div
        style={{
          background: "rgba(12,14,18,0.86)",
          border: `1px solid ${theme.accent}55`,
          borderRadius: 14,
          padding: "12px 24px",
          fontFamily: font,
          color: theme.text,
          display: "flex",
          gap: 14,
          alignItems: "baseline",
          boxShadow: "0 10px 30px rgba(0,0,0,0.45)",
        }}
      >
        <span style={{ color: theme.accent, fontWeight: 700, fontSize: 18, letterSpacing: 0.4 }}>{kicker}</span>
        <span style={{ fontSize: 24, fontWeight: 600 }}>{text}</span>
      </div>
    </div>
  );
};

/** A stylised game frame: sky, hills, a HUD. Stands in for whatever is running on the TV. */
const GameBackdrop: React.FC<{ dim?: number }> = ({ dim = 0 }) => {
  const f = useCurrentFrame();
  const drift = f * 0.6;
  return (
    <AbsoluteFill style={{ background: "linear-gradient(180deg,#1e3a5f 0%,#3f6e8f 45%,#e0a96d 100%)", overflow: "hidden" }}>
      <div style={{ position: "absolute", left: 880 - drift * 0.2, top: 110, width: 120, height: 120, borderRadius: 60, background: "#ffd9a0", boxShadow: "0 0 80px #ffcf8a" }} />
      {[0, 1, 2].map((i) => (
        <div
          key={i}
          style={{
            position: "absolute",
            left: -200 + i * 520 - ((drift * (0.5 + i * 0.3)) % 520),
            bottom: -120 + i * 30,
            width: 900,
            height: 380 - i * 60,
            borderRadius: "50% 50% 0 0",
            background: ["#243b2f", "#2f4d3a", "#3b5e45"][i],
          }}
        />
      ))}
      {/* HUD */}
      <div style={{ position: "absolute", left: 36, top: 30, fontFamily: font, color: "white", fontSize: 20, fontWeight: 700, textShadow: "0 2px 6px #000" }}>
        ❤ ❤ ❤ &nbsp; <span style={{ opacity: 0.8, fontWeight: 500 }}>Chapter 3</span>
      </div>
      <AbsoluteFill style={{ background: `rgba(8,10,14,${dim})` }} />
    </AbsoluteFill>
  );
};

/** A face button badge like the app's HintBadge. */
const Badge: React.FC<{ label: string; lit?: number; size?: number; ink?: string }> = ({ label, lit = 0, size = 44, ink = theme.text }) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: size / 2,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontFamily: font,
      fontWeight: 800,
      fontSize: size * 0.42,
      color: lit > 0.5 ? "#0b1512" : ink,
      background: `rgba(62,207,160,${lit})`,
      border: `2px solid ${lit > 0.5 ? theme.accent : ink === theme.text ? "rgba(255,255,255,0.35)" : "rgba(0,0,0,0.35)"}`,
      boxShadow: lit > 0.5 ? `0 0 ${24 * lit}px ${theme.accent}` : "none",
      scale: 1 - lit * 0.08,
    }}
  >
    {label}
  </div>
);

/** Minimal controller silhouette with the two buttons of the combo. */
const Pad: React.FC<{ kind: "xbox" | "ps"; pressed: number }> = ({ kind, pressed }) => {
  const select = kind === "xbox" ? "⧉" : "▭";
  const y = kind === "xbox" ? "Y" : "△";
  const body = kind === "xbox" ? "#2b2d33" : "#e9ecf2";
  const ink = kind === "xbox" ? theme.text : "#1b1d22";
  return (
    <div style={{ position: "relative", width: 300, height: 190 }}>
      <div
        style={{
          position: "absolute",
          inset: 0,
          background: body,
          borderRadius: "90px 90px 120px 120px / 80px 80px 140px 140px",
          boxShadow: "0 18px 40px rgba(0,0,0,0.5), inset 0 -8px 0 rgba(0,0,0,0.15)",
        }}
      />
      {/* D-pad */}
      <div style={{ position: "absolute", left: 52, top: 70, width: 54, height: 54, color: ink, fontSize: 40, lineHeight: "54px", textAlign: "center", opacity: 0.6 }}>✚</div>
      {/* Select / Create */}
      <div style={{ position: "absolute", left: 112, top: 52 }}>
        <Badge label={select} lit={pressed} size={36} ink={ink} />
      </div>
      {/* Face buttons, Y / Triangle on top */}
      <div style={{ position: "absolute", left: 206, top: 42 }}>
        <Badge label={y} lit={pressed} size={40} ink={ink} />
      </div>
      <div style={{ position: "absolute", left: 206, top: 118, opacity: 0.55 }}>
        <Badge label={kind === "xbox" ? "A" : "✕"} size={40} ink={ink} />
      </div>
      <div style={{ position: "absolute", left: 168, top: 80, opacity: 0.55 }}>
        <Badge label={kind === "xbox" ? "X" : "□"} size={40} ink={ink} />
      </div>
      <div style={{ position: "absolute", left: 244, top: 80, opacity: 0.55 }}>
        <Badge label={kind === "xbox" ? "B" : "○"} size={40} ink={ink} />
      </div>
    </div>
  );
};

/** The corner toast from SessionHintWindow. */
const Toast: React.FC<{ combo: string; progress: number }> = ({ combo, progress }) => (
  <div
    style={{
      position: "absolute",
      right: 28,
      top: 28,
      width: 470,
      padding: "14px 20px",
      background: "rgba(15,20,24,0.95)",
      border: "1px solid rgba(255,255,255,0.25)",
      display: "flex",
      gap: 16,
      alignItems: "center",
      fontFamily: font,
      color: theme.text,
      translate: `${(1 - progress) * 520}px 0`,
      boxShadow: "0 12px 30px rgba(0,0,0,0.5)",
    }}
  >
    <div style={{ fontSize: 30 }}>🎮</div>
    <div>
      <div style={{ fontSize: 17, fontWeight: 600 }}>Console Mode menu</div>
      <div style={{ fontSize: 15, opacity: 0.85 }}>
        Hold <b style={{ color: theme.accent }}>{combo}</b> to open the panel over the game.
      </div>
    </div>
  </div>
);

// ───────────────────────── session menu ─────────────────────────

const TILES = [
  { icon: "↩", label: "Back to game" },
  { icon: "🔊", label: "Volume", value: "60%" },
  { icon: "🖥", label: "Resolution", value: "3840×2160 @ 120" },
  { icon: "🎧", label: "Audio output", value: "TV (HDMI)" },
  { icon: "⏱", label: "FPS limit", value: "No limit" },
  { icon: "☀", label: "HDR", value: "On" },
  { icon: "⏺", label: "Record last 30 s", value: "Soon", disabled: true },
];

const SessionMenu: React.FC<{ focus: number; volume: number; editing: boolean; open: number; pad: string }> = ({ focus, volume, editing, open, pad }) => (
  <div
    style={{
      position: "absolute",
      left: 1280 / 2 - 520,
      top: 80,
      width: 1040,
      height: 500,
      background: "rgba(15,20,24,0.97)",
      borderRadius: 18,
      padding: "28px 36px",
      boxSizing: "border-box",
      fontFamily: font,
      color: theme.text,
      opacity: open,
      scale: 0.94 + open * 0.06,
      boxShadow: "0 30px 80px rgba(0,0,0,0.6)",
    }}
  >
    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
      <div>
        <div style={{ fontSize: 30, fontWeight: 600 }}>Session menu</div>
        <div style={{ fontSize: 15, color: theme.muted }}>1:12:40 · {pad}</div>
      </div>
      <div style={{ fontSize: 32, fontWeight: 600 }}>21:47</div>
    </div>
    <div style={{ display: "flex", flexWrap: "wrap", gap: 12, justifyContent: "center", marginTop: 22 }}>
      {TILES.map((t, i) => {
        const focused = Math.round(focus) === i;
        const value = i === 1 ? `${Math.round(volume)}%` : t.value;
        return (
          <div
            key={t.label}
            style={{
              width: 212,
              height: 140,
              borderRadius: 14,
              background: theme.card,
              border: "1px solid rgba(255,255,255,0.08)",
              outline: focused ? `4px solid ${theme.accent}` : "none",
              outlineOffset: 4,
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              justifyContent: "center",
              gap: 6,
              opacity: t.disabled ? 0.45 : 1,
            }}
          >
            <div style={{ fontSize: 28 }}>{t.icon}</div>
            <div style={{ fontSize: 16 }}>{t.label}</div>
            {value && (
              <div style={{ fontSize: 15, fontWeight: 600, color: theme.accent }}>
                {i === 1 && editing ? `◀  ${value}  ▶` : value}
              </div>
            )}
          </div>
        );
      })}
    </div>
    <div style={{ display: "flex", gap: 12, justifyContent: "center", marginTop: 18 }}>
      {["⟲  Back to the PC", "⏻  Exit Console Mode"].map((l) => (
        <div key={l} style={{ width: 380, height: 60, borderRadius: 14, background: theme.card, border: "1px solid rgba(255,255,255,0.08)", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 18 }}>
          {l}
        </div>
      ))}
    </div>
  </div>
);

// ───────────────────────── scenes ─────────────────────────

const Intro: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: f, fps, config: { damping: 14 } });
  return (
    <AbsoluteFill style={{ background: theme.bg, alignItems: "center", justifyContent: "center", fontFamily: font, color: theme.text }}>
      <div style={{ fontSize: 22, color: theme.accent, fontWeight: 700, letterSpacing: 3, opacity: s }}>CONSOLE MODE 1.5</div>
      <div style={{ fontSize: 64, fontWeight: 700, marginTop: 8, scale: 0.9 + s * 0.1, opacity: s }}>What's new</div>
    </AbsoluteFill>
  );
};

const ToastScene: React.FC = () => {
  const f = useCurrentFrame();
  const slide = interpolate(f, [20, 40, 85, 100], [0, 1, 1, 0], ease);
  return (
    <AbsoluteFill>
      <GameBackdrop />
      <Toast combo="Select + Y" progress={slide} />
      <Caption kicker="NEW" text="A heads-up when the game starts" dur={S.toast.dur} />
    </AbsoluteFill>
  );
};

const MenuScene: React.FC = () => {
  const f = useCurrentFrame();
  const press = interpolate(f, [8, 14, 26, 32], [0, 1, 1, 0], linear);
  const padOut = interpolate(f, [30, 38], [1, 0], linear);
  const open = interpolate(f, [36, 48], [0, 1], ease);
  // Focus walks right across the tiles, lands on Volume, adjusts it, then moves on.
  const focus = interpolate(f, [50, 62, 118, 130, 140, 150], [0, 1, 1, 2, 2, 3], linear);
  const editing = f >= 72 && f < 112;
  const volume = interpolate(f, [78, 106], [60, 85], linear);
  return (
    <AbsoluteFill>
      <GameBackdrop dim={open * 0.45} />
      <div style={{ position: "absolute", left: 490, top: 250, opacity: padOut }}>
        <Pad kind="xbox" pressed={press} />
      </div>
      <SessionMenu focus={focus} volume={volume} editing={editing} open={open} pad="Xbox controller" />
      <Caption kicker="NEW LAYOUT" text="Select + Y: a horizontal menu over the game" dur={S.menu.dur} />
    </AbsoluteFill>
  );
};

const PlayStationScene: React.FC = () => {
  const f = useCurrentFrame();
  const press = interpolate(f, [30, 36, 50, 56], [0, 1, 1, 0], linear);
  const toast = interpolate(f, [4, 20], [0, 1], ease);
  const works = interpolate(f, [58, 70], [0, 1], ease);
  return (
    <AbsoluteFill>
      <GameBackdrop dim={0.35} />
      <Toast combo="Create + △" progress={toast} />
      <div style={{ position: "absolute", left: 490, top: 230 }}>
        <Pad kind="ps" pressed={press} />
      </div>
      <div
        style={{
          position: "absolute",
          left: 0,
          right: 0,
          top: 450,
          textAlign: "center",
          fontFamily: font,
          fontSize: 26,
          fontWeight: 700,
          color: theme.accent,
          opacity: works,
        }}
      >
        ✓ Read directly over HID, no Steam Input needed
      </div>
      <Caption kicker="FIXED" text="DualSense and DualShock 4 work in the menu" dur={S.playstation.dur} />
    </AbsoluteFill>
  );
};

const SETTINGS_ROWS = [
  ["Resolution and refresh rate", "3840 × 2160 @ 120 Hz"],
  ["How to turn off the other screens", "Disconnect in Windows"],
  ["Open", "Steam Big Picture"],
  ["Audio output", "TV (HDMI)"],
  ["Open with the controller's Home button", "On"],
];

const SettingsScene: React.FC = () => {
  const f = useCurrentFrame();
  const focus = interpolate(f, [18, 30, 48, 60, 78, 90], [0, 1, 1, 2, 2, 3], linear);
  return (
    <AbsoluteFill style={{ background: "#0F1418", fontFamily: font, color: theme.text, padding: "50px 70px" }}>
      <div style={{ fontSize: 40, fontWeight: 600 }}>Settings</div>
      <div style={{ fontSize: 18, color: theme.muted, marginBottom: 22 }}>Press A on a row to change it. Everything saves right away.</div>
      {SETTINGS_ROWS.map(([label, value], i) => {
        const focused = Math.round(focus) === i;
        return (
          <div
            key={label}
            style={{
              width: 760,
              height: 66,
              marginBottom: 10,
              borderRadius: 14,
              background: theme.card,
              border: "1px solid rgba(255,255,255,0.08)",
              outline: focused ? `4px solid ${theme.accent}` : "none",
              outlineOffset: 4,
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              padding: "0 24px",
              boxSizing: "border-box",
              fontSize: 19,
            }}
          >
            <span>{label}</span>
            <span style={{ color: theme.accent, fontWeight: 600 }}>{value}</span>
          </div>
        );
      })}
      <Caption kicker="FIXED" text="Controller navigation in Settings no longer skips rows" dur={S.settings.dur} />
    </AbsoluteFill>
  );
};

const UpdateScene: React.FC = () => {
  const f = useCurrentFrame();
  const progress = interpolate(f, [16, 58], [0, 1], ease);
  const done = f > 60;
  return (
    <AbsoluteFill style={{ background: theme.bg, alignItems: "center", justifyContent: "center", fontFamily: font, color: theme.text }}>
      <div style={{ width: 700, background: "#233322", border: "1px solid #3b5a33", borderRadius: 10, padding: "22px 26px" }}>
        <div style={{ fontSize: 20, fontWeight: 600 }}>⬇ Console Mode 1.5.0-beta.11 available</div>
        <div style={{ height: 10, background: "rgba(255,255,255,0.12)", borderRadius: 5, marginTop: 20, overflow: "hidden" }}>
          <div style={{ width: `${progress * 100}%`, height: "100%", background: theme.accent }} />
        </div>
        <div style={{ fontSize: 16, color: done ? theme.accent : theme.muted, marginTop: 12 }}>
          {done ? "✓ Downloaded and verified. Restarting…" : `Downloading… ${Math.round(progress * 100)}%`}
        </div>
      </div>
      <Caption kicker="FIXED" text="Updates no longer time out on slow connections" dur={S.update.dur} />
    </AbsoluteFill>
  );
};

const Outro: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const a = spring({ frame: f - 6, fps, config: { damping: 14 } });
  const b = spring({ frame: f - 14, fps, config: { damping: 14 } });
  const tag = interpolate(f, [34, 50], [0, 1], linear);
  const card = (logo: string, name: string, desc: string, s: number) => (
    <div style={{ display: "flex", gap: 16, alignItems: "center", background: theme.card, borderRadius: 14, padding: "16px 22px", width: 400, opacity: s, translate: `0 ${(1 - s) * 30}px` }}>
      <Img src={staticFile(logo)} style={{ width: 56, height: 56, borderRadius: 10 }} />
      <div>
        <div style={{ fontSize: 22, fontWeight: 700 }}>{name}</div>
        <div style={{ fontSize: 16, color: theme.muted }}>{desc}</div>
      </div>
    </div>
  );
  return (
    <AbsoluteFill style={{ background: theme.bg, alignItems: "center", justifyContent: "center", fontFamily: font, color: theme.text }}>
      <div style={{ fontSize: 20, color: theme.accent, fontWeight: 700, letterSpacing: 2, marginBottom: 20 }}>MORE APPS FROM THE DEVELOPER</div>
      <div style={{ display: "flex", gap: 24 }}>
        {card("wakeon.png", "WakeOn", "Wake-on-LAN automation", a)}
        {card("nextboost.png", "Next Boost", "Windows optimization", b)}
      </div>
      <div style={{ marginTop: 44, fontSize: 18, color: theme.muted, opacity: tag }}>
        Console Mode 1.5.0-beta.11 · github.com/lippdev/consolemode
      </div>
    </AbsoluteFill>
  );
};

export const WhatsNew: React.FC = () => {
  const scenes: [keyof typeof S, React.FC][] = [
    ["intro", Intro],
    ["toast", ToastScene],
    ["menu", MenuScene],
    ["playstation", PlayStationScene],
    ["settings", SettingsScene],
    ["update", UpdateScene],
    ["outro", Outro],
  ];
  return (
    <AbsoluteFill style={{ background: theme.bg }}>
      {scenes.map(([key, Scene]) => (
        <Sequence key={key} from={S[key].from} durationInFrames={S[key].dur}>
          <SceneFade dur={S[key].dur}>
            <Scene />
          </SceneFade>
        </Sequence>
      ))}
    </AbsoluteFill>
  );
};
