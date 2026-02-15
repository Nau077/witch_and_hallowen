using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerLightningBolt : PlayerProjectileDamageBase
{
    [Header("Lightning")]
    [Range(1, 3)] public int currentLightningSkillLevel = 1;

    [Tooltip("Угол между боковыми лучами.")]
    [Min(1f)] public float angleStep = 22f;

    [Tooltip("Множитель урона боковых лучей относительно центрального.")]
    [Range(0f, 2f)] public float sideDamageMultiplier = 0.7f;

    [Tooltip("Множитель скорости боковых лучей.")]
    [Min(0.1f)] public float sideSpeedMultiplier = 0.95f;

    [Tooltip("Множитель дальности боковых лучей.")]
    [Min(0.1f)] public float sideDistanceMultiplier = 0.9f;

    [Tooltip("Доп. прирост дальности для каждого следующего шага веера.")]
    [Min(0f)] public float sideDistanceBoostPerStep = 0.12f;

    [Tooltip("Ограничение максимального множителя дальности боковых молний.")]
    [Min(0.1f)] public float maxSideDistanceMultiplier = 1.8f;

    [Tooltip("Умножает число боковых молний (2 = в 2 раза больше).")]
    [Min(1)] public int sideBoltCountMultiplier = 2;

    [Tooltip("Падение урона для каждого следующего шага веера.")]
    [Range(0f, 1f)] public float sideDamageFalloffPerStep = 0.12f;

    [Tooltip("Минимальный множитель урона для дальних боковых молний.")]
    [Range(0.01f, 1f)] public float minSideDamageMultiplier = 0.15f;

    [Tooltip("Поворачивать спрайт по направлению полета.")]
    public bool alignSpriteToDirection = true;

    [Tooltip("Смещение угла (если базовый спрайт смотрит не вверх).")]
    public float spriteAngleOffset = 0f;

    [Tooltip("Доп. наклон боковых молний относительно угла веера.")]
    public float sideBoltExtraTilt = 6f;

    [SerializeField] private bool spawnedAsSideBolt;
    [SerializeField] private float spawnedTiltOffset;

    private bool _spawnedSideBolts;

    public void SetSkillLevel(int level)
    {
        currentLightningSkillLevel = Mathf.Clamp(level, 1, 3);
    }

    public override void Init(Vector2 dir, float distance, float speedOverride = -1f, float ignoreFirstMeters = 0f)
    {
        base.Init(dir, distance, speedOverride, ignoreFirstMeters);
        ApplyRotationForDirection(_dir, spawnedTiltOffset);

        if (spawnedAsSideBolt || _spawnedSideBolts)
            return;

        _spawnedSideBolts = true;
        SpawnSideBolts(distance, speedOverride, ignoreFirstMeters);
    }

    private void SpawnSideBolts(float distance, float speedOverride, float ignoreFirstMeters)
    {
        int sidePerDirection = GetSideBoltsPerDirection();
        if (sidePerDirection <= 0)
            return;

        for (int step = 1; step <= sidePerDirection; step++)
        {
            SpawnOneSideBolt(step, step * angleStep, distance, speedOverride, ignoreFirstMeters);
            SpawnOneSideBolt(step, -step * angleStep, distance, speedOverride, ignoreFirstMeters);
        }
    }

    private int GetSideBoltsPerDirection()
    {
        int baseCount;
        switch (Mathf.Clamp(currentLightningSkillLevel, 1, 3))
        {
            case 1: baseCount = 1; break;
            case 2: baseCount = 2; break;
            default: baseCount = 3; break;
        }
        return baseCount * Mathf.Max(1, sideBoltCountMultiplier);
    }

    private void SpawnOneSideBolt(int stepIndex, float angleDeg, float distance, float speedOverride, float ignoreFirstMeters)
    {
        GameObject go = Instantiate(gameObject, transform.position, transform.rotation);

        float step = Mathf.Max(1, stepIndex);
        float damageMul = Mathf.Max(
            minSideDamageMultiplier,
            sideDamageMultiplier * (1f - sideDamageFalloffPerStep * (step - 1f))
        );
        float distanceMul = Mathf.Min(
            maxSideDistanceMultiplier,
            sideDistanceMultiplier + sideDistanceBoostPerStep * (step - 1f)
        );

        var bolt = go.GetComponent<PlayerLightningBolt>();
        if (bolt != null)
        {
            bolt.spawnedAsSideBolt = true;
            bolt.spawnedTiltOffset = Mathf.Sign(angleDeg) * sideBoltExtraTilt;
            bolt.currentLightningSkillLevel = currentLightningSkillLevel;
            bolt._spawnedSideBolts = true;
            bolt.damage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMul));
        }

        Vector2 sideDir = Quaternion.Euler(0f, 0f, angleDeg) * _dir;

        var proj = go.GetComponent<IProjectile>();
        if (proj != null)
        {
            float sideDistance = Mathf.Max(0.1f, distance * distanceMul);
            float sideSpeedOverride = speedOverride > 0f ? speedOverride * sideSpeedMultiplier : speed * sideSpeedMultiplier;
            proj.Init(sideDir, sideDistance, sideSpeedOverride, ignoreFirstMeters);
        }
    }

    private void ApplyRotationForDirection(Vector2 dir, float extraTilt)
    {
        if (!alignSpriteToDirection) return;
        if (dir.sqrMagnitude <= 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f + spriteAngleOffset + extraTilt);
    }
}
