# Android Air-Defence Game — Claude Code Handoff Brief

**Document:** Game concept and pre-production brief  
**Version:** 1.1  
**Status:** Updated handoff brief; includes gameplay concept and screenshot-driven visual production requirements  
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


---

# 17. Visual production review — current gameplay screenshot

**Reference:** The user-provided gameplay screenshot showing Wave 1 at a night-time installation, the ZU-23-2 view, radar, weapon tiles, aim instruction and fire control.

**Assessment:** The screenshot is useful evidence that a basic playable prototype and HUD layout exist. It is not yet an acceptable production visual benchmark. It reads as an early prototype because the environment, weapon presentation, lighting, effects and interface do not yet have a consistent finished-art direction. Do not treat this as a failed project or discard working gameplay automatically; preserve and audit the working systems before changing them.

## 17.1 Specific issues visible in the screenshot

1. **Development diagnostics are visible to the player.** The upper-right FPS, render resolution, scale, graphics API and GPU name, plus the development-build label, must not appear in normal player-facing builds. Keep diagnostics available in a developer/QA mode only.
2. **Android system UI is visible.** The phone status bar and bottom navigation/gesture area interrupt the game presentation. Plan a landscape, immersive gameplay presentation with correct handling of display cut-outs, safe areas and gesture navigation. Do not put essential controls under system gestures.
3. **The scene is too dark and visually flat.** Large areas of the foreground and installation lose detail in shadow. The sky/horizon occupies a lot of the composition without enough readable threat or environmental interest. Improve exposure, contrast and visual hierarchy without making a night scene look artificially bright.
4. **Searchlight beams look like flat translucent polygons.** Replace the obvious crossed geometric strips with believable spotlights and, where performance allows, restrained volumetric cones interacting with atmospheric haze/dust. Light beams should have a clear source, falloff and believable direction.
5. **The base environment looks under-dressed and low-detail.** The visible buildings, terrain, ground, revetment and distant background need stronger scale, material definition, silhouette variety and contextual detail. The installation should feel like a designed, functional military site rather than a few disconnected structures placed on a flat area.
6. **The weapon does not yet read as a hero asset.** The ZU-23-2 should have a clear, recognisable silhouette, convincing proportions, material response and visible mechanical structure. Improve the model and presentation rather than relying on darkness to hide missing detail.
7. **Radar presentation is too generic.** The circular rings and a single contact do not yet communicate a polished operational display. Create a designed radar visual language with an animated sweep, clear range rings, readable contacts and threat states. Keep it stylised enough to read on a phone; do not add decorative clutter.
8. **The HUD feels assembled from separate prototype elements.** The large red FIRE circle, weapon tiles, radar and status bars do not share a strong typography, spacing, colour or component system. Create a consistent interface design system and adapt it to multiple phone aspect ratios.
9. **The central tutorial instruction competes with the battlefield.** Move onboarding into a short contextual prompt with a clear visual cue. It should disappear once the player completes the action and must not permanently cover the weapon or target view.
10. **Threat readability needs improvement.** The wave title says contacts are inbound, but the threat must be visually and tactically understandable. Provide clear radar-to-world correspondence, readable distance/status cues and an obvious target indication without filling the sky with markers.

## 17.2 What to preserve and what to redesign

**Preserve after code/project audit:**
- The existing playable loop, if functional.
- Working ZU-23-2 aiming and firing input.
- The three weapon tiles and their locked/unlocked concept.
- Current target detection, wave, credit and health logic if it works correctly.
- Any functional weapon switching or automated-support logic already implemented.

**Redesign or substantially upgrade:**
- The complete visual presentation of the base and battlefield.
- The ZU-23-2 model, materials, animation and camera framing where needed.
- Lighting, shadows, atmosphere, searchlights and post-processing.
- Projectile, muzzle flash, impact, smoke and explosion effects.
- Radar graphics and the full HUD component system.
- First-launch camera language and transition into gameplay.
- Player-facing build presentation, including hiding development diagnostics.

Do not delete or rewrite working systems merely to make the scene look different. Separate visual upgrades from gameplay changes and keep changes testable.

