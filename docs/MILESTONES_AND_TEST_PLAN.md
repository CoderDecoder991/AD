# Milestones and Test Plan

Each milestone ends with a playable build on the Xiaomi Pad 8 and a user review before the next one starts. Decision IDs refer to [DECISIONS_AND_ASSUMPTIONS.md](DECISIONS_AND_ASSUMPTIONS.md).

## 1. Milestone sequence

```
M0 Setup ─► M1 ZU greybox ─► M2 Vertical slice (MVP) ─► M3 C-RAM + switching
                                                          │
                     M5 Content & modes ◄── M4 Tunguska ◄─┘
                            │
                            ▼
                 M6 Upgrades & economy ─► M7 Monetisation ─► M8 Polish & release
```

| # | Milestone | Goal | Depends on |
|---|---|---|---|
| M0 | Project setup | A buildable empty project running on the Pad 8 | Engine approval (O-05) |
| M1 | ZU-23-2 greybox prototype | Prove aiming and shooting feel good on touch | M0 |
| M2 | Vertical slice (MVP) | One polished base, the full ZU loop, credits, locked tiles, cinematic animatic | M1 + art packs chosen |
| M3 | C-RAM + switching + autonomy | The core three-system mechanic, with two systems | M2, O-06 |
| M4 | 2K22 Tunguska | All three systems working together | M3 |
| M5 | Content and modes | More missions, Survival, Multi-Threat, more threats, multi-asset base | M4 |
| M6 | Upgrades, economy, Daily Challenge | Balanced long-term progression | M5 |
| M7 | Monetisation | Rewarded ads, interstitials, IAP, Remove Ads | M6, O-07, O-08, O-04 |
| M8 | Polish and release prep | Full cinematic, optimisation, accessibility, device testing, store listing | M7 |

## 2. Milestone details and acceptance criteria

### M0 — Project setup
- Unity 6 LTS project created on the C: SSD with URP, Input System and Cinemachine.
- Git + LFS, `.gitignore`, `.gitattributes`.
- Android build settings (R-10): IL2CPP, ARM64, Vulkan + GLES3, landscape.
- An empty test scene with an FPS counter, built and run on the Pad 8.
- **Accept:** the APK installs and runs on the Pad 8 at 60 fps. The Profiler connects over USB. The repo excludes `Library/` and secrets.
- **Result (2026-11-23):**
  - ✅ Dev APK (38.2 MB) installed and ran on the Pad 8: **60.0 fps, worst frame 16.7 ms**, Vulkan, Adreno 825, 3200×2136 at render scale 0.70 (400 lit, shadowed, spinning cubes).
  - ✅ `.gitignore` excludes `Library/`, builds and keystores.
  - ⏳ Profiler-over-USB connection not yet tried (build has `ConnectWithProfiler`; check from the Unity editor GUI).
  - ⏳ 20-minute soak test not yet run (required from M2, not M0).
  - Note: the log shows a harmless `ClassNotFoundException` for Play Asset Delivery's `AssetPackManager`. That's expected while Play Asset Delivery isn't used.
  - Note: this PC's connection corrupts long Java/Gradle TLS downloads (`bad_record_mac`). `tools/Build-Android.ps1` handles it with Gradle retries, automatic build reruns and a resumable, checksum-verified local seed repository (`.gradle-seed/`, git-ignored).

### M1 — ZU-23-2 greybox prototype
- Greybox base (simple shapes, labelled placeholder). Placeholder ZU-23-2 with working traverse, elevation, firing, heat, reload.
- Touch controls (R-06) with sensitivity settings.
- Small drone + fixed-wing drone with basic paths.
- Radar panel, installation health, simple end screen.
- Data assets for weapon, threats and one mission.
- **Accept:**
  - 5 test players (or the user, repeatedly) can hit moving drones within 1 minute of starting, without instructions beyond the prompts.
  - Aiming feels responsive: input-to-camera latency is not noticeable, with no jitter.
  - A full mission plays from start to success or failure.
  - 60 fps on the Pad 8 with 20 active threats and continuous firing.
  - All placeholder art is labelled.
- **Result (2026-11-23), build 0.0.1 dev:**
  - ✅ Full mission plays start → briefing → 3 waves → success/failure → after-action report → play again (failure path verified on device).
  - ✅ 60.0 fps, worst frame 16.7 ms on the Pad 8 with ~22 active threats and continuous fire (DEV stress button in the pause menu).
  - ✅ Placeholder art labelled on screen ("PLACEHOLDER GREYBOX ART - M1 PROTOTYPE") and in object names.
  - ✅ Automated tests: 11 EditMode (ballistics, lead, hit detection, dispersion, ammo/heat/reload) + 3 PlayMode (lead-aimed fire destroys a moving threat; unengaged threat impacts; wide miss does no damage).
  - ⏳ **Needs human playtest:** aiming feel and "hit a drone within 1 minute". Automated adb input can aim and fire but can't track moving targets precisely.
  - Fixed during M1: runtime `GameObject.CreatePrimitive` failed in builds because collider classes were stripped. Runtime code now builds mesh-only objects via `PrototypeVisuals.Part`.
  - Known greybox issues for M2: edge arrows can overlap the top-right pause button and the dev FPS overlay; distant drones are only a few pixels wide (brackets and zoom compensate); daylight lighting (night comes with real art).

