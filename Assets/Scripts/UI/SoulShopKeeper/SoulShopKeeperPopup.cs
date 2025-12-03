using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoulShopKeeperPopup : MonoBehaviour
{
    [Header("Root")]
    public GameObject popupRoot;

    [Header("Buttons")]
    public Button goToForestButton;
    public Button closeButton;

    [Header("Shop sections")]
    public GameObject coinsSectionRoot;
    public GameObject soulsSectionRoot;

    [Tooltip("До 10 ячеек покупок за монеты.")]
    public ShopItemSlotUI[] coinSlots = new ShopItemSlotUI[10];

    [Tooltip("До 10 ячеек покупок за души.")]
    public ShopItemSlotUI[] soulSlots = new ShopItemSlotUI[10];

    [Header("Shop config")]
    [Tooltip("Товары за монеты (по порядку заполнят coinSlots).")]
    public ShopItemDefinition[] coinItems = new ShopItemDefinition[10];

    [Tooltip("Товары за души (по порядку заполнят soulSlots).")]
    public ShopItemDefinition[] soulItems = new ShopItemDefinition[10];

    public bool enableCoinSection = true;
    public bool enableSoulSection = true;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        HideImmediate();

        if (goToForestButton != null)
            goToForestButton.onClick.AddListener(OnClickGoToForest);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        BuildShop();
    }

    public void Show()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            BuildShop();
        }
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void HideImmediate() => Hide();

    public void OnClickGoToForest()
    {
        if (RunLevelManager.Instance != null)
        {
            RunLevelManager.Instance.GoDeeper();
        }
        Hide();
    }

    public void OnClickClose()
    {
        Hide();
    }

    public void OnShopItemPurchased(ShopItemDefinition purchasedDef)
    {
        RefreshAllSlots();
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

                ShopItemDefinition def = (coinItems != null && i < coinItems.Length)
                    ? coinItems[i]
                    : null;

                if (def == null)
                    slot.gameObject.SetActive(false);
                else
                    slot.Setup(this, def);
            }
        }

        if (enableSoulSection && soulSlots != null)
        {
            for (int i = 0; i < soulSlots.Length; i++)
            {
                var slot = soulSlots[i];
                if (slot == null) continue;

                ShopItemDefinition def = (soulItems != null && i < soulItems.Length)
                    ? soulItems[i]
                    : null;

                if (def == null)
                    slot.gameObject.SetActive(false);
                else
                    slot.Setup(this, def);
            }
        }

        RefreshAllSlots();
    }

    private void RefreshAllSlots()
    {
        if (coinSlots != null)
        {
            foreach (var s in coinSlots)
                if (s != null) s.RefreshVisual();
        }

        if (soulSlots != null)
        {
            foreach (var s in soulSlots)
                if (s != null) s.RefreshVisual();
        }
    }
}
