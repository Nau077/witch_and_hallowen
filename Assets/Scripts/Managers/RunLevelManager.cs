using UnityEngine;

public class RunLevelManager : MonoBehaviour
{
    public static RunLevelManager Instance { get; private set; }

    [Header("Stages (logical levels)")]
    [Tooltip("Общее количество логических уровней леса (без базы). Обычно = числу спавнеров.")]
    public int maxStages = 8;

    [Tooltip("Текущий логический уровень (0..maxStages).\n0 = база, 1 = первый этаж леса.")]
    [SerializeField] private int currentStage = 0;
    public int CurrentStage => currentStage;

    /// <summary>
    /// Количество этажей леса (без базы).
    /// </summary>
    public int TotalStages
    {
        get
        {
            int bySpawners = (stageSpawners != null) ? stageSpawners.Length : 0;
            if (bySpawners <= 0) return maxStages;
            return Mathf.Min(maxStages, bySpawners);
        }
    }

    [Header("Player refs")]
    public PlayerHealth playerHealth;
    public Transform playerTransform;
    [Tooltip("Точка появления игрока на базе (и при начале этажа).")]
    public Transform playerStartPoint;

    [Header("Spawners by stage")]
    [Tooltip("Спавнеры для этажей леса. Индекс 0 = этаж 1, индекс 1 = этаж 2 и т.д.")]
    public EnemyTopSpawner[] stageSpawners;

    [Header("UI")]
    [Tooltip("HUD с линией прогрессии (0 -> I -> ( II ) -> III).")]
    public InterLevelUI interLevelUI;

    [Tooltip("Попап после победы: содержит счёт / сундук / кнопку 'Войти в лес глубже'.")]
    public StageTransitionPopup stagePopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[RunLevelManager] Awake. Instance установлен.");
    }

    private void Start()
    {
        Debug.Log("[RunLevelManager] Start → InitializeRun()");
        InitializeRun();
    }

    /// <summary>
    /// Начинаем забег с базы (этаж 0).
    /// </summary>
    public void InitializeRun()
    {
        currentStage = 0;

        if (stagePopup != null)
            stagePopup.HideImmediate();

        ResetVictoryController();

        DeactivateAllSpawners();
        ResetPlayerPosition();
        UpdateHudProgress();

        // --- ВАЖНО: уведомляем ShopKeeperManager ---
        ShopKeeperManager.Instance?.OnStageChanged(0);
    }

    private void ResetVictoryController()
    {
        var vc = FindObjectOfType<LevelVictoryController>();
        if (vc != null)
        {
            vc.ResetForNewStage();
        }
    }

    private void UpdateHudProgress()
    {
        if (interLevelUI != null)
        {
            Debug.Log($"[RunLevelManager] UpdateHudProgress → stage {currentStage}/{TotalStages} (0 = база)");
            interLevelUI.SetProgress(currentStage, TotalStages);
        }
        else
        {
            Debug.LogWarning("[RunLevelManager] interLevelUI не назначен.");
        }
    }

    private void ResetPlayerPosition()
    {
        if (playerTransform != null && playerStartPoint != null)
        {
            playerTransform.position = playerStartPoint.position;
        }
    }

    private void DeactivateAllSpawners()
    {
        if (stageSpawners == null) return;

        foreach (var sp in stageSpawners)
        {
            if (sp != null)
                sp.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Активируем спавнер только для этажей леса (1..TotalStages).
    /// Для базы (0) спавнеры не включаем.
    /// </summary>
    private void ActivateSpawnerForStage(int stage)
    {
        if (stage <= 0)
        {
            Debug.Log("[RunLevelManager] Stage = 0 (база), спавнер не включаем.");
            return;
        }

        if (stageSpawners == null || stageSpawners.Length == 0)
        {
            Debug.LogWarning("[RunLevelManager] stageSpawners не настроены.");
            return;
        }

        int index = stage - 1;
        if (index < 0 || index >= stageSpawners.Length)
        {
            Debug.LogWarning("[RunLevelManager] Нет спавнера для stage = " + stage);
            return;
        }

        var sp = stageSpawners[index];
        if (sp == null)
        {
            Debug.LogWarning("[RunLevelManager] Пустой спавнер для stage = " + stage);
            return;
        }

        Debug.Log("[RunLevelManager] Активируем спавнер для stage = " + stage);
        sp.gameObject.SetActive(true);
    }

    /// <summary>
    /// Вызывается LevelVictoryController после победы.
    /// </summary>
    public void OnStageCleared()
    {
        int totalStages = TotalStages;
        Debug.Log($"[RunLevelManager] OnStageCleared. currentStage={currentStage}, totalStages={totalStages}");

        if (currentStage <= 0)
        {
            Debug.LogWarning("[RunLevelManager] Победа вызвана на базе — игнор.");
            return;
        }

        // --- уведомляем ShopKeeperManager (если включим логику "появиться после победы") ---
        ShopKeeperManager.Instance?.OnStageCleared(currentStage);

        if (stagePopup != null)
        {
            bool hasNext = currentStage < totalStages;
            stagePopup.Show(currentStage, totalStages, hasNext);
        }
        else
        {
            Debug.LogWarning("[RunLevelManager] stagePopup не назначен → сразу GoDeeper()");
            GoDeeper();
        }
    }

    /// <summary>
    /// Переход на следующий логический уровень.
    /// </summary>
    public void GoDeeper()
    {
        int totalStages = TotalStages;
        Debug.Log($"[RunLevelManager] GoDeeper. currentStage={currentStage}/{totalStages}");

        if (currentStage < totalStages)
        {
            currentStage++;

            if (stagePopup != null)
                stagePopup.Hide();

            ResetVictoryController();
            ResetPlayerPosition();
            DeactivateAllSpawners();
            ActivateSpawnerForStage(currentStage);
            UpdateHudProgress();

            // --- уведомляем менеджер ---
            ShopKeeperManager.Instance?.OnStageChanged(currentStage);
        }
        else
        {
            Debug.Log("[RunLevelManager] Все уровни пройдены → старт заново (с базы).");

            if (stagePopup != null)
                stagePopup.Hide();

            InitializeRun();
        }
    }

    /// <summary>
    /// Игрок умер → возвращаем на базу.
    /// </summary>
    public void ReturnToBaseAfterDeath()
    {
        Debug.Log("[RunLevelManager] ReturnToBaseAfterDeath → stage = 0");

        currentStage = 0;

        if (stagePopup != null)
            stagePopup.HideImmediate();

        ResetVictoryController();
        DeactivateAllSpawners();

        if (playerHealth != null)
            playerHealth.RespawnFull();

        ResetPlayerPosition();
        UpdateHudProgress();

        // --- уведомляем менеджер ---
        ShopKeeperManager.Instance?.OnStageChanged(0);
    }

    public void ReturnToMenu()
    {
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogWarning("[RunLevelManager] ReturnToMenu: нет GameFlow.Instance.");
        }
    }
}
