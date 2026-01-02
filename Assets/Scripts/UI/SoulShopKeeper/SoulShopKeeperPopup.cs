using UnityEngine;
using UnityEngine.UI;

public class SoulShopKeeperPopup : MonoBehaviour
{
    [Header("Root")]
    public GameObject popupRoot;

    [Header("Buttons")]
    public Button goToForestButton;   // база
    public Button goDeeperButton;     // после победы (НОВАЯ)
    public Button closeButton;        // база (можно оставить)

    [Header("Shop sections")]
    public GameObject coinsSectionRoot;
    public GameObject soulsSectionRoot;

    [Tooltip("До 10 ячеек покупок за монеты.")]
    public ShopItemSlotUI[] coinSlots = new ShopItemSlotUI[10];

    [Tooltip("До 10 ячеек покупок за души.")]
    public ShopItemSlotUI[] soulSlots = new ShopItemSlotUI[10];

    [Header("Shop config")]
    public ShopItemDefinition[] coinItems = new ShopItemDefinition[10];
    public ShopItemDefinition[] soulItems = new ShopItemDefinition[10];

    [Header("Right perks panel (optional)")]
    public SoulPerksPanelUI perksPanelUI;

    [Header("Runtime toggles")]
    public bool enableCoinSection = true;
    public bool enableSoulSection = true;

    private enum OpenMode
    {
        Base,
        StageClearShop
    }

    private OpenMode _mode = OpenMode.Base;

    private void Awake()
    {
        if (popupRoot == null)
        {
            Debug.LogError("[SoulShopKeeperPopup] PopupRoot is not assigned!");
            return;
        }

        popupRoot.SetActive(false);
    }

    private void Start()
    {
        if (goToForestButton != null)
            goToForestButton.onClick.AddListener(OnClickGoToForest_Base);

        if (goDeeperButton != null)
            goDeeperButton.onClick.AddListener(OnClickGoDeeper_StageClear);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
    }

    // ---------- PUBLIC API ----------

    /// <summary>
    /// Открыть магазин как базовый (stage 0): показываем GO TO THE FOREST и CLOSE,
    /// скрываем GO DEEPER.
    /// </summary>
    public void OpenAsBaseShop()
    {
        _mode = OpenMode.Base;

        SetCurrencyAvailability(true, true);

        if (goToForestButton != null) goToForestButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        if (goDeeperButton != null) goDeeperButton.gameObject.SetActive(false);

        Show(forceOpen: true);
    }

    /// <summary>
    /// Открыть магазин после победы на этапе:
    /// - показываем только GO DEEPER
    /// - скрываем GO TO THE FOREST и CLOSE
    /// </summary>
    public void OpenAsStageClearShop(bool allowCoins, bool allowSouls)
    {
        _mode = OpenMode.StageClearShop;

        SetCurrencyAvailability(allowCoins, allowSouls);

        if (goToForestButton != null) goToForestButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        if (goDeeperButton != null) goDeeperButton.gameObject.SetActive(true);

        Show(forceOpen: true);
    }

    public void SetCurrencyAvailability(bool allowCoins, bool allowSouls)
    {
        enableCoinSection = allowCoins;
        enableSoulSection = allowSouls;

        if (coinsSectionRoot != null) coinsSectionRoot.SetActive(enableCoinSection);
        if (soulsSectionRoot != null) soulsSectionRoot.SetActive(enableSoulSection);

        if (popupRoot != null && popupRoot.activeSelf)
        {
            BuildShop();
            perksPanelUI?.Refresh();
        }
    }

    public void Show(bool forceOpen = false)
    {
        if (popupRoot == null) return;

        if (!forceOpen && popupRoot.activeSelf)
        {
            Hide();
            return;
        }

        popupRoot.SetActive(true);

        RunLevelManager.Instance?.SetInputLocked(true);

        BuildShop();
        perksPanelUI?.Refresh();
    }

    public void Hide()
    {
        RunLevelManager.Instance?.SetInputLocked(false);
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    public void HideImmediate() => Hide();

    // ---------- BUTTON HANDLERS ----------

    private void OnClickGoToForest_Base()
    {
        RunLevelManager.Instance?.GoDeeper();
        Hide();
    }

    private void OnClickGoDeeper_StageClear()
    {
        RunLevelManager.Instance?.GoDeeper();
        Hide();
    }

    private void OnClickClose()
    {
        Hide();
    }

    // ---------- SHOP BUILD ----------

    public void OnShopItemPurchased(ShopItemDefinition purchasedDef)
    {
        RefreshAllSlots();
        perksPanelUI?.Refresh();
    }

    private void BuildShop()
    {
        if (coinsSectionRoot != null)
            coinsSectionRoot.SetActive(enableCoinSection);

        if (soulsSectionRoot != null)
            soulsSectionRoot.SetActive(enableSoulSection);

        if (enableCoinSection && coinSlots != null)
        {
            for (int i = 0; i < coinSlots.Length; i++)
            {
                var slot = coinSlots[i];
                if (slot == null) continue;

                ShopItemDefinition def = (coinItems != null && i < coinItems.Length) ? coinItems[i] : null;

                if (def == null)
                {
                    slot.gameObject.SetActive(false);
                }
                else
                {
                    slot.gameObject.SetActive(true);
                    slot.Setup(this, def);
                }
            }
        }
        else
        {
            if (coinSlots != null)
                foreach (var slot in coinSlots)
                    if (slot != null) slot.gameObject.SetActive(false);
        }

        if (enableSoulSection && soulSlots != null)
        {
            for (int i = 0; i < soulSlots.Length; i++)
            {
                var slot = soulSlots[i];
                if (slot == null) continue;

                ShopItemDefinition def = (soulItems != null && i < soulItems.Length) ? soulItems[i] : null;

                if (def == null)
                {
                    slot.gameObject.SetActive(false);
                }
                else
                {
                    slot.gameObject.SetActive(true);
                    slot.Setup(this, def);
                }
            }
        }
        else
        {
            if (soulSlots != null)
                foreach (var slot in soulSlots)
                    if (slot != null) slot.gameObject.SetActive(false);
        }

        RefreshAllSlots();
    }

    private void RefreshAllSlots()
    {
        if (coinSlots != null)
            foreach (var s in coinSlots)
                if (s != null) s.RefreshVisual();

        if (soulSlots != null)
            foreach (var s in soulSlots)
                if (s != null) s.RefreshVisual();
    }

    private void OnDestroy()
    {
        RunLevelManager.Instance?.SetInputLocked(false);
    }
}