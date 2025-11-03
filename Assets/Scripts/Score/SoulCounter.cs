using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-100)]
public class SoulCounter : MonoBehaviour
{
    public static SoulCounter Instance { get; private set; }

    // ---------- KEYS ----------
    private const string KILLS_KEY = "kills_lifetime";
    private const string LAST_RUN_GOLD_KEY = "gold_last_run"; // для экрана смерти/итогов

    // ---------- DATA ----------
    [Header("Scores")]
    [Min(0)] public int killsLifetime = 0; // живёт между забегами и сессиями
    [Min(0)] public int cursedGoldRun = 0; // живёт в рамках забега

    [Tooltip("Если EnemyHealth не укажет своё значение, возьмём это.")]
    public int defaultGoldPerKill = 10;

    // ---------- UI ----------
    [Header("UI (TMP)")]
    [SerializeField] private TMP_Text killsText; // иконка-череп + число
    [SerializeField] private TMP_Text goldText;  // иконка-золото + число

    // ---------- VICTORY SNAPSHOT / LOCK ----------
    // Пока активен экран победы, запрещаем неожиданный сброс золота
    // и храним "снимок" цифр, чтобы их не "сдуло" сценой/загрузкой.
    private bool victoryLock = false;
    private int snapshotKills = 0;
    private int snapshotGold = 0;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // загрузка прогресса
        killsLifetime = PlayerPrefs.GetInt(KILLS_KEY, 0);
        cursedGoldRun = 0; // золото — только текущий забег

        UpdateUI(updateKills: true, updateGold: true);
    }

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
    }

    // === Вызывается из EnemyHealth.OnAnyEnemyDied ===
    private void HandleEnemyDied(EnemyHealth enemy)
    {
        int addGold = (enemy != null) ? Mathf.Max(0, enemy.cursedGoldOnDeath) : defaultGoldPerKill;

        // 1) убийства — +1 и сохраняем навсегда
        killsLifetime++;
        PlayerPrefs.SetInt(KILLS_KEY, killsLifetime);

        // 2) золото забега — +N (живёт только этот ран)
        cursedGoldRun += addGold;

        PlayerPrefs.Save();
        UpdateUI(updateKills: true, updateGold: true);

        // ---- ПОПАПЫ ----
        Vector3 pos = enemy ? enemy.transform.position : Vector3.zero;
        SoulPopup.Create(pos, 1, SoulPopup.PopupType.Souls);
        SoulPopup.Create(pos, addGold, SoulPopup.PopupType.CursedGold, snapshotGold, cursedGoldRun);
    }

    // === ТОЛЬКО при смерти игрока ===
    public void ResetRunGold_OnDeathOrRestart()
    {
        // Если открыт экран победы — игнорируем случайный сброс
        if (victoryLock)
        {
            Debug.LogWarning("[SoulCounter] Run gold reset ignored during Victory (lock active).");
            return;
        }

        PlayerPrefs.SetInt(LAST_RUN_GOLD_KEY, cursedGoldRun);
        PlayerPrefs.Save();

        cursedGoldRun = 0;
        UpdateUI(updateKills: false, updateGold: true);
    }

    // === Экран смерти может забирать золото прошлого забега ===
    public int GetLastRunGoldAndClear(bool clear = false)
    {
        int last = PlayerPrefs.GetInt(LAST_RUN_GOLD_KEY, 0);
        if (clear)
        {
            PlayerPrefs.SetInt(LAST_RUN_GOLD_KEY, 0);
            PlayerPrefs.Save();
        }
        return last;
    }

    // Полный сброс прогресса убийств (метапрогресс)
    public void ResetKillsLifetime()
    {
        killsLifetime = 0;
        PlayerPrefs.SetInt(KILLS_KEY, 0);
        PlayerPrefs.Save();
        UpdateUI(updateKills: true, updateGold: false);
    }

    private void UpdateUI(bool updateKills, bool updateGold)
    {
        if (updateKills && killsText) killsText.text = killsLifetime.ToString();
        if (updateGold && goldText) goldText.text = cursedGoldRun.ToString();
    }

    // ---------- PUBLIC API ДЛЯ ЭКРАНА ПОБЕДЫ ----------
    // Фиксируем значения и включаем "замок" от сброса
    public void BeginVictorySequence()
    {
        victoryLock = true;
        snapshotKills = killsLifetime;
        snapshotGold = cursedGoldRun;
    }

    // Вызывай, когда закрываешь экран победы (Continue/Next Level)
    public void EndVictorySequence()
    {
        victoryLock = false;
    }

    // Отдать зафиксированные числа; если снимка нет — отдаём текущие
    public void GetVictorySnapshot(out int kills, out int gold)
    {
        if (victoryLock)
        {
            kills = snapshotKills;
            gold = snapshotGold;
        }
        else
        {
            kills = killsLifetime;
            gold = cursedGoldRun;
        }
    }

    // Геттеры
    public int Kills => killsLifetime;
    public int RunGold => cursedGoldRun;
}
