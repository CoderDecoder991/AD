# Game Design Document

Working title: **AirDefense** (O-11). Decision IDs (C-xx, R-xx, O-xx) refer to [DECISIONS_AND_ASSUMPTIONS.md](DECISIONS_AND_ASSUMPTIONS.md).

## 1. Vision

You command a fictional air-defence post at night. Threats arrive on radar, and you decide what to engage, with which weapon, and when. You aim and fire one weapon by hand while your other systems defend autonomously. It should feel tense, grounded and cinematic, not arcade (C-02).

**Pillars**
1. **Decide under pressure.** The challenge is prioritising and allocating, not just reflexes.
2. **Believable machines.** Each weapon looks, sounds and handles distinctly.
3. **Readable chaos.** Even a busy wave can be understood at a glance on a phone.
4. **Fair progress.** Everything core is earned through play (C-06).

## 2. Core loop

```
Select mission ─► Briefing ─► Radar watch ─► Engage (manual + autonomous)
      ▲                                              │
      │                                              ▼
 Unlock / upgrade ◄── Credits ◄── After-action report ◄── Mission end
```

1. Choose a mission or survival session.
2. A short briefing: objective and expected threat types.
3. Enter the operator view. The radar shows incoming contacts.
4. Aim and fire the selected weapon. Switch weapons when the situation needs it.
5. Unselected unlocked weapons engage on their own within their limits.
6. Threats that get through damage the installation.
7. The mission ends in success (objective met) or failure (installation destroyed).
8. The after-action report shows credits, performance and tips.
9. Spend credits on unlocks and upgrades, then pick a harder mission.

## 3. Weapons and control

Full detail is in [WEAPON_SYSTEMS.md](WEAPON_SYSTEMS.md).

| | ZU-23-2 | C-RAM (fictionalised battery) | 2K22 Tunguska |
|---|---|---|---|
| Unlock | Start | Credits, 1st unlock (R-04) | Credits, 2nd unlock |
| Role | Short-range manual gun | Very-short-range point defence against rockets, artillery, mortars and small drones | Medium-range guns + missiles against aircraft and cruise missiles |
| Manual feel | Pure gunnery: lead the target, manage bursts and reloads | Designate and track; the fire-control computer helps aim | Guns for close targets, radar lock and missile launch for distant ones |
| Autonomous strength | Weak (crew fires at nearby low threats) | Strong against its threat class, limited magazine | Strong, limited missile count |

### 3.1 Manual control rules
- Exactly one weapon is under manual control at any time. Its tile is highlighted and the camera is at its operator position.
- Switching hands manual control to the new weapon. The old one becomes autonomous if it's unlocked and working (C-07).
- Switching has a short camera transition (target ≤0.4 s), and during it the radar stays visible.

### 3.2 Autonomous rules
- Each autonomous weapon picks targets from those it may engage, within its range band, following its support priority (R-18).
- Limits come from data (C-08): eligible threat classes, min/max range, simultaneous targets, reaction delay, accuracy modifier, ammo, reload time.
- Autonomous accuracy and reaction are always worse than a skilled player's, so manual play keeps its value.
- Every autonomous engagement is shown: a target line on the radar, a status chip on the weapon tile and a kill or miss marker.

### 3.3 Weapon states
`Selected`, `Ready (autonomous)`, `Engaging`, `Reloading`, `Overheated` (ZU only), `Damaged/Unavailable`, `Locked`.

## 4. Threats

All behaviour values are game-balance values in data files.

| Class | Example presentation | Speed | Health | Danger | Introduced |
|---|---|---|---|---|---|
| Small drone | Quadcopter, erratic path | Slow | Very low | Low | M1 (MVP) |
| Fixed-wing drone | Propeller drone, straight run | Slow–medium | Low | Medium | M1 (MVP) |
| Loitering munition | Delta-wing, dives at the end | Medium | Low | High | M3 |
| Rocket salvo | Several rockets on ballistic arcs | Fast | Very low | High | M3 (C-RAM) |
| Mortar | Single high-arc round | Medium | Very low | Medium | M3 (C-RAM) |
| Cruise missile | Low and fast, terrain-hugging | Fast | Medium | Very high | M4 (Tunguska) |
| Strike aircraft | Fast jet, drops ordnance, can evade | Fast | High | Very high | M5 |

**Readability rules**
- Every threat class has a distinct radar symbol and a distinct silhouette.
- Small or fast threats get a screen-space marker and a short trail.
- Incoming ballistic threats show a predicted impact marker on the base.
- At most one new threat class is introduced per mission.

## 5. Installation and damage

- MVP: one shared health pool (R-05), shown as a bar with damage stages at 75/50/25%.
- Each threat that gets through deals damage based on its class.
- Visible damage: fires, smoke and damaged structures at each stage.
- Later (M5): protectable assets such as the radar, fuel depot and command post. Losing the radar degrades radar information, for example.

## 6. Modes and roadmap

| Mode | Description | Milestone |
|---|---|---|
| Mission | Scripted objective, briefing, fixed waves | M1 (one mission), more in M5 |
| Survival | Escalating endless waves | M5 |
| Multi-Threat | Overlapping classes that test all three weapons | M5 (after M4) |
| Daily Challenge | Daily seeded scenario with a bonus | M6 |

## 7. Scoring and the after-action report

**Measures:** interceptions by class, threats missed, damage taken, accuracy (hits/rounds), average time from detection to engagement, survival time, ammo efficiency.

**Report contents:** grade (S/A/B/C/D), credits breakdown, progress toward the next unlock, and one or two specific tips (e.g. "3 drones reached the base from the north-east while you were reloading. Try shorter bursts.").

## 8. Progression

- Credits are earned through play (see [ECONOMY_AND_MONETISATION.md](ECONOMY_AND_MONETISATION.md)).
- Unlock order: ZU-23-2 → C-RAM → Tunguska (R-04).
- Upgrades (M6): ammo capacity, reload speed, traverse speed, radar range, autonomous reaction time. Each has a small number of levels with capped benefits.
- New bases and environments (day/dusk/night) are progression rewards (M5+).

## 9. Onboarding

- The first mission teaches through short contextual prompts: "Drag to aim", "Hold to fire", "Lead the target", "Reload".
- Prompts appear only when relevant and disappear once the player has done the action.
- C-RAM and Tunguska each get a short guided first use when unlocked.

## 10. Tone and content limits

- A fictional setting and adversary (R-14).
- Violence is limited to equipment and vehicles. No graphic injuries (A-04).
- Radio callouts are short and professional.
