import React from "react";
import { AbsoluteFill, Easing, Freeze, interpolate, Sequence, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Badge, G, type Glyph } from "./Alpha3";
import { C, Center, Check, clamp, Desktop, DesktopWindow, Game, Line, Logo, MONO, SANS, Screen, tw, useFonts } from "./kit";

// The README GIF about Console Mode itself (not a release): who it is for, the chores it
// removes, and what it delivers. Built for GIF quality: 25 fps (a frame rate GIF delays can
// hold exactly), flat colours (no blur, glow or big gradients that band and bloat a GIF),
// frozen game pictures, and type sized to read at 1280 px. English and Portuguese (`lang`).

type Lang = "en" | "pt";

const T = {
  en: {
    tagline: "Turn your Windows PC into a game console.",
    scenes: [
      ["Who it's for", "Made for people who play on the TV."],
      ["Without it", "Every time you want to play on the TV…"],
      ["One click", "The TV turns on. The desk goes dark."],
      ["Tuned for the TV", "Resolution, HDR, VRR, FPS and audio."],
      ["Your launcher", "Big Picture, Playnite or Xbox."],
      ["From the couch", "Your buttons, your shortcuts."],
      ["Menu over the game", "Windows, files, volume, HDR. Without leaving."],
      ["Two interfaces", "Mouse at the desk. Controller on the couch."],
      ["Automatic restore", "Quit the game. Your desk comes back."],
      ["Automation", "Stream Deck, links, command line, local API."],
      ["Free and open source", "Small, simple and yours."],
    ],
    who: [
      ["tv", "PC + TV", "Your PC drives the living room TV."],
      ["monitor", "Multi-monitor desks", "The desk goes dark while you play."],
      ["controller", "Couch players", "Everything from the controller."],
      ["play", "Steam, Playnite, Xbox", "Your library, your launcher."],
      ["menu", "Stream Deck & scripts", "Start it from anywhere."],
    ],
    chores: [
      "Turn off the desk monitors",
      "Move the audio to the TV",
      "Set resolution, HDR and VRR",
      "Turn on the TV and pick the input",
      "Open Big Picture or Playnite",
      "Undo it all when you're done",
    ],
    punch: "Console Mode does it in one click.",
    input: "TV on · Input: PC",
    specs: ["3840 × 2160 · 120 Hz", "HDR on", "VRR on", "FPS limit 60", "FPS counter", "Audio on the TV"],
    launchers: ["Big Picture", "Fullscreen", "Full screen"],
    shortcuts: [
      ["Xbox", "Start console mode"],
      ["Select + Y", "Menu over the game"],
      ["Start + Select", "Back to the PC"],
    ],
    pads: "Xbox and PlayStation controllers · you pick the buttons",
    menu: {
      controlFs: "File explorer (ControlFS)",
      back: "Back to the game",
      volume: "Volume",
      res: "Resolution and refresh rate",
      hdr: "HDR on the gaming display",
      on: "On",
      windows: "Windows",
      open: (n: number) => `${n} open`,
    },
    hints: { select: "Select", close: "Close window", back: "Back to the game" },
    desktop: { screens: ["Living room TV", "Monitor 1", "Monitor 2"], play: "Play now" },
    console: { tabs: ["Home", "Session", "System"], heading: "Where do you want to play?", summary: "Play on Living room TV", play: "Play now" },
    labels: ["Desktop", "Console"],
    restored: ["Screens", "Audio", "Resolution"],
    exact: "Exactly as it was. Portrait monitors too.",
    keys: ["Start", "Menu", "Stop"],
    chips: ["Free and open source", "No admin", "Updates itself", "Portable version", "English · Português · Español", "Windows 10 and 11"],
    outro: "Your PC. Now a console.",
  },
  pt: {
    tagline: "Transforme seu PC com Windows em um console.",
    scenes: [
      ["Para quem é", "Feito para quem joga na TV."],
      ["Sem ele", "Toda vez que você quer jogar na TV…"],
      ["Um clique", "A TV liga. A mesa apaga."],
      ["Ajustado para a TV", "Resolução, HDR, VRR, FPS e áudio."],
      ["Seu launcher", "Big Picture, Playnite ou Xbox."],
      ["Do sofá", "Seus botões, seus atalhos."],
      ["Menu sobre o jogo", "Janelas, arquivos, volume, HDR. Sem sair."],
      ["Duas interfaces", "Mouse na mesa. Controle no sofá."],
      ["Volta automática", "Saiu do jogo, a mesa volta."],
      ["Automação", "Stream Deck, links, linha de comando, API local."],
      ["Grátis e open source", "Leve, simples e seu."],
    ],
    who: [
      ["tv", "PC + TV", "Seu PC comanda a TV da sala."],
      ["monitor", "Vários monitores", "A mesa apaga enquanto você joga."],
      ["controller", "Quem joga no sofá", "Tudo pelo controle."],
      ["play", "Steam, Playnite, Xbox", "Sua biblioteca, seu launcher."],
      ["menu", "Stream Deck e scripts", "Comece de qualquer lugar."],
    ],
    chores: [
      "Desligar os monitores da mesa",
      "Passar o áudio para a TV",
      "Ajustar resolução, HDR e VRR",
      "Ligar a TV e trocar a entrada",
      "Abrir o Big Picture ou o Playnite",
      "Desfazer tudo no final",
    ],
    punch: "O Console Mode faz tudo com um clique.",
    input: "TV ligada · Entrada: PC",
    specs: ["3840 × 2160 · 120 Hz", "HDR ligado", "VRR ligado", "Limite de 60 FPS", "Contador de FPS", "Áudio na TV"],
    launchers: ["Big Picture", "Tela cheia", "Tela cheia"],
    shortcuts: [
      ["Xbox", "Entrar no modo console"],
      ["Select + Y", "Menu sobre o jogo"],
      ["Start + Select", "Voltar ao PC"],
    ],
    pads: "Controles de Xbox e PlayStation · você escolhe os botões",
    menu: {
      controlFs: "Explorador de arquivos (ControlFS)",
      back: "Voltar ao jogo",
      volume: "Volume",
      res: "Resolução e taxa de atualização",
      hdr: "HDR na tela de jogo",
      on: "Ligado",
      windows: "Janelas",
      open: (n: number) => `${n} abertas`,
    },
    hints: { select: "Selecionar", close: "Fechar janela", back: "Voltar ao jogo" },
    desktop: { screens: ["TV da sala", "Monitor 1", "Monitor 2"], play: "Jogar agora" },
    console: { tabs: ["Início", "Sessão", "Sistema"], heading: "Onde você vai jogar?", summary: "Jogar na TV da sala", play: "Jogar agora" },
    labels: ["Desktop", "Console"],
    restored: ["Telas", "Áudio", "Resolução"],
    exact: "Do jeito que estava. Até monitor em retrato.",
    keys: ["Iniciar", "Menu", "Parar"],
    chips: ["Grátis e open source", "Sem admin", "Se atualiza sozinho", "Versão portátil", "English · Português · Español", "Windows 10 e 11"],
    outro: "Seu PC. Agora um console.",
  },
};
type Strings = (typeof T)["en"];