# 18. Visual production standard — target for the game

## 18.1 Overall art direction

Target **grounded, photorealistic military-simulation presentation optimised for Android**. The game should have believable scale, materials, lighting, camera work and effects. It should not look like a cartoon, a generic mobile tower-defence game or a sci-fi space shooter.

The target is not “maximum PC graphics at any cost.” The target is a coherent, premium mobile experience that remains stable on the agreed Android device tiers. Visual quality must come from strong art direction, well-made assets, good composition and disciplined performance budgets—not simply from increasing resolution, shadow quality or particle counts.

Use a fictional installation and fictional mission narrative unless the project later obtains the references and permissions needed for a specific real-world location. Named weapon systems should have researched, recognisable visual identities. Record references and asset provenance in the asset register.

## 18.2 Environment and level art

The first production-quality environment should be one complete, believable air-defence installation rather than a large but empty map. Build it as a coherent place with a clear layout and visual landmarks.

Include, as appropriate to the chosen setting:
- A properly sized weapon emplacement/platform with believable ground contact and surrounding protection.
- Concrete, compacted soil, gravel, mud or other setting-appropriate ground surfaces with variation and visible material response.
- Revetments/berms, barriers, sandbags or other plausible site-protection elements where appropriate to the fictional base design.
- A small number of detailed operational buildings such as a command/radar building, hangar or equipment shelter.
- Perimeter fencing, gates, service roads, lighting poles, cables, utility equipment, storage objects and other contextual props.
- Distant terrain or a skyline that gives the scene depth and scale.
- A composed sky and horizon with atmospheric perspective; avoid a large empty background unless the mission intentionally calls for it.

Use a deliberate asset-density plan: hero objects receive the highest detail; mid-distance objects support recognition; far-distance objects can use simpler geometry or silhouettes. Add environmental detail only where it contributes to scale, believability, navigation or composition. Avoid random clutter.

The base should feel active even when no enemy is visible. Use subtle ambient movement such as rotating searchlights, warning beacons, wind-driven elements, antenna motion or distant vehicle/crew activity where appropriate and affordable on the target device.

## 18.3 Weapon asset quality

Treat the currently selected weapon as a hero asset because it occupies a major part of the player's attention.

For the ZU-23-2, require:
- A recognisable and proportionally consistent overall silhouette based on approved visual references.
- Clearly modelled barrels, mount, platform, sights and visible mechanical assemblies appropriate to the chosen view.
- Convincing metal, painted surfaces, worn edges and material variation; avoid a single flat grey material.
- Correct grounding, scale and contact shadows so the weapon does not appear to float or sink into the emplacement.
- Visible, purposeful movement when aiming and firing, with no clipping or parts moving independently without reason.
- A camera angle that allows the player to understand the weapon and its target direction without losing aim visibility.

For C-RAM and 2K22 Tunguska, create separate visual identity briefs before asset production. Do not use one generic turret model with different labels. The C-RAM configuration still requires an explicit selected visual reference/configuration as noted in Section 2.

Use a game-ready asset pipeline where appropriate: detailed source model, optimised in-game mesh, UVs, physically based materials, normal/roughness/metallic maps as supported by the engine, collision setup, and level-of-detail variants. If the existing project cannot create or import a required asset automatically, identify the external modelling/texturing task honestly and provide a specification; do not pretend a placeholder is a final asset.

## 18.4 Materials and surface detail

Use physically based material principles supported by the selected engine:
- Metals should have believable roughness and reflectivity, not mirror-like shine everywhere.
- Painted surfaces should show controlled roughness, subtle edge wear and sensible colour variation.
- Concrete, soil, gravel, fabric, rubber and glass should look materially distinct.
- Large surfaces need subtle variation so they do not appear like uniform flat colours or noisy procedural textures.
- Normal and roughness detail should support close viewing without creating visual noise at phone resolution.
- Avoid excessive dirt, scratches or grunge. Wear should match the environment and the object's use.

Check materials under the actual day/night lighting conditions used in gameplay. A material that looks good in a bright editor viewport may disappear or become muddy at night.

