# Shop + Soul Perks + Tooltips Context (Codex Handoff)

This document is a quick handoff for future Codex chats working on:
- shop popup flow
- soul perks and hearts UI
- hover tooltip system

Keep this file updated when logic changes.

## Recent gameplay flow updates (updated 2026-02-15)

- Final stage victory flow now shows `Victory!` popup (via `WitchIsDeadPopup`) and then returns to base (`stage 0`) by calling `RunLevelManager.InitializeRun()`.
- It no longer jumps to `stage 1` after the final victory popup.
- Added optional debug cheats in `RunLevelManager`:
  - configurable currency grant for new run start (`debugBonusCoins`, `debugBonusSouls`)
  - hotkeys to skip stage: `Shift + D` or `Shift + L`
  - final popup duration setting: `finalVictoryPopupDuration`

### Stage spawner assignment note (important)

- Runtime stage activation uses `RunLevelManager.stageSpawners[index]`, where `index = stage - 1`.
- If total stages include 9th stage, `stageSpawners` must contain 9 entries and `Element 8` must be assigned (e.g. `EnemyTopSpawner_9`).
- `EnemyTopSpawner.enemyZone` being empty does not block spawning in current implementation.

## Core runtime flow

1. `ShopKeeperManager` generates a per-run shop schedule for between-level stages.
2. `RunLevelManager` opens shop popup (`SoulShopKeeperPopup`) in base or stage-clear mode.
3. `SoulShopKeeperPopup` builds slots from `coinItems[]` and `soulItems[]`.
4. `ShopItemSlotUI` reads a `ShopItemDefinition` and:
   - shows icon/name/price
   - validates purchase availability
   - performs purchase on click
5. Permanent soul perks are managed by `SoulPerksManager`.
6. `SoulPerksPanelUI` listens to `SoulPerksManager.OnPerksChanged` and redraws hearts.
7. Tooltip triggers on hover use shared runtime tooltip UI (`HoverTooltipUI`).

## Current interlevel shop schedule (updated 2026-02-14)

- Base (`stage 0`) has a shop entry in schedule.
- Between-level shop stages are limited to `1..(totalStages - 1)` (for 9 stages: `1..8`).
- Fixed guaranteed shop points:
  - after stage `3` (shop on `stage 3`)
  - before last level (shop on `stage totalStages - 1`, for 9 stages this is `8`)
- Total between-level shop count uses a hard minimum of `4`:
  - `forestNeed = max(4, shopsInForestCount)`
  - remaining points are selected randomly from non-fixed stages.

## Current shop visibility / currency behavior (updated 2026-02-14)

- `InterLevelUI` marker icon is always coins-only sprite (`coinsMarkerSprite`) for all scheduled shop points.
- Stage-clear shop popup (`SoulShopKeeperPopup.OpenAsStageClearShop`) always enables both sections:
  - coin section (skill charges/items)
  - soul section (perks/soul upgrades)
- Practical result: when stage-clear shop appears, both skills/charges and perks are available in the same popup.

## Combat input interaction note (dash + windup)

Latest fix (added in this context on 2026-02-14):
- issue: player could sometimes remain visually/state-wise in windup after pressing dash (`Space`) during/around attack charge.
- current behavior:
  - dash is allowed to start even if charge was active;
  - dash now force-cancels current charge/windup state before dash motion starts;
  - windup sprite is reset to idle during this forced cancel.

Implementation points:
- `Assets/Scripts/Player/Attack/SkillsAndElements/skills/Dash/PlayerDash.cs`
  - added serialized cached link to `PlayerSkillShooter`.
  - removed early return that blocked dash when shooter was charging.
  - in `TryDash()`, before spending dash energy, calls:
    - `shooter.CancelAllImmediate(resetToIdleSprite: true);`
- `Assets/Scripts/Player/Attack/SkillsAndElements/PlayerSkillShooter.cs`
  - `CancelAllImmediate(...)` extended with optional parameter:
    - `bool resetToIdleSprite = false`
  - method remains backward compatible for existing callers.

If this area is changed later, verify these scenarios:
- hold LMB (charge) -> press Space -> run left/right after dash
- quick alternating LMB/Space inputs
- ensure no persistent windup sprite after dash

## Death visual priority note (updated 2026-02-14)

Latest fix (added in this context on 2026-02-14):
- issue: if player died during movement/attack transitions (especially during dash throw/exit coroutines),
  throw/dash code could restore non-death visuals after `PlayerHealth.Die()`.
