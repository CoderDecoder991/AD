# Weapon and Threat Asset Briefs

**Status:** Recommended (awaiting user review). Each weapon has its own visual identity (brief §18.3); no shared generic turret. Research for behaviour: `WEAPON_SYSTEMS.md` §6–7.

## 1. ZU-23-2 (starting weapon, hero asset)

| Field | Value |
|---|---|
| Identity | Twin 23 mm towed anti-aircraft gun: two long barrels with flash hiders on top, two side ammo boxes, two operator seats, sight post, low tripod carriage on two wheels |
| Reference / provenance | Model: "ZU - 23" by LuckySkink9661, Sketchfab, CC BY 4.0 (M-01). Visual references: public photographs of ZU-23-2 in service (to be collected in `SourceArt/Reference/`, not shipped) |
| Current state | 101k triangles after decimation (from 256k), 30 textures 1024², rigged (base / traverse / elevation / split barrels), recoil per barrel. **Needs improvement**: materials look uniform and dark at night; no grounding detail in the pit |
| Required | Painted olive steel with edge wear and dust; darker gunmetal barrels and mechanisms; rubber tyres; worn seat pads; correct roughness/metal maps (verify import); a contact shadow and ground contact; visible traverse handwheels moving with aim; ammo box feed detail |
| Budget | LOD0 ≤ 120k tris (hero, close camera), LOD1 40k, LOD2 10k; textures 2K for barrels/mount (ASTC 6×6) |
| Animation | Traverse, elevation, alternating barrel recoil (done); handwheel rotation, magazine change (A-02, later) |
| Decision needed | **VS-6:** in-house material pass first. If the result is still not hero quality, buy a higher-quality licensed model or commission one (cost to be approved) |

## 2. C-RAM (selected configuration)

| Field | Value |
|---|---|
| Configuration (decision C-22/C-23) | Land-based Phalanx-type 20 mm six-barrel rotary gun on a trailer/pallet base, white radar dome above the gun, side-mounted electro-optical sensor. Shown as a fictionalised "C-RAM" with no manufacturer names (R-13) |
| Reference / provenance | Model: "Centurion C-RAM" by Muhamad Mirza Arrafi, Sketchfab, CC BY 4.0 (M-02); rigged by `tools/blender/process_cram.py`. Behaviour sources: `WEAPON_SYSTEMS.md` §7 |
| Current state | ~13k tris, rigged (turntable+yoke / radome+gun+drum / spinning barrels). **Needs improvement**: materials simple; no base trailer detail, cables, generator; no LODs |
| Required | Radome off-white composite with panel lines and grime; grey painted mount; dark barrels with heat discolouration; cable trunk to a generator; revetted pad (`ENVIRONMENT_ART_BRIEF.md`) |
| Budget | LOD0 ≤ 40k tris, LOD1 15k, LOD2 4k; 2K textures |
| Animation | Traverse/elevation/barrel spin (done); muzzle smoke cloud after long bursts |

## 3. 2K22 Tunguska

| Field | Value |
|---|---|
| Identity | Tracked chassis; turret with two twin 30 mm guns, eight missile tubes (2×4), rotating search radar on top, tracking radar dish on the turret front |
| Reference / provenance | Model: "2K22 Tunguska" by usedm, Sketchfab, CC BY 4.0 (M-04). Display-only today |
| Current state | **Needs improvement / rig required** for M4: turret, gun elevation, radar rotation, missile tubes |
| Required | Camouflage paint with wear, track and road-wheel detail, rotating radar, missile tube caps |
| Budget | LOD0 ≤ 80k tris, LOD1 25k, LOD2 6k |
| Animation | Radar spin, turret traverse, gun elevation and recoil, missile launch (M4) |

## 4. Threats (all currently greybox primitives — temporary placeholders)

| Threat | Readable silhouette | Night readability | Source (proposal) | Budget |
|---|---|---|---|---|
| Small quadcopter | X-frame, four rotors, small body | Red/green nav lights, rotor shimmer | Licensed store model or CC-BY (to search) | 3k tris |
| Fixed-wing drone | Twin-boom or straight-wing pusher, slim fuselage | Nav lights, engine exhaust glow | Licensed / CC-BY | 5k tris |
| Delta-wing loitering munition (M3b) | Delta wing, rear pusher propeller | Engine glow, distinct buzzing sound | Licensed or in-house (simple shape) | 4k tris |
| Rocket / mortar round (M3b) | Small elongated body, rocket motor flame | Bright motor trail (rocket), predicted-impact marker (mortar) | In-house Blender | 1k tris |
| Cruise missile (M4) | Long body, small wings | Exhaust glow | Licensed / in-house | 5k tris |

All threats are fictional designs or generic types; no real markings or insignia (R-14). Each class needs a distinct destruction effect (`VFX_AND_ANIMATION_SPEC.md` §5).

## 5. Asset pipeline (all models)

Source model (store or Blender) → `tools/blender/process_*.py` (clean, merge, rig, decimate per LOD, ground, export .gltf with separate textures) → Unity import (glTFast; `ModelTexturePostprocessor` ASTC rules) → scene builder places and wires them. Every asset is recorded in `ASSET_REGISTER.md` with licence and status. Items marked "licensed" or "commissioned" need user approval before purchase.
