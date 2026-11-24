# Screenshot Visual Audit

**Input:** user gameplay screenshot, 2026-11-24, Wave 1, ZU-23-2 view, Xiaomi Pad 8 (`docs/visual/reference/2026-11-24_user_wave1_prototype.jpg`). Compared with an adb capture of the same build while firing (`docs/visual/reference/2026-11-24_adb_wave1_firing.png`).
**Brief:** `docs/source/Android_Air_Defence_Game_Claude_Handoff_v1.1.md` §17–22.
**Build inspected:** commit `3ca592a` (after the first visual-clarity pass: 85% render scale + FSR, 4x MSAA, soft shadows, SSAO, neutral grade, base detailing).

Labels used: **Observed** = visible in the screenshot or measured on the device. **Confirmed in project** = verified in code or settings. **Likely cause to investigate** = a hypothesis, not yet verified. **Proposed fix** = recommendation, not yet approved.

---

## 1. What the prototype already does well (preserve)

| Strength | Evidence |
|---|---|
| Playable loop: waves, threats, hits, installation health, credits, after-action report | Confirmed in project (`MissionDirector`, `RewardCalculator`, `HudController`). 39 automated tests pass. |
| ZU-23-2 aiming and firing feel (drag aim, hold fire, recoil, real recorded sound, drag ballistics, tracers, self-destruct airbursts) | User approved the gun feel, tracers and airbursts during M1/M2 ("Looks real now", "Sounds right"). |
| Three weapon tiles; locked / unlocked / manual / auto states; C-RAM unlock, switching and autonomous support | Confirmed in project (M3a, `WeaponStation`, `AutonomousWeaponController`). Verified on the Pad 8. |
| Opening cinematic with staged HUD reveal and seamless hand-over | Confirmed in project (`CinematicDirector`). |
| Stable frame rate on the test tablet | **Observed:** 59–60 fps average across six samples over ~1 minute of a live wave with both guns firing; occasional single 25 ms frames. CPU 54–57 °C, thermal status 0 (no throttling). Not yet a 20-minute soak. |
| Correct real-world scale of the weapon models | ZU-23-2, C-RAM (4.7 m) and Tunguska (7.93 m) scaled to real dimensions (`process_zu23.py`, `process_display_weapons.py`). |

---

## 2. Issues from the brief (§17.1), audited against the project

### 2.1 Development diagnostics visible — Severity: **High** (release blocker) — *Code* — **Fixed in VS-1 (2026-11-24)**
- **Observed:** upper right shows `55.5 fps (worst 25.0 ms)`, `3200x2136 scale 0.85`, `Vulkan Adreno (TM) 825`; bottom-right corner shows Unity's "Development Build" watermark.
- **Confirmed in project:** the overlay is created whenever `Debug.isDebugBuild` is true (`Core/RuntimeBootstrap.cs`). All test APKs are Development builds (`Editor/BuildScript.cs`), so Unity also draws the watermark.
- **Proposed fix:** overlay hidden by default in every build; enabled only from a hidden developer gesture (e.g. 4-finger tap) or a dev-menu toggle, positioned so it never overlaps HUD. Add a non-development "review build" option to `BuildScript` (no watermark, no dev buttons) for screenshots and user review; keep Development builds for profiling.

### 2.2 Android system UI visible — Severity: **High** — *Code* — **Fixed in VS-1 (2026-11-24)**
- **Observed:** status bar (time, date, battery) at the top and the gesture handle at the bottom in the user's screenshot.
- **Observed:** adb screen captures of the same build show no system bars. The bars therefore appear only in some situations.
- **Confirmed in project:** `androidStartInFullscreen: 1`, `androidRenderOutsideSafeArea: 1`, entry point GameActivity (`androidApplicationEntry: 2`).
- **Likely cause to investigate:** the bars reappear after a system gesture (e.g. the screenshot gesture, a swipe from the edge, or returning from the recent-apps screen) and the game does not re-apply immersive mode when it regains focus. The radar and pause button also sit under the status bar area, so the safe-area handling must be checked with bars visible.
- **Proposed fix:** re-apply immersive sticky full screen on focus regain (`OnApplicationFocus`), verify display-cutout mode, keep all controls inside `Screen.safeArea` plus a gesture margin at the bottom edge. Test with bars forced visible.

