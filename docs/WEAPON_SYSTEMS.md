# Weapon Systems

> **All numbers below are fictional game-balance values** (C-09). They are starting points for tuning and are not claims about real-world performance. They live in data assets (`WeaponDefinition`), not in code.

Range bands use game units (1 unit ≈ 1 m in the scene) and are compressed for gameplay readability.

---

## 1. ZU-23-2 — starting weapon

**Status:** Unlocked from the start (C-05).

### Visual identity
- A towed twin-barrel 23 mm anti-aircraft gun on a two-wheel carriage, with wheels raised and the gun set on its firing base.
- Exposed operator seats with handwheels, a simple optical sight and box magazines on the side of each barrel.
- Moving parts: traverse, elevation, recoiling barrels, ejected brass, magazine swap.
- Silhouette: low, open, mechanical. The "hands-on" weapon.
- References to gather in asset research: museum walk-around photos and public reference images of the towed configuration. Record all sources in [ASSET_REGISTER.md](ASSET_REGISTER.md).

### Gameplay role
A short-range, high-rate manual gun. It teaches leading targets, burst discipline and reload timing.

### Manual behaviour
- Drag to traverse/elevate. Hold the fire button to fire both barrels.
- No automatic lead. A simple lead indicator appears only with aim assist on (setting).
- Barrel heat rises while firing. Overheating forces a short cooldown.
- Magazine reload when empty, or by manual tap.

### Autonomous behaviour (when not selected)
- The crew engages only low, slow threats (drones) inside a reduced range.
- Low accuracy, long reaction delay, one target at a time.

### Balance fields (starting values)
| Field | Manual | Autonomous |
|---|---|---|
| Eligible targets | All air threats | Small drone, fixed-wing drone |
| Effective range (min–max) | 50–1500 | 50–900 |
| Traverse speed (°/s) | 60 | 40 |
| Fire rate (rounds/s, both barrels) | 30 | 20 |
| Magazine (rounds) | 100 | 100 |
| Reload time (s) | 4.0 | 6.0 |
| Heat per second of fire / cooling per second | 0.20 / 0.15 (overheat lock until < 0.30) — M1 tuned | n/a (bursts only) |
| Muzzle velocity (m/s) / round lifetime (s) | 900 / 2.2 — M1 | 900 / 2.2 |
| Damage per round | 1.0 | 1.0 |
| Dispersion (°, std. dev.) | 0.35 — M1 tuned | 1.2 |
| Reaction delay (s) | — | 1.5 |
| Simultaneous targets | 1 | 1 |

---

## 2. C-RAM — fictionalised battery

**Status:** Locked. 1st unlock (R-04). **Identity: Recommended R-13, needs approval (O-06).**

### Design choice (brief §2 note)
C-RAM is a capability, not one specific weapon. We present a **clearly labelled, fictionalised C-RAM battery**, made up of:
1. A **rotary-cannon gun mount** on a trailer or pallet, with its own fire-control radar dome.
2. A separate **search radar** mast.
3. A **command shelter** with an operator console (the manual camera position).

The gun mount is visually inspired by real land-based counter-rocket gun systems. It uses no manufacturer names, logos or trademarks, and the in-game name is "C-RAM battery". The configuration is not claimed to match any specific real system.

### Visual identity
- Silhouette: an enclosed, white/grey rounded radar dome above a multi-barrel rotary cannon. Clean, high-tech, very different from the ZU-23-2.
- Signature effects: a continuous tracer stream, a distinctive "buzz" sound, a self-destructing round flash.

### Gameplay role
Very-short-range point defence. Excellent against rockets, artillery and mortars (RAM) and small drones. Poor against anything fast at distance.

### Manual behaviour
- The player designates a target by tapping it on the radar or on screen. The fire-control computer tracks it and assists aim. The player fine-adjusts and holds fire.
- Very high fire rate and a small magazine, so it's about timing: fire too early and you run dry.

