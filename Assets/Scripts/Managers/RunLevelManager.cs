using UnityEngine;

public class RunLevelManager : MonoBehaviour
{
    public static RunLevelManager Instance { get; private set; }

    [Header("Stages (logical levels)")]
    public int maxStages = 9;

    [SerializeField] private int currentStage = 0;
    public int CurrentStage => currentStage;

    [Header("Audio")]
    public StageMusicController music;

    public int TotalStages
    {
        get
        {
            // Always present totalStages as configured by `maxStages` so UI shows
            // the intended number of logical stages (e.g. 9), even if not all
            // spawner entries are assigned in the inspector.
            return maxStages;
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

    [Header("Shop popup")]
    [Tooltip("Перетащи сюда SoulShopKeeperPopup из Canvas. Если пусто — найдём в сцене.")]
    public SoulShopKeeperPopup shopPopup;

    public static bool inputLocked;

    [Header("Player Mana (optional assign)")]
    public PlayerMana playerMana;

    // ✅ SKULL EVENT
    [Header("Skull Event (optional)")]
    [Tooltip("Перетащи сюда компонент SkullEventController (обычно висит на RunLevelManager).")]
    public SkullEventController skullEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Ensure runtime maxStages respects minimum of 9 so UI shows all stages
        if (maxStages < 9)
        {
            Debug.LogWarning($"[RunLevelManager] maxStages was {maxStages} in inspector — forcing to 9 at runtime so UI shows all stages.");
            maxStages = 9;
        }

        EnsurePlayerMana();

        if (shopPopup == null)
            shopPopup = FindObjectOfType<SoulShopKeeperPopup>(true);

        // ✅ SKULL EVENT: если не назначили — попробуем найти на себе, затем в сцене
        if (skullEvent == null)
            skullEvent = GetComponent<SkullEventController>();
        if (skullEvent == null)
            skullEvent = FindObjectOfType<SkullEventController>(true);

        Debug.Log($"[RunLevelManager] Awake. shopPopup={(shopPopup ? shopPopup.name : "NULL")}, skullEvent={(skullEvent ? skullEvent.name : "NULL")}");
    }

    private void Start()
    {
        InitializeRun();
    }

    private void EnsurePlayerMana()
    {
        if (playerMana != null) return;

        if (playerHealth != null)
            playerMana = playerHealth.GetComponent<PlayerMana>();

        if (playerMana == null && playerTransform != null)
            playerMana = playerTransform.GetComponent<PlayerMana>();

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
    }

    public bool CanProcessGameplayInput() => !inputLocked;
    public void SetInputLocked(bool val) => inputLocked = val;

    public void InitializeRun()
    {
        currentStage = 0;

        stagePopup?.HideImmediate();
        shopPopup?.HideImmediate();

        ResetVictoryController();
        DeactivateAllSpawners();
        ResetPlayerPosition();
        UpdateHudProgress();

        music?.SetStage(currentStage);
        FillManaToMaxSafe("InitializeRun");

        // ✅ SKULL EVENT: база (stage 0) — череп выключаем
        skullEvent?.SetStage(0);

        // Новый ран = новое расписание магазина
        if (ShopKeeperManager.Instance != null)
        {
            ShopKeeperManager.Instance.GenerateRunSchedule(TotalStages);
            ShopKeeperManager.Instance.OnStageChanged(0);
        }

        // Иконки магазина над стрелками — только тут и после смерти
        interLevelUI?.ApplyShopSchedule(TotalStages);
    }

    private void ResetVictoryController()
    {
        var vc = FindObjectOfType<LevelVictoryController>();
        if (vc != null) vc.ResetForNewStage();
    }

    private void UpdateHudProgress()
    {
        if (interLevelUI != null)
            interLevelUI.SetProgress(currentStage, TotalStages);
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
        if (stage <= 0) return;

        if (stageSpawners == null || stageSpawners.Length == 0) return;

        int index = stage - 1;
        if (index < 0 || index >= stageSpawners.Length) return;

        var sp = stageSpawners[index];
        if (sp == null) return;

        sp.gameObject.SetActive(true);
    }

    public void OnStageCleared()
    {
        int totalStages = TotalStages;
        if (currentStage <= 0) return;

        bool hasNext = currentStage < totalStages;

        // Сообщим менеджеру (если нужно)
        ShopKeeperManager.Instance?.OnStageCleared(currentStage);

        // Проверяем расписание: есть ли магазин после победы на этом stage
        ShopCurrencyMode mode = ShopCurrencyMode.None;
        if (ShopKeeperManager.Instance != null)
            mode = ShopKeeperManager.Instance.GetShopModeForStage(currentStage);

        if (mode != ShopCurrencyMode.None && shopPopup != null)
        {
            // если показываем магазин — stagePopup не нужен
            stagePopup?.HideImmediate();

            bool allowCoins = true;
            bool allowSouls = (mode == ShopCurrencyMode.CoinsAndSouls);

            shopPopup.OpenAsStageClearShop(allowCoins, allowSouls);
            return;
        }

        // Иначе обычный попап перехода
        if (stagePopup != null)
            stagePopup.Show(currentStage, totalStages, hasNext);
        else
            GoDeeper();
    }

    public void GoDeeper()
    {
        int totalStages = TotalStages;

        // на всякий: прячем магазин
        shopPopup?.HideImmediate();

        if (currentStage < totalStages)
        {
            currentStage++;

            music?.SetStage(currentStage);

            stagePopup?.Hide();

            ResetVictoryController();
            ResetPlayerPosition();
            DeactivateAllSpawners();
            ActivateSpawnerForStage(currentStage);
            UpdateHudProgress();

            FillManaToMaxSafe($"Enter stage {currentStage}");

            ShopKeeperManager.Instance?.OnStageChanged(currentStage);

            // ✅ SKULL EVENT: новый stage -> стартуем расписание спавна на этот stage
            skullEvent?.SetStage(currentStage);
        }
        else
        {
            // прошли всё — переход на 1-й уровень (сброс прогресса на stage 1)
            currentStage = 1;

            music?.SetStage(currentStage);

            stagePopup?.Hide();

            ResetVictoryController();
            ResetPlayerPosition();
            DeactivateAllSpawners();
            ActivateSpawnerForStage(currentStage);
            UpdateHudProgress();

            FillManaToMaxSafe($"Enter stage {currentStage}");

            ShopKeeperManager.Instance?.OnStageChanged(currentStage);

            // ✅ SKULL EVENT: новый stage -> стартуем расписание спавна на этот stage
            skullEvent?.SetStage(currentStage);
        }
    }

    public void ReturnToBaseAfterDeath()
    {
        currentStage = 0;

        music?.SetStage(currentStage);

        stagePopup?.HideImmediate();
        shopPopup?.HideImmediate();

        ResetVictoryController();
        DeactivateAllSpawners();

        playerHealth?.RespawnFull();

        ResetPlayerPosition();
        UpdateHudProgress();
        FillManaToMaxSafe("ReturnToBaseAfterDeath");

        // ✅ SKULL EVENT: смерть -> база -> череп выключаем
        skullEvent?.SetStage(0);

        // смерть = новый ран
        if (ShopKeeperManager.Instance != null)
        {
            ShopKeeperManager.Instance.GenerateRunSchedule(TotalStages);
            ShopKeeperManager.Instance.OnStageChanged(0);
        }

        interLevelUI?.ApplyShopSchedule(TotalStages);
    }

    public void ReturnToMenu()
    {
        if (GameFlow.Instance != null) GameFlow.Instance.LoadMainMenu();
    }

    // ====== SAVE/LOAD SUPPORT ======

    /// <summary>
    /// Восстановить stage из сейва без "прокликивания" GoDeeper().
    /// </summary>
    public void SetStageFromSave(int stage)
    {
        int total = TotalStages;
        int clamped = Mathf.Clamp(stage, 0, total);

        currentStage = clamped;
        ApplyStageState_FromCurrent("SetStageFromSave");
    }

    private void ApplyStageState_FromCurrent(string reason)
    {
        // закрываем UI, магазин
        stagePopup?.HideImmediate();
        shopPopup?.HideImmediate();

        // сбрасываем победу, спавнеры, позицию
        ResetVictoryController();
        DeactivateAllSpawners();
        ResetPlayerPosition();

        // stage 0 => база, stage > 0 => включаем спавнер
        ActivateSpawnerForStage(currentStage);

        // UI/аудио/мана
        UpdateHudProgress();
        music?.SetStage(currentStage);
        FillManaToMaxSafe($"ApplyStageState ({reason}) stage={currentStage}");

        // skull event
        skullEvent?.SetStage(currentStage);

        // shopkeeper stage change (для NPC)
        ShopKeeperManager.Instance?.OnStageChanged(currentStage);

        // стрелки/иконки магазина — обновим на всякий
        interLevelUI?.ApplyShopSchedule(TotalStages);
    }
}
