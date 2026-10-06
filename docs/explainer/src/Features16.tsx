import React from "react";
import { AbsoluteFill, Easing, interpolate, Sequence, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Badge, G, type Glyph } from "./Alpha3";
import { C, Center, Check, clamp, Desktop, Game, gradText, Line, Logo, SANS, Scene, Screen, tw, useFonts } from "./kit";

// Quick tour of 1.6 for the README GIF, built like the 1.5 one (Features.tsx): a title, one
// numbered feature per scene with a progress bar, an outro. Flat on purpose (no blur, glow or
// footage) so the GIF stays sharp and light. The mockups follow the app (alpha.3) and use the
// en-US / pt-BR strings of the catalogs, drawn larger than in the videos so they read at 960 px.

type Lang = "en" | "pt";

const T = {
  en: {
    tagline: "Your PC becomes a console in one click.",
    scenes: [
      ["Pick your screen", "Play on the TV. The rest goes dark."],
      ["New session menu", "Everything you need, over the game."],
      ["Alt + Tab for the couch", "Every open window. X closes it."],
      ["File explorer built in", "ControlFS, made for the controller."],
      ["Console interface", "Redesigned. LB / RB to switch tabs."],
      ["Your library as backdrop", "Covers of your Steam games."],
      ["Your shortcuts", "Hold the buttons, then let go."],
      ["And more", "Polish all over."],
      ["Coming soon", "JoyChromium: the web with a controller."],
    ],
    menu: {
      controlFs: "File explorer (ControlFS)",
      controlFsSub: "Browse your files with the controller",
      back: "Back to the game",
      volume: "Volume",
      res: "Resolution and refresh rate",
      audio: "Audio output",
      windows: "Windows",
      open: (n: number) => `${n} open`,
    },
    hints: { select: "Select", close: "Close window", back: "Back to the game" },
    fs: {
      nav: ["Home", "Favorites", "Recent", "Downloads", "Documents", "Pictures", "Videos", "Music"],
      heading: "Main folders",
      folders: ["Downloads", "Documents", "Desktop", "Pictures", "Videos", "Music"],
      items: "1,284 items · 42.7 GB",
      keys: [
        ["A", "Open"],
        ["B", "Back"],
        ["X", "Actions"],
        ["Y", "Search"],
      ],
    },
    console: {
      tabs: ["Home", "Session", "System"],
      heading: "Where do you want to play?",
      summary: "Play on Living room TV and turn off 2 screens",
      play: "Play now",
      screens: "Screens",
      tv: "Living room TV",
      roles: ["Play here", "Turn off", "Turn off"],
      system: [
        ["Background", "Steam covers"],
        ["Interface sounds", "On"],
        ["Receive test versions (alpha and beta)", "On"],
      ],
    },
    covers: ["Steam covers", "Gradient only", "My picture"],
    shortcuts: {
      title: "Set up your controller shortcuts",
      hold: "Hold the buttons, then let go…",
      rows: ["Open Console Mode", "Session menu", "Back to the PC"],
    },
    more: ["Interface sounds", "Steam closes cleanly", "Portrait screens come back", "Test versions in Settings"],
    joy: { search: "Search or type a web address", tabs: ["streamio – Home", "Couch gaming – Wikiverse"], keys: ["Cursor", "Click", "Tabs", "Scroll"] },
    outro: "Free and open source · Windows 10 and 11",
  },
  pt: {
    tagline: "Seu PC vira um console com um clique.",
    scenes: [
      ["Escolha a tela", "Joga na TV. O resto apaga."],
      ["Novo menu da sessão", "Tudo o que precisa, sobre o jogo."],
      ["Alt + Tab do sofá", "Todas as janelas abertas. X fecha."],
      ["Explorador de arquivos", "ControlFS, feito para o controle."],
      ["Interface Console", "Redesenhada. LB / RB troca de aba."],
      ["Sua biblioteca no fundo", "As capas dos seus jogos da Steam."],
      ["Seus atalhos", "Segure os botões e depois solte."],
      ["E mais", "Capricho em todo canto."],
      ["Em breve", "JoyChromium: a web no controle."],
    ],
    menu: {
      controlFs: "Explorador de arquivos (ControlFS)",
      controlFsSub: "Navegue pelos seus arquivos com o controle",
      back: "Voltar ao jogo",
      volume: "Volume",
      res: "Resolução e taxa de atualização",
      audio: "Saída de áudio",
      windows: "Janelas",
      open: (n: number) => `${n} abertas`,
    },
    hints: { select: "Selecionar", close: "Fechar janela", back: "Voltar ao jogo" },
    fs: {
      nav: ["Início", "Favoritos", "Recentes", "Downloads", "Documentos", "Imagens", "Vídeos", "Músicas"],
      heading: "Pastas principais",
      folders: ["Downloads", "Documentos", "Área de trabalho", "Imagens", "Vídeos", "Músicas"],
      items: "1.284 itens · 42,7 GB",
      keys: [
        ["A", "Abrir"],
        ["B", "Voltar"],
        ["X", "Ações"],
        ["Y", "Buscar"],
      ],
    },
    console: {
      tabs: ["Início", "Sessão", "Sistema"],
      heading: "Onde você vai jogar?",
      summary: "Jogar na TV da sala e desligar 2 telas",
      play: "Jogar agora",
      screens: "Telas",
      tv: "TV da sala",
      roles: ["Jogar aqui", "Desligar", "Desligar"],
      system: [
        ["Plano de fundo", "Capas da Steam"],
        ["Sons da interface", "Ligado"],
        ["Receber versões de teste (alpha e beta)", "Ligado"],
      ],
    },
    covers: ["Capas da Steam", "Só gradiente", "Minha imagem"],
    shortcuts: {
      title: "Configure os atalhos do controle",
      hold: "Segure os botões e depois solte…",
      rows: ["Abrir o Console Mode", "Menu da sessão", "Voltar ao PC"],
    },
    more: ["Sons da interface", "A Steam fecha direito", "Telas em retrato voltam", "Versões de teste nos Ajustes"],
    joy: { search: "Pesquise ou digite um endereço", tabs: ["streamio – Início", "Jogar no sofá – Wikiverse"], keys: ["Cursor", "Clicar", "Abas", "Rolar"] },
    outro: "Grátis e open source · Windows 10 e 11",
  },
};
type Strings = (typeof T)["en"];

