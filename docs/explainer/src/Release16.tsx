import React from "react";
import { AbsoluteFill, Audio, Easing, getStaticFiles, OffthreadVideo, Sequence, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { Badge, ConsoleIntro, ConsoleUi, Covers, G, type Glyph, More, Part, SessionMenu, Shortcuts } from "./Alpha3";
import { C, Center, Glow, Line, Logo, SANS, tw, useFonts } from "./kit";
import { GameContext, type GameInfo } from "./GamePlay";
import { DeskScene } from "./Launch";
import { RacingGame } from "./RacingGame";

// "Console Mode 1.6" (en-US): one click to the TV, the new session menu over a real game,
// ControlFS, the redesigned Console interface, Steam covers, shortcuts of your choice, the
// fixes, and JoyChromium (coming soon). Music: scripts/soundtrack.py v16.
//
// Real footage and the ControlFS screenshot are read from public/local/ (gitignored, never
// committed): gameplay.mp4 and controlfs.png. Without them the film falls back to the
// synthwave race and the plain folder animation.

const R = {
  desk: { from: 0, dur: 150 },
  title: { from: 150, dur: 120 },
  menu: { from: 270, dur: 345 },
  consoleIntro: { from: 615, dur: 60 },
  console: { from: 675, dur: 240 },
  covers: { from: 915, dur: 90 },
  shortcuts: { from: 1005, dur: 180 },
  more: { from: 1185, dur: 150 },
  joy: { from: 1335, dur: 120 },
  outro: { from: 1455, dur: 150 },
};
export const RELEASE16_DURATION = R.outro.from + R.outro.dur;

const GAMEPLAY = "local/gameplay.mp4";
const CONTROLFS = "local/controlfs.png";
/** Where in gameplay.mp4 the film's frame 0 sits (frames at 30 fps). */
const CLIP = 30;

/** Real footage in the game slot, muted (only the film's music plays); `t - f` is the scene's start, so the clip never jumps. */
const Footage: React.FC<{ t: number }> = ({ t }) => {
  const f = useCurrentFrame();
  const start = t - f;
  return (
    <OffthreadVideo
      src={staticFile(GAMEPLAY)}
      startFrom={CLIP + start}
      muted
      style={{ position: "absolute", inset: 0, width: "100%", height: "100%", objectFit: "cover" }}
    />
  );
};

const DARK_SOULS: GameInfo = { proc: "DarkSoulsIII", title: "DARK SOULS III", letter: "D", color: "linear-gradient(135deg,#6b5a45,#1c1612)" };

const hasLocal = (name: string) => getStaticFiles().some((s) => s.name === name);

// ───────────────────────── scenes ─────────────────────────

/** 150–270: the name, then the new menu, over the game. */
const TitleOverGame: React.FC = () => {
  const f = useCurrentFrame();
  const shadow = { textShadow: "0 10px 60px rgba(0,0,0,0.6)" };
  return (
    <AbsoluteFill>
      <GameSlotView t={R.title.from + f} />
      <AbsoluteFill style={{ background: `rgba(5,4,8,${tw(f, [0, 10], [0, 0.45], Easing.linear)})` }} />
      {f < 58 && (
        <Center>
          <div style={{ display: "flex", justifyContent: "center", scale: String(tw(f, [0, 16], [0.8, 1])) }}>
            <Logo size={150} draw={1} glow={1} />
          </div>
          <div style={{ height: 30 }} />
          <Line words={["Console", "Mode", { t: "1.6", grad: true }]} at={4} size={170} weight={800} stagger={4} style={shadow} />
        </Center>
      )}
      {f >= 60 && (
        <Center>
          <Line words={["A", "new", "session", { t: "menu.", grad: true }]} at={60} size={150} stagger={4} style={shadow} />
          <div style={{ height: 24 }} />
          <Line words={["Steam", "overlay", "style,", "over", "any", "game."]} at={76} size={52} weight={600} stagger={2} color="rgba(245,245,247,0.88)" style={shadow} />
        </Center>
      )}
    </AbsoluteFill>
  );
};

/** Renders whatever the game slot holds (footage or the race) at global frame t. */
const GameSlotView: React.FC<{ t: number }> = ({ t }) => {
  const slot = React.useContext(GameContext);
  return <>{slot.render(t, true)}</>;
};

/** A shortcut tile on the JoyChromium start page. */
const WebTile: React.FC<{ label: string; color: string; glyph: Glyph; focus: number }> = ({ label, color, glyph, focus }) => (
  <div
    style={{
      width: 236,
      height: 168,
      borderRadius: 22,
      background: "rgba(255,255,255,0.07)",
      display: "flex",
      flexDirection: "column",
      alignItems: "center",
      justifyContent: "center",
      gap: 14,
      fontFamily: SANS,
      fontSize: 24,
      fontWeight: 600,
      color: C.text,
      scale: String(1 + 0.07 * focus),
      outline: focus > 0.5 ? "3px solid #fff" : "3px solid transparent",
      outlineOffset: 5,
    }}
  >
    <div style={{ width: 64, height: 64, borderRadius: 18, background: color, display: "flex", alignItems: "center", justifyContent: "center" }}>
      <G name={glyph} size={32} color="#fff" />
    </div>
    {label}
  </div>
);

/** 1335–1455: coming soon, JoyChromium, a browser for the controller. Illustrative start page. */
const JoyChromium: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const win = spring({ frame: f - 18, fps, config: { damping: 18, stiffness: 110 } });
  let pos = 0;
  for (let k = 0; k < 4; k++) pos += tw(f, [50 + 12 * k, 55 + 12 * k], [0, 1]);
  const tiles: { label: string; color: string; glyph: Glyph }[] = [
    { label: "Videos", color: "linear-gradient(135deg,#ff5f6d,#c2185b)", glyph: "video" },
    { label: "Music", color: "linear-gradient(135deg,#8b6cff,#4a2cc9)", glyph: "sound" },
    { label: "Guides", color: "linear-gradient(135deg,#38c6ff,#1769aa)", glyph: "folder" },
    { label: "Store", color: "linear-gradient(135deg,#3ecfa0,#1f8a67)", glyph: "play" },
    { label: "Streams", color: "linear-gradient(135deg,#ffb347,#e0661b)", glyph: "monitor" },
  ];
  return (
    <AbsoluteFill>
      <Glow color={C.violet} y={700} opacity={0.16} size={1700} />
      <Center top={60}>
        <div style={{ display: "flex", justifyContent: "center", opacity: tw(f, [0, 8], [0, 1], Easing.linear) }}>
          <span style={{ padding: "8px 20px", borderRadius: 999, border: `1.5px solid ${C.mint}`, color: C.mint, fontFamily: SANS, fontSize: 26, fontWeight: 700, letterSpacing: "0.12em" }}>COMING SOON</span>
        </div>
        <div style={{ height: 18 }} />
        <Line words={[{ t: "JoyChromium.", grad: true }]} at={4} size={110} stagger={4} />
        <div style={{ height: 10 }} />
        <Line words={["A", "web", "browser", "made", "for", "the", "controller.", "Straight", "from", "Console", "Mode."]} at={14} size={40} weight={600} stagger={2} color={C.muted} />
      </Center>
      <div
        style={{
          position: "absolute",
          left: 960 - 680,
          top: 440,
          width: 1360,
          height: 470,
          borderRadius: 28,
          background: "linear-gradient(180deg,#14161c,#0b0c10)",
          border: `1px solid ${C.line}`,
          boxShadow: "0 40px 120px rgba(0,0,0,0.6)",
          opacity: win,
          translate: `0 ${(1 - win) * 120}px`,
          overflow: "hidden",
          fontFamily: SANS,
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 18, padding: "26px 34px" }}>
          <Badge label="LB" />
          <div style={{ padding: "10px 22px", borderRadius: 14, background: "rgba(255,255,255,0.1)", color: C.text, fontSize: 22, fontWeight: 600 }}>Home</div>
          <div style={{ padding: "10px 22px", borderRadius: 14, color: C.muted, fontSize: 22 }}>New tab</div>
          <Badge label="RB" />
          <div style={{ flex: 1, height: 58, borderRadius: 29, background: "rgba(255,255,255,0.07)", display: "flex", alignItems: "center", gap: 14, padding: "0 24px", color: C.muted, fontSize: 24 }}>
            <Badge label="Y" h={30} /> Search or type a web address
          </div>
        </div>
        <div style={{ display: "flex", justifyContent: "center", gap: 22, marginTop: 50 }}>
          {tiles.map((t, i) => (
            <div key={t.label} style={{ opacity: tw(f - 28 - i * 3, [0, 10], [0, 1], Easing.linear) }}>
              <WebTile {...t} focus={Math.max(0, 1 - Math.abs(pos - i))} />
            </div>
          ))}
        </div>
        <div style={{ position: "absolute", right: 34, bottom: 26, display: "flex", gap: 26, alignItems: "center", fontSize: 20, color: C.text }}>
          {[
            ["A", "Open"],
            ["B", "Back"],
            ["LB RB", "Tabs"],
            ["Y", "Search"],
          ].map(([b, t]) => (
            <span key={t} style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <Badge label={b} />
              {t}
            </span>
          ))}
        </div>
      </div>
    </AbsoluteFill>
  );
};

