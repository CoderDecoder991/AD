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
| Muzzle velocity (m/s) | 970 (23x152 HEI-T spec) | 970 |
| Air drag k (1/m) | 4.6e-4 (≈ 970 → 610 m/s by 1 km) | same |
| Tracer burn / self-destruct (s) | ~5 (±6% per round) / 5.4–9 (spec 5.4–12, capped for performance) | same |
| Tracers | Every round, both barrels (ZU belts are mostly tracer rounds) | — |
| Recoil: barrel travel / return time | 0.12 m / 0.09 s, barrels alternate | — |
| Recoil: camera kick / shake / muzzle climb per round | 0.22° / 0.12° / 0.02° (+ ±0.012° drift) | — |
| Damage per round | 1.0 | 1.0 |
| Dispersion (°, std. dev.) | 0.35 — M1 tuned | 1.2 |
| Reaction delay (s) | — | 1.5 |
| Simultaneous targets | 1 | 1 |

---

## 2. C-RAM — fictionalised battery

**Status:** Playable (M3a, 2026-11-23). 1st unlock, 4,000 credits (R-04). Model: the supplied Sketchfab C-RAM (C-22), rigged by `tools/blender/process_cram.py`. Research and the values used: section 7.

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

### Manual behaviour (as built, M3a)
- Tap the C-RAM tile to take it over; the camera blends across (0.4 s) to a third-person view behind the mount. The ZU-23-2 goes autonomous.
- Drag to aim (115°/s mount), hold FIRE: the barrels spin up for 0.3 s, then 75 rounds/s. Very light kick, 1,550 rounds, 15 s reload, overheats after about 14 s of continuous fire.
- Later (M3b): tap-to-designate with radar-assisted tracking, once rockets and mortars exist.

### Autonomous behaviour (as built, M3a: `AutonomousWeaponController`)
- Sense, decide, intercept: tracks threats from 1.25 × max range; engages only threats whose ground track passes within 140 m of the base centre (`ThreatensArea`), most urgent first (time to reach the base), with hysteresis on the current target.
- Opens fire after a 0.8 s track (radar track + engagement decision), when the bore is within 0.5° of the drag-and-gravity intercept point and the target is in 30–2,000 m. Bursts of 2.5 s with 0.35 s pauses to re-assess.
- Returns to rest and tops up the magazine when idle. After the magazine is empty, a long reload: the weakness the player must cover.
- The ZU-23-2 uses the same controller when not selected, as a gun crew: 1.5 s reaction, 0.5° wandering aim error, 1.2° fire tolerance, 1.2 s bursts.
- Accuracy and the accuracy bonus count only the player's own rounds.