## 18.5 Lighting, atmosphere and rendering

The first scene is a night mission, so preserve the night-time mood while keeping important gameplay objects readable.

Requirements:
- Establish a clear lighting hierarchy: ambient/environment contribution, key lights for important shapes, practical base lights, warning beacons and searchlights.
- Keep the weapon, emplacement and key target areas legible. Do not crush the foreground into featureless black.
- Use shadows selectively and consistently. Avoid unstable flicker, excessive aliasing or shadows that consume the entire performance budget.
- Searchlights must be represented by real light sources with believable cones/falloff. Add restrained volumetric light only if it performs acceptably; never use obvious flat translucent triangles as a shortcut.
- Use fog/haze sparingly to create depth and show light beams, not to obscure targets.
- Use exposure, tone mapping, colour grading and bloom carefully. Preserve detail in both bright lights and dark surfaces; do not overuse bloom or sharpen/noise effects.
- Ensure the scene remains readable on common phone displays at different brightness levels.

Provide scalable graphics settings for expensive features. If a feature is disabled on a lower tier, preserve the underlying visual clarity through simpler alternatives.

## 18.6 Animation and visual effects

The battlefield must feel alive, but effects must remain readable and performance-conscious.

Create or plan distinct, reusable effects for:
- Weapon traverse/elevation movement and firing recoil.
- Muzzle flash and barrel smoke appropriate to the weapon.
- Projectile/tracer visuals that communicate direction and flight without becoming oversized glowing lines.
- Impact sparks, dust, debris, smoke and fire scaled to the type of impact.
- Aircraft/drone movement and readable incoming-threat silhouettes.
- Searchlight rotation, warning beacon flashes, radar sweep and environmental activity.
- Hit, interception, near-miss and objective-damage feedback.

Effects should have a clear start, duration and end. Avoid persistent smoke or explosions that accumulate without limits. Avoid using the same explosion for every type of target. Test effects against a dark sky and against the terrain so they remain visible without washing out the scene.

## 18.7 Camera and composition

The normal gameplay camera should make the player feel physically present at the weapon station while keeping the incoming threat and aiming space visible. During pre-production, evaluate a small number of camera options (operator/weapon-adjacent, over-weapon third-person, or a controlled hybrid) on a phone-sized screen. Recommend one and document why it best supports aiming and situational awareness.

Requirements:
- Avoid framing that hides most of the weapon or makes it too small to recognise.
- Keep the target area and aiming direction clear.
- Avoid camera clipping through the weapon, terrain or nearby structures.
- Use controlled camera movement and subtle recoil; do not add distracting shake during routine aiming.
- The opening cinematic may use more dramatic camera moves, but the handoff to player control must settle smoothly into the exact gameplay camera.
- Test composition in portrait-independent landscape aspect ratios and on devices with different screen proportions; do not assume the screenshot's aspect ratio is universal.

## 18.8 Radar and HUD design system

Replace the prototype-style HUD with a consistent, legible military-inspired command interface. The HUD should support decisions, not compete with the 3D battlefield.

Create a documented visual system covering:
- Typography: one coherent type family or compatible type pairing, clear hierarchy, readable sizes and consistent numeric styling.
- Colour semantics: define a restrained palette for normal status, friendly/system status, caution, threat, critical warning, selected state and unavailable/locked state. Never rely on colour alone; pair colour with shape, icon or text.
- Spacing and alignment: consistent margins, padding, tile sizes, corner treatment and panel opacity.
- Icons and symbols: one consistent icon style; avoid mixing unrelated line, filled and decorative styles.
- Panel backgrounds: enough contrast over bright or dark scenery, with restrained transparency and no excessive visual obstruction.
- Touch areas: controls must be large enough for reliable phone use and spaced to prevent accidental taps.

**Radar requirements:** animated sweep, readable range rings, clear contact symbols, selected target state, threat classification at a simple gameplay level, and a clear link between a radar contact and its world-space target. Radar animation should communicate information, not be decorative only.

