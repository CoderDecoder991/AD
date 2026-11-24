# HUD and UI Specification

**Status:** Recommended (awaiting user review). Replaces the prototype HUD visual design; the HUD logic in `UI/HudController.cs`, `RadarView.cs`, `ThreatMarkers.cs`, `TouchControls.cs` is kept and re-skinned.
Layout unit: reference pixels at 1920×1080, `CanvasScaler` match 0.5 (as now).

## 1. Typography

| Use | Proposal | Size (ref px) |
|---|---|---|
| Labels, buttons, banners | **Barlow Semi Condensed** (SIL Open Font License) — Medium / SemiBold | 22–28 labels, 34–40 banners |
| Numbers (ammo, range, credits, timers) | **Barlow Semi Condensed** with tabular figures, or **JetBrains Mono** (OFL) for fixed-width readouts | 24–32 |
| Small captions | Barlow Semi Condensed Regular | ≥ 18 (minimum readable on a phone) |

- Rendered with **TextMeshPro SDF** (included in Unity 6 UGUI) so text stays sharp at every resolution, with an outline/underlay for contrast over the 3D scene.
- Upper case for labels and states; sentence case for prompts and briefings.
- **Open decision U-03:** font choice. Both are OFL (free for games, attribution in Credits). Alternatives: Rajdhani, Saira Condensed.

## 2. Colour tokens (semantic)

| Token | Use | Value |
|---|---|---|
| `ink` | Primary text | #E8ECE6 |
| `ink-dim` | Secondary text, locked labels | #E8ECE6 at 55% |
| `panel` | Panel background | #0E1412 at 72% |
| `panel-line` | Panel border, separators | #9FB8A6 at 25% |
| `system` | Friendly/system state, radar, selected outline | #7FD49A (muted green) |
| `info` | Autonomous support state | #7FB8D4 (muted blue) |
| `caution` | Reloading, overheating, unlock available | #E6B64C (amber) |
| `threat` | Hostile contacts, markers | #F0904A (orange) |
| `critical` | Leak / impact / low health / alarms | #E5534B (red) |
| `disabled` | Unavailable, locked | #6B706C |

Colour is never the only signal: every state also has an icon, shape or text (brief §18.8).

## 3. Spacing and shape

- 8 px grid. Outer safe margin 24 px plus `Screen.safeArea`; extra 32 px bottom margin above the gesture area.
- One panel style: 6 px corner radius, `panel` fill, 1 px `panel-line` border, 12–16 px inner padding.
- Minimum touch target 96×96 ref px (≈ 9 mm on the Pad 8); 16 px minimum gap between targets.
- Icons: one line style (2 px stroke at 48 px), from one source; drawn in-house as SVG and imported as sprites.

## 4. Radar (top left, 280×280)

- **Display:** north-up (current), with a wedge showing the selected weapon's facing and field of fire.
- **Sweep:** rotating line, one turn per 2.5 s, with a fading afterglow; contacts brighten when the sweep passes and fade between sweeps (they never disappear while tracked).
- **Range rings:** 3 rings with labels (e.g. 1 / 2 / 3 km) in small `ink-dim` text.
- **Contact symbols by class:** drone = triangle, aircraft = chevron, cruise missile = arrow, rocket/mortar = small diamond with predicted-impact cross (M3b). Colour `threat`; diving or about to impact = `critical` with a pulse.
- **Selected / engaged state:** the contact the player is aiming at gets a square bracket; contacts engaged by an autonomous weapon show a small tag of that weapon (e.g. "C").
- **Link to the world:** each contact and its world marker share an ID colour accent and highlight together when the player taps either.

## 5. Weapon tiles (bottom left, 3 tiles, 160×120 each)

| State | Visual |
|---|---|
| Manual (selected) | `system` outline 3 px, filled header, label "MANUAL" |
| Auto (unlocked support) | `info` accent bar, label "AUTO" + live mini status (e.g. "ENGAGING", ammo bar) |
| Reloading | `caution` progress ring over the tile icon, label "RELOAD 6s" |
| Overheated / unavailable | `caution`/`disabled` with icon |
| Damaged (future) | `critical` icon |
| Locked | `disabled`, padlock icon, cost "4,000 cr" |
| Unlock available | `caution` glow pulse, "TAP TO UNLOCK" |

Each tile: weapon silhouette icon, name, state, one key value (ammo or cost). Nothing else.

## 6. Fire and aim controls

- **FIRE:** reduced from 240 px to ~200 px diameter; ring design (dark fill + `critical` ring) instead of a solid red disc; pressed state fills; disabled state (reloading/overheated) shows the reason inside the ring.
- **Zoom:** smaller secondary button above FIRE with an icon instead of text.
- **Aim zone:** left 60% of the screen (decision C-19); a faint corner hint appears only while it has never been used.
- **Ammo/heat panel:** bottom centre, compact (360×72), two thin bars and numbers; tap to reload (as now).

## 7. Prompts and banners

- Onboarding prompt: a compact card anchored next to the control it explains (drag hint inside the aim zone with an animated hand icon; fire hint next to FIRE). Dismissed by completing the action; never covers the weapon or the aim point.
- Wave banner: top centre, one line, auto-hide after 2.5 s.
- Critical alerts ("IMPACT", "INCOMING"): short banner + border flash in `critical`; never more than one at a time.

## 8. Threat markers

- On-screen: thin corner brackets sized to the threat's distance; range label below in `threat`.
- Off-screen: edge chevrons with range.
- Lead ring (aim assist "Light"): thin `caution` ring.
- Marker density cap: at most 6 labelled markers; others show brackets only.

## 9. Debug and developer information

- FPS, resolution, render scale, graphics API, GPU name and any profiler data appear only in the developer overlay, off by default, toggled by a hidden gesture or a dev-menu switch; placed below the pause button so it never overlaps the HUD.
- "Development Build" watermark: only in Development builds. Review builds (shown to the user, used for screenshots) are non-development builds.

## 10. Android presentation

- Landscape, immersive sticky full screen; re-applied when the app regains focus.
- Layout inside `Screen.safeArea` (cut-outs) plus a gesture margin at the bottom.
- The back gesture opens Pause (current behaviour).
- Checked at 4:3, 3:2, 16:9 and 20:9 with and without system bars visible.

## 11. Implementation notes

**As built (VS-2, 2026-11-24):** tokens in `UI/UiTheme.cs`; `UiFactory` maps the older colour names onto them and adds the panel/border/underlay helpers, radar sprites and the zoom icon. Text uses UGUI dynamic fonts with the Barlow faces (not TextMeshPro yet: a TMP migration touches every UI script and is not needed for sharpness at the current sizes; revisit if outlines or very large text are needed).


- New `UiTheme` ScriptableObject holding tokens (colours, fonts, sizes); `UiFactory` reads from it so components are consistent.
- Radar sweep and symbols drawn with UGUI sprites and a small custom shader (afterglow) — no per-frame allocations.
- HUD layout snapshot test (PlayMode) at the four aspect ratios to catch overlaps.
