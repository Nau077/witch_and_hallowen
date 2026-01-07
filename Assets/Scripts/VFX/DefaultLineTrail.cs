using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class DefaultLineTrail : MonoBehaviour
{
    [Header("Auto")]
    public bool autoSetupInEditMode = true;

    [Header("Spawn point (offset)")]
    public Vector2 localOffset = new Vector2(0f, -0.10f);

    [Header("3 Stripes")]
    public bool useThreeStripes = true;
    public float stripeSpacing = 0.06f;
    [Range(0.1f, 1f)] public float sideWidthMul = 0.75f;
    [Range(0.1f, 1f)] public float sideAlphaMul = 0.75f;

    [Header("Trail settings")]
    public float time = 0.18f;
    public float minVertexDistance = 0.07f;
    public float startWidth = 0.18f;
    public float endWidth = 0.0f;

    [Header("Color")]
    public Color startColor = new Color(1f, 1f, 1f, 0.55f);
    public Color endColor = new Color(1f, 1f, 1f, 0f);

    [Header("Sorting")]
    public string sortingLayerName = "FX";
    public int orderInLayer = 10;

    [Header("Material Fix")]
    public bool forceGoodMaterial = true;

    [Header("Jitter (wiggle)")]
    public bool enableJitter = false;
    public Vector2 jitterAmplitude = new Vector2(0.03f, 0.015f);
    public float jitterFrequency = 14f;
    public float jitterSmooth = 10f;
    public float jitterSeed = 0f;

    private const string CHILD_ROOT = "LineTrail";
    private const string STRIPE_L = "Stripe_L";
    private const string STRIPE_C = "Stripe_C";
    private const string STRIPE_R = "Stripe_R";

    private static Material _trailMatCached;

    // jitter runtime
    private Vector3 _jitterBaseLocalPos;
    private Vector3 _jitterVel;
    private bool _jitterInit;

    private void Reset() => ApplyOrCreate();

    private void OnValidate()
    {
        if (!autoSetupInEditMode) return;
        ApplyOrCreate();
    }

    private void Awake()
    {
        if (jitterSeed == 0f) jitterSeed = Random.Range(0.1f, 9999f);
    }

    private void OnEnable()
    {
        var root = transform.Find(CHILD_ROOT);
        if (root != null)
        {
            foreach (var tr in root.GetComponentsInChildren<TrailRenderer>(true))
                tr.Clear();
        }
        _jitterInit = false;
    }

    public void ApplyOrCreate()
    {
        var root = EnsureChild(transform, CHILD_ROOT, localOffset);

        _jitterBaseLocalPos = root.localPosition;
        _jitterVel = Vector3.zero;
        _jitterInit = true;

        if (useThreeStripes)
        {
            var left = EnsureChild(root, STRIPE_L, new Vector2(-stripeSpacing, 0f));
            var center = EnsureChild(root, STRIPE_C, Vector2.zero);
            var right = EnsureChild(root, STRIPE_R, new Vector2(+stripeSpacing, 0f));

            ApplyTrail(left, startWidth * sideWidthMul, startColor.a * sideAlphaMul);
            ApplyTrail(center, startWidth, startColor.a);
            ApplyTrail(right, startWidth * sideWidthMul, startColor.a * sideAlphaMul);

            var legacy = root.GetComponent<TrailRenderer>();
            if (legacy != null) DestroyImmediateSafe(legacy);
        }
        else
        {
            var t = EnsureChild(root, STRIPE_C, Vector2.zero);
            ApplyTrail(t, startWidth, startColor.a);

            DeleteChildIfExists(root, STRIPE_L);
            DeleteChildIfExists(root, STRIPE_R);
        }

        if (jitterSeed == 0f) jitterSeed = Random.Range(0.1f, 9999f);
    }

    private void LateUpdate()
    {
        if (!enableJitter) return;

        var root = transform.Find(CHILD_ROOT);
        if (root == null) return;

        if (!_jitterInit)
        {
            _jitterBaseLocalPos = root.localPosition;
            _jitterVel = Vector3.zero;
            _jitterInit = true;
            if (jitterSeed == 0f) jitterSeed = Random.Range(0.1f, 9999f);
        }

        float t = Time.time * jitterFrequency;

        float nx = Mathf.PerlinNoise(jitterSeed, t) * 2f - 1f;
        float ny = Mathf.PerlinNoise(jitterSeed + 17.3f, t) * 2f - 1f;

        Vector3 target = _jitterBaseLocalPos + new Vector3(nx * jitterAmplitude.x, ny * jitterAmplitude.y, 0f);

        if (jitterSmooth <= 0.01f) root.localPosition = target;
        else root.localPosition = Vector3.SmoothDamp(root.localPosition, target, ref _jitterVel, 1f / jitterSmooth);
    }

    private void ApplyTrail(Transform stripe, float stripeStartWidth, float alphaStart)
    {
        var tr = stripe.GetComponent<TrailRenderer>();
        if (tr == null) tr = stripe.gameObject.AddComponent<TrailRenderer>();

        tr.time = time;
        tr.minVertexDistance = minVertexDistance;
        tr.startWidth = stripeStartWidth;
        tr.endWidth = endWidth;
        tr.autodestruct = false;
        tr.emitting = true;

        tr.sortingLayerName = sortingLayerName;
        tr.sortingOrder = orderInLayer;

        var sc = startColor; sc.a = alphaStart;
        var ec = endColor; ec.a = 0f;

        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(sc, 0f), new GradientColorKey(ec, 1f) },
            new[] { new GradientAlphaKey(sc.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        tr.colorGradient = g;

        if (forceGoodMaterial)
        {
            var mat = GetGoodTrailMaterial();
            if (mat != null) tr.sharedMaterial = mat;
        }
    }

    private static Material GetGoodTrailMaterial()
    {
        if (_trailMatCached != null) return _trailMatCached;

        // ✅ URP 2D (твой случай по Sprite-Lit-Default)
        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");

        // ✅ Built-in fallback
        if (sh == null) sh = Shader.Find("Sprites/Default");

        if (sh == null) return null;

        _trailMatCached = new Material(sh) { name = "VFX_Trail_Shared" };
        return _trailMatCached;
    }

    private Transform EnsureChild(Transform parent, string name, Vector2 localPos2)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            t = go.transform;
            t.SetParent(parent, false);
        }

        t.localPosition = new Vector3(localPos2.x, localPos2.y, 0f);
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
        return t;
    }

    private void DeleteChildIfExists(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) DestroyImmediateSafe(t.gameObject);
    }

    private static void DestroyImmediateSafe(Object obj)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) Object.DestroyImmediate(obj);
        else Object.Destroy(obj);
#else
        Object.Destroy(obj);
#endif
    }
}
