# Visual Acceptance Checklist — First Vertical Slice

Scope (brief §21): the night installation, ZU-23-2 as hero weapon, one representative threat, radar/HUD, aiming and firing, one complete interception and one impact sequence, and the cinematic-to-gameplay transition. Checked on the Xiaomi Pad 8 with a **non-development review build**, from the fixed capture positions in §2.

## 1. Checks

| # | Check | Pass rule | Result |
|---|---|---|---|
| P1 | No debug information in the player view | No fps, resolution, GPU name, profiler data or "Development Build" text in any capture | |
| P2 | Immersive presentation | No status bar or navigation bar during gameplay, including after a screenshot gesture and after returning from the recent-apps screen; no control under a cut-out or gesture area | |
| P3 | HUD inside safe areas at 4:3, 3:2, 16:9, 20:9 | No overlap between HUD elements; all touch targets ≥ 96 ref px | |
| E1 | One coherent installation | From the ZU camera at least one landmark (radar tower, command) and the base layout are visible; no floating or intersecting objects | |
| E2 | Depth and horizon | Real terrain ridges visible as separate layers fading into haze; the sky shows no printed mountains | |
| E3 | Night readability | In the gameplay view, no large area of featureless black; the weapon, emplacement and aim area are lit by at least two light layers | |
| E4 | Searchlights | Beams have a visible source, soft edges and falloff; no hard-edged flat strips | |
| W1 | ZU-23-2 hero quality | Barrels, mount, seats, sights and ammo boxes recognisable; at least three distinct materials (paint, bare metal, rubber) visible in the gameplay capture | |
| W2 | Weapon grounding | Contact shadows under wheels/jacks; no gap or sinking at the ground contact | |
| W3 | Motion | Full traverse and elevation sweep without clipping through the weapon, the pit or the camera | |
| T1 | Threat readability | The threat's silhouette is recognisable at 800 m and its light/marker at 3 km; the radar contact and the world marker highlight together | |
| T2 | Engagement sequence | Detection (radar + sound) → marker → player fire (flash, tracers, sound) → hit feedback → destruction effect → result in the HUD, all visible in one recording | |
| T3 | Impact sequence | A leaked threat produces a blast, smoke and lasting damage on the base, with HUD and audio feedback | |
| H1 | HUD design system | One type family, the colour tokens from `HUD_UI_SPECIFICATION.md` §2, one panel style; colour never the only state signal | |
| H2 | Radar | Animated sweep, labelled range rings, class symbols, selected-target state | |
| H3 | Onboarding | Prompts sit next to their controls and disappear after the action; nothing covers the weapon or aim point | |
| C1 | Cinematic hand-over | The last cinematic camera settles exactly on the gameplay camera; HUD reveals in stages; no second loading screen | |
| F1 | Performance (High tier) | ≥ 59 fps average and ≤ 1% frames over 20 ms in scenarios (a)–(c) of `ANDROID_PERFORMANCE_BUDGET.md` §6 | |
| F2 | Thermal | 20-minute soak without throttling below the target frame rate | |
| F3 | Lower tiers | Performance and Balanced tiers keep T1, T2, H1 readable | |
| L1 | Provenance | Every asset in the captures is listed in `ASSET_REGISTER.md` with licence and status; placeholders are labelled | |

## 2. Capture positions (before/after comparisons)

1. ZU-23-2 gameplay camera, aiming north at 10° elevation, Wave 1 start.
2. Same, while firing at a threat at 1.2 km.
3. C-RAM gameplay camera, aiming north.
4. Cinematic shot 2 (wide reveal) at its midpoint.
5. Close-up of the ZU pit (debug free camera, development build only).

Captures are stored in `docs/visual/captures/<date>_<step>_<position>.png`. The prototype baseline is `docs/visual/reference/2026-11-24_*`.
