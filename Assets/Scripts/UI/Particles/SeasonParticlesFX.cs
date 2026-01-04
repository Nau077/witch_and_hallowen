using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class SeasonParticlesFX : MonoBehaviour
{
    public enum Preset { Fire, Snow }

    [Header("Preset")]
    public Preset preset = Preset.Fire;

    [Header("Rendering")]
    public string sortingLayerName = "FX";
    public int sortingOrder = 200;

    [Header("Sprites (optional but recommended)")]
    public Sprite fireSprite;
    public Sprite snowSprite;

    [Header("Colors")]
    public Gradient fireColorOverLifetime;
    public Gradient snowColorOverLifetime;

    [Header("Tuning")]
    [Tooltip("Насколько шире/выше экрана делать область спавна частиц (чтобы не было пустот на краях).")]
    [Min(1f)] public float spawnPadding = 1.2f;

    [Tooltip("Привязать объект к камере (чтобы эффект всегда был на экране).")]
    public bool followCamera = true;

    [Tooltip("Z позиции для FX. Обычно 0, но если у тебя камера/слои по Z — подстрой.")]
    public float zPosition = 0f;

    [Header("Diagnostics")]
    [Tooltip("Если включишь — скрипт принудительно выключит Limit Velocity / Inherit Velocity (на случай, если шаблон PS их включил).")]
    public bool forceDisableOtherVelocityModules = true;

    private ParticleSystem ps;
    private Camera cam;

    void OnEnable()
    {
        Ensure();
        ApplyPreset(preset);
        FitToCamera();
    }

    void Reset()
    {
        Ensure();
        ApplyPreset(preset);
        FitToCamera();
    }

    void OnValidate()
    {
        Ensure();
        ApplyPreset(preset);
        FitToCamera();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            Ensure();
            FitToCamera();
        }

        if (followCamera)
            FollowCamera();
    }

    /// <summary>Вызов из RunManager/LevelManager чтобы переключать сезон.</summary>
    public void SetPreset(Preset newPreset)
    {
        preset = newPreset;
        Ensure();
        ApplyPreset(preset);

        ps.Clear(true);
        ps.Play(true);
    }

    private void Ensure()
    {
        if (!ps) ps = GetComponent<ParticleSystem>();
        if (!ps) ps = gameObject.AddComponent<ParticleSystem>();

        if (!cam) cam = Camera.main;
        if (!cam) cam = FindFirstObjectByType<Camera>();

        // Default gradients if empty
        if (fireColorOverLifetime == null || fireColorOverLifetime.colorKeys == null || fireColorOverLifetime.colorKeys.Length == 0)
            fireColorOverLifetime = MakeSimpleGradient(
                new Color(1f, 0.55f, 0.15f, 1f),
                new Color(1f, 0.2f, 0.05f, 0f)
            );

        if (snowColorOverLifetime == null || snowColorOverLifetime.colorKeys == null || snowColorOverLifetime.colorKeys.Length == 0)
            snowColorOverLifetime = MakeSimpleGradient(
                new Color(0.85f, 0.95f, 1f, 1f),
                new Color(0.85f, 0.95f, 1f, 0f)
            );

        // Renderer setup
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sortingLayerName = sortingLayerName;
        r.sortingOrder = sortingOrder;
        r.renderMode = ParticleSystemRenderMode.Billboard;

        // Use sprite shader by default
        if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name != "Sprites/Default")
            r.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    private void ApplyPreset(Preset p)
    {
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var vel = ps.velocityOverLifetime;
        var noise = ps.noise;
        var col = ps.colorOverLifetime;
        var size = ps.sizeOverLifetime;
        var tex = ps.textureSheetAnimation;

        // ===== Common defaults =====
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 2000;

        emission.enabled = true;

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;

        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        col.enabled = true;

        size.enabled = true;
        size.separateAxes = false;

        // Texture Sheet Animation (Sprites) — включаем только если есть sprite
        tex.enabled = false;
        tex.mode = ParticleSystemAnimationMode.Sprites;

        // Velocity over Lifetime: используем ТОЛЬКО x/y/z и ВСЕ в одном режиме,
        // иначе Unity будет спамить "Particle Velocity curves must all be in the same mode".
        vel.enabled = true;

        // Optional: disable other velocity-like modules that can cause confusion/spam in some templates
        if (forceDisableOtherVelocityModules)
        {
            var limitVel = ps.limitVelocityOverLifetime;
            limitVel.enabled = false;

            var inheritVel = ps.inheritVelocity;
            inheritVel.enabled = false;
        }

        switch (p)
        {
            case Preset.Fire:
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);

                emission.rateOverTime = 25f;

                // 🔥 Fire drifts slightly up + small horizontal
                // IMPORTANT: all three axes are TwoConstants (even z is 0..0)
                vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
                vel.y = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

                noise.strength = 0.25f;
                noise.frequency = 0.35f;

                col.color = new ParticleSystem.MinMaxGradient(fireColorOverLifetime);
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1f, 1f, 0.2f));

                ApplySingleSpriteToTextureSheet(tex, fireSprite);
                break;

            case Preset.Snow:
                main.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 6.0f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);

                emission.rateOverTime = 18f;

                // ❄️ Snow falls down + small horizontal drift
                // IMPORTANT: all three axes are TwoConstants (even z is 0..0)
                vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.45f, -0.15f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

                noise.strength = 0.15f;
                noise.frequency = 0.25f;

                col.color = new ParticleSystem.MinMaxGradient(snowColorOverLifetime);
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1f, 1f, 0.6f));

                ApplySingleSpriteToTextureSheet(tex, snowSprite);
                break;
        }

        // Re-apply renderer settings
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sortingLayerName = sortingLayerName;
        r.sortingOrder = sortingOrder;
    }

    private static void ApplySingleSpriteToTextureSheet(ParticleSystem.TextureSheetAnimationModule tex, Sprite sprite)
    {
        if (sprite == null)
        {
            tex.enabled = false;
            return;
        }

        tex.enabled = true;
        tex.mode = ParticleSystemAnimationMode.Sprites;

        while (tex.spriteCount > 0)
            tex.RemoveSprite(0);

        tex.AddSprite(sprite);
    }

    private void FitToCamera()
    {
        if (!cam || !cam.orthographic) return;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        var shape = ps.shape;
        shape.scale = new Vector3(width * spawnPadding, height * spawnPadding, 0.1f);

        var camPos = cam.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, zPosition);
    }

    private void FollowCamera()
    {
        if (!cam) return;
        var p = cam.transform.position;
        transform.position = new Vector3(p.x, p.y, zPosition);
    }

    private static Gradient MakeSimpleGradient(Color from, Color to)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(from.a, 0f), new GradientAlphaKey(to.a, 1f) }
        );
        return g;
    }
}
