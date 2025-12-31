using UnityEngine;

public class RunLevelManager : MonoBehaviour
{
    public static RunLevelManager Instance { get; private set; }

    [Header("Stages (logical levels)")]
    public int maxStages = 8;

    [SerializeField] private int currentStage = 0;
    public int CurrentStage => currentStage;

    [Header("Audio")]
    public StageMusicController music;

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
    public InterLevelUI interLevelUI;
    public StageTransitionPopup stagePopup;

    public static bool inputLocked;

    [Header("Player Mana (optional assign)")]
    public PlayerMana playerMana;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // <-- ВАЖНО: подхватываем ману даже если не назначили в инспекторе
        EnsurePlayerMana();

        Debug.Log($"[RunLevelManager] Awake. Instance установлен. playerMana={(playerMana ? playerMana.name : "NULL")}");
    }

    private void Start()
    {
        Debug.Log("[RunLevelManager] Start → InitializeRun()");
        InitializeRun();
    }

    private void EnsurePlayerMana()
    {
        if (playerMana != null) return;

        // 1) пробуем с playerHealth
        if (playerHealth != null)
            playerMana = playerHealth.GetComponent<PlayerMana>();

        // 2) пробуем с playerTransform
        if (playerMana == null && playerTransform != null)
            playerMana = playerTransform.GetComponent<PlayerMana>();

        // 3) крайний случай: ищем в сцене
        if (playerMana == null)
            playerMana = FindObjectOfType<PlayerMana>(true);
    }

    private void FillManaToMaxSafe(string reason)
    {
        EnsurePlayerMana();
        if (playerMana == null)
        {
            Debug.LogWarning($"[RunLevelManager] FillManaToMaxSafe skipped ({reason}) — playerMana is NULL");
            return;
        }

        playerMana.FillToMax();
        Debug.Log($"[RunLevelManager] Mana filled to max ({reason}). currentMana={playerMana.currentMana}/{playerMana.maxMana}");
    }

    public bool CanProcessGameplayInput()
    {
        return !inputLocked;
    }

    public void SetInputLocked(bool val)
    {
        inputLocked = val;
    }

    public void InitializeRun()
    {
        currentStage = 0;

        if (stagePopup != null)
            stagePopup.HideImmediate();

        ResetVictoryController();

        DeactivateAllSpawners();
        ResetPlayerPosition();
        UpdateHudProgress();

        music?.SetStage(currentStage);

        // На базе тоже можно фуллить (не мешает, зато дебаг проще)
        FillManaToMaxSafe("InitializeRun (base)");

        ShopKeeperManager.Instance?.OnStageChanged(0);
    }

    private void ResetVictoryController()
    {
        var vc = FindObjectOfType<LevelVictoryController>();
        if (vc != null)
            vc.ResetForNewStage();
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
            playerTransform.position = playerStartPoint.position;
    }

    private void DeactivateAllSpawners()
    {
        if (stageSpawners == null) return;
        foreach (var sp in stageSpawners)
            if (sp != null) sp.gameObject.SetActive(false);
    }

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

    public void OnStageCleared()
    {
        int totalStages = TotalStages;
        Debug.Log($"[RunLevelManager] OnStageCleared. currentStage={currentStage}, totalStages={totalStages}");

        if (currentStage <= 0)
        {
            Debug.LogWarning("[RunLevelManager] Победа вызвана на базе — игнор.");
            return;
        }

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

    public void GoDeeper()
    {
        int totalStages = TotalStages;
        Debug.Log($"[RunLevelManager] GoDeeper. currentStage={currentStage}/{totalStages}");

        if (currentStage < totalStages)
        {
            currentStage++;

            music?.SetStage(currentStage);

            if (stagePopup != null)
                stagePopup.Hide();

            ResetVictoryController();
            ResetPlayerPosition();
            DeactivateAllSpawners();
            ActivateSpawnerForStage(currentStage);
            UpdateHudProgress();

            // <-- ВАЖНО: фуллим после входа на новый stage (а не до)
            FillManaToMaxSafe($"Enter stage {currentStage}");

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

    public void ReturnToBaseAfterDeath()
    {
        Debug.Log("[RunLevelManager] ReturnToBaseAfterDeath → stage = 0");

        currentStage = 0;

        music?.SetStage(currentStage);

        if (stagePopup != null)
            stagePopup.HideImmediate();

        ResetVictoryController();
        DeactivateAllSpawners();

        if (playerHealth != null)
            playerHealth.RespawnFull();

        ResetPlayerPosition();
        UpdateHudProgress();

        // На базе тоже фуллим, чтобы после смерти всё было предсказуемо
        FillManaToMaxSafe("ReturnToBaseAfterDeath");

        ShopKeeperManager.Instance?.OnStageChanged(0);
    }

    public void ReturnToMenu()
    {
        if (GameFlow.Instance != null) GameFlow.Instance.LoadMainMenu();
        else Debug.LogWarning("[RunLevelManager] ReturnToMenu: нет GameFlow.Instance.");
    }
}
