import React from "react";
import {
  AbsoluteFill,
  Easing,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { theme } from "./theme";

// Roadmap board for the README: three columns (Next, Later, Exploring) whose cards pop in,
// then each column is spotlighted in turn. Items mirror the "Roadmap" section of README.md.

const font = "Segoe UI, system-ui, sans-serif";
const ease = {
  extrapolateLeft: "clamp" as const,
  extrapolateRight: "clamp" as const,
  easing: Easing.bezier(0.22, 1, 0.36, 1),
};
const linear = { extrapolateLeft: "clamp" as const, extrapolateRight: "clamp" as const };

type Item = { icon: string; title: string; note: string; issue?: number };
type Column = { name: string; color: string; items: Item[] };

const COLUMNS: Column[] = [
  {
    name: "Next",
    color: theme.accent,
    items: [
      { icon: "✅", title: "1.5.0 stable", note: "Wrap up the betas" },
      { icon: "📈", title: "FPS overlay", note: "Toggle it from Select + Y", issue: 66 },
      { icon: "⏺", title: "Record last 30 s", note: "From the session menu" },
      { icon: "🗂", title: "Desktop icons", note: "Saved and put back", issue: 30 },
    ],
  },
  {
    name: "Later",
    color: "#66c0f4",
    items: [
      { icon: "🎯", title: "Per-game profiles", note: "Resolution, HDR, audio per game", issue: 69 },
      { icon: "🔋", title: "Controller battery", note: "Shown in the session menu", issue: 70 },
      { icon: "📦", title: "winget", note: "winget install ConsoleMode", issue: 13 },
      { icon: "🌍", title: "More languages", note: "Community translations", issue: 14 },
    ],
  },
  {
    name: "Exploring",
    color: "#c792ea",
    items: [
      { icon: "🧩", title: "Game Bar widget", note: "Start and restore from Win + G", issue: 11 },
      { icon: "🗣", title: "Voice triggers", note: "Alexa, assistants, MCP", issue: 12 },
      { icon: "⚡", title: "WakeOn integration", note: "Wake the PC straight into console mode", issue: 71 },
      { icon: "📱", title: "Phone remote", note: "Over the local control API", issue: 72 },
    ],
  },
];

const INTRO = 50;
const BOARD = 60; // cards finish popping in by INTRO + BOARD
const SPOT = 75; // frames per column spotlight
const OUTRO = 70;
export const ROADMAP_DURATION = INTRO + BOARD + SPOT * COLUMNS.length + OUTRO;

const Card: React.FC<{ item: Item; appear: number; lit: number; color: string }> = ({ item, appear, lit, color }) => (
  <div
    style={{
      background: theme.card,
      border: `1px solid ${lit > 0.5 ? color : "rgba(255,255,255,0.08)"}`,
      borderRadius: 14,
      padding: "14px 16px",
      display: "flex",
      gap: 14,
      alignItems: "center",
      opacity: appear,
      translate: `0 ${(1 - appear) * 24}px`,
      boxShadow: lit > 0.5 ? `0 0 24px ${color}55` : "none",
    }}
  >
    <div style={{ fontSize: 28, width: 36, textAlign: "center" }}>{item.icon}</div>
    <div style={{ flex: 1, minWidth: 0 }}>
      <div style={{ fontSize: 19, fontWeight: 700, display: "flex", gap: 8, alignItems: "baseline" }}>
        {item.title}
        {item.issue && <span style={{ fontSize: 13, color: theme.muted, fontWeight: 600 }}>#{item.issue}</span>}
      </div>
      <div style={{ fontSize: 14, color: theme.muted, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{item.note}</div>
    </div>
  </div>
);

export const Roadmap: React.FC = () => {
  const f = useCurrentFrame();
  const { fps, durationInFrames } = useVideoConfig();
  const fade = interpolate(f, [0, 12, durationInFrames - 14, durationInFrames], [0, 1, 1, 0], linear);

  const titleIn = spring({ frame: f, fps, config: { damping: 14 } });
  // Title starts centered, then moves up to make room for the board.
  const titleY = interpolate(f, [INTRO - 14, INTRO + 6], [300, 44], ease);
  const titleScale = interpolate(f, [INTRO - 14, INTRO + 6], [1.25, 0.8], ease);

  const spotStart = INTRO + BOARD;
  const spotIndex = f < spotStart ? -1 : Math.floor((f - spotStart) / SPOT);
  const outroStart = spotStart + SPOT * COLUMNS.length;
  const outro = interpolate(f, [outroStart, outroStart + 16], [0, 1], ease);

  return (
    <AbsoluteFill style={{ background: theme.bg, fontFamily: font, color: theme.text, opacity: fade }}>
      <div
        style={{
          position: "absolute",
          left: 0,
          right: 0,
          top: titleY,
          textAlign: "center",
          scale: titleScale,
          opacity: titleIn,
        }}
      >
        <div style={{ fontSize: 20, color: theme.accent, fontWeight: 700, letterSpacing: 3 }}>CONSOLE MODE</div>
        <div style={{ fontSize: 56, fontWeight: 700 }}>Roadmap</div>
      </div>

      <div style={{ position: "absolute", left: 50, right: 50, top: 170, display: "flex", gap: 24, opacity: 1 - outro * 0.9, filter: `blur(${outro * 6}px)` }}>
        {COLUMNS.map((col, c) => {
          const colIn = interpolate(f, [INTRO + c * 8, INTRO + c * 8 + 14], [0, 1], ease);
          const lit = spotIndex === c ? 1 : 0;
          const dim = spotIndex >= 0 && spotIndex < COLUMNS.length && spotIndex !== c ? 0.4 : 1;
          const lift = spotIndex === c ? interpolate(f - (spotStart + c * SPOT), [0, 14], [0, 1], ease) : 0;
          return (
            <div
              key={col.name}
              style={{
                flex: 1,
                display: "flex",
                flexDirection: "column",
                gap: 12,
                opacity: colIn * dim,
                translate: `0 ${-lift * 8}px`,
                scale: 1 + lift * 0.03,
              }}
            >
              <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 4 }}>
                <div style={{ width: 12, height: 12, borderRadius: 6, background: col.color, boxShadow: `0 0 12px ${col.color}` }} />
                <div style={{ fontSize: 22, fontWeight: 700, letterSpacing: 1 }}>{col.name.toUpperCase()}</div>
              </div>
              {col.items.map((item, i) => {
                const start = INTRO + 10 + c * 8 + i * 7;
                const appear = interpolate(f, [start, start + 14], [0, 1], ease);
                return <Card key={item.title} item={item} appear={appear} lit={lit} color={col.color} />;
              })}
            </div>
          );
        })}
      </div>

      {/* Outro */}
      <div
        style={{
          position: "absolute",
          left: 0,
          right: 0,
          top: 300,
          textAlign: "center",
          opacity: outro,
          translate: `0 ${(1 - outro) * 20}px`,
        }}
      >
        <div style={{ fontSize: 40, fontWeight: 700 }}>Have an idea?</div>
        <div style={{ fontSize: 22, color: theme.muted, marginTop: 10 }}>
          Vote or suggest on <span style={{ color: theme.accent, fontWeight: 600 }}>github.com/lippdev/consolemode/issues</span>
        </div>
      </div>
    </AbsoluteFill>
  );
};
