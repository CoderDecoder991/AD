# AirDefense (working title)

A realistic 3D air-defence command game for Android. You start on a ZU-23-2, earn credits to unlock a C-RAM battery and the 2K22 Tunguska, and switch between them while the unselected systems defend autonomously.

## Status

**Milestone 1 — ZU-23-2 greybox prototype, built and running on the Pad 8; awaiting your playtest.** One mission (3 waves, small drones + fixed-wing drones), touch aiming and firing, heat and reload, radar, target brackets and edge arrows, a lead ring (aim assist), installation health, and an after-action report. 60 fps with 20+ threats. 14 automated tests pass. All art is labelled placeholder greybox. (M0 is done, apart from a Profiler connection check.)

## Working with the project
- Open `C:\dev\AirDefense` in Unity Hub (Add → Add project from disk).
- **AirDefense ▸ Setup ▸ Apply Android Settings**: reapplies the M0 Android settings (IL2CPP, ARM64, minSdk 26, Vulkan + GLES3, landscape, 0.7 render scale).
- **Recommended build:** `.\tools\Build-Android.ps1 -Install` builds the dev APK, retrying automatically through interrupted dependency downloads, then installs and launches it on the connected device.
- **AirDefense ▸ Build ▸ Android Development APK + Run on Device**: the same build from the editor menu, without the download retries.
- **Xiaomi devices:** Developer options → turn on **Install via USB**, keep the tablet unlocked, and tap **Install** on the prompt.
- **AirDefense ▸ Setup ▸ Build M1 Greybox Scene**: regenerates `Scenes/M1_Greybox.unity` and placeholder materials. Data assets in `Assets/_Project/Data/` (weapon, threats, mission) are created only if missing, so your tuning is kept.
- Scripts: `Assets/_Project/Scripts/` (`Core`, `Data`, `Weapons`, `Threats`, `Missions`, `UI`, `Editor`). Tests: `Assets/_Project/Tests/` (EditMode + PlayMode). Run them from Window ▸ General ▸ Test Runner.
- Weapon models supplied by the user go in `SourceArt/Weapons/` (outside `Assets/` until inspected; see `docs/ASSET_REGISTER.md`).
- **ZU-23-2 model pipeline** (needs Blender 5.x): `tools/blender/process_zu23.py` merges, reduces and rigs the source model and exports `Assets/_Project/Art/Models/ZU23/ZU23.gltf`. Unity imports it with glTFast, and `ModelTexturePostprocessor` sets ASTC compression and normal/linear maps. Then rerun **Build M1 Greybox Scene**. `preview_operator_view.py` renders candidate first-person camera positions.
- Third-party credits: [CREDITS.md](CREDITS.md).

| Item | State |
|---|---|
| Engine | Unity 6 LTS + URP — *Confirmed (C-17)* |
| Location | `C:\dev\AirDefense` (SSD) — *Confirmed (C-18)* |
| Dev PC | Windows 11, i5-6400, 16 GB, GT 730 |
| Test device | Xiaomi Pad 8 — *Confirmed* |
| Version control | Git + Git LFS initialised, branch `main`, no commits yet |

## Documentation

| Document | Contents |
|---|---|
| [DECISIONS_AND_ASSUMPTIONS.md](docs/DECISIONS_AND_ASSUMPTIONS.md) | Confirmed / Recommended / Open decisions. **Start here.** |
| [GDD.md](docs/GDD.md) | Game design: loop, weapons, threats, modes, scoring |
| [PRD.md](docs/PRD.md) | Product requirements, goals, platform, release criteria |
| [WEAPON_SYSTEMS.md](docs/WEAPON_SYSTEMS.md) | Identity, behaviour and balance fields for all three weapons |
| [ECONOMY_AND_MONETISATION.md](docs/ECONOMY_AND_MONETISATION.md) | Credits, pacing, ads and IAP design |
| [UI_UX_FLOW.md](docs/UI_UX_FLOW.md) | Screen flow, HUD layout, controls |
| [TECHNICAL_ARCHITECTURE.md](docs/TECHNICAL_ARCHITECTURE.md) | Engine trade-offs, systems, save model, budgets, folder layout |
| [CINEMATIC_OPENING.md](docs/CINEMATIC_OPENING.md) | Shot list, audio cues, transition, fallback |
| [ASSET_REGISTER.md](docs/ASSET_REGISTER.md) | Every required asset, with status and licence |
| [MILESTONES_AND_TEST_PLAN.md](docs/MILESTONES_AND_TEST_PLAN.md) | M0–M8, acceptance criteria, device and performance tests |
| [source/](docs/source/) | The original handoff brief |

## Ground rules
- All weapon and threat numbers are fictional game-balance values.
- Placeholder art is always labelled as such.
- No real-money requirement for core progression.
- No secrets, keystores or API keys in source control.
