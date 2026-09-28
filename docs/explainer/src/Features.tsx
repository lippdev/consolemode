import React from "react";
import { AbsoluteFill, Easing, interpolate, Sequence, spring, useCurrentFrame, useVideoConfig } from "remotion";
import {
  C,
  Center,
  Check,
  clamp,
  ConsoleWindow,
  Desktop,
  DesktopWindow,
  Game,
  gradText,
  Icon,
  type IconName,
  Line,
  Logo,
  MONO,
  SANS,
  Scene,
  Screen,
  tw,
  useFonts,
} from "./kit";

// Quick tour of the main features of 1.5.0 for the README GIF (assets/features*.gif).
// One feature per scene, same look as the Keynote film but flatter (no big glows) so the
// GIF palette stays clean. Rendered in English and Portuguese from the `lang` prop.

type Lang = "en" | "pt";

const T = {
  en: {
    tagline: "Your PC becomes a console in one click.",
    scenes: [
      ["Pick your screen", "Play on the TV. The rest goes dark."],
      ["Tuned for the TV", "Resolution, HDR, VRR, FPS and audio."],
      ["Your launcher", "Big Picture, Playnite or Xbox."],
      ["Automatic restore", "Quit the game. Your desk comes back."],
      ["From the couch", "Hold Home to start."],
      ["Menu over the game", "Hold Select + Y."],
      ["Back to the PC", "Hold Start + Select."],
      ["Two interfaces", "Mouse or controller."],
      ["Automation", "Links, command line and a local API."],
      ["And more", "Light, simple and free."],
    ],
    specs: ["3840 × 2160 · 120 Hz", "HDR on", "VRR on", "FPS limit 60", "Audio on the TV"],
    restored: ["Screens", "Audio", "Resolution"],
    tiles: ["Volume", "Resolution", "Audio", "FPS", "HDR", "Exit"],
    values: ["72", "4K", "TV", "60", "On", "Desk"],
    pads: "Xbox and PlayStation controllers",
    desktop: { screens: ["Living room TV", "Monitor 1", "Monitor 2"], play: "Play now" },
    console: { tiles: ["Play", "Screens", "Settings"], select: "Select", back: "Back" },
    labels: ["Desktop", "Console"],
    keys: ["Start", "Menu", "Stop"],
    chips: ["No admin", "Portable version", "Updates itself", "English · Português · Español"],
    outro: "Free and open source · Windows 10 and 11",
  },
  pt: {
    tagline: "Seu PC vira um console com um clique.",
    scenes: [
      ["Escolha a tela", "Joga na TV. O resto apaga."],
      ["Ajustado para a TV", "Resolução, HDR, VRR, FPS e áudio."],
      ["Seu launcher", "Big Picture, Playnite ou Xbox."],
      ["Volta automática", "Saiu do jogo, a mesa volta."],
      ["Do sofá", "Segure Home para começar."],
      ["Menu sobre o jogo", "Segure Select + Y."],
      ["De volta ao PC", "Segure Start + Select."],
      ["Duas interfaces", "Mouse ou controle."],
      ["Automação", "Links, linha de comando e API local."],
      ["E mais", "Leve, simples e gratuito."],
    ],
    specs: ["3840 × 2160 · 120 Hz", "HDR ligado", "VRR ligado", "Limite de 60 FPS", "Áudio na TV"],
    restored: ["Telas", "Áudio", "Resolução"],
    tiles: ["Volume", "Resolução", "Áudio", "FPS", "HDR", "Sair"],
    values: ["72", "4K", "TV", "60", "Lig.", "Mesa"],
    pads: "Controles de Xbox e PlayStation",
    desktop: { screens: ["TV da sala", "Monitor 1", "Monitor 2"], play: "Jogar agora" },
    console: { tiles: ["Jogar", "Telas", "Ajustes"], select: "Selecionar", back: "Voltar" },
    labels: ["Desktop", "Console"],
    keys: ["Iniciar", "Menu", "Parar"],
    chips: ["Sem admin", "Versão portátil", "Se atualiza sozinho", "English · Português · Español"],
    outro: "Grátis e open source · Windows 10 e 11",
  },
};
type Strings = (typeof T)["en"];

/** Scene lengths in frames (30 fps); the first and last are the title and the outro. */
const DURS = [60, 105, 105, 90, 105, 75, 105, 75, 105, 90, 75, 90];
const STARTS = DURS.map((_, i) => DURS.slice(0, i).reduce((a, b) => a + b, 0));
export const FEATURES_DURATION = DURS.reduce((a, b) => a + b, 0);

// ───────────────────────── pieces ─────────────────────────