export const SHOWCASE_FPS = 25;
/** Scene lengths in frames at 25 fps; the first and last are the title and the outro. */
const DURS = [75, 125, 130, 125, 100, 90, 115, 125, 110, 100, 100, 100, 90];
const STARTS = DURS.map((_, i) => DURS.slice(0, i).reduce((a, b) => a + b, 0));
const COUNT = DURS.length - 2;
export const SHOWCASE_DURATION = DURS.reduce((a, b) => a + b, 0);

const ACCENT = "#4FCFA3";
const CARD = "#15171b";
const EDGE = "#2a2d33";
const SUB = "rgba(255,255,255,0.7)";

// ───────────────────────── pieces ─────────────────────────

const Header: React.FC<{ n: number; t: Strings }> = ({ n, t }) => {
  const f = useCurrentFrame();
  const [kicker, title] = t.scenes[n];
  return (
    <Center top={64}>
      <div
        style={{
          textAlign: "center",
          fontFamily: SANS,
          fontSize: 30,
          fontWeight: 700,
          letterSpacing: "0.14em",
          textTransform: "uppercase",
          color: ACCENT,
          opacity: tw(f, [0, 8], [0, 1], Easing.linear),
          marginBottom: 14,
        }}
      >
        {kicker}
      </div>
      <Line words={title.split(" ")} at={3} size={78} stagger={2} />
    </Center>
  );
};

