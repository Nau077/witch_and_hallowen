using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Header("Runtime Coins (used by shop when currency = Coins)")]
    [Min(0)] public int coins = 0;

    [Header("UI")]
    [Tooltip("Текст для отображения coins (можно указать GoldText).")]
    [SerializeField] private TMP_Text coinsText;

    public enum DebugApplyMode { None, Overwrite, Add }

    [Header("DEBUG (Editor/Play)")]
    public DebugApplyMode debugMode = DebugApplyMode.None;

    [Tooltip("Значение/прибавка для coins.")]
    public int debugCoinsValue = 100;

    [Tooltip("Значение/прибавка для souls (SoulCounter.killsLifetime).")]
    public int debugSoulsValue = 200;

    private const string KILLS_KEY = "kills_lifetime";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RefreshUI();
    }

    private void Start()
    {
#if UNITY_EDITOR
        ApplyDebugValuesIfNeeded();
#endif
        // Важно: после любых стартовых апдейтов — ещё раз зафиксировать UI
        RefreshUI();
    }

#if UNITY_EDITOR
    private void ApplyDebugValuesIfNeeded()
    {
        if (debugMode == DebugApplyMode.None) return;

        // --- COINS ---
        if (debugMode == DebugApplyMode.Overwrite)
            coins = Mathf.Max(0, debugCoinsValue);
        else if (debugMode == DebugApplyMode.Add)
            coins = Mathf.Max(0, coins + debugCoinsValue);

        // --- SOULS ---
        var sc = SoulCounter.Instance;
        if (sc != null)
        {
            if (debugMode == DebugApplyMode.Overwrite)
                sc.killsLifetime = Mathf.Max(0, debugSoulsValue);
            else if (debugMode == DebugApplyMode.Add)
                sc.killsLifetime = Mathf.Max(0, sc.killsLifetime + debugSoulsValue);

            PlayerPrefs.SetInt(KILLS_KEY, sc.killsLifetime);
            PlayerPrefs.Save();
            sc.RefreshUI();
        }
        else
        {
            Debug.LogWarning("[PlayerWallet] Debug souls: SoulCounter.Instance is null (souls not applied).");
        }

        Debug.Log($"[PlayerWallet] Debug applied. Mode={debugMode}, coins={coins}");
    }
#endif

    // ---------- COINS API ----------
    public bool CanSpend(int c) => coins >= c;

    public bool TrySpend(int c)
    {
        if (coins < c) return false;
        coins -= c;
        RefreshUI();
        return true;
    }

    public void Add(int c)
    {
        coins += c;
        RefreshUI();
    }

    public void SetCoins(int value)
    {
        coins = Mathf.Max(0, value);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (coinsText)
            coinsText.text = coins.ToString();
    }
}
