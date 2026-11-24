# Android Performance Budget

**Status:** Recommended targets; measured results recorded as they become available. No frame-rate or device claim is made without a device measurement (brief §19).

## 1. Devices

| Role | Device | SoC / GPU | RAM | Screen | Status |
|---|---|---|---|---|---|
| Primary test device (High tier) | Xiaomi Pad 8 (25097RP43I) | Snapdragon 8s Gen 4 (SM8735P) / Adreno 825 | 12 GB | 3200×2136, 3:2 | In use |
| Second test device (phone, 19.5:9) | Samsung Galaxy S24 Ultra (SM-S928B) | Snapdragon 8 Gen 3 (SM8650) / Adreno 750 | 12 GB | 3120×1440, 19.5:9 | In use from 2026-11-24 |
| Minimum supported device (Performance tier) | **Open decision P-01** — to be agreed with the user (suggestion: a 2021–2022 mid-range phone, e.g. Snapdragon 7-series / Adreno 6xx, 6 GB) | — | — | — | Not available yet |
| Balanced tier reference | **Open decision P-02** | — | — | — | Not available yet |

## 2. Measured baseline (2026-11-24, commit `3ca592a`, Development build, High tier)

| Measurement | Result | How |
|---|---|---|
| Frame rate, live wave, ZU firing + C-RAM autonomous | 59–60 fps average over 6 samples / ~60 s; worst single frame 25 ms in 3 of 6 samples | In-game overlay (1 s window) |
| Render resolution | 3200×2136 at 0.85 render scale, FSR 1 upscaling, 4× MSAA | Overlay / URP asset |
| Temperature after ~2 min | CPU 54–57 °C, thermal status 0 (none) | `dumpsys thermalservice` |
| Memory (in mission) | Total PSS 469 MB, of which graphics 122 MB, native heap 51 MB | `dumpsys meminfo` |
| APK size | 180 MB (Development build, uncompressed debug data) | Build output |
| Cold start to first frame | 0.9 s (after removing profiler autoconnect) | logcat "Fully drawn" |

### VS-4 investigation (2026-11-24, review and development builds, `tools/measure_fps.sh` on SurfaceFlinger present times)

After VS-2…VS-4 the Pad 8 on High dropped to 40–58 fps (frames alternating one and two refresh intervals). Measured with live dev switches (Pause ▸ DEV, `Core/DevPerfToggles.cs`), after a warm-up:

| Pad 8, High (0.85 scale) | Result |
|---|---|
| 4x MSAA + post + shadows + beams (as shipped in VS-2) | 40–58 fps, ~10–50% frames > 20 ms |
| MSAA 2x | 60 fps, 1–3% frames > 20 ms |
| MSAA off | 60 fps, 0–2% |
| Render scale 0.70 (4x MSAA) | 60 fps |
| Shadows off | no gain (~41 fps) |
| Post-processing off | 60 fps |
| Searchlight beams off | no clear gain (~58 fps) |
| SSAO off (separate build) | no gain |

Conclusion: the cost is resolve/bandwidth (4x MSAA together with post-processing at 0.85 of 3200×2136), not the new geometry, shadows or beams. **High now uses 2x MSAA** (keeps 0.85 scale and post). Release and development builds perform the same; both run slower for ~10–15 s after launch (shader/pipeline warm-up, clock ramp), and long sessions while charging ran slower (warm device) — measure after warm-up and note charging state.

Samsung S24 Ultra, High with 4x MSAA (cool): **60.6 fps, 0 frames > 20 ms, worst 16.9 ms** — comfortable headroom (fewer pixels: 3120×1440). A later run after ~1 h connected and charging measured 40 fps with the phone at 42 °C, thermal status 2 and **Power saving mode on** — not representative; re-test cool with power saving off.

Even with 2x MSAA, the Pad 8 dropped to 40 fps when warm (battery 38 °C, charging): the device lowers GPU clocks. **Dynamic resolution** (`Core/AdaptiveResolution.cs`) now adapts the render scale between the tier's value and a floor (High 0.85 → 0.65, Medium 0.70 → 0.55): warm Pad 8 on High measured **57.7–59.1 fps, 3–6 of 125 frames > 20 ms** (from a steady 40). The step-up wait was then lengthened from 8 s to 20 s to reduce see-sawing (to re-measure).

