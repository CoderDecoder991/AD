# Android Air-Defence Game — Claude Code Handoff Brief

**Document:** Game concept and pre-production brief  
**Version:** 1.0  
**Status:** Concept brief for review and planning  
**Target platform:** Android  
**Genre:** Realistic 3D air-defence command / action simulation  
**Business model:** Free-to-play, supported by optional advertising and in-app purchases

---

## 1. Purpose of this document

This document captures the intended game concept and the decisions discussed so far. It is a product-design handoff, not a request to immediately generate gameplay code.

Claude Code should read the complete brief, inspect the existing project/repository (if one exists), explain its understanding, identify important assumptions and risks, and then prepare the necessary design and project documents. Do not silently make major product decisions or begin a large implementation before the user has reviewed the understanding and proposed architecture.

The game should feel like a polished, realistic military air-defence experience on Android—not a simple arcade turret shooter. The central fantasy is that the player is operating an air-defence post, making rapid threat-priority decisions while several weapon systems protect the installation.

## 2. High-level game concept

The player defends a military installation against waves of incoming aerial and indirect-fire threats. The player begins with one available weapon system and earns in-game credits by completing missions and successfully defending the base. Credits are used to unlock additional weapon systems and improve the defence network.

The three planned weapon-system tiles are:

1. **ZU-23-2** — the starting weapon and the player's first manually controlled system.
2. **C-RAM** — initially locked; unlocked using earned in-game credits.
3. **2K22 Tunguska** — initially locked; unlocked using earned in-game credits.

At the beginning, only the ZU-23-2 is available. The other two tiles appear locked and communicate their unlock requirements. Once additional systems are unlocked, the player can switch between available systems during a mission.

### Important C-RAM design note

C-RAM describes a counter-rocket, artillery and mortar defence role/capability rather than one universally defined weapon model. Before finalising its 3D model, HUD identity and game behaviour, choose a specific visual reference or deliberately present it as a clearly labelled, fictionalised C-RAM battery. Do not mix unrelated real-world configurations without documenting the design choice.

## 3. Core gameplay rules

### 3.1 Player-controlled weapon

- The currently selected weapon is the player's primary/manual weapon.
- The player can aim, track and fire it using touch-friendly Android controls.
- The selected weapon should have a clear first-person, operator-station or close weapon-control view. The exact camera arrangement should be proposed during UI/UX planning and validated with a mobile prototype.
- The player must be able to understand which weapon is under manual control at all times.

### 3.2 Autonomous supporting weapons

- Any unlocked weapon that is not currently selected can operate automatically as a supporting defensive system.
- Supporting systems detect eligible threats, track them and engage automatically according to their designed gameplay rules.
- Automatic engagement must be visible and understandable: show target assignment, firing activity, ammunition status and engagement results in the HUD or a compact status panel.
- The player should not feel that the game is playing itself. Manual target prioritisation, switching decisions, threat management and limited resources should remain meaningful.
- Consider allowing the player to set simple support priorities between waves, such as prioritising the most dangerous target, the closest eligible target or a selected threat class. Treat this as a proposed feature and assess usability before committing to it.
- Define fair limits for autonomous systems, including target eligibility, ammunition, reload/cooldown, engagement range bands and simultaneous target capacity. These are game-balance values, not claims about exact real-world performance.

### 3.3 Weapon switching

- Display three persistent weapon tiles: ZU-23-2, C-RAM and 2K22 Tunguska.
- Locked tiles show the credit cost or progression requirement.
- Unlocked tiles are selectable during gameplay.
- Switching should be fast, clear and usable on a phone screen, without hiding urgent threat information.
- Switching transfers manual control to the selected system; the previous system becomes an autonomous support system if it is unlocked and operational.
- Provide clear visual feedback for selected, unlocked, locked, busy, reloading and unavailable states.

### 3.4 Credits and progression

