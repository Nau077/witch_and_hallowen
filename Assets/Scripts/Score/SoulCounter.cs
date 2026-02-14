using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class SoulCounter : MonoBehaviour
{
    public static SoulCounter Instance { get; private set; }

    // ---------- KEYS ----------
    // ⚠️ Оставляем старый ключ, чтобы не потерять прогресс игроков
    private const string SOULS_KEY = "kills_lifetime";
    private const string LAST_RUN_COINS_KEY = "coins_last_run"; // для экрана смерти/итогов (если нужно)

    // ---------- DATA ----------
    [Header("Currencies")]
    [Min(0)] public int souls = 0; // ✅ перманентно
    [Min(0)] public int coins = 0; // ✅ витрина coins (истина = PlayerWallet.coins)

    [Tooltip("Если EnemyHealth не укажет своё значение, возьмём это.")]
    public int defaultCoinsPerKill = 10;

    // ---------- UI ----------
    [Header("UI (TMP)")]
    [SerializeField] private TMP_Text soulsText; // было killsText
    [SerializeField] private TMP_Text coinsText; // было goldText

    // ---------- AUDIO (optional) ----------
    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip soulSfx;   // звук душ
    public AudioClip coinSfx;   // звон монет

    // ---------- VICTORY SNAPSHOT / LOCK ----------
    private bool victoryLock = false;
    private int snapshotSouls = 0;
    private int snapshotCoins = 0;

    // ---------- ANIMATION STATE (numbers) ----------
    private Coroutine soulsAnimRoutine;
    private Coroutine coinsAnimRoutine;

    // ---------- ANIMATION STATE (scale punch) ----------
    [Header("Counter Punch Animation")]
    public float counterPunchScale = 1.2f;
    public float counterPunchTime = 0.16f;

    private Coroutine soulsScaleRoutine;
    private Coroutine coinsScaleRoutine;

    private Vector3 soulsBaseScale = Vector3.one;
    private Vector3 coinsBaseScale = Vector3.one;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Transform root = transform.root;
        if (root != null)
            DontDestroyOnLoad(root.gameObject);
        else
            DontDestroyOnLoad(gameObject);

        // базовый масштаб текстов
        if (soulsText)
            soulsBaseScale = soulsText.rectTransform.localScale;
        if (coinsText)
            coinsBaseScale = coinsText.rectTransform.localScale;

        // загрузка перманентных душ
        souls = PlayerPrefs.GetInt(SOULS_KEY, 0);

        // coins только ран → начинаем с 0, но если кошелёк уже существует — синхроним
        SyncCoinsFromWallet();

        UpdateUI(updateSouls: true, updateCoins: true);
    }

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
    }

    // ---------- PUBLIC API: souls ----------
    public void SetSouls(int value)
    {
        souls = Mathf.Max(0, value);
        PlayerPrefs.SetInt(SOULS_KEY, souls);
        PlayerPrefs.Save();
        UpdateUI(updateSouls: true, updateCoins: false);
    }

    public void AddSouls(int amount)
    {
        if (amount <= 0) return;
        souls += amount;
        PlayerPrefs.SetInt(SOULS_KEY, souls);
        PlayerPrefs.Save();
        UpdateUI(updateSouls: true, updateCoins: false);
    }

    // ---------- COINS SYNC ----------
    /// <summary>
    /// Витрина coins в SoulCounter обновляется из PlayerWallet (истина coins).
    /// </summary>
    public void SyncCoinsFromWallet()
    {
        if (PlayerWallet.Instance != null)
            coins = Mathf.Max(0, PlayerWallet.Instance.coins);
        else
            coins = Mathf.Max(0, coins);
    }

    // === Вызывается из EnemyHealth.OnAnyEnemyDied ===
    private void HandleEnemyDied(EnemyHealth enemy)
    {
        int addCoins = (enemy != null) ? Mathf.Max(0, enemy.cursedGoldOnDeath) : defaultCoinsPerKill;

        // старые значения для анимации
        int oldSouls = souls;
        int oldCoins = coins;

        // 1) souls — +1 и сохраняем навсегда
        souls++;
        PlayerPrefs.SetInt(SOULS_KEY, souls);
        PlayerPrefs.Save();

        // 2) coins — +N в кошелёк (истина coins)
        if (PlayerWallet.Instance != null && addCoins > 0)
            PlayerWallet.Instance.Add(addCoins);

        // 3) синхронизируем витрину coins
        SyncCoinsFromWallet();

        // ---- AUDIO ----
        PlaySoulSound();
        if (addCoins > 0) PlayCoinSound();

        // ---- ANIM ----
        AnimateSouls(oldSouls, souls);
        if (addCoins != 0)
            AnimateCoins(oldCoins, coins);
        else
            UpdateUI(updateSouls: false, updateCoins: true);

        // ---- POPUPS ----
        Vector3 pos = enemy ? enemy.transform.position : Vector3.zero;
        SoulPopup.Create(pos, 1, SoulPopup.PopupType.Souls);
        SoulPopup.Create(pos, addCoins, SoulPopup.PopupType.CursedGold, oldCoins, coins);
    }

    /// <summary>
    /// Вызывать при смерти игрока/рестарте рана.
    /// coins сбрасываются, souls — нет.
    /// </summary>
    public void ResetRunCoins_OnDeathOrRestart()
    {
        if (victoryLock)
        {
            Debug.LogWarning("[SoulCounter] Run coins reset ignored during Victory (lock active).");
            return;
        }

        // если нужно отображать "coins last run" где-то
        PlayerPrefs.SetInt(LAST_RUN_COINS_KEY, coins);
        PlayerPrefs.Save();

        // Сброс coins в истине
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.ResetRunCoins();

        // Сброс витрины
        SyncCoinsFromWallet();
        UpdateUI(updateSouls: false, updateCoins: true);
    }

    public int GetLastRunCoinsAndClear(bool clear = false)
    {
        int last = PlayerPrefs.GetInt(LAST_RUN_COINS_KEY, 0);
        if (clear)
        {
            PlayerPrefs.SetInt(LAST_RUN_COINS_KEY, 0);
            PlayerPrefs.Save();
        }
        return last;
    }

    public void RefreshUI()
    {
        SyncCoinsFromWallet();
        UpdateUI(updateSouls: true, updateCoins: true);
    }

    private void UpdateUI(bool updateSouls, bool updateCoins)
    {
        if (updateSouls && soulsText) soulsText.text = souls.ToString();
        if (updateCoins && coinsText) coinsText.text = coins.ToString();
    }

    // ---------- VICTORY API ----------
    public void BeginVictorySequence()
    {
        victoryLock = true;
        snapshotSouls = souls;
        snapshotCoins = coins; // coins-витрина уже синхронизирована
    }

    public void EndVictorySequence()
    {
        victoryLock = false;
    }

    public void GetVictorySnapshot(out int outSouls, out int outCoins)
    {
        if (victoryLock)
        {
            outSouls = snapshotSouls;
            outCoins = snapshotCoins;
        }
        else
        {
            outSouls = souls;
            outCoins = coins;
        }
    }

    public int Souls => souls;
    public int Coins => coins;

    // ---------- ANIMATION HELPERS ----------
    private void AnimateSouls(int from, int to)
    {
        if (!soulsText) return;

        if (soulsAnimRoutine != null)
            StopCoroutine(soulsAnimRoutine);

        soulsAnimRoutine = StartCoroutine(AnimateIntRoutine(from, to, soulsText));
        AnimateTextPunch(soulsText, ref soulsScaleRoutine, soulsBaseScale);
    }

    private void AnimateCoins(int from, int to)
    {
        if (!coinsText) return;

        if (coinsAnimRoutine != null)
            StopCoroutine(coinsAnimRoutine);

        coinsAnimRoutine = StartCoroutine(AnimateIntRoutine(from, to, coinsText));
        AnimateTextPunch(coinsText, ref coinsScaleRoutine, coinsBaseScale);
    }

    private IEnumerator AnimateIntRoutine(int from, int to, TMP_Text targetText)
    {
        int current = from;
        targetText.text = current.ToString();

        if (from == to)
            yield break;

        int step = (to > from) ? 1 : -1;
        const float stepDelay = 0.03f;

        while (current != to)
        {
            current += step;
            targetText.text = current.ToString();
            yield return new WaitForSeconds(stepDelay);
        }
    }

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

        while (t < halfTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / halfTime);
            float s = Mathf.Lerp(1f, counterPunchScale, k);
            rect.localScale = baseScale * s;
            yield return null;
        }

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

    private void PlaySoulSound()
    {
        if (audioSource && soulSfx)
            audioSource.PlayOneShot(soulSfx, 1f);
    }

    private void PlayCoinSound()
    {
        if (audioSource && coinSfx)
            audioSource.PlayOneShot(coinSfx, 1f);
    }
}