Not yet measured: 20-minute soak (thermal throttling, battery drain), GPU/CPU frame-time split, draw calls, worst case with 20 threats + both guns + smoke, the Balanced and Performance tiers on real lower-end devices.

## 3. Frame targets

| Tier | Target | Frame budget | Notes |
|---|---|---|---|
| High | 60 fps, ≤ 1% frames over 20 ms | 16.6 ms | Pad 8 class |
| Balanced | 60 fps, or 45 fps locked if 60 is unstable | 16.6 / 22 ms | Decided after device profiling |
| Performance | 30 fps locked, stable pacing | 33.3 ms | Stable 30 is better than unstable 60 (brief §19) |

## 4. Tier settings (current code: `Core/QualityTiers.cs`)

| Setting | Performance (LOW) | Balanced (MEDIUM) | High |
|---|---|---|---|
| Render scale (FSR 1 upscale) | 0.55 | 0.70 (dynamic, ≥ 0.55) | 0.85 (dynamic, ≥ 0.65) |
| MSAA | off | 2× | 2× (4× measured too costly on the Pad 8) |
| Shadows | off (baked only) | 80 m | 100 m, soft |
| SSAO | off | off | off (baked AO since VS-4) |
| Anisotropic filtering | 2× | 4× | 8× |
| Realtime point/spot lights | ≤ 4 near the camera | ≤ 8 | ≤ 16 |
| Particle budget (live particles) | 400 | 800 | 1500 |
| Searchlight beams | shader, no raymarch | shader | shader (+ optional raymarch) |
| Far scatter | impostors only | LOD2 | LOD1–2 |
| Target fps | 30 | 60 | 60 |

## 5. Content budgets (per frame, High tier, to be validated with the profiler)

| Item | Budget |
|---|---|
| Draw calls (after batching) | ≤ 400 |
| Visible triangles | ≤ 1.5 M |
| Hero weapon | ≤ 120k tris LOD0 |
| Texture memory | ≤ 350 MB |
| Total app memory (PSS) | ≤ 900 MB on 8 GB+ devices; ≤ 600 MB on the Performance tier |
| Release APK / AAB | ≤ 150 MB base; larger content via Play Asset Delivery if needed |
| Load time to briefing | ≤ 5 s on the Pad 8 |

## 6. Profiling method

1. **Builds:** Development build with profiler (`AirDefense ▸ Build ▸ Android Development APK (Autoconnect Profiler)`, USB connected) for CPU/GPU timing; non-development review build for user-facing captures.
2. **Tools:** Unity Profiler and Frame Debugger (draw calls, passes); Android GPU Inspector or Snapdragon Profiler for GPU time; `dumpsys meminfo`, `dumpsys thermalservice`, `dumpsys batterystats`.
3. **Scenarios (same every time):** (a) idle base view; (b) Wave 1 firing; (c) dev stress test: +20 threats, both guns firing, smoke and fires active; (d) 20-minute soak on the charger and on battery.
4. **Record** in §2 with date, commit, tier and scenario.

## 7. Risks

| Risk | Mitigation |
|---|---|
| SSAO + MSAA + soft shadows push GPU time past budget in heavy scenes (25 ms frames already seen) | Profile scenario (c); move SSAO to baked AO for static geometry; dynamic resolution on High |
| Thermal throttling in long sessions | 20-minute soak; frame cap and dynamic resolution; "battery saver" option |
| Many realtime lights from base detailing | Bake practical lights; keep realtime lights to the weapon area |
| Particle-heavy effects (C-RAM: 75 rounds/s) | Pooled systems, per-tier caps (already: smoke on every 3rd flash) |
| Texture memory growth with 2K sets | ASTC 6×6, mip streaming, 2K only for close surfaces |
| No lower-end device to test | Agree P-01/P-02 and obtain/borrow devices before tier targets are final |

## 8. Safe fallback

On start, a short benchmark (first 10 s of the briefing) picks the tier; during play, if the average frame time exceeds the budget for 10 s, the game lowers render scale first, then drops SSAO, then shadows, and shows a one-time notice in settings. The player can always override the tier in Pause ▸ Graphics.
