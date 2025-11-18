using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageTransitionPopup : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Корневая панель попапа. Если оставить пустым, будет использован этот GameObject.")]
    public GameObject root;

    [Header("Texts")]
    public TextMeshProUGUI titleText;

    [Header("Buttons")]
    public Button nextButton;
    public Button mainMenuButton;

    private RunLevelManager runManager;
    private bool hasNextStage = false;

    private void Awake()
    {
        // root — это сам объект панели
        if (root == null)
            root = gameObject;

        runManager = RunLevelManager.Instance;

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        Debug.Log("[StageTransitionPopup] Awake. root=" + (root != null ? root.name : "NULL"));

        // ВАЖНО: больше НИЧЕГО здесь не прячем.
        // Панель должна быть выключена в инспекторе изначально,
        // а Show() будет включать её через root.SetActive(true).
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

        Debug.Log($"[StageTransitionPopup] Show() called. stage={currentStage}/{totalStages}, hasNext={hasNext}, root={root.name}");

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
