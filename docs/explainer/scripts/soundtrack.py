"""Synthesises the soundtrack of the Keynote film (src/Keynote.tsx).

Everything is generated here, so there is no third-party audio to license. The hits follow
the scene timings of the films (30 fps, 120 BPM: one beat = 15 frames, one bar = 60 frames).

    pip install numpy scipy
    python scripts/soundtrack.py          # writes out/soundtrack.wav (Keynote)
    python scripts/soundtrack.py launch   # writes out/launch.wav (Launch)
    python scripts/soundtrack.py alpha3   # writes out/alpha3.wav (Alpha3)
    python scripts/soundtrack.py v16      # writes out/release16.wav (Release16)
"""

import os
import sys
import wave

import numpy as np
from scipy.signal import butter, sosfilt

SR = 44100

# What happens when, in frames. `full` is the Keynote film (src/Keynote.tsx, S) and `launch`
# the short film (src/Launch.tsx, L).
TIMELINES = {
    "full": dict(
        fps=30, end=2100, out="out/soundtrack.wav",
        beat=(300, 1860), drops=[(900, 1020), (1830, 1860)], claps=720, arp=540,
        risers=[(95, 150, 0.25), (960, 1020, 0.3), (1780, 1860, 0.3)],
        impacts=[(150, 3.5, 0.9, 0.25), (1020, 2.0, 0.55, 0), (1860, 3.5, 1.0, 0.35)],
        whooshes=[300, 540, 720, 1020, 1140, 1380, 1560, 1740],
        blips=[(390, 1200, 0.25), (585, 1568, 0.18), (600, 1568, 0.18), (615, 1568, 0.18),
               (1485, 1320, 0.18), (1500, 1320, 0.18), (1515, 1320, 0.18)],
        typing=(1400, 1442), engine=[],
    ),
    "launch": dict(
        fps=30, end=960, out="out/launch.wav",
        beat=(150, 810), drops=[(780, 810)], claps=270, arp=150,
        risers=[(100, 150, 0.3), (740, 810, 0.3)],
        impacts=[(60, 2.0, 0.45, 0), (150, 3.5, 0.9, 0.25), (810, 3.5, 1.0, 0.35)],
        whooshes=[270, 390, 510, 630, 720],
        blips=[(30, 1200, 0.25), (390, 988, 0.16), (405, 988, 0.16), (420, 988, 0.16), (435, 988, 0.16),
               (450, 988, 0.16), (585, 1568, 0.18), (600, 1568, 0.18), (615, 1568, 0.18),
               (686, 1760, 0.2), (725, 1320, 0.15), (740, 1320, 0.15), (755, 1320, 0.15)],
        typing=None, engine=[(60, 150, 0.1), (150, 546, 0.2)],
    ),
    "alpha3": dict(
        fps=30, end=1380, out="out/alpha3.wav",
        beat=(120, 1230), drops=[(1200, 1230)], claps=510, arp=210,
        risers=[(70, 120, 0.25), (440, 472, 0.2), (1150, 1230, 0.3)],
        impacts=[(120, 3.0, 0.8, 0.25), (510, 2.0, 0.45, 0), (1230, 3.5, 1.0, 0.35)],
        whooshes=[210, 402, 510, 570, 810, 900, 1080],
        blips=[(240, 1320, 0.14), (255, 1320, 0.14), (262, 988, 0.1), (268, 988, 0.1), (274, 988, 0.1),
               (280, 988, 0.1), (300, 1320, 0.14), (315, 1320, 0.14), (330, 1320, 0.14),
               (345, 784, 0.18), (360, 1320, 0.14), (390, 1320, 0.14), (460, 1568, 0.2),
               (610, 1320, 0.14), (625, 1320, 0.14), (640, 1320, 0.14), (670, 1175, 0.16),
               (710, 1320, 0.14), (750, 1175, 0.16), (775, 1320, 0.14), (798, 1320, 0.14),
               (962, 1568, 0.18), (1006, 1568, 0.18), (1052, 1568, 0.18)],
        typing=None, engine=[(120, 500, 0.12)],
    ),
    "v16": dict(
        fps=30, end=1605, out="out/release16.wav",
        beat=(150, 1455), drops=[(1425, 1455)], claps=675, arp=270,
        risers=[(100, 150, 0.3), (500, 532, 0.2), (1375, 1455, 0.3)],
        impacts=[(60, 2.0, 0.45, 0), (150, 3.0, 0.85, 0.25), (615, 2.0, 0.45, 0), (1455, 3.5, 1.0, 0.35)],
        whooshes=[270, 462, 615, 675, 915, 1005, 1185, 1335],
        blips=[(30, 1200, 0.25), (300, 1320, 0.14), (315, 1320, 0.14), (322, 988, 0.1), (328, 988, 0.1), (334, 988, 0.1), (340, 988, 0.1), (360, 1320, 0.14), (375, 1320, 0.14), (390, 1320, 0.14), (405, 784, 0.18), (420, 1320, 0.14), (450, 1320, 0.14), (520, 1568, 0.2), (715, 1320, 0.14), (730, 1320, 0.14), (745, 1320, 0.14), (775, 1175, 0.16), (815, 1320, 0.14), (855, 1175, 0.16), (880, 1320, 0.14), (903, 1320, 0.14), (1067, 1568, 0.18), (1111, 1568, 0.18), (1157, 1568, 0.18), (1385, 1320, 0.14), (1397, 1320, 0.14), (1409, 1320, 0.14), (1421, 1320, 0.14)],
        typing=None, engine=[],
    ),
}
TL = TIMELINES[sys.argv[1] if len(sys.argv) > 1 else "full"]

