using System.Collections;
using UnityEngine;

public class SkullEventController : MonoBehaviour
{
    [Header("Prefab + Area")]
    public GameObject skullPrefab;
    public BoxCollider2D groundArea;

    [Header("Timing")]
    public float minDelay = 10f;
    public float maxDelay = 25f;
    public float skullLifetime = 10f;

    [Tooltip("Минимальный разрыв между появлениями в рамках одной стадии (сек).")]
    public float minGapBetweenSpawns = 8f;

    [Header("Stage rules")]
    public bool disableOnStageZero = true;

    [Header("How many times per stage")]
    public int minSpawnsPerStage = 1;
    public int maxSpawnsPerStage = 3;

    [Header("Spawn Height (fixed)")]
    public SpawnYMode spawnYMode = SpawnYMode.GroundTopPlusOffset;

    [Tooltip("Если spawnYMode = FixedWorldY, то Y берется отсюда (world).")]
    public float fixedWorldY = 0f;

    [Tooltip("Если spawnYMode = GroundTopPlusOffset, то Y = groundArea.bounds.max.y + это значение.")]
    public float yOffsetAboveGroundTop = 0.35f;

    [Tooltip("Если true — X всегда берется строго внутри groundArea по bounds.min.x..max.x.")]
    public bool clampToAreaX = true;

    [Header("Blink (spawn + pre-despawn)")]
    [Tooltip("Мигание сразу после появления (сек).")]
    public float spawnBlinkDuration = 0.6f;

    [Tooltip("За сколько секунд до исчезновения начать мигание.")]
    public float preDespawnBlinkLeadTime = 1.0f;

    [Tooltip("Длительность мигания перед исчезновением (сек).")]
    public float preDespawnBlinkDuration = 1.0f;

    [Tooltip("Как часто переключать альфу при мигании (сек).")]
    public float blinkInterval = 0.10f;

    [Range(0f, 1f)]
    [Tooltip("Нижняя альфа при мигании (0..1).")]
    public float blinkAlphaLow = 0.15f;

    [Header("Visual safety")]
    public int forceSpriteSortingOrder = 200;

    private int _currentStage = 0;
    private int _spawnsLeft = 0;
    private Coroutine _routine;
    private GameObject _aliveSkull;

    public enum SpawnYMode
    {
        FixedWorldY,
        GroundTopPlusOffset
    }

    public void SetStage(int stageIndex)
    {
        _currentStage = stageIndex;

        if (_routine != null) { StopCoroutine(_routine); _routine = null; }

        if (_aliveSkull != null)
        {
            Destroy(_aliveSkull);
            _aliveSkull = null;
        }

        if (disableOnStageZero && _currentStage <= 0)
        {
            _spawnsLeft = 0;
            return;
        }

        int min = Mathf.Max(0, minSpawnsPerStage);
        int max = Mathf.Max(min, maxSpawnsPerStage);
        _spawnsLeft = Random.Range(min, max + 1);

        if (_spawnsLeft <= 0) return;

        _routine = StartCoroutine(StageSpawnLoop());
    }

    private IEnumerator StageSpawnLoop()
    {
        if (!skullPrefab) { Debug.LogWarning("[SkullEventController] skullPrefab is NULL."); yield break; }
        if (!groundArea) { Debug.LogWarning("[SkullEventController] groundArea is NULL."); yield break; }

        while (isActiveAndEnabled)
        {
            if (disableOnStageZero && _currentStage <= 0) yield break;
            if (_spawnsLeft <= 0) yield break;

            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSecondsRealtime(delay);

            while (_aliveSkull != null)
                yield return new WaitForSecondsRealtime(0.2f);

            SpawnOnce();
            _spawnsLeft--;

            yield return new WaitForSecondsRealtime(minGapBetweenSpawns);
        }
    }

    private void SpawnOnce()
    {
        if (!groundArea) return;

        Bounds b = groundArea.bounds;

        // ✅ РАНДОМ ТОЛЬКО ПО X
        float x = Random.Range(b.min.x, b.max.x);

        // ✅ Y ВСЕГДА ФИКСИРОВАННЫЙ (по выбранному режиму)
        float y;
        if (spawnYMode == SpawnYMode.FixedWorldY)
        {
            y = fixedWorldY;
        }
        else // GroundTopPlusOffset
        {
            y = b.max.y + yOffsetAboveGroundTop;
        }

        Vector3 pos = new Vector3(x, y, 0f);

        // на всякий случай: если кто-то подменит bounds — можно зажать X
        if (clampToAreaX)
            pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);

        _aliveSkull = Instantiate(skullPrefab, pos, Quaternion.identity);

        // sorting safety
        if (forceSpriteSortingOrder > 0)
        {
            var rs = _aliveSkull.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in rs)
                if (r) r.sortingOrder = forceSpriteSortingOrder;
        }

        // мигание при появлении
        if (spawnBlinkDuration > 0.01f)
            StartCoroutine(BlinkAlphaForDuration(_aliveSkull, spawnBlinkDuration));

        // lifetime + мигание перед исчезновением
        if (skullLifetime > 0f)
            StartCoroutine(DespawnAfter(_aliveSkull, skullLifetime));
    }

    private IEnumerator DespawnAfter(GameObject go, float lifetime)
    {
        if (!go) yield break;

        float start = Time.unscaledTime;
        float end = start + lifetime;

        float lead = Mathf.Clamp(preDespawnBlinkLeadTime, 0f, lifetime);
        float blinkStartTime = end - lead;

        while (go != null && Time.unscaledTime < blinkStartTime)
            yield return null;

        if (go != null && preDespawnBlinkDuration > 0.01f)
        {
            float blinkEnd = Time.unscaledTime + preDespawnBlinkDuration;
            StartCoroutine(BlinkAlphaForDuration(go, preDespawnBlinkDuration));

            while (go != null && Time.unscaledTime < blinkEnd && Time.unscaledTime < end)
                yield return null;
        }

        while (go != null && Time.unscaledTime < end)
            yield return null;

        if (go != null) Destroy(go);
        if (_aliveSkull == go) _aliveSkull = null;
    }

    private IEnumerator BlinkAlphaForDuration(GameObject go, float duration)
    {
        if (!go) yield break;

        var rs = go.GetComponentsInChildren<SpriteRenderer>(true);
        if (rs == null || rs.Length == 0) yield break;

        float[] baseA = new float[rs.Length];
        for (int i = 0; i < rs.Length; i++)
        {
            if (!rs[i]) continue;
            baseA[i] = rs[i].color.a;
        }

        float dt = Mathf.Max(0.02f, blinkInterval);
        float end = Time.unscaledTime + Mathf.Max(0.01f, duration);

        bool low = true;

        while (go != null && Time.unscaledTime < end)
        {
            float aMul = low ? blinkAlphaLow : 1f;

            for (int k = 0; k < rs.Length; k++)
            {
                if (!rs[k]) continue;
                var c = rs[k].color;
                c.a = baseA[k] * aMul;
                rs[k].color = c;
            }

            low = !low;
            yield return new WaitForSecondsRealtime(dt);
        }

        if (go != null)
        {
            for (int k = 0; k < rs.Length; k++)
            {
                if (!rs[k]) continue;
                var c = rs[k].color;
                c.a = baseA[k];
                rs[k].color = c;
            }
        }
    }
}