const Progress: React.FC<{ n: number }> = ({ n }) => {
  const f = useCurrentFrame();
  const fill = tw(f, [0, DURS[n + 1] - 8], [0, 1], Easing.linear);
  return (
    <div style={{ position: "absolute", bottom: 38, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 10 }}>
      {Array.from({ length: COUNT }, (_, i) => (
        <div key={i} style={{ width: 60, height: 6, borderRadius: 3, background: i < n ? "rgba(255,255,255,0.45)" : "rgba(255,255,255,0.14)", overflow: "hidden" }}>
          {i === n && <div style={{ width: `${fill * 100}%`, height: "100%", background: ACCENT }} />}
        </div>
      ))}
    </div>
  );
};

const Feature: React.FC<{ n: number; t: Strings; children: React.ReactNode; bg?: React.ReactNode }> = ({ n, t, children, bg }) => (
  <AbsoluteFill>
    {bg}
    <Header n={n} t={t} />
    {children}
    <Progress n={n} />
  </AbsoluteFill>
);

const rise = (f: number, at: number, dist = 50): React.CSSProperties => ({
  opacity: tw(f - at, [0, 8], [0, 1], Easing.linear),
  translate: `0 ${tw(f - at, [0, 14], [dist, 0])}px`,
});

const near = (pos: number, i: number) => Math.max(0, 1 - Math.abs(pos - i));

const steps = (f: number, marks: [number, number][], start: number) => {
  let v = start;
  let prev = start;
  for (const [at, to] of marks) {
    v += (to - prev) * tw(f, [at, at + 4], [0, 1]);
    prev = to;
  }
  return v;
};

const Circle: React.FC<{ glyph: Glyph; size?: number; bg?: string; color?: string }> = ({ glyph, size = 60, bg = "rgba(255,255,255,0.12)", color = "#fff" }) => (
  <div style={{ width: size, height: size, borderRadius: size / 2, background: bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
    <G name={glyph} size={size * 0.48} color={color} />
  </div>
);

const Hint: React.FC<{ b: string; label: string; lit?: number }> = ({ b, label, lit = 0 }) => (
  <span style={{ display: "flex", alignItems: "center", gap: 12, fontFamily: SANS, fontSize: 28, color: C.text }}>
    <Badge label={b} lit={lit} h={40} />
    {label}
  </span>
);

/** A game picture that does not move (moving pixels are what make a GIF heavy). */
const StillGame: React.FC<{ dim?: number }> = ({ dim = 0 }) => (
  <Freeze frame={40}>
    <Game dim={dim} />
  </Freeze>
);

const pill: React.CSSProperties = {
  display: "flex",
  alignItems: "center",
  gap: 20,
  padding: "0 30px",
  height: 96,
  borderRadius: 28,
  background: CARD,
  border: `1px solid ${EDGE}`,
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
          <Logo size={190} draw={tw(f, [0, 28], [0, 1], Easing.inOut(Easing.cubic))} />
        </div>
        <div style={{ height: 40 }} />
        <Line words={["Console", "Mode"]} at={8} size={170} weight={800} stagger={4} />
        <div style={{ height: 26 }} />
        <Line words={t.tagline.split(" ")} at={20} size={52} weight={500} stagger={2} color={C.muted} />
      </Center>
    </AbsoluteFill>
  );
};

/** Who it's for: five kinds of player. */
const Who: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const W = 500;
  const H = 230;
  const GAP = 28;
  const pos = (i: number) => {
    const row = i < 3 ? 0 : 1;
    const inRow = row === 0 ? 3 : 2;
    const col = row === 0 ? i : i - 3;
    const left = (1920 - (inRow * W + (inRow - 1) * GAP)) / 2;
    return { left: left + col * (W + GAP), top: 300 + row * (H + GAP) };
  };
  return (
    <Feature n={0} t={t}>
      {t.who.map(([g, title, sub], i) => (
        <div
          key={title}
          style={{
            position: "absolute",
            ...pos(i),
            width: W,
            height: H,
            borderRadius: 30,
            background: CARD,
            border: `1px solid ${EDGE}`,
            padding: "30px 34px",
            boxSizing: "border-box",
            display: "flex",
            flexDirection: "column",
            justifyContent: "space-between",
            fontFamily: SANS,
            ...rise(f, 10 + i * 8, 40),
          }}
        >
          <Circle glyph={g as Glyph} size={68} bg="#173a30" color={ACCENT} />
          <div>
            <div style={{ fontSize: 38, fontWeight: 700, color: C.text, letterSpacing: "-0.02em" }}>{title}</div>
            <div style={{ fontSize: 27, color: C.muted, marginTop: 6 }}>{sub}</div>
          </div>
        </div>
      ))}
    </Feature>
  );
};

