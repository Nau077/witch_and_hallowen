using UnityEngine;
using TMPro;
using System.Collections;

public class LevelVictoryController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI victoryText;      // �������� ���� Text (TMP) �� Canvas

    [Header("Appearance")]
    [Tooltip("�������� ��������� �������")]
    public float fadeInSpeed = 0.8f;

    private bool shown = false;

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        if (victoryText)
        {
            victoryText.gameObject.SetActive(false);
            var c = victoryText.color;
            c.a = 0f;
            victoryText.color = c;
            // ����� �� �������:
            victoryText.text = "SOULS CAPTURED";
        }
    }

    private void HandleEnemyDied(EnemyHealth _)
    {
        // ������ ���, ����� ���-�� �������, ��������� �������� �� �����
        if (shown) return; // ��� ��������

        var enemies = FindObjectsOfType<EnemyHealth>(); // �� ��� 2D �����, ������ � �������
        foreach (var e in enemies)
        {
            if (!e.IsDead)
                return; // ���� ����� � �������
        }

        // ��� ������ � ���������� �������
        ShowVictoryText();
    }

    private void ShowVictoryText()
    {
        if (!victoryText) return;
        shown = true;
        victoryText.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        var c = victoryText.color;
        c.a = 0f;
        victoryText.color = c;

        while (c.a < 1f)
        {
            c.a += Time.deltaTime * fadeInSpeed;
            victoryText.color = c;
            yield return null;
        }
        c.a = 1f;
        victoryText.color = c;
    }
}
