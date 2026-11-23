"""Build game-ready SFX for AirDefense.

1. Processes selected CC0 recordings (OpenGameArt "Impacts + Guns SFX", see
   docs/ASSET_REGISTER.md): mono, pitch-shifted, trimmed, faded, normalised.
2. Synthesises project-original sounds (siren, radio static, radar ping,
   warning tone). These are made from scratch here, so they carry no third-party licence.

Usage: python -I tools/audio/make_sfx.py <source_dir> <out_dir>
  source_dir = SourceArt/Audio/OpenGameArt   out_dir = Assets/_Project/Audio/SFX
"""
import os
import sys
import wave

import numpy as np

SR = 44100


def read_wav(path):
    with wave.open(path) as w:
        n, ch, sw, sr = w.getnframes(), w.getnchannels(), w.getsampwidth(), w.getframerate()
        raw = w.readframes(n)
    dt = {1: np.int8, 2: np.int16, 4: np.int32}[sw]
    a = np.frombuffer(raw, dtype=dt).astype(np.float32) / float(np.iinfo(dt).max)
    if ch > 1:
        a = a.reshape(-1, ch).mean(axis=1)
    if sr != SR:
        a = resample(a, SR / sr)
    return a


def resample(a, factor):
    """Changes length by `factor` (linear interpolation)."""
    n = max(1, int(len(a) * factor))
    return np.interp(np.linspace(0, len(a) - 1, n), np.arange(len(a)), a).astype(np.float32)


def pitch(a, ratio):
    """ratio < 1 lowers pitch (and lengthens), like slowing a tape."""
    return resample(a, 1.0 / ratio)


def trim(a, seconds, fade_out=0.05, start_threshold=0.02):
    idx = np.argmax(np.abs(a) > start_threshold * np.abs(a).max())
    a = a[idx: idx + int(seconds * SR)].copy()
    f = min(len(a), int(fade_out * SR))
    if f > 0:
        a[-f:] *= np.linspace(1, 0, f)
    a[: min(len(a), 32)] *= np.linspace(0, 1, min(len(a), 32))  # de-click
    return a


def lowpass(a, cutoff_hz):
    k = np.exp(-2 * np.pi * cutoff_hz / SR)
    out = np.empty_like(a)
    acc = 0.0
    for i, x in enumerate(a):
        acc = (1 - k) * x + k * acc
        out[i] = acc
    return out


def normalise(a, peak_db=-1.0):
    m = np.abs(a).max()
    return a if m < 1e-9 else a * (10 ** (peak_db / 20) / m)


def write_wav(path, a):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(a, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"  {os.path.basename(path):28s} {len(a) / SR:5.2f}s")


def t(seconds):
    return np.arange(int(seconds * SR)) / SR


def main():
    src, out = sys.argv[1], sys.argv[2]
    imp = lambda name: read_wav(os.path.join(src, "impacts", "Impacts", name + ".wav"))  # noqa: E731
    rng = np.random.default_rng(23)

    print("Processed recordings (CC0):")
    # ZU-23-2 shot variants: heavy, short, with a little tail for overlap at 15 rounds/s per barrel.
    for i, (name, ratio) in enumerate((("Shotgun 1", 0.72), ("Impact Rash 1", 0.70), ("Pistol 2", 0.66)), 1):
        a = trim(pitch(imp(name), ratio), 0.55, fade_out=0.25)
        write_wav(os.path.join(out, f"zu23_shot_{i}.wav"), normalise(a, -1.5))
    # Air bursts (threat destroyed) and the hit on the base.
    for i, (name, ratio) in enumerate((("Impact 6", 0.55), ("Impact 8", 0.6)), 1):
        a = trim(pitch(imp(name), ratio), 1.8, fade_out=0.8)
        write_wav(os.path.join(out, f"airburst_{i}.wav"), normalise(lowpass(a, 3500), -2))
    a = trim(pitch(imp("Impact Rash 2"), 0.5), 2.5, fade_out=1.2)
    write_wav(os.path.join(out, "impact_base.wav"), normalise(lowpass(a, 2500), -1))
    a = trim(pitch(imp("Flash Recive"), 0.6), 3.5, fade_out=1.8)
    write_wav(os.path.join(out, "distant_rumble.wav"), normalise(lowpass(a, 900), -3))

    print("Synthesised (project-original):")
    # Air-raid siren: slow rise to a wail, held, with motor-like waver.
    tt = t(7.0)
    env_f = np.interp(tt, [0, 2.5, 5.0, 7.0], [180, 620, 640, 300])
    f = env_f * (1 + 0.006 * np.sin(2 * np.pi * 5.5 * tt))
    ph = 2 * np.pi * np.cumsum(f) / SR
    siren = 0.6 * np.sin(ph) + 0.25 * np.sin(2 * ph) + 0.1 * np.sin(3 * ph)
    siren *= np.interp(tt, [0, 0.4, 6.0, 7.0], [0, 1, 1, 0])
    write_wav(os.path.join(out, "siren.wav"), normalise(lowpass(siren.astype(np.float32), 4000), -3))

    # Radio static loop (2 s, seamless: crossfaded ends).
    n = int(2.0 * SR)
    noise = rng.normal(0, 1, n + 4410).astype(np.float32)
    bp = lowpass(noise, 3000) - lowpass(noise, 300)
    crackle = (rng.random(n + 4410) > 0.9985) * rng.normal(0, 4, n + 4410)
    s = (bp + crackle.astype(np.float32))
    s = s[:n]
    head, tail = s[:4410].copy(), (bp + crackle.astype(np.float32))[n:n + 4410]
    s[:4410] = head * np.linspace(0, 1, 4410) + tail * np.linspace(1, 0, 4410)
    write_wav(os.path.join(out, "radio_static_loop.wav"), normalise(s, -12))

    # Radar ping and warning tone.
    tt = t(0.18)
    ping = np.sin(2 * np.pi * 1350 * tt) * np.exp(-tt * 22)
    write_wav(os.path.join(out, "radar_ping.wav"), normalise(ping.astype(np.float32), -8))
    tt = t(0.9)
    tone = np.sign(np.sin(2 * np.pi * 880 * tt)) * 0.5 + np.sin(2 * np.pi * 880 * tt) * 0.5
    gate = (np.sin(2 * np.pi * 4 * tt) > 0).astype(np.float32)
    write_wav(os.path.join(out, "warning_tone.wav"), normalise(lowpass((tone * gate).astype(np.float32), 3000), -6))


if __name__ == "__main__":
    main()
