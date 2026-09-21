import React from "react";
import { theme } from "./theme";

export type RoomProps = {
  deskOn: number;
  tvOn: number;
  foco: number;
  hdmiDraw: number;
  hdmiGlow: number;
  steamOn: number;
  ambientDesk: number;
  tvFlash: number;
};

const font = "Segoe UI, system-ui, sans-serif";

const DesktopScreen: React.FC<{ variant: "left" | "right" }> = ({ variant }) => {
  const windowLeft = variant === "left" ? 14 : 22;
  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        background: "linear-gradient(160deg, #1a4a4a 0%, #163044 55%, #1b2838 100%)",
        overflow: "hidden",
        position: "relative",
      }}
    >
      <div
        style={{
          position: "absolute",
          left: variant === "left" ? -20 : 80,
          top: -30,
          width: 140,
          height: 140,
          borderRadius: "50%",
          background: "rgba(62, 207, 160, 0.18)",
        }}
      />
      <div
        style={{
          position: "absolute",
          left: windowLeft,
          top: 12,
          width: "62%",
          height: "52%",
          background: theme.card,
          borderRadius: 3,
          border: "1px solid #3A3A42",
        }}
      />
      <div
        style={{
          position: "absolute",
          right: 10,
          top: variant === "left" ? 18 : 40,
          width: "24%",
          height: "34%",
          background: theme.surface,
          borderRadius: 3,
        }}
      />
      <div
        style={{
          position: "absolute",
          left: 0,
          right: 0,
          bottom: 0,
          height: 9,
          background: "#0c0c10",
        }}
      />
      <div
        style={{
          position: "absolute",
          left: 8,
          bottom: 2,
          width: 20,
          height: 5,
          borderRadius: 1,
          background: theme.accent,
        }}
      />
    </div>
  );
};

const SteamUi: React.FC = () => {
  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        background: "linear-gradient(180deg, #1b2838 0%, #0d151d 100%)",
        padding: 12,
        display: "flex",
        flexDirection: "column",
        fontFamily: font,
      }}
    >
      <div
        style={{
          color: "#c7d5e0",
          fontSize: 13,
          fontWeight: 700,
          letterSpacing: 3,
        }}
      >
        STEAM
      </div>
      <div style={{ flex: 1, display: "flex", gap: 8, marginTop: 10 }}>
        <div
          style={{
            flex: 2.2,
            background: "linear-gradient(135deg, #2a475e, #1b3a4f)",
            borderRadius: 6,
            border: `2px solid ${theme.steamHi}`,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <div
            style={{
              width: 0,
              height: 0,
              borderLeft: "16px solid rgba(255,255,255,0.85)",
              borderTop: "10px solid transparent",
              borderBottom: "10px solid transparent",
            }}
          />
        </div>
        <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 8 }}>
          <div style={{ flex: 1, background: "#193d5a", borderRadius: 5 }} />
          <div style={{ flex: 1, background: "#1a4540", borderRadius: 5 }} />
        </div>
      </div>
    </div>
  );
};

const Bezel: React.FC<{
  width: number;
  height: number;
  children: React.ReactNode;
  on: number;
  led?: string;
}> = ({ width, height, children, on, led }) => {
  return (
    <div
      style={{
        width,
        height,
        background: theme.bezel,
        borderRadius: 8,
        padding: 7,
        boxShadow: "0 10px 28px rgba(0,0,0,0.45)",
        position: "relative",
      }}
    >
        <div
        style={{
          width: "100%",
          height: "100%",
          background: "#050506",
          overflow: "hidden",
          borderRadius: 3,
          position: "relative",
        }}
      >
        <div
          style={{
            width: "100%",
            height: "100%",
            opacity: on,
            position: "relative",
          }}
        >
          {children}
        </div>
        {on < 0.15 ? (
          <div
            style={{
              position: "absolute",
              right: 8,
              bottom: 6,
              width: 5,
              height: 5,
              borderRadius: "50%",
              background: led ?? "#7a1f1f",
              boxShadow: `0 0 6px ${led ?? "#7a1f1f"}`,
            }}
          />
        ) : null}
      </div>
    </div>
  );
};

const Stand: React.FC<{ width: number }> = ({ width }) => {
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
      <div style={{ width: 10, height: 14, background: "#2a2a30" }} />
      <div
        style={{
          width,
          height: 8,
          background: "#2a2a30",
          borderRadius: 3,
        }}
      />
    </div>
  );
};

