# VFX and Animation Specification

**Status:** Recommended (awaiting user review). Effects must have a clear start, duration and end, be readable against both the dark sky and the terrain, and stay within the particle budgets in `ANDROID_PERFORMANCE_BUDGET.md`.

| # | Effect | Current state (inspected) | Target | Status |
|---|---|---|---|---|
| 1 | Weapon traverse / elevation | Done for ZU and C-RAM (`GunMount`, speed-limited) | Add handwheel rotation (ZU), radar spin (Tunguska) | Existing — usable |
| 2 | Recoil | ZU per-barrel recoil, camera kick (`GunRecoil`) | Keep | Existing — usable |
| 3 | Muzzle flash + smoke | Particle fireball sheet + light + smoke (`MuzzleFlashFX`); C-RAM continuous flame | Add a short side-vent flash for the ZU flash hider; lingering barrel smoke after long bursts | Needs improvement (minor) |
| 4 | Tracers | Camera-exposure streaks, per-weapon width/length, burnout, self-destruct airbursts (user-approved) | Keep; consider GPU-instanced draw for cost | Existing — usable |
| 5 | Threat destruction | **Placeholder:** unlit orange sphere swells and fades (`ThreatManager.SpawnEffect`) | Per class: drone = small fireball + black smoke + falling burning debris + sparks; fixed-wing = larger fireball, burning wreck falling with smoke trail; rocket/mortar = sharp airburst flash + grey puff. Flipbook textures rendered in Blender (smoke/fire simulation) | New asset required |
| 6 | Ground impact on the base | Particles: fire, smoke column, flickering light (`DamageVisuals`) | Add a blast flash, dust ring, debris and scorch decal; persistent fire with a lifetime cap | Needs improvement |
| 7 | Near miss / hit feedback | Hits are silent except destruction | Small spark flash on hit; bracket flashes on the marker | New (code) |
| 8 | Searchlights | **Flat-looking** additive cone mesh (`SearchlightSweep`) | Beam shader: view-angle soft edges, length falloff, dust noise, depth fade, lamp glow sprite and lens flare; real spot light kept | New (shader) |
| 9 | Radar sweep | None (static rings) | UI sweep with afterglow (`HUD_UI_SPECIFICATION.md` §4); 3D radar antenna rotation on the tower | New (code) |
| 10 | Beacons | Rotating red pulse light + bulb (`AlarmBeacon`) | Add a lens glow sprite; keep | Existing — usable |
| 11 | Environment motion | Searchlight sweep, beacons | Flags/camo nets wind (vertex shader), patrol vehicle, lamp flicker | New |
| 12 | Brass / link ejection | None | ZU: brass and links ejected per round (pooled meshes, short life) | New (later) |
| 13 | Missile launch / trail (M4) | None | Launch flash, booster smoke trail, sustainer glow | New (M4) |

## Rules

- Every effect is pooled; no runtime instantiation during combat.
- Lifetimes are capped (smoke ≤ 8 s, fires ≤ 60 s or until mission end) and the number of simultaneous large effects is limited per tier.
- No "firework" sparks for airbursts (user feedback); no sci-fi glows; no oversized glowing lines.
- Light-emitting effects use the fog-free additive shader; smoke uses the fog-free alpha shader only for sky effects.
- Each effect is checked in three situations: against the night sky, against lit buildings, and at 1.5 km and 3 km range.
