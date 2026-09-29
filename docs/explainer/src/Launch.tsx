import React from "react";
import { AbsoluteFill, Audio, Easing, interpolate, Sequence, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { C, Center, Check, clamp, Cursor, Desktop, Glow, gradText, Icon, type IconName, Line, Logo, SANS, Screen, tw, useFonts } from "./kit";
import { RacingGame } from "./RacingGame";

// ~32 s launch film for social media, built around a game that plays: one click turns the
// desk off and the TV on, the camera flies into the race, the menu opens over it, and quitting
// brings the desk back. The race runs on the film's global frame, so it never restarts between
// scenes. Music: scripts/soundtrack.py launch (120 BPM, one beat = 15 frames).

const L = {
  desk: { from: 0, dur: 150 },
  play: { from: 150, dur: 120 },
  menu: { from: 270, dur: 120 },
  specs: { from: 390, dur: 120 },
  quit: { from: 510, dur: 120 },
  home: { from: 630, dur: 90 },
  launchers: { from: 720, dur: 90 },
  finale: { from: 810, dur: 150 },
};
export const LAUNCH_DURATION = L.finale.from + L.finale.dur;

// ───────────────────────── the desk ─────────────────────────

// The TV, sized so its picture is exactly 16:9 (677 × 381 inside the bezel).
const TV = { x: 1100, y: 430, w: 700, h: 404 };
const TV_INNER = { x: TV.x + 11.5, y: TV.y + 11.5, w: TV.w - 23, h: TV.h - 23 };
const ZOOM = (1920 / TV_INNER.w) * 1.012;

/** Transform that flies the camera into the TV picture: p = 0 is the desk, 1 is full screen. */
const flyIn = (p: number): React.CSSProperties => {
  const s = Math.exp(Math.log(ZOOM) * p);
  const q = (s - 1) / (ZOOM - 1);
  const cx = TV_INNER.x + TV_INNER.w / 2;
  const cy = TV_INNER.y + TV_INNER.h / 2;
  const tx = cx + (960 - cx) * q - s * cx;
  const ty = cy + (540 - cy) * q - s * cy;
  return { transformOrigin: "0 0", transform: `translate(${tx}px, ${ty}px) scale(${s})` };
};

const Desk: React.FC<{ t: number; monA: number; monB: number; tv: number; zoom: number; pressed?: number; app?: boolean }> = ({
  t,
  monA,
  monB,
  tv,
  zoom,
  pressed = 0,
  app = false,
}) => (
  <AbsoluteFill style={flyIn(zoom)}>
    <Screen x={150} y={520} w={420} h={250} power={monA} kind="monitor">
      <Desktop />
    </Screen>
    <Screen x={600} y={520} w={420} h={250} power={monB} kind="monitor">
      <Desktop app={app} pressed={pressed} />
    </Screen>
    <Screen x={TV.x} y={TV.y} w={TV.w} h={TV.h} power={1} kind="tv">
      <AbsoluteFill style={{ scale: `1 ${Math.max(0.004, tv)}`, opacity: tv > 0.002 ? 1 : 0 }}>
        <RacingGame t={t} />
      </AbsoluteFill>
    </Screen>
  </AbsoluteFill>
);

/** 0–150: click Play now, the desk goes dark, the TV lights up, the camera flies in. */
const DeskScene: React.FC = () => {
  const f = useCurrentFrame();
  const click = 30;
  const pressed = interpolate(f, [click - 3, click, click + 6], [0, 1, 0], clamp);
  const tvOn = tw(f, [60, 70], [0, 1], Easing.out(Easing.cubic));
  const flash = interpolate(f, [60, 64, 76], [0, 1, 0], clamp);
  const zoom = tw(f, [80, 150], [0, 1], Easing.inOut(Easing.cubic));
  const cx = interpolate(f, [4, 26], [700, 810], { ...clamp, easing: Easing.inOut(Easing.cubic) });
  const cy = interpolate(f, [4, 26], [930, 671], { ...clamp, easing: Easing.inOut(Easing.cubic) });
  const ripple = tw(f, [click, click + 18], [0, 1]);
  const textOut = tw(f, [92, 110], [0, 1], Easing.linear);
  return (
    <AbsoluteFill>
      <Glow color={tvOn > 0 ? "#ff4fa0" : C.cyan} x={tvOn > 0 ? 1440 : 800} y={700} opacity={(0.1 + tvOn * 0.1) * (1 - zoom)} />
      <Desk t={f} monA={1 - tw(f, [42, 48], [0, 1], Easing.linear)} monB={1 - tw(f, [50, 56], [0, 1], Easing.linear)} tv={tvOn} zoom={zoom} pressed={pressed} app />
      <AbsoluteFill style={{ ...flyIn(zoom), pointerEvents: "none" }}>
        <div style={{ position: "absolute", left: TV_INNER.x, top: TV_INNER.y, width: TV_INNER.w, height: TV_INNER.h, background: "#fff", opacity: flash * 0.7 }} />
        {f >= click && f < click + 18 && (
          <div
            style={{
              position: "absolute",
              left: 810 - 60 * ripple,
              top: 671 - 60 * ripple,
              width: 120 * ripple,
              height: 120 * ripple,
              borderRadius: "50%",
              border: `3px solid ${C.mint}`,
              opacity: 1 - ripple,
            }}
          />
        )}
        {f < 58 && <Cursor x={cx} y={cy} scale={1 - pressed * 0.15} />}
      </AbsoluteFill>
      <div style={{ opacity: 1 - textOut, filter: `blur(${textOut * 12}px)` }}>
        <Center top={110}>
          <Line words={["One", { t: "click.", grad: true }]} at={2} size={130} stagger={6} />
          <div style={{ height: 22 }} />
          <Line words={["The", "TV", "takes", "over."]} at={60} size={52} weight={600} stagger={3} color={C.muted} />
        </Center>
      </div>
    </AbsoluteFill>
  );
};

// ───────────────────────── in the game ─────────────────────────

/** Big type over the running game; `word` pops in on its frame. */
const Punch: React.FC<{ at: number; until: number; children: React.ReactNode; size?: number }> = ({ at, until, children, size = 190 }) => {
  const f = useCurrentFrame();
  if (f < at || f >= until) return null;
  const t = f - at;
  return (
    <AbsoluteFill style={{ alignItems: "center", justifyContent: "center" }}>
      <div
        style={{
          fontFamily: SANS,
          fontSize: size,
          fontWeight: 800,
          letterSpacing: "-0.045em",
          lineHeight: 1.02,
          color: C.text,
          textAlign: "center",
          textShadow: "0 10px 60px rgba(0,0,0,0.55)",
          scale: String(tw(t, [0, 10], [1.12, 1])),
          opacity: tw(t, [0, 4], [0, 1], Easing.linear),
          filter: `blur(${tw(t, [0, 6], [10, 0])}px)`,
        }}
      >
        {children}
      </div>
    </AbsoluteFill>
  );
};

const Shade: React.FC<{ o: number }> = ({ o }) => <AbsoluteFill style={{ background: `radial-gradient(ellipse at center, rgba(5,2,15,${o * 0.8}), rgba(5,2,15,${o}))` }} />;

/** 150–270: the race full screen. */
const PlayScene: React.FC = () => {
  const f = useCurrentFrame();
  return (
    <AbsoluteFill>
      <RacingGame t={L.play.from + f} />
      <Shade o={tw(f, [0, 10], [0, 0.35], Easing.linear)} />
      <Punch at={4} until={58}>Your PC.</Punch>
      <Punch at={60} until={120}>
        Now a <span style={gradText(f)}>console.</span>
      </Punch>
      <div style={{ position: "absolute", left: 60, bottom: 50, display: "flex", alignItems: "center", gap: 18, opacity: tw(f, [8, 20], [0, 1], Easing.linear) }}>
        <Logo size={64} draw={1} />
        <span style={{ fontFamily: SANS, fontSize: 38, fontWeight: 800, color: C.text, letterSpacing: "-0.03em", textShadow: "0 4px 20px rgba(0,0,0,0.6)" }}>Console Mode</span>
      </div>
    </AbsoluteFill>
  );
};

/** 270–390: Select + Y opens the menu over the race, which keeps going behind it. */
const MenuScene: React.FC = () => {
  const f = useCurrentFrame();
  const tiles: { icon: IconName; label: string; value: string }[] = [
    { icon: "volume", label: "Volume", value: "72" },
    { icon: "resize", label: "Resolution", value: "4K" },
    { icon: "tv", label: "Audio", value: "TV" },
    { icon: "fps", label: "FPS limit", value: "60" },
    { icon: "hdr", label: "HDR", value: "On" },
    { icon: "exit", label: "Exit", value: "Desk" },
  ];
  const open = tw(f, [0, 14], [0, 1]) * (1 - tw(f, [104, 118], [0, 1]));
  let pos = 0;
  for (let k = 0; k < 4; k++) pos += tw(f, [40 + 15 * k, 47 + 15 * k], [0, 1]);
  const W = 222;
  const GAP = 22;
  const left = (1920 - (6 * W + 5 * GAP)) / 2;
  return (
    <AbsoluteFill>
      <RacingGame t={L.menu.from + f} />
      <Shade o={0.55 * open} />
      <div style={{ opacity: open }}>
        <Center top={150}>
          <Line words={["Hold", "Select", "+", { t: "Y.", grad: true }]} at={2} size={112} stagger={4} style={{ textShadow: "0 6px 40px rgba(0,0,0,0.5)" }} />
          <div style={{ height: 14 }} />
          <Line words={["A", "menu", "over", "any", "game."]} at={12} size={46} weight={600} stagger={2} color="rgba(245,245,247,0.85)" />
        </Center>
      </div>
      {tiles.map((t, i) => {
        const sel = Math.max(0, 1 - Math.abs(pos - i));
        const enter = tw(f - 8 - i * 2, [0, 16], [0, 1]) * open;
        return (
          <div
            key={t.label}
            style={{
              position: "absolute",
              left: left + i * (W + GAP),
              top: 560,
              width: W,
              height: 250,
              borderRadius: 30,
              boxSizing: "border-box",
              padding: 26,
              background: `rgba(16,17,20,${0.72 + sel * 0.1})`,
              backdropFilter: "blur(24px)",
              border: `${1.5 + sel * 1.5}px solid ${sel > 0.5 ? C.mint : "rgba(255,255,255,0.16)"}`,
              boxShadow: sel > 0.3 ? `0 0 ${50 * sel}px ${C.mint}66` : "0 20px 50px rgba(0,0,0,0.5)",
              scale: String(0.94 + sel * 0.1),
              opacity: enter,
              translate: `0 ${(1 - enter) * 140}px`,
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
            }}
          >
            <Icon name={t.icon} size={44} color={sel > 0.5 ? C.mint : C.text} />
            <div>
              <div style={{ fontFamily: SANS, fontSize: 56, fontWeight: 800, color: C.text, letterSpacing: "-0.03em" }}>{t.value}</div>
              <div style={{ fontFamily: SANS, fontSize: 24, fontWeight: 500, color: C.muted }}>{t.label}</div>
            </div>
          </div>
        );
      })}
    </AbsoluteFill>
  );
};

/** 390–510: one word per beat over the race, then the line that sums them up. */
const SpecsScene: React.FC = () => {
  const f = useCurrentFrame();
  const words = ["4K", "120 Hz", "HDR", "VRR", "FPS limit"];
  return (
    <AbsoluteFill>
      <RacingGame t={L.specs.from + f} />
      <Shade o={0.45} />
      {words.map((w, i) => (
        <Punch key={w} at={i * 15} until={i * 15 + 15} size={260}>
          {i % 2 === 0 ? <span style={gradText(f)}>{w}</span> : w}
        </Punch>
      ))}
      <Punch at={75} until={120} size={120}>
        Tuned for your TV.
        <br />
        <span style={{ color: C.muted, fontSize: 60, fontWeight: 600, letterSpacing: "-0.02em" }}>Automatically.</span>
      </Punch>
    </AbsoluteFill>
  );
};

/** 510–630: quit the game; the camera flies out, the TV turns off and the desk comes back. */
const QuitScene: React.FC = () => {
  const f = useCurrentFrame();
  const zoom = 1 - tw(f, [0, 40], [0, 1], Easing.inOut(Easing.cubic));
  const tv = 1 - tw(f, [46, 54], [0, 1], Easing.in(Easing.cubic));
  const chips: { icon: IconName; label: string; at: number }[] = [
    { icon: "screen", label: "Screens", at: 75 },
    { icon: "speaker", label: "Audio", at: 90 },
    { icon: "resize", label: "Resolution", at: 105 },
  ];
  return (
    <AbsoluteFill>
      <Desk t={L.quit.from + f} monA={tw(f, [58, 64], [0, 1], Easing.linear)} monB={tw(f, [64, 70], [0, 1], Easing.linear)} tv={tv} zoom={zoom} />
      <Center top={90}>
        <Line words={["Quit", "the", "game."]} at={40} size={104} stagger={4} />
        <div style={{ height: 6 }} />
        <Line words={["Your", "desk", { t: "comes", grad: true }, { t: "back.", grad: true }]} at={52} size={104} stagger={4} />
      </Center>
      <div style={{ position: "absolute", top: 900, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 30 }}>
        {chips.map((c) => {
          const t = f - c.at + 10;
          return (
            <div
              key={c.label}
              style={{
                width: 380,
                height: 100,
                borderRadius: 28,
                background: "linear-gradient(180deg,#18181b,#101012)",
                border: `1px solid ${C.line}`,
                display: "flex",
                alignItems: "center",
                gap: 20,
                padding: "0 26px",
                boxSizing: "border-box",
                opacity: tw(t, [0, 10], [0, 1], Easing.linear),
                translate: `0 ${tw(t, [0, 18], [50, 0])}px`,
              }}
            >
              <Icon name={c.icon} size={46} />
              <span style={{ flex: 1, fontFamily: SANS, fontSize: 38, fontWeight: 600, color: C.text }}>{c.label}</span>
              <Check p={tw(f, [c.at, c.at + 10], [0, 1])} size={50} />
            </div>
          );
        })}
      </div>
    </AbsoluteFill>
  );
};

// ───────────────────────── on black ─────────────────────────

/** 630–720: hold Home from the couch. */
const HomeScene: React.FC = () => {
  const f = useCurrentFrame();
  const fill = tw(f, [8, 56], [0, 1], Easing.inOut(Easing.quad));
  const done = f >= 56;
  const burst = tw(f, [56, 84], [0, 1]);
  const R = 170;
  const circ = 2 * Math.PI * R;
  return (
    <AbsoluteFill>
      <Glow color={C.mint} y={600} opacity={0.08 + fill * 0.2} size={1000 + fill * 400} />
      <Center top={100}>
        <Line words={["Hold", { t: "Home.", grad: true }]} at={2} size={118} stagger={5} />
      </Center>
      <div style={{ position: "absolute", left: 960 - 220, top: 600 - 220, width: 440, height: 440 }}>
        <svg width={440} height={440} viewBox="0 0 440 440" style={{ position: "absolute", inset: 0, rotate: "-90deg" }}>
          <circle cx="220" cy="220" r={R} fill="none" stroke="rgba(255,255,255,0.10)" strokeWidth="12" />
          <circle cx="220" cy="220" r={R} fill="none" stroke={C.mint} strokeWidth="12" strokeLinecap="round" strokeDasharray={circ} strokeDashoffset={circ * (1 - fill)} style={{ filter: `drop-shadow(0 0 ${10 + fill * 20}px ${C.mint})` }} />
        </svg>
        {done && (
          <div
            style={{
              position: "absolute",
              left: 220 - R * (1 + burst * 0.8),
              top: 220 - R * (1 + burst * 0.8),
              width: 2 * R * (1 + burst * 0.8),
              height: 2 * R * (1 + burst * 0.8),
              borderRadius: "50%",
              border: `6px solid ${C.mint}`,
              opacity: 1 - burst,
            }}
          />
        )}
        <div
          style={{
            position: "absolute",
            left: 105,
            top: 105,
            width: 230,
            height: 230,
            borderRadius: "50%",
            background: done ? `radial-gradient(circle at 40% 35%, #5fe0b6, ${C.mint} 60%, #1f8a67)` : "radial-gradient(circle at 40% 35%, #2c2c31, #151518)",
            border: "1.5px solid rgba(255,255,255,0.18)",
            boxShadow: done ? `0 0 80px ${C.mint}aa` : "0 20px 60px rgba(0,0,0,0.6)",
            scale: String(f >= 6 && f < 58 ? 0.94 : 1),
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <svg width="96" height="96" viewBox="0 0 24 24">
            <path d="M4 11 L12 4 L20 11 V20 H14.5 V14.5 H9.5 V20 H4 Z" fill="none" stroke={done ? "#03130d" : C.text} strokeWidth="1.8" strokeLinejoin="round" />
          </svg>
        </div>
      </div>
      <Center top={880}>
        <Line words={["Start", "from", "the", "couch.", "Xbox", "or", "PlayStation."]} at={50} size={44} weight={600} stagger={2} color={C.muted} />
      </Center>
    </AbsoluteFill>
  );
};

/** 720–810: the three launchers, one per beat. */
const LaunchersScene: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const cards = [
    { name: "Steam", sub: "Big Picture", bg: "linear-gradient(160deg,#1f3a57,#0c1522)", accent: "#66c0f4" },
    { name: "Playnite", sub: "Fullscreen", bg: "linear-gradient(160deg,#3b2466,#120b21)", accent: "#b690ff" },
    { name: "Xbox", sub: "Full-screen mode", bg: "linear-gradient(160deg,#1d4d24,#08170b)", accent: "#5fd36f" },
  ];
  return (
    <AbsoluteFill>
      <Glow color={C.violet} y={760} opacity={0.14} />
      <Center top={110}>
        <Line words={["Your", "launcher."]} at={2} size={112} stagger={5} />
      </Center>
      <div style={{ position: "absolute", top: 360, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 48 }}>
        {cards.map((c, i) => {
          const s = spring({ frame: f - 5 - i * 15, fps, config: { damping: 15, stiffness: 140 } });
          return (
            <div
              key={c.name}
              style={{
                width: 440,
                height: 520,
                borderRadius: 40,
                background: c.bg,
                border: "1px solid rgba(255,255,255,0.14)",
                boxShadow: `0 40px 100px rgba(0,0,0,0.6), 0 0 80px ${c.accent}22`,
                padding: 44,
                boxSizing: "border-box",
                display: "flex",
                flexDirection: "column",
                justifyContent: "flex-end",
                opacity: s,
                translate: `0 ${(1 - s) * 220}px`,
                position: "relative",
                overflow: "hidden",
              }}
            >
              <div style={{ position: "absolute", right: -80, top: -80, width: 320, height: 320, borderRadius: "50%", border: `40px solid ${c.accent}33` }} />
              <div style={{ position: "absolute", right: 30, top: 30, width: 110, height: 110, borderRadius: "50%", background: `radial-gradient(circle at 35% 35%, ${c.accent}, ${c.accent}44)` }} />
              <div style={{ fontFamily: SANS, fontSize: 76, fontWeight: 800, color: "#fff", letterSpacing: "-0.04em" }}>{c.name}</div>
              <div style={{ fontFamily: SANS, fontSize: 34, fontWeight: 500, color: c.accent, marginTop: 6 }}>{c.sub}</div>
            </div>
          );
        })}
      </div>
    </AbsoluteFill>
  );
};

/** 810–960: name, promise, download. */
const FinaleScene: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const pop = spring({ frame: f, fps, config: { damping: 14, stiffness: 110 } });
  const ring = tw(f, [0, 45], [0, 1]);
  const end = tw(f, [118, 150], [0, 1], Easing.inOut(Easing.cubic));
  const btn = spring({ frame: f - 50, fps, config: { damping: 16, stiffness: 120 } });
  return (
    <AbsoluteFill style={{ opacity: 1 - end }}>
      <Glow color={C.mint} y={470} opacity={0.2 + pop * 0.08} size={1600} />
      <div
        style={{
          position: "absolute",
          left: 960 - 400 * ring,
          top: 330 - 400 * ring,
          width: 800 * ring,
          height: 800 * ring,
          borderRadius: "50%",
          border: `3px solid ${C.mint}`,
          opacity: (1 - ring) * 0.8,
        }}
      />
      <Center top={200}>
        <div style={{ display: "flex", justifyContent: "center", scale: String(0.6 + pop * 0.4) }}>
          <Logo size={200} draw={1} glow={pop} />
        </div>
        <div style={{ height: 40 }} />
        <Line words={["Console", "Mode"]} at={4} size={170} weight={800} stagger={4} />
        <div style={{ height: 22 }} />
        <Line words={["Free", "and", "open", "source", "for", { t: "Windows.", grad: true }]} at={26} size={58} weight={600} stagger={3} color={C.muted} />
      </Center>
      <div style={{ position: "absolute", top: 830, left: 0, right: 0, display: "flex", flexDirection: "column", alignItems: "center", gap: 26 }}>
        <div
          style={{
            padding: "20px 50px",
            borderRadius: 999,
            background: C.text,
            color: "#000",
            fontFamily: SANS,
            fontSize: 34,
            fontWeight: 700,
            opacity: btn,
            scale: String(0.85 + btn * 0.15),
          }}
        >
          Download for Windows
        </div>
        <div style={{ fontFamily: SANS, fontSize: 30, fontWeight: 500, color: C.muted, opacity: tw(f, [62, 74], [0, 1], Easing.linear) }}>github.com/lippdev/consolemode</div>
      </div>
    </AbsoluteFill>
  );
};

// ───────────────────────── film ─────────────────────────

/** Straight cut, no fade: used where the race continues from one scene to the next. */
const Cut: React.FC<{ t: { from: number; dur: number }; children: React.ReactNode }> = ({ t, children }) => (
  <Sequence from={t.from} durationInFrames={t.dur}>
    {children}
  </Sequence>
);

/** Blurs and zooms out into black over the last frames; `fadeIn` also fades in from black. */
const ExitFx: React.FC<{ dur: number; fadeIn: boolean; children: React.ReactNode }> = ({ dur, fadeIn, children }) => {
  const f = useCurrentFrame();
  const out = tw(f, [dur - 12, dur], [0, 1], Easing.in(Easing.cubic));
  return (
    <AbsoluteFill
      style={{
        opacity: (fadeIn ? tw(f, [0, 8], [0, 1], Easing.linear) : 1) * (1 - out),
        filter: out > 0 ? `blur(${out * 16}px)` : undefined,
        scale: String(1 + out * 0.06),
      }}
    >
      {children}
    </AbsoluteFill>
  );
};

/** Scene that fades out into black at its end. */
const Faded: React.FC<{ t: { from: number; dur: number }; fadeIn?: boolean; children: React.ReactNode }> = ({ t, fadeIn = true, children }) => (
  <Sequence from={t.from} durationInFrames={t.dur}>
    <ExitFx dur={t.dur} fadeIn={fadeIn}>
      {children}
    </ExitFx>
  </Sequence>
);

export const Launch: React.FC = () => {
  useFonts();
  return (
    <AbsoluteFill style={{ background: C.bg, overflow: "hidden" }}>
      <Audio src={staticFile("keynote/launch.mp3")} />
      <Cut t={L.desk}>
        <DeskScene />
      </Cut>
      <Cut t={L.play}>
        <PlayScene />
      </Cut>
      <Cut t={L.menu}>
        <MenuScene />
      </Cut>
      <Cut t={L.specs}>
        <SpecsScene />
      </Cut>
      <Faded t={L.quit} fadeIn={false}>
        <QuitScene />
      </Faded>
      <Faded t={L.home}>
        <HomeScene />
      </Faded>
      <Faded t={L.launchers}>
        <LaunchersScene />
      </Faded>
      <Cut t={L.finale}>
        <FinaleScene />
      </Cut>
    </AbsoluteFill>
  );
};
