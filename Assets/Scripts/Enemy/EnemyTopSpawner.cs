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

    public float spawnInterval = 0f;
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

    private void Awake()
    {
        if (container == null) container = transform;
    }

    private IEnumerator Start()
    {
        // --- стартовый спавн ---
        if (spawnMode == SpawnMode.ExactSequence)
        {
            // Спавним ровно по списку Enemy Prefabs
            if (enemyPrefabs != null)
            {
                int len = enemyPrefabs.Length;
                for (int i = 0; i < len; i++)
                {
                    SpawnOne();
                }
            }
        }
        else
        {
            // Старое поведение: initialCount штук
            for (int i = 0; i < initialCount; i++)
            {
                SpawnOne();
            }
        }

        // --- периодический спавн (если нужен) ---
        if (spawnInterval > 0f)
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                SpawnOne();
            }
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
                        // Спавним врагов строго по очереди из массива и один раз
                        if (_sequenceIndex >= enemyPrefabs.Length)
                        {
                            // Список закончился – больше никого не спавним
                            return null;
                        }

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

        // запасной вариант – одиночное поле
        return enemyPrefab;
    }

    public GameObject SpawnOne()
    {
        GameObject prefabToSpawn = GetNextEnemyPrefab();
        if (prefabToSpawn == null) return null;

        var go = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity, container);
        var walker = go.GetComponent<EnemyWalker>();

        // пробрасываем игрока
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

            // десинхрон логики
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
            x = 0f;
            y = 0f;
        }

        go.transform.position = new Vector3(x, y, 0f);
        return go;
    }
}
