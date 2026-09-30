import React, { createContext, useContext } from "react";
import { RacingGame } from "./RacingGame";

// The game shown on the TV and behind the session menu. By default it is the synthwave race
// drawn in RacingGame.tsx; a film can provide real footage instead (see Release16.tsx).
// `t` is the film's global frame, so the same run continues across scenes.

export type GameInfo = { proc: string; title: string; letter: string; color: string };

type GameSlot = {
  render: (t: number, hud: boolean) => React.ReactNode;
  /** How the game shows up in the session menu's window list. */
  info: GameInfo;
  /** Screenshot of ControlFS to reveal when it opens, from public/. */
  controlFs?: string;
};

const RACE: GameSlot = {
  render: (t, hud) => <RacingGame t={t} hud={hud} />,
  info: { proc: "NeonDrift", title: "Neon Drift", letter: "N", color: "linear-gradient(135deg,#ff3ec9,#7a2cff)" },
};

export const GameContext = createContext<GameSlot>(RACE);
export const useGameSlot = () => useContext(GameContext);

export const GamePlay: React.FC<{ t: number; hud?: boolean }> = ({ t, hud = true }) => {
  const slot = useGameSlot();
  return <>{slot.render(t, hud)}</>;
};
