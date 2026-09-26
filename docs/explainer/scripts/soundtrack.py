"""Synthesises the soundtrack of the Keynote film (src/Keynote.tsx).

Everything is generated here, so there is no third-party audio to license. The hits follow
the scene timings in Keynote.tsx (30 fps, 120 BPM: one beat = 15 frames, one bar = 60 frames).

    pip install numpy scipy
    python scripts/soundtrack.py          # writes out/soundtrack.wav
"""

import os
import wave

import numpy as np
from scipy.signal import butter, sosfilt

SR = 44100
FPS = 30
TOTAL_FRAMES = 2100
N = int(TOTAL_FRAMES / FPS * SR) + SR * 2
BEAT = 15  # frames

rng = np.random.default_rng(7)


def at(frame: float) -> int:
    return int(frame / FPS * SR)


def midi(m: float) -> float:
    return 440.0 * 2 ** ((m - 69) / 12)


def lowpass(x, hz, order=2):
    return sosfilt(butter(order, hz, "low", fs=SR, output="sos"), x)


def highpass(x, hz, order=2):
    return sosfilt(butter(order, hz, "high", fs=SR, output="sos"), x)


def bandpass(x, lo, hi, order=2):
    return sosfilt(butter(order, [lo, hi], "band", fs=SR, output="sos"), x)


def add(bus, start, sig, gain=1.0):
    end = min(len(bus), start + len(sig))
    if start < end:
        bus[start:end] += sig[: end - start] * gain


def reverb(x, seconds=2.6, mix=0.35):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-t * 6.5 / seconds)
    ir = lowpass(ir, 6000)
    ir /= np.sqrt(np.sum(ir**2))
    size = 1 << int(np.ceil(np.log2(len(x) + n)))
    wet = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[: len(x)]
    return x * (1 - mix) + wet * mix


# F – C – G – Am, one chord per bar.
CHORDS = [[53, 57, 60, 65], [48, 55, 60, 64], [55, 59, 62, 67], [57, 60, 64, 69]]
ROOTS = [41, 36, 43, 45]


def chord_at(frame):
    return int(frame // 60) % 4


def saw(freq, n, phase=0.0):
    t = np.arange(n) / SR
    return 2 * ((t * freq + phase) % 1.0) - 1


# ───────── instruments ─────────


def kick(n=int(0.45 * SR)):
    t = np.arange(n) / SR
    f = 45 + 110 * np.exp(-t * 28)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph) * np.exp(-t * 7) + 0.25 * np.sin(ph) * np.exp(-t * 60)


def hat(n=int(0.06 * SR)):
    t = np.arange(n) / SR
    return highpass(rng.standard_normal(n), 7000) * np.exp(-t * 70)


def clap(n=int(0.25 * SR)):
    t = np.arange(n) / SR
    noise = bandpass(rng.standard_normal(n), 900, 4000)
    env = np.exp(-t * 22) + 0.6 * np.exp(-((t - 0.012) * 260) ** 2) + 0.5 * np.exp(-((t - 0.024) * 260) ** 2)
    return noise * env + 0.3 * np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30)


def pluck(freq, n=int(0.5 * SR)):
    t = np.arange(n) / SR
    s = 0.6 * np.sin(2 * np.pi * freq * t) + 0.25 * np.sin(2 * np.pi * 2 * freq * t) + 0.1 * saw(freq, n)
    return lowpass(s, 3500) * np.exp(-t * 9) * np.minimum(1, t * 400)


def bass(freq, n):
    t = np.arange(n) / SR
    s = saw(freq, n) + saw(freq * 1.005, n)
    env = np.exp(-t * 5) * 0.8 + 0.2
    return lowpass(s * env * np.minimum(1, t * 300), 420) + 0.6 * np.sin(2 * np.pi * freq * t) * env


def impact(n=int(3.5 * SR)):
    t = np.arange(n) / SR
    f = 28 + 60 * np.exp(-t * 6)
    sub = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 1.4)
    crack = lowpass(rng.standard_normal(n), 2500) * np.exp(-t * 9)
    return sub * 1.1 + crack * 0.5


def riser(n):
    t = np.arange(n) / SR
    x = rng.standard_normal(n)
    out = np.zeros(n)
    steps = 24
    for i in range(steps):
        a, b = i * n // steps, (i + 1) * n // steps
        hz = 300 * (12000 / 300) ** (i / (steps - 1))
        out[a:b] = bandpass(x, hz * 0.6, min(hz * 1.6, SR / 2 - 100))[a:b]
    return out * (t / t[-1]) ** 2


def whoosh(n=int(0.7 * SR)):
    t = np.arange(n) / SR
    env = np.sin(np.pi * np.clip(t / t[-1], 0, 1)) ** 2
    return bandpass(rng.standard_normal(n), 400, 5000) * env


def blip(freq=1760, n=int(0.18 * SR)):
    t = np.arange(n) / SR
    return (np.sin(2 * np.pi * freq * t) + 0.3 * np.sin(2 * np.pi * freq * 2 * t)) * np.exp(-t * 26)