**Weapon tiles:** make the selected, unlocked support, locked, reloading, unavailable and damaged states visually distinct. Show the weapon name, state and one or two critical status values. Do not cram every statistic into the tile.

**Fire/aim controls:** keep the primary action prominent but proportionate. The current oversized red FIRE button should be reviewed for visual balance, reachability and accidental-touch risk. Provide visual pressed/disabled/reloading feedback. Keep the aiming interaction zone clear and avoid permanent tutorial text over the centre of the battlefield.

**Debug information:** FPS, resolution, render scale, graphics API, GPU name, profiler information and development-build labels belong only in a developer overlay. They must be disabled in the normal release UI and must never overlap player HUD elements.

**Android presentation:** use landscape immersive presentation during gameplay, handle system bars and display cut-outs correctly, and reserve safe margins for gesture/navigation areas. Provide a clear pause/settings route. Do not hide system UI in a way that prevents a player from using normal Android navigation when required by platform conventions.

## 18.9 Audio-visual integration

Visual events and sound cues must correspond. Radar contact, warning state, weapon firing, hit, interception, incoming impact and mission result should have recognisable but non-fatiguing audio cues. Use spatial placement where practical. Keep critical radio and warning information audible above ambience and weapon sound. Provide separate volume controls for master, music, effects and radio/alerts where feasible.

# 19. Visual quality tiers and Android performance

Claude Code must inspect the actual engine, renderer, project configuration and current test devices before recommending exact settings. Do not assume the GPU shown in the screenshot represents every target device, and do not treat the screenshot's reported frame rate as proof of stable performance.

Create at least three proposed quality tiers, then validate them on real Android hardware:

- **Performance:** reduced shadow distance/resolution, simpler atmosphere and particles, fewer dynamic lights, reduced render scale where necessary, and lower-cost materials/effects while preserving target readability and control responsiveness.
- **Balanced:** intended default for the broadest supported device range; stable frame pacing, good lighting and effects with controlled scene complexity.
- **High:** enhanced shadows, atmosphere, effects and render scale for capable devices, without changing gameplay fairness or visual information.

Before setting numeric targets, establish the minimum supported device profile and expected device range with the user. Then record:
- Stable frame-rate target and frame pacing, not just a peak FPS reading.
- Resolution/render-scale behaviour and memory usage.
- CPU/GPU frame time, thermal behaviour and battery impact during a representative long session.
- Loading time and memory spikes when a wave or effects-heavy scene begins.
- Performance with several threats, multiple weapons firing, smoke and explosions active at once.
- Safe fallback behaviour if a device cannot sustain the selected tier.

Prioritise stable and responsive gameplay over expensive visual effects. A consistent 30 FPS may be a better supported-device target than unstable 60 FPS; the actual target must be chosen after device profiling. Do not advertise a frame rate or device compatibility that has not been tested.

# 20. Required visual pre-production deliverables

Before a large visual rewrite, Claude Code should inspect the current project and create or update these documents in the repository:

1. `SCREENSHOT_VISUAL_AUDIT.md` — observations from the supplied screenshot, prototype strengths, visual defects, severity, likely causes to investigate and recommended fixes. Clearly distinguish observed facts from hypotheses.
2. `VISUAL_PRODUCTION_STANDARD.md` — the approved art direction and standards from Sections 17–19, adapted to the actual engine and project.
3. `HUD_UI_SPECIFICATION.md` — radar, weapon tiles, aim/fire controls, status indicators, typography, palette, safe areas, responsive layouts and HUD states.
4. `ENVIRONMENT_ART_BRIEF.md` — the first base layout, hero/mid/far asset groups, lighting plan, composition and atmosphere.
5. `WEAPON_ASSET_BRIEFS.md` — individual visual requirements for ZU-23-2, selected C-RAM configuration and 2K22 Tunguska, with reference/provenance fields.
6. `VFX_AND_ANIMATION_SPEC.md` — weapon movement, muzzle effects, projectiles, impacts, explosions, radar sweep, searchlights and environmental animation.
7. `ANDROID_PERFORMANCE_BUDGET.md` — target device profiles, quality tiers, profiling method, performance risks and measured results as they become available.
8. `VISUAL_ACCEPTANCE_CHECKLIST.md` — measurable review checks for the first production-quality vertical slice.

