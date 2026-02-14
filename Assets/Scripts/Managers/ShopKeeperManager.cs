using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Отвечает за:
/// 1) появление/скрытие NPC на базе (stage=0)
/// 2) расписание магазина на ран: в лесу (1..7) магазин появляется 3 раза, stage 7 обязателен,
///    ещё 2 раза случайно среди 1..6. В эти 2 появления режим либо CoinsOnly, либо CoinsAndSouls.
/// 3) выдаёт режим магазина для StageTransitionPopup / InterLevelUI.
/// </summary>
public class ShopKeeperManager : MonoBehaviour
{
    public static ShopKeeperManager Instance { get; private set; }

    [Header("NPC reference")]
    [Tooltip("Сам объект с SoulShopKeeper (ведьмочка-продавец) на базе/сцене, если нужен.")]
    public GameObject shopKeeperNPC;

    [Header("Base rules")]
    [Tooltip("Появляется ли NPC на базе (stage = 0).")]
    public bool appearOnBase = true;

    [Tooltip("Появляется ли NPC сразу после победы на этапе (для комнат награды).")]
    public bool appearAfterStageClear = false;

    [Header("Run shop schedule (stages 1..7)")]
    [Tooltip("Сколько раз магазин должен появиться в лесу (stage 1..7) за один ран. ДОЛЖНО быть 3 по ТЗ.")]
    public int shopsInForestCount = 3;

    [Tooltip("Этаж, где магазин появляется всегда (по ТЗ = 7).")]
    public int guaranteedForestStage = 7;

    [Tooltip("Вероятность, что магазин в лесу будет только за монеты. Иначе будет за монеты+души.")]
    [Range(0f, 1f)] public float coinsOnlyChance = 0.5f;

    // schedule: stage -> mode
    private readonly Dictionary<int, ShopCurrencyMode> _shopByStage = new Dictionary<int, ShopCurrencyMode>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Генерирует расписание магазина на текущий ран.
    /// Вызывать при старте рана (InitializeRun) и при смерти (ReturnToBaseAfterDeath), т.к. ран сбрасывается.
    /// totalStages — сколько логических стадий вообще есть у RunLevelManager (включая 0).
    /// </summary>
    public void GenerateRunSchedule(int totalStages)
    {
        _shopByStage.Clear();

        // База (0) — всегда есть магазин и ВСЕГДА CoinsAndSouls
        _shopByStage[0] = ShopCurrencyMode.CoinsAndSouls;

        // Если стадий мало — выходим
        if (totalStages < 1) return;

        // Гарантированный stage 7 должен быть достижим
        int guaranteed = Mathf.Clamp(guaranteedForestStage, 1, totalStages);

        // 7-й всегда магазин (если достижим)
        _shopByStage[guaranteed] = RollForestMode();

        // Добираем до нужного количества появлений в лесу
        int forestNeed = Mathf.Max(0, shopsInForestCount);
        int alreadyForest = CountForestShops();
        int needMore = Mathf.Max(0, forestNeed - alreadyForest);

        // Кандидаты для случайных: 1..(guaranteed-1)
        int maxRandom = Mathf.Min(guaranteed - 1, totalStages);
        List<int> candidates = new List<int>();
        for (int st = 1; st <= maxRandom; st++) candidates.Add(st);

        while (needMore > 0 && candidates.Count > 0)
        {
            int idx = Random.Range(0, candidates.Count);
            int stage = candidates[idx];
            candidates.RemoveAt(idx);

            if (!_shopByStage.ContainsKey(stage))
            {
                _shopByStage[stage] = RollForestMode();
                needMore--;
            }
        }

    }

    private int CountForestShops()
    {
        int c = 0;
        foreach (var kv in _shopByStage)
        {
            if (kv.Key >= 1) c++;
        }
        return c;
    }

    private ShopCurrencyMode RollForestMode()
    {
        return (Random.value < coinsOnlyChance) ? ShopCurrencyMode.CoinsOnly : ShopCurrencyMode.CoinsAndSouls;
    }

    public ShopCurrencyMode GetShopModeForStage(int stage)
    {
        return _shopByStage.TryGetValue(stage, out var mode) ? mode : ShopCurrencyMode.None;
    }

    public bool HasShopOnStage(int stage) => GetShopModeForStage(stage) != ShopCurrencyMode.None;

    /// <summary>
    /// Вызывается RunLevelManager, когда активный этаж ИЗМЕНИЛСЯ.
    /// Здесь мы управляем NPC на базе (по твоей старой логике).
    /// </summary>
    public void OnStageChanged(int stage)
    {
        if (shopKeeperNPC == null) return;

        bool shouldAppear = false;

        // база
        if (stage == 0 && appearOnBase)
            shouldAppear = true;

        shopKeeperNPC.SetActive(shouldAppear);
    }

    /// <summary>
    /// Вызывается RunLevelManager при победе на этаже (stage очищен).
    /// Если включён appearAfterStageClear — можно показать NPC.
    /// (UI-магазин после победы мы делаем через StageTransitionPopup.)
    /// </summary>
    public void OnStageCleared(int stage)
    {
        if (!appearAfterStageClear) return;
        if (shopKeeperNPC != null) shopKeeperNPC.SetActive(true);
    }

    private string DebugScheduleToString()
    {
        List<int> keys = new List<int>(_shopByStage.Keys);
        keys.Sort();
        string s = "";
        foreach (var k in keys) s += $"[{k}:{_shopByStage[k]}] ";
        return s;
    }
}
