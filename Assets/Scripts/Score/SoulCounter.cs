using UnityEngine;
using TMPro; // <-- ���������� TMP

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

        // ���� ���� �� ��������� � ���������� � ��������� ����� ����� �����
        if (soulText == null)
            soulText = GetComponentInChildren<TMP_Text>(true);

        // ���� �� ����� "��������" ����� ������� � ��� ������ ����� ������
        // DontDestroyOnLoad(gameObject);

        UpdateUI();
    }

    public void AddSouls(int amount)
    {
        if (amount <= 0) return;
        currentSouls += amount;
        UpdateUI();
        // ��� ����� ������� ��������/����
    }

    private void UpdateUI()
    {
        if (soulText != null)
            soulText.text = currentSouls.ToString();
    }
}