### 2.3 Scene too dark and visually flat — Severity: **High** — *Both*
- **Observed:** the lower half of the frame is the dark inside face of the earth berm; buildings are only a thin strip at the horizon; the sky is about half the frame. The installation is barely readable.
- **Confirmed in project:** the ZU sits in a pit on a 14 m berm with a 4.4 m sandbag ring and berm walls; the third-person camera (C-21, distance 7.5 m, height 2.8 m, 10° down-tilt) looks across the berm rim, so the rim hides the base. Lighting is realtime only (moon directional light + trilight ambient, no baked GI or light probes); berm, pit floor and platform use one dark mud texture.
- **Likely cause to investigate:** no bounce light or baked ambient occlusion, so shadowed areas are uniformly dark; the emplacement design itself (high berm rim) blocks the view of the base.
- **Proposed fix:** (a) redesign the emplacement: lower rim on the threat side, sandbag/HESCO revetment instead of a mud box, a lit pit floor with crew equipment; (b) tune the gameplay camera with the emplacement so the weapon, the rim and the base below are all readable; (c) bake lightmaps and light probes for the static base (indirect light, contact shadows), keep the moon realtime for the weapon; (d) a clear lighting hierarchy (§18.5): moon key, practical lights, beacons, searchlights.

### 2.4 Searchlight beams read as flat translucent strips — Severity: **High** — *Code (shader)*
- **Observed:** two long, evenly lit, sharp-edged grey bands crossing the sky.
- **Confirmed in project:** `SearchlightSweep` draws an open additive cone mesh (18 sides, 220 m long, 9 m radius) with vertex alpha fading linearly from 0.22 to 0. There is no soft edge, no view-angle falloff, no source glow and no interaction with haze. Each beam also has a real spot light.
- **Proposed fix:** a custom beam shader (cheap, no raymarching): falloff across the beam computed from view angle (brightest along the axis, soft edges), inverse-square brightness along the length, faint animated dust noise, soft fade where the beam meets geometry (depth fade), a bright glow sprite plus lens flare at the lamp head. Narrower, shorter beams that fade into the haze; optional low-cost cone raymarch on the High tier only.

### 2.5 Base looks under-dressed and low detail — Severity: **High** — *Asset production + level design*
- **Observed:** a few box buildings with flat roofs; no clear layout or landmark; the distant terrain is a photo backdrop.
- **Confirmed in project:** buildings are textured boxes with added facade props (first visual pass: windows, doors, lamps, AC units, ducts, parapets, cameras); the ground is one flat 5 km plane; the mountains are part of the HDRI sky image (`rogland_clear_night`), so there is no real distant geometry; the area outside the fence has only scattered rocks and bushes.
- **Proposed fix:** see `ENVIRONMENT_ART_BRIEF.md`. A designed site layout with landmarks (radar tower, command bunker, revetted gun positions, vehicle park, fuel point, gate), real modelled terrain with ridges for depth (generated in Blender from a heightfield), hero/mid/far detail levels, and site-protection elements (HESCO lines, T-walls, revetments).

### 2.6 Weapon does not read as a hero asset — Severity: **High** — *Asset production*
- **Observed:** the ZU-23-2 is dark grey and low contrast; the barrels and mount are readable but the materials look uniform.
- **Confirmed in project:** Sketchfab CC-BY model, decimated 256k → 101k triangles, 30 textures at 1024 px; materials come straight from the glTF import; nothing in the pit gives the weapon context (crew, ammo boxes in lit area, scale cues).
- **Likely cause to investigate:** whether the source textures (roughness/metal maps) survive import correctly. The model may need a material pass rather than replacement.
- **Proposed fix:** see `WEAPON_ASSET_BRIEFS.md`: material audit and rework (painted olive steel with edge wear, darker gunmetal barrels, rubber tyres, worn seats), a key light on the weapon (practical work light in the pit), proper contact shadow and ground contact, and a decision on whether to commission or buy a hero-quality model.

### 2.7 Radar presentation too generic — Severity: **Medium** — *Code + UI art*
- **Observed:** three green rings, a north line and one square contact.
- **Confirmed in project:** `RadarView` draws rings, a heading line and coloured square blips; no sweep, no range labels, no threat symbols, no selected-target state.
- **Proposed fix:** see `HUD_UI_SPECIFICATION.md` §4: animated sweep with decaying afterglow, range rings with labels, a weapon arc wedge, class symbols, selected-target bracket, and a link between radar contact and world marker (same ID and colour, highlight on tap).

