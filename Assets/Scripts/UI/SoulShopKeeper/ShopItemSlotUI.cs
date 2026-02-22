using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour
{
    [Header("UI refs")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Button buyButton;
    public CanvasGroup canvasGroup;

    private ShopItemDefinition _def;
    private SoulShopKeeperPopup _ownerPopup;
    private HoverTooltipTrigger _tooltipTrigger;

    public void Setup(SoulShopKeeperPopup owner, ShopItemDefinition def)
    {
        _ownerPopup = owner;
        _def = def;

        RefreshVisual();
        HookButton();
        EnsureTooltip();
    }

    private void HookButton()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnClickBuy);
        }
    }

    public void RefreshVisual()
    {
        if (_def == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
            iconImage.sprite = _def.icon;

        if (nameText != null)
            nameText.text = _def.displayName;

        int shownPrice = GetCurrentPrice();
        if (priceText != null)
            priceText.text = shownPrice.ToString();

        bool requirementsMet = true;
        if (_def.requiredSkill != SkillId.None && PlayerSkills.Instance == null)
        {
            requirementsMet = false;
        }
        else if (PlayerSkills.Instance != null)
        {
            requirementsMet = PlayerSkills.Instance.MeetsRequirement(_def.requiredSkill, _def.requiredSkillLevel);
        }

        bool canInteract = requirementsMet && CanPurchaseNow();
        bool chargeLocked = IsChargePurchaseLockedBySkillLevel();

        if (chargeLocked)
            canInteract = false;

        if (priceText != null && chargeLocked)
            priceText.text = TooltipLocalization.Tr("Unavailable", "Недоступно");

        if (buyButton != null)
            buyButton.interactable = canInteract;

        if (canvasGroup != null)
            canvasGroup.alpha = canInteract ? 1f : 0.35f;
    }

    private int GetCurrentPrice()
    {
        if (_def == null) return 0;

        var perks = SoulPerksManager.Instance;

        if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth && perks != null)
            return perks.GetHealthUpgradePrice();

        if (_def.effectType == ShopItemEffectType.IncreaseDashLevel && perks != null)
            return perks.GetStaminaUpgradePrice();

        if (_def.effectType == ShopItemEffectType.IncreaseManaLevel && perks != null)
            return perks.GetManaUpgradePrice();

        if (_def.effectType == ShopItemEffectType.ResetSoulPerks && perks != null)
            return perks.resetPrice;

        return _def.price;
    }

    private bool CanPurchaseNow()
    {
        if (_def == null) return false;
        if (IsChargePurchaseLockedBySkillLevel()) return false;

        if (_def.currency == ShopCurrency.Souls)
        {
            var perks = SoulPerksManager.Instance;
            var sc = SoulCounter.Instance;
            if (sc == null) return false;

            if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth)
                return perks != null && perks.CanBuyHealthUpgrade();

            if (_def.effectType == ShopItemEffectType.IncreaseDashLevel)
                return perks != null && perks.CanBuyStaminaUpgrade();

            if (_def.effectType == ShopItemEffectType.IncreaseManaLevel)
                return perks != null && perks.CanBuyManaUpgrade();

            if (_def.effectType == ShopItemEffectType.ResetSoulPerks)
                return perks != null && perks.HasAnythingToReset();

            return sc.souls >= GetCurrentPrice();
        }

        if (_def.currency == ShopCurrency.Coins)
            return PlayerWallet.Instance != null && PlayerWallet.Instance.CanSpend(GetCurrentPrice());

        return false;
    }

    private void OnClickBuy()
    {
        if (_def == null) return;
        if (IsChargePurchaseLockedBySkillLevel()) return;

        bool paidOrDone = false;

        if (_def.currency == ShopCurrency.Souls)
        {
            var perks = SoulPerksManager.Instance;

            if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth)
            {
                if (perks != null)
                    paidOrDone = perks.TryBuyHealthUpgrade();
            }
            else if (_def.effectType == ShopItemEffectType.IncreaseDashLevel)
            {
                if (perks != null)
                    paidOrDone = perks.TryBuyStaminaUpgrade();
            }
            else if (_def.effectType == ShopItemEffectType.IncreaseManaLevel)
            {
                if (perks != null)
                    paidOrDone = perks.TryBuyManaUpgrade();
            }
            else if (_def.effectType == ShopItemEffectType.ResetSoulPerks)
            {
                if (perks != null)
                    paidOrDone = perks.ResetAllPerksWithRefund();
            }
            else
            {
                var sc = SoulCounter.Instance;
                int price = GetCurrentPrice();
                if (sc != null && sc.souls >= price)
                {
                    sc.SetSouls(sc.souls - price);
                    sc.RefreshUI();
                    paidOrDone = true;
                    ApplyEffect_RegularShopItem();
                }
            }
        }
        else if (_def.currency == ShopCurrency.Coins)
        {
            if (PlayerWallet.Instance != null)
            {
                int price = GetCurrentPrice();
                paidOrDone = PlayerWallet.Instance.TrySpend(price);
                if (paidOrDone)
                    ApplyEffect_RegularShopItem();
            }
        }

        if (!paidOrDone)
            return;

        RefreshVisual();
        _ownerPopup?.OnShopItemPurchased(_def);
    }

    private void ApplyEffect_RegularShopItem()
    {
        if (PlayerSkills.Instance != null && _def.skillId != SkillId.None)
        {
            if (_def.unlockSkill)
                PlayerSkills.Instance.UnlockSkill(_def.skillId, _def.unlockLevel);

            if (_def.upgradeToLevel > 0)
                PlayerSkills.Instance.SetSkillLevel(_def.skillId, _def.upgradeToLevel);

            if (_def.addCharges > 0)
                PlayerSkills.Instance.AddCharges(_def.skillId, _def.addCharges);
        }

        if (_def.addCharges > 0 && _def.skillDef != null && SkillLoadout.Instance != null)
            SkillLoadout.Instance.AddChargesToSkill(_def.skillDef, _def.addCharges);
    }

    private void EnsureTooltip()
    {
        var target = (buyButton != null) ? buyButton.gameObject : gameObject;
        if (target == null) return;

        _tooltipTrigger = target.GetComponent<HoverTooltipTrigger>();
        if (_tooltipTrigger == null)
            _tooltipTrigger = target.AddComponent<HoverTooltipTrigger>();

        _tooltipTrigger.Bind(BuildTooltipData, 0.3f);
    }

    private HoverTooltipData BuildTooltipData()
    {
        if (_def == null) return default;

        int price = GetCurrentPrice();
        string currency = (_def.currency == ShopCurrency.Souls)
            ? TooltipLocalization.Tr("souls", "души")
            : TooltipLocalization.Tr("coins", "монеты");
        bool chargeLocked = IsChargePurchaseLockedBySkillLevel();

        return new HoverTooltipData
        {
            title = _def.displayName,
            levelLine = GetCurrentLevelLine(),
            priceLine = chargeLocked
                ? TooltipLocalization.Tr("Unavailable", "Недоступно")
                : (TooltipLocalization.Tr("Price: ", "Цена: ") + price + " " + currency),
            description = GetEffectDescription()
        };
    }

    private string GetCurrentLevelLine()
    {
        var perks = SoulPerksManager.Instance;

        if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth && perks != null)
            return TooltipLocalization.Tr("Level: ", "Уровень: ") + (1 + perks.HpLevel) + "/" + (1 + perks.hpMaxPurchases);

        if (_def.effectType == ShopItemEffectType.IncreaseManaLevel && perks != null)
            return TooltipLocalization.Tr("Level: ", "Уровень: ") + (1 + perks.ManaLevel) + "/" + (1 + perks.manaMaxPurchases);

        if (_def.effectType == ShopItemEffectType.IncreaseDashLevel && perks != null)
            return TooltipLocalization.Tr("Level: ", "Уровень: ") + (1 + perks.StaminaLevel) + "/" + (1 + perks.staminaMaxPurchases);

        SkillId skillId = ResolveSkillId();
        if (skillId != SkillId.None && PlayerSkills.Instance != null)
            return TooltipLocalization.Tr("Skill level: ", "Уровень навыка: ") + PlayerSkills.Instance.GetSkillLevel(skillId);

        return TooltipLocalization.Tr("Level: -", "Уровень: -");
    }

    private string GetEffectDescription()
    {
        if (_def == null) return "";

        if (IsChargePurchaseLockedBySkillLevel())
            return TooltipLocalization.Tr(
                "Unavailable: unlock this skill first (level 1+) to buy charges.",
                "Недоступно: сначала откройте навык (уровень 1+), потом можно покупать заряды.");

        if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth)
            return TooltipLocalization.Tr("Permanently increases max HP.", "Перманентно повышает максимум HP.");

        if (_def.effectType == ShopItemEffectType.IncreaseManaLevel)
            return TooltipLocalization.Tr("Permanently increases max Mana.", "Перманентно повышает максимум маны.");

        if (_def.effectType == ShopItemEffectType.IncreaseDashLevel)
            return TooltipLocalization.Tr("Permanently increases dash stamina reserve.", "Перманентно повышает запас выносливости для дэша.");

        if (_def.effectType == ShopItemEffectType.ResetSoulPerks)
            return TooltipLocalization.Tr("Resets perks and refunds spent souls with commission.", "Сбрасывает перки и возвращает потраченные души с комиссией.");

        SkillId skillId = ResolveSkillId();

        if (_def.addCharges > 0)
        {
            return TooltipLocalization.Tr("Adds charges: +", "Добавляет заряды: +") + _def.addCharges + ".";
        }

        if (_def.unlockSkill && skillId != SkillId.None)
            return TooltipLocalization.Tr("Unlocks skill.", "Открывает навык.");

        if (_def.upgradeToLevel > 0 && skillId != SkillId.None)
            return TooltipLocalization.Tr("Upgrades skill to level ", "Повышает навык до уровня ") + _def.upgradeToLevel + ".";

        return TooltipLocalization.Tr("Shop purchase.", "Покупка в магазине.");
    }

    private SkillId ResolveSkillId()
    {
        if (_def == null) return SkillId.None;
        if (_def.skillId != SkillId.None) return _def.skillId;
        if (_def.skillDef != null) return _def.skillDef.skillId;
        return SkillId.None;
    }

    private bool IsChargePurchaseLockedBySkillLevel()
    {
        if (_def == null) return false;
        if (_def.addCharges <= 0) return false;

        SkillId skillId = ResolveSkillId();
        if (skillId == SkillId.None) return false;

        if (PlayerSkills.Instance == null) return true;
        return PlayerSkills.Instance.GetSkillLevel(skillId) <= 0;
    }
}