/** Scene lengths in frames (30 fps); the first and last are the title and the outro. */
const DURS = [60, 105, 105, 105, 105, 120, 90, 105, 90, 105, 90];
const STARTS = DURS.map((_, i) => DURS.slice(0, i).reduce((a, b) => a + b, 0));
const COUNT = DURS.length - 2;
export const FEATURES16_DURATION = DURS.reduce((a, b) => a + b, 0);

const ACCENT = "#4FCFA3";
const SUB = "rgba(255,255,255,0.7)";
const PANEL = "#141a21";

// ───────────────────────── pieces (as in Features.tsx) ─────────────────────────

const Header: React.FC<{ n: number; t: Strings }> = ({ n, t }) => {
  const f = useCurrentFrame();
  const [kicker, title] = t.scenes[n];
  return (
    <Center top={70}>
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
          marginBottom: 16,
        }}
      >
        {String(n + 1).padStart(2, "0")} · {kicker}
      </div>
      <Line words={title.split(" ")} at={4} size={80} stagger={3} />
    </Center>
  );
};

const Progress: React.FC<{ n: number }> = ({ n }) => {
  const f = useCurrentFrame();
  const fill = tw(f, [0, DURS[n + 1] - 10], [0, 1], Easing.linear);
  return (
    <div style={{ position: "absolute", bottom: 40, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 10 }}>
      {Array.from({ length: COUNT }, (_, i) => (
        <div key={i} style={{ width: 70, height: 6, borderRadius: 3, background: i < n ? "rgba(255,255,255,0.45)" : "rgba(255,255,255,0.14)", overflow: "hidden" }}>
          {i === n && <div style={{ width: `${fill * 100}%`, height: "100%", background: C.mint }} />}
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

const rise = (f: number, at: number, dist = 60): React.CSSProperties => ({
  opacity: tw(f - at, [0, 10], [0, 1], Easing.linear),
  translate: `0 ${tw(f - at, [0, 18], [dist, 0])}px`,
});

/** 0…1 closeness of a focus position to item i. */
const near = (pos: number, i: number) => Math.max(0, 1 - Math.abs(pos - i));

/** Eases a focus index to each new value on its frame. */
const steps = (f: number, marks: [number, number][], start: number) => {
  let v = start;
  let prev = start;
  for (const [at, to] of marks) {
    v += (to - prev) * tw(f, [at, at + 5], [0, 1]);
    prev = to;
  }
  return v;
};

const Hint: React.FC<{ b: string; label: string; lit?: number }> = ({ b, label, lit = 0 }) => (
  <span style={{ display: "flex", alignItems: "center", gap: 12, fontFamily: SANS, fontSize: 28, color: C.text }}>
    <Badge label={b} lit={lit} h={40} />
    {label}
  </span>
);

/** The game under the session menu: the flat landscape, dimmed (no blur, to keep the GIF clean). */
const GameBehind: React.FC = () => <Game dim={0.72} />;

const Circle: React.FC<{ glyph: Glyph; size?: number; bg?: string; color?: string }> = ({ glyph, size = 60, bg = "rgba(255,255,255,0.15)", color = "#fff" }) => (
  <div style={{ width: size, height: size, borderRadius: size / 2, background: bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
    <G name={glyph} size={size * 0.48} color={color} />
  </div>
);

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
        <Line words={["Console", "Mode", { t: "1.6", grad: true }]} at={8} size={150} weight={800} stagger={4} />
        <div style={{ height: 24 }} />
        <Line words={t.tagline.split(" ")} at={22} size={48} weight={500} stagger={2} color={C.muted} />
      </Center>
    </AbsoluteFill>
  );
};

/** 01: the desk monitors turn off and the TV turns on (same as 1.5). */
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

/** A row of the session menu's side panel, drawn large. */
const MenuRow: React.FC<{ glyph: Glyph; label: string; value?: React.ReactNode; focus?: number; bg?: string; children?: React.ReactNode }> = ({
  glyph,
  label,
  value,
  focus = 0,
  bg = "rgba(255,255,255,0.05)",
  children,
}) => (
  <div
    style={{
      height: 100,
      borderRadius: 22,
      padding: "0 24px",
      display: "flex",
      alignItems: "center",
      gap: 22,
      background: bg,
      boxShadow: focus > 0.5 ? "inset 0 0 0 3px #fff" : undefined,
      position: "relative",
      fontFamily: SANS,
    }}
  >
    <Circle glyph={glyph} />
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

type Win = { proc: string; title: string; color: string; letter: string };
const WINDOWS: Win[] = [
  { proc: "NeonDrift", title: "Neon Drift", color: "linear-gradient(135deg,#ff3ec9,#7a2cff)", letter: "N" },
  { proc: "steam", title: "Steam Big Picture", color: "linear-gradient(135deg,#2a475e,#171a21)", letter: "S" },
  { proc: "Discord", title: "#general", color: "linear-gradient(135deg,#6b7cff,#4450c9)", letter: "D" },
  { proc: "explorer", title: "Downloads", color: "linear-gradient(135deg,#f7c948,#c99312)", letter: "E" },
];

const WinCard: React.FC<{ w: Win; focus?: number; style?: React.CSSProperties; big?: boolean }> = ({ w, focus = 0, style, big }) => (
  <div
    style={{
      width: big ? 330 : 300,
      height: big ? 240 : 210,
      borderRadius: 22,
      padding: 24,
      boxSizing: "border-box",
      background: "#1d242c",
      boxShadow: focus > 0.5 ? "inset 0 0 0 3px #fff" : undefined,
      scale: String(1 + 0.05 * focus),
      display: "flex",
      flexDirection: "column",
      justifyContent: "space-between",
      fontFamily: SANS,
      ...style,
    }}
  >
    <div style={{ width: 64, height: 64, borderRadius: 32, background: "rgba(255,255,255,0.12)", display: "flex", alignItems: "center", justifyContent: "center" }}>
      <div style={{ width: 42, height: 42, borderRadius: 11, background: w.color, display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 800, fontSize: 22, color: "#fff" }}>{w.letter}</div>
    </div>
    <div>
      <div style={{ fontSize: 22, color: SUB }}>{w.proc}</div>
      <div style={{ fontSize: 28, fontWeight: 600, color: "#fff" }}>{w.title}</div>
    </div>
  </div>
);

/** 02: the side panel; volume gets adjusted. */
const SessionMenu: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const pos = steps(f, [[34, 2]], 1);
  const adjusting = f >= 48 && f < 92;
  const vol = Math.round(interpolate(f, [54, 84], [65, 80], clamp));
  const m = t.menu;
  return (
    <Feature n={1} t={t} bg={<GameBehind />}>
      <div style={{ position: "absolute", left: 170, top: 300, width: 760, padding: 18, borderRadius: 32, background: PANEL, display: "flex", flexDirection: "column", gap: 12, ...rise(f, 6, 40) }}>
        <MenuRow glyph="folder" label={m.controlFs} bg="linear-gradient(135deg,#2B5C8F,#16263A)" />
        <MenuRow glyph="play" label={m.back} bg="linear-gradient(135deg,#2A8F69,#17362F)" focus={near(pos, 1)} />
        <MenuRow glyph="volume" label={m.volume} value=" " focus={near(pos, 2)}>
          <div style={{ position: "absolute", left: 106, right: 24, bottom: 22, height: 8, borderRadius: 4, background: "rgba(255,255,255,0.15)" }}>
            <div style={{ width: `${vol}%`, height: "100%", borderRadius: 4, background: ACCENT }} />
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 14, fontSize: 30, fontWeight: 600, color: "#fff", marginTop: -22 }}>
            {adjusting && <span style={{ color: ACCENT }}>◀</span>}
            {vol}%
            {adjusting && <span style={{ color: ACCENT }}>▶</span>}
          </div>
        </MenuRow>
        <MenuRow glyph="monitor" label={m.res} value="3840 × 2160 · 120 Hz" />
        <MenuRow glyph="speakers" label={m.audio} value="LG TV" />
      </div>
      <div style={{ position: "absolute", left: 980, top: 300, ...rise(f, 12, 40) }}>
        <div style={{ display: "flex", alignItems: "baseline", gap: 16, fontFamily: SANS }}>
          <span style={{ fontSize: 40, fontWeight: 700, color: "#fff" }}>{m.windows}</span>
          <span style={{ fontSize: 28, color: SUB }}>{m.open(4)}</span>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 330px)", gap: 20, marginTop: 22 }}>
          {WINDOWS.map((w) => (
            <WinCard key={w.proc} w={w} big />
          ))}
        </div>
      </div>
    </Feature>
  );
};

/** 03: the open windows, Alt + Tab style; X closes one. */
const Switcher: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const pos = steps(
    f,
    [
      [30, 1],
      [44, 2],
    ],
    0,
  );
  const closing = tw(f, [62, 72], [0, 1], Easing.in(Easing.cubic));
  const closed = f >= 72;
  const slide = tw(f, [72, 82], [0, 1]);
  const W = 360;
  const GAP = 28;
  const total = 4 * W + 3 * GAP;
  const left = (1920 - total) / 2;
  return (
    <Feature n={2} t={t} bg={<GameBehind />}>
      <div style={{ position: "absolute", left, top: 330, fontFamily: SANS, display: "flex", alignItems: "baseline", gap: 16, ...rise(f, 4, 30) }}>
        <span style={{ fontSize: 40, fontWeight: 700, color: "#fff" }}>{t.menu.windows}</span>
        <span style={{ fontSize: 28, color: SUB }}>{t.menu.open(closed ? 3 : 4)}</span>
      </div>
      {WINDOWS.map((w, i) => {
        if (closed && i === 2) return null;
        const x = closed && i === 3 ? left + 3 * (W + GAP) - slide * (W + GAP) : left + i * (W + GAP);
        const c = i === 2 ? closing : 0;
        return (
          <div key={w.proc} style={{ position: "absolute", left: x, top: 410, opacity: (1 - c) * tw(f - 6 - i * 3, [0, 10], [0, 1], Easing.linear), translate: `0 ${c * 30}px` }}>
            <WinCard w={w} focus={near(pos, i) * (1 - c)} style={{ width: W, height: 260 }} />
          </div>
        );
      })}
      <div style={{ position: "absolute", top: 760, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 46 }}>
        <Hint b="A" label={t.hints.select} />
        <Hint b="X" label={t.hints.close} lit={f >= 58 && f < 70 ? 1 : 0} />
        <Hint b="B" label={t.hints.back} />
      </div>
    </Feature>
  );
};

const FOLDER_COLORS = ["#1fbf8f", "#9aa7b8", "#2ea8e6", "#3b8eea", "#8b5cf6", "#f08a4b"];
const FOLDER_GLYPHS: Glyph[] = ["back", "folder", "monitor", "hdr", "play", "sound"];

/** A folder icon in the style of ControlFS. */
const Folder: React.FC<{ color: string; glyph: Glyph; size?: number }> = ({ color, glyph, size = 84 }) => (
  <svg width={size} height={size * 0.8} viewBox="0 0 100 80">
    <path d="M4 14a6 6 0 0 1 6-6h24l8 8h48a6 6 0 0 1 6 6v46a6 6 0 0 1-6 6H10a6 6 0 0 1-6-6z" fill={color} />
    <rect x="4" y="24" width="92" height="50" rx="6" fill={color} opacity="0.85" />
    <foreignObject x="34" y="30" width="32" height="40">
      <div style={{ display: "flex", justifyContent: "center", paddingTop: 4 }}>
        {glyph === "back" ? (
          <svg width="26" height="26" viewBox="0 0 24 24">
            <path d="M12 4v12M6 11l6 6 6-6M5 20h14" fill="none" stroke="#fff" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        ) : (
          <G name={glyph} size={26} color="#fff" />
        )}
      </div>
    </foreignObject>
  </svg>
);

/** 04: the ControlFS row is pressed and the file explorer opens full screen. */
const ControlFs: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const press = interpolate(f, [22, 26, 32], [0, 1, 0], clamp);
  const open = tw(f, [32, 50], [0, 1]);
  const pos = steps(
    f,
    [
      [70, 1],
      [84, 3],
    ],
    0,
  );
  const fs = t.fs;
  return (
    <Feature n={3} t={t}>
      {open < 1 && (
        <div style={{ position: "absolute", left: 960 - 420, top: 420, width: 840, opacity: 1 - open, scale: String(1 - press * 0.04), ...rise(f, 4, 30) }}>
          <MenuRow glyph="folder" label={t.menu.controlFs} bg="linear-gradient(135deg,#2B5C8F,#16263A)" focus={1} />
          <div style={{ display: "flex", justifyContent: "center", marginTop: 26 }}>
            <Hint b="A" label={t.hints.select} lit={press > 0.3 ? 1 : 0} />
          </div>
        </div>
      )}
      {open > 0 && (
        <div
          style={{
            position: "absolute",
            left: 140,
            top: 290,
            width: 1640,
            height: 690,
            borderRadius: 24,
            background: "#0d1824",
            border: "1px solid rgba(255,255,255,0.12)",
            overflow: "hidden",
            opacity: open,
            scale: String(0.9 + 0.1 * open),
            fontFamily: SANS,
            color: "#fff",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 14, padding: "22px 30px" }}>
            <div style={{ width: 38, height: 28, borderRadius: 7, background: "linear-gradient(135deg,#2f7bff,#18c0ff)" }} />
            <span style={{ fontSize: 28, fontWeight: 700 }}>ControlFS</span>
          </div>
          <div style={{ margin: "0 24px", padding: "12px 16px", borderRadius: 16, border: "1px solid rgba(255,255,255,0.12)", display: "flex", gap: 26, alignItems: "center", fontSize: 22, color: "#d8e2ee", whiteSpace: "nowrap", overflow: "hidden" }}>
            <Badge label="LB" h={32} />
            {fs.nav.map((n, i) => (
              <span key={n} style={{ padding: i === 0 ? "6px 14px" : 0, borderRadius: 10, border: i === 0 ? "1px solid rgba(255,255,255,0.3)" : undefined, fontWeight: i === 0 ? 700 : 400 }}>
                {n}
              </span>
            ))}
          </div>
          <div style={{ display: "flex", gap: 24, padding: "22px 30px" }}>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 30, fontWeight: 600, marginBottom: 18 }}>{fs.heading}</div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 18 }}>
                {fs.folders.map((name, i) => (
                  <div
                    key={name}
                    style={{
                      height: 118,
                      borderRadius: 14,
                      border: `2px solid ${near(pos, i) > 0.5 ? "#3fb6ff" : "rgba(255,255,255,0.1)"}`,
                      background: near(pos, i) > 0.5 ? "#173250" : "#11202f",
                      display: "flex",
                      alignItems: "center",
                      gap: 24,
                      padding: "0 26px",
                    }}
                  >
                    <Folder color={FOLDER_COLORS[i]} glyph={FOLDER_GLYPHS[i]} />
                    <div style={{ flex: 1 }}>
                      <div style={{ fontSize: 30, fontWeight: 500 }}>{name}</div>
                      <div style={{ fontSize: 20, color: "#93a3b5", marginTop: 4 }}>C:\Users\Player\{fs.folders[i] === "Área de trabalho" ? "Desktop" : name}</div>
                    </div>
                    <span style={{ fontSize: 30, color: "#93a3b5" }}>›</span>
                  </div>
                ))}
              </div>
            </div>
            <div style={{ width: 380, borderRadius: 18, background: "#12263a", display: "flex", flexDirection: "column", alignItems: "center", paddingTop: 50 }}>
              <Folder color={FOLDER_COLORS[Math.round(pos)]} glyph={FOLDER_GLYPHS[Math.round(pos)]} size={150} />
              <div style={{ fontSize: 36, fontWeight: 600, marginTop: 30 }}>{fs.folders[Math.round(pos)]}</div>
              <div style={{ fontSize: 22, color: "#93a3b5", marginTop: 10 }}>{fs.items}</div>
            </div>
          </div>
        </div>
      )}
    </Feature>
  );
};

