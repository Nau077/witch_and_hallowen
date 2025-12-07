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
            // просто для ясности
            string currencyLabel = _def.currency == ShopCurrency.Coins ? "coins" : "souls";
            priceText.text = $"{_def.price} {currencyLabel}";
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

        switch (_def.currency)
        {
            // ====== ПОКУПКА ЗА COINS (монеты) ======
            case ShopCurrency.Coins:
                {
                    int coinsNow = PlayerWallet.Instance ? PlayerWallet.Instance.coins : -1;
                    Debug.Log($"[SHOP] Покупка '{_def.displayName}' за COINS. " +
                              $"price={_def.price}, coinsNow={coinsNow}");

                    if (PlayerWallet.Instance == null)
                    {
                        Debug.LogError("[SHOP] PlayerWallet.Instance == null, не могу списать coins.");
                        paid = false;
                    }
                    else
                    {
                        paid = PlayerWallet.Instance.TrySpend(_def.price);
                        Debug.Log($"[SHOP] TrySpend COINS -> {paid}, coinsAfter={PlayerWallet.Instance.coins}");
                    }

                    break;
                }

            // ====== ПОКУПКА ЗА SOULS (души) ======
            case ShopCurrency.Souls:
                {
                    int soulsNow = PlayerSoulsWallet.Instance != null
                        ? PlayerSoulsWallet.Instance.CurrentSouls
                        : (SoulCounter.Instance != null ? SoulCounter.Instance.cursedGoldRun : -1);

                    Debug.Log($"[SHOP] Покупка '{_def.displayName}' за SOULS. " +
                              $"price={_def.price}, soulsNow={soulsNow}");

                    if (PlayerSoulsWallet.Instance != null)
                    {
                        paid = PlayerSoulsWallet.Instance.TrySpend(_def.price);
                        Debug.Log($"[SHOP] PlayerSoulsWallet.TrySpend({_def.price}) -> {paid}, " +
                                  $"soulsAfter={PlayerSoulsWallet.Instance.CurrentSouls}");
                    }
                    else if (SoulCounter.Instance != null)
                    {
                        // Fallback напрямую через SoulCounter, если по какой-то причине
                        // кошелёк душ не инициализировался.
                        var sc = SoulCounter.Instance;
                        if (sc.cursedGoldRun >= _def.price)
                        {
                            sc.cursedGoldRun -= _def.price;
                            sc.RefreshUI();
                            paid = true;
                            Debug.Log($"[SHOP] Fallback: списали SOULS через SoulCounter. " +
                                      $"soulsAfter={sc.cursedGoldRun}");
                        }
                        else
                        {
                            paid = false;
                            Debug.Log($"[SHOP] Fallback: недостаточно SOULS в SoulCounter. " +
                                      $"have={sc.cursedGoldRun}, need={_def.price}");
                        }
                    }
                    else
                    {
                        Debug.LogError("[SHOP] Нет PlayerSoulsWallet и нет SoulCounter. " +
                                       "Не могу списать SOULS.");
                        paid = false;
                    }

                    break;
                }
        }

        if (!paid)
        {
            Debug.Log($"[SHOP] Недостаточно {(_def.currency == ShopCurrency.Coins ? "COINS" : "SOULS")} " +
                      $"для покупки '{_def.displayName}'.");
            return;
        }

        // ---------- ПРИМЕНЯЕМ ЭФФЕКТ ----------
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

        // 2) Заряды в инвентарь (панель скиллов)
        if (_def.addCharges > 0 && _def.skillDef != null && SkillLoadout.Instance != null)
        {
            SkillLoadout.Instance.AddChargesToSkill(_def.skillDef, _def.addCharges);
        }
    }
}