const Outro: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const pop = spring({ frame: f, fps, config: { damping: 14, stiffness: 110 } });
  const btn = spring({ frame: f - 50, fps, config: { damping: 16, stiffness: 120 } });
  const end = tw(f, [118, 150], [0, 1], Easing.inOut(Easing.cubic));
  return (
    <AbsoluteFill style={{ opacity: 1 - end }}>
      <Glow color={C.mint} y={470} opacity={0.2 + pop * 0.08} size={1600} />
      <Center top={170}>
        <div style={{ display: "flex", justifyContent: "center", scale: String(0.6 + pop * 0.4) }}>
          <Logo size={190} draw={1} glow={pop} />
        </div>
        <div style={{ height: 36 }} />
        <Line words={["Console", "Mode", { t: "1.6", grad: true }]} at={4} size={170} weight={800} stagger={4} />
        <div style={{ height: 22 }} />
        <Line words={["Your", "PC.", { t: "Now", grad: true }, { t: "a", grad: true }, { t: "console.", grad: true }]} at={24} size={58} weight={600} stagger={3} color={C.muted} />
      </Center>
      <div style={{ position: "absolute", top: 830, left: 0, right: 0, display: "flex", flexDirection: "column", alignItems: "center", gap: 26 }}>
        <div style={{ padding: "20px 50px", borderRadius: 999, background: C.text, color: "#000", fontFamily: SANS, fontSize: 34, fontWeight: 700, opacity: btn, scale: String(0.85 + btn * 0.15) }}>
          Download for Windows
        </div>
        <div style={{ fontFamily: SANS, fontSize: 30, fontWeight: 500, color: C.muted, opacity: tw(f, [62, 74], [0, 1], Easing.linear) }}>github.com/lippdev/consolemode</div>
      </div>
    </AbsoluteFill>
  );
};

