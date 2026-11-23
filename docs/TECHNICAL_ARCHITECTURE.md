# Technical Architecture

> **Status: Recommended, awaiting approval (R-01, O-05).** No Unity project is created until the engine choice is approved.

## 1. Engine and rendering

| Item | Recommendation |
|---|---|
| Engine | Unity 6 LTS (latest 6000.x LTS at project creation) |
| Render pipeline | Universal Render Pipeline (URP), forward rendering |
| Graphics API | Vulkan primary, OpenGL ES 3.x fallback |
| Scripting backend | IL2CPP, ARM64 only, .NET Standard 2.1 |
| Input | Unity Input System (touch + gyro) |
| Cameras | Cinemachine |
| Cinematic | Timeline + Cinemachine, using gameplay assets |
| UI | uGUI for the in-game HUD (mature, predictable on mobile). UI Toolkit may be used for menus if it proves easier. |
| Audio | Unity Audio + Audio Mixer (snapshots for "alert" and "pause" states) |
| Content loading | Addressables from M5, when content grows |
| Testing | Unity Test Framework (EditMode + PlayMode) |

### Engine trade-offs (plain language)
- **Unity 6 (recommended):** The most common engine for 3D Android games. Many ready-made military assets in its store. Good ad/billing plugins. It runs acceptably on your PC. Visual quality is slightly lower than Unreal's out of the box, but good enough for realistic mobile games with careful lighting and assets.
- **Unreal Engine 5:** The best visual quality, but very heavy for both your PC (GT 730) and mid-range Android. Builds are slower and the mobile workflow is more complex.
- **Godot 4:** Free and light, but its mobile 3D quality, asset ecosystem and ad/billing plugins are less mature. More risk for a realistic, monetised game.

### Licensing note
Unity Personal is free up to a revenue/funding threshold. Check the current Unity terms at project creation and before release.

## 2. Development environment
- **Editor PC:** Windows 11, GT 730 (C-16). Editor preview uses low quality. Real performance is measured on the device.
- **Project location:** the C: SSD (e.g. `C:\dev\AirDefense`), not the H: HDD (R-17). The documentation can stay here or move with the project.
- **Device:** Xiaomi Pad 8 over USB (ADB). Unity Profiler and Frame Debugger connect to the device. Android Logcat package for logs.
- **Version control:** Git + Git LFS (`.gitattributes` for textures, models, audio, video). `.gitignore` excludes `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `UserSettings/` and keystores.
- **Secrets:** the release keystore and any SDK keys stay out of source control (C: user folder plus a backup). Build settings read them from environment variables or a local untracked file.

## 3. System overview

```
                    ┌────────────────────┐
                    │   GameBootstrap    │  loads settings, save, quality tier
                    └─────────┬──────────┘
                              ▼
┌──────────────┐    ┌────────────────────┐    ┌──────────────────┐
│ SaveService  │◄──►│  SessionManager    │◄──►│ ProgressionSvc   │ credits, unlocks, upgrades
└──────────────┘    │ (menu ↔ mission)   │    └──────────────────┘
                    └─────────┬──────────┘
                              ▼
                    ┌────────────────────┐
                    │  MissionDirector   │  waves, objectives, end conditions
                    └──┬──────────────┬──┘
                       ▼              ▼
            ┌───────────────┐   ┌─────────────────┐
            │ ThreatSpawner │   │ InstallationHP  │
            │ + ThreatAgents│   └─────────────────┘
            └───────┬───────┘
                    ▼
            ┌───────────────┐   contact list   ┌──────────────────┐
            │ SensorSystem  │─────────────────►│  RadarView (UI)  │
            │ (radar model) │                  └──────────────────┘
            └───────┬───────┘
                    ▼
     ┌─────────────────────────────┐
     │   WeaponManager             │  owns 3 WeaponInstances, tracks which is manual
     │  ┌────────────┐ ┌─────────┐ │
     │  │ Manual     │ │ Auto    │ │  "controller" swapped on switch;
     │  │ Controller │ │ Contr.  │ │  same WeaponInstance underneath
     │  └────────────┘ └─────────┘ │
     └──────────────┬──────────────┘
                    ▼
     ┌─────────────────────────────┐
     │ FireControl / Ballistics    │  lead solution, dispersion
     │ ProjectilePool / Missiles   │  pooled, simple physics
     └──────────────┬──────────────┘
                    ▼
            ┌───────────────┐
            │ DamageSystem  │──► MissionStats ──► AfterActionReport
            └───────────────┘
