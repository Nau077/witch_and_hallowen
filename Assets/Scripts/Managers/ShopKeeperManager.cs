using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Отвечает за:
/// 1) появление/скрытие NPC на базе (stage=0)
/// 2) расписание магазина на ран: между уровнями (stage 1..totalStages-1) магазин появляется N раз,
///    фиксированно после stage 3 и перед последним уровнем, остальное — случайно.
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

    [Header("Run shop schedule (between levels)")]
    [Tooltip("Сколько раз магазин должен появиться между уровнями за один ран. По ТЗ = 4 (минимум 4).")]
    public int shopsInForestCount = 4;

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

        // Для магазина "между уровнями" нужен хотя бы один переход.
        // Пример: totalStages=9 -> валидные stage для магазина: 1..8.
        int maxBetweenStage = totalStages - 1;
        if (maxBetweenStage < 1) return;

        // Фиксированные появления:
        // 1) после 3-го уровня (stage 3)
        // 2) перед последним уровнем (stage totalStages-1)
        if (3 <= maxBetweenStage)
            _shopByStage[3] = RollForestMode();

        _shopByStage[maxBetweenStage] = RollForestMode();

        // Добираем до нужного количества появлений в лесу.
        // По ТЗ минимум 4 появления, даже если в инспекторе осталось старое значение (например 3).
        int forestNeed = Mathf.Max(4, shopsInForestCount);
        int alreadyForest = CountForestShops();
        int needMore = Mathf.Max(0, forestNeed - alreadyForest);

        // Кандидаты для случайных: только промежуточные стадии 1..(totalStages-1),
        // исключая фиксированные.
        int maxRandom = maxBetweenStage;
        List<int> candidates = new List<int>();
        for (int st = 1; st <= maxRandom; st++)
        {
            if (_shopByStage.ContainsKey(st)) continue;
            candidates.Add(st);
        }

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
