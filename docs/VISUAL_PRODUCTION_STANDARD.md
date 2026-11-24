# Visual Production Standard

**Status:** Recommended (awaiting user review). Adapts brief v1.1 §17–19 to this project: Unity 6000.6.10f1, URP 17, Android (Vulkan, IL2CPP ARM64), test device Xiaomi Pad 8 (Snapdragon 8s Gen 4 / Adreno 825, 12 GB, 3200×2136, 3:2).

## 1. Art direction

**Grounded, photoreal military simulation at night, built for a phone/tablet screen.** The reference feel is a modern military shooter's night mission seen through a weapon operator's eyes: believable scale, worn materials, practical lighting, haze, and light-emitting effects (tracers, flashes, beams) as the brightest things in frame.

Pillars, in priority order:
1. **Readability first.** Threats, the aim point, the weapon and the installation must be readable on a 6–11" screen at normal brightness. Darkness sets the mood; it never hides gameplay information or missing detail.
2. **Credible, not exaggerated.** Effects, colours and motion follow real references (`WEAPON_SYSTEMS.md` §6–7). No neon, no sci-fi glow, no arcade explosions.
3. **One coherent place.** A designed installation with a layout, landmarks and purpose, not scattered objects.
4. **Performance is part of quality.** Stable frame pacing on the agreed tiers beats peak detail (§19 of the brief, `ANDROID_PERFORMANCE_BUDGET.md`).

Not wanted: cartoon or tower-defence look, saturated UI colours, flat translucent light shapes, uniform grey materials, permanent tutorial text, visible debug data.

## 2. Colour and value

| Element | Value / colour rule |
|---|---|
| Sky | Deep blue-black, stars visible; brightest near the horizon haze. Never pure black. |
| Moonlit surfaces | Desaturated, slightly cool, mid-dark values (no crushed blacks: dark areas keep texture detail). |
| Practical lights (lamps, windows) | Warm (2700–3500 K look), local pools of light; the main source of colour contrast. |
| Searchlights | Neutral-cool white beams, visible only through haze, soft-edged. |
| Weapon effects | Hottest values in the frame: tracers yellow-white core / red-orange edge; muzzle flash white-yellow core, orange lobes. |
| Warning lights | Red rotating beacons, short pulses only. |
| UI | Separate restrained palette (`HUD_UI_SPECIFICATION.md` §2); never competes with tracers or threat markers. |

## 3. Materials (URP Lit, physically based)

| Material | Base colour (sRGB) | Smoothness | Metallic | Notes |
|---|---|---|---|---|
| Painted military steel (olive/green) | 0.20–0.32 | 0.25–0.4 | 0 (paint) | Edge wear to bare steel; dust in recesses |
| Bare / gunmetal steel (barrels, mechanisms) | 0.25–0.35 | 0.35–0.55 | 1 | Darker, sharper reflections than the paint |
| Rubber (tyres, seals) | 0.05–0.08 | 0.15–0.25 | 0 | |
| Concrete | 0.35–0.55 | 0.1–0.2 | 0 | Texture variation + decals (stains, cracks) to break tiling |
| Compacted soil / gravel | 0.25–0.45 | 0.05–0.15 | 0 | Two-layer blend to avoid visible tiling |
| Sandbags / fabric | 0.35–0.5 | 0.05–0.1 | 0 | Normal map for weave and bag shape |
| Glass (windows) | dark 0.03–0.06 | 0.85–0.95 | 0 | Emissive when lit from inside |

Rules: no single flat material on a hero asset; large surfaces get variation (texture blend, decals, vertex colour) so tiling is not visible; check every material under the night lighting on the device, not only in the editor. Textures: 2K for close surfaces and hero assets, 1K for props, 512 for far scatter; ASTC 6×6 (4×4 for hero normal maps if needed).

## 4. Lighting hierarchy (night)

| Layer | Source | Implementation |
|---|---|---|
| 1. Ambient | Sky + moon bounce | Baked lightmaps (indirect light and AO) for static geometry; light probes for moving objects; trilight ambient as the fallback on the Performance tier |
| 2. Key | Moon (directional) | Realtime, soft shadows on High, 2 cascades; shadows required near the weapon |
| 3. Practicals | Lamps over doors, street lamps, windows, pit work light | Baked where static; a small number of realtime lights near the gun only |
| 4. Accents | Red beacons, searchlights | Realtime, animated; searchlight beam shader (`VFX_AND_ANIMATION_SPEC.md` §8) |
| 5. Effects lights | Muzzle flash, explosions | Short realtime point lights, no shadows |

The weapon, its emplacement and the aiming area must always be lit by at least two layers (moon + a practical work light).

## 5. Atmosphere and post-processing

- Exponential-squared fog for depth. Light-emitting effects ignore fog (existing `AdditiveGlowNoFog`, `AlphaSmokeNoFog` shaders).
- ACES tonemapping, neutral colour grade (slight cool shadows only), contrast +10–15.
- Bloom threshold ≈ 0.95: only real light sources bloom.
- Vignette ≤ 0.2. No film grain, no chromatic aberration, no extra sharpening beyond FSR's.
- Exposure tuned so the HUD remains readable on top of the darkest and brightest areas.

## 6. Detail density

| Band | Distance from gameplay camera | Treatment |
|---|---|---|
| Hero | 0–15 m (selected weapon, emplacement) | Highest-detail meshes and 2K materials, realtime shadows, contact AO |
| Near-mid | 15–80 m (base buildings, roads, other weapon positions) | Detailed facades and props, baked lighting, LOD0–1 |
| Far-mid | 80–400 m (fence, outside terrain, scatter) | Simple meshes, LOD2, no realtime shadows |
| Far | 400 m – horizon | Terrain silhouettes, ridges, haze; sky keeps only stars and moon |

Detail is added only where it gives scale, purpose or composition (brief §18.2), not random clutter.

## 7. Camera and composition

- Gameplay camera: third-person behind and above the selected weapon (decision C-21, user-confirmed). Tuning rule: the weapon fills roughly the lower third, the aim point sits in the upper half, and the base is visible below the horizon. The emplacement is designed together with the camera (lower rim on the threat side).
- Recoil feedback: small and fast; no shake during routine aiming (existing setting `Screen shake` stays).
- No camera clipping through the weapon, the berm or buildings, in every aiming direction (to be checked with a camera sweep test).
- Supported aspect ratios: 4:3, 3:2 (Pad 8), 16:9, 19.5–20:9. Composition checked on each.

## 8. Quality tiers

Three tiers (brief §19). Names in the game: **Performance / Balanced / High** (currently LOW / MEDIUM / HIGH in code). Exact settings and measured results: `ANDROID_PERFORMANCE_BUDGET.md`. Lower tiers drop cost (shadows, SSAO, render scale, particle counts, beam raymarching) but keep the same information: threats, markers, tracers and the weapon stay equally readable.

## 9. Provenance and labelling

Every asset is recorded in `ASSET_REGISTER.md` with source, licence and production status (§10 there). Placeholder assets are labelled as placeholders in the register and are never shown to the user as final quality.
