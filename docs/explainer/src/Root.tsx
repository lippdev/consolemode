import "./index.css";
import type { FC } from "react";
import { Composition } from "remotion";
import { Explainer } from "./Explainer";

export const RemotionRoot: FC = () => {
  return (
    <Composition
      id="ConsoleModeExplainer"
      component={Explainer}
      durationInFrames={630}
      fps={30}
      width={1280}
      height={720}
    />
  );
};
