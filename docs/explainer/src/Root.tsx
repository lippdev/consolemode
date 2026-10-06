import "./index.css";
import type { FC } from "react";
import { Composition } from "remotion";
import { Explainer } from "./Explainer";
import { WHATS_NEW_DURATION, WhatsNew } from "./WhatsNew";
import { ROADMAP_DURATION, Roadmap } from "./Roadmap";
import { KEYNOTE_DURATION, Keynote } from "./Keynote";
import { FEATURES_DURATION, Features } from "./Features";
import { FEATURES16_DURATION, Features16 } from "./Features16";
import { LAUNCH_DURATION, Launch } from "./Launch";
import { ALPHA3_DURATION, Alpha3 } from "./Alpha3";
import { RELEASE16_DURATION, Release16 } from "./Release16";

export const RemotionRoot: FC = () => {
  return (
    <>
      <Composition
        id="ConsoleModeExplainer"
        component={Explainer}
        durationInFrames={630}
        fps={30}
        width={1280}
        height={720}
      />
      <Composition
        id="WhatsNew"
        component={WhatsNew}
        durationInFrames={WHATS_NEW_DURATION}
        fps={30}
        width={1280}
        height={720}
      />
      <Composition
        id="Roadmap"
        component={Roadmap}
        durationInFrames={ROADMAP_DURATION}
        fps={30}
        width={1280}
        height={720}
      />
      <Composition
        id="Keynote"
        component={Keynote}
        durationInFrames={KEYNOTE_DURATION}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Launch"
        component={Launch}
        durationInFrames={LAUNCH_DURATION}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Alpha3"
        component={Alpha3}
        durationInFrames={ALPHA3_DURATION}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Release16"
        component={Release16}
        durationInFrames={RELEASE16_DURATION}
        fps={30}
        width={1920}
        height={1080}
      />
      <Composition
        id="Features"
        component={Features}
        durationInFrames={FEATURES_DURATION}
        fps={30}
        width={1920}
        height={1080}
        defaultProps={{ lang: "en" as const }}
      />
      <Composition
        id="Features16"
        component={Features16}
        durationInFrames={FEATURES16_DURATION}
        fps={30}
        width={1920}
        height={1080}
        defaultProps={{ lang: "en" as const }}
      />
    </>
  );
};
