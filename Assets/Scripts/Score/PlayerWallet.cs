using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Header("Run Coins (ONLY during run)")]
    [Min(0)] public int coins = 0;

    [Header("UI")]
    [Tooltip("Текст для отображения coins.")]
    [SerializeField] private TMP_Text coinsText;

    public enum DebugApplyMode { None, Overwrite, Add }

    [Header("DEBUG (Editor/Play)")]
    public DebugApplyMode debugMode = DebugApplyMode.None;

    [Tooltip("Значение/прибавка для coins.")]
    public int debugCoinsValue = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Если PlayerWallet есть в нескольких сценах — лучше оставить один:
        // DontDestroyOnLoad(gameObject);

        RefreshUI();
    }

    private void Start()
    {
#if UNITY_EDITOR
        ApplyDebugValuesIfNeeded();
#endif
        RefreshUI();
    }

#if UNITY_EDITOR
    private void ApplyDebugValuesIfNeeded()
    {
        if (debugMode == DebugApplyMode.None) return;

        if (debugMode == DebugApplyMode.Overwrite)
            coins = Mathf.Max(0, debugCoinsValue);
        else if (debugMode == DebugApplyMode.Add)
            coins = Mathf.Max(0, coins + debugCoinsValue);

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
        coins = Mathf.Max(0, coins + c);
        RefreshUI();
    }

    public void SetCoins(int value)
    {
        coins = Mathf.Max(0, value);
        RefreshUI();
    }

    /// <summary>
    /// ✅ Сброс coins (рановая валюта). Должна вызываться при смерти/рестарте рана.
    /// </summary>
    public void ResetRunCoins()
    {
        coins = 0;
        RefreshUI();
        Debug.Log("[PlayerWallet] ResetRunCoins → coins=0");
    }

    public void RefreshUI()
    {
        if (coinsText)
            coinsText.text = coins.ToString();
    }
}
