# Shop + Soul Perks Context (Codex Handoff)

This document is a quick handoff for future Codex chats working on the shop/perk system.
Keep this file updated when shop logic changes.

## Core runtime flow

1. `RunLevelManager` opens shop popup (`SoulShopKeeperPopup`) in base or stage-clear mode.
2. `SoulShopKeeperPopup` builds slots from `coinItems[]` and `soulItems[]`.
3. Each `ShopItemSlotUI` reads a `ShopItemDefinition` and:
   - shows icon/name/price
   - validates if purchase is available now
   - performs purchase on click
4. Permanent soul perks are applied via `SoulPerksManager`.
5. `SoulPerksPanelUI` listens to `SoulPerksManager.OnPerksChanged` and redraws hearts.

## Key scripts and responsibilities

- `Assets/Scripts/UI/SoulShopKeeper/SoulShopKeeperPopup.cs`
  - Shop popup controller, modes, section toggles, slot setup.
- `Assets/Scripts/UI/SoulShopKeeper/ShopItemDefinition.cs`
  - ScriptableObject data for shop items (`effectType`, price, icon, etc).
- `Assets/Scripts/UI/SoulShopKeeper/ShopItemSlotUI.cs`
  - Slot visual + purchase logic.
- `Assets/Scripts/UI/SoulShopKeeper/SoulPerksManager.cs`
  - Permanent perk levels, pricing, buy/reset logic, PlayerPrefs persistence.
- `Assets/Scripts/UI/SoulShopKeeper/SoulPerksPanelUI.cs`
  - Heart UI for HP/Mana/Stamina. Shows `1 + level` hearts per type.

Related gameplay stat targets:
- `Assets/Scripts/Player/Attack/Mana/PlayerMana.cs` (max mana)
- `Assets/Scripts/Player/Attack/SkillsAndElements/skills/Dash/PlayerDash.cs` (max dash energy)
- `Assets/Scripts/Player/PlayerHealth.cs` (max HP)

## Current soul perk effect mapping

From `ShopItemEffectType`:

- `IncreaseMaxHealth` (10)
  - Uses `SoulPerksManager` HP upgrade.
- `ResetSoulPerks` (11)
  - Resets all perk levels to `0`, recalculates stats, returns refund logic.
- `IncreaseDashLevel` (12)
  - In shop logic this is used for **Stamina hearts/energy** upgrades.
  - Calls stamina buy path (`TryBuyStaminaUpgrade`).
- `IncreaseManaLevel` (13)
  - Calls mana buy path (`TryBuyManaUpgrade`).

Important: `IncreaseDashLevel` enum name is legacy; currently used as stamina perk in shop slots.

## Current perk progression rules

HP:
- `HpLevel` starts at `0`
- UI shows `1 + HpLevel` hearts
- Price: linear from `GetHealthUpgradePrice()`

Mana:
- `ManaLevel` range `0..3`
- UI shows `1 + ManaLevel` hearts
- Per purchase: `+25` max mana
- Prices: `50 -> 100 -> 200`

Stamina:
- `StaminaLevel` range `0..3`
- UI shows `1 + StaminaLevel` hearts
- Per purchase: `+25` max dash energy
- Prices: `50 -> 100 -> 200`

Reset:
- Sets `HpLevel`, `DashLevel`, `ManaLevel`, `StaminaLevel` to `0`
- Therefore heart UI returns to one base heart for each type.

## Applying perk values to player

`SoulPerksManager.ApplyToPlayerIfPossible()` currently applies:
- HP bonus -> `PlayerHealth.ApplyPermanentMaxHpBonus(...)`
- Mana bonus -> `PlayerMana.ApplyPermanentMaxManaBonus(...)`
- Stamina bonus -> `PlayerDash.ApplyPermanentMaxEnergyBonus(...)`

If gameplay stat behavior changes, update this mapping first.

## How to add a new soul perk item (ScriptableObject)

1. Create `Shop Item` asset.
2. Set:
   - `currency = Souls`
   - `effectType = one of soul perk effects`
3. Add SO to `SoulShopKeeperPopup.soulItems[]`.
4. Ensure `ShopItemSlotUI` has corresponding branches in:
   - `GetCurrentPrice()`
   - `CanPurchaseNow()`
   - `OnClickBuy()`
5. If UI hearts/stat must change, wire it through `SoulPerksManager`.

## Maintenance checklist (update this doc when changed)

When editing shop/perks, update:
- Effect mapping section (if enum/use meaning changes)
- Progression rules section (steps, caps, prices)
- Applied-to-player section (which runtime stats are modified)
- Any renamed or moved key script paths