- required behavior:
  - death sprite must have priority over run/attack/dash visuals;
  - after death, no coroutine should re-enable animator or restore previous sprite.

Implementation points:
- `Assets/Scripts/Player/Attack/SkillsAndElements/PlayerSkillShooter.cs`
  - cached `PlayerHealth` reference.
  - in `FlashRoutine()` and `ThrowRoutine()`, after wait:
    - if `IsDead` is true, stop routine early without `BackToIdle()` and without animator re-enable.
- `Assets/Scripts/Player/Attack/SkillsAndElements/skills/Dash/PlayerDash.cs`
  - in `DashRoutine()` visual exit block:
    - checks `isDeadNow = hp != null && hp.IsDead`.
    - skips restore of sprite/color/scale/animator and movement re-enable when dead.

If this area is changed later, verify these scenarios:
- die while running (must keep dead sprite)
- die during charge/windup (must keep dead sprite)
- die during dash (must keep dead sprite, no post-dash restore)

## Key scripts and responsibilities

- `Assets/Scripts/UI/SoulShopKeeper/SoulShopKeeperPopup.cs`
  - shop popup controller, modes, currency section toggles, slot setup.
- `Assets/Scripts/UI/SoulShopKeeper/ShopItemDefinition.cs`
  - item data SO (display, price, effect type, optional skill effect data).
- `Assets/Scripts/UI/SoulShopKeeper/ShopItemSlotUI.cs`
  - slot visual + purchase logic + tooltip text for shop items.
- `Assets/Scripts/UI/SoulShopKeeper/SoulPerksManager.cs`
  - permanent perk levels, pricing, buy/reset logic, persistence via PlayerPrefs.
- `Assets/Scripts/UI/SoulShopKeeper/SoulPerksPanelUI.cs`
  - hearts UI (HP/Mana/Stamina), pooling, pop animation, heart tooltips.
- `Assets/Scripts/Managers/RunLevelManager.cs`
  - stage transitions, final victory flow, optional debug cheats/hotkeys.
- `Assets/Scripts/Player/Attack/SkillsAndElements/SkillBarUI.cs`
  - skill bar visuals and skill slot tooltips.
- `Assets/Scripts/Player/Attack/SkillsAndElements/DashPerkPanelUI.cs`
  - dash perk icon UI and tooltip.
- `Assets/Scripts/UI/Tooltip/HoverTooltipUI.cs`
  - shared tooltip runtime UI + hover trigger component.
- `Assets/Scripts/UI/Tooltip/HoverTooltipData.cs`
  - tooltip data struct (title/level/price/description).

Related gameplay stat targets:
- `Assets/Scripts/Player/Attack/Mana/PlayerMana.cs` (max mana)
- `Assets/Scripts/Player/Attack/SkillsAndElements/skills/Dash/PlayerDash.cs` (max dash energy)
- `Assets/Scripts/Player/PlayerHealth.cs` (max HP)

## Current soul perk effect mapping

From `ShopItemEffectType`:

- `IncreaseMaxHealth` (10)
  - uses `SoulPerksManager` HP upgrade.
- `ResetSoulPerks` (11)
  - resets perk levels and applies refund logic.
- `IncreaseDashLevel` (12)
  - currently used as stamina hearts / dash energy upgrade path.
  - calls stamina buy logic (`TryBuyStaminaUpgrade`).
- `IncreaseManaLevel` (13)
  - calls mana buy logic (`TryBuyManaUpgrade`).

Important:
- `IncreaseDashLevel` enum name is legacy; in current shop usage it maps to stamina upgrade.

## Current perk progression rules

HP:
- `HpLevel` range `0..hpMaxPurchases` (default max purchases: 4)
- UI hearts shown: `1 + HpLevel`
- price model: linear (`base * (index+1)`)

Mana:
- `ManaLevel` range `0..3`
- UI hearts shown: `1 + ManaLevel`
- stat bonus: `+25` max mana per level
- prices: `50 -> 100 -> 200` (default)

Stamina:
- `StaminaLevel` range `0..3`
- UI hearts shown: `1 + StaminaLevel`
- stat bonus: `+25` max dash energy per level
- prices: `50 -> 100 -> 200` (default)

Reset:
- resets `HpLevel`, `DashLevel`, `ManaLevel`, `StaminaLevel` to `0`
- hearts UI returns to one base heart per type

## Applying perk values to player

`SoulPerksManager.ApplyToPlayerIfPossible()` applies:
- HP bonus -> `PlayerHealth.ApplyPermanentMaxHpBonus(...)`
- Mana bonus -> `PlayerMana.ApplyPermanentMaxManaBonus(...)`
- Stamina bonus -> `PlayerDash.ApplyPermanentMaxEnergyBonus(...)`

