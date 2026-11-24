# Decisions and Assumptions

Single source of truth for project decisions. Every other document refers back here.

**Status labels**
- **Confirmed** — stated in the handoff brief or explicitly approved by the user.
- **Recommended** — Claude's proposal; in effect as a working default, but not user-approved.
- **Open** — needs a user decision; no default is being relied on yet unless noted.

---

## 1. Confirmed

| ID | Decision | Source |
|---|---|---|
| C-01 | Target platform is Android. | Brief §header |
| C-02 | Genre: realistic 3D air-defence command / action game, not an arcade turret shooter. | Brief §1 |
| C-03 | Free-to-play, supported by optional ads and in-app purchases. | Brief §header, §10 |
| C-04 | Three persistent weapon tiles: ZU-23-2, C-RAM, 2K22 Tunguska. | Brief §2, §3.3 |
| C-05 | ZU-23-2 is the only weapon available at the start. C-RAM and Tunguska start locked. | Brief §2 |
| C-06 | Locked weapons are unlocked with credits earned in play. Core unlocks never require real money. | Brief §3.4, §10 |
| C-07 | The selected weapon is under manual control. Other unlocked weapons fight autonomously. | Brief §3.1–3.3 |
| C-08 | Autonomous support must be visible (target, firing, ammo, result) and limited (eligibility, ammo, reload, range, simultaneous targets). | Brief §3.2 |
| C-09 | All gameplay performance values are data-driven, fictional game-balance values. No real-world performance claims. | Brief §8, §15 |
| C-10 | First-launch cinematic of about 30–45 s, night air-raid premise, skippable, replayable, flows straight into gameplay. | Brief §7 |
| C-11 | No pay-to-win, no paid random loot boxes as a core dependency, no ads during live engagements. | Brief §10 |
| C-12 | No ad or billing SDKs until the user approves a provider after current policy checks. | Brief §10 |
| C-13 | The MVP proves the ZU-23-2 core loop before C-RAM, Tunguska or monetisation are built. | Brief §12, §15 |
| C-14 | Placeholder art is allowed only when clearly labelled. Asset licences and attribution are recorded. | Brief §15 |
| C-15 | Primary Android test device is the user's **Xiaomi Pad 8** (Snapdragon 8s Gen 4, Adreno 825, 11.2" 3:2, 3200×2136, up to 144 Hz). | User, 2026-11-23 |
| C-16 | Development PC: i5-6400, 16 GB RAM, GT 730. Performance is judged on the device, not in the editor. | User, 2026-11-23 |
| C-17 | Engine: **Unity 6 LTS with URP** (was R-01, closes O-05). | User, 2026-11-23 |
| C-17a | Installed editor is **6000.6.10f1 (Unity 6.6)**. Unity Hub lists it as a "Supported" release, not LTS. Acceptable for M0; review moving to an LTS version before M2 (see O-12). | Install, 2026-11-23 |
| C-21 | Third-person camera behind and above the selected weapon, showing the full model; view tilted 10° down, crosshair follows the real aim point (replaces the first-person operator view of R-03). | User, 2026-11-23 |
| C-22 | All three weapon models shown in the base (C-RAM and Tunguska on raised positions; display-only until M3/M4). The supplied Sketchfab C-RAM model is used, with its required CC-BY credit (closes O-13 by user direction). | User, 2026-11-23 |
| C-23 | C-RAM implemented from public-source research (WEAPON_SYSTEMS.md §7): 75 rounds/s with spin-up, 115°/s mount, rounds self-destruct at ~2 km. Unlocked by tapping its tile (confirmation card, 4,000 cr); tapping an unlocked tile switches manual control with a camera blend; the weapon not operated defends on its own and engages only threats heading into the base. Accuracy counts only the player's own rounds. | User ("Start M3", "do a deep search and implement"), 2026-11-23 |
| C-20 | ZU-23-2 firing sound uses the user's own Tunguska recording (opening shot + seamless loop + echo tail), pitched x1.05. | User, 2026-11-23 |
| C-19 | Aim by dragging on the **left** side of the screen; FIRE stays bottom-right (replaces R-06 "right ~60%"). | User, 2026-11-23 |
| C-18 | Project lives on the C: SSD at `C:\dev\AirDefense`, in Git with Git LFS (was R-17). | User approved M0 plan, 2026-11-23 |

## 2. Recommended (working defaults, awaiting approval)