/** Without it: the chores, struck out one by one, then the one click. */
const Problem: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const strikeAt = (i: number) => 62 + i * 6;
  const punch = f >= 100;
  return (
    <Feature n={1} t={t}>
      <div style={{ position: "absolute", left: 960 - 760, top: 290, width: 1520, display: "grid", gridTemplateColumns: "1fr 1fr", gap: 22 }}>
        {t.chores.map((c, i) => {
          const s = tw(f, [strikeAt(i), strikeAt(i) + 7], [0, 1]);
          return (
            <div key={c} style={{ ...pill, height: 104, fontSize: 34, opacity: 1 - s * 0.55, ...rise(f, 8 + i * 6, 30) }}>
              <span style={{ width: 44, height: 44, borderRadius: 12, border: `3px solid ${s > 0.5 ? "#ff6b6b" : "rgba(255,255,255,0.35)"}`, display: "flex", alignItems: "center", justifyContent: "center", color: "#ff6b6b", fontSize: 30, fontWeight: 800, flexShrink: 0 }}>
                {s > 0.5 ? "✕" : ""}
              </span>
              <span style={{ position: "relative" }}>
                {c}
                <span style={{ position: "absolute", left: 0, top: "52%", height: 4, width: `${s * 100}%`, background: "#ff6b6b", borderRadius: 2 }} />
              </span>
            </div>
          );
        })}
      </div>
      <div style={{ position: "absolute", top: 720, left: 0, right: 0, display: "flex", justifyContent: "center", opacity: punch ? 1 : 0 }}>
        <div style={{ padding: "26px 54px", borderRadius: 999, background: ACCENT, color: "#03130d", fontFamily: SANS, fontSize: 54, fontWeight: 800, letterSpacing: "-0.02em", scale: String(tw(f, [100, 110], [0.85, 1])) }}>
          {t.punch}
        </div>
      </div>
    </Feature>
  );
};

/** FPS counter in the corner of the game, like the RTSS overlay. */
const FpsBadge: React.FC<{ value: number; scale?: number }> = ({ value, scale = 1 }) => (
  <div style={{ position: "absolute", left: 12 * scale, top: 12 * scale, padding: `${4 * scale}px ${10 * scale}px`, borderRadius: 6 * scale, background: "rgba(0,0,0,0.75)", fontFamily: MONO, fontSize: 18 * scale, fontWeight: 700, color: "#7CFF6B" }}>
    {value} FPS
  </div>
);

/** One click: Play now, the desk goes dark, the TV turns on and switches to the PC. */
const OneClick: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const pressed = interpolate(f, [16, 19, 24], [0, 1, 0], clamp);
  const tvOn = tw(f, [46, 54], [0, 1], Easing.out(Easing.cubic));
  return (
    <Feature n={2} t={t}>
      <Screen x={170} y={420} w={430} h={256} power={1 - tw(f, [30, 35], [0, 1], Easing.linear)} kind="monitor">
        <Desktop />
      </Screen>
      <Screen x={630} y={420} w={430} h={256} power={1 - tw(f, [38, 43], [0, 1], Easing.linear)} kind="monitor">
        <Desktop app pressed={pressed} />
      </Screen>
      <Screen x={1100} y={330} w={660} h={380} power={1} kind="tv">
        <AbsoluteFill style={{ scale: `1 ${Math.max(0.005, tvOn)}`, opacity: tvOn > 0 ? 1 : 0 }}>
          <StillGame />
        </AbsoluteFill>
      </Screen>
      <div style={{ position: "absolute", left: 1100, width: 660, top: 770, display: "flex", justifyContent: "center", ...rise(f, 58, 20) }}>
        <div style={{ ...pill, height: 76, fontSize: 30, gap: 14 }}>
          <G name="power" size={30} color={ACCENT} />
          {t.input}
        </div>
      </div>
    </Feature>
  );
};

/** Tuned for the TV: the settings it applies, and the FPS counter. */
const Tuned: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const icons: Glyph[] = ["monitor", "hdr", "vrr", "fps", "fps", "speakers"];
  return (
    <Feature n={3} t={t}>
      <Screen x={150} y={330} w={820} h={472} power={1} kind="tv">
        <StillGame />
        {f >= 50 && <FpsBadge value={f >= 58 ? 60 : 59} scale={1.6} />}
      </Screen>
      <div style={{ position: "absolute", left: 1030, top: 318, display: "flex", flexDirection: "column", gap: 14 }}>
        {t.specs.map((s, i) => (
          <div key={s} style={{ ...pill, width: 740, height: 84, fontSize: 34, ...rise(f, 8 + i * 7, 30) }}>
            <G name={icons[i]} size={36} color={ACCENT} />
            {s}
          </div>
        ))}
      </div>
    </Feature>
  );
};