const Header: React.FC<{ n: number; t: Strings }> = ({ n, t }) => {
  const f = useCurrentFrame();
  const [kicker, title] = t.scenes[n];
  return (
    <Center top={80}>
      <div
        style={{
          textAlign: "center",
          fontFamily: SANS,
          fontSize: 30,
          fontWeight: 700,
          letterSpacing: "0.14em",
          textTransform: "uppercase",
          color: C.mint,
          opacity: tw(f, [0, 10], [0, 1], Easing.linear),
          marginBottom: 18,
        }}
      >
        {String(n + 1).padStart(2, "0")} · {kicker}
      </div>
      <Line words={title.split(" ")} at={4} size={84} stagger={3} />
    </Center>
  );
};

/** One segment per feature at the bottom, the current one in mint. */
const Progress: React.FC<{ n: number }> = ({ n }) => {
  const f = useCurrentFrame();
  const fill = tw(f, [0, DURS[n + 1] - 10], [0, 1], Easing.linear);
  return (
    <div style={{ position: "absolute", bottom: 50, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 10 }}>
      {Array.from({ length: 10 }, (_, i) => (
        <div key={i} style={{ width: 64, height: 6, borderRadius: 3, background: i < n ? "rgba(255,255,255,0.45)" : "rgba(255,255,255,0.14)", overflow: "hidden" }}>
          {i === n && <div style={{ width: `${fill * 100}%`, height: "100%", background: C.mint }} />}
        </div>
      ))}
    </div>
  );
};

const Feature: React.FC<{ n: number; t: Strings; children: React.ReactNode }> = ({ n, t, children }) => (
  <AbsoluteFill>
    <Header n={n} t={t} />
    {children}
    <Progress n={n} />
  </AbsoluteFill>
);

const Row: React.FC<{ top: number; gap?: number; children: React.ReactNode }> = ({ top, gap = 36, children }) => (
  <div style={{ position: "absolute", top, left: 0, right: 0, display: "flex", justifyContent: "center", gap }}>{children}</div>
);

/** Fades and lifts something in `at` frames after the scene starts. */
const rise = (f: number, at: number, dist = 60): React.CSSProperties => ({
  opacity: tw(f - at, [0, 10], [0, 1], Easing.linear),
  translate: `0 ${tw(f - at, [0, 18], [dist, 0])}px`,
});

const pill: React.CSSProperties = {
  display: "flex",
  alignItems: "center",
  gap: 20,
  padding: "0 30px",
  height: 96,
  borderRadius: 28,
  background: "#141416",
  border: `1px solid ${C.line}`,
  fontFamily: SANS,
  fontSize: 36,
  fontWeight: 600,
  color: C.text,
  boxSizing: "border-box",
};

// ───────────────────────── scenes ─────────────────────────

const Title: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const pop = spring({ frame: f, fps, config: { damping: 18, stiffness: 110 } });
  return (
    <AbsoluteFill>
      <Center>
        <div style={{ display: "flex", justifyContent: "center", scale: String(0.8 + pop * 0.2) }}>
          <Logo size={170} draw={tw(f, [0, 30], [0, 1], Easing.inOut(Easing.cubic))} />
        </div>
        <div style={{ height: 36 }} />
        <Line words={["Console", "Mode", { t: "1.5", grad: true }]} at={8} size={150} weight={800} stagger={4} />
        <div style={{ height: 24 }} />
        <Line words={t.tagline.split(" ")} at={22} size={48} weight={500} stagger={2} color={C.muted} />
      </Center>
    </AbsoluteFill>
  );
};

/** 01: the desk monitors turn off and the TV turns on. */
const PickScreen: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const pressed = interpolate(f, [18, 22, 28], [0, 1, 0], clamp);
  const tvOn = tw(f, [52, 62], [0, 1], Easing.out(Easing.cubic));
  return (
    <Feature n={0} t={t}>
      <Screen x={170} y={420} w={430} h={256} power={1 - tw(f, [34, 42], [0, 1], Easing.linear)} kind="monitor">
        <Desktop />
      </Screen>
      <Screen x={630} y={420} w={430} h={256} power={1 - tw(f, [44, 52], [0, 1], Easing.linear)} kind="monitor">
        <Desktop app pressed={pressed} />
      </Screen>
      <Screen x={1100} y={330} w={660} h={380} power={1} kind="tv">
        <AbsoluteFill style={{ scale: `1 ${Math.max(0.005, tvOn)}`, opacity: tvOn > 0 ? 1 : 0 }}>
          <Game />
        </AbsoluteFill>
      </Screen>
    </Feature>
  );
};

