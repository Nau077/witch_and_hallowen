using UnityEngine;
using TMPro; // <-- используем TMP

[DefaultExecutionOrder(-100)]
public class SoulCounter : MonoBehaviour
{
    public static SoulCounter Instance { get; private set; }

    [Min(0)] public int currentSouls = 0;

    [Header("UI (TMP)")]
    [SerializeField] private TMP_Text soulText; // <-- TMP_Text

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Если поле не назначено в инспекторе — попробуем найти среди детей
        if (soulText == null)
            soulText = GetComponentInChildren<TMP_Text>(true);

        // Если не нужна "вечность" между сценами — эту строку можно убрать
        // DontDestroyOnLoad(gameObject);

        UpdateUI();
    }

    public void AddSouls(int amount)
    {
        if (amount <= 0) return;
        currentSouls += amount;
        UpdateUI();
        // тут можно дергать анимацию/звук
    }

    private void UpdateUI()
    {
        if (soulText != null)
            soulText.text = currentSouls.ToString();
    }
}