/** Your launcher. */
const Launchers: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const cards = [
    { name: "Steam", bg: "#15293d", accent: "#66c0f4" },
    { name: "Playnite", bg: "#261844", accent: "#b690ff" },
    { name: "Xbox", bg: "#123317", accent: "#5fd36f" },
  ];
  return (
    <Feature n={4} t={t}>
      <div style={{ position: "absolute", top: 330, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 44 }}>
        {cards.map((c, i) => {
          const s = spring({ frame: f - 6 - i * 6, fps, config: { damping: 16, stiffness: 110 } });
          return (
            <div
              key={c.name}
              style={{
                width: 440,
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
                translate: `0 ${(1 - s) * 160}px`,
                fontFamily: SANS,
              }}
            >
              <div style={{ position: "absolute", right: -70, top: -70, width: 300, height: 300, borderRadius: "50%", border: `38px solid ${c.accent}40` }} />
              <div style={{ position: "absolute", right: 34, top: 34, width: 100, height: 100, borderRadius: "50%", background: c.accent }} />
              <div style={{ fontSize: 76, fontWeight: 800, color: "#fff", letterSpacing: "-0.04em" }}>{c.name}</div>
              <div style={{ fontSize: 36, fontWeight: 500, color: c.accent, marginTop: 6 }}>{t.launchers[i]}</div>
            </div>
          );
        })}
      </div>
    </Feature>
  );
};

/** From the couch: the three controller shortcuts, each lighting up in turn. */
const Couch: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const glyphs: Glyph[] = ["controller", "menu", "back"];
  return (
    <Feature n={5} t={t}>
      <div style={{ position: "absolute", left: 960 - 680, top: 300, width: 1360, display: "flex", flexDirection: "column", gap: 22 }}>
        {t.shortcuts.map(([combo, action], i) => {
          const at = 18 + i * 24;
          const lit = f >= at && f < at + 18 ? 1 : 0;
          const fill = tw(f, [at, at + 16], [0, 1], Easing.linear);
          return (
            <div key={combo} style={{ ...pill, height: 140, padding: "0 40px", gap: 30, border: `2px solid ${lit ? ACCENT : EDGE}`, ...rise(f, 4 + i * 5, 30) }}>
              <Circle glyph={glyphs[i]} size={74} bg={lit ? ACCENT : "rgba(255,255,255,0.1)"} color={lit ? "#03130d" : "#fff"} />
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 44, fontWeight: 800, color: C.text }}>{combo}</div>
                <div style={{ height: 6, borderRadius: 3, background: "rgba(255,255,255,0.12)", width: 360, marginTop: 12, overflow: "hidden" }}>
                  <div style={{ width: `${fill * 100}%`, height: "100%", background: ACCENT }} />
                </div>
              </div>
              <span style={{ fontSize: 38, fontWeight: 600, color: lit || f >= at + 18 ? C.text : C.muted }}>{action}</span>
            </div>
          );
        })}
      </div>
      <Center top={810}>
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 32, color: C.muted, opacity: tw(f, [20, 30], [0, 1], Easing.linear) }}>{t.pads}</div>
      </Center>
    </Feature>
  );
};

type Win = { proc: string; title: string; color: string; letter: string };
const WINDOWS: Win[] = [
  { proc: "NeonDrift", title: "Neon Drift", color: "#c235e8", letter: "N" },
  { proc: "steam", title: "Steam Big Picture", color: "#2a475e", letter: "S" },
  { proc: "Discord", title: "#general", color: "#5865c9", letter: "D" },
  { proc: "explorer", title: "Downloads", color: "#d9a520", letter: "E" },
];

const MenuRow: React.FC<{ glyph: Glyph; label: string; value?: React.ReactNode; focus?: number; bg?: string; children?: React.ReactNode }> = ({
  glyph,
  label,
  value,
  focus = 0,
  bg = "#1b2129",
  children,
}) => (
  <div style={{ height: 96, borderRadius: 22, padding: "0 24px", display: "flex", alignItems: "center", gap: 22, background: bg, boxShadow: focus > 0.5 ? "inset 0 0 0 3px #fff" : undefined, position: "relative", fontFamily: SANS }}>
    <Circle glyph={glyph} size={58} />
    <div style={{ flex: 1 }}>
      {value !== undefined ? (
        <>
          <div style={{ fontSize: 24, color: SUB }}>{label}</div>
          <div style={{ fontSize: 30, fontWeight: 600, color: "#fff" }}>{value}</div>
        </>
      ) : (
        <div style={{ fontSize: 30, fontWeight: 600, color: "#fff" }}>{label}</div>
      )}
    </div>
    {children}
  </div>
);

