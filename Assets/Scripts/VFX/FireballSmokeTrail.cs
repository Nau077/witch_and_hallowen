using UnityEngine;

[DisallowMultipleComponent]
public class FireballSmokeTrail : ProjectileParticleTrailBase
{
    [Header("Fire smoke look")]
    public float startSizeMin = 0.10f;
    public float startSizeMax = 0.18f;

    // 🔥 более красный/огненный
    public Color startColor = new Color(1.00f, 0.18f, 0.05f, 0.45f);
    public Color endColor = new Color(0.35f, 0.05f, 0.02f, 0f);

    protected override void Reset()
    {
        sortingLayerName = "FX";
        orderInLayer = 11;

        // вниз (чуть сильнее)
        localOffset = new Vector2(0f, -0.12f);

        worldSpace = true;
        emissionRate = 55f;
        lifetime = 0.35f;
        startSpeed = 0.06f;
        spawnRadius = 0.04f;

        if (particleSprite == null)
            particleSprite = ProceduralVFXSprites.GetFireSmokeSprite16();

        base.Reset();
        ApplyLineTrailPreset();
    }

    protected override void OnValidate()
    {
        if (particleSprite == null)
            particleSprite = ProceduralVFXSprites.GetFireSmokeSprite16();

        base.OnValidate();
        ApplyLineTrailPreset();
    }

    protected override void ApplyTypeSpecific()
    {
        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
        main.startSpeed = Mathf.Max(0.03f, startSpeed * 0.85f);

        var col = ps.colorOverLifetime;
        col.enabled = true;

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
            new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.85f),
            new Keyframe(0.35f, 1.0f),
            new Keyframe(1f, 0f)
        ));
    }

    private void ApplyLineTrailPreset()
    {
        var line = GetComponent<DefaultLineTrail>();
        if (line == null) return;

        line.sortingLayerName = "FX";
        line.orderInLayer = 10;

        // 🔥 огненный хвост (красно-оранжевый)
        line.startColor = new Color(1.00f, 0.20f, 0.06f, 0.70f);
        line.endColor = new Color(1.00f, 0.20f, 0.06f, 0f);

        line.startWidth = 0.20f;
        line.endWidth = 0.00f;
        line.time = 0.16f;

        // вниз как у частиц
        line.localOffset = new Vector2(0f, -0.12f);

        line.ApplyOrCreate();
    }
}
