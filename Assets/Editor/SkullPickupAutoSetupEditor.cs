using System.Collections;
using UnityEngine;

/// <summary>
/// Управляет событием "череп": спавнит 1 раз на стадию (кроме 0),
/// через рандомную задержку, на случайной позиции внутри зоны.
/// </summary>
public class SkullEventController : MonoBehaviour
{
    [Header("Prefab + Area")]
    [Tooltip("Префаб черепа (SkullPickup_TMP).")]
    public GameObject skullPrefab;

    [Tooltip("Зона, внутри которой спавним череп. Обычно это GameObject с BoxCollider2D (IsTrigger=true).")]
    public BoxCollider2D groundArea;

    [Header("Timing")]
    [Tooltip("Минимальная задержка перед спавном (сек).")]
    public float minDelay = 10f;

    [Tooltip("Максимальная задержка перед спавном (сек).")]
    public float maxDelay = 25f;

    [Tooltip("Сколько секунд череп живёт, если его не собрали.")]
    public float skullLifetime = 10f;

    [Header("Rules")]
    [Tooltip("Не спавнить на стадии 0 (база).")]
    public bool disableOnStageZero = true;

    [Tooltip("Спавнить максимум 1 раз на стадию.")]
    public bool oncePerStage = true;

    [Header("Visual Safety (optional)")]
    [Tooltip("Если >0 — принудительно выставим sortingOrder всем SpriteRenderer внутри черепа.")]
    public int forceSpriteSortingOrder = 200;

    [Tooltip("Если true — после спавна поставим scale корня = (1,1,1). Полезно, если в сцене кто-то меняет scale.")]
    public bool forceRootScaleToOne = false;

    // runtime
    private int _currentStage = 0;
    private bool _spawnedThisStage = false;
    private Coroutine _spawnRoutine;
    private GameObject _aliveSkull;

    /// <summary>
    /// Вызывать из RunLevelManager при смене этапа.
    /// stageIndex=0 — база (спавн выключен).
    /// </summary>
    public void SetStage(int stageIndex)
    {
        _currentStage = stageIndex;

        // сброс флага "уже спавнили"
        _spawnedThisStage = false;

        // убиваем текущий череп при смене стадии (опционально, но обычно правильно)
        if (_aliveSkull != null)
        {
            Destroy(_aliveSkull);
            _aliveSkull = null;
        }

        // остановить предыдущую корутину
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        // stage 0 = база -> не спавнить
        if (disableOnStageZero && _currentStage <= 0)
            return;

        // стартуем ожидание спавна на этой стадии
        _spawnRoutine = StartCoroutine(SpawnOnceRoutine());
    }

    private void OnDisable()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    private IEnumerator SpawnOnceRoutine()
    {
        // защита от некорректных ссылок
        if (!skullPrefab)
        {
            Debug.LogWarning("[SkullEventController] skullPrefab is NULL.");
            yield break;
        }
        if (!groundArea)
        {
            Debug.LogWarning("[SkullEventController] groundArea (BoxCollider2D) is NULL.");
            yield break;
        }

        // не спавним если по правилам нельзя
        if (disableOnStageZero && _currentStage <= 0)
            yield break;

        // уже спавнили на этой стадии
        if (oncePerStage && _spawnedThisStage)
            yield break;

        // ждём рандомное время
        float delay = Random.Range(minDelay, maxDelay);
        // realtime — чтобы не зависеть от timeScale (у тебя бывают паузы/попапы)
        yield return new WaitForSecondsRealtime(delay);

        // ещё раз перепроверим, вдруг стадия стала 0 или объект выключили
        if (!isActiveAndEnabled)
            yield break;
        if (disableOnStageZero && _currentStage <= 0)
            yield break;
        if (oncePerStage && _spawnedThisStage)
            yield break;

        // ставим флаг
        _spawnedThisStage = true;

        // позиция спавна внутри bounds
        Vector3 pos = GetRandomPointInBounds(groundArea.bounds);
        pos.z = 0f;

        // создаём
        _aliveSkull = Instantiate(skullPrefab, pos, Quaternion.identity);

        // визуальные "страховки"
        if (forceRootScaleToOne && _aliveSkull != null)
            _aliveSkull.transform.localScale = Vector3.one;

        if (forceSpriteSortingOrder > 0 && _aliveSkull != null)
        {
            var renderers = _aliveSkull.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in renderers)
                r.sortingOrder = forceSpriteSortingOrder;
        }

        // авто-деспавн
        if (skullLifetime > 0 && _aliveSkull != null)
        {
            Destroy(_aliveSkull, skullLifetime);
        }

        _spawnRoutine = null;
    }

    private static Vector3 GetRandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            0f
        );
    }
}