/** Menu over the game: side panel and the open windows; X closes one. */
const SessionMenu: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const m = t.menu;
  const pos = steps(
    f,
    [
      [26, 2],
      [44, 10],
      [56, 11],
      [68, 12],
    ],
    1,
  );
  const vol = Math.round(interpolate(f, [30, 42], [65, 80], clamp));
  const closing = tw(f, [86, 94], [0, 1], Easing.in(Easing.cubic));
  const closed = f >= 94;
  return (
    <Feature n={6} t={t} bg={<StillGame dim={0.78} />}>
      <div style={{ position: "absolute", left: 150, top: 290, width: 740, padding: 16, borderRadius: 30, background: "#11161c", display: "flex", flexDirection: "column", gap: 10, ...rise(f, 4, 30) }}>
        <MenuRow glyph="folder" label={m.controlFs} bg="#1f3d5e" />
        <MenuRow glyph="play" label={m.back} bg="#1f5e48" focus={near(pos, 1)} />
        <MenuRow glyph="volume" label={m.volume} value=" " focus={near(pos, 2)}>
          <div style={{ position: "absolute", left: 104, right: 24, bottom: 20, height: 8, borderRadius: 4, background: "rgba(255,255,255,0.15)" }}>
            <div style={{ width: `${vol}%`, height: "100%", borderRadius: 4, background: ACCENT }} />
          </div>
          <div style={{ fontSize: 30, fontWeight: 600, color: "#fff", marginTop: -20 }}>{vol}%</div>
        </MenuRow>
        <MenuRow glyph="monitor" label={m.res} value="3840 × 2160 · 120 Hz" />
        <MenuRow glyph="hdr" label={m.hdr} value={m.on} />
      </div>
      <div style={{ position: "absolute", left: 940, top: 290, ...rise(f, 10, 30) }}>
        <div style={{ display: "flex", alignItems: "baseline", gap: 16, fontFamily: SANS }}>
          <span style={{ fontSize: 40, fontWeight: 700, color: "#fff" }}>{m.windows}</span>
          <span style={{ fontSize: 28, color: SUB }}>{m.open(closed ? 3 : 4)}</span>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 380px)", gap: 20, marginTop: 20 }}>
          {WINDOWS.map((w, i) => {
            if (closed && i === 2) return null;
            const slot = closed && i > 2 ? i - 1 : i;
            const on = near(pos, 10 + slot);
            const c = i === 2 ? closing : 0;
            return (
              <div
                key={w.proc}
                style={{
                  height: 200,
                  borderRadius: 22,
                  padding: 24,
                  boxSizing: "border-box",
                  background: "#1b2129",
                  boxShadow: on > 0.5 ? "inset 0 0 0 3px #fff" : undefined,
                  opacity: 1 - c,
                  translate: `0 ${c * 24}px`,
                  display: "flex",
                  flexDirection: "column",
                  justifyContent: "space-between",
                  fontFamily: SANS,
                }}
              >
                <div style={{ width: 52, height: 52, borderRadius: 13, background: w.color, display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 800, fontSize: 26, color: "#fff" }}>{w.letter}</div>
                <div>
                  <div style={{ fontSize: 22, color: SUB }}>{w.proc}</div>
                  <div style={{ fontSize: 30, fontWeight: 600, color: "#fff" }}>{w.title}</div>
                </div>
              </div>
            );
          })}
        </div>
        <div style={{ display: "flex", gap: 34, marginTop: 30 }}>
          <Hint b="A" label={t.hints.select} />
          <Hint b="X" label={t.hints.close} lit={f >= 80 && f < 92 ? 1 : 0} />
        </div>
      </div>
    </Feature>
  );
};

