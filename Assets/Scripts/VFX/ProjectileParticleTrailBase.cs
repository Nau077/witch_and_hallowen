using UnityEngine;

public abstract class ProjectileParticleTrailBase : MonoBehaviour
{
    [Header("Auto")]
    public bool autoSetupInEditMode = true;

    [Header("Spawn point (offset)")]
    public Vector2 localOffset = new Vector2(0f, -0.10f);

    [Header("Optional sprite for particles (Texture Sheet Animation: Sprites)")]
    public Sprite particleSprite;

    [Header("Sorting")]
    public string sortingLayerName = "FX";
    public int orderInLayer = 11;

    [Header("General")]
    public bool worldSpace = true;
    public float emissionRate = 55f;
    public float lifetime = 0.45f;
    public float startSpeed = 0.15f;
    public float spawnRadius = 0.06f;

    [Header("Material Fix")]
    public bool forceGoodParticleMaterial = true;

    protected const string CHILD_NAME = "ParticleTrail";
    protected ParticleSystem ps;
    protected ParticleSystemRenderer psr;

    private static Material _particleMatCached;

    protected virtual void Reset() => ApplyOrCreate();

    protected virtual void OnValidate()
    {
        if (!autoSetupInEditMode) return;
        ApplyOrCreate();
    }

    public void ApplyOrCreate()
    {
        EnsureChild();
        ApplyCommon();
        ApplyTypeSpecific();

        if (!enabled && ps != null)
            StopAndClear();
    }

    private void EnsureChild()
    {
        var t = transform.Find(CHILD_NAME);
        if (t == null)
        {
            var go = new GameObject(CHILD_NAME);
            t = go.transform;
            t.SetParent(transform, false);
        }

        t.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        ps = t.GetComponent<ParticleSystem>();
        if (ps == null) ps = t.gameObject.AddComponent<ParticleSystem>();

        psr = t.GetComponent<ParticleSystemRenderer>();
        if (psr == null) psr = t.gameObject.AddComponent<ParticleSystemRenderer>();
    }

    private void ApplyCommon()
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = 2000;
        main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Local;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spawnRadius;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.3f;

        psr.sortingLayerName = sortingLayerName;
        psr.sortingOrder = orderInLayer;
        psr.renderMode = ParticleSystemRenderMode.Billboard;

        if (forceGoodParticleMaterial)
        {
            var mat = GetGoodParticleMaterial();
            if (mat != null) psr.sharedMaterial = mat;
        }

        var tsa = ps.textureSheetAnimation;
        if (particleSprite != null)
        {
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;

            while (tsa.spriteCount > 0)
                tsa.RemoveSprite(0);

            tsa.AddSprite(particleSprite);
        }
        else
        {
            tsa.enabled = false;
        }
    }

    private static Material GetGoodParticleMaterial()
    {
        if (_particleMatCached != null) return _particleMatCached;

        // ✅ URP Particles (правильный шейдер именно для ParticleSystemRenderer)
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        // ✅ Built-in fallback
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");

        if (sh == null) return null;

        _particleMatCached = new Material(sh) { name = "VFX_Particles_Shared" };
        return _particleMatCached;
    }

    protected abstract void ApplyTypeSpecific();

    protected virtual void OnEnable()
    {
        if (ps == null)
        {
            var t = transform.Find(CHILD_NAME);
            if (t != null) ps = t.GetComponent<ParticleSystem>();
        }

        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    protected virtual void OnDisable()
    {
        if (ps != null)
            StopAndClear();
    }

    protected void StopAndClear()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);
    }
}