| ID | Topic (brief §14 #) | Recommendation | Why | Alternatives |
|---|---|---|---|---|
| R-01 | Engine (#1) — **now Confirmed as C-17** | **Unity 6 LTS** with **URP** | Mature Android 3D pipeline, large asset ecosystem, solid ad/billing plugins, workable on the dev PC. | Unreal 5 (better visuals, too heavy for the dev PC and mid-range Android); Godot 4 (weaker mobile 3D and monetisation plugins). |
| R-02 | Graphics API | Vulkan primary, OpenGL ES 3.x fallback | Best performance on modern Adreno; fallback for older devices. | GLES only. |
| R-03 | Camera (#2) | Hybrid: close operator view for the selected weapon, plus an always-visible compact radar panel. Optional short tactical overview on demand. | Keeps the operator fantasy while keeping threat awareness. | Pure first-person; pure third-person tactical. |
| R-04 | Unlock order (#3) | Sequential: C-RAM first, then Tunguska. | Teaches one new system at a time; simpler economy. | Independent unlocks. |
| R-05 | Base health model (#5) | One shared installation health pool in the MVP. Several protectable assets from Milestone 5. | Easiest to read; leaves room for objective missions later. | Several assets from the start. |
| R-06 | Touch aiming (#6) | Drag anywhere on the right ~60% of the screen to traverse/elevate, a large hold-to-fire button, adjustable sensitivity, optional aim assist and gyro aiming. | Familiar mobile pattern; works on tablet and phone. | Virtual joystick; tap-to-designate. |
| R-07 | Connectivity (#7) | Fully offline at launch. Local save on the device. | No server cost or privacy burden for the MVP. | Online or hybrid. |
| R-08 | Online features (#8) | Leaderboards, cloud save and seasons are post-launch. | Focus on the core loop first. | Include at launch. |
| R-09 | First-mission threats (#4) | Small quadcopter drones and a slow propeller-driven fixed-wing drone. | Readable, slow enough to learn aiming and leading targets. | Include rockets/mortars early. |
| R-10 | Minimum Android (#10) | minSdk 26 (Android 8.0), ARM64 only, IL2CPP. targetSdk set to Google Play's current requirement at release. | Covers almost all active devices; ARM64 is required on Play. | minSdk 24/28. |
| R-11 | Device tiers (#10) | High = Xiaomi Pad 8 class. Medium and Low tiers designed in from day one, validated on at least one cheaper phone before release. | Pad 8 is far above the average Android device. | Single quality level. |
| R-12 | Frame rate | 60 fps cap (30 fps on Low tier). Render scale ~0.7 of native on the Pad 8. | Battery, heat and sustained performance. | Uncapped/120 fps. |
| R-13 | C-RAM identity | A **clearly labelled fictionalised C-RAM battery**: a land-based rotary-cannon gun mount on a trailer, with a separate search radar and command shelter. The gun mount is visually inspired by real land-based counter-rocket gun systems, but uses no manufacturer names or trademarks. | Meets the brief's §2 note without mixing unrelated configurations. | Choose one specific real system as the exact reference. |
| R-14 | Fictional setting | A fictional country, base and adversary. No real nations, flags or conflicts. | Avoids political sensitivity and store-policy risk. | Real-world setting. |
| R-15 | Art approach (#11) | Licensed asset-store packs for environment/props, commissioned or bought models for the three weapons, in-engine VFX, licensed SFX libraries. | Realistic quality without a large art team. | Fully commissioned; fully in-house. |
| R-16 | Cinematic in MVP | Production storyboard plus an in-engine animatic in Milestone 2, built with Timeline from gameplay assets. Full-quality version in Milestone 8. | Avoids blocking the core prototype on expensive character animation. | Full cinematic in MVP. |
| R-17 | Version control — **now Confirmed as C-18** | Git with Git LFS for binary assets. Unity project on the C: SSD, not the H: HDD. | Safety and editor speed. | Plastic SCM / Unity Version Control. |
| R-19 | Failed missions | Pay interception credits only (no completion or bonuses). Dev stress-test runs pay nothing. | Effort is never wasted, but winning matters; the economy stays honest during testing. | Pay nothing on failure. |
| R-20 | Quality auto-detect | High if ≥7 GB RAM and ≥8 cores; Medium if ≥3.5 GB and ≥6 cores; else Low. Refine with a benchmark in M8. | Simple first pass; the Pad 8 detects as High. | GPU-name lookup tables. |
| R-18 | Support priorities | Simple per-weapon priority setting between waves (Most dangerous / Closest / Threat class). Assessed in playtests before committing. | Gives the player control over autonomy. | No setting; per-target designation. |

## 3. Open (needs user decision)

| ID | Question | Why it matters | Default if unanswered |
|---|---|---|---|
| O-01 | Team size, skills and roles? | Sets the realistic scope of art, code and the cinematic. | Assume one developer plus Claude Code, bought assets. |
| O-02 | Budget for assets, devices and services? | Decides bought vs commissioned art and audio. | Low budget; prefer asset-store packs. |
| O-03 | Target timeline / release window? | Milestone sizing. | No date; milestone-gated. |
| O-04 | Target audience age? | Google Play content rating, Families policy and which ad formats are allowed. | Teen-and-up (13+), not in the Families programme. |
| ~~O-05~~ | ~~Engine approval (R-01)?~~ | **Closed 2026-11-23: approved (C-17).** | — |
| O-06 | C-RAM identity approval (R-13)? | 3D model and HUD design. | Fictionalised battery. |
| O-07 | Ad network and billing provider? (brief §14 #9) | SDK, privacy and policy work. | Deferred to Milestone 7; Google Play Billing is the expected billing path. |
| O-08 | Analytics provider and consent approach? | Privacy policy and data-safety form. | Local debug logging only until decided. |
| O-09 | Which Pad 8 model (8 GB or 12 GB RAM)? | Memory budget on the test device. | Budget against 8 GB. |
| O-10 | A second, cheaper Android test phone? | Validating Medium/Low tiers. | Use a cloud device-testing service before release. |
| O-11 | Game title, branding and Android package name? | Store listing, UI. The package name is permanent after the first Play upload. | Working title "AirDefense"; package `com.placeholder.airdefense` (dev only). |
| ~~O-13~~ | **Closed 2026-11-23 (C-22): used at user direction.** Use the supplied Sketchfab C-RAM model (CC-BY, "Centurion C-RAM")? The required credit must show the author's Sketchfab profile link, and that profile's username contains the word "nazi". | That link would appear in the credits and store listing. | Hold. Decide before M3. Alternatives: another CC-BY or bought model, or commission one. |
| O-12 | Stay on Unity 6.6 ("Supported") or move to an LTS release before M2? | LTS gets longer bug-fix support. | Stay on 6.6 for M0–M1. |
| ~~O-14~~ | **Closed 2026-11-24: approved by the user ("Approve, start VS-1").** Visual vertical slice VS-1…VS-8 before M3b. | — | — |
| ~~O-15~~ | **Closed 2026-11-24: Barlow Semi Condensed (SIL OFL)**, accepted with "Start VS-2". Rendered with UGUI dynamic font text for now (see `HUD_UI_SPECIFICATION.md` §11); TextMeshPro SDF migration remains an option. | — | — |
| O-16 | ZU-23-2 hero asset route (VS-6)? | Hero quality vs cost. | In-house material pass first; buy or commission only if still short of the standard (cost approved by user). |
| O-17 | Threat models (M-06–M-11): licensed store models, CC-BY, or in-house simple shapes? | Threat readability; budget. | CC-BY/CC0 where good enough, licensed for the hero threats, approved per item. |
| O-18 | Bake lighting (lightmaps + probes) for the static base? | Night quality on mobile; adds a bake step to scene builds (slow on the GT 730 PC). | Yes, from VS-4. |
| P-01 / P-02 | Minimum supported device and the Balanced-tier reference device? | Tier targets and test coverage (`ANDROID_PERFORMANCE_BUDGET.md`). | A 2021–2022 mid-range phone (Snapdragon 7-series class, 6 GB) as minimum; to be confirmed. |

## 4. Assumptions

- A-01: The game is single-player.
- A-02: English is the only language at launch; text is kept localisation-ready.
- A-03: The user owns or will buy licences for all third-party assets used.
- A-04: Realistic military violence is limited to vehicles and equipment; no graphic depiction of injured people.

## 5. Decision history

| Date | Change |
|---|---|
| 2026-11-23 | Document created from the handoff brief. C-15, C-16 confirmed by user. All R-items proposed. |
| 2026-11-23 | User approved Unity 6 + URP and started M0. R-01 → C-17, R-17 → C-18, O-05 closed. Docs moved to `C:\dev\AirDefense`. |
| 2026-11-23 | Unity project created (6000.6.10f1, URP). Unapproved packages Unity added by default (Unity IAP, Unity Analytics) removed per C-12, along with unused 2D/multiplayer/navigation/collab packages. Added C-17a, O-12. |
| 2026-11-23 | User: show all three weapons in full, third-person view; muzzle flash looked animated, want real flash and bullet travel. Implemented C-21, C-22, turbulent fireball flash, glowing tracer streaks. |
| 2026-11-23 | User confirmed the ZU-23-2 sound built from their own Tunguska recording "sounds right" (C-20). |
| 2026-11-23 | User feedback: sound not realistic, add realistic muzzle flash and recoil, aim on the left. Implemented layered autocannon sound, muzzle flash (burst, flame tongue, light, smoke), per-barrel recoil + camera kick + muzzle climb (Screen shake setting), left-side aiming (C-19). |
| 2026-11-23 | M1 greybox built and verified on the Pad 8. User supplied three .glb weapon models; their metadata shows Sketchfab CC-BY-4.0 downloads by third-party authors. Recorded in ASSET_REGISTER. Added O-13. |
