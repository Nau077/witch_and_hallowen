using UnityEngine;
using System.Collections;

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

    [Header("Level Transition FX")]
    [SerializeField] private bool enableIrisTransition = true;
    [SerializeField] private float irisCloseDuration = 0.28f;
    [SerializeField] private float irisOpenDuration = 0.24f;
    [SerializeField] private float irisHoldBlackDuration = 0.03f;

    [Header("Shop popup")]
    [Tooltip("ÐŸÐµÑ€ÐµÑ‚Ð°Ñ‰Ð¸ ÑÑŽÐ´Ð° SoulShopKeeperPopup Ð¸Ð· Canvas. Ð•ÑÐ»Ð¸ Ð¿ÑƒÑÑ‚Ð¾ â€” Ð½Ð°Ð¹Ð´Ñ‘Ð¼ Ð² ÑÑ†ÐµÐ½Ðµ.")]
    public SoulShopKeeperPopup shopPopup;

    [Header("Upgrade rewards")]
    [Tooltip("Optional: assign UpgradeRewardSystem. If empty, it will be found automatically.")]
    public UpgradeRewardSystem upgradeRewardSystem;

    public static bool inputLocked;

    [Header("Player Mana (optional assign)")]
    public PlayerMana playerMana;

    // SKULL EVENT
    [Header("Skull Event (optional)")]
    [Tooltip("ÐŸÐµÑ€ÐµÑ‚Ð°Ñ‰Ð¸ ÑÑŽÐ´Ð° ÐºÐ¾Ð¼Ð¿Ð¾Ð½ÐµÐ½Ñ‚ SkullEventController (Ð¾Ð±Ñ‹Ñ‡Ð½Ð¾ Ð²Ð¸ÑÐ¸Ñ‚ Ð½Ð° RunLevelManager).")]
    public SkullEventController skullEvent;

    [Header("Debug Cheats (optional)")]
    [Tooltip("Ð’ÐºÐ»ÑŽÑ‡Ð°ÐµÑ‚ debug-Ñ‡Ð¸Ñ‚ Ñ„Ð»Ð°Ð³ Ð¸ hotkeys.")]
    [SerializeField] private bool enableDebugCheats = false;
    [Tooltip("Ð•ÑÐ»Ð¸ Ð²ÐºÐ»ÑŽÑ‡ÐµÐ½Ð¾, Ð² Ð½Ð°Ñ‡Ð°Ð»Ðµ Ð½Ð¾Ð²Ð¾Ð³Ð¾ Ñ€Ð°Ð½Ð° Ð´Ð¾Ð±Ð°Ð²Ð»ÑÐµÑ‚ Ð±Ð¾Ð½ÑƒÑÐ½Ñ‹Ðµ coins/souls.")]
    [SerializeField] private bool grantDebugCurrenciesOnNewRun = false;
    [Min(0)] [SerializeField] private int debugBonusCoins = 300;
    [Min(0)] [SerializeField] private int debugBonusSouls = 300;
    [Tooltip("ÐŸÑ€Ð¾Ð¿ÑƒÑÐº Ð½Ð° ÑÐ»ÐµÐ´ÑƒÑŽÑ‰Ð¸Ð¹ ÑÑ‚Ð°Ð¿: Shift + D Ð¸Ð»Ð¸ Shift + L.")]
    [SerializeField] private bool enableSkipStageHotkeys = true;
    [Tooltip("Ð¡ÐºÐ¾Ð»ÑŒÐºÐ¾ ÑÐµÐºÑƒÐ½Ð´ Ð´ÐµÑ€Ð¶Ð°Ñ‚ÑŒ Ð¿Ð¾Ð¿Ð°Ð¿ Victory! Ð½Ð° Ð¿Ð¾ÑÐ»ÐµÐ´Ð½ÐµÐ¼ ÑÑ‚Ð°Ð¿Ðµ.")]
    [Min(0.1f)] [SerializeField] private float finalVictoryPopupDuration = 1.2f;

    private bool debugCurrenciesAppliedThisRun = false;
    private bool isFinalVictoryFlowRunning = false;
    private bool _isGoDeeperTransitionRunning = false;

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
            maxStages = 9;
        }

        EnsurePlayerMana();

        if (shopPopup == null)
            shopPopup = FindObjectOfType<SoulShopKeeperPopup>(true);

        if (upgradeRewardSystem == null)
            upgradeRewardSystem = FindObjectOfType<UpgradeRewardSystem>(true);

        // SKULL EVENT: ÐµÑÐ»Ð¸ Ð½Ðµ Ð½Ð°Ð·Ð½Ð°Ñ‡Ð¸Ð»Ð¸ â€” Ð¿Ð¾Ð¿Ñ€Ð¾Ð±ÑƒÐµÐ¼ Ð½Ð°Ð¹Ñ‚Ð¸ Ð½Ð° ÑÐµÐ±Ðµ, Ð·Ð°Ñ‚ÐµÐ¼ Ð² ÑÑ†ÐµÐ½Ðµ
        if (skullEvent == null)
            skullEvent = GetComponent<SkullEventController>();
        if (skullEvent == null)
            skullEvent = FindObjectOfType<SkullEventController>(true);
    }

    private void Start()
    {
        InitializeRun();
    }

    private void Update()
    {
        HandleDebugHotkeys();
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
            return;
        }

        playerMana.FillToMax();
    }

    public bool CanProcessGameplayInput() => !inputLocked;
    public void SetInputLocked(bool val) => inputLocked = val;

    public void InitializeRun()
    {
        SetInputLocked(false);
        currentStage = 0;
        debugCurrenciesAppliedThisRun = false;
        isFinalVictoryFlowRunning = false;

        stagePopup?.HideImmediate();
        shopPopup?.HideImmediate();

        ResetVictoryController();
        DeactivateAllSpawners();
        ResetPlayerPosition();
        UpdateHudProgress();

        music?.SetStage(currentStage);
        FillManaToMaxSafe("InitializeRun");

        // SKULL EVENT: Ð±Ð°Ð·Ð° (stage 0) â€” Ñ‡ÐµÑ€ÐµÐ¿ Ð²Ñ‹ÐºÐ»ÑŽÑ‡Ð°ÐµÐ¼
        skullEvent?.SetStage(0);

        // ÐÐ¾Ð²Ñ‹Ð¹ Ñ€Ð°Ð½ = Ð½Ð¾Ð²Ð¾Ðµ Ñ€Ð°ÑÐ¿Ð¸ÑÐ°Ð½Ð¸Ðµ Ð¼Ð°Ð³Ð°Ð·Ð¸Ð½Ð°
        if (ShopKeeperManager.Instance != null)
        {
            ShopKeeperManager.Instance.GenerateRunSchedule(TotalStages);
            ShopKeeperManager.Instance.OnStageChanged(0);
        }

        // Ð˜ÐºÐ¾Ð½ÐºÐ¸ Ð¼Ð°Ð³Ð°Ð·Ð¸Ð½Ð° Ð½Ð°Ð´ ÑÑ‚Ñ€ÐµÐ»ÐºÐ°Ð¼Ð¸ â€” Ñ‚Ð¾Ð»ÑŒÐºÐ¾ Ñ‚ÑƒÑ‚ Ð¸ Ð¿Ð¾ÑÐ»Ðµ ÑÐ¼ÐµÑ€Ñ‚Ð¸
        interLevelUI?.ApplyShopSchedule(TotalStages);

        TryApplyDebugCurrenciesForNewRun();
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
        if (isFinalVictoryFlowRunning)
            return;

        if (currentStage <= 0) return;

        NoDeathStreakRecord.RegisterStageCleared(currentStage, TotalStages);

        ShopKeeperManager.Instance?.OnStageCleared(currentStage);

        if (upgradeRewardSystem != null &&
            upgradeRewardSystem.TryTriggerStageCleared(currentStage, ContinueStageClearFlow))
        {
            return;
        }

        ContinueStageClearFlow();
    }

    private void ContinueStageClearFlow()
    {
        int totalStages = TotalStages;
        bool hasNext = currentStage < totalStages;

        if (!hasNext)
        {
            StartCoroutine(FinalVictoryAndLoopRoutine());
            return;
        }

        ShopCurrencyMode mode = ShopCurrencyMode.None;
        if (ShopKeeperManager.Instance != null)
            mode = ShopKeeperManager.Instance.GetShopModeForStage(currentStage);

        if (mode != ShopCurrencyMode.None && shopPopup != null)
        {
            stagePopup?.HideImmediate();

            bool allowCoins = true;
            bool allowSouls = true;

            shopPopup.OpenAsStageClearShop(allowCoins, allowSouls);
            return;
        }

        if (stagePopup != null)
            stagePopup.Show(currentStage, totalStages, hasNext);
        else
            GoDeeper();
    }

    public void GoDeeper()
    {
        if (_isGoDeeperTransitionRunning)
            return;

        if (enableIrisTransition)
        {
            StartCoroutine(GoDeeperWithTransitionRoutine());
            return;
        }

        ExecuteGoDeeperNow();
    }

    private IEnumerator GoDeeperWithTransitionRoutine()
    {
        _isGoDeeperTransitionRunning = true;
        SetInputLocked(true);

        try
        {
            yield return IrisScreenTransition.Close(Mathf.Max(0.01f, irisCloseDuration));
            yield return IrisScreenTransition.HoldBlack(Mathf.Max(0f, irisHoldBlackDuration));
            ExecuteGoDeeperNow();
            yield return IrisScreenTransition.Open(Mathf.Max(0.01f, irisOpenDuration));
        }
        finally
        {
            SetInputLocked(false);
            _isGoDeeperTransitionRunning = false;
        }
    }

    private void ExecuteGoDeeperNow()
    {
        int totalStages = TotalStages;

        // Ð½Ð° Ð²ÑÑÐºÐ¸Ð¹: Ð¿Ñ€ÑÑ‡ÐµÐ¼ Ð¼Ð°Ð³Ð°Ð·Ð¸Ð½
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

            // SKULL EVENT: Ð½Ð¾Ð²Ñ‹Ð¹ stage -> ÑÑ‚Ð°Ñ€Ñ‚ÑƒÐµÐ¼ Ñ€Ð°ÑÐ¿Ð¸ÑÐ°Ð½Ð¸Ðµ ÑÐ¿Ð°Ð²Ð½Ð° Ð½Ð° ÑÑ‚Ð¾Ñ‚ stage
            skullEvent?.SetStage(currentStage);
        }
        else
        {
            // Ð¿Ñ€Ð¾ÑˆÐ»Ð¸ Ð²ÑÑ‘ â€” Ð¿ÐµÑ€ÐµÑ…Ð¾Ð´ Ð½Ð° 1-Ð¹ ÑƒÑ€Ð¾Ð²ÐµÐ½ÑŒ (ÑÐ±Ñ€Ð¾Ñ Ð¿Ñ€Ð¾Ð³Ñ€ÐµÑÑÐ° Ð½Ð° stage 1)
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

            // SKULL EVENT: Ð½Ð¾Ð²Ñ‹Ð¹ stage -> ÑÑ‚Ð°Ñ€Ñ‚ÑƒÐµÐ¼ Ñ€Ð°ÑÐ¿Ð¸ÑÐ°Ð½Ð¸Ðµ ÑÐ¿Ð°Ð²Ð½Ð° Ð½Ð° ÑÑ‚Ð¾Ñ‚ stage
            skullEvent?.SetStage(currentStage);
        }

        // Новый stage всегда должен запускаться с доступным управлением.
        SetInputLocked(false);
    }

    public void ReturnToBaseAfterDeath()
    {
        SetInputLocked(false);
        currentStage = 0;
        debugCurrenciesAppliedThisRun = false;
        isFinalVictoryFlowRunning = false;

        music?.SetStage(currentStage);

        stagePopup?.HideImmediate();
        shopPopup?.HideImmediate();

        ResetVictoryController();
        DeactivateAllSpawners();

        playerHealth?.RespawnFull();

        ResetPlayerPosition();
        UpdateHudProgress();
        FillManaToMaxSafe("ReturnToBaseAfterDeath");

        // SKULL EVENT: ÑÐ¼ÐµÑ€Ñ‚ÑŒ -> Ð±Ð°Ð·Ð° -> Ñ‡ÐµÑ€ÐµÐ¿ Ð²Ñ‹ÐºÐ»ÑŽÑ‡Ð°ÐµÐ¼
        skullEvent?.SetStage(0);

        // ÑÐ¼ÐµÑ€Ñ‚ÑŒ = Ð½Ð¾Ð²Ñ‹Ð¹ Ñ€Ð°Ð½
        if (ShopKeeperManager.Instance != null)
        {
            ShopKeeperManager.Instance.GenerateRunSchedule(TotalStages);
            ShopKeeperManager.Instance.OnStageChanged(0);
        }

        interLevelUI?.ApplyShopSchedule(TotalStages);

        TryApplyDebugCurrenciesForNewRun();
    }

    public void ReturnToMenu()
    {
        if (GameFlow.Instance != null) GameFlow.Instance.LoadMainMenu();
    }

    // ====== SAVE/LOAD SUPPORT ======

    /// <summary>
    /// Ð’Ð¾ÑÑÑ‚Ð°Ð½Ð¾Ð²Ð¸Ñ‚ÑŒ stage Ð¸Ð· ÑÐµÐ¹Ð²Ð° Ð±ÐµÐ· "Ð¿Ñ€Ð¾ÐºÐ»Ð¸ÐºÐ¸Ð²Ð°Ð½Ð¸Ñ" GoDeeper().
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
        // Ð·Ð°ÐºÑ€Ñ‹Ð²Ð°ÐµÐ¼ UI, Ð¼Ð°Ð³Ð°Ð·Ð¸Ð½
        stagePopup?.HideImmediate();
        shopPopup?.HideImmediate();

        // ÑÐ±Ñ€Ð°ÑÑ‹Ð²Ð°ÐµÐ¼ Ð¿Ð¾Ð±ÐµÐ´Ñƒ, ÑÐ¿Ð°Ð²Ð½ÐµÑ€Ñ‹, Ð¿Ð¾Ð·Ð¸Ñ†Ð¸ÑŽ
        ResetVictoryController();
        DeactivateAllSpawners();
        ResetPlayerPosition();

        // stage 0 => Ð±Ð°Ð·Ð°, stage > 0 => Ð²ÐºÐ»ÑŽÑ‡Ð°ÐµÐ¼ ÑÐ¿Ð°Ð²Ð½ÐµÑ€
        ActivateSpawnerForStage(currentStage);

        // UI/Ð°ÑƒÐ´Ð¸Ð¾/Ð¼Ð°Ð½Ð°
        UpdateHudProgress();
        music?.SetStage(currentStage);
        FillManaToMaxSafe($"ApplyStageState ({reason}) stage={currentStage}");

        // skull event
        skullEvent?.SetStage(currentStage);

        // shopkeeper stage change (Ð´Ð»Ñ NPC)
        ShopKeeperManager.Instance?.OnStageChanged(currentStage);

        // ÑÑ‚Ñ€ÐµÐ»ÐºÐ¸/Ð¸ÐºÐ¾Ð½ÐºÐ¸ Ð¼Ð°Ð³Ð°Ð·Ð¸Ð½Ð° â€” Ð¾Ð±Ð½Ð¾Ð²Ð¸Ð¼ Ð½Ð° Ð²ÑÑÐºÐ¸Ð¹
        interLevelUI?.ApplyShopSchedule(TotalStages);
    }

    [ContextMenu("Apply Debug Currencies Now")]
    public void ApplyDebugCurrenciesNow()
    {
        TryApplyDebugCurrencies(force: true);
    }

    private void TryApplyDebugCurrenciesForNewRun()
    {
        if (debugCurrenciesAppliedThisRun)
            return;

        if (TryApplyDebugCurrencies(force: false))
            debugCurrenciesAppliedThisRun = true;
    }

    private bool TryApplyDebugCurrencies(bool force)
    {
        if (!enableDebugCheats)
            return false;

        if (!force && !grantDebugCurrenciesOnNewRun)
            return false;

        bool applied = false;

        if (debugBonusCoins > 0)
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.Add(debugBonusCoins);
                applied = true;
            }
            else
            {
                Debug.LogWarning("[RunLevelManager] PlayerWallet.Instance is null, debug coins were not added.");
            }
        }

        if (debugBonusSouls > 0)
        {
            if (SoulCounter.Instance != null)
            {
                SoulCounter.Instance.AddSouls(debugBonusSouls);
                applied = true;
            }
            else
            {
                Debug.LogWarning("[RunLevelManager] SoulCounter.Instance is null, debug souls were not added.");
            }
        }

        if (applied)
            SoulCounter.Instance?.RefreshUI();

        return applied;
    }

    private void HandleDebugHotkeys()
    {
        if (!enableDebugCheats || !enableSkipStageHotkeys)
            return;

        if (isFinalVictoryFlowRunning)
            return;

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shiftPressed)
            return;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.L))
            ForceSkipToNextStage();
    }

    private void ForceSkipToNextStage()
    {
        if (currentStage < TotalStages)
        {
            GoDeeper();
            return;
        }

        if (currentStage == TotalStages)
            StartCoroutine(FinalVictoryAndLoopRoutine());
    }

    private IEnumerator FinalVictoryAndLoopRoutine()
    {
        if (isFinalVictoryFlowRunning)
            yield break;

        isFinalVictoryFlowRunning = true;
        SetInputLocked(true);

        stagePopup?.HideImmediate();
        shopPopup?.HideImmediate();

        if (WitchIsDeadPopup.Instance != null)
        {
            WitchIsDeadPopup.Instance.Show("Victory!");
            yield return new WaitForSeconds(finalVictoryPopupDuration);
            WitchIsDeadPopup.Instance.HideImmediate();
        }
        else
        {
            Debug.LogWarning("[RunLevelManager] WitchIsDeadPopup.Instance is null, showing final victory delay without popup.");
            yield return new WaitForSeconds(finalVictoryPopupDuration);
        }

        SetInputLocked(false);
        isFinalVictoryFlowRunning = false;

        InitializeRun();
    }
}




