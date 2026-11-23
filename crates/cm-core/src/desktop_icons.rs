//! Desktop icon positions (ported from 1.7's DesktopIconLayout). Windows can reshuffle the icons
//! when the primary display changes or a screen is disconnected (issue #30): the positions are read
//! before the session and put back after it.

use std::collections::{HashMap, HashSet};

use serde::{Deserialize, Serialize};

/// One desktop icon: the shell's parsing name (a path or a `::{CLSID}`) and where it sits.
#[derive(Clone, Debug, PartialEq, Eq, Serialize, Deserialize)]
pub struct DesktopIcon {
    pub name: String,
    pub x: i32,
    pub y: i32,
}

/// The saved icons that are still on the desktop but somewhere else. Icons that were added or
/// removed during the session are left alone.
pub fn moves(saved: &[DesktopIcon], current: &[DesktopIcon]) -> Vec<DesktopIcon> {
    let now: HashMap<String, &DesktopIcon> = current.iter().filter(|i| !i.name.trim().is_empty()).map(|i| (i.name.to_lowercase(), i)).collect();
    let mut seen = HashSet::new();
    let mut out = Vec::new();
    for icon in saved {
        if icon.name.trim().is_empty() || !seen.insert(icon.name.to_lowercase()) { continue; }
        if now.get(&icon.name.to_lowercase()).is_some_and(|there| (there.x, there.y) != (icon.x, icon.y)) { out.push(icon.clone()); }
    }
    out
}

#[cfg(test)]
mod tests {
    use super::*;

    fn icon(name: &str, x: i32, y: i32) -> DesktopIcon { DesktopIcon { name: name.into(), x, y } }
    fn bin() -> DesktopIcon { icon("::{645FF040-5081-101B-9F08-00AA002F954E}", 0, 0) }
    fn game() -> DesktopIcon { icon(r"C:\Users\Me\Desktop\Game.lnk", 0, 96) }

    #[test]
    fn icons_already_in_place_are_not_moved() { assert!(moves(&[bin(), game()], &[bin(), game()]).is_empty()); }

    #[test]
    fn icons_somewhere_else_go_back_to_where_they_were() {
        let now = [bin(), DesktopIcon { x: 1800, y: 0, ..game() }];
        assert_eq!(moves(&[bin(), game()], &now), vec![game()]);
    }

    #[test]
    fn icons_removed_during_the_session_are_skipped() {
        let now = [DesktopIcon { x: 200, ..bin() }];
        assert!(moves(&[bin(), game()], &now).iter().all(|m| m.name != game().name));
    }

    #[test]
    fn icons_added_during_the_session_are_left_alone() {
        assert!(moves(&[bin()], &[bin(), icon(r"C:\Users\Me\Desktop\New.txt", 500, 500)]).is_empty());
    }

    #[test]
    fn names_match_regardless_of_case() {
        let now = [DesktopIcon { name: game().name.to_uppercase(), x: 64, ..game() }];
        assert_eq!(moves(&[game()], &now).len(), 1);
    }

    #[test]
    fn blank_and_repeated_names_are_ignored() {
        let blank = icon("", 0, 0);
        let now = [DesktopIcon { x: 5, ..blank.clone() }, DesktopIcon { x: 10, ..game() }];
        assert_eq!(moves(&[blank, game(), DesktopIcon { x: 999, ..game() }], &now), vec![game()]);
    }
}
