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
    private bool _purchased;
    private SoulShopKeeperPopup _ownerPopup;

    public void Setup(SoulShopKeeperPopup owner, ShopItemDefinition def)
    {
        _ownerPopup = owner;
        _def = def;
        _purchased = false;

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

        if (priceText != null)
        {
            string currencySymbol = _def.currency == ShopCurrency.Coins ? "🪙" : "💀";
            priceText.text = $"{_def.price} {currencySymbol}";
        }

        bool requirementsMet = true;

        if (PlayerSkills.Instance != null)
        {
            requirementsMet = PlayerSkills.Instance.MeetsRequirement(
                _def.requiredSkill,
                _def.requiredSkillLevel
            );
        }

        bool canInteract = requirementsMet && !_purchased;

        if (buyButton != null)
            buyButton.interactable = canInteract;

        if (canvasGroup != null)
            canvasGroup.alpha = canInteract ? 1f : 0.35f;
    }

    private void OnClickBuy()
    {
        if (_def == null || _purchased) return;

        bool paid = false;
        if (_def.currency == ShopCurrency.Coins)
        {
            if (PlayerWallet.Instance != null)
                paid = PlayerWallet.Instance.TrySpend(_def.price);
        }
        else
        {
            if (PlayerSoulsWallet.Instance != null)
                paid = PlayerSoulsWallet.Instance.TrySpend(_def.price);
        }

        if (!paid)
        {
            Debug.Log("Недостаточно валюты для покупки " + _def.displayName);
            return;
        }

        ApplyEffect();

        _purchased = true;
        RefreshVisual();

        _ownerPopup?.OnShopItemPurchased(_def);
    }

    private void ApplyEffect()
    {
        // 1) Прогресс скиллов
        if (PlayerSkills.Instance != null && _def.skillId != SkillId.None)
        {
            if (_def.unlockSkill)
                PlayerSkills.Instance.UnlockSkill(_def.skillId, _def.unlockLevel);

            if (_def.upgradeToLevel > 0)
                PlayerSkills.Instance.SetSkillLevel(_def.skillId, _def.upgradeToLevel);

            if (_def.addCharges > 0)
                PlayerSkills.Instance.AddCharges(_def.skillId, _def.addCharges);
        }

        // 2) Реальные заряды в панель скиллов
        if (_def.addCharges > 0 && _def.skillDef != null && SkillLoadout.Instance != null)
        {
            SkillLoadout.Instance.AddChargesToSkill(_def.skillDef, _def.addCharges);
        }
    }
}