/** Two interfaces: Desktop for the mouse, Console for the controller. */
const Interfaces: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const c = t.console;
  const label: React.CSSProperties = { fontFamily: SANS, fontSize: 40, fontWeight: 700, color: C.text, textAlign: "center", marginTop: 24 };
  return (
    <Feature n={7} t={t}>
      <div style={{ position: "absolute", top: 300, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 60 }}>
        <div style={rise(f, 6, 30)}>
          <DesktopWindow t={t.desktop} />
          <div style={label}>{t.labels[0]}</div>
        </div>
        <div style={rise(f, 14, 30)}>
          <div style={{ width: 760, height: 440, borderRadius: 18, overflow: "hidden", background: "#11201c", border: `1px solid ${EDGE}`, fontFamily: SANS, color: "#fff", position: "relative" }}>
            <div style={{ height: 70, display: "flex", alignItems: "center", gap: 10, padding: "0 24px" }}>
              <Circle glyph="controller" size={40} bg={ACCENT} color="#03130d" />
              <span style={{ flex: 1 }} />
              <Badge label="LB" h={32} />
              {c.tabs.map((name, i) => (
                <span key={name} style={{ padding: "0 12px", fontSize: 24, fontWeight: 600, opacity: i === 0 ? 1 : 0.5, borderBottom: i === 0 ? `3px solid ${ACCENT}` : "3px solid transparent", paddingBottom: 4 }}>
                  {name}
                </span>
              ))}
              <Badge label="RB" h={32} />
              <span style={{ flex: 1 }} />
            </div>
            <div style={{ margin: "10px 24px", borderRadius: 18, padding: "26px 30px", background: "#1d5a42" }}>
              <div style={{ fontSize: 22, color: SUB }}>{c.heading}</div>
              <div style={{ fontSize: 32, fontWeight: 600, marginTop: 6 }}>{c.summary}</div>
              <div style={{ display: "inline-flex", alignItems: "center", gap: 10, marginTop: 18, padding: "12px 26px", borderRadius: 12, background: ACCENT, color: "#03130d", fontSize: 26, fontWeight: 700, outline: f % 24 < 12 ? "3px solid #fff" : "3px solid rgba(255,255,255,0.4)", outlineOffset: 4 }}>
                <G name="play" size={22} color="#03130d" /> {c.play}
              </div>
            </div>
            <div style={{ display: "flex", gap: 14, margin: "18px 24px" }}>
              {(["monitor", "volume", "play", "hdr"] as Glyph[]).map((g) => (
                <div key={g} style={{ flex: 1, height: 84, borderRadius: 16, background: "rgba(255,255,255,0.08)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                  <G name={g} size={34} />
                </div>
              ))}
            </div>
          </div>
          <div style={label}>{t.labels[1]}</div>
        </div>
      </div>
    </Feature>
  );
};

/** Automatic restore: the TV goes off, the desk comes back, each item checked. */
const Restore: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const tv = 1 - tw(f, [8, 14], [0, 1], Easing.linear);
  const icons: Glyph[] = ["monitor", "speakers", "monitor"];
  return (
    <Feature n={8} t={t}>
      <Screen x={330} y={310} w={330} h={196} power={tw(f, [16, 22], [0, 1], Easing.linear)} kind="monitor">
        <Desktop />
      </Screen>
      <Screen x={690} y={310} w={330} h={196} power={tw(f, [22, 28], [0, 1], Easing.linear)} kind="monitor">
        <Desktop />
      </Screen>
      <Screen x={1080} y={250} w={500} h={288} power={1} kind="tv">
        <AbsoluteFill style={{ opacity: tv }}>
          <StillGame />
        </AbsoluteFill>
      </Screen>
      <div style={{ position: "absolute", top: 680, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 28 }}>
        {t.restored.map((r, i) => (
          <div key={r} style={{ ...pill, width: 430, ...rise(f, 28 + i * 6, 30) }}>
            <G name={icons[i]} size={42} />
            <span style={{ flex: 1 }}>{r}</span>
            <Check p={tw(f, [36 + i * 8, 46 + i * 8], [0, 1])} />
          </div>
        ))}
      </div>
      <Center top={820}>
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 32, color: C.muted, opacity: tw(f, [60, 70], [0, 1], Easing.linear) }}>{t.exact}</div>
      </Center>
    </Feature>
  );
};

