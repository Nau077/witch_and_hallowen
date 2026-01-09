using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public void Setup(SoulShopKeeperPopup owner, ShopItemDefinition def)
    {
        _ownerPopup = owner;
        _def = def;

        RefreshVisual();
        HookButton();
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
            priceText.text = $"{shownPrice}";

        bool requirementsMet = true;
        if (PlayerSkills.Instance != null)
        {
            requirementsMet = PlayerSkills.Instance.MeetsRequirement(
                _def.requiredSkill,
                _def.requiredSkillLevel
            );
        }

        bool canInteract = requirementsMet && CanPurchaseNow();

        if (buyButton != null)
            buyButton.interactable = canInteract;

        if (canvasGroup != null)
            canvasGroup.alpha = canInteract ? 1f : 0.35f;
    }

    private int GetCurrentPrice()
    {
        if (_def == null) return 0;

        if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth)
        {
            var perks = SoulPerksManager.Instance;
            if (perks != null)
                return perks.GetHealthUpgradePrice();
        }

        return _def.price;
    }

    private bool CanPurchaseNow()
    {
        if (_def == null) return false;

        // --- SOULS (перманентно) ---
        if (_def.currency == ShopCurrency.Souls)
        {
            var perks = SoulPerksManager.Instance;
            var sc = SoulCounter.Instance;
            if (sc == null) return false;

            if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth)
                return perks != null && perks.CanBuyHealthUpgrade();

            if (_def.effectType == ShopItemEffectType.ResetSoulPerks)
                return perks != null && perks.HasAnythingToReset();

            // обычные товары за souls
            return sc.souls >= GetCurrentPrice();
        }

        // --- COINS (только ран) ---
        if (_def.currency == ShopCurrency.Coins)
        {
            return PlayerWallet.Instance != null && PlayerWallet.Instance.CanSpend(_def.price);
        }

        return false;
    }

    private void OnClickBuy()
    {
        if (_def == null) return;

        bool paidOrDone = false;

        // --- SOULS PERKS / SOULS покупки ---
        if (_def.currency == ShopCurrency.Souls)
        {
            var perks = SoulPerksManager.Instance;

            if (_def.effectType == ShopItemEffectType.IncreaseMaxHealth)
            {
                if (perks != null)
                    paidOrDone = perks.TryBuyHealthUpgrade();
            }
            else if (_def.effectType == ShopItemEffectType.ResetSoulPerks)
            {
                if (perks != null)
                    paidOrDone = perks.ResetAllPerksWithRefund();
            }
            else
            {
                // обычная покупка за souls
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
        // --- COINS ---
        else if (_def.currency == ShopCurrency.Coins)
        {
            if (PlayerWallet.Instance != null)
            {
                paidOrDone = PlayerWallet.Instance.TrySpend(_def.price);
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
        // 1) Прогресс/разблокировка скиллов
        if (PlayerSkills.Instance != null && _def.skillId != SkillId.None)
        {
            if (_def.unlockSkill)
                PlayerSkills.Instance.UnlockSkill(_def.skillId, _def.unlockLevel);

            if (_def.upgradeToLevel > 0)
                PlayerSkills.Instance.SetSkillLevel(_def.skillId, _def.upgradeToLevel);

            if (_def.addCharges > 0)
                PlayerSkills.Instance.AddCharges(_def.skillId, _def.addCharges);
        }

        // 2) Заряды прямо в инвентарь (панель скиллов)
        if (_def.addCharges > 0 && _def.skillDef != null && SkillLoadout.Instance != null)
        {
            SkillLoadout.Instance.AddChargesToSkill(_def.skillDef, _def.addCharges);
        }
    }
}
