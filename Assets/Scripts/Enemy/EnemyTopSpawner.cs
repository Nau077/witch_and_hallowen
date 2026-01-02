using UnityEngine;
using System.Collections;

public class EnemyTopSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer enemyZone;

    [Tooltip("Старое поле для совместимости. Можно не использовать, если заполнил Enemy Prefabs.")]
    public GameObject enemyPrefab;

    [Header("Enemy Prefabs (new)")]
    [Tooltip("Список разных типов врагов, которых можно спавнить.")]
    public GameObject[] enemyPrefabs;

    public enum SpawnMode
    {
        Random,         // случайный выбор из списка
        RoundRobin,     // по кругу: 1-й, 2-й, 3-й, снова 1-й...
        ExactSequence   // ТОЧНО как в списке, по одному разу каждый
    }

    [Tooltip("Как выбирать тип врага при спавне.")]
    public SpawnMode spawnMode = SpawnMode.Random;

    [Header("Spawn Setup")]
    [Tooltip("Используется только для режимов Random / RoundRobin. В ExactSequence игнорируется.")]
    public int initialCount = 3;

    [Tooltip("Если > 0 — будет спавнить новых врагов раз в N секунд.")]
    public float spawnInterval = 0f;

    [Tooltip("Куда складывать заспавненных врагов. Если не задано — будет использован transform спавнера.")]
    public Transform container;

    [Header("Placement Tweaks")]
    public float xInset = 0.05f;
    public int cellsBelowTop = 2;  // на сколько клеток ниже верхней границы
    public float cellSize = 1f;
    public float spawnYOffset = 0f; // тонкая подстройка

    [Header("Desync / Jitter")]
    [Range(0f, 0.6f)] public float decideJitter = 0.35f;   // ±к decideEvery
    [Range(0f, 0.6f)] public float attackJitter = 0.35f;   // ±к attackInterval
    public Vector2 firstDecisionDelayRange = new Vector2(0.0f, 0.8f);
    public Vector2 firstAttackDelayRange = new Vector2(0.2f, 1.0f);
    [Range(0f, 0.2f)] public float moveSpeedJitter = 0.08f;

    // для RoundRobin
    private int _nextIndex = 0;
    // для ExactSequence
    private int _sequenceIndex = 0;

    private Coroutine _spawnLoop;

    private void Awake()
    {
        // ✅ безопасный дефолт
        if (container == null) container = transform;
    }

    private void OnEnable()
    {
        // ✅ при реактивации тоже
        if (container == null) container = transform;

        // ✅ сброс индексов, чтобы RoundRobin/Sequence не ломались после смерти
        _nextIndex = 0;
        _sequenceIndex = 0;

        // ✅ чистим старых врагов этого спавнера (иначе мусор после смерти)
        ResetState();

        // ✅ стартовый спавн каждый раз при включении
        SpawnInitial();

        // ✅ периодический спавн
        if (spawnInterval > 0f)
        {
            _spawnLoop = StartCoroutine(SpawnLoop());
        }
    }

    private void OnDisable()
    {
        if (_spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }
    }

    /// <summary>
    /// Удаляем всех заспавненных детей из container.
    /// </summary>
    public void ResetState()
    {
        if (container == null) container = transform;

        // ВАЖНО: backward loop
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private void SpawnInitial()
    {
        if (spawnMode == SpawnMode.ExactSequence)
        {
            if (enemyPrefabs != null)
            {
                int len = enemyPrefabs.Length;
                for (int i = 0; i < len; i++)
                    SpawnOne();
            }
        }
        else
        {
            for (int i = 0; i < initialCount; i++)
                SpawnOne();
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnOne();
        }
    }

    /// <summary>
    /// Выбрать подходящий префаб врага в зависимости от режима.
    /// Если список пустой – используем старое поле enemyPrefab.
    /// В ExactSequence, когда список кончился, возвращаем null.
    /// </summary>
    private GameObject GetNextEnemyPrefab()
    {
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            switch (spawnMode)
            {
                case SpawnMode.ExactSequence:
                    {
                        if (_sequenceIndex >= enemyPrefabs.Length)
                            return null;

                        var prefab = enemyPrefabs[_sequenceIndex];
                        _sequenceIndex++;
                        return prefab;
                    }

                case SpawnMode.Random:
                    {
                        int len = enemyPrefabs.Length;
                        if (len == 0) break;
                        return enemyPrefabs[Random.Range(0, len)];
                    }

                case SpawnMode.RoundRobin:
                    {
                        int len = enemyPrefabs.Length;
                        if (len == 0) break;

                        if (_nextIndex >= len)
                            _nextIndex = 0;

                        var prefab = enemyPrefabs[_nextIndex];
                        _nextIndex++;
                        return prefab;
                    }
            }
        }

        return enemyPrefab;
    }

    public GameObject SpawnOne()
    {
        if (container == null) container = transform;

        GameObject prefabToSpawn = GetNextEnemyPrefab();
        if (prefabToSpawn == null) return null;

        var go = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity, container);
        var walker = go.GetComponent<EnemyWalker>();

        // ✅ пробрасываем игрока (важно для атаки)
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (walker && playerGO) walker.player = playerGO.transform;

        float x, y;

        if (walker != null)
        {
            // Позиция по X – внутри его собственных границ
            x = Random.Range(walker.leftLimit + xInset, walker.rightLimit - xInset);

            // на N клеток ниже topLimit, но не ниже bottomLimit
            y = walker.topLimit - cellsBelowTop * cellSize + spawnYOffset;
            y = Mathf.Clamp(y, walker.bottomLimit + 0.001f, walker.topLimit - 0.001f);

            // ✅ десинхрон логики (это влияет на то, когда он начнет “думать” и атаковать)
            float firstDecisionDelay = Random.Range(firstDecisionDelayRange.x, firstDecisionDelayRange.y);
            float firstAttackDelay = Random.Range(firstAttackDelayRange.x, firstAttackDelayRange.y);

            walker.ApplyDesync(
                decideJitter: decideJitter,
                attackJitter: attackJitter,
                firstDecisionDelay: firstDecisionDelay,
                firstAttackDelay: firstAttackDelay
            );

            // лёгкий джиттер скорости
            float k = 1f + Random.Range(-moveSpeedJitter, moveSpeedJitter);
            walker.moveSpeed *= Mathf.Max(0.05f, k);
        }
        else
        {
            // если EnemyWalker нет — хотя бы не в нуле
            x = transform.position.x;
            y = transform.position.y;
        }

        go.transform.position = new Vector3(x, y, 0f);
        return go;
    }
}