/** A made-up cover (no real game art). */
const Cover: React.FC<{ i: number; w: number; h: number }> = ({ i, w, h }) => {
  const hue = (i * 47 + 200) % 360;
  const hue2 = (hue + 40 + (i % 3) * 30) % 360;
  return (
    <div style={{ width: w, height: h, borderRadius: 8, overflow: "hidden", position: "relative", background: `linear-gradient(${160 + (i % 5) * 20}deg, hsl(${hue} 55% 40%), hsl(${hue2} 65% 16%))` }}>
      {i % 3 === 0 && <div style={{ position: "absolute", left: "22%", top: "18%", width: "56%", aspectRatio: "1", borderRadius: "50%", background: `hsl(${hue2} 85% 68% / 0.6)` }} />}
      {i % 3 === 1 && <div style={{ position: "absolute", left: 0, right: 0, bottom: "26%", height: "40%", background: `hsl(${hue} 40% 10% / 0.7)`, clipPath: "polygon(0 60%,30% 10%,55% 50%,75% 0,100% 45%,100% 100%,0 100%)" }} />}
      {i % 3 === 2 && <div style={{ position: "absolute", left: "30%", top: "14%", width: "40%", height: "52%", borderRadius: "40% 40% 10% 10%", background: `hsl(${hue2} 80% 62% / 0.55)` }} />}
      <div style={{ position: "absolute", left: "14%", right: "14%", bottom: "10%", height: h * 0.05, borderRadius: 3, background: "rgba(255,255,255,0.6)" }} />
    </div>
  );
};

