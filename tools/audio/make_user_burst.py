"""Cut the user's own Tunguska firing recording into game clips.

Input: SourceArt/Audio/User/tunguska_firing.mp3 (user-recorded, 2026-11-23), decoded to WAV by ffmpeg.
Output (Assets/_Project/Audio/SFX/):
  zu23_first_shot.wav  - the isolated opening shot (plays on trigger press)
  zu23_burst_loop.wav  - seamless loop of the sustained burst, cut on shot onsets
  tunguska_burst_loop.wav / tunguska_first_shot.wav - unpitched originals for M4
Usage: python -I tools/audio/make_user_burst.py <decoded.wav> <out_dir>
"""
import os
import sys
import wave

import numpy as np

SR = 44100


def read(path):
    with wave.open(path) as w:
        a = np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32767
    return a


def write(path, a):
    pcm = (np.clip(a, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    print(f"  {os.path.basename(path):26s} {len(a) / SR:5.3f}s")


def onsets(a):
    h = int(0.002 * SR)
    win = int(0.005 * SR)
    env = np.sqrt(np.convolve(a ** 2, np.ones(win) / win, "same"))
    d = np.diff(env[::h])
    thr = np.percentile(d, 99) * 0.5
    out, last = [], -1e9
    for i, v in enumerate(d):
        t = i * h / SR
        if v > thr and t - last > 0.025:
            out.append(int(t * SR))
            last = t
    return out


def resample(a, ratio):
    """ratio > 1 raises pitch (and shortens)."""
    n = int(len(a) / ratio)
    return np.interp(np.linspace(0, len(a) - 1, n), np.arange(len(a)), a).astype(np.float32)


def normalise(a, db=-1.0):
    return a * (10 ** (db / 20) / max(1e-9, np.abs(a).max()))


def main():
    src, out = sys.argv[1], sys.argv[2]
    a = read(src)
    on = onsets(a)
    pre = int(0.003 * SR)  # start each cut just before the shot's attack

    # First shot: from the first onset up to just before the second.
    first = a[max(0, on[0] - pre): on[1] - pre].copy()
    first[-int(0.03 * SR):] *= np.linspace(1, 0, int(0.03 * SR))

    # Loop: skip the opening shot and the first few burst shots (spin-up), end on a later onset.
    start = on[3] - pre
    end = max(o for o in on if o < len(a) - int(0.05 * SR)) - pre
    xf = int(0.012 * SR)
    loop = a[start:end].copy()
    # Crossfade the loop's end into the audio just before its start, so the seam is inaudible.
    loop[-xf:] = loop[-xf:] * np.linspace(1, 0, xf) + a[start - xf:start] * np.linspace(0, 1, xf)

    write(os.path.join(out, "tunguska_first_shot.wav"), normalise(first))
    write(os.path.join(out, "tunguska_burst_loop.wav"), normalise(loop))
    # ZU-23-2: smaller calibre, slightly higher; 1.05 also nudges 29.5 -> ~31 shots/s, close to the 30/s fire rate.
    write(os.path.join(out, "zu23_first_shot.wav"), normalise(resample(first, 1.05)))
    write(os.path.join(out, "zu23_burst_loop.wav"), normalise(resample(loop, 1.05)))
    print(f"  loop spans shots {on.index(on[3])}..{on.index(end + pre)} of {len(on)}")


if __name__ == "__main__":
    main()
