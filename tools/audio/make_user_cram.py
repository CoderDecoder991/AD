"""Cut the user's C-RAM firing recording and air-raid siren into game clips.

Inputs (SourceArt/Audio/User/, supplied by the user 2026-11-23), decoded to mono 44.1 kHz WAV by ffmpeg:
  cram_firing.mp3   - 9.05 s: ~3.6 s of fire at ~76 rounds/s (a real 4,500 rounds/min gun), then ~5 s of echo
  air_raid_siren.mp3 - 15 s: one siren wail rising, holding and dying away
Outputs (Assets/_Project/Audio/SFX/), replacing the synthesised versions:
  cram_spinup.wav  - the recording's opening (barrels coming up to speed, first rounds)
  cram_loop.wav    - seamless loop of steady fire, a whole number of round periods, crossfaded seam
  cram_tail.wav    - the real echo after the last round
  siren.wav        - the siren, leading silence trimmed, normalised, faded out
Usage: python -I tools/audio/make_user_cram.py <cram.wav> <siren.wav> <out_dir>
"""
import os
import sys
import wave

import numpy as np

SR = 44100


def read(path):
    with wave.open(path) as w:
        return np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32767


def write(path, a):
    pcm = (np.clip(a, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    print(f"  {os.path.basename(path):16s} {len(a) / SR:6.3f}s  peak {np.abs(a).max():.2f}")


def rms(a, win):
    return np.sqrt(np.convolve(a ** 2, np.ones(win) / win, "same"))


def fade(a, t_in=0.0, t_out=0.0):
    a = a.copy()
    if t_in:
        n = int(t_in * SR); a[:n] *= np.linspace(0, 1, n)
    if t_out:
        n = int(t_out * SR); a[-n:] *= np.linspace(1, 0, n)
    return a


def round_period(a):
    """Round-to-round period (samples) from the autocorrelation of the loudness envelope (60-100 rounds/s)."""
    e = np.abs(a) - np.abs(a).mean()
    ac = np.correlate(e, e, "full")[len(e) - 1:]
    lo, hi = SR // 100, SR // 60
    return lo + int(np.argmax(ac[lo:hi]))


def main():
    cram_path, siren_path, out = sys.argv[1:4]
    a = read(cram_path)
    env = rms(a, int(0.01 * SR))
    steady = np.median(env[int(0.6 * SR):int(3.0 * SR)])
    start = int(np.argmax(env > steady * 0.25))                 # first sound
    firing = np.where(env > steady * 0.45)[0]
    end_fire = firing[-1]                                        # last loud round
    period = round_period(a[int(0.6 * SR):int(3.0 * SR)])
    print(f"  fire {start / SR:.3f}-{end_fire / SR:.3f}s, round period {period / SR * 1000:.2f} ms "
          f"({SR / period:.1f} rounds/s)")

    # Loop: steady part, a whole number of periods; nudge the end so its waveform lines up with the start.
    l0 = start + int(0.55 * SR)
    n = int((end_fire - int(0.25 * SR) - l0) / period)
    l1 = l0 + n * period
    ref = a[l0:l0 + period]
    best = max(range(l1 - period // 2, l1 + period // 2),
               key=lambda e: float(np.dot(a[e:e + period], ref)))
    l1 = best
    xf = int(0.015 * SR)
    loop = a[l0:l1].copy()
    loop[-xf:] = loop[-xf:] * np.linspace(1, 0, xf) + a[l0 - xf:l0] * np.linspace(0, 1, xf)

    # Spin-up / opening: from the first sound to where the game's loop takes over (~0.3 s spin-up + a little).
    spin = fade(a[start:start + int(0.36 * SR)], 0.002, 0.05)

    # Tail: the real echo after the last round, until it fades into the noise floor.
    floor = np.where(env > steady * 0.02)[0][-1]
    tail = fade(a[end_fire - int(0.01 * SR):floor], 0.01, 0.4)

    g = 10 ** (-1 / 20) / np.abs(loop).max()   # one gain for all three, so levels match
    write(os.path.join(out, "cram_spinup.wav"), spin * g)
    write(os.path.join(out, "cram_loop.wav"), loop * g)
    write(os.path.join(out, "cram_tail.wav"), tail * g)
    print(f"  loop = {n} rounds; spin-up clip {len(spin) / SR * SR / period:.0f} rounds long")

    s = read(siren_path)
    senv = rms(s, int(0.05 * SR))
    on = np.where(senv > senv.max() * 0.03)[0]
    s = s[max(0, on[0] - int(0.05 * SR)):on[-1]]
    s = fade(s * (10 ** (-1 / 20) / np.abs(s).max()), 0.05, 0.8)
    write(os.path.join(out, "siren.wav"), s)


if __name__ == "__main__":
    main()