/** 02: what the TV gets while you play. */
const Tuned: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const icons: IconName[] = ["resize", "hdr", "fps", "fps", "speaker"];
  return (
    <Feature n={1} t={t}>
      <Screen x={170} y={340} w={820} h={470} power={1} kind="tv">
        <Game />
      </Screen>
      <div style={{ position: "absolute", left: 1060, top: 330, display: "flex", flexDirection: "column", gap: 16 }}>
        {t.specs.map((s, i) => (
          <div key={s} style={{ ...pill, width: 690, height: 90, ...rise(f, 12 + i * 8, 40) }}>
            <Icon name={icons[i]} size={42} color={C.mint} />
            {s}
          </div>
        ))}
      </div>
    </Feature>
  );
};

/** 03: the three launchers. */
const Launchers: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const cards = [
    { name: "Steam", sub: "Big Picture", bg: "#15293d", accent: "#66c0f4" },
    { name: "Playnite", sub: "Fullscreen", bg: "#261844", accent: "#b690ff" },
    { name: "Xbox", sub: "Full screen", bg: "#123317", accent: "#5fd36f" },
  ];
  return (
    <Feature n={2} t={t}>
      <Row top={350} gap={44}>
        {cards.map((c, i) => {
          const s = spring({ frame: f - 8 - i * 7, fps, config: { damping: 16, stiffness: 100 } });
          return (
            <div
              key={c.name}
              style={{
                width: 430,
                height: 480,
                borderRadius: 40,
                background: c.bg,
                border: "1px solid rgba(255,255,255,0.14)",
                padding: 44,
                boxSizing: "border-box",
                display: "flex",
                flexDirection: "column",
                justifyContent: "flex-end",
                position: "relative",
                overflow: "hidden",
                opacity: s,
                translate: `0 ${(1 - s) * 200}px`,
              }}
            >
              <div style={{ position: "absolute", right: -70, top: -70, width: 300, height: 300, borderRadius: "50%", border: `38px solid ${c.accent}40` }} />
              <div style={{ position: "absolute", right: 34, top: 34, width: 100, height: 100, borderRadius: "50%", background: c.accent }} />
              <div style={{ fontFamily: SANS, fontSize: 74, fontWeight: 800, color: "#fff", letterSpacing: "-0.04em" }}>{c.name}</div>
              <div style={{ fontFamily: SANS, fontSize: 34, fontWeight: 500, color: c.accent, marginTop: 6 }}>{c.sub}</div>
            </div>
          );
        })}
      </Row>
    </Feature>
  );
};

/** 04: the TV turns off, the desk comes back, and each item gets its check. */
const Restore: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const tv = 1 - tw(f, [10, 18], [0, 1], Easing.linear);
  const icons: IconName[] = ["screen", "speaker", "resize"];
  return (
    <Feature n={3} t={t}>
      <Screen x={330} y={320} w={330} h={196} power={tw(f, [20, 28], [0, 1], Easing.linear)} kind="monitor">
        <Desktop />
      </Screen>
      <Screen x={690} y={320} w={330} h={196} power={tw(f, [28, 36], [0, 1], Easing.linear)} kind="monitor">
        <Desktop />
      </Screen>
      <Screen x={1080} y={260} w={500} h={288} power={1} kind="tv">
        <AbsoluteFill style={{ opacity: tv }}>
          <Game />
        </AbsoluteFill>
      </Screen>
      <Row top={700} gap={30}>
        {t.restored.map((r, i) => (
          <div key={r} style={{ ...pill, width: 440, ...rise(f, 34 + i * 8, 40) }}>
            <Icon name={icons[i]} size={48} />
            <span style={{ flex: 1 }}>{r}</span>
            <Check p={tw(f, [44 + i * 10, 56 + i * 10], [0, 1])} />
          </div>
        ))}
      </Row>
    </Feature>
  );
};

