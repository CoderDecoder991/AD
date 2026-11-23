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


def highpass(a, cutoff_hz):
    return a - lowpass(a, cutoff_hz)


def delay_mix(a, delay_s, gain, lp_hz=None):
    """Adds a delayed (optionally low-passed) copy: a simple early reflection."""
    d = int(delay_s * SR)
    out = np.concatenate([a, np.zeros(d, dtype=np.float32)])
    echo = lowpass(a, lp_hz) if lp_hz else a
    out[d:] += echo * gain
    return out


def zu_round(crack_src, rng, length=0.42):
    """One 23 mm round: crack + boom + mechanism clank + early reflections.

    - crack: the first 30 ms of a recorded shot, high-passed (supersonic report)
    - boom:  falling sine thump (~130 -> 45 Hz) plus low-passed noise (muzzle blast body)
    - clank: short damped metallic partials (breech/feed cycling), a few ms late
    - early reflections off nearby structures (45 ms, 110 ms), duller each time
    """
    n = int(length * SR)
    tt = np.arange(n) / SR
    # Crack (recorded, CC0): onset-aligned, high-passed, very short.
    start = int(np.argmax(np.abs(crack_src) > 0.2 * np.abs(crack_src).max()))
    crack = crack_src[start:start + int(0.03 * SR)].copy()
    crack = highpass(crack, 900 + rng.uniform(-150, 150))
    crack *= np.exp(-np.arange(len(crack)) / SR / 0.008)
    crack = np.pad(crack, (0, n - len(crack)))
    # Boom: pitch-falling thump plus rumbling body.
    f0, f1 = rng.uniform(120, 145), rng.uniform(40, 50)
    f = f1 + (f0 - f1) * np.exp(-tt / 0.025)
    ph = 2 * np.pi * np.cumsum(f) / SR
    boom = np.sin(ph) * np.exp(-tt / rng.uniform(0.075, 0.095))
    noise = rng.normal(0, 1, n).astype(np.float32)
    body = lowpass(lowpass(lowpass(noise, 380), 380), 380) * np.exp(-tt / 0.06) * 9.0  # steep 18 dB/oct low-pass
    # Clank: metallic partials from the mechanism.
    clank = np.zeros(n, dtype=np.float32)
    off = int(rng.uniform(0.004, 0.007) * SR)
    for fr, amp, dec in ((1180, 0.12, 0.022), (2360, 0.08, 0.016), (3710, 0.05, 0.012), (5020, 0.03, 0.009)):
        fr *= rng.uniform(0.96, 1.04)
        seg = np.sin(2 * np.pi * fr * tt[: n - off]) * np.exp(-tt[: n - off] / dec) * amp
        clank[off:] += seg.astype(np.float32)
    a = (crack * 0.5 + boom * 1.5 + body * 1.0 + clank * 0.7).astype(np.float32)
    a = np.tanh(a * 1.15) / np.tanh(1.15)  # light saturation, like a close mic on a loud gun
    a = delay_mix(a, 0.045, 0.32, lp_hz=2500)
    a = delay_mix(a, 0.11, 0.18, lp_hz=1200)[: n]
    a[-int(0.06 * SR):] *= np.linspace(1, 0, int(0.06 * SR))
    return normalise(a.astype(np.float32), -1.0)


def zu_tail(rng, length=2.6):
    """Rolling echo heard after a burst ends: low rumble with scattered distant reflections."""
    n = int(length * SR)
    tt = np.arange(n) / SR
    rumble = lowpass(rng.normal(0, 1, n).astype(np.float32), 260) * np.exp(-tt / 0.55)
    for _ in range(9):  # discrete reflections off terrain and buildings, getting duller and quieter
        d = rng.uniform(0.05, 1.4)
        i = int(d * SR)
        seg_t = tt[: n - i]
        thump = np.sin(2 * np.pi * rng.uniform(45, 70) * seg_t) * np.exp(-seg_t / 0.12) * np.exp(-d / 0.6) * 0.8
        rumble[i:] += thump.astype(np.float32)
    rumble = lowpass(rumble, 700)
    rumble[: int(0.02 * SR)] *= np.linspace(0, 1, int(0.02 * SR))
    rumble[-int(0.5 * SR):] *= np.linspace(1, 0, int(0.5 * SR))
    return normalise(rumble.astype(np.float32), -4.0)


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

    print("ZU-23-2 autocannon (layered: recorded crack + synthesised boom/clank/echo):")
    crack_src = imp("Shotgun 1")
    for v in range(4):
        r = np.random.default_rng(100 + v)
        write_wav(os.path.join(out, f"zu23_round_{v + 1}.wav"), zu_round(crack_src, r))
    write_wav(os.path.join(out, "zu23_tail.wav"), zu_tail(np.random.default_rng(7)))

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