- Players earn credits through gameplay: successful interceptions, mission completion, survival, objective protection and optional challenge goals.
- Credits unlock the additional weapon systems and may also be used for balanced upgrades.
- Do not make the initial game dependent on real-money purchases. A player should be able to earn the core weapon unlocks through play.
- Show a clear reward summary at the end of each mission, including credits earned, performance and progress toward the next unlock.
- Decide and document whether C-RAM and Tunguska unlock independently or in a fixed sequence. A simple sequential progression is a reasonable starting proposal, but it is not yet a confirmed decision.

## 4. Core gameplay loop

1. The player selects a mission or survival session.
2. A briefing explains the defence objective and the types of incoming threats.
3. The player enters the air-defence station and monitors the radar / threat display.
4. Incoming targets appear and the player takes manual control of the available weapon.
5. The player identifies priorities, aims and engages threats.
6. Unselected unlocked weapons provide autonomous support.
7. The player protects the installation and manages the consequences of any threat that gets through.
8. The session ends with a performance report and credits.
9. The player unlocks or upgrades systems and starts a more challenging mission.

The central tension should come from choosing what to engage, when to switch weapons and how to use the available defensive capacity—not from arbitrary difficulty spikes.

## 5. Threat and mission design

Potential threat classes for progressive introduction:

- Small and larger unmanned aerial vehicles (drones).
- Fixed-wing aircraft or other aerial targets.
- Cruise-missile-like threats.
- Incoming rockets, artillery and mortar threats represented in an accessible game format.
- Mixed waves in which more than one threat class arrives at the same time.

These are proposed game categories. Their visual identification, behaviour and difficulty should be designed for readable gameplay on a mobile display. Avoid overwhelming new players with several threat types at once.

### Suggested game modes

**A. Mission Mode**  
Structured missions with a specific defence objective, a short briefing and a defined completion condition.

**B. Survival Mode**  
Endless or progressively escalating waves. The player tries to survive for as long as possible and earns rewards based on performance.

**C. Multi-Threat Mode**  
Several threat classes arrive in overlapping windows. The player must prioritise threats and make effective use of the three weapon systems.

**D. Daily Challenge**  
A repeatable daily scenario with a defined challenge and bonus reward. Rewards must remain fair for players who do not make purchases.

These modes are proposed features. Claude Code should organise them into a sensible development roadmap rather than attempting to build all of them in the first milestone.

## 6. Damage, performance and player feedback

- The defended installation should have a visible condition/health state.
- A threat that penetrates the defence can damage the installation or an objective asset.
- Mission performance can consider successful interceptions, missed threats, objective damage, accuracy, response time and survival duration.
- Present a useful after-action report. It should explain what happened and how the player can improve, not just show a score.
- Keep feedback readable: clear radar marks, threat warnings, target status, impact indicators, weapon activity and concise audio alerts.
- Avoid excessive HUD clutter. The battlefield view must remain the visual focus.

## 7. First-launch cinematic story sequence

### Purpose

The first launch should begin with a short, cinematic story sequence that establishes atmosphere, urgency and the player's role. It should feel like the opening of a high-quality military thriller, not a long tutorial or a generic mobile-game splash screen.

**Target duration:** approximately 30–45 seconds.  
**Tone:** tense, grounded, realistic and cinematic.  
**Setting:** a fictional military air-defence installation at night.  
**Story premise:** an air-defence base receives an unexpected incoming-threat warning and moves to combat readiness.

### Suggested storyboard

**Shot 1 — Black screen / warning (0–5 seconds)**
- Begin in near silence or with faint night ambience.
- A distant radio transmission is heard, partially obscured by static.
- A warning tone begins; the air-raid siren rises sharply.
- Use sound to create anticipation before revealing the base.

**Shot 2 — Reveal the installation (5–12 seconds)**
- Cut to a wide cinematic view of a military air-defence installation at night.
- Searchlights, warning beacons and base lighting establish the environment.
- Distant flashes or a low rumble may suggest activity beyond the base, without revealing everything immediately.

**Shot 3 — Radar warning (12–20 seconds)**
- Move into the radar / command area.
- A radar operator notices multiple unidentified incoming contacts.
- Warning indicators activate and a short radio call confirms that the installation is moving to alert status.
- Show enough radar information to tell the audience that a threat has been detected, but do not overload the screen with technical text.

