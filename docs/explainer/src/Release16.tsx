import React from "react";
import { AbsoluteFill, Audio, Easing, getStaticFiles, interpolate, OffthreadVideo, Sequence, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { Badge, ConsoleIntro, ConsoleUi, Covers, G, More, Part, SessionMenu, Shortcuts } from "./Alpha3";
import { C, Center, clamp, Glow, Line, Logo, SANS, tw, useFonts } from "./kit";
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

// ── JoyChromium: a Chromium-style browser window driven by the controller (illustrative). ──

const BW = { x: 240, y: 380, w: 1440, h: 660 };
const STRIP = 46;
const BAR = 52;
const PAGE_H = BW.h - STRIP - BAR;
const CHROME = { strip: "#1f2023", active: "#35363a", text: "#e8eaed", sub: "#9aa0a6", omni: "#202124", page: "#0f0f10" };

const Thumb: React.FC<{ i: number; w: number; hover?: number }> = ({ i, w, hover = 0 }) => {
  const hue = (i * 61 + 190) % 360;
  return (
    <div style={{ width: w }}>
      <div
        style={{
          width: w,
          height: (w * 9) / 16,
          borderRadius: 12,
          background: `linear-gradient(135deg, hsl(${hue} 60% 42%), hsl(${(hue + 50) % 360} 70% 16%))`,
          position: "relative",
          outline: hover > 0.5 ? "3px solid #fff" : "3px solid transparent",
          outlineOffset: 3,
          scale: String(1 + 0.04 * hover),
        }}
      >
        <div style={{ position: "absolute", left: "38%", top: "30%", width: "24%", aspectRatio: "1", borderRadius: "50%", background: `hsl(${(hue + 120) % 360} 80% 70% / 0.5)` }} />
        <span style={{ position: "absolute", right: 8, bottom: 8, padding: "1px 6px", borderRadius: 4, background: "rgba(0,0,0,0.8)", color: "#fff", fontFamily: SANS, fontSize: 13, fontWeight: 600 }}>
          {10 + ((i * 7) % 40)}:{String((i * 13) % 60).padStart(2, "0")}
        </span>
      </div>
      <div style={{ display: "flex", gap: 10, marginTop: 10 }}>
        <div style={{ width: 30, height: 30, borderRadius: 15, background: `hsl(${(hue + 200) % 360} 50% 50%)`, flexShrink: 0 }} />
        <div style={{ flex: 1 }}>
          <div style={{ height: 11, borderRadius: 5, background: "rgba(255,255,255,0.75)", width: "92%" }} />
          <div style={{ height: 11, borderRadius: 5, background: "rgba(255,255,255,0.75)", width: "60%", marginTop: 6 }} />
          <div style={{ height: 9, borderRadius: 5, background: "rgba(255,255,255,0.3)", width: "50%", marginTop: 8 }} />
        </div>
      </div>
    </div>
  );
};

/** Tab 1: a video site, then the video that was clicked. */
const VideoSite: React.FC<{ hover: number; playing: number }> = ({ hover, playing }) => (
  <AbsoluteFill style={{ background: CHROME.page, fontFamily: SANS }}>
    <div style={{ height: 58, display: "flex", alignItems: "center", gap: 14, padding: "0 28px" }}>
      <div style={{ width: 34, height: 24, borderRadius: 7, background: "#ff3b5c", display: "flex", alignItems: "center", justifyContent: "center" }}>
        <G name="play" size={14} />
      </div>
      <span style={{ color: CHROME.text, fontSize: 22, fontWeight: 700, letterSpacing: "-0.02em" }}>streamio</span>
      <div style={{ flex: 1, display: "flex", justifyContent: "center" }}>
        <div style={{ width: 520, height: 38, borderRadius: 19, border: "1px solid #303134", color: CHROME.sub, fontSize: 16, display: "flex", alignItems: "center", padding: "0 18px" }}>Search</div>
      </div>
      <div style={{ width: 34, height: 34, borderRadius: 17, background: "linear-gradient(135deg,#3ecfa0,#38c6ff)" }} />
    </div>
    {playing < 1 ? (
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 312px)", gap: "26px 22px", padding: "14px 28px", opacity: 1 - playing }}>
        {Array.from({ length: 8 }, (_, i) => (
          <Thumb key={i} i={i} w={312} hover={i === 1 ? hover : 0} />
        ))}
      </div>
    ) : null}
    {playing > 0 && (
      <div style={{ position: "absolute", top: 70, left: 28, right: 28, display: "flex", gap: 24, opacity: playing }}>
        <div style={{ width: 860 }}>
          <div style={{ width: 860, height: 430, borderRadius: 14, overflow: "hidden", position: "relative", background: "linear-gradient(135deg, hsl(251 60% 42%), hsl(301 70% 16%))" }}>
            <div style={{ position: "absolute", left: "40%", top: "28%", width: "20%", aspectRatio: "1", borderRadius: "50%", background: "hsl(11 80% 70% / 0.5)" }} />
            <div style={{ position: "absolute", left: 18, right: 18, bottom: 16, height: 5, borderRadius: 3, background: "rgba(255,255,255,0.3)" }}>
              <div style={{ width: `${8 + playing * 4}%`, height: "100%", borderRadius: 3, background: "#ff3b5c" }} />
            </div>
          </div>
          <div style={{ height: 16, borderRadius: 8, background: "rgba(255,255,255,0.85)", width: "70%", marginTop: 16 }} />
        </div>
        <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 14 }}>
          {[2, 3, 4, 5].map((i) => (
            <div key={i} style={{ display: "flex", gap: 12 }}>
              <div style={{ width: 168, height: 94, borderRadius: 10, background: `linear-gradient(135deg, hsl(${(i * 61 + 190) % 360} 60% 42%), hsl(${(i * 61 + 240) % 360} 70% 16%))` }} />
              <div style={{ flex: 1 }}>
                <div style={{ height: 10, borderRadius: 5, background: "rgba(255,255,255,0.7)", width: "90%" }} />
                <div style={{ height: 10, borderRadius: 5, background: "rgba(255,255,255,0.7)", width: "55%", marginTop: 6 }} />
                <div style={{ height: 8, borderRadius: 4, background: "rgba(255,255,255,0.3)", width: "45%", marginTop: 8 }} />
              </div>
            </div>
          ))}
        </div>
      </div>
    )}
  </AbsoluteFill>
);

