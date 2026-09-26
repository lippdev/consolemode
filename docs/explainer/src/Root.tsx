import "./index.css";
import type { FC } from "react";
import { Composition } from "remotion";
import { Explainer } from "./Explainer";
import { WHATS_NEW_DURATION, WhatsNew } from "./WhatsNew";
import { ROADMAP_DURATION, Roadmap } from "./Roadmap";
import { KEYNOTE_DURATION, Keynote } from "./Keynote";

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
    </>
  );
};
