// PlayerWallet.cs
using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Min(0)] public int coins = 0;
    [SerializeField] TMP_Text coinsText; // опционально Ч UI

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RefreshUI();
    }

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

    void RefreshUI()
    {
        if (coinsText) coinsText.text = coins.ToString();
    }
}