/** Automation: consolemode:// typed out, then Stream Deck keys. */
const Automation: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const cmd = "consolemode://start";
  const typed = Math.floor(interpolate(f, [6, 30], [0, cmd.length], clamp));
  const keys: Glyph[] = ["play", "menu", "power"];
  return (
    <Feature n={9} t={t}>
      <div style={{ position: "absolute", top: 310, left: 0, right: 0, display: "flex", justifyContent: "center" }}>
        <div style={{ ...pill, height: 120, minWidth: 900, fontFamily: MONO, fontSize: 52, fontWeight: 500, padding: "0 44px", ...rise(f, 0, 20) }}>
          <span style={{ color: ACCENT }}>›</span>
          <span>
            {cmd.slice(0, typed)}
            <span style={{ display: "inline-block", width: 24, height: 56, marginLeft: 4, verticalAlign: "middle", background: ACCENT, opacity: f < 32 || Math.floor(f / 7) % 2 === 0 ? 1 : 0 }} />
          </span>
        </div>
      </div>
      <div style={{ position: "absolute", top: 520, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 40 }}>
        {keys.map((k, i) => {
          const at = 44 + i * 9;
          const lit = f >= at && f < at + 7 ? 1 : 0;
          return (
            <div
              key={k}
              style={{
                width: 200,
                height: 200,
                borderRadius: 36,
                background: lit ? ACCENT : CARD,
                border: `2px solid ${lit ? ACCENT : EDGE}`,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                gap: 14,
                ...rise(f, 34 + i * 3, 40),
              }}
            >
              <G name={k} size={64} color={lit ? "#03130d" : C.text} />
              <span style={{ fontFamily: SANS, fontSize: 30, fontWeight: 700, color: lit ? "#03130d" : C.text }}>{t.keys[i]}</span>
            </div>
          );
        })}
      </div>
    </Feature>
  );
};

/** Free and open source, and the rest. */
const Open: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  return (
    <Feature n={10} t={t}>
      <div style={{ position: "absolute", top: 320, left: 180, right: 180, display: "flex", flexWrap: "wrap", justifyContent: "center", gap: 24 }}>
        {t.chips.map((c, i) => (
          <div key={c} style={{ ...pill, fontSize: 42, height: 112, padding: "0 40px", ...rise(f, 6 + i * 5, 30) }}>
            <Check p={tw(f, [10 + i * 5, 18 + i * 5], [0, 1])} size={50} />
            {c}
          </div>
        ))}
      </div>
      <div style={{ position: "absolute", top: 830, left: 0, right: 0, display: "flex", justifyContent: "center", ...rise(f, 50, 20) }}>
        <div style={{ ...pill, fontFamily: MONO, fontSize: 34, height: 80, background: "#0b0c0e" }}>
          <span style={{ color: ACCENT }}>›</span> winget install lippdev.ConsoleMode
        </div>
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
          <Logo size={170} draw={1} />
        </div>
        <div style={{ height: 34 }} />
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 150, fontWeight: 800, letterSpacing: "-0.045em", color: C.text, ...rise(f, 0, 24) }}>Console Mode</div>
        <div style={{ height: 18 }} />
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 56, fontWeight: 600, color: ACCENT, opacity: tw(f, [8, 16], [0, 1], Easing.linear) }}>{t.outro}</div>
        <div style={{ height: 44 }} />
        <div style={{ textAlign: "center", fontFamily: SANS, fontSize: 38, fontWeight: 600, color: C.muted, opacity: tw(f, [20, 28], [0, 1], Easing.linear) }}>github.com/lippdev/consolemode</div>
      </Center>
    </AbsoluteFill>
  );
};

// ───────────────────────── tour ─────────────────────────

/** Fades in and out over a few frames: no blur or zoom, which smear a GIF. */
const Part: React.FC<{ dur: number; last: boolean; children: React.ReactNode }> = ({ dur, last, children }) => {
  const f = useCurrentFrame();
  const o = tw(f, [0, 6], [0, 1], Easing.linear) * (last ? 1 : 1 - tw(f, [dur - 6, dur], [0, 1], Easing.linear));
  return <AbsoluteFill style={{ opacity: o }}>{children}</AbsoluteFill>;
};

export const Showcase: React.FC<{ lang: Lang }> = ({ lang }) => {
  useFonts();
  const t = T[lang];
  const scenes: React.FC<{ t: Strings }>[] = [Title, Who, Problem, OneClick, Tuned, Launchers, Couch, SessionMenu, Interfaces, Restore, Automation, Open, Outro];
  return (
    <AbsoluteFill style={{ background: C.bg, overflow: "hidden" }}>
      {scenes.map((S, i) => (
        <Sequence key={i} from={STARTS[i]} durationInFrames={DURS[i]}>
          <Part dur={DURS[i]} last={i === scenes.length - 1}>
            <S t={t} />
          </Part>
        </Sequence>
      ))}
    </AbsoluteFill>
  );
};