### 2.8 HUD assembled from unrelated prototype elements — Severity: **High** — *Code + UI art*
- **Observed:** mixed styles: big red FIRE circle, grey tiles, green rings, bright green bars, yellow banner.
- **Confirmed in project:** legacy UGUI `Text` with Unity's built-in `LegacyRuntime.ttf` (Arial-like) font; colours hard-coded per element in `HudController`/`UiFactory`; no shared design tokens.
- **Proposed fix:** a HUD design system (`HUD_UI_SPECIFICATION.md`): one OFL-licensed type family rendered with TextMeshPro SDF, semantic colour tokens, a spacing grid, one panel style, and redesigned components.

### 2.9 Tutorial instruction competes with the battlefield — Severity: **Medium** — *Code*
- **Observed:** "Drag on the left side of the screen to aim" sits across the weapon, in the centre of the screen.
- **Confirmed in project:** the onboarding prompt is a centred text at 32% screen height (`HudController.UpdatePrompts`).
- **Proposed fix:** a compact prompt card near the control it explains (drag hint in the left aim zone with an animated hand icon; fire hint next to FIRE), dismissed by completing the action; never over the weapon or the aim point.

### 2.10 Threat readability — Severity: **High** — *Both*
- **Observed:** a single yellow bracket labelled "1.5 km"; the threat itself is not visible at that range.
- **Confirmed in project:** threats are greybox primitive shapes (capsule/cube) with a blinking red nav light (`ThreatManager.BuildPlaceholderBody`); destruction is a placeholder unlit sphere that swells and shrinks (`ThreatManager.SpawnEffect`).
- **Proposed fix:** real threat models with readable silhouettes (`WEAPON_ASSET_BRIEFS.md` §4), engine/exhaust glow or nav lights for night readability, distinct destruction effects per class (`VFX_AND_ANIMATION_SPEC.md`), and a clearer screen marker that ties to the radar contact.

---

## 3. Further issues found during the project inspection

| # | Observed / confirmed | Severity | Type | Proposed fix |
|---|---|---|---|---|
| 3.1 | Destruction effect is a placeholder sphere (confirmed, `ThreatManager.SpawnEffect`) | High | Code + VFX | Fireball/smoke/debris flipbook effects per threat class |
| 3.2 | Threat bodies are primitive greybox shapes (confirmed) | High | Asset | Licensed or commissioned threat models |
| 3.3 | No real distant terrain: flat 5 km plane + mountains printed in the sky image (confirmed) | High | Asset (Blender) | Modelled terrain and ridges; the sky keeps stars and the moon only |
| 3.4 | No baked lighting or light probes; all lighting realtime (confirmed) | Medium | Code / pipeline | Bake static lighting for the base; probes for moving objects |
| 3.5 | No LOD groups on any model (confirmed: none created by the scene builder) | Medium | Pipeline | LOD0–2 for weapons, buildings, props; impostors for far scatter |
| 3.6 | Legacy UGUI text, not TextMeshPro (confirmed) | Medium | Code | Move HUD text to TextMeshPro SDF |
| 3.7 | Sandbag berm and pit are textured boxes (confirmed) | High | Asset | Modelled revetment: sandbags, HESCO, timber, ammo niche |
| 3.8 | No crew or ambient life (confirmed; cinematic has none either) | Medium | External | Licensed soldier models + animation pack (A-01, M-12) |
| 3.9 | Every lit window/lamp is static; base feels empty | Low | Code | Ambient animation: patrol vehicle, flickering lights, wind on flags/camo nets |

---

## 4. Code-addressable vs asset-production problems

