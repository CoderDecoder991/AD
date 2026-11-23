"""Build the C-RAM (20 mm six-barrel rotary gun) firing sound from the user's own recording.

A Gatling at 4,500 rounds/min fires 75 rounds a second: the reports merge into the
characteristic ripping "brrrt" whose pitch IS the fire rate (75 Hz fundamental). So the sound
is built by laying the real recorded shot (user's Tunguska recording, ASSET_REGISTER S-01),
pitched up for the smaller 20 mm round, at exactly the real rate:

  cram_spinup.wav  - first rounds while the barrels spin up (rate climbs to 75/s over the
                     same time as WeaponDefinition.spinUpTime), up to round LOOP_FROM
  cram_loop.wav    - seamless 2.000 s loop = 150 rounds at 75/s (tails wrap around)
  cram_tail.wav    - the reverberant decay heard after the last round

Usage: python -I tools/audio/make_cram_sound.py <tunguska_first_shot.wav> <out_dir>
"""
import os
import sys
import wave

import numpy as np

SR = 44100
RATE = 75.0          # rounds per second at full speed
SPIN_UP = 0.3        # seconds to full speed (matches Weapon_CRAM spinUpTime)
MIN_SPIN = 0.2       # fires from 20% speed (WeaponState.MinSpinToFire)
LOOP_ROUNDS = 150    # 2.0 s loop
PITCH = 1.22         # 20 mm vs 30 mm source: shorter, brighter report
rng = np.random.default_rng(20)


def read(path):
    with wave.open(path) as w:
        return np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32767


def write(path, a):
    pcm = (np.clip(a, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    print(f"  {os.path.basename(path):18s} {len(a) / SR:5.3f}s")


def resample(a, ratio):
    n = int(len(a) / ratio)
    return np.interp(np.linspace(0, len(a) - 1, n), np.arange(len(a)), a).astype(np.float32)


def highpass(a, hz):
    """One-pole high-pass: stops the overlapping low-frequency tails piling up into a rumble."""
    k = np.exp(-2 * np.pi * hz / SR)
    out = np.empty_like(a)
    prev_x = prev_y = 0.0
    for i, x in enumerate(a):
        prev_y = k * (prev_y + x - prev_x)
        prev_x = x
        out[i] = prev_y
    return out


def shot_times_spinup():
    """Round times (s) from trigger press while the rate ramps linearly to 75/s over SPIN_UP."""
    times, t, acc = [], 0.0, 1.0  # first round as soon as the gun reaches MIN_SPIN
    dt = 1.0 / SR
    while t < SPIN_UP:
        spin = t / SPIN_UP
        if spin >= MIN_SPIN:
            acc += RATE * spin * dt
            if acc >= 1.0:
                acc -= 1.0
                times.append(t)
        t += dt
    return times


def lay(shot, times, length, wrap=False):
    out = np.zeros(length, np.float32)
    for t in times:
        i = max(0, int(round((t + rng.normal(0, 0.0004)) * SR)))
        g = rng.uniform(0.85, 1.05)
        seg = shot * g
        end = i + len(seg)
        if end <= length:
            out[i:end] += seg
        else:
            out[i:] += seg[:length - i]
            if wrap:
                rest = seg[length - i:]
                while len(rest):
                    n = min(len(rest), length)
                    out[:n] += rest[:n]
                    rest = rest[n:]
    return out


def main():
    src, out_dir = sys.argv[1], sys.argv[2]
    shot = highpass(resample(read(src), PITCH), 60.0)
    shot = shot[: int(0.9 * SR)]
    shot[-int(0.1 * SR):] *= np.linspace(1, 0, int(0.1 * SR))
    # Feed mechanism rattle under the reports (linkless feed and bolts cycling).
    def rattle(n):
        r = rng.normal(0, 1, n).astype(np.float32)
        r = highpass(r, 1500.0)
        return r * 0.02

    # Loop: 150 rounds over exactly 2 s, tails wrapped so the seam is continuous.
    n = int(LOOP_ROUNDS / RATE * SR)
    loop = lay(shot, [k / RATE for k in range(LOOP_ROUNDS)], n, wrap=True) + rattle(n)
    peak = np.abs(loop).max()
    loop *= 10 ** (-1 / 20) / peak

    # Spin-up: rounds as the rate climbs; ends where the loop takes over.
    times = shot_times_spinup()
    loop_from = len(times) + 1
    t_end = SPIN_UP
    n_su = int((t_end + 0.05) * SR)
    spin = lay(shot, times, n_su)
    spin[-int(0.05 * SR):] *= np.linspace(1, 0, int(0.05 * SR))
    spin *= 10 ** (-1 / 20) / peak  # same gain as the loop so levels match

    # Tail: after the last round, the overlapping report tails decay and the burst rolls back off
    # the surrounding terrain. The recorded shot is dry (0.17 s), so the echo is a simple outdoor
    # reverb: the last 0.4 s of fire convolved with decaying, softened noise (about 1.4 s).
    src_len = int(0.4 * SR)
    burst = loop[-src_len:]
    t = np.arange(int(1.4 * SR)) / SR
    ir = rng.normal(0, 1, len(t)).astype(np.float32) * np.exp(-t / 0.32)
    ir = np.convolve(ir, np.ones(24) / 24, "same")            # duller: high frequencies die in the air
    ir[: int(0.03 * SR)] *= np.linspace(0, 1, int(0.03 * SR))  # first echo arrives after ~30 ms
    m = len(burst) + len(ir)
    wet = np.fft.irfft(np.fft.rfft(burst, m) * np.fft.rfft(ir, m), m)[src_len:].astype(np.float32)
    wet *= 0.35 / max(1e-9, np.abs(wet).max())
    direct = np.zeros_like(wet)
    for k in range(1, 30):
        i = int(round(k / RATE * SR))
        if i < len(shot):
            direct[: len(shot) - i] += shot[i:] * (10 ** (-1 / 20) / peak)
    tail = direct + wet
    fade = min(len(tail), int(0.3 * SR))
    tail[-fade:] *= np.linspace(1, 0, fade)

    write(os.path.join(out_dir, "cram_spinup.wav"), spin)
    write(os.path.join(out_dir, "cram_loop.wav"), loop)
    write(os.path.join(out_dir, "cram_tail.wav"), tail)
    print(f"  spin-up has {len(times)} rounds; loop takes over at round {loop_from}")


if __name__ == "__main__":
    main()