FPS = TL["fps"]
TOTAL_FRAMES = TL["end"]
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

def engine(n, start):
    """Engine hum whose pitch follows the speedometer in RacingGame.tsx."""
    t = np.arange(n) / SR
    frame = start + t * FPS
    hz = 62 * (1 + 0.02 * np.sin(frame / 13) + 0.014 * np.sin(frame / 5))
    ph = 2 * np.pi * np.cumsum(hz) / SR
    s = sum(np.sin(k * ph) / k for k in range(1, 7)) + 0.25 * np.sign(np.sin(ph))
    env = np.minimum(1, t / 0.25) * np.minimum(1, (t[-1] - t) / 0.25)
    return lowpass(s, 700) * env


BEAT_FROM, BEAT_TO = TL["beat"]
DROP = TL["drops"]


def dropped(frame):
    return any(a <= frame < b for a, b in DROP)


K = kick()
H = hat()
CL = clap()
for f in range(BEAT_FROM, BEAT_TO, BEAT):
    if dropped(f):
        continue
    add(drums, at(f), K, 0.9)
    add(drums, at(f + BEAT / 2), H, 0.22)
    beat_in_bar = (f // BEAT) % 4
    if f >= TL["claps"] and beat_in_bar in (1, 3):
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
mixer = np.clip((np.arange(N) - at(BEAT_FROM - 30)) / (at(BEAT_FROM + 30) - at(BEAT_FROM - 30)), 0, 1)
music += pad_cut * (1 - mixer) + pad_open * mixer * 0.8

# Bass (8ths) and arpeggio (16ths) with the beat.
eighth = BEAT / 2
f = float(BEAT_FROM)
while f < BEAT_TO:
    if not dropped(f):
        c = chord_at(f)
        add(music, at(f), bass(midi(ROOTS[c]), int(0.22 * SR)), 0.28)
    f += eighth

sixteenth = BEAT / 4
step = 0
f = float(TL["arp"])
while f < BEAT_TO:
    if not dropped(f):
        c = chord_at(f)
        notes = CHORDS[c] + [n + 12 for n in CHORDS[c]]
        add(music, at(f), pluck(midi(notes[step % len(notes)] + 12)), 0.09)
    step += 1
    f += sixteenth

# Hits and transitions.
for a, b, gain in TL["risers"]:
    add(fx, at(a), riser(at(b) - at(a)), gain)
for f, seconds, gain, chime_gain in TL["impacts"]:
    add(fx, at(f), impact(int(seconds * SR)), gain)
    if chime_gain:
        add(music, at(f), chime(), chime_gain)
for f in TL["whooshes"]:
    add(fx, at(f) - int(0.35 * SR), whoosh(), 0.18)

# UI sounds that match what is on screen, and the engine under the race.
for f, hz, gain in TL["blips"]:
    add(fx, at(f), blip(hz), gain)
if TL["typing"]:
    for f in range(TL["typing"][0], TL["typing"][1], 3):
        add(fx, at(f), highpass(rng.standard_normal(int(0.02 * SR)), 3000) * np.exp(-np.arange(int(0.02 * SR)) / SR * 200), 0.08)
for a, b, gain in TL["engine"]:
    add(drums, at(a), engine(at(b) - at(a), a), gain)

# Mix.
mix = drums + reverb(music, 2.8, 0.35) + reverb(fx, 2.2, 0.25)
fade = np.clip((at(TOTAL_FRAMES) - np.arange(N)) / (at(TOTAL_FRAMES) - at(TOTAL_FRAMES - 90)), 0, 1)
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
with wave.open(TL["out"], "wb") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes((stereo * 32767).astype("<i2").tobytes())
print(TL["out"], len(mix) / SR, "s")
