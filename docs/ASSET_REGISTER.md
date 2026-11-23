# Asset Register

Tracks every asset we need, its status and its licence. **Nothing ships without a recorded licence** (C-14).

**Status values:** `Needed` → `Sourcing` → `Placeholder` (clearly labelled, not final) → `Acquired` → `Integrated` → `Final`
**Source values:** `Store` (asset-store pack), `Commissioned`, `In-house`, `Engine` (made in Unity), `Recorded`
**Approach (R-15):** store packs for environment/props/SFX, commissioned or bought models for the weapons, in-engine VFX.

## 1. 3D models

| ID | Asset | Needed by | Source (plan) | Status | Licence / attribution |
|---|---|---|---|---|---|
| M-01 | ZU-23-2 (firing position, animated traverse/elevation/recoil, magazines) | M1 (greybox), M2 (final) | Store: Sketchfab, `SourceArt/Weapons/zu.glb` → `tools/blender/process_zu23.py` → `Assets/_Project/Art/Models/ZU23/ZU23.gltf` | **Integrated (M1).** Modified: 4 star-stencil parts removed (R-14); 508 meshes merged into 3 rig parts (base / traverse / elevation); decimated 256k → 101k tris; muzzle points added. Textures: 30 × 1024², ASTC 6x6 on Android | **CC-BY-4.0.** "ZU - 23, зенитная установка, ЗУ-23, зенитка" by LuckySkink9661, https://sketchfab.com/3d-models/zu-23-23-fdae5a37901941fea3a02578506072a3 |
| M-02 | Fictionalised C-RAM gun mount + radar dome (R-13) | M3 | Store: Sketchfab, `SourceArt/Weapons/c-ram.glb` | **Integrated as display model** (2026-11-23, user asked to show all three weapons): scaled to 4.7 m, `Assets/_Project/Art/Models/CRAM/`. Rigging for M3 | **CC-BY-4.0.** "Centurion C-RAM" by Muhamad Mirza Arrafi, https://sketchfab.com/3d-models/centurion-c-ram-10ea7349c7574ca7a43991eece92c9a9. "Centurion" is a real product name; don't use it in-game (R-13) |
| M-03 | C-RAM search radar mast + command shelter | M3 | Store / Commissioned | Needed | — |
| M-04 | 2K22 Tunguska (turret, guns, missile tubes, radars) | M4 | Store: Sketchfab, `SourceArt/Weapons/tunguska.glb` | **Integrated as display model** (2026-11-23): scaled to 7.93 m, `Assets/_Project/Art/Models/Tunguska/`. Turret split for M4 | **CC-BY-4.0.** "2K22 Tunguska" by usedm, https://sketchfab.com/3d-models/2k22-tunguska-54ff4d655755473d99eab1ccc7eb6bfc |
| M-05 | Missile model (Tunguska) | M4 | Commissioned / In-house | Needed | — |
| M-06 | Small quadcopter drone | M1 | Store | Needed | — |
| M-07 | Fixed-wing propeller drone | M1 | Store | Needed | — |
| M-08 | Delta-wing loitering munition | M3 | Store / In-house | Needed | — |
| M-09 | Rocket, mortar round | M3 | In-house (simple) | Needed | — |
| M-10 | Cruise missile | M4 | Store / In-house | Needed | — |
| M-11 | Strike aircraft (fictional design) | M5 | Store | Needed | — |
| M-12 | Generic soldiers (silhouette-quality) | M2 (cinematic) | Store | Needed | — |

## 2. Environment

| ID | Asset | Needed by | Source | Status | Licence |
|---|---|---|---|---|---|
| E-01 | Military base kit (barracks, fences, sandbags, containers, shelters) | M2 | Poly Haven props (fence, barriers, crates, ammo boxes, jerrycans, barrel, generator) + world-UV boxes for buildings | Integrated (M2). Reduced via `tools/blender/process_props.py` (e.g. barrier 61k→1.5k tris) | **CC0 1.0**, Poly Haven (polyhaven.com), MD5-verified download 2026-11-23; see `SourceArt/PolyHaven/polyhaven_manifest*.json` |
| E-02 | Terrain, rocks, sparse vegetation | M2 | Poly Haven textures: Brown Mud Dry (ground/berm), Concrete Floor Worn 001, Concrete Wall 003, Corrugated Iron | Integrated (textures). Rocks/vegetation: Needed | **CC0 1.0**, Poly Haven (polyhaven.com), MD5-verified download 2026-11-23; see `SourceArt/PolyHaven/polyhaven_manifest*.json` |
| E-03 | Night sky / skybox, moon, clouds | M2 | Poly Haven HDRI "Rogland Clear Night" (2k) + moon directional light | Integrated (M2) | **CC0 1.0**, Poly Haven (polyhaven.com), MD5-verified download 2026-11-23; see `SourceArt/PolyHaven/polyhaven_manifest*.json` |
| E-04 | Lighting props: floodlights, searchlights, beacons | M2 | Poly Haven Flood Lights, Portable Searchlight (scaled ×6 on masts), Security Light | Integrated (M2) | **CC0 1.0**, Poly Haven (polyhaven.com), MD5-verified download 2026-11-23; see `SourceArt/PolyHaven/polyhaven_manifest*.json` |
| E-05 | Radar/command interior (cinematic shot 3) | M2 | Store | Needed | — |
| E-06 | Damage states: fires, rubble, damaged buildings | M2 | Engine (`DamageVisuals`: fire, smoke, flickering light per impact) | Fires/smoke integrated (M2); rubble/damaged meshes: Needed | Project-original |
| E-07 | Day and dusk lighting setups | M5 | Engine | Needed | — |

