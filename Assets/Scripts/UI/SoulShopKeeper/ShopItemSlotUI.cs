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

        // -------- COINS --------
        if (_def.currency == ShopCurrency.Coins)
        {
            int coinsNow = PlayerWallet.Instance ? PlayerWallet.Instance.coins : -1;
            Debug.Log($"[SHOP] Покупка '{_def.displayName}' за COINS. Price={_def.price}, coinsNow={coinsNow}");

            if (PlayerWallet.Instance != null)
                paid = PlayerWallet.Instance.TrySpend(_def.price);
        }
        // -------- SOULS (killsLifetime) --------
        else // ShopCurrency.Souls
        {
            var sc = SoulCounter.Instance;
            if (sc != null)
            {
                // Тратим именно killsLifetime — то, что показывается под черепом.
                int soulsNow = sc.killsLifetime;
                Debug.Log($"[SHOP] Покупка '{_def.displayName}' за SOULS. Price={_def.price}, soulsNow={soulsNow}");

                if (soulsNow >= _def.price)
                {
                    sc.killsLifetime = soulsNow - _def.price;
                    sc.RefreshUI();
                    paid = true;

                    Debug.Log($"[SHOP] Покупка за SOULS успешна. Осталось souls={sc.killsLifetime}");
                }
                else
                {
                    Debug.Log($"[SHOP] Недостаточно SOULS: надо {_def.price}, есть {soulsNow}");
                }
            }
            else
            {
                Debug.LogWarning("[SHOP] SoulCounter.Instance == null — не можем списать SOULS");
            }
        }

        if (!paid)
            return;

        ApplyEffect();

        _purchased = true;
        RefreshVisual();

        _ownerPopup?.OnShopItemPurchased(_def);
    }

    private void ApplyEffect()
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