**Shot 4 — Base goes to alert (20–29 seconds)**
- Show personnel moving quickly to their stations.
- Red warning lights, doors, equipment start-up and controlled urgency build the tension.
- Cut briefly to the ZU-23-2 crew preparing the starting weapon system.

**Shot 5 — Introduce the player's weapon (29–38 seconds)**
- Use a dramatic but believable camera move toward the ZU-23-2.
- Reveal the weapon and its operator position in detail.
- The siren and radio ambience continue; the first threat is visible on the radar or in the distant sky.

**Shot 6 — Transition to gameplay (38–45 seconds)**
- The camera settles into the player's actual gameplay/control camera.
- The HUD appears in stages: radar, threat warning, weapon status and the ZU-23-2 selection tile.
- Control is handed to the player while the opening threat is still relevant.
- The first playable objective should be immediately understandable.

### Cinematic requirements

- The sequence should be achievable with reusable in-game 3D environments, props, lighting and camera animation where practical.
- Use cinematic camera moves, depth, atmospheric lighting, realistic materials and carefully layered sound.
- Avoid excessive text, exposition or voice-over. Let visuals and sound tell the story.
- The first-launch cinematic should be skippable. After it has been viewed once, provide a way to replay it from a menu or briefing section.
- The transition into gameplay must be seamless: do not make the player sit through an unnecessary second loading or title sequence.
- Plan a fallback for lower-end Android devices, such as reduced effects, shorter transition shots or a lower-resolution cinematic presentation.

## 8. Visual and audio direction

### Visual target

- Realistic 3D military environment with high-quality materials, believable lighting and convincing scale.
- Detailed weapon-system models, including moving components, crew positions, mounts, sensors and supporting equipment where appropriate.
- Convincing muzzle flashes, projectile trails, impact effects, smoke, debris and distant explosions.
- Day, dusk and night environments may be introduced progressively.
- Radar and command interfaces should look like functional military-inspired displays while remaining legible and simplified for gameplay.
- Use a restrained military colour palette and avoid exaggerated arcade-style effects.

### Realism and accuracy

- Use credible visual references for each named weapon system.
- Keep the weapon silhouettes and major visual features distinct; do not make all three systems look interchangeable.
- Do not invent exact real-world performance claims. Use clearly documented fictionalised gameplay values for balance.
- Any real-world technical details included in the game should be checked against reliable references during asset research.
- This is an entertainment game, not a real-world weapons training tool. Keep gameplay systems understandable and game-balanced.

### Audio target

- Air-raid sirens, warning klaxons, radar beeps, radio static and concise radio callouts.
- Distinct mechanical start-up and firing sounds for each weapon system.
- Environmental ambience: wind, distant engines, base machinery and night atmosphere.
- Distinct audio cues for detection, target warning, engagement, interception, impact and mission completion.
- Audio must be mixable and adjustable; sirens and weapon effects should not make radio or threat cues unintelligible.

## 9. Android controls and user experience

The game must be designed for touch screens from the beginning.

- Use large, clearly separated touch targets for weapon selection and primary actions.
- Keep the key radar and threat information visible without covering the main action.
- Provide adjustable sensitivity and optional simplified controls.
- Explain the first mission through short contextual prompts rather than a long tutorial wall of text.
- Include pause, settings, audio controls, graphics options and a clear way to resume.
- Support a range of Android screen sizes and aspect ratios.
- Plan for performance scaling rather than assuming every device can render maximum-quality effects.

Claude Code should propose the most suitable engine and rendering approach after inspecting the existing repository and considering realistic 3D visuals, Android performance, developer workflow and future content expansion. Do not select or migrate an engine without explaining the trade-offs first.

## 10. Monetisation strategy

### Business objective

The game is intended to generate revenue while maintaining player trust and long-term retention. Monetisation should be designed into the product structure from the beginning, but it must not make the core experience frustrating or unfair.

### Recommended revenue channels

**A. Rewarded advertisements — high priority**
- Optional ads in exchange for a clearly stated reward, such as a bonus-credit claim or a limited post-mission reward multiplier.
- The player must actively choose to watch the ad.
- Never interrupt a live engagement with an advertisement.