/** 05: holding Home fills the ring. */
const Home: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const fill = tw(f, [10, 48], [0, 1], Easing.inOut(Easing.quad));
  const done = f >= 48;
  const R = 150;
  const circ = 2 * Math.PI * R;
  return (
    <Feature n={4} t={t}>
      <div style={{ position: "absolute", left: 960 - 190, top: 590 - 190, width: 380, height: 380 }}>
        <svg width={380} height={380} viewBox="0 0 380 380" style={{ position: "absolute", inset: 0, rotate: "-90deg" }}>
          <circle cx="190" cy="190" r={R} fill="none" stroke="rgba(255,255,255,0.12)" strokeWidth="12" />
          <circle cx="190" cy="190" r={R} fill="none" stroke={C.mint} strokeWidth="12" strokeLinecap="round" strokeDasharray={circ} strokeDashoffset={circ * (1 - fill)} />
        </svg>
        <div
          style={{
            position: "absolute",
            left: 190 - 100,
            top: 190 - 100,
            width: 200,
            height: 200,
            borderRadius: "50%",
            background: done ? C.mint : "#1c1c1f",
            border: "1.5px solid rgba(255,255,255,0.18)",
            scale: String(f >= 8 && f < 50 ? 0.94 : 1),
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <svg width="84" height="84" viewBox="0 0 24 24">
            <path d="M4 11 L12 4 L20 11 V20 H14.5 V14.5 H9.5 V20 H4 Z" fill="none" stroke={done ? "#03130d" : C.text} strokeWidth="1.8" strokeLinejoin="round" />
          </svg>
        </div>
      </div>
      <Center top={830}>
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 34, fontWeight: 500, color: C.muted, opacity: tw(f, [20, 30], [0, 1], Easing.linear) }}>{t.pads}</div>
      </Center>
    </Feature>
  );
};

/** 06: the session menu over the game, the highlight walking across the tiles. */
const Menu: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const icons: IconName[] = ["volume", "resize", "tv", "fps", "hdr", "exit"];
  let pos = 0;
  for (let k = 0; k < 5; k++) pos += tw(f, [30 + 12 * k, 36 + 12 * k], [0, 1]);
  const W = 222;
  const GAP = 22;
  const left = (1920 - (6 * W + 5 * GAP)) / 2;
  return (
    <AbsoluteFill>
      <Game dim={0.55} />
      <Header n={5} t={t} />
      {t.tiles.map((label, i) => {
        const sel = Math.max(0, 1 - Math.abs(pos - i));
        return (
          <div
            key={label}
            style={{
              position: "absolute",
              left: left + i * (W + GAP),
              top: 480,
              width: W,
              height: 250,
              borderRadius: 30,
              boxSizing: "border-box",
              padding: 26,
              background: "#111214",
              border: `${1.5 + sel * 1.5}px solid ${sel > 0.5 ? C.mint : "rgba(255,255,255,0.16)"}`,
              scale: String(0.94 + sel * 0.1),
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
              ...rise(f, 8 + i * 3, 100),
            }}
          >
            <Icon name={icons[i]} size={44} color={sel > 0.5 ? C.mint : C.text} />
            <div>
              <div style={{ fontFamily: SANS, fontSize: 54, fontWeight: 800, color: C.text, letterSpacing: "-0.03em" }}>{t.values[i]}</div>
              <div style={{ fontFamily: SANS, fontSize: 24, fontWeight: 500, color: C.muted }}>{label}</div>
            </div>
          </div>
        );
      })}
      <Progress n={5} />
    </AbsoluteFill>
  );
};

/** A controller button drawn as a key cap. */
const Cap: React.FC<{ label: string; lit: number }> = ({ label, lit }) => (
  <div
    style={{
      padding: "0 48px",
      height: 150,
      minWidth: 300,
      boxSizing: "border-box",
      borderRadius: 75,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontFamily: SANS,
      fontSize: 56,
      fontWeight: 800,
      color: lit > 0.5 ? "#03130d" : C.text,
      background: lit > 0.5 ? C.mint : "#1c1c1f",
      border: `2px solid ${lit > 0.5 ? C.mint : "rgba(255,255,255,0.18)"}`,
      scale: String(1 - lit * 0.06),
    }}
  >
    {label}
  </div>
);

/** 07: Start + Select, then the desk is back. */
const BackToPc: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const lit = f >= 12 && f < 40 ? 1 : 0;
  return (
    <Feature n={6} t={t}>
      <Row top={430} gap={40}>
        <Cap label="Start" lit={lit} />
        <div style={{ fontFamily: SANS, fontSize: 80, fontWeight: 300, color: C.muted, lineHeight: "150px" }}>+</div>
        <Cap label="Select" lit={lit} />
      </Row>
      <Row top={660} gap={24}>
        {[0, 1].map((i) => (
          <div key={i} style={{ position: "relative", width: 260, height: 240 }}>
            <Screen x={0} y={0} w={260} h={154} power={tw(f, [40 + i * 6, 48 + i * 6], [0, 1], Easing.linear)} kind="monitor">
              <Desktop />
            </Screen>
          </div>
        ))}
      </Row>
    </Feature>
  );
};