/** Tab 2: an encyclopedia article, long enough to scroll. */
const WikiPage: React.FC<{ scroll: number }> = ({ scroll }) => {
  const para = (n: number, key: string) => (
    <div key={key} style={{ marginTop: 18 }}>
      {Array.from({ length: n }, (_, i) => (
        <div key={i} style={{ height: 11, borderRadius: 5, background: "rgba(255,255,255,0.32)", width: i === n - 1 ? "58%" : `${92 + ((i * 7) % 8)}%`, marginTop: 10 }} />
      ))}
    </div>
  );
  return (
    <AbsoluteFill style={{ background: "#101114", fontFamily: SANS, overflow: "hidden" }}>
      <div style={{ translate: `0 ${-scroll}px`, padding: "34px 120px" }}>
        <div style={{ color: CHROME.sub, fontSize: 16 }}>From Wikiverse, the free encyclopedia</div>
        <div style={{ color: CHROME.text, fontSize: 44, fontWeight: 600, marginTop: 6, fontFamily: "Georgia, serif" }}>Couch gaming</div>
        <div style={{ height: 1, background: "#3c4043", marginTop: 14 }} />
        <div style={{ display: "flex", gap: 40 }}>
          <div style={{ flex: 1 }}>
            {para(5, "a")}
            {para(6, "b")}
            <div style={{ color: CHROME.text, fontSize: 28, fontWeight: 600, marginTop: 30, fontFamily: "Georgia, serif" }}>History</div>
            {para(7, "c")}
            {para(5, "d")}
            <div style={{ color: CHROME.text, fontSize: 28, fontWeight: 600, marginTop: 30, fontFamily: "Georgia, serif" }}>On the PC</div>
            {para(6, "e")}
          </div>
          <div style={{ width: 320, marginTop: 24 }}>
            <div style={{ height: 220, borderRadius: 8, background: "linear-gradient(135deg,#1F6B4A,#10191D)" }} />
            <div style={{ height: 10, borderRadius: 5, background: "rgba(255,255,255,0.3)", width: "80%", marginTop: 10 }} />
          </div>
        </div>
      </div>
      <div style={{ position: "absolute", right: 4, top: 6 + scroll * 0.35, width: 6, height: 150, borderRadius: 3, background: "rgba(255,255,255,0.3)" }} />
    </AbsoluteFill>
  );
};