**B. Interstitial advertisements — use sparingly**
- Only at natural breaks, such as after a completed session, and subject to frequency limits.
- Do not show ads after every short wave or every minor action.
- Provide an ad-free purchase if this format is used.

**C. In-app purchases**
- Optional credit packs or convenience items.
- Cosmetic weapon finishes, camouflage, base appearance options and visual customisation.
- Premium progression track or season pass.
- Avoid selling overwhelming combat advantages. Core weapon unlocks should remain earnable through gameplay.

**D. Remove Ads purchase**
- A one-time purchase to remove forced interstitial advertising.
- Optional rewarded advertising can remain available because the player explicitly chooses it.

### Monetisation principles

- No pay-to-win design that makes free players unable to progress competitively.
- Show prices and rewards clearly before purchase.
- Avoid deceptive countdowns, misleading reward previews and pressure tactics.
- Do not use random paid loot boxes as a core revenue dependency.
- Protect purchase flows against accidental taps and provide clear confirmation.
- Follow current Google Play policies, applicable child-safety requirements and privacy requirements before release.
- Do not integrate real advertising or billing SDKs until the user approves the provider choice and the current SDK/policy requirements have been checked.

## 11. Retention and progression features

Potential features to support repeat play:

- Daily challenge with a clearly described objective.
- Weekly missions and longer-term goals.
- Weapon unlock progression and upgrade milestones.
- New bases and environments as progression rewards.
- Personal bests and detailed performance statistics.
- Optional leaderboard, once its online, moderation and privacy requirements are understood.
- Season/operation themes with cosmetic rewards and mission variations.

Do not build every retention feature in the first version. Prioritise a satisfying core loop and stable performance first.

## 12. Suggested first playable version (MVP)

The first playable milestone should prove the central idea without overbuilding the game.

### Include in the first playable milestone

- One polished military base environment.
- One playable mission or survival scenario.
- ZU-23-2 as the available starting weapon.
- A small set of readable incoming threat types.
- Manual aiming and firing with Android touch controls.
- Basic radar / warning interface.
- Basic installation damage and mission-end results.
- Credits earned from performance.
- Locked UI tiles for C-RAM and Tunguska, even if their full gameplay is scheduled for a later milestone.
- The first-launch cinematic, or a production-ready storyboard and technical plan if the full sequence would delay the core prototype.

### Later milestones

1. Add C-RAM, weapon switching and autonomous support behaviour.
2. Add 2K22 Tunguska and complete the three-system interaction.
3. Add mission variety, survival progression and additional threat classes.
4. Add upgrades, economy balancing and retention features.
5. Add monetisation only after the core game loop is tested and enjoyable.
6. Add polish, optimisation, accessibility, device testing and release preparation.

Claude Code should propose a realistic milestone sequence and identify dependencies. The user should be able to review scope before significant implementation begins.

## 13. Success measures

Plan analytics and test events for the following product measures, subject to privacy and platform requirements:

- First-session completion and tutorial completion.
- Day 1, Day 7 and Day 30 retention.
- Average session length and sessions per active player.
- Mission completion and failure rates.
- Weapon unlock progression and time to unlock.
- Optional rewarded-ad engagement and ad frequency.
- Purchase conversion and average revenue per daily active user.
- Crash rate, frame rate, load time and device-specific performance.

Do not optimise only for ad impressions or purchase conversion. Retention, player satisfaction, stability and fair progression are essential to a sustainable game.

## 14. Key decisions that still need validation

The following are not yet final decisions. Claude Code should list them in its understanding report and recommend practical defaults:

1. Game engine and rendering pipeline.
2. Camera style during normal gameplay: weapon-mounted, operator position, third-person tactical view, or a hybrid.
3. Whether C-RAM and Tunguska unlock sequentially or independently.
4. Exact threat roster and which threats appear in the first playable mission.
5. Whether the base has one shared health pool or several protectable assets.
6. Exact touch-control layout and whether aiming uses swipe, drag, virtual controls or a hybrid.
7. Whether the game is fully offline at launch, online, or hybrid.
8. Whether leaderboards, cloud saves and seasonal content are part of the first release or later updates.
9. Advertising and billing providers, after current policy and SDK checks.
10. Minimum Android version and target device-performance tiers.
11. Art and audio production approach, including what can be produced in-engine versus sourced or commissioned.

