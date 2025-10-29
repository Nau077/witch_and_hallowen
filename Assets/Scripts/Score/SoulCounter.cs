using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-100)]
public class SoulCounter : MonoBehaviour
{
    public static SoulCounter Instance { get; private set; }

    [Header("Souls")]
    [Min(0)] public int currentSouls = 0;

    [Header("UI (TMP)")]
    [SerializeField] private TMP_Text soulText;

    private void Awake()
    {
        // --- Singleton, ����� �� ������������ ����� ������� ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // ��������� ��� ����� ����

        // --- UI-����� ---
        if (soulText == null)
            soulText = GetComponentInChildren<TMP_Text>(true);

        // --- ��������� ���������� ��� (���� ������ ��������� ����� ��������) ---
        currentSouls = PlayerPrefs.GetInt("souls", 0);

        UpdateUI();
    }

    public void AddSouls(int amount)
    {
        if (amount <= 0) return;
        currentSouls += amount;

        // ��������� �������� ����� ������� � ���� ��� �����������
        PlayerPrefs.SetInt("souls", currentSouls);
        PlayerPrefs.Save();

        UpdateUI();
    }

    public void ResetSouls()
    {
        currentSouls = 0;
        PlayerPrefs.SetInt("souls", 0);
        PlayerPrefs.Save();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (soulText != null)
            soulText.text = currentSouls.ToString();
    }
}