const TabChip: React.FC<{ title: string; favicon: string; active: boolean; x: number }> = ({ title, favicon, active, x }) => (
  <div
    style={{
      position: "absolute",
      left: x,
      bottom: 0,
      width: 236,
      height: 36,
      borderRadius: "10px 10px 0 0",
      background: active ? CHROME.active : "transparent",
      display: "flex",
      alignItems: "center",
      gap: 10,
      padding: "0 12px",
      boxSizing: "border-box",
      fontFamily: SANS,
      fontSize: 14,
      color: active ? CHROME.text : CHROME.sub,
    }}
  >
    <div style={{ width: 16, height: 16, borderRadius: 4, background: favicon, flexShrink: 0 }} />
    <span style={{ flex: 1, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{title}</span>
    <span style={{ fontSize: 14 }}>✕</span>
  </div>
);

const NavIcon: React.FC<{ d: string }> = ({ d }) => (
  <svg width="22" height="22" viewBox="0 0 24 24">
    <path d={d} fill="none" stroke={CHROME.text} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
  </svg>
);

/** 1335–1455: coming soon, JoyChromium, a browser you drive with the controller. */
const JoyChromium: React.FC = () => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const win = spring({ frame: f - 14, fps, config: { damping: 18, stiffness: 110 } });
  const tab = f >= 76 ? 1 : 0;
  // Virtual cursor, in window coordinates, moved with the left stick.
  const cx = interpolate(f, [26, 52], [760, 520], { ...clamp, easing: Easing.inOut(Easing.cubic) });
  const cy = interpolate(f, [26, 52], [470, 230], { ...clamp, easing: Easing.inOut(Easing.cubic) });
  const press = interpolate(f, [54, 57, 62], [0, 1, 0], clamp);
  const hover = f >= 46 && f < 60 ? 1 : 0;
  const loading = tw(f, [57, 68], [0, 1], Easing.out(Easing.quad));
  const playing = tw(f, [64, 72], [0, 1], Easing.linear);
  const slide = tw(f, [76, 84], [1, 0]);
  const scroll = tw(f, [88, 116], [0, 230], Easing.inOut(Easing.quad));
  const lit = (from: number, to: number) => (f >= from && f < to ? 1 : 0);
  const tabs = [
    { title: "streamio – Home", favicon: "#ff3b5c", url: f >= 60 ? "streamio.tv/watch?v=c0uchm0de" : "streamio.tv" },
    { title: "Couch gaming – Wikiverse", favicon: "#e8eaed", url: "wikiverse.org/wiki/Couch_gaming" },
  ];
  return (
    <AbsoluteFill>
      <Glow color={C.violet} y={720} opacity={0.16} size={1800} />
      <Center top={50}>
        <div style={{ display: "flex", justifyContent: "center", opacity: tw(f, [0, 8], [0, 1], Easing.linear) }}>
          <span style={{ padding: "8px 20px", borderRadius: 999, border: `1.5px solid ${C.mint}`, color: C.mint, fontFamily: SANS, fontSize: 26, fontWeight: 700, letterSpacing: "0.12em" }}>COMING SOON</span>
        </div>
        <div style={{ height: 16 }} />
        <Line words={[{ t: "JoyChromium.", grad: true }]} at={4} size={104} stagger={4} />
        <div style={{ height: 8 }} />
        <Line words={["The", "whole", "web,", "with", "a", "controller.", "Straight", "from", "Console", "Mode."]} at={14} size={40} weight={600} stagger={2} color={C.muted} />
      </Center>
      <div
        style={{
          position: "absolute",
          left: BW.x,
          top: BW.y,
          width: BW.w,
          height: BW.h,
          borderRadius: 12,
          overflow: "hidden",
          background: CHROME.strip,
          border: "1px solid #3c4043",
          boxShadow: "0 40px 120px rgba(0,0,0,0.65)",
          opacity: win,
          translate: `0 ${(1 - win) * 120}px`,
        }}
      >
        {/* Tab strip */}
        <div style={{ position: "relative", height: STRIP, background: CHROME.strip }}>
          {tabs.map((t, i) => (
            <TabChip key={t.title} title={t.title} favicon={t.favicon} active={i === tab} x={10 + i * 238} />
          ))}
          <div style={{ position: "absolute", left: 10 + 2 * 238 + 8, bottom: 8, color: CHROME.sub, fontFamily: SANS, fontSize: 22 }}>+</div>
          <div style={{ position: "absolute", right: 0, top: 0, height: STRIP, display: "flex", fontFamily: SANS, fontSize: 16, color: CHROME.text }}>
            {["–", "□", "✕"].map((c) => (
              <div key={c} style={{ width: 54, display: "flex", alignItems: "center", justifyContent: "center" }}>
                {c}
              </div>
            ))}
          </div>
        </div>
        {/* Toolbar */}
        <div style={{ height: BAR, background: CHROME.active, display: "flex", alignItems: "center", gap: 14, padding: "0 14px", position: "relative" }}>
          <NavIcon d="M15 18l-6-6 6-6" />
          <NavIcon d="M9 18l6-6-6-6" />
          <NavIcon d="M20 11a8 8 0 1 0-2.3 5.7M20 4v7h-7" />
          <div style={{ flex: 1, height: 36, borderRadius: 18, background: CHROME.omni, display: "flex", alignItems: "center", gap: 12, padding: "0 16px", fontFamily: SANS, fontSize: 16, color: CHROME.text }}>
            <svg width="14" height="16" viewBox="0 0 14 16">
              <rect x="1" y="7" width="12" height="8" rx="2" fill={CHROME.sub} />
              <path d="M4 7V5a3 3 0 0 1 6 0v2" fill="none" stroke={CHROME.sub} strokeWidth="1.8" />
            </svg>
            <span>
              <span style={{ color: CHROME.text }}>{tabs[tab].url.split("/")[0]}</span>
              <span style={{ color: CHROME.sub }}>{tabs[tab].url.slice(tabs[tab].url.split("/")[0].length)}</span>
            </span>
            <span style={{ flex: 1 }} />
            <span style={{ color: CHROME.sub }}>☆</span>
          </div>
          <div style={{ width: 30, height: 30, borderRadius: 15, background: "linear-gradient(135deg,#3ecfa0,#38c6ff)", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <G name="controller" size={18} color="#03130d" />
          </div>
          <div style={{ color: CHROME.text, fontFamily: SANS, fontSize: 20 }}>⋮</div>
          {loading > 0 && loading < 1 && <div style={{ position: "absolute", left: 0, bottom: 0, height: 3, width: `${loading * 100}%`, background: "#8ab4f8" }} />}
        </div>
        {/* Page */}
        <div style={{ position: "relative", height: PAGE_H, overflow: "hidden" }}>
          {tab === 0 ? (
            <VideoSite hover={hover} playing={playing} />
          ) : (
            <AbsoluteFill style={{ translate: `${slide * 80}px 0`, opacity: 1 - slide * 0.8 }}>
              <WikiPage scroll={scroll} />
            </AbsoluteFill>
          )}
          {/* Controller hints */}
          <div
            style={{
              position: "absolute",
              left: "50%",
              bottom: 16,
              translate: "-50% 0",
              display: "flex",
              gap: 22,
              alignItems: "center",
              padding: "10px 20px",
              borderRadius: 999,
              background: "rgba(20,21,24,0.9)",
              border: "1px solid rgba(255,255,255,0.12)",
              fontFamily: SANS,
              fontSize: 17,
              color: CHROME.text,
              whiteSpace: "nowrap",
            }}
          >
            {(
              [
                ["LS", "Cursor", lit(26, 52)],
                ["A", "Click", lit(54, 62)],
                ["B", "Back", 0],
                ["LB RB", "Tabs", lit(74, 82)],
                ["RS", "Scroll", lit(88, 116)],
                ["Y", "Address bar", 0],
              ] as [string, string, number][]
            ).map(([b, t, on]) => (
              <span key={t} style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <Badge label={b} lit={on} h={28} />
                {t}
              </span>
            ))}
          </div>
        </div>
        {/* Virtual cursor */}
        {f >= 22 && f < 74 && (
          <div
            style={{
              position: "absolute",
              left: cx - 18,
              top: STRIP + BAR + cy - 18,
              width: 36,
              height: 36,
              borderRadius: 18,
              border: "3px solid #fff",
              background: `rgba(62,207,160,${0.35 + press * 0.5})`,
              boxShadow: "0 4px 14px rgba(0,0,0,0.6)",
              scale: String(1 - press * 0.25),
              opacity: tw(f, [22, 28], [0, 1], Easing.linear),
            }}
          />
        )}
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