export const Room: React.FC<RoomProps> = ({
  deskOn,
  tvOn,
  foco,
  hdmiDraw,
  hdmiGlow,
  steamOn,
  ambientDesk,
  tvFlash,
}) => {
  const cableLength = 1100;

  return (
    <div style={{ position: "absolute", inset: 0, fontFamily: font }}>
      <div style={{ position: "absolute", inset: 0, background: theme.wall }} />
      <div
        style={{
          position: "absolute",
          left: 0,
          right: 0,
          bottom: 0,
          height: 250,
          background: "linear-gradient(180deg, #16161c 0%, #0b0b0e 100%)",
        }}
      />
      <div
        style={{
          position: "absolute",
          left: "38%",
          right: 0,
          top: 0,
          bottom: 250,
          background:
            "linear-gradient(90deg, rgba(20,20,23,0) 0%, rgba(10,10,14,0.35) 100%)",
        }}
      />

      <svg
        viewBox="0 0 1280 720"
        style={{
          position: "absolute",
          inset: 0,
          overflow: "visible",
          zIndex: 1,
        }}
      >
        <path
          d="M 78 505 C 360 640, 780 640, 1055 360"
          fill="none"
          stroke="#3a3a44"
          strokeWidth="7"
          strokeLinecap="round"
          strokeDasharray={cableLength}
          strokeDashoffset={(1 - hdmiDraw) * cableLength}
        />
        <path
          d="M 78 505 C 360 640, 780 640, 1055 360"
          fill="none"
          stroke={theme.accent}
          strokeWidth="2.5"
          strokeLinecap="round"
          opacity={hdmiGlow}
          strokeDasharray={cableLength}
          strokeDashoffset={(1 - hdmiDraw) * cableLength}
        />
      </svg>

      <div
        style={{
          position: "absolute",
          left: 620,
          top: 628,
          opacity: hdmiDraw,
          color: theme.muted,
          fontSize: 13,
          fontWeight: 700,
          letterSpacing: 2,
          zIndex: 2,
        }}
      >
        HDMI
      </div>

      <div
        style={{
          position: "absolute",
          left: 48,
          bottom: 176,
          width: 520,
          height: 16,
          background: theme.desk,
          borderRadius: 3,
          boxShadow: "0 8px 18px rgba(0,0,0,0.4)",
          opacity: 0.5 + ambientDesk * 0.5,
          zIndex: 2,
        }}
      />
      <div
        style={{
          position: "absolute",
          left: 70,
          bottom: 148,
          width: 14,
          height: 28,
          background: "#2c2722",
          opacity: 0.5 + ambientDesk * 0.5,
        }}
      />
      <div
        style={{
          position: "absolute",
          left: 520,
          bottom: 148,
          width: 14,
          height: 28,
          background: "#2c2722",
          opacity: 0.5 + ambientDesk * 0.5,
        }}
      />
      <div
        style={{
          position: "absolute",
          left: 62,
          bottom: 192,
          width: 36,
          height: 84,
          background: "#1a1a20",
          borderRadius: 4,
          opacity: 0.55 + ambientDesk * 0.45,
          zIndex: 3,
        }}
      >
        <div
          style={{
            position: "absolute",
            left: 8,
            bottom: 10,
            width: 6,
            height: 6,
            borderRadius: "50%",
            background: theme.accent,
            boxShadow: `0 0 8px ${theme.accent}`,
          }}
        />
      </div>

      <div
        style={{
          position: "absolute",
          left: 112,
          bottom: 192,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          opacity: 0.4 + ambientDesk * 0.6,
          zIndex: 3,
        }}
      >
        <Bezel width={220} height={138} on={deskOn}>
          <DesktopScreen variant="left" />
        </Bezel>
        <Stand width={54} />
      </div>
      <div
        style={{
          position: "absolute",
          left: 342,
          bottom: 192,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          opacity: 0.4 + ambientDesk * 0.6,
          zIndex: 3,
        }}
      >
        <Bezel width={200} height={126} on={deskOn}>
          <DesktopScreen variant="right" />
        </Bezel>
        <Stand width={48} />
      </div>

      <div
        style={{
          position: "absolute",
          left: 900,
          bottom: 176,
          width: 280,
          height: 38,
          background: "#1a1a22",
          borderRadius: "18px 18px 6px 6px",
          opacity: 0.9,
        }}
      />

      <div
        style={{
          position: "absolute",
          left: 868,
          bottom: 196,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          zIndex: 4,
        }}
      >
        <div style={{ position: "relative" }}>
          <Bezel width={360} height={210} on={tvOn} led={tvOn > 0.2 ? theme.accent : "#7a1f1f"}>
            <SteamUi />
            <div
              style={{
                position: "absolute",
                inset: 0,
                background: "#050506",
                opacity: 1 - steamOn,
              }}
            />
            <div
              style={{
                position: "absolute",
                inset: 0,
                background: "#ffffff",
                opacity: tvFlash,
              }}
            />
          </Bezel>
          {foco > 0.05 ? (
            <div
              style={{
                position: "absolute",
                left: "50%",
                top: -36,
                translate: "-50% 0",
                opacity: foco,
                background: theme.accentDark,
                border: `1px solid ${theme.accent}`,
                color: theme.accent,
                fontSize: 13,
                fontWeight: 700,
                letterSpacing: 1,
                padding: "4px 12px",
                borderRadius: 999,
                whiteSpace: "nowrap",
              }}
            >
              Foco
            </div>
          ) : null}
        </div>
        <div style={{ width: 16, height: 18, background: "#2a2a30" }} />
        <div
          style={{
            width: 90,
            height: 10,
            background: "#2a2a30",
            borderRadius: 3,
          }}
        />
      </div>
    </div>
  );
};
