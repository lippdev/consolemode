//! UI sounds on one shared output (Sistema → Som): "Console" plays the 1.7 set, "Suave" soft
//! synthesized tones; a volume of their own; the opening sound when the app starts.

use std::{io::Cursor, time::Duration};

use rodio::Source;

const MOVE: &[u8] = include_bytes!("../../../src/ConsoleMode/Assets/Sounds/move.wav");
const TAB: &[u8] = include_bytes!("../../../src/ConsoleMode/Assets/Sounds/tab.wav");
const CONFIRM: &[u8] = include_bytes!("../../../src/ConsoleMode/Assets/Sounds/confirm.wav");
const BACK: &[u8] = include_bytes!("../../../src/ConsoleMode/Assets/Sounds/back.wav");
const BOOT: &[u8] = include_bytes!("../../../src/ConsoleMode/Assets/Sounds/boot.wav");

#[derive(Clone, Copy)]
pub enum Sound { Move, Tab, Confirm, Back, Boot }

impl Sound {
    /// The opening's sounds have their own switch (`introSound`): they play even with the navigation sounds off.
    fn is_boot(self) -> bool { matches!(self, Sound::Boot) }
}

#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum Style { Console, Soft }

impl Style {
    pub fn from_config(s: &str) -> Style { if s == "soft" { Style::Soft } else { Style::Console } }
}

pub struct Sounds {
    /// The audio output, open only while sounds are playing or were played a moment ago: an open
    /// output keeps a thread feeding silence to the device, the biggest cost of an idle app.
    out: std::cell::RefCell<Option<rodio::MixerDeviceSink>>,
    /// Until when the output must stay open (the last sound's end, plus a moment).
    keep_until: std::cell::Cell<std::time::Instant>,
    /// Sistema → Som → "Sons de navegação".
    pub enabled: bool,
    pub style: Style,
    /// 0–100.
    pub volume: u8,
    /// The opening's sound while it plays.
    boot: std::cell::RefCell<Option<rodio::Player>>,
}

impl Sounds {
    /// How long the output stays open after the last sound ends (opening it again takes a moment).
    const LINGER: Duration = Duration::from_secs(20);

    pub fn open() -> Self {
        Self { out: Default::default(), keep_until: std::cell::Cell::new(std::time::Instant::now()), enabled: true, style: Style::Console, volume: 60, boot: Default::default() }
    }

    /// The output, opened if it was closed.
    fn output(&self) -> Option<std::cell::Ref<'_, rodio::MixerDeviceSink>> {
        if self.out.borrow().is_none() {
            let out = rodio::DeviceSinkBuilder::open_default_sink().ok().map(|mut s| { s.log_on_drop(false); s });
            *self.out.borrow_mut() = out;
        }
        std::cell::Ref::filter_map(self.out.borrow(), |o| o.as_ref()).ok()
    }

    /// The output stays open until `d` from now (a sound's length) and the linger after it.
    fn keep_open_for(&self, d: Duration) {
        let until = std::time::Instant::now() + d + Self::LINGER;
        if until > self.keep_until.get() { self.keep_until.set(until); }
    }

    /// Closes the output once nothing has played for a while (called every few seconds).
    pub fn close_when_idle(&self) {
        if self.out.borrow().is_none() || std::time::Instant::now() < self.keep_until.get() { return; }
        if self.boot.borrow().as_ref().is_some_and(|p| !p.empty()) { return; }
        self.boot.borrow_mut().take();
        self.out.borrow_mut().take();
    }

    pub fn play(&self, s: Sound) {
        if !self.enabled && !s.is_boot() { return; }
        let Some(out) = self.output() else { return };
        let gain = gain(self.volume);
        if self.style == Style::Soft && !s.is_boot() {
            // one short sine note per sound, quieter than the samples
            let (f, ms) = match s { Sound::Move => (660., 45), Sound::Tab => (520., 70), Sound::Confirm => (880., 90), _ => (440., 80) };
            let tone = rodio::source::SineWave::new(f).take_duration(Duration::from_millis(ms)).fade_out(Duration::from_millis(ms)).amplify(0.18 * gain);
            out.mixer().add(tone);
            self.keep_open_for(Duration::from_millis(ms));
            return;
        }
        let bytes = match s { Sound::Move => MOVE, Sound::Tab => TAB, Sound::Confirm => CONFIRM, Sound::Back => BACK, Sound::Boot => BOOT };
        let Ok(src) = rodio::Decoder::new(Cursor::new(bytes)) else { return };
        self.keep_open_for(src.total_duration().unwrap_or(Duration::from_secs(10)));
        if s.is_boot() {
            // on a player of its own, so skipping the opening can stop it (the long ones run 8 s)
            let player = rodio::Player::connect_new(out.mixer());
            player.append(src.amplify(gain));
            *self.boot.borrow_mut() = Some(player);
        } else {
            out.mixer().add(src.amplify(gain));
        }
    }

    /// The opening was skipped: its sound stops with it.
    pub fn stop_boot(&self) {
        if let Some(p) = self.boot.borrow_mut().take() { p.stop(); }
    }
}

/// The slider is linear to the ear: squared, so 50 % sounds like half (60 % ≈ the samples as they are).
pub fn gain(volume: u8) -> f32 { let v = volume.min(100) as f32 / 100.; v * v * 2.8 }

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn opening_sound_decodes() {
        let sound = rodio::Decoder::new(Cursor::new(BOOT)).unwrap();
        assert!(sound.total_duration().is_some_and(|duration| !duration.is_zero()));
    }
    #[test]
    fn gain_curve() {
        assert_eq!(gain(0), 0.);
        assert!(gain(100) > gain(60) && gain(60) > gain(20));
        assert!((gain(60) - 1.0).abs() < 0.02);
        assert_eq!(Style::from_config("soft"), Style::Soft);
        assert_eq!(Style::from_config(""), Style::Console);
    }
}