def chime(n=int(2.0 * SR)):
    t = np.arange(n) / SR
    s = sum(np.sin(2 * np.pi * midi(m) * t) * g for m, g in [(81, 0.6), (88, 0.4), (93, 0.3), (100, 0.15)])
    return s * np.exp(-t * 2.2) * np.minimum(1, t * 200)


# ───────── arrangement ─────────

drums = np.zeros(N)
music = np.zeros(N)  # goes through the reverb
fx = np.zeros(N)

# Beat runs 300–1860, drops out while the Home button fills (900–1020) and before the finale.
DROP = [(900, 1020), (1830, 1860)]


def dropped(frame):
    return any(a <= frame < b for a, b in DROP)


K = kick()
H = hat()
CL = clap()
for f in range(300, 1860, BEAT):
    if dropped(f):
        continue
    add(drums, at(f), K, 0.9)
    add(drums, at(f + BEAT / 2), H, 0.22)
    beat_in_bar = (f // BEAT) % 4
    if f >= 720 and beat_in_bar in (1, 3):
        add(drums, at(f), CL, 0.35)

# Pad from the start, opening up with the beat.
pad = np.zeros(N)
for bar in range(TOTAL_FRAMES // 60 + 1):
    notes = CHORDS[bar % 4]
    s, n = at(bar * 60), at(60) + int(0.4 * SR)
    t = np.arange(n) / SR
    env = np.minimum(1, t / 0.35) * np.minimum(1, np.maximum(0, (n / SR - t) / 0.4))
    chord = sum(saw(midi(m), n, 0.1 * i) + saw(midi(m) * 1.004, n, 0.37 * i) for i, m in enumerate(notes))
    add(pad, s, chord * env, 0.05)
pad_cut = lowpass(pad, 700)
pad_open = lowpass(pad, 2600)
mixer = np.clip((np.arange(N) - at(270)) / (at(330) - at(270)), 0, 1)
music += pad_cut * (1 - mixer) + pad_open * mixer * 0.8

# Bass (8ths) and arpeggio (16ths) with the beat.
eighth = BEAT / 2
f = 300.0
while f < 1860:
    if not dropped(f):
        c = chord_at(f)
        add(music, at(f), bass(midi(ROOTS[c]), int(0.22 * SR)), 0.28)
    f += eighth

sixteenth = BEAT / 4
step = 0
f = 540.0
while f < 1860:
    if not dropped(f):
        c = chord_at(f)
        notes = CHORDS[c] + [n + 12 for n in CHORDS[c]]
        add(music, at(f), pluck(midi(notes[step % len(notes)] + 12)), 0.09)
    step += 1
    f += sixteenth

# Hits and transitions.
add(fx, at(95), riser(at(150) - at(95)), 0.25)
add(fx, at(150), impact(), 0.9)
add(music, at(150), chime(), 0.25)
for f in [300, 540, 720, 1020, 1140, 1380, 1560, 1740]:
    add(fx, at(f) - int(0.35 * SR), whoosh(), 0.18)
add(fx, at(960), riser(at(1020) - at(960)), 0.3)  # Home ring filling
add(fx, at(1020), impact(int(2.0 * SR)), 0.55)
add(fx, at(1780), riser(at(1860) - at(1780)), 0.3)
add(fx, at(1860), impact(), 1.0)
add(music, at(1860), chime(), 0.35)

# UI sounds that match what is on screen.
add(fx, at(390), blip(1200), 0.25)  # click on Play now
for f in [585, 600, 615]:  # restore checks
    add(fx, at(f), blip(1568), 0.18)
for f in range(1400, 1442, 3):  # typing consolemode://start
    add(fx, at(f), highpass(rng.standard_normal(int(0.02 * SR)), 3000) * np.exp(-np.arange(int(0.02 * SR)) / SR * 200), 0.08)
for f in [1485, 1500, 1515]:  # Start / Menu / Stop keys
    add(fx, at(f), blip(1320), 0.18)

# Mix.
mix = drums + reverb(music, 2.8, 0.35) + reverb(fx, 2.2, 0.25)
fade = np.clip((at(2100) - np.arange(N)) / (at(2100) - at(2010)), 0, 1)
mix *= fade
mix = np.tanh(mix * 1.2)
mix /= np.max(np.abs(mix)) / 0.89
mix = mix[: at(TOTAL_FRAMES)]

# Slight stereo width: delay one channel of the reverb-heavy part by a few ms.
wide = np.roll(mix, int(0.008 * SR)) * 0.15
left, right = mix + wide, mix - wide
stereo = np.stack([left, right], axis=1)
stereo /= np.max(np.abs(stereo)) / 0.89

os.makedirs("out", exist_ok=True)
with wave.open("out/soundtrack.wav", "wb") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes((stereo * 32767).astype("<i2").tobytes())
print("out/soundtrack.wav", len(mix) / SR, "s")