Do not invent answers and present them as user-approved. Label decisions as **Confirmed**, **Recommended**, or **Open** in project documentation.

## 15. Instructions for Claude Code

Follow this workflow carefully.

### Phase 1 — Read and report understanding

1. Read this entire document.
2. Inspect the repository and current project files, if present. Do not overwrite or reorganise existing work without understanding it.
3. Return a concise but complete understanding report covering: game concept, player loop, three weapon systems, automatic support behaviour, cinematic opening, monetisation, MVP scope, risks and open decisions.
4. Separate confirmed requirements from your recommendations and assumptions.
5. Identify any contradictions, technical risks or missing information that could materially change the architecture.

### Phase 2 — Prepare the project documentation and structure

After the understanding report, prepare the necessary pre-production files, adapting to the repository's existing conventions. At minimum, consider:

- `README.md` — project purpose, setup status and documentation index.
- `GDD.md` — game design document with gameplay rules and progression.
- `PRD.md` — product requirements, user experience and monetisation requirements.
- `TECHNICAL_ARCHITECTURE.md` — proposed engine, systems, data flow, save model and platform approach.
- `DECISIONS_AND_ASSUMPTIONS.md` — confirmed decisions, recommendations, open questions and decision history.
- `CINEMATIC_OPENING.md` — detailed storyboard, shot list, audio cues and transition requirements.
- `WEAPON_SYSTEMS.md` — visual identity, gameplay role, unlock state, manual/automatic behaviour and balancing fields for each system.
- `ECONOMY_AND_MONETISATION.md` — credits, rewards, upgrade economy, ads and purchase design.
- `UI_UX_FLOW.md` — screen flow, HUD layout requirements and touch-control principles.
- `ASSET_REGISTER.md` — required 3D models, animations, environments, VFX, audio and UI assets, with status and source/licensing notes.
- `MILESTONES_AND_TEST_PLAN.md` — implementation sequence, acceptance criteria, device tests and performance checks.

Create or adapt a sensible folder structure for documentation, scenes, scripts, assets, audio, UI, tests and configuration only after checking the existing project. Do not create empty folders or placeholder files without a stated purpose.

### Phase 3 — Review gate

Present the proposed architecture, folder structure, milestone plan and unresolved decisions to the user. Do not begin substantial gameplay implementation, install unapproved paid services, add monetisation SDKs, or replace project foundations until the user has reviewed the plan.

### Phase 4 — Implementation after approval

Once approved, implement in small milestones. Keep the project buildable, test each milestone on Android, document changes and avoid implementing multiple large systems at once. Prioritise a stable, enjoyable ZU-23-2 prototype before expanding to all three weapon systems and monetisation.

### General working rules

- Do not assume the user wants code or features that are not in this brief.
- Do not silently change the core concept: three weapon tiles, ZU-23-2 starting system, credit-based unlocks, manual switching and autonomous support weapons are central requirements.
- Do not use real-money purchases as a requirement for core progression.
- Keep all gameplay performance values data-driven and clearly labelled as game-balance values.
- Use placeholder art only when clearly labelled; do not present it as final-quality realistic art.
- Keep asset licensing and attribution records.
- Do not expose secrets, API keys or signing credentials in source control.
- Explain technical trade-offs in plain language so the user can make informed decisions.

---

## 16. Product vision in one paragraph

Create a visually realistic Android air-defence command game in which the player begins operating a ZU-23-2, earns credits to unlock C-RAM and 2K22 Tunguska, and switches between unlocked systems while the unselected systems autonomously support the defence. The experience should combine tense radar-led decision-making, cinematic military presentation, believable weapon and environment visuals, progressively challenging missions, fair long-term progression and carefully balanced optional monetisation. The opening should establish the story through a 30–45 second night-time air-raid-siren sequence that transitions directly into the first playable defence.
