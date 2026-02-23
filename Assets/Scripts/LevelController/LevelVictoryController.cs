using UnityEngine;
using System.Collections;

/// <summary>
/// Tracks stage clear state when all enemies are dead,
/// then notifies RunLevelManager after the configured delay.
/// </summary>
public class LevelVictoryController : MonoBehaviour
{
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

    public void ResetForNewStage()
    {
        shown = false;
        StopAllCoroutines();
    }

    private void HandleEnemyDied(EnemyHealth _)
    {
        if (shown) return;

        // Check that all enemies are dead.
        var enemies = FindObjectsOfType<EnemyHealth>();
        foreach (var e in enemies)
        {
            if (!e.IsDead)
                return;
        }

        shown = true;
        StopAllCoroutines();
        StartCoroutine(CompleteStageAfterDelay());
    }

    private IEnumerator CompleteStageAfterDelay()
    {
        float fadeInDuration = fadeInSpeed > 0f ? (1f / fadeInSpeed) : 0f;
        float fadeOutDuration = fadeOutSpeed > 0f ? (1f / fadeOutSpeed) : 0f;
        float totalDelay = Mathf.Max(0f, fadeInDuration) + Mathf.Max(0f, visibleDuration) + Mathf.Max(0f, fadeOutDuration) + Mathf.Max(0f, loadDelay);

        if (totalDelay > 0f)
            yield return new WaitForSeconds(totalDelay);

        if (RunLevelManager.Instance != null)
        {
            RunLevelManager.Instance.OnStageCleared();
        }
        else
        {
            Debug.LogWarning("[LevelVictoryController] RunLevelManager.Instance is null. Cannot continue stage flow.");
        }
    }
}
