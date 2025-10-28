using UnityEngine;
using TMPro;
using System.Collections;

public class LevelVictoryController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI victoryText;      // перетащи сюда Text (TMP) из Canvas

    [Header("Appearance")]
    [Tooltip("Скорость появления надписи")]
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
            // Текст по заданию:
            victoryText.text = "SOULS CAPTURED";
        }
    }

    private void HandleEnemyDied(EnemyHealth _)
    {
        // Каждый раз, когда кто-то умирает, проверяем остались ли живые
        if (shown) return; // уже показали

        var enemies = FindObjectsOfType<EnemyHealth>(); // ок для 2D сцены, просто и надежно
        foreach (var e in enemies)
        {
            if (!e.IsDead)
                return; // есть живые — выходим
        }

        // Все мертвы — показываем надпись
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
