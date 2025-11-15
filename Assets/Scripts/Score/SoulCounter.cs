using UnityEngine;
using TMPro;
using System.Collections;

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
    [SerializeField] private TMP_Text killsText; // иконка-череп + число (души)
    [SerializeField] private TMP_Text goldText;  // иконка-золото + число

    // ---------- AUDIO (опционально) ----------
    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip killSfx;   // звук убийства / душ
    public AudioClip goldSfx;   // звон монет

    // ---------- VICTORY SNAPSHOT / LOCK ----------
    private bool victoryLock = false;
    private int snapshotKills = 0;
    private int snapshotGold = 0;

    // ---------- ANIMATION STATE (числа) ----------
    private Coroutine killsAnimRoutine;
    private Coroutine goldAnimRoutine;

    // ---------- ANIMATION STATE (scale-прыжок) ----------
    [Header("Counter Punch Animation")]
    [Tooltip("Во сколько раз увеличивать текст при «прыжке».")]
    public float counterPunchScale = 1.2f;
    [Tooltip("Время на полный цикл (увеличить и вернуть).")]
    public float counterPunchTime = 0.16f;

    private Coroutine killsScaleRoutine;
    private Coroutine goldScaleRoutine;

    private Vector3 killsBaseScale = Vector3.one;
    private Vector3 goldBaseScale = Vector3.one;

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

        // базовый масштаб текстов
        if (killsText)
            killsBaseScale = killsText.rectTransform.localScale;
        if (goldText)
            goldBaseScale = goldText.rectTransform.localScale;

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

        // старые значения для анимации
        int oldKills = killsLifetime;
        int oldGold = cursedGoldRun;

        // 1) убийства — +1 и сохраняем навсегда
        killsLifetime++;
        PlayerPrefs.SetInt(KILLS_KEY, killsLifetime);

        // 2) золото забега — +N (живёт только этот ран)
        cursedGoldRun += addGold;

        PlayerPrefs.Save();

        // ---- АУДИО (если настроено) ----
        PlayKillSound();
        if (addGold > 0) PlayGoldSound();

        // ---- АНИМАЦИЯ СЧЁТЧИКОВ (числа + scale-прыжок) ----
        AnimateKills(oldKills, killsLifetime);
        if (addGold != 0)
            AnimateGold(oldGold, cursedGoldRun);
        else
            UpdateUI(updateKills: false, updateGold: true); // синхронизация, если золота нет

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
    public void BeginVictorySequence()
    {
        victoryLock = true;
        snapshotKills = killsLifetime;
        snapshotGold = cursedGoldRun;
    }

    public void EndVictorySequence()
    {
        victoryLock = false;
    }

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

    // ---------- ANIMATION HELPERS (число) ----------

    private void AnimateKills(int from, int to)
    {
        if (!killsText) return;

        if (killsAnimRoutine != null)
            StopCoroutine(killsAnimRoutine);

        killsAnimRoutine = StartCoroutine(AnimateIntRoutine(from, to, killsText));

        // scale-прыжок счётчика душ
        AnimateTextPunch(killsText, ref killsScaleRoutine, killsBaseScale);
    }

    private void AnimateGold(int from, int to)
    {
        if (!goldText) return;

        if (goldAnimRoutine != null)
            StopCoroutine(goldAnimRoutine);

        goldAnimRoutine = StartCoroutine(AnimateIntRoutine(from, to, goldText));

        // scale-прыжок счётчика золота
        AnimateTextPunch(goldText, ref goldScaleRoutine, goldBaseScale);
    }

    private IEnumerator AnimateIntRoutine(int from, int to, TMP_Text targetText)
    {
        int current = from;
        targetText.text = current.ToString();

        if (from == to)
            yield break;

        int step = (to > from) ? 1 : -1;
        const float stepDelay = 0.03f; // скорость перебора

        while (current != to)
        {
            current += step;
            targetText.text = current.ToString();
            yield return new WaitForSeconds(stepDelay);
        }
    }

    // ---------- ANIMATION HELPERS (scale-прыжок) ----------

    private void AnimateTextPunch(TMP_Text text, ref Coroutine routine, Vector3 baseScale)
    {
        if (!text) return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(TextPunchRoutine(text.rectTransform, baseScale));
    }

    private IEnumerator TextPunchRoutine(RectTransform rect, Vector3 baseScale)
    {
        if (!rect) yield break;

        float halfTime = counterPunchTime * 0.5f;
        float t = 0f;

        // Увеличиваем
        while (t < halfTime)
        {
            t += Time.unscaledDeltaTime; // чтобы не зависеть от Time.timeScale
            float k = Mathf.Clamp01(t / halfTime);
            float s = Mathf.Lerp(1f, counterPunchScale, k);
            rect.localScale = baseScale * s;
            yield return null;
        }

        // Возвращаем к базовому
        t = 0f;
        while (t < halfTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / halfTime);
            float s = Mathf.Lerp(counterPunchScale, 1f, k);
            rect.localScale = baseScale * s;
            yield return null;
        }

        rect.localScale = baseScale;
    }

    // ---------- AUDIO HELPERS ----------

    private void PlayKillSound()
    {
        if (audioSource && killSfx)
            audioSource.PlayOneShot(killSfx, 1f);
    }

    private void PlayGoldSound()
    {
        if (audioSource && goldSfx)
            audioSource.PlayOneShot(goldSfx, 1f);
    }
}
