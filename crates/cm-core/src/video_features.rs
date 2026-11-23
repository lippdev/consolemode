//! Windows' "Variable refresh rate" setting (1.7 VideoFeaturesService): a flag inside the text value
//! `DirectXUserGlobalSettings` of `HKCU\Software\Microsoft\DirectX\UserGpuPreferences`
//! (`SwapEffectUpgradeEnable=1;VRROptimizeEnable=1;`). Pure text rules, so they are tested anywhere;
//! reading and writing the value is `cm_platform::video_win`, the session's backup and restore are
//! in cm-app (`video.rs`) and the journal.

pub const KEY: &str = r"Software\Microsoft\DirectX\UserGpuPreferences";
pub const VALUE: &str = "DirectXUserGlobalSettings";
const FLAG: &str = "VRROptimizeEnable=";

/// Whether the setting text turns VRR on.
pub fn vrr_enabled(setting: &str) -> bool { setting.contains("VRROptimizeEnable=1") }

/// The setting text with the VRR flag set: the flag is replaced where it is, and added where it is not
/// (an empty text, or after the others, with a `;` between when they did not end in one).
pub fn with_vrr(current: &str, enabled: bool) -> String {
    let flag = if enabled { "1" } else { "0" };
    if let Some(i) = current.find(FLAG) {
        let after = &current[i + FLAG.len()..];
        // VRROptimizeEnable=<digit>: the digit is replaced; with none (a hand-edited value) the flag is put in
        let rest = after.strip_prefix(|c: char| c.is_ascii_digit()).unwrap_or(after);
        return format!("{}{FLAG}{flag}{rest}", &current[..i]);
    }
    if current.trim().is_empty() { format!("{FLAG}{flag};") }
    else if current.trim_end().ends_with(';') { format!("{current}{FLAG}{flag};") }
    else { format!("{current};{FLAG}{flag};") }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn reads_the_flag() {
        assert!(vrr_enabled("SwapEffectUpgradeEnable=1;VRROptimizeEnable=1;"));
        assert!(!vrr_enabled("SwapEffectUpgradeEnable=1;VRROptimizeEnable=0;"));
        assert!(!vrr_enabled("SwapEffectUpgradeEnable=1;"));
        assert!(!vrr_enabled(""));
    }

    #[test]
    fn an_empty_setting_gets_just_the_flag() {
        assert_eq!(with_vrr("", true), "VRROptimizeEnable=1;");
        assert_eq!(with_vrr("  ", true), "VRROptimizeEnable=1;");
    }

    #[test]
    fn the_flag_is_replaced_where_it_is_and_the_rest_stays() {
        assert_eq!(with_vrr("SwapEffectUpgradeEnable=1;VRROptimizeEnable=0;AutoHDREnable=1;", true), "SwapEffectUpgradeEnable=1;VRROptimizeEnable=1;AutoHDREnable=1;");
        assert_eq!(with_vrr("VRROptimizeEnable=1;", false), "VRROptimizeEnable=0;");
        // a value that has none after the equals sign (hand-edited)
        assert_eq!(with_vrr("A=1;VRROptimizeEnable=;", true), "A=1;VRROptimizeEnable=1;");
    }

    #[test]
    fn the_flag_is_added_after_the_others() {
        assert_eq!(with_vrr("SwapEffectUpgradeEnable=1;", true), "SwapEffectUpgradeEnable=1;VRROptimizeEnable=1;");
        // without the closing ; one is added between
        assert_eq!(with_vrr("SwapEffectUpgradeEnable=1", true), "SwapEffectUpgradeEnable=1;VRROptimizeEnable=1;");
    }

    #[test]
    fn turning_it_on_is_idempotent_and_reads_back() {
        for start in ["", "A=1;", "A=1", "A=1;VRROptimizeEnable=0;"] {
            let on = with_vrr(start, true);
            assert!(vrr_enabled(&on), "{start}");
            assert_eq!(with_vrr(&on, true), on);
            assert!(!vrr_enabled(&with_vrr(&on, false)));
        }
    }
}
