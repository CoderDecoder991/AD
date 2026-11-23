# Economy and Monetisation

> All amounts are **starting balance values** for tuning, kept in data files. Monetisation is built in **Milestone 7**, after the core loop is proven (C-13). No SDK is integrated before the user approves a provider (C-12, O-07).

## 1. Currency
- **Credits** — the only currency at launch. Earned through play; spent on unlocks and upgrades.
- No premium currency at launch. Real-money items are priced directly, which avoids confusing exchange rates.

## 2. Earning credits

| Source | Starting value |
|---|---|
| Mission completion | 300 (scales with mission tier ×1.0–×2.5) |
| Interception | 10 (small drone) to 60 (cruise missile/aircraft) |
| Installation health bonus | Up to +30% of the mission total at 100% health |
| Accuracy bonus | Up to +15% |
| First-time completion | +200 |
| Optional challenge goals (e.g. "no damage", "under 3 min") | 100–250 each |
| Survival mode | Per wave survived, rising with wave number |
| Daily Challenge (M6) | Fixed bonus, the same for paying and non-paying players |

Target average: ~500 credits per early mission (~8–10 min), ~800 per mid-game mission.

## 3. Spending credits

| Item | Cost | Prerequisite |
|---|---|---|
| C-RAM unlock | 4,000 | — (R-04) |
| 2K22 Tunguska unlock | 10,000 | C-RAM unlocked |
| Upgrades (M6) | 500–3,000 per level, 3–5 levels each | Weapon unlocked |
| New base/environment (M5+) | Progression reward or credits | Mission progress |

Pacing check: C-RAM ≈ 8 early missions (~1.5–2.5 h). Tunguska ≈ 12–14 more missions (~4–5 h more). This matches the PRD goal and is validated in M6 balancing.

**Upgrade rules:** Every upgrade has a hard cap, and upgrade bonuses are small (≤ +25% per stat at max), so skill and decisions stay more important than spending.

## 4. Monetisation channels (M7)

### 4.1 Rewarded ads — priority
- **Double mission credits** after a completed mission (once per mission).
- **Daily bonus claim** (once per day).
- **Second chance** after failure: retry the last wave with 50% health (once per mission, optional).
- Always shown as a button with the exact reward. Never auto-plays. Never during play.

### 4.2 Interstitial ads — sparingly
- Only after a completed session, never after a single wave.
- Frequency cap: at most 1 per 3 completed sessions and at least 10 minutes apart. Never in the first session or the first day.
- Removed entirely by the Remove Ads purchase.

### 4.3 In-app purchases
| Product | Type | Notes |
|---|---|---|
| Remove Ads | One-time | Removes interstitials. Rewarded ads stay available by choice. |
| Credit packs | Consumable | Optional shortcut. Never the only route to anything. |
| Weapon finishes / camouflage | One-time cosmetic | No gameplay effect. |
| Base appearance packs | One-time cosmetic | No gameplay effect. |
| Operation pass (post-launch) | Seasonal | Free track plus a paid cosmetic track. |

### 4.4 Not included
- Paid random loot boxes (C-11).
- Energy/stamina timers that block play.
- Fake countdowns, misleading "discounts" or pressure pop-ups.
- Combat advantages for sale beyond time-saving credits.

## 5. Purchase flow safeguards
- Two-step confirmation: product screen with full price, then Google Play's own purchase sheet.
- A clear "Restore purchases" entry in settings.
- Tap areas spaced to avoid accidental taps. No purchase buttons near gameplay controls.

## 6. Provider options (to decide in M7, O-07)
- **Billing:** Google Play Billing (required for digital goods on Play), most likely through Unity IAP.
- **Ads:** candidates include Google AdMob, Unity LevelPlay and AppLovin MAX. To compare at decision time: current SDK versions, policy requirements, Families-programme rules (if O-04 means under-13 players), consent requirements and revenue terms.

## 7. Economy telemetry (when analytics is approved, O-08)
Credits earned/spent per session, time to each unlock, upgrade purchase order, rewarded-ad acceptance rate, interstitial frequency per user, purchase conversion and refunds.
