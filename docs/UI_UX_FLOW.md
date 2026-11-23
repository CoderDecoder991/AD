# UI / UX Flow

Landscape only. Designed for both the Xiaomi Pad 8 (3:2) and phones (up to ~21:9). Decision IDs refer to [DECISIONS_AND_ASSUMPTIONS.md](DECISIONS_AND_ASSUMPTIONS.md).

## 1. Screen flow

```
App start
  │
  ├─ First launch ─► Opening cinematic (skippable) ─► Mission 1 (gameplay, seamless)
  │
  └─ Returning ─► Main menu
                    ├─ Play ─► Mode select ─► Mission select ─► Briefing ─► Gameplay
                    ├─ Armoury (unlocks, upgrades, cosmetics)
                    ├─ Records (stats, personal bests)
                    ├─ Settings
                    └─ Replay opening cinematic

Gameplay ─► Pause ─► Resume / Settings / Quit
Gameplay ─► Mission end ─► After-action report ─► Rewards (rewarded-ad offer from M7)
          ─► Armoury or Next mission
```

## 2. Gameplay HUD layout (landscape)

```
┌──────────────────────────────────────────────────────────────────┐
│ [Radar]                         THREAT WARNING            [Pause]│
│ (top-left,                     (top-centre, brief)               │
│  ~22% height)                                                    │
│                                                                  │
│                      BATTLEFIELD VIEW                            │
│                 (aim area = right ~60%)                          │
│                                                                  │
│ [Base health]                                    [Mode]  (Tung.) │
│                                                                  │
│ [ZU-23-2][C-RAM][TUNGUSKA]   Ammo/heat            ( FIRE )       │
│  (bottom-left weapon tiles)  (bottom-centre)     (bottom-right)  │
└──────────────────────────────────────────────────────────────────┘
```

**Principles**
- The battlefield is the focus. Persistent HUD covers ≤ 20% of the screen.
- Left thumb: weapon tiles. Right thumb: aiming and firing. Nothing important under the thumbs' rest positions.
- The radar is always visible, even during weapon-switch transitions.
- On phones (20:9+), the extra width goes to the battlefield. HUD elements anchor to the safe-area corners.

## 3. Weapon tiles

| State | Visual |
|---|---|
| Selected | Bright border, slightly larger, "MANUAL" label |
| Ready (autonomous) | Normal, "AUTO" label |
| Engaging | Pulsing edge plus a small target icon |
| Reloading | Radial progress fill |
| Overheated | Orange fill draining |
| Damaged/Unavailable | Desaturated, warning icon |
| Locked | Padlock plus credit cost, e.g. "🔒 4,000 cr" |

- Tapping a locked tile during play shows the unlock requirement without leaving the mission.
- Tapping an unlocked tile switches instantly (camera transition ≤ 0.4 s).
- Each tile shows a compact ammo bar so autonomous status is readable without switching.

## 4. Radar panel
- Top-down, centred on the base, with range rings.
- Contacts use class symbols (shape-coded, colour as secondary).
- Lines from each autonomous weapon to its current target.
- Tap a contact to designate it (C-RAM manual, Tunguska missile lock). On the ZU-23-2 this rotates the camera toward the contact (assist setting).
- An optional enlarge-radar gesture (hold) for a tactical overview (R-03).

## 5. Controls (R-06)
| Action | Default control |
|---|---|
| Aim | Drag on the right ~60% of the screen |
| Fire | Hold the large FIRE button (bottom-right) |
| Switch weapon | Tap a weapon tile |
| Reload | Automatic when empty; tap the ammo bar to reload early |
| Designate / lock | Tap a radar contact or an on-screen marker |
| Tunguska mode | Toggle button above FIRE |
| Pause | Top-right button; Android back gesture also pauses |

**Settings:** sensitivity (X/Y), invert Y, aim assist (Off / Light / Strong), gyro aim (Off/On), left-handed layout (mirrored), button size.

## 6. Onboarding (first mission)
Short prompts that appear in context:
1. "Drag to aim" when the first contact appears.
2. "Hold FIRE" when it's in range.
3. "Aim ahead of moving targets" after the first misses.
4. "Watch the radar" when a contact appears off-screen.
5. "Reload" when the magazine is low.

Each prompt disappears once its action is done and is never repeated.

## 7. After-action report
- Grade and headline ("Installation held — 92% health").
- Credits breakdown (each line shown).
- Progress bar to the next unlock.
- 1–2 specific improvement tips.
- Buttons: Continue, Retry, Armoury. From M7, an optional "Watch ad: ×2 credits" button with clear labelling.

## 8. Feedback and audio cues
| Event | Visual | Audio |
|---|---|---|
| New contact | Radar ping, symbol appears | Radar beep |
| High-danger threat | Red edge flash, warning text | Warning tone plus radio callout |
| Interception | Hit marker, debris | Explosion, short confirmation callout |
| Threat got through | Impact marker on base, health drop | Impact, alarm |
| Reload/overheat | Tile state | Mechanical sound |

## 9. Accessibility
Subtitles for radio, shape-coded threat symbols, adjustable UI scale, reduced screen shake, separate volume sliders (Master, Music, Effects, Radio/Voice, Alerts).