The asset register should identify each asset as one of: **existing and usable**, **needs improvement**, **new asset required**, **temporary placeholder**, or **external artist/tool required**. Include source/reference, licence status, owner and implementation status where known.

# 21. First visual vertical slice — scope and acceptance criteria

Do not attempt to upgrade every mission, weapon and environment at once. Build one vertical slice that proves the intended production standard.

**Vertical-slice scope:** one night-time military installation, the ZU-23-2 as the hero weapon, one representative incoming threat, the radar/HUD, core aiming/firing, one complete interception/impact sequence, and the cinematic-to-gameplay transition. C-RAM and Tunguska may remain represented by their existing locked/unlocked UI states until their individual visual/gameplay milestones.

The slice is ready for user review when:
- The player-facing gameplay view contains no debug text, profiler data, GPU name or development watermark.
- The scene reads clearly as one coherent military installation with believable depth, scale and environmental composition.
- The ZU-23-2 silhouette and materials are recognisable and visibly more detailed than the current prototype presentation.
- The weapon sits correctly in the environment and its aiming/firing animation has no obvious clipping or floating.
- Night lighting preserves a cinematic mood while keeping the weapon, emplacement and incoming threat readable.
- Searchlights look like light sources rather than flat translucent polygon strips.
- The radar sweep and contacts communicate threat activity and correspond to the visible target.
- The HUD uses a coherent type, spacing, colour and component system; key controls remain readable on supported phone aspect ratios.
- The player can aim and fire without tutorial text or HUD panels obscuring the important view.
- At least one complete threat engagement has clear detection, target indication, player response, projectile/effect feedback and an understandable result.
- Performance is measured on the agreed Android test devices, with no unsupported claims based only on editor preview or one screenshot.

Use side-by-side screenshots or short recordings from the same camera positions and settings to compare the prototype with the revised vertical slice. Provide the user with a concise change report explaining what is complete, what remains placeholder and what requires external art production.

# 22. Additional instructions for Claude Code — screenshot-led workflow

1. Treat the supplied gameplay screenshot as a **prototype-state reference and review input**, not as an approved final-art reference.
2. Inspect the actual project before proposing fixes. Identify the engine/version, render pipeline, scene structure, current assets, materials, lights, camera, HUD implementation and Android build configuration.
3. Produce `SCREENSHOT_VISUAL_AUDIT.md` first. For every issue, label it as **Observed**, **Likely cause to investigate**, or **Proposed fix**. Do not state a suspected technical cause as fact until you have inspected the project.
4. Separate code-addressable presentation problems from asset-production problems. For example, a HUD layout or debug overlay may be fixable in project code; a genuinely detailed ZU-23-2 model may require a suitable licensed model or a 3D modelling workflow.
5. Do not claim that changing colour grading, adding bloom, increasing resolution or applying a generic material makes a scene production-grade. Address geometry, composition, material quality, lighting, animation, effects and interface as a joined art direction.
6. Do not silently replace the current engine, render pipeline, project architecture or gameplay systems. Document the trade-offs and request approval before major migration or destructive changes.
7. First present the audit, asset gap list, proposed visual pipeline, device-performance tiers and vertical-slice plan. Wait for the user's review before major asset purchases, paid services, engine migration or broad implementation.
8. Implement the vertical slice in small, reviewable steps. Preserve a working build at each milestone and maintain a change log.
9. Clearly label temporary geometry, generated placeholder textures, stock assets and unverified models. Record licences and usage restrictions. Do not use unlicensed images/models/audio in a release build.
10. Do not represent editor screenshots, mock-ups or placeholder assets as the final Android result. Validate the actual build on physical devices.

**Updated priority:** The existing prototype demonstrates the initial gameplay layout. The next production milestone should be a visual-quality vertical slice that upgrades the night installation, ZU-23-2 hero asset, radar/HUD, lighting and threat engagement presentation while preserving the working gameplay foundation wherever possible.