/** 05: the Console interface; LB/RB walks Home → Session → System. */
const ConsoleUi: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const tab = f < 46 ? 0 : f < 84 ? 1 : 2;
  const tabAt = [0, 46, 84][tab];
  const slide = tab === 0 ? 0 : tw(f - tabAt, [0, 8], [1, 0]);
  const c = t.console;
  const rb = f >= 44 && f < 50 ? 1 : f >= 82 && f < 88 ? 1 : 0;
  return (
    <Feature n={4} t={t}>
      <div
        style={{
          position: "absolute",
          left: 140,
          top: 290,
          width: 1640,
          height: 690,
          borderRadius: 24,
          overflow: "hidden",
          background: "linear-gradient(135deg,#123a30 0%,#0f1b1f 45%,#0f1418 100%)",
          border: "1px solid rgba(255,255,255,0.1)",
          fontFamily: SANS,
          color: "#fff",
          ...rise(f, 2, 40),
        }}
      >
        <div style={{ height: 96, display: "flex", alignItems: "center", padding: "0 36px" }}>
          <div style={{ flex: 1, display: "flex", alignItems: "center", gap: 14 }}>
            <Circle glyph="controller" size={52} bg={ACCENT} color="#03130d" />
            <span style={{ fontSize: 28, fontWeight: 600 }}>Console Mode</span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <Badge label="LB" h={40} />
            {c.tabs.map((name, i) => (
              <div key={name} style={{ padding: "8px 22px", fontSize: 32, fontWeight: 600, opacity: i === tab ? 1 : 0.5, position: "relative" }}>
                {name}
                {i === tab && <div style={{ position: "absolute", left: 22, right: 22, bottom: -6, height: 4, borderRadius: 2, background: ACCENT }} />}
              </div>
            ))}
            <Badge label="RB" h={40} lit={rb} />
          </div>
          <div style={{ flex: 1, textAlign: "right", fontSize: 30, fontWeight: 600 }}>21:52</div>
        </div>
        <div style={{ padding: "18px 36px", translate: `${slide * 70}px 0`, opacity: 1 - slide * 0.7 }}>
          {tab === 0 && (
            <>
              <div style={{ borderRadius: 24, padding: "36px 44px", background: "linear-gradient(135deg,#1F6B4A,#17362F 55%,#10191D)", display: "flex", alignItems: "center" }}>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 28, color: SUB }}>{c.heading}</div>
                  <div style={{ fontSize: 44, fontWeight: 600, marginTop: 10 }}>{c.summary}</div>
                  <div style={{ display: "flex", alignItems: "center", gap: 12, fontSize: 28, marginTop: 14 }}>
                    <G name="play" size={24} /> Steam Big Picture
                  </div>
                </div>
                <div style={{ padding: "22px 44px", borderRadius: 18, background: ACCENT, color: "#03130d", fontSize: 34, fontWeight: 700, display: "flex", alignItems: "center", gap: 14, outline: "4px solid #fff", outlineOffset: 5 }}>
                  <G name="play" size={28} color="#03130d" /> {c.play}
                </div>
              </div>
              <div style={{ display: "flex", gap: 20, marginTop: 30 }}>
                {(
                  [
                    ["monitor", c.tv],
                    ["volume", "LG TV"],
                    ["play", "Big Picture"],
                    ["hdr", "HDR · VRR"],
                  ] as [Glyph, string][]
                ).map(([g, v]) => (
                  <div key={v} style={{ flex: 1, height: 130, borderRadius: 20, background: "rgba(255,255,255,0.08)", display: "flex", alignItems: "center", gap: 18, padding: "0 24px" }}>
                    <Circle glyph={g} size={56} />
                    <span style={{ fontSize: 28, fontWeight: 600 }}>{v}</span>
                  </div>
                ))}
              </div>
            </>
          )}
          {tab === 1 && (
            <>
              <div style={{ fontSize: 30, fontWeight: 600, color: SUB, marginBottom: 20 }}>{c.screens}</div>
              <div style={{ display: "flex", gap: 24 }}>
                {[c.tv, "Monitor 1", "Monitor 2"].map((name, i) => (
                  <div
                    key={name}
                    style={{
                      width: 470,
                      height: 300,
                      borderRadius: 22,
                      padding: 28,
                      boxSizing: "border-box",
                      background: i === 0 ? "linear-gradient(135deg,#1F6B4A,#10362B)" : "linear-gradient(135deg,#262D34,#151A1F)",
                      opacity: i === 0 ? 1 : 0.6,
                      display: "flex",
                      flexDirection: "column",
                      justifyContent: "space-between",
                      outline: i === 0 ? "3px solid #fff" : "3px solid transparent",
                      outlineOffset: 5,
                    }}
                  >
                    <div style={{ width: 180, height: 104, border: "4px solid #E6EBEF", borderRadius: 12, padding: 4, boxSizing: "border-box" }}>
                      <div style={{ width: "100%", height: "100%", borderRadius: 7, background: i === 0 ? "linear-gradient(135deg,#7DF0CB,#23A67F)" : "#0A0D10", display: "flex", alignItems: "center", justifyContent: "center" }}>
                        <G name={i === 0 ? "play" : "power"} size={34} color={i === 0 ? "#0B1F18" : "rgba(255,255,255,0.6)"} />
                      </div>
                    </div>
                    <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
                      <span style={{ fontSize: 32, fontWeight: 600 }}>{name}</span>
                      <span style={{ padding: "4px 14px", borderRadius: 14, fontSize: 22, fontWeight: 600, background: i === 0 ? "#6FDDB9" : "rgba(255,255,255,0.15)", color: i === 0 ? "#0B1F18" : "#DDE3E8" }}>
                        {c.roles[i]}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
          {tab === 2 && (
            <div style={{ display: "flex", flexDirection: "column", gap: 16, maxWidth: 1300 }}>
              {c.system.map(([l, v], i) => (
                <div
                  key={l}
                  style={{
                    height: 100,
                    borderRadius: 20,
                    padding: "0 32px",
                    background: "rgba(255,255,255,0.08)",
                    display: "flex",
                    alignItems: "center",
                    fontSize: 32,
                    outline: i === 0 ? "3px solid #fff" : "3px solid transparent",
                    outlineOffset: 5,
                  }}
                >
                  <span style={{ flex: 1 }}>{l}</span>
                  <span style={{ fontWeight: 600, color: ACCENT }}>{v}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </Feature>
  );
};

/** 06: the covers background, and the three choices. */
const Covers: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const pick = steps(
    f,
    [
      [40, 1],
      [58, 0],
    ],
    0,
  );
  const gradOnly = near(pick, 1);
  return (
    <Feature n={5} t={t}>
      <div style={{ position: "absolute", left: 140, top: 290, width: 1640, height: 520, borderRadius: 24, overflow: "hidden", background: "#0F1418", ...rise(f, 2, 40) }}>
        <div style={{ position: "absolute", left: -20, top: -60, display: "grid", gridTemplateColumns: "repeat(9, 190px)", gap: 10, opacity: 0.7 * (1 - gradOnly) }}>
          {Array.from({ length: 27 }, (_, i) => (
            <Cover key={i} i={i} w={190} h={285} />
          ))}
        </div>
        <AbsoluteFill style={{ background: "linear-gradient(135deg, rgba(18,58,48,0.55) 0%, rgba(15,27,31,0.65) 45%, rgba(15,20,24,0.8) 100%)" }} />
        <div style={{ position: "absolute", left: 48, top: 40, display: "flex", alignItems: "center", gap: 14, fontFamily: SANS, color: "#fff" }}>
          <Circle glyph="controller" size={52} bg={ACCENT} color="#03130d" />
          <span style={{ fontSize: 30, fontWeight: 600 }}>Console Mode</span>
        </div>
      </div>
      <div style={{ position: "absolute", top: 850, left: 0, right: 0, display: "flex", justifyContent: "center", gap: 18 }}>
        {t.covers.map((label, i) => {
          const on = near(pick, i) > 0.5;
          return (
            <span
              key={label}
              style={{
                padding: "14px 30px",
                borderRadius: 999,
                fontFamily: SANS,
                fontSize: 32,
                fontWeight: 600,
                color: on ? "#03130d" : C.text,
                background: on ? ACCENT : "#16191d",
                border: `1px solid ${on ? ACCENT : "rgba(255,255,255,0.2)"}`,
                ...rise(f, 10 + i * 5, 20),
              }}
            >
              {label}
            </span>
          );
        })}
      </div>
    </Feature>
  );
};

/** 07: capturing the three shortcuts. */
const Shortcuts: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const s = t.shortcuts;
  const rows = [
    { name: s.rows[0], start: 14, parts: ["Xbox"] },
    { name: s.rows[1], start: 40, parts: ["Select", "Select + Y"] },
    { name: s.rows[2], start: 66, parts: ["Start", "Start + Select"] },
  ];
  return (
    <Feature n={6} t={t}>
      <div style={{ position: "absolute", left: 960 - 640, top: 300, width: 1280, display: "flex", flexDirection: "column", gap: 20 }}>
        {rows.map((r, i) => {
          const tt = f - r.start;
          const capturing = tt >= 0 && tt < 24;
          const saved = tt >= 24;
          let value = "—";
          if (capturing) value = tt < 7 ? s.hold : r.parts[tt < 15 ? 0 : r.parts.length - 1];
          if (saved) value = r.parts[r.parts.length - 1];
          return (
            <div
              key={r.name}
              style={{
                height: 140,
                borderRadius: 28,
                padding: "0 40px",
                background: "#141416",
                border: `2px solid ${capturing ? ACCENT : C.line}`,
                display: "flex",
                alignItems: "center",
                gap: 26,
                fontFamily: SANS,
                ...rise(f, 2 + i * 4, 30),
              }}
            >
              <span style={{ flex: 1, fontSize: 40, fontWeight: 600, color: C.text }}>{r.name}</span>
              <span style={{ fontSize: capturing && tt < 7 ? 30 : 44, fontWeight: 800, color: capturing && tt < 7 ? SUB : C.text }}>{value}</span>
              <div style={{ width: 60 }}>{saved && <Check p={tw(tt, [24, 32], [0, 1])} size={60} />}</div>
            </div>
          );
        })}
      </div>
    </Feature>
  );
};

/** 08: the rest, as chips (like 1.5's "And more"). */
const More: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const glyphs: Glyph[] = ["sound", "steam", "rotate", "flask"];
  return (
    <Feature n={7} t={t}>
      <div style={{ position: "absolute", top: 340, left: 200, right: 200, display: "flex", flexWrap: "wrap", justifyContent: "center", gap: 28 }}>
        {t.more.map((c, i) => (
          <div
            key={c}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 22,
              height: 130,
              padding: "0 44px",
              borderRadius: 32,
              background: "#141416",
              border: `1px solid ${C.line}`,
              fontFamily: SANS,
              fontSize: 44,
              fontWeight: 600,
              color: C.text,
              ...rise(f, 8 + i * 7, 40),
            }}
          >
            <Circle glyph={glyphs[i]} size={66} bg={`${C.mint}22`} color={C.mint} />
            {c}
          </div>
        ))}
      </div>
    </Feature>
  );
};

/** 09: JoyChromium, coming soon: a Chromium-style window driven by the controller. */
const Joy: React.FC<{ t: Strings }> = ({ t }) => {
  const f = useCurrentFrame();
  const j = t.joy;
  const tab = f >= 70 ? 1 : 0;
  const cx = interpolate(f, [20, 44], [820, 560], { ...clamp, easing: Easing.inOut(Easing.cubic) });
  const cy = interpolate(f, [20, 44], [470, 250], { ...clamp, easing: Easing.inOut(Easing.cubic) });
  const press = interpolate(f, [48, 51, 56], [0, 1, 0], clamp);
  const hover = f >= 40 && f < 70 ? 1 : 0;
  const lit = (a: number, b: number) => (f >= a && f < b ? 1 : 0);
  return (
    <Feature n={8} t={t}>
      <div
        style={{
          position: "absolute",
          left: 200,
          top: 290,
          width: 1520,
          height: 690,
          borderRadius: 16,
          overflow: "hidden",
          background: "#1f2023",
          border: "1px solid #3c4043",
          fontFamily: SANS,
          ...rise(f, 2, 40),
        }}
      >
        <div style={{ height: 60, position: "relative" }}>
          {j.tabs.map((title, i) => (
            <div
              key={title}
              style={{
                position: "absolute",
                left: 12 + i * 330,
                bottom: 0,
                width: 320,
                height: 48,
                borderRadius: "12px 12px 0 0",
                background: i === tab ? "#35363a" : "transparent",
                display: "flex",
                alignItems: "center",
                gap: 12,
                padding: "0 16px",
                boxSizing: "border-box",
                fontSize: 22,
                color: i === tab ? "#e8eaed" : "#9aa0a6",
              }}
            >
              <div style={{ width: 20, height: 20, borderRadius: 5, background: i === 0 ? "#ff3b5c" : "#e8eaed" }} />
              <span style={{ flex: 1, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{title}</span>✕
            </div>
          ))}
          <div style={{ position: "absolute", left: 12 + 2 * 330 + 8, bottom: 10, color: "#9aa0a6", fontSize: 30 }}>+</div>
          <div style={{ position: "absolute", right: 0, top: 0, height: 60, display: "flex", fontSize: 22, color: "#e8eaed" }}>
            {["–", "□", "✕"].map((ch) => (
              <div key={ch} style={{ width: 66, display: "flex", alignItems: "center", justifyContent: "center" }}>
                {ch}
              </div>
            ))}
          </div>
        </div>
        <div style={{ height: 68, background: "#35363a", display: "flex", alignItems: "center", gap: 20, padding: "0 20px", color: "#e8eaed", fontSize: 28 }}>
          <span>‹</span>
          <span>›</span>
          <span>↻</span>
          <div style={{ flex: 1, height: 46, borderRadius: 23, background: "#202124", display: "flex", alignItems: "center", gap: 14, padding: "0 22px", fontSize: 22 }}>
            <svg width="16" height="20" viewBox="0 0 14 16">
              <rect x="1" y="7" width="12" height="8" rx="2" fill="#9aa0a6" />
              <path d="M4 7V5a3 3 0 0 1 6 0v2" fill="none" stroke="#9aa0a6" strokeWidth="1.8" />
            </svg>
            <span>{tab === 0 ? (f >= 54 ? "streamio.tv/watch?v=c0uchm0de" : "streamio.tv") : "wikiverse.org/wiki/Couch_gaming"}</span>
          </div>
          <Circle glyph="controller" size={40} bg="linear-gradient(135deg,#3ecfa0,#38c6ff)" color="#03130d" />
        </div>
        <div style={{ position: "relative", height: 690 - 128, background: "#0f0f10", overflow: "hidden" }}>
          {tab === 0 ? (
            <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 22, padding: 28 }}>
              {Array.from({ length: 8 }, (_, i) => {
                const hue = (i * 61 + 190) % 360;
                return (
                  <div key={i}>
                    <div
                      style={{
                        height: 190,
                        borderRadius: 14,
                        background: `linear-gradient(135deg, hsl(${hue} 60% 42%), hsl(${(hue + 50) % 360} 70% 18%))`,
                        outline: i === 1 && hover ? "4px solid #fff" : "4px solid transparent",
                        outlineOffset: 3,
                      }}
                    />
                    <div style={{ height: 14, borderRadius: 7, background: "rgba(255,255,255,0.7)", width: "85%", marginTop: 12 }} />
                  </div>
                );
              })}
            </div>
          ) : (
            <div style={{ padding: "34px 140px", translate: `0 ${-tw(f, [78, 100], [0, 140], Easing.inOut(Easing.quad))}px` }}>
              <div style={{ color: "#e8eaed", fontSize: 52, fontFamily: "Georgia, serif" }}>Couch gaming</div>
              <div style={{ height: 1, background: "#3c4043", margin: "16px 0" }} />
              {Array.from({ length: 16 }, (_, i) => (
                <div key={i} style={{ height: 14, borderRadius: 7, background: "rgba(255,255,255,0.3)", width: `${80 + ((i * 7) % 18)}%`, marginTop: 16 }} />
              ))}
            </div>
          )}
          <div style={{ position: "absolute", left: "50%", bottom: 18, translate: "-50% 0", display: "flex", gap: 26, padding: "10px 24px", borderRadius: 999, background: "rgba(20,21,24,0.94)", border: "1px solid rgba(255,255,255,0.12)" }}>
            <Hint b="LS" label={j.keys[0]} lit={lit(20, 44)} />
            <Hint b="A" label={j.keys[1]} lit={lit(46, 56)} />
            <Hint b="RB" label={j.keys[2]} lit={lit(66, 74)} />
            <Hint b="RS" label={j.keys[3]} lit={lit(78, 100)} />
          </div>
        </div>
        {f >= 16 && f < 68 && (
          <div
            style={{
              position: "absolute",
              left: cx - 22,
              top: 128 + cy - 22,
              width: 44,
              height: 44,
              borderRadius: 22,
              border: "4px solid #fff",
              background: `rgba(62,207,160,${0.4 + press * 0.5})`,
              scale: String(1 - press * 0.25),
            }}
          />
        )}
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
          Console Mode <span style={gradText(0)}>1.6</span>
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

export const Features16: React.FC<{ lang: Lang }> = ({ lang }) => {
  useFonts();
  const t = T[lang];
  const scenes: React.FC<{ t: Strings }>[] = [Title, PickScreen, SessionMenu, Switcher, ControlFs, ConsoleUi, Covers, Shortcuts, More, Joy, Outro];
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