### Autonomous behaviour
- Automatically engages RAM threats and small drones that enter its range band.
- Prioritises the threat predicted to land closest to the base.
- After the magazine is empty, a long reload. This is the key weakness the player must cover.

### Balance fields (starting values)
| Field | Manual | Autonomous |
|---|---|---|
| Eligible targets | Rocket, mortar, small drone, loitering munition | Rocket, mortar, small drone |
| Effective range (min–max) | 30–1200 | 30–1000 |
| Traverse speed (°/s) | 120 | 120 |
| Fire rate (rounds/s) | 60 | 60 |
| Magazine (rounds) | 600 | 600 |
| Reload time (s) | 12.0 | 15.0 |
| Damage per round | 0.4 | 0.4 |
| Dispersion (°) | 0.4 (with assist) | 0.6 |
| Reaction delay (s) | — | 0.6 |
| Simultaneous targets | 1 | 1 (fast re-target 0.5 s) |

---

## 3. 2K22 Tunguska

**Status:** Locked. 2nd unlock (R-04).

### Visual identity
- A tracked air-defence vehicle with a large turret, twin cannon on each side, missile launch tubes, a rotating search radar at the rear of the turret and a tracking radar at the front.
- Moving parts: turret rotation, radar dishes spinning/tracking, elevating gun and missile packs, missile launch with booster smoke.
- Silhouette: a large, heavy armoured vehicle. The "command" weapon.
- References to gather in asset research: public museum and parade reference photos. Record all sources.

### Gameplay role
Medium-range hybrid. Guns for close targets, missiles for distant, fast threats (cruise missiles, aircraft).

### Manual behaviour
- **Gun mode:** like the ZU-23-2 but with radar-assisted lead and a longer range.
- **Missile mode:** tap a contact to start a radar lock (lock time depends on range). When locked, tap Launch. The player chooses when to commit a limited missile.
- Toggle modes with a large button. Gun/missile ammo is shown separately.

### Autonomous behaviour
- Uses missiles on high-danger targets (cruise missiles, aircraft) beyond gun range.
- Uses guns on targets within gun range.
- Limited missiles per mission, so autonomous use can drain them. The support priority setting (R-18) matters here.

### Balance fields (starting values)
| Field | Guns | Missiles |
|---|---|---|
| Eligible targets | All air threats | Fixed-wing drone, loitering munition, cruise missile, aircraft |
| Effective range (min–max) | 100–2500 | 1500–6000 |
| Traverse speed (°/s) | 80 | 80 |
| Fire rate | 40 rounds/s | 1 missile per 2.0 s |
| Ammo | 400 rounds | 8 missiles |
| Reload | 10.0 s | Missiles not reloaded mid-mission (M4 tuning) |
| Damage | 1.0 per round | 10.0 (proximity) |
| Lock time (s) | — | 1.0 + 0.3 per 1000 units |
| Autonomous reaction delay (s) | 1.0 | 1.5 |
| Simultaneous targets (autonomous) | 1 | 2 missiles in flight |

---

## 4. Distinctness checklist
| | ZU-23-2 | C-RAM | Tunguska |
|---|---|---|---|
| Shape | Low, open, towed | Dome + rotary gun, static | Tracked armoured vehicle |
| Colour/finish | Weathered olive | Light grey/white dome | Camouflage green/brown |
| Sound | Heavy rhythmic thump | Continuous buzz | Cannon roar + missile whoosh |
| Camera | Open seat, optical sight | Shelter console + gun cam | Turret sight + radar screen |

## 5. Data structure (summary)
Each weapon is a `WeaponDefinition` data asset with: id, display name, unlock cost, unlock prerequisite, model prefab, camera rig, one or more `WeaponMode`s (guns/missiles), each with the fields in the tables above, plus `AutonomousProfile` (eligible classes, range, reaction delay, accuracy multiplier, priority rule). See [TECHNICAL_ARCHITECTURE.md](TECHNICAL_ARCHITECTURE.md).