// ───────────────────────── film ─────────────────────────

export const Release16: React.FC = () => {
  useFonts();
  const footage = hasLocal(GAMEPLAY);
  const slot = {
    render: footage ? (t: number) => <Footage t={t} /> : (t: number, hud: boolean) => <RacingGame t={t} hud={hud} />,
    info: footage ? DARK_SOULS : { proc: "NeonDrift", title: "Neon Drift", letter: "N", color: "linear-gradient(135deg,#ff3ec9,#7a2cff)" },
    controlFs: hasLocal(CONTROLFS) ? CONTROLFS : undefined,
  };
  return (
    <GameContext.Provider value={slot}>
      <AbsoluteFill style={{ background: C.bg, overflow: "hidden" }}>
        <Audio src={staticFile("keynote/release16.mp3")} />
        <Sequence from={R.desk.from} durationInFrames={R.desk.dur}>
          <DeskScene />
        </Sequence>
        <Part t={R.title} cutIn cutOut>
          <TitleOverGame />
        </Part>
        <Part t={R.menu} cutIn>
          <SessionMenu from={R.menu.from} />
        </Part>
        <Part t={R.consoleIntro}>
          <ConsoleIntro />
        </Part>
        <Part t={R.console}>
          <ConsoleUi />
        </Part>
        <Part t={R.covers}>
          <Covers />
        </Part>
        <Part t={R.shortcuts}>
          <Shortcuts />
        </Part>
        <Part t={R.more}>
          <More />
        </Part>
        <Part t={R.joy}>
          <JoyChromium />
        </Part>
        <Part t={R.outro} cutOut>
          <Outro />
        </Part>
      </AbsoluteFill>
    </GameContext.Provider>
  );
};