If gameplay stat behavior changes, update this mapping first.

## Tooltip system (current)

### Overview

- Tooltip UI is created entirely at runtime by `HoverTooltipUI` (no prefab setup required).
- Shared format uses `HoverTooltipData` with 4 lines:
  - `title`
  - `levelLine`
  - `priceLine`
  - `description`
- Show delay is currently `0.6` sec by default.

### Where tooltips are attached

- Shop slots: `ShopItemSlotUI.EnsureTooltip()`
- Skill slots: `SkillBarUI.EnsureTooltips()`
- Hearts (HP/Mana/Stamina): `SoulPerksPanelUI.EnsureHeartTooltip(...)`
- Dash perk icon: `DashPerkPanelUI.EnsureIconExists()` binds tooltip

### Where tooltip text is edited

- Shop item tooltips:
  - `ShopItemSlotUI.BuildTooltipData()`
  - `ShopItemSlotUI.GetCurrentLevelLine()`
  - `ShopItemSlotUI.GetEffectDescription()`
- Skill tooltips:
  - `SkillBarUI.BuildSkillTooltipData(int index)`
- Heart tooltips:
  - `SoulPerksPanelUI.BuildPerkTooltipData(PerkType type)`
- Dash perk tooltip:
  - `DashPerkPanelUI.BuildTooltipData()`

### Global tooltip look/behavior knobs

In `Assets/Scripts/UI/Tooltip/HoverTooltipUI.cs`:
- position near cursor: `mouseOffset`
- default show delay: `showDelayDefault`
- background color: `bg.color` in `EnsureView()`
- text styling: `CreateLine(...)` calls in `EnsureView()`

### Current style defaults

- delay: `0.6s`
- offset: `(10, -10)` from cursor
- background: dark blue (`new Color(0.04f, 0.08f, 0.17f, 0.96f)`)

## How to add a new soul perk shop item

1. Create `Shop Item` asset.
2. Set:
   - `currency = Souls`
   - `effectType = desired soul perk effect`
3. Add SO to `SoulShopKeeperPopup.soulItems[]`.
4. Ensure `ShopItemSlotUI` includes branches for this effect in:
   - `GetCurrentPrice()`
   - `CanPurchaseNow()`
   - `OnClickBuy()`
5. If hearts/stat behavior changes, wire through `SoulPerksManager`.
6. If tooltip text must be special, update `GetEffectDescription()` / tooltip builder logic.

## Notes about logs / debugging

- `InterLevelUI` timing logs are currently effectively disabled in code (`LogTimingIfNeeded` early return).
- `RunLevelManager` and `ShopKeeperManager` noisy startup logs were reduced.

## Maintenance checklist

When editing shop/perks/tooltips, update:
- effect mapping section (if enum meaning/use changes)
- progression rules (caps/prices/bonuses)
- player-apply mapping
- tooltip attachment points and text edit locations
- any moved/renamed key script paths

## Lightning skill notes (updated 2026-02-15)

Implemented gameplay additions for new `Lightning` player skill:

- `SkillId.Lightning = 3` added in:
  - `Assets/Scripts/UI/SoulShopKeeper/ShopTypes.cs`

- New projectile behavior in:
  - `Assets/Scripts/Player/Attack/SkillsAndElements/skills/PlayerLightningBolt.cs`
  - Core behavior:
    - main bolt flies like standard projectile
    - side bolts are spawned in fan pattern
    - side bolt count scales by skill level and multiplier
    - side bolts can use wider angles and longer distance
    - side bolt damage falls off by fan step (farther side bolts hit weaker)
    - sprite can align to movement direction with optional tilt

- Projectile selection safety in:
  - `Assets/Scripts/Player/Attack/SkillsAndElements/PlayerSkillShooter.cs`
  - if prefab accidentally contains multiple projectile scripts, shooter picks script by `SkillId` and disables other projectile components for that spawned instance.

### Pink/magenta artifact finding

- Tried runtime-only fix in `PlayerLightningBolt` (forcing unlit material / white color / point+clamp texture settings).
- Result: no reliable improvement in project case; fix was rolled back.
- Current conclusion:
  - this artifact is most likely texture/slicing alpha-bleed (asset/import side), not runtime component logic.
  - primary fix should be texture import/slice padding adjustments (extrude, clamp, point/no compression, clean transparent edges in source sprite).
