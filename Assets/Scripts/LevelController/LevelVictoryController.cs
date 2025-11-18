using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Отслеживает момент, когда на сцене не остаётся живых врагов,
/// показывает текст победы и после этого даёт сигнал RunLevelManager,
/// что этаж леса очищен.
/// </summary>
public class LevelVictoryController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI victoryText;

    [Header("Timings")]
    public float fadeInSpeed = 0.8f;
    public float visibleDuration = 1.5f;
    public float fadeOutSpeed = 0.8f;
    public float loadDelay = 0.2f;

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
        InitVictoryTextState();
    }

    /// <summary>
    /// Сбрасываем состояние контроллера, когда начинается новый этаж леса.
    /// </summary>
    public void ResetForNewStage()
    {
        shown = false;
        InitVictoryTextState();
    }

    private void InitVictoryTextState()
    {
        if (!victoryText) return;

        victoryText.gameObject.SetActive(false);

        var c = victoryText.color;
        c.a = 0f;
        victoryText.color = c;

        victoryText.text = "VICTORY\nSOULS CAPTURED";
    }

    private void HandleEnemyDied(EnemyHealth _)
    {
        if (shown) return;

        // Проверяем, что ВСЕ враги мертвы
        var enemies = FindObjectsOfType<EnemyHealth>();
        foreach (var e in enemies)
        {
            if (!e.IsDead)
                return;
        }

        ShowVictoryText();
    }

    private void ShowVictoryText()
    {
        if (!victoryText) return;

        shown = true;
        victoryText.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        var c = victoryText.color;
        c.a = 0f;
        victoryText.color = c;

        // Fade-in
        while (c.a < 1f)
        {
            c.a += Time.deltaTime * fadeInSpeed;
            victoryText.color = c;
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(visibleDuration);

        // Fade-out
        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * fadeOutSpeed;
            victoryText.color = c;
            yield return null;
        }

        yield return new WaitForSeconds(loadDelay);

        // Сигнал менеджеру забега
        if (RunLevelManager.Instance != null)
        {
            RunLevelManager.Instance.OnStageCleared();
        }
        else
        {
            Debug.LogWarning("[LevelVictoryController] Нет RunLevelManager.Instance. Не знаю, что делать после победы.");
        }
    }
}