### Balance fields (as built; FICTIONAL GAME-BALANCE VALUES informed by section 7)
| Field | Value | Real-world reference (section 7) |
|---|---|---|
| Fire rate | 75 rounds/s, 0.3 s spin-up | 4,500 rounds/min |
| Traverse / elevation speed | 115°/s / 115°/s | ~115°/s (Block 1B) |
| Elevation | −25° to +85° | −25° to +85° |
| Magazine / reload | 1,550 rounds / 15 s | 1,550 rounds / manual reload (much longer) |
| Muzzle velocity / drag | 1,100 m/s / k = 5.5e-4 per m | 20 mm M940 class |
| Self-destruct | 3.3–3.8 s (≈2.0–2.2 km), tracer burns 3.5 s | M940 self-destructs at tracer burnout, ~2 km |
| Effective range | 30–2,000 m | 1.5–2 km |
| Damage per round / dispersion | 0.6 / 0.12° | — |
| A tracer is a point of light; the streak is that point swept across the sensor during the exposure (motion blur), so it is an evenly bright bar with **round ends**, not a comet with a pointed tail. A long exposure shows a continuous line; a short one, separate dashes | [Motion blur (arXiv 2503.17358)](https://arxiv.org/pdf/2503.17358), [Projectile tracer patent](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/9739584) | Each round drawn as a capsule (round cap, even body, round cap) whose length is the distance flown in 1/60 s; caps stay round at any length; a streak coming toward the camera becomes a dot. All tracers in one dynamic mesh (one draw call) |
| Muzzle flash | a flash on every round, 1.35x the ZU core, flame reaching 1.5x further, bright light (16, 28 m range) that lights the mount; smoke on every 3rd round | user request 2026-11-24 ("more the tracer stream and the muzzle flash") |
| Self-destruct look | 0.7 × size flash, smoke puff and pop on 15% of rounds | many small bursts at the end of the stream |

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

## 6. Tracer behaviour (research notes, 2026-11-23)

What real 23 mm fire looks like, and how the game reproduces it:

| Real behaviour | Source | In game (`ProjectileSystem`) |
|---|---|---|
| 23x152 HEI-T muzzle velocity ≈ 970 m/s | Manufacturer spec sheets ([Arcus](https://arcus.bg/?p=21642), [Arsenal](https://arsenal-defense.com/?p=619), [Yugoimport](https://www.yugoimport.com/sites/default/files/documents/2023-12/23%20mm%20x%20152B%20HE-T_HEI-T%20eng..pdf)) | `muzzleVelocity = 970` |
| Shells slow under air drag and arc down | Standard exterior ballistics (quadratic drag, k = ½ρCdA/m) | `Ballistics.Step`, k = 4.6e-4/m; lead ring and tests include drag |
| Tracer burns ~5 s (OFZT/BZT 5–5.4 s), so tracers glow nearly all the way to where they burst | [ZU-23-2 (Wikipedia)](https://en.wikipedia.org/wiki/ZU-23-2), [ZU-23-2 ammunition table](https://military-history.fandom.com/wiki/ZU-23-2), AZP-23 forum figures | `tracerBurnTime = 5`, varied ±6% so the stream fades raggedly |
| Self-destruct after 5.4–12 s; max range 2,500 m, altitude 1,500 m | [Arcus HEI-T](https://arcus.bg/23x152mm-hei-t-high-explosive-incendiary-tracer-round/), [Arcus spec PDF](https://arcus.bg/wp-content/uploads/2020/04/23x152mm-HEI-T-Tech-Specs.pdf) | Random 5.4–9 s. Appearance per flak accounts ([SAS1946](https://www.sas1946.com/main/index.php?topic=43852.24), [DCS forums](https://forum.dcs.world/topic/262457-effects-flak-burst-dissipation-time/), [War History Online](https://www.warhistoryonline.com/war-articles/flak.html)): one sharp round white-hot flash (~0.1 s) with a dim halo lighting the haze, then a dark grey-black puff that hangs 5–7 s and slowly spreads, sometimes two "dumb-bell" lobes. No falling sparks (user: looked like a firework) |
| Distant bursts are heard after they are seen (sound 343 m/s; air absorbs high frequencies over km) | Physics (not found in sources; inferred) | `sd_pop_1-4.wav` (dull pop + thump + ground echo), played distance/343 s after the flash (≈7–10 s at 2.5–3.5 km), quieter and lower with distance |
| Tracers and bursts are bright light sources visible through night haze | Perception | `AirDefense/AdditiveGlowNoFog` (flashes, tracers) and `AirDefense/AlphaSmokeNoFog` (burst puffs) ignore scene fog; scenery keeps it |
| Burning core far brighter than the sensor can record (clips to yellow-white); edge and flame show the red of strontium (Sr(NO3)2 + Mg). The burning element is ~2 cm; what is seen is its glow | [Tracer ammunition (Wikipedia)](https://en.wikipedia.org/wiki/Tracer_ammunition); saturation is inference | Texture: thin saturated yellow-white core, red-orange halo; glow 0.2 m wide; minimum 6 px on screen so far tracers stay points of light |
| Tracers | every round; streaks 1.25x the 1/60 s length, so near the gun they join into one fine line and further out show as separate dashes | night accounts: "bright tracers which illuminate the night" ([DVIDS, Balad](https://www.dvidshub.net/news/printable/39004)) |
| Bright tracers ignite almost at the muzzle | [Tracer ammunition](https://www.wikipedia.org/wiki/Tracer_round) | Visible from 3 m past the muzzle |

## 7. C-RAM research notes (2026-11-23)

Public sources on the land-based Phalanx (C-RAM / Centurion) and how the game uses them. In-game the weapon is called "C-RAM"; no manufacturer names (R-13). Values in the game are fictional balance values informed by these figures.

| Real behaviour | Source | In game |
|---|---|---|
| 20 mm six-barrel rotary cannon, 4,500 rounds/min (some sources: 3,000 or 4,500 selectable) | [Centurion C-RAM (Wikipedia)](https://en.wikipedia.org/wiki/Centurion_C-RAM), [DigDB](https://en.digdb.net/phalanx-ciwscenturion-c-ram/), [Army Recognition](https://www.armyrecognition.com/military-products/army/air-defense-systems/self-propelled-anti-aircraft-guns/centurion-c-ram-united-states-uk) | 75 rounds/s; barrels spin at 12.5 rev/s (`BarrelSpin`); 0.3 s spin-up before full rate |
| 1,550-round magazine; bursts of up to ~1,000 rounds | [Brickmania](https://www.brickmania.com/centurion-c-ram-land-based-phalanx-weapon-system/) (weak source), [Nihonkoku Shoukan wiki](https://nihonkoku-shoukan.fandom.com/wiki/Phalanx_CIWS) | 1,550 rounds; 2.5 s autonomous bursts (~190 rounds) |
| Effective 1.5–2 km. M940 MPT-SD rounds self-destruct (tracer burnout) at ~2–2.3 km to limit collateral damage | [Centurion C-RAM (Wikipedia)](https://en.wikipedia.org/wiki/Centurion_C-RAM), [DigDB](https://en.digdb.net/phalanx-ciwscenturion-c-ram/), [Navy Matters](https://navy-matters.blogspot.com/2021/06/c-ram.html) | Max range 2,000 m; rounds self-destruct at 3.3–3.8 s (≈2.0–2.2 km) |
| Mount: elevation −25° to +85°, slew about 115°/s (Block 1B); Block 0 86°/s elevation, 100°/s train | [NavWeaps](https://www.navweaps.com/Weapons/WNUS_Phalanx.php), [Phalanx CIWS (Wikipedia)](https://en.wikipedia.org/wiki/Phalanx_CIWS), [seaforces](https://www.seaforces.org/wpnsys/SURFACE/Mk-15-close-in-weapon-system.htm), [babriet](https://babriet.tripod.com/navy/stat/statphalanx.htm) | 115°/s both axes; −25°..+85° |
| Sense → warn → intercept. Radars (LCMR, Firefinder) track the incoming round and predict its impact point; a loud alarm and an "INCOMING, INCOMING" voice warn troops to take cover; only rounds threatening the protected area are engaged; hundreds of rounds detonate the threat in the air. Reported intercept rate 60–70% | [DVIDS](https://www.dvidshub.net/news/22272/c-ram-system-protects-deployed-service-members), [Interesting Engineering](https://interestingengineering.com/innovation/c-ram-an-advanced-automated-point-defense-gatling-gun), [C-RAM (Wikipedia)](https://en.wikipedia.org/wiki/C-RAM), [CENTCOM Citadel](https://centcomcitadel.com/en_GB/articles/ssc/features/2023/10/06/feature-02), [afghanwarnews](https://afghanwarnews.info/equipment/C-RAM.htm), [EDN](https://ednnews-12.com/airdefense/how-c-ram-defenses-stop-rockets-artillery-and-mortars/) | `AutonomousWeaponController`: tracks, engages only threats heading into the base (`ThreatensArea`), most urgent first. The "INCOMING" warning and impact prediction arrive with rockets and mortars (M3b) |
| Distinctive sound: individual reports merge into a ripping "brrrt" whose pitch is the fire rate | Physics (75 rounds/s = 75 Hz fundamental) | `tools/audio/make_cram_sound.py` lays the user's real recorded shot (S-01), pitched ×1.22 for 20 mm, at exactly 75/s; spin-up clip with the rate ramp; outdoor-reverb tail. Check: loop spectrum peaks at 75, 150, 225 Hz |