## 3. Animations

| ID | Asset | Needed by | Source | Status | Licence |
|---|---|---|---|---|---|
| A-01 | Soldier run, operate console, man gun, remove cover | M2 | Store (animation pack) | Needed | — |
| A-02 | ZU-23-2 mechanics (reload, recoil) | M2 | In-house (Timeline/rig) | Needed | — |
| A-03 | Tunguska radar spin/turret | M4 | In-house | Needed | — |

## 4. VFX (mostly in-engine)

| ID | Asset | Needed by | Source | Status | Licence |
|---|---|---|---|---|---|
| V-01 | Muzzle flash (ZU, C-RAM, Tunguska guns) | M1 / M3 / M4 | Engine | Needed | — |
| V-02 | Tracers (GPU-instanced) | M1 | Engine | Needed | — |
| V-03 | Air bursts, drone destruction, debris | M1 | Engine / Store | Needed | — |
| V-04 | Ground impacts, fire, smoke columns | M1 | Engine (particles, generated soft sprite) | Integrated (M2) | Project-original |
| V-05 | Missile launch and smoke trail | M4 | Engine | Needed | — |
| V-06 | Searchlight cones, fog | M2 | Engine (`SearchlightSweep` additive cone; exp² fog) | Integrated (M2) | Project-original |
| V-07 | Brass ejection | M2 | Engine | Needed | — |

## 5. Audio

| ID | Asset | Needed by | Source | Status | Licence |
|---|---|---|---|---|---|
| S-01 | ZU-23-2 fire, reload, handwheels | M1 | **User's own recording** `SourceArt/Audio/User/tunguska_firing.mp3` (2.18 s, ~29.5 shots/s) cut by `tools/audio/make_user_burst.py` into `zu23_first_shot.wav` + seamless `zu23_burst_loop.wav` (pitched x1.05 for the smaller calibre); synthesised `zu23_tail.wav`; reload/empty click from Residue SFX. Older layered rounds kept as fallback | Integrated (M2); handwheels: Needed | User-owned (stated by user 2026-11-23: "my own creation"); Residue SFX CC0 |
| S-02 | C-RAM fire (continuous), spin-up | M3 | Store / designed | Needed | — |
| S-03 | Tunguska guns, missile launch | M4 | Guns: user's own recording, unpitched `tunguska_first_shot.wav` + `tunguska_burst_loop.wav` (ready). Missile launch: Needed | Guns ready; missile: Needed | User-owned (guns) |
| S-04 | Explosions (near, far), impacts | M1 | `airburst_1-2`, `impact_base`, `distant_rumble` (processed by `tools/audio/make_sfx.py`), `boom_residue.ogg` | Integrated (M2) | **CC0**, "Impacts + Guns SFX", opengameart.org/content/impactsgunsfx; **CC0**, "Residue SFX", opengameart.org/content/residue-sfx |
| S-05 | Drone and engine loops per threat | M1+ | Store | Needed | — |
| S-04b | Self-destruct distant pops | M2 | `sd_pop_1-4.wav` synthesised by `tools/audio/make_sfx.py` (`sd_pop`) | Integrated | Project-original |
| S-06 | Air-raid siren, klaxon, radar beeps, warning tones | M1 | Synthesised by `tools/audio/make_sfx.py` | Integrated (M2) | Project-original |
| S-07 | Radio callouts (fictional call signs) + static | M2 | Static: synthesised loop. Callouts: voice needed | Static integrated; callouts: Needed | Project-original (static) |
| S-08 | Night ambience, base machinery | M2 | Store | Needed | — |
| S-09 | Music: tense ambient, combat layer, results | M2 | Licensed / commissioned | Needed | — |
| S-10 | UI sounds | M2 | Store | Needed | — |

## 6. UI

| ID | Asset | Needed by | Source | Status | Licence |
|---|---|---|---|---|---|
| U-01 | Weapon tile icons (3) + state overlays | M1 | In-house | Needed | — |
| U-02 | Radar symbols per threat class | M1 | In-house | Needed | — |
| U-03 | HUD fonts (licensed for apps/games) | M1 | Open-licence font (e.g. SIL OFL) | Needed | — |
| U-04 | Menu backgrounds, app icon, store graphics | M8 | In-house / Commissioned | Needed | — |

## 7. Reference library (not shipped)
Visual references for each weapon system, with URL, date retrieved and usage note ("reference only"). Real-world technical details used in game text must be checked against reliable sources (brief §8).

| Ref ID | Subject | Source URL | Retrieved | Notes |
|---|---|---|---|---|
| — | — | — | — | — |

## 8. CC-BY attribution obligations
CC-BY-4.0 allows commercial use if we credit each model. This needs: the title, the author name with a link, the source link, the licence name with a link, and a note of changes made (e.g. "reduced, re-textured, split for animation"). It goes on the in-game Credits screen (from M2) and in the store listing or a linked credits page. The source information above was read from each file's embedded Sketchfab metadata on 2026-11-23. Re-check each source page before release, and keep a screenshot of the licence.

## 9. Licence rules
- Record the store, product name, version, licence type and purchase date for every pack.
- No assets with unclear origin (e.g. "free" downloads from rip sites).
- No real military unit insignia, national flags or manufacturer logos (R-13, R-14).
- Fonts: confirm embedding in apps is allowed.
- Commissioned work: a written agreement transferring or licensing rights for commercial game use.