```

### Key design ideas
- **One weapon, two "brains".** Each `WeaponInstance` holds state (ammo, heat, reload, health). A controller drives it: `ManualWeaponController` (player input) or `AutoWeaponController` (target selection + fire logic). Switching weapons swaps controllers, so the rules and limits are identical whether the player or AI is in charge, apart from the profile modifiers.
- **Data-driven everything (C-09).** `WeaponDefinition`, `ThreatDefinition`, `MissionDefinition`, `WaveDefinition`, `EconomyConfig` and `QualityTierConfig` are ScriptableObjects. Balance changes don't need code changes.
- **Event bus** for decoupled feedback: `ThreatDetected`, `ThreatDestroyed`, `ThreatImpacted`, `WeaponStateChanged`, `WeaponSwitched`, `MissionEnded`. UI, audio and analytics subscribe; gameplay doesn't know they exist.
- **Service interfaces** for future SDKs: `IAdService`, `IPurchaseService`, `IAnalyticsService`. Stub/offline versions until providers are approved (C-12).
- **Deterministic waves.** Missions use a seed, so Daily Challenge (M6) is the same scenario for every player.

## 4. Simulation approach
- Threats move along authored splines or simple steering paths with class-specific behaviour (weave, dive, ballistic arc). No full flight physics.
- Gun projectiles: pooled, ballistic with gravity and a short lifetime. Hit detection via raycast per frame segment (cheap, reliable at high speed).
- Missiles: simple proportional-navigation homing with a turn-rate limit and proximity fuse.
- Radar: a contact list updated at a fixed rate (e.g. 4 Hz), with detection range per threat class and simple low-altitude masking. Not a full radar simulation.

## 5. Save model
- JSON file in `Application.persistentDataPath`, written atomically (write a temp file, then replace).
- Contents: save version, credits, unlocked weapons, upgrade levels, settings, mission progress, stats, cinematic-seen flag, purchase entitlements cache (Play remains the source of truth).
- `saveVersion` with migration steps for future changes.
- Basic tamper resistance (checksum). Offline single-player, so no heavy anti-cheat.

## 6. Performance budgets (High tier, Xiaomi Pad 8)
| Budget | Target |
|---|---|
| Frame rate | 60 fps cap, ≥ 55 fps sustained after 20 min |
| Frame time (CPU / GPU) | ≤ 14 ms each |
| Render scale | ~0.7 of native (3200×2136) |
| Draw calls (SRP batches) | ≤ 250 in a busy wave |
| Triangles on screen | ≤ 1.0 M |
| Memory (total app) | ≤ 1.5 GB (budget against the 8 GB model, O-09) |
| Cold start to menu | ≤ 8 s |
| Menu to gameplay | ≤ 6 s |

Medium/Low tiers: lower render scale (0.6/0.5), fewer particles, baked lighting only, shorter shadow distance, 30 fps on Low.

## 7. Quality tier system
- First launch: detect the tier from GPU family and memory, then run a short benchmark during the cinematic's first seconds.
- Player override in settings.
- Each tier is a `QualityTierConfig` (URP asset, render scale, shadow settings, particle density, post-processing, LOD bias).

## 8. Proposed folder structure (created in M0, after approval)

```
AirDefense/                       (Unity project root, on C: SSD)
├─ Assets/
│  ├─ _Project/                   our own content (kept apart from store packs)
│  │  ├─ Art/                     Models/, Materials/, Textures/, VFX/
│  │  ├─ Audio/                   SFX/, Radio/, Music/, Mixers/
│  │  ├─ Data/                    Weapons/, Threats/, Missions/, Economy/, Quality/
│  │  ├─ Prefabs/                 Weapons/, Threats/, Environment/, UI/
│  │  ├─ Scenes/                  Boot, Menu, Base_Night_01, Cinematic_Opening
│  │  ├─ Scripts/                 Core/, Weapons/, Threats/, Sensors/, Missions/,
│  │  │                           Progression/, UI/, Audio/, Services/
│  │  ├─ Timeline/                cinematic assets
│  │  └─ UI/                      sprites, fonts, layouts
│  ├─ ThirdParty/                 imported store assets (one folder per pack)
│  └─ Tests/                      EditMode/, PlayMode/
├─ Packages/
├─ ProjectSettings/
└─ docs/                          these documents
```

Folders are created only when there's content for them.

## 9. Build pipeline
- Development builds: installed to the Pad 8 via Build And Run (USB).
- Release builds: AAB, signed with an upload key kept outside the repo, Play App Signing enabled.
- A build script (`Editor/BuildScript.cs`) for repeatable builds and later CI.
- Version: `major.minor.patch` plus an auto-incremented version code.

## 10. Technical risks
| Risk | Impact | Mitigation |
|---|---|---|
| The weak editor GPU makes content work slow | Slower art/VFX iteration | Low editor quality, test on device, GPU upgrade optional |
| Pad 8 hides performance problems on cheaper devices | Bad reviews on mid-range phones | Tiers from day one, a Low-tier device before release |
| Heat throttling in long sessions | Frame drops | 60 fps cap, render scale, 20-minute soak tests |
| Many small projectiles and particles | CPU/GPU spikes | Pooling, GPU-instanced tracers, particle budgets per tier |
| Asset style mismatch between store packs | Breaks realism | Pick packs early, consistent material/lighting pass, art bible |
| SDK policy changes | Release delays | Service interfaces; SDKs added late (M7) after fresh policy checks |