| Can be fixed in project code / shaders (this repo) | Needs asset production (Blender pipeline in-house, or external) |
|---|---|
| Debug overlay, watermark, review-build option (2.1) | ZU-23-2 hero material pass (in-house); full hero remodel (external if required) (2.6) |
| Immersive mode, safe areas, gesture margins (2.2) | Emplacement / revetment models (in-house Blender) (2.3, 3.7) |
| Camera framing and emplacement layout (2.3) | Terrain and ridges (in-house Blender) (3.3) |
| Searchlight beam shader, lamp glow, flares (2.4) | Threat models: quadcopter, fixed-wing drone, delta loitering munition (licensed store or commissioned) (2.10, 3.2) |
| Radar, HUD design system, TextMeshPro, prompts (2.7–2.9) | Explosion and smoke flipbooks (in-house: Blender smoke simulation rendered to texture sheets) (3.1) |
| Lighting hierarchy, baking, probes, LODs (3.4, 3.5) | Soldiers and animations (licensed pack; external) (3.8) |
| Ambient animation systems (3.9) | Radar/command interior for cinematic shot 3 (external or simplified) |

Lighting, grading and resolution changes alone will not make the scene production-grade (brief §22.5); they support the asset and layout work but do not replace it.

---

## 5. Proposed production upgrade plan (for review — not started)

A **visual vertical slice** (brief §21) before M3b, in small reviewable steps, each built and checked on the Pad 8 with before/after captures from the same camera positions:

| Step | Content | Kind | Review output |
|---|---|---|---|
| VS-1 | Player-facing presentation: hide diagnostics, review-build option, immersive mode and safe areas | Code | **Done 2026-11-24** — see §6 |
| VS-2 | HUD design system: font, tokens, TextMeshPro, new radar with sweep, weapon tiles, FIRE control, prompts, threat markers | Code + UI art | **Done 2026-11-24** (3:2 verified on the Pad 8; other aspect ratios pending) — see §6 |
| VS-3 | Emplacement redesign + gameplay camera tuning (ZU pit, revetment, lit work area) | Blender + code | **Done 2026-11-24** — see §6 |
| VS-4 | Lighting: hierarchy, baked lightmaps/probes for the base, searchlight beam shader and lamp glow | Code + pipeline | **Done 2026-11-24** — see §6 |
| VS-5 | Environment: site layout landmarks, modelled terrain and ridges, HESCO/T-walls, LODs | Blender + code | Wide and gameplay captures |
| VS-6 | ZU-23-2 hero material pass and grounding; decide on commissioning a hero model | In-house / decision | Close-up captures |
| VS-7 | One threat model + destruction effects (fireball, smoke, debris) + interception sequence | Asset + VFX | Recorded engagement |
| VS-8 | Performance pass on all three tiers, 20-minute soak, acceptance checklist | Test | `ANDROID_PERFORMANCE_BUDGET.md` results |

Details: `VISUAL_PRODUCTION_STANDARD.md`, `HUD_UI_SPECIFICATION.md`, `ENVIRONMENT_ART_BRIEF.md`, `WEAPON_ASSET_BRIEFS.md`, `VFX_AND_ANIMATION_SPEC.md`, `ANDROID_PERFORMANCE_BUDGET.md`, `VISUAL_ACCEPTANCE_CHECKLIST.md`.

---

## 6. Change log

### VS-1 — Player-facing presentation (2026-11-24)
- Diagnostics readout hidden by default in every build; development builds toggle it with a four-finger tap, F3, or Pause ▸ "DEV: DIAGNOSTICS" (`Core/DiagnosticsOverlay.cs`).
- Review build: `BuildScript.BuildAndroidReview` / `tools/Build-Android.ps1 -Review` — a non-development player with no watermark, no readout and no DEV buttons.
- Immersive full screen re-applied at start, on focus regain, on resume and 0.5 s later (`Core/ImmersiveMode.cs`, Android `WindowInsetsController`, transient bars by swipe).
- HUD keeps a bottom margin (2% of screen height) above the gesture bar.
- Build tool: curl retry messages no longer abort the Gradle dependency seeding (needed for release builds).
- **Verified on the Pad 8 (review build):** no debug text or watermark (`docs/visual/captures/2026-11-24_VS1_review_start.png`); system bars hidden again after pulling down the notification shade and after returning from recent apps (`…_after_shade.png`, `…_after_recents.png`; `dumpsys window`: statusBars and navigationBars `visible=false`).
- Not verified: the four-finger toggle (adb cannot send a four-finger touch); the user's own screenshot gesture (to be checked on the next review).

