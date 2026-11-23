//! UI sounds: the 1.7 wav files, decoded once and played on a shared output stream.

use std::{io::Cursor, sync::Arc, time::Instant};

use crate::log;

const MOVE: &[u8] = include_bytes!("../../../src/ConsoleMode/Assets/Sounds/move.wav");
const CONFIRM: &[u8] = include_bytes!("../../../src/ConsoleMode/Assets/Sounds/confirm.wav");

pub struct Ui { stream: rodio::MixerDeviceSink }

impl Ui {
    pub fn open() -> Option<Self> {
        let t = Instant::now();
        match rodio::DeviceSinkBuilder::open_default_sink() {
            Ok(mut stream) => { stream.log_on_drop(false); log(format!("audio out open in {:?}", t.elapsed())); Some(Self { stream }) }
            Err(e) => { log(format!("audio out failed: {e}")); None }
        }
    }
    fn play(&self, bytes: &'static [u8]) {
        let t = Instant::now();
        match rodio::Decoder::new(Cursor::new(bytes)) {
            Ok(src) => { self.stream.mixer().add(src); log(format!("sound queued in {:?}", t.elapsed())); }
            Err(e) => log(format!("sound decode failed: {e}")),
        }
    }
    pub fn play_move(&self) { self.play(MOVE) }
    pub fn play_confirm(&self) { self.play(CONFIRM) }
}

#[allow(dead_code)]
fn _unused(_: Arc<()>) {}
