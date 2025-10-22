using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth;

    [Header("UI")]
    public Image barFill; // �������� ���� UI Image (BarFill) �� Canvas

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateBar();
    }

    /// <summary>
    /// ������� ���� ������. ���������� ����������� ���� (����� ���� 0).
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (amount <= 0) return 0;
        int prev = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateBar();

        // ����� ����� �������� Death() ���� �����
        // if (currentHealth <= 0) { ... }

        return prev - currentHealth;
    }

    /// <summary>
    /// ������/��������� ������� (�� �������).
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (barFill != null)
        {
            float t = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;
            barFill.fillAmount = t;
        }
    }
}
