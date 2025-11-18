using UnityEngine;

public class RunLevelManager : MonoBehaviour
{
    public static RunLevelManager Instance { get; private set; }

    [Header("Stages (logical levels)")]
    [Tooltip("Общее количество логических уровней (этажей леса). Обычно = числу спавнеров.")]
    public int maxStages = 8;

    [Tooltip("Текущий логический уровень (1..maxStages). Первый этаж леса = 1.")]
    [SerializeField] private int currentStage = 1;
    public int CurrentStage => currentStage;

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
    public Transform playerStartPoint;

    [Header("Spawners by stage")]
    public EnemyTopSpawner[] stageSpawners;

    [Header("UI")]
    [Tooltip("HUD с линией прогрессии (I -> II -> ( III ) -> IV).")]
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

    /// <summary>Начинаем забег с 1-го этажа леса.</summary>
    public void InitializeRun()
    {
        currentStage = 1;

        if (stagePopup != null)
            stagePopup.HideImmediate();

        ResetVictoryController();

        DeactivateAllSpawners();
        ActivateSpawnerForStage(currentStage);
        ResetPlayerPosition();
        UpdateHudProgress();
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
            Debug.Log($"[RunLevelManager] UpdateHudProgress → stage {currentStage}/{TotalStages}");
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

    private void ActivateSpawnerForStage(int stage)
    {
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

        Debug.Log("[RunLevelManager] Активируем спавнер для stage = " + stage + " → " + sp.name);
        sp.gameObject.SetActive(true);
    }

    /// <summary>
    /// Вызывается LevelVictoryController после победы,
    /// когда текст "VICTORY SOULS CAPTURED" уже отыграл.
    /// </summary>
    public void OnStageCleared()
    {
        int totalStages = TotalStages;
        Debug.Log($"[RunLevelManager] OnStageCleared. currentStage={currentStage}, totalStages={totalStages}, popupNull={stagePopup == null}");

        if (stagePopup != null)
        {
            bool hasNext = currentStage < totalStages;
            stagePopup.Show(currentStage, totalStages, hasNext);
        }
        else
        {
            Debug.LogWarning("[RunLevelManager] stagePopup не назначен. Переходим сразу на следующий уровень.");
            GoDeeper();
        }
    }

    /// <summary>Переход на следующий логический уровень (кнопка 'Войти в лес глубже...').</summary>
    public void GoDeeper()
    {
        int totalStages = TotalStages;
        Debug.Log($"[RunLevelManager] GoDeeper. currentStage={currentStage}, totalStages={totalStages}");

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
        }
        else
        {
            Debug.Log("[RunLevelManager] Все уровни пройдены. Перезапуск забега.");
            if (stagePopup != null)
                stagePopup.Hide();

            InitializeRun();
        }
    }

    public void ReturnToMenu()
    {
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogWarning("[RunLevelManager] ReturnToMenu: нет GameFlow.Instance. " +
                             "Если нужно, добавь прямую загрузку сцены меню здесь.");
        }
    }
}