### VS-2 — HUD design system (2026-11-24)
- Typography: Barlow Semi Condensed (SIL OFL) in four weights, UGUI dynamic font (TextMeshPro deferred, see HUD spec §11).
- Tokens (`UI/UiTheme.cs`): ink, panel, system, info, caution, threat, critical, disabled; one panel style (fill + 1.5 px rounded line).
- Radar: 2.5 s sweep with afterglow, 1/2/3 km labelled rings, cross lines, weapon facing wedge; drone triangles / aircraft chevrons pointing along their track, threat colour, pulsing critical when diving; bracket on the contact nearest the bore; blue ring when an autonomous weapon engages it.
- Weapon tiles: model silhouette icon (`tools/blender/render_weapon_icons.py`), name, live state (MANUAL / AUTO / ENGAGING / RELOAD / OVERHEAT / LOCKED / TAP TO UNLOCK), one value (ammo, reload seconds, cost), thin bar (ammo, reload, credit progress), state accent strip.
- Ammo panel, installation panel, framed banner, icon zoom button, ring-style FIRE button (pressed fill; grey ring + "RELOAD"/"HOT" when it cannot fire).
- Prompts are cards beside their control (aim zone, FIRE, above the ammo panel), never over the weapon or aim point; threat markers use threat/critical colours, max six range labels; amber lead ring.
- Verified on the Pad 8 (review build): `docs/visual/captures/2026-11-24_VS2_*.png`. Tests: 36 EditMode + 3 PlayMode pass.
- Not yet verified: 4:3, 16:9 and 20:9 layouts (checklist P3), pause/after-action screens in detail.

### VS-3 — ZU-23-2 emplacement and camera framing (2026-11-24)
- Box berm replaced by a round earth mound with sloped sides (top r 10.5 m, foot r 20 m, 14 m high) and a gravel pit floor; dried-mud texture at real scale.
- Sandbag revetment: 486 individual bags (bevelled, pinched ends, sag, per-bag UV offsets) in a staggered double ring — 4 courses (~0.55 m) across the threat side, 5–7 courses (~0.95 m) on the flanks, rear access gap under the camera. 52k triangles.
- Concrete gun pad; work lights on the rear stands raised to lamp height and brightened (11) so the weapon and pit are lit by two layers (moon + practical).
- Camera: distance 7.8 m, height 3.2 m, view tilt 13° (from 7.5 / 2.8 / 10°): horizon higher, the base visible below it, aim point ~28% from the top.
- Items overlapped by the bigger mound moved: road start, barrier ring (r 21.5 m), south barriers, checkpoint barriers, a street lamp, the gate guard.
- Verified on the Pad 8 (review build): `docs/visual/captures/2026-11-24_VS3_zu_front.png`; camera sweep north/east/south/low elevation shows no clipping (`…_VS3_camera_sweep.png`).
- Still open for this area: crew ammo niches and a hooded work light model (props), contact-shadow quality under the gun (VS-4 lighting), the gun's own materials (VS-6).

### VS-4 — Lighting and searchlights (2026-11-24)
- Searchlights: `AirDefense/LightBeam` shader (soft edges from view angle, falloff along the beam, drifting dust, fog-free) on a cone with proper normals/UVs; lamp-head glow that flares when the beam points at the camera. The flat translucent strips are gone (checklist E4).
- Baked lighting (`Editor/LightBake.cs`, run after the scene builder): static/GI flags, small and far objects lit by probes, moon Mixed (baked indirect, realtime direct and shadows), fixed lamps Mixed (realtime pools + baked bounce), 579 light probes, baked AO. CPU bake ≈ 0.8 min, one 2048 lightmap.
- Lit windows use an unlit glow material (the bake re-validation stripped the Lit shader's emission keyword).
- Performance investigation and fixes (details in `ANDROID_PERFORMANCE_BUDGET.md` §2): High tier 2x MSAA (4x + post was the cost), SSAO off (baked AO), shadows 100 m, scatter casts no shadows, sandbags receive only, distant guards cast no shadows, **dynamic resolution**. Dev-build performance switches (`Core/DevPerfToggles.cs`) and `tools/measure_fps.sh` added for future measurement.
- Verified on the Pad 8 (review build): `docs/visual/captures/2026-11-24_VS4_*.png`; second device Samsung S24 Ultra (19.5:9 HUD layout: `…_S24_zu_20x9.png`).
- Open: 20-minute soak with the 20 s step-up wait; re-test the S24 cool with power saving off.
