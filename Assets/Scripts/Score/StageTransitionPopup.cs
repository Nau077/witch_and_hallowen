using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageTransitionPopup : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Корневая панель попапа. Если пусто, используется этот GameObject.")]
    public GameObject root;

    [Header("Texts")]
    public TextMeshProUGUI titleText;

    [Header("Buttons")]
    public Button nextButton;
    public Button mainMenuButton;

    [Header("Shop block (between title and Next button)")]
    [Tooltip("Контейнер (плашка/панель) магазина под LEVEL и над кнопкой. Включаем/выключаем его.")]
    public GameObject shopBlockRoot;

    [Tooltip("Иконка валюты магазина (монета или монета+душа).")]
    public Image shopCurrencyIcon;

    public Sprite coinsIcon;
    public Sprite coinsAndSoulsIcon;

    [Tooltip("Ссылка на твой попап магазина.")]
    public SoulShopKeeperPopup shopPopup;

    [Header("Optional shopkeeper portrait")]
    public Image shopkeeperPortrait;
    public Sprite shopkeeperSprite;

    private RunLevelManager runManager;
    private bool hasNextStage = false;

    private void Awake()
    {
        if (root == null) root = gameObject;

        runManager = RunLevelManager.Instance;

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        Debug.Log("[StageTransitionPopup] Awake. root=" + (root != null ? root.name : "NULL"));
    }

    public void Show(int currentStage, int totalStages, bool hasNext)
    {
        hasNextStage = hasNext;

        if (runManager == null)
            runManager = RunLevelManager.Instance;

        if (titleText != null)
            titleText.text = $"LEVEL {currentStage}";

        if (root == null)
            root = gameObject;

        // кнопка Next
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(hasNext);
            nextButton.interactable = hasNext;
        }

        // --- SHOP UI: показываем только на этапах 1..7 где запланирован магазин ---
        var mode = ShopKeeperManager.Instance != null
            ? ShopKeeperManager.Instance.GetShopModeForStage(currentStage)
            : ShopCurrencyMode.None;

        bool shouldShowShopBlock = (currentStage != 0) && (mode != ShopCurrencyMode.None);

        if (shopBlockRoot != null)
            shopBlockRoot.SetActive(shouldShowShopBlock);

        if (shouldShowShopBlock)
        {
            // иконка валюты
            if (shopCurrencyIcon != null)
            {
                if (mode == ShopCurrencyMode.CoinsOnly) shopCurrencyIcon.sprite = coinsIcon;
                else shopCurrencyIcon.sprite = coinsAndSoulsIcon;
            }

            // портрет продавца
            if (shopkeeperPortrait != null && shopkeeperSprite != null)
                shopkeeperPortrait.sprite = shopkeeperSprite;

            // настроить доступность секций и открыть магазин автоматически
            if (shopPopup != null)
            {
                bool allowSouls = (mode == ShopCurrencyMode.CoinsAndSouls);
                shopPopup.SetCurrencyAvailability(allowCoins: true, allowSouls: allowSouls);

                // по ТЗ: "автоматически открывается магазин после победы, в тех местах где он есть"
                shopPopup.Show(forceOpen: true);
            }
        }
        else
        {
            // если на этом stage магазина нет — закрываем его на всякий
            if (shopPopup != null)
                shopPopup.HideImmediate();
        }

        Debug.Log($"[StageTransitionPopup] Show() stage={currentStage}/{totalStages}, hasNext={hasNext}, shopMode={mode}");
        root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void HideImmediate()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnNextClicked()
    {
        if (runManager == null)
            runManager = RunLevelManager.Instance;

        if (runManager == null)
        {
            Debug.LogError("[StageTransitionPopup] Нет RunLevelManager при клике Next.");
            return;
        }

        Debug.Log("[StageTransitionPopup] Next clicked → GoDeeper");
        runManager.GoDeeper();
    }

    private void OnMainMenuClicked()
    {
        if (runManager == null)
            runManager = RunLevelManager.Instance;

        if (runManager == null)
        {
            Debug.LogError("[StageTransitionPopup] Нет RunLevelManager при клике MainMenu.");
            return;
        }

        Debug.Log("[StageTransitionPopup] MainMenu clicked → ReturnToMenu");
        runManager.ReturnToMenu();
    }
}
