using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelVictoryController : MonoBehaviour
{
    public enum NextMode { ByName, ByBuildIndex }

    [Header("UI")]
    public TextMeshProUGUI victoryText;

    [Header("Timings")]
    public float fadeInSpeed = 0.8f;
    public float visibleDuration = 1.5f;
    public float fadeOutSpeed = 0.8f;
    public float loadDelay = 0.2f;

    [Header("Next Level")]
    public NextMode nextMode = NextMode.ByBuildIndex; // <-- рекомендованный режим
    public string nextSceneName = "Level_2";           // используется только в режиме ByName
    public string lastSceneFallback = "MainMenu";      // куда идти, если текущая сцена последняя

    private bool shown = false;

    private void OnEnable() { EnemyHealth.OnAnyEnemyDied += HandleEnemyDied; }
    private void OnDisable() { EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied; }

    private void Start()
    {
        if (victoryText)
        {
            victoryText.gameObject.SetActive(false);
            var c = victoryText.color; c.a = 0f; victoryText.color = c;
            victoryText.text = "VICTORY SOULS CAPTURED";
        }
    }

    private void HandleEnemyDied(EnemyHealth _)
    {
        if (shown) return;

        var enemies = FindObjectsOfType<EnemyHealth>();
        foreach (var e in enemies) if (!e.IsDead) return;

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
        var c = victoryText.color; c.a = 0f; victoryText.color = c;

        // Fade-in
        while (c.a < 1f) { c.a += Time.deltaTime * fadeInSpeed; victoryText.color = c; yield return null; }

        // Hold
        yield return new WaitForSeconds(visibleDuration);

        // Fade-out
        while (c.a > 0f) { c.a -= Time.deltaTime * fadeOutSpeed; victoryText.color = c; yield return null; }

        yield return new WaitForSeconds(loadDelay);
        LoadNext();
    }

    private void LoadNext()
    {
        if (nextMode == NextMode.ByBuildIndex)
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            int next = current + 1;
            int total = SceneManager.sceneCountInBuildSettings;

            if (next < total)
            {
                SceneManager.LoadScene(next);
            }
            else if (!string.IsNullOrEmpty(lastSceneFallback))
            {
                SceneManager.LoadScene(lastSceneFallback);
            }
            else
            {
                Debug.LogWarning("[LevelVictoryController] No next scene and no fallback set.");
            }
        }
        else // ByName
        {
            if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
            else Debug.LogWarning("[LevelVictoryController] nextSceneName is empty.");
        }
    }
}
