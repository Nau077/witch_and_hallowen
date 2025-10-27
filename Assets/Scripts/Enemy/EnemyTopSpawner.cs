using UnityEngine;
using System.Collections;

public class EnemyTopSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer enemyZone;
    public GameObject enemyPrefab;

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
    [Range(0f, 0.6f)] public float decideJitter = 0.35f;   // ±35% к decideEvery на инстанс
    [Range(0f, 0.6f)] public float attackJitter = 0.35f;   // ±35% к attackInterval на инстанс
    public Vector2 firstDecisionDelayRange = new Vector2(0.0f, 0.8f); // стартовая задержка AI цикла
    public Vector2 firstAttackDelayRange = new Vector2(0.2f, 1.0f);   // стартовая задержка до первой атаки
    [Range(0f, 0.2f)] public float moveSpeedJitter = 0.08f;           // лёгкая рассинхронизация скорости

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

    public GameObject SpawnOne()
    {
        if (enemyPrefab == null) return null;

        var go = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity, container);
        var walker = go.GetComponent<EnemyWalker>();

        // --- НИЧЕГО НЕ МЕНЯЕМ В ГРАНИЦАХ ВРАГА! ---
        // НЕ трогаем walker.leftLimit/rightLimit/topLimit/bottomLimit

        // пробрасываем игрока
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (walker && playerGO) walker.player = playerGO.transform;

        // Позиция спавна: по верхней кромке границ ВРАГА (его собственных)
        float x, y;
        if (walker != null)
        {
            x = Random.Range(walker.leftLimit + xInset, walker.rightLimit - xInset);

            // на N клеток ниже topLimit, но не ниже bottomLimit
            y = walker.topLimit - cellsBelowTop * cellSize + spawnYOffset;
            y = Mathf.Clamp(y, walker.bottomLimit + 0.001f, walker.topLimit - 0.001f);

            // лёгкий десинхрон
            walker.ApplyDesync(decideJitter: 0.35f, attackJitter: 0.35f,
                               firstDecisionDelay: Random.Range(0f, 0.8f),
                               firstAttackDelay: Random.Range(0.2f, 1.0f));
            // при желании — лёгкий джиттер скорости:
            float k = 1f + Random.Range(-moveSpeedJitter, moveSpeedJitter);
            walker.moveSpeed *= Mathf.Max(0.05f, k);
        }
        else
        {
            // На случай, если вдруг префаб без EnemyWalker
            x = 0f;
            y = 0f;
        }

        go.transform.position = new Vector3(x, y, 0f);
        return go;
    }
}