/** 08: the two interfaces side by side. */
const Interfaces: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const label: React.CSSProperties = { fontFamily: SANS, fontSize: 38, fontWeight: 700, color: C.text, textAlign: "center", marginTop: 26 };
  return (
    <Feature n={7} t={t}>
      <Row top={330} gap={70}>
        <div style={rise(f, 8, 40)}>
          <DesktopWindow t={t.desktop} />
          <div style={label}>{t.labels[0]}</div>
        </div>
        <div style={rise(f, 18, 40)}>
          <ConsoleWindow focus={f % 20 < 10 ? 1 : 0.6} t={t.console} />
          <div style={label}>{t.labels[1]}</div>
        </div>
      </Row>
    </Feature>
  );
};

/** 09: consolemode:// typed out, then the Start / Menu / Stop keys light up. */
const Automation: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const cmd = "consolemode://start";
  const typed = Math.floor(interpolate(f, [8, 38], [0, cmd.length], clamp));
  const keys: IconName[] = ["play", "menu", "stop"];
  return (
    <Feature n={8} t={t}>
      <Row top={330}>
        <div style={{ ...pill, height: 120, minWidth: 880, fontFamily: MONO, fontSize: 50, fontWeight: 500, padding: "0 44px", ...rise(f, 2, 30) }}>
          <span style={{ color: C.mint }}>›</span>
          <span>
            {cmd.slice(0, typed)}
            <span style={{ display: "inline-block", width: 24, height: 54, marginLeft: 4, verticalAlign: "middle", background: C.mint, opacity: f < 40 || Math.floor(f / 8) % 2 === 0 ? 1 : 0 }} />
          </span>
        </div>
      </Row>
      <Row top={540} gap={40}>
        {keys.map((k, i) => {
          const at = 50 + i * 10;
          const lit = f >= at && f < at + 8 ? 1 : 0;
          return (
            <div
              key={k}
              style={{
                width: 190,
                height: 190,
                borderRadius: 36,
                background: lit ? C.mint : "#161618",
                border: `1.5px solid ${lit ? C.mint : "rgba(255,255,255,0.16)"}`,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                gap: 12,
                ...rise(f, 38 + i * 3, 50),
              }}
            >
              <Icon name={k} size={60} color={lit ? "#03130d" : C.text} />
              <span style={{ fontFamily: SANS, fontSize: 28, fontWeight: 700, color: lit ? "#03130d" : C.text }}>{t.keys[i]}</span>
            </div>
          );
        })}
      </Row>
    </Feature>
  );
};

/** 10: the rest, as chips. */
const More: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  return (
    <Feature n={9} t={t}>
      <div style={{ position: "absolute", top: 360, left: 260, right: 260, display: "flex", flexWrap: "wrap", justifyContent: "center", gap: 28 }}>
        {t.chips.map((c, i) => (
          <div key={c} style={{ ...pill, fontSize: 44, height: 120, padding: "0 44px", ...rise(f, 8 + i * 7, 40) }}>
            <Check p={tw(f, [14 + i * 7, 24 + i * 7], [0, 1])} size={50} />
            {c}
          </div>
        ))}
      </div>
    </Feature>
  );
};

const Outro: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  return (
    <AbsoluteFill>
      <Center>
        <div style={{ display: "flex", justifyContent: "center" }}>
          <Logo size={150} draw={1} />
        </div>
        <div style={{ height: 34 }} />
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 130, fontWeight: 800, letterSpacing: "-0.045em", color: C.text, ...rise(f, 0, 30) }}>
          Console Mode <span style={gradText(0)}>1.5.0</span>
        </div>
        <div style={{ height: 24 }} />
        <Line words={t.outro.split(" ")} at={10} size={42} weight={500} stagger={1} color={C.muted} />
        <div style={{ height: 40 }} />
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 38, fontWeight: 700, color: C.mint, opacity: tw(f, [24, 34], [0, 1], Easing.linear) }}>
          github.com/lippdev/consolemode
        </div>
      </Center>
    </AbsoluteFill>
  );
};

// ───────────────────────── tour ─────────────────────────

export const Features: React.FC<{ lang: Lang }> = ({ lang }) => {
  useFonts();
  const t = T[lang];
  const scenes: React.FC<{ t: Strings }>[] = [Title, PickScreen, Tuned, Launchers, Restore, Home, Menu, BackToPc, Interfaces, Automation, More, Outro];
  return (
    <AbsoluteFill style={{ background: C.bg, overflow: "hidden" }}>
      {scenes.map((S, i) =>
        i === scenes.length - 1 ? (
          <Sequence key={i} from={STARTS[i]} durationInFrames={DURS[i]}>
            <S t={t} />
          </Sequence>
        ) : (
          <Scene key={i} from={STARTS[i]} dur={DURS[i]}>
            <S t={t} />
          </Scene>
        ),
      )}
    </AbsoluteFill>
  );
};
