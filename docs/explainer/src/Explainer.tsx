import React from "react";
import {
  AbsoluteFill,
  Easing,
  interpolate,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { Room } from "./Room";
import { theme } from "./theme";

const ease = {
  extrapolateLeft: "clamp" as const,
  extrapolateRight: "clamp" as const,
  easing: Easing.bezier(0.22, 1, 0.36, 1),
};

const linear = {
  extrapolateLeft: "clamp" as const,
  extrapolateRight: "clamp" as const,
};

export const Explainer: React.FC = () => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();

  const hdmiDraw = interpolate(frame, [8, 48], [0, 1], ease);
  const title = interpolate(frame, [55, 80, 200, 225], [0, 1, 1, 0], linear);
  const hud = interpolate(frame, [100, 125, 195, 220], [0, 1, 1, 0], linear);
  const clicked = interpolate(frame, [168, 176, 188], [0, 1, 0], linear);
  const foco = interpolate(frame, [175, 200, 500, 525], [0, 1, 1, 0], ease);
  const deskOn = interpolate(frame, [205, 245, 440, 490], [1, 0, 0, 1], ease);
  const tvOn = interpolate(frame, [220, 270, 430, 480], [0, 1, 1, 0], ease);
  const steamOn = interpolate(frame, [258, 298, 425, 460], [0, 1, 1, 0], ease);
  const tvFlash = interpolate(frame, [222, 234, 258], [0, 0.65, 0], linear);
  const ambientDesk = interpolate(frame, [205, 260, 440, 500], [1, 0.25, 0.25, 1], ease);
  const hdmiGlow = interpolate(frame, [200, 250, 420, 470], [0.15, 1, 1, 0.15], linear);
  const camScale = interpolate(frame, [200, 280, 430, 500], [1, 1.14, 1.14, 1], ease);
  const fade = interpolate(
    frame,
    [0, 18, durationInFrames - 22, durationInFrames],
    [0, 1, 1, 0],
    linear,
  );

  const cursorX = interpolate(frame, [105, 165], [280, 1188], ease);
  const cursorY = interpolate(frame, [105, 165], [300, 86], ease);
  const cursorOp = interpolate(frame, [95, 115, 188, 210], [0, 1, 1, 0], linear);

  return (
    <AbsoluteFill style={{ backgroundColor: theme.bg }}>
      <div
        style={{
          position: "absolute",
          inset: 0,
          opacity: fade,
          transformOrigin: "78% 42%",
          scale: camScale,
        }}
      >
        <Room
          deskOn={deskOn}
          tvOn={tvOn}
          foco={foco}
          hdmiDraw={hdmiDraw}
          hdmiGlow={hdmiGlow}
          steamOn={steamOn}
          ambientDesk={ambientDesk}
          tvFlash={tvFlash}
        />
      </div>

      <div
        style={{
          position: "absolute",
          left: 36,
          top: 28,
          opacity: title,
          display: "flex",
          alignItems: "center",
          gap: 12,
        }}
      >
        <div
          style={{
            width: 42,
            height: 42,
            borderRadius: 12,
            background: theme.accentDark,
            color: theme.accent,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontSize: 18,
            fontWeight: 800,
            fontFamily: "Segoe UI, system-ui, sans-serif",
          }}
        >
          ▣
        </div>
        <div
          style={{
            color: theme.accent,
            fontSize: 28,
            fontWeight: 800,
            fontFamily: "Segoe UI, system-ui, sans-serif",
          }}
        >
          Console Mode
        </div>
      </div>

      <div
        style={{
          position: "absolute",
          right: 36,
          top: 28,
          opacity: hud,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: 10,
        }}
      >
        <div style={{ display: "flex", gap: 8 }}>
          <Pill label="Steam" active />
          <Pill label="Xbox" />
        </div>
        <div
          style={{
            background: theme.accent,
            color: "#10241D",
            fontWeight: 800,
            fontSize: 16,
            fontFamily: "Segoe UI, system-ui, sans-serif",
            padding: "8px 22px",
            borderRadius: 10,
            scale: 1 - clicked * 0.08,
          }}
        >
          Iniciar
        </div>
      </div>

      <div
        style={{
          position: "absolute",
          left: cursorX,
          top: cursorY,
          opacity: cursorOp,
          width: 0,
          height: 0,
          borderLeft: "16px solid white",
          borderTop: "10px solid transparent",
          borderBottom: "16px solid transparent",
          filter: "drop-shadow(0 2px 3px rgba(0,0,0,0.55))",
          scale: 1 - clicked * 0.12,
        }}
      />
    </AbsoluteFill>
  );
};

const Pill: React.FC<{ label: string; active?: boolean }> = ({ label, active }) => {
  return (
    <div
      style={{
        padding: "5px 14px",
        borderRadius: 999,
        fontSize: 14,
        fontWeight: 700,
        fontFamily: "Segoe UI, system-ui, sans-serif",
        background: active ? theme.accentDark : theme.card,
        color: active ? theme.accent : theme.muted,
        border: `1px solid ${active ? theme.accent : "#3A3A42"}`,
      }}
    >
      {label}
    </div>
  );
};
