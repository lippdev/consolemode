import React from "react";
import { AbsoluteFill, Audio, Easing, getStaticFiles, OffthreadVideo, Sequence, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { ConsoleIntro, ConsoleUi, Covers, G, type Glyph, More, Part, SessionMenu, Shortcuts } from "./Alpha3";
import { C, Center, Glow, Line, Logo, SANS, tw, useFonts } from "./kit";
import { GameContext, type GameInfo } from "./GamePlay";
import { DeskScene } from "./Launch";
import { RacingGame } from "./RacingGame";

// "Console Mode 1.6" (en-US): one click to the TV, the new session menu over a real game,
// ControlFS, the redesigned Console interface, Steam covers, shortcuts of your choice, the
// project's own display/audio code and the fixes. Music: scripts/soundtrack.py v16.
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
  native: { from: 1185, dur: 120 },
  more: { from: 1305, dur: 150 },
  outro: { from: 1455, dur: 150 },
};
export const RELEASE16_DURATION = R.outro.from + R.outro.dur;

const GAMEPLAY = "local/gameplay.mp4";
const CONTROLFS = "local/controlfs.png";
/** Where in gameplay.mp4 the film's frame 0 sits (frames at 30 fps). */
const CLIP = 30;

/** Game audio: silent until the TV turns on, up while it fills the screen, low behind the menu. */
const gameVolume = (t: number) => (t < 60 ? 0 : t < 150 ? 0.2 : t < 270 ? 0.45 : t < 615 ? 0.14 : 0);

/** Real footage in the game slot; `t - f` is the scene's start, so the clip never jumps. */
const Footage: React.FC<{ t: number }> = ({ t }) => {
  const f = useCurrentFrame();
  const start = t - f;
  return (
    <OffthreadVideo
      src={staticFile(GAMEPLAY)}
      startFrom={CLIP + start}
      volume={(vf) => gameVolume(start + vf)}
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

/** 1185–1305: 1.6 runs on the project's own display and audio code. */
const Native: React.FC = () => {
  const f = useCurrentFrame();
  const strike = (at: number) => tw(f, [at, at + 10], [0, 1]);
  const box: React.CSSProperties = {
    width: 560,
    borderRadius: 32,
    padding: "34px 40px",
    boxSizing: "border-box",
    background: "linear-gradient(180deg,#161618,#0c0c0e)",
    border: `1px solid ${C.line}`,
    fontFamily: SANS,
  };
  const item = (glyph: Glyph, text: string, gone = 0, key = text) => (
    <div key={key} style={{ display: "flex", alignItems: "center", gap: 16, fontSize: 32, fontWeight: 600, color: C.text, opacity: 1 - gone * 0.6, position: "relative", marginTop: 18 }}>
      <G name={glyph} size={30} color={C.muted} />
      <span style={{ position: "relative" }}>
        {text}
        <span style={{ position: "absolute", left: 0, top: "52%", height: 3, width: `${gone * 100}%`, background: "#ff5c5c" }} />
      </span>
    </div>
  );
  return (
    <AbsoluteFill>
      <Glow color={C.mint} y={640} opacity={0.12} />
      <Center top={80}>
        <Line words={["100%", "our", { t: "own", grad: true }, { t: "code.", grad: true }]} at={2} size={104} stagger={4} />
        <div style={{ height: 16 }} />
        <Line words={["Displays", "and", "audio", "now", "go", "through", "Windows’", "own", "APIs."]} at={14} size={42} weight={600} stagger={2} color={C.muted} />
      </Center>
      <div style={{ position: "absolute", top: 380, left: 0, right: 0, display: "flex", justifyContent: "center", alignItems: "center", gap: 60 }}>
        <div style={{ ...box, opacity: tw(f, [20, 30], [0, 1], Easing.linear) }}>
          <div style={{ fontSize: 28, fontWeight: 700, color: C.muted, letterSpacing: "0.1em" }}>1.5 SHIPPED</div>
          {item("monitor", "MultiMonitorTool", strike(44))}
          {item("speakers", "SoundVolumeView", strike(54))}
          {item("fps", "rtss-cli")}
        </div>
        <div style={{ fontFamily: SANS, fontSize: 70, color: C.muted, opacity: tw(f, [34, 44], [0, 1], Easing.linear) }}>→</div>
        <div style={{ ...box, border: `1.5px solid ${C.mint}88`, opacity: tw(f, [40, 50], [0, 1], Easing.linear) }}>
          <div style={{ fontSize: 28, fontWeight: 700, color: C.mint, letterSpacing: "0.1em" }}>1.6 SHIPS</div>
          {item("monitor", "Console Mode’s own code", 0, "own")}
          {item("fps", "rtss-cli (MIT)", 0, "rtss")}
        </div>
      </div>
      <Center top={820}>
        <Line words={["Paving", "the", "way", "for", "a", "signed", "app", "and", "fewer", "antivirus", "false", "positives."]} at={66} size={40} weight={600} stagger={2} color={C.text} />
        <div style={{ height: 12 }} />
        <Line words={["Your", "saved", "screens", "and", "audio", "carry", "over."]} at={80} size={34} weight={500} stagger={2} color={C.muted} />
      </Center>
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
        <Part t={R.native}>
          <Native />
        </Part>
        <Part t={R.more}>
          <More />
        </Part>
        <Part t={R.outro} cutOut>
          <Outro />
        </Part>
      </AbsoluteFill>
    </GameContext.Provider>
  );
};
