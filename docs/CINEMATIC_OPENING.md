# Cinematic Opening

**Length:** ~40 s (30–45 s range, C-10). **Setting:** fictional air-defence installation at night (R-14).
**Plan:** storyboard + in-engine animatic in M2 (R-16); full-quality pass in M8.
**Technology:** Unity Timeline + Cinemachine in the gameplay base scene, using reusable gameplay assets.

## 1. Shot list

| # | Time | Shot | Camera | Visual | Audio |
|---|---|---|---|---|---|
| 1 | 0:00–0:05 | Black / warning | — | Black. A faint red pulse at 0:03. A subtle title-safe logo is optional. | Wind, crickets. Distant radio through static ("…contacts bearing…"). A warning tone at 0:03, the siren starts rising at 0:04. |
| 2 | 0:05–0:12 | Installation reveal | Slow high crane/drone push-in over the perimeter fence | The base at night: sodium lights, searchlights sweeping, rotating amber beacons, low fog. A distant flash on the horizon at 0:10. | The siren at full volume, then ducked under the next shot's sound. Low rumble at 0:10 (about 2 s delay after the flash). |
| 3 | 0:12–0:20 | Radar warning | Interior, close on the radar screen, then rack-focus to the operator's silhouette | The radar sweep shows 3 → 6 contacts. Warning lamps turn red. Minimal on-screen text: "UNKNOWN CONTACTS ×6". | Radar beeps speed up. Short radio: "All stations, air raid warning red. Man your positions." |
| 4 | 0:20–0:29 | Base to alert | 3 quick cuts: a door bursting open (exterior), feet running past (low), a generator/console powering up (insert) | Red lighting, figures moving at a distance or in silhouette (low animation cost), equipment start-up. A brief cut to the ZU-23-2 crew pulling the cover off. | Boots, doors, mechanical start-up, shouted orders (indistinct). |
| 5 | 0:29–0:38 | Weapon reveal | Slow dolly around the ZU-23-2 from low angle, ending behind the operator seat | Barrels elevating, magazine loaded with a clack, the operator's hands on the handwheels. A first contact is visible as a faint light in the sky. | The siren drops under. Weapon mechanics in close detail. Radio: "Gun one, contact north-east, weapons free." |
| 6 | 0:38–0:45 | Handover | The camera blends into the gameplay camera (Cinemachine blend, ~1.5 s) | The HUD appears in stages: radar (0:39), threat warning (0:40), weapon status (0:41), ZU-23-2 tile (0:42). Prompt: "Drag to aim". | Ambience and siren continue into gameplay without a break. Gameplay music layer starts. |

## 2. Transition rules
- The cinematic plays **in the gameplay scene**, so there's no load between it and Mission 1.
- The first threat wave is spawned (but frozen) during Shot 5, so it's already in the sky when control starts.
- "Skip" (tap → hold-to-confirm button, top-right, appears after 1 s) jumps to the Shot 6 blend: a short 1 s blend, then the HUD appears.
- After the first viewing, the cinematic doesn't autoplay again. It's available from Main menu → "Replay opening".

## 3. Characters
To keep cost realistic, people appear in silhouette, at a distance, or in partial close-up (hands, boots). This needs only a few generic animated soldiers (run, operate console, man gun) from a licensed animation pack. No dialogue lip-sync.

## 4. Low-end fallback
| Tier | Change |
|---|---|
| High | Full effects, fog, volumetric-style light cones (faked with meshes), full particles |
| Medium | Simplified fog, fewer searchlights, reduced particles |
| Low | Shots 2 and 4 shortened, no light cones, baked lighting only. Option: a pre-rendered 720p video for Shots 1–5, then the live Shot 6 blend |

## 5. Audio cue list
| Cue | Source type | Notes |
|---|---|---|
| Night ambience | Licensed SFX | Wind, insects |
| Radio static + callouts | Recorded voice actor or licensed voice pack, processed | Short, professional, fictional call signs |
| Warning tone | Designed SFX | Distinct from in-game threat alert |
| Air-raid siren | Licensed SFX | Mixable, with a ducking snapshot |
| Distant explosion/rumble | Licensed SFX | Delayed after the flash |
| Base activity | Licensed SFX | Doors, boots, generator |
| ZU-23-2 mechanics | Licensed or recorded SFX | Magazine clack, handwheels |
| Music | Licensed or commissioned | Low, tense pulse, enters at Shot 3 |

## 6. Acceptance criteria
- Runs between 30 and 45 s.
- Skippable from 1 s. Skip lands in gameplay within 1.5 s.
- No loading screen between the cinematic and gameplay.
- Holds the tier's frame-rate target on the Pad 8.
- Replay is available from the menu after first viewing.
- Radio lines are subtitled.
