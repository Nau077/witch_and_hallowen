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
        Random,     // выбирать случайного врага из списка
        RoundRobin  // по кругу: 1-й, 2-й, 3-й, снова 1-й...
    }

    [Tooltip("Как выбирать тип врага при спавне.")]
    public SpawnMode spawnMode = SpawnMode.Random;

    [Header("Spawn Setup")]
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

    // для round-robin
    private int _nextIndex = 0;

    private void Awake()
    {
        if (container == null) container = transform;
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < initialCount; i++)
            SpawnOne();

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
    /// </summary>
    private GameObject GetNextEnemyPrefab()
    {
        // 1) если массив задан и там есть хотя бы один not-null – работаем с ним
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            // считаем количество валидных элементов
            int validCount = 0;
            for (int i = 0; i < enemyPrefabs.Length; i++)
                if (enemyPrefabs[i] != null) validCount++;

            if (validCount == 0)
                goto FALLBACK_SINGLE; // всё null, идём в запасной вариант

            switch (spawnMode)
            {
                case SpawnMode.Random:
                    {
                        // случайный not-null
                        for (int safety = 0; safety < 20; safety++)
                        {
                            int idx = Random.Range(0, enemyPrefabs.Length);
                            if (enemyPrefabs[idx] != null)
                                return enemyPrefabs[idx];
                        }
                        // если по какой-то причине не нашли – просто линейный поиск
                        for (int i = 0; i < enemyPrefabs.Length; i++)
                            if (enemyPrefabs[i] != null)
                                return enemyPrefabs[i];
                        break;
                    }

                case SpawnMode.RoundRobin:
                    {
                        // двигаемся по кругу, пропуская null
                        for (int safety = 0; safety < enemyPrefabs.Length; safety++)
                        {
                            if (_nextIndex >= enemyPrefabs.Length)
                                _nextIndex = 0;

                            var prefab = enemyPrefabs[_nextIndex];
                            _nextIndex++;

                            if (prefab != null)
                                return prefab;
                        }
                        break;
                    }
            }
        }

    // 2) запасной вариант – старое единичное поле
    FALLBACK_SINGLE:
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
