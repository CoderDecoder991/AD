# Product Requirements Document

Decision IDs refer to [DECISIONS_AND_ASSUMPTIONS.md](DECISIONS_AND_ASSUMPTIONS.md).

## 1. Product summary
A free-to-play, realistic 3D air-defence game for Android. The player runs a ZU-23-2 at first, earns credits to unlock C-RAM and the 2K22 Tunguska, and switches between them while unselected systems defend autonomously.

## 2. Goals
| Goal | Measure |
|---|---|
| A satisfying core loop | First-mission completion ≥ 80% of players who start it; D1 retention target set after the first playtest |
| Fair progression | C-RAM unlockable in about 2–3 hours of play; Tunguska in about 6–8 hours total, without purchases |
| Stable performance | 60 fps sustained on High tier (Pad 8), 30 fps on Low tier; crash-free sessions ≥ 99.5% |
| Sustainable revenue | Optional, trust-preserving monetisation added after the loop is proven (M7) |

## 3. Non-goals (first release)
- Multiplayer, online PvP, chat or user-generated content.
- Real-world training accuracy.
- Leaderboards, cloud save or seasons (R-08, post-launch).
- iOS (not ruled out later; architecture avoids Android-only game logic).

## 4. Target player
- Fans of military simulation and strategy who want short, intense mobile sessions (5–15 min).
- Age: **Open (O-04)**. Default assumption: 13+, outside Google Play's Families programme.

## 5. Functional requirements

### 5.1 Gameplay (MVP = Milestones 1–2)
| ID | Requirement | Milestone |
|---|---|---|
| F-01 | Touch aim and fire for the ZU-23-2 with adjustable sensitivity. | M1 |
| F-02 | Radar panel showing contacts, threat class symbols and the selected target. | M1 |
| F-03 | At least two threat classes with distinct visuals and behaviour. | M1 |
| F-04 | Installation health with visible damage stages. | M1 |
| F-05 | Mission success and failure conditions, and a results screen. | M1 |
| F-06 | Credits earned and saved between sessions. | M2 |
| F-07 | Three weapon tiles; C-RAM and Tunguska locked with cost shown. | M2 |
| F-08 | First-launch cinematic (animatic quality in M2), skippable, replayable from the menu. | M2 |
| F-09 | Pause, settings (audio, graphics, sensitivity), resume. | M2 |
| F-10 | C-RAM unlock, switching, autonomous support. | M3 |
| F-11 | Tunguska with gun and missile modes. | M4 |
| F-12 | Survival and Multi-Threat modes, more threats and missions. | M5 |
| F-13 | Upgrades, Daily Challenge. | M6 |
| F-14 | Rewarded ads, optional interstitials, IAP, Remove Ads. | M7 |

### 5.2 UX requirements
- Touch targets ≥ 48 dp; weapon tiles ≥ 64 dp.
- Radar and threat warnings never covered by other UI during play.
- Layout adapts from 3:2 tablets (Pad 8) to 20:9+ phones, with safe-area support for notches.
- Contextual onboarding prompts instead of text walls.
- Accessibility: subtitles for radio callouts, colour-blind-safe threat colours (shape plus colour), separate volume sliders, an optional reduced screen-shake setting.

### 5.3 Monetisation requirements (M7, see [ECONOMY_AND_MONETISATION.md](ECONOMY_AND_MONETISATION.md))
- Rewarded ads are always optional, with the reward stated before watching.
- No ad ever shown during a live engagement.
- Interstitials only after completed sessions, frequency-capped, removable by purchase.
- All prices and contents shown before purchase; purchase confirmation required.
- Core weapon unlocks always earnable through play.

### 5.4 Data and privacy
- Offline at launch (R-07). Save data stays on the device.
- No analytics or ad SDK until approved (O-07, O-08). Before any is added: privacy policy, Play Data safety form, consent flow where required.

## 6. Platform requirements
| Item | Requirement |
|---|---|
| OS | minSdk 26 (Android 8.0), targetSdk = current Play requirement (R-10) |
| CPU arch | ARM64 only |
| Graphics | Vulkan, GLES 3.x fallback (R-02) |
| Download size | Base AAB within Play limits; large content via Play Asset Delivery if needed |
| Orientation | Landscape only |
| Quality tiers | Low / Medium / High, auto-detected with a manual override (R-11) |

## 7. Release criteria (summary)
Full list in [MILESTONES_AND_TEST_PLAN.md](MILESTONES_AND_TEST_PLAN.md).
- All M1–M8 acceptance criteria met.
- Performance budgets met on the Pad 8 and on at least one Low-tier device.
- Content rating questionnaire, privacy policy and Data safety form complete.
- Asset register complete, with every licence recorded.