### M2 — Vertical slice (MVP)
- One night base with realistic art (store packs + ZU-23-2 model), lighting, VFX, audio.
- Onboarding prompts. Pause and settings. Quality tiers (Low/Medium/High).
- Credits, save/load, three weapon tiles (C-RAM/Tunguska locked with cost).
- After-action report with tips.
- Cinematic storyboard and in-engine animatic (R-16), seamless into Mission 1, skippable.
- **Accept:** everything in brief §12. A 20-minute soak test at ≥ 55 fps on the Pad 8. Save survives app kill and restart. Cinematic acceptance criteria in [CINEMATIC_OPENING.md](CINEMATIC_OPENING.md) met (animatic quality).

### M3 — C-RAM, switching, autonomy
- C-RAM battery model and behaviour, the unlock flow.
- Weapon switching with camera transitions. The autonomous controller for both weapons.
- Rockets, mortars and loitering munitions. Predicted impact markers.
- Support priority setting prototype (R-18).
- **Accept:** switching ≤ 0.4 s, and the radar stays visible. Autonomous engagements are visible on the radar and tiles. In playtests, players use switching on purpose. Autonomous systems alone cannot win a standard mission (C-08).

### M4 — 2K22 Tunguska
- Model, gun and missile modes, radar lock, autonomous missile logic, cruise missiles.
- **Accept:** all three systems work together. Missile lock is understandable without explanation after a guided first use. 60 fps with all three firing.

### M5 — Content and modes
- 10+ missions, Survival mode, Multi-Threat mode, strike aircraft, protectable assets, dusk/day lighting.
- **Accept:** a difficulty curve with no sudden spikes (failure rate per mission within the target band). Each new threat is introduced alone first.

### M6 — Upgrades, economy, Daily Challenge
- Upgrade system, economy tuning, Daily Challenge (seeded).
- **Accept:** simulated and playtest pacing matches the targets (C-RAM in ~2–3 h, Tunguska in ~6–8 h). No upgrade makes a mission trivial.

### M7 — Monetisation
- Provider decision (O-07) after current policy and SDK checks. Privacy policy, consent, Data safety form.
- Rewarded ads, frequency-capped interstitials, IAP, Remove Ads, Restore purchases.
- **Accept:** no ad during engagements; all caps enforced; purchases tested with Play test accounts; refund/restore paths work; the game is fully playable offline with ads unavailable.

### M8 — Polish and release prep
- Full-quality cinematic, audio mix pass, accessibility settings, localisation readiness.
- Device matrix testing, crash reporting (if approved), store listing, content rating, closed testing track.
- **Accept:** release criteria in the PRD.

## 3. Test plan

### 3.1 Automated tests (Unity Test Framework)
| Area | Examples |
|---|---|
| EditMode | Economy calculations, unlock rules, save serialisation and migration, wave definition validation, lead-solution maths |
| PlayMode | A weapon fires and a projectile hits a static target; autonomous controller respects eligibility and range; switching swaps controllers correctly; mission end conditions |
| Data validation | Every `WeaponDefinition`/`ThreatDefinition` has required fields, sane ranges, no missing prefabs |

### 3.2 Manual playtest checklist (each milestone)
- First-time experience from a clean install (cinematic → Mission 1 → results).
- Each control setting (sensitivity, invert, aim assist, gyro, left-handed).
- Pause/resume, app backgrounding mid-mission, incoming notification, screen rotation lock.
- Save after kill/restart, low storage behaviour.

### 3.3 Device testing
| Device | Tier | When |
|---|---|---|
| Xiaomi Pad 8 (user's) | High | Every milestone |
| A cheaper phone (O-10) or cloud device service | Medium / Low | From M2, required before M8 |
| A long-aspect phone (20:9+) | UI layout | From M2 (Device Simulator in the editor until then) |

### 3.4 Performance checks (every milestone, on device)
| Check | Method | Target (High tier) |
|---|---|---|
| Frame rate | Profiler / on-screen counter | 60 fps, ≥ 55 sustained |
| Soak / thermal | 20 min continuous play | No drop below 55 fps on the Pad 8 |
| Memory | Memory Profiler | ≤ 1.5 GB |
| Load times | Stopwatch / log timestamps | Cold start ≤ 8 s, mission load ≤ 6 s |
| Battery | 20 min session, battery % drop | Logged and tracked (target set in M2) |
| Build size | AAB size | Within Play limits; tracked per milestone |

### 3.5 Analytics events (planned, enabled only after O-08)
`session_start`, `cinematic_complete/skip`, `tutorial_step`, `mission_start`, `mission_end` (result, duration, grade), `weapon_unlock`, `weapon_switch`, `upgrade_purchase`, `rewarded_ad_offer/accept`, `interstitial_shown`, `iap_purchase`, `fps_sample`, `crash`.
