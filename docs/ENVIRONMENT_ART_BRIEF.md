# Environment Art Brief — First Installation ("Night Watch" base)

**Status:** Recommended (awaiting user review). Fictional forward air-defence site in a dry, hilly region at night. No real location.

## 1. Current state (inspected, commit `3ca592a`)

- 180 m square perimeter fence (chain-link, CC0), apron of gravel concrete, two asphalt roads, street lamps.
- Five box buildings with facade detail: Command (24×8×16 m), Barracks A/B, Hangar (corrugated metal), Depot (container texture).
- ZU-23-2 in a pit on a 14 m earth berm south of the buildings, facing north; C-RAM and Tunguska on 9 m / 7 m earth mounds.
- Radar mast with a spinning bar dish; three cylinder fuel tanks; two searchlight masts; four red beacons.
- Flat 5 km ground plane; distant mountains exist only in the sky image.
- Desert scatter (bushes, boulders, logs) outside the fence.

## 2. Layout goals

1. **Readable from the gameplay camera.** From each weapon position the player sees: their weapon, its emplacement, part of the base below/around it, and the threat sector (north) above the horizon.
2. **Functional logic.** Each area has a reason: command and radar at the centre, weapons on the perimeter facing the threat sector, logistics at the back (south), living area away from the gun line.
3. **Landmarks** visible from every gun: the radar tower (tallest, with a rotating antenna and a red obstruction light), the command bunker, the hangar.
4. **Depth.** Near ridges and far mountain silhouettes as real geometry, fading into haze.

## 3. Proposed site plan (north = threat sector)

```
                 N (threat sector)
   ridge line 600-1500 m (real terrain, hazy silhouettes)
 ------------------------------------------------------ fence (±90 m), HESCO line on the north side
   [Tunguska pos.]        [Radar tower]        [C-RAM pos.]       <- weapons on revetted pads, north edge
         \                     |                     /
   [Barracks A]   ===== road ===[Command bunker]=== road =====  [Barracks B]
                               |
   [Hangar]      [vehicle park]|[fuel point / generators]   [Depot]
                               |
                     [ZU-23-2 revetted pit, south berm]
                               |
                   gate + guard post (south)
```

Proposed changes versus today (to confirm with the camera tests in VS-3/VS-5):
- Keep the ZU in the south (start position, it covers the base); lower its berm on the threat side so the base and the sky are visible over it.
- Move C-RAM and Tunguska to revetted pads near the north edge (their real role: closest to the threat axis), facing north.
- Replace the radar mast + bar with a lattice radar tower and a modelled rotating antenna.
- Add a gate with guard post, a vehicle park, a fuel point, generators and a cable run.

## 4. Asset groups

### Hero (0–15 m from a gameplay camera)
| Asset | Description | Source |
|---|---|---|
| ZU-23-2 pit | Circular pit, sandbag and HESCO walls, timber ammo niches, duckboards, cable, crew equipment, a hooded work light | In-house Blender (new) |
| C-RAM / Tunguska pads | Concrete pad, HESCO revetment on three sides, generator / cable trunk | In-house Blender (new) |
| Close props | Ammo boxes, jerrycans, crates (existing CC0) | Existing — usable |

### Mid (15–80 m)
| Asset | Description | Source |
|---|---|---|
| Command bunker | Low reinforced concrete block with sandbagged entrance, antennas, cable trays | In-house Blender (new) |
| Barracks, hangar, depot | Current boxes + facade detail; needs roof edges, overhangs, varied silhouettes | Needs improvement (Blender) |
| HESCO / T-wall barriers | Modular blast-wall pieces | In-house Blender (new) |
| Radar tower | Lattice tower, rotating antenna, obstruction light | In-house Blender (new) |
| Vehicles | Covered cars (existing CC0); military truck / generator trailer | Existing + licensed (new) |
| Roads, apron, decals | Existing asphalt/gravel; add tyre marks, cracks, oil stains as decals | Needs improvement |

### Far (80 m – horizon)
| Asset | Description | Source |
|---|---|---|
| Terrain | Modelled ground with gentle relief inside 400 m, rocky ridges at 600–1500 m, far mountain silhouettes | In-house Blender (heightfield) |
| Scatter | Existing bushes/boulders/logs + impostors for far bands | Existing + needs LODs |
| Sky | Stars and moon only (current HDRI shows mountains; replace or mask the horizon) | Needs improvement |

## 5. Lighting plan (night)

- Moon (directional, cool) as key; baked indirect light and ambient occlusion on static geometry; light probes for weapons and threats.
- Practical pools along roads and at doors (warm), lit windows (warm/fluorescent mix), a hooded work light at each crewed weapon (keeps the hero weapon readable).
- Red rotating beacons on command and tower; red obstruction light on the radar tower.
- Searchlights at the north corners: real spot lights + new beam shader + lamp glow.
- Fog density tuned so the ridge lines fade but threats at 2–3 km stay visible (threat lights ignore fog).

## 6. Ambient life (affordable)

Rotating radar antenna; sweeping searchlights; flashing beacons; one patrol vehicle on the perimeter road (headlights); camouflage nets and flags moving slightly (vertex wind); occasional flicker on one or two lamps. Crew figures (licensed models) at the weapons in a later step.

## 7. Composition checks

For each weapon camera and the four cinematic shots: horizon in the upper third, weapon in the lower third, at least one landmark visible, no large uniform dark areas, threats appear against the sky (not against bright lamps). Captures saved under `docs/visual/captures/` before and after each step.
