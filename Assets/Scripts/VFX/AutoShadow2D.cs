using UnityEngine;

[DisallowMultipleComponent]
public class AutoShadow2D_Procedural : MonoBehaviour
{
    [Header("Auto sizing")]
    public bool preferColliderSize = false;
    public float sizeMultiplier = 1.0f;
    [Range(0.15f, 0.8f)] public float heightFactor = 0.35f; // ellipse squash
    public float yOffsetMultiplier = 0.55f; // how far under the sprite (relative to height)

    [Header("Look")]
    [Range(0f, 1f)] public float alpha = 0.35f;
    public Color tint = Color.black;

    [Header("Sorting")]
    public bool matchProjectileSorting = true;
    public string fallbackSortingLayer = "FX";
    public int fallbackOrder = -50; // if can't detect

    [Header("Perf")]
    [Range(32, 256)] public int textureSize = 96; // soft edge quality
    [Range(1.5f, 6f)] public float edgeSoftness = 3.2f; // bigger = softer edge

    private const string CHILD = "Shadow";
    private static Sprite _sharedShadowSprite;     // shared across all instances
    private static Material _sharedUnlitMat;       // optional (URP safe)

    private Transform _shadowT;
    private SpriteRenderer _shadowSR;

    private void Awake()
    {
        EnsureShadow();
        Apply();
    }

    private void OnEnable()
    {
        // In pooled projectiles, re-apply on enable
        EnsureShadow();
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!gameObject.scene.IsValid()) return; // avoid weird prefab-stage spam
        EnsureShadow();
        Apply();
    }
#endif

    private void EnsureShadow()
    {
        if (_sharedShadowSprite == null)
            _sharedShadowSprite = CreateSoftCircleSprite(textureSize, edgeSoftness);

        if (_sharedUnlitMat == null)
            _sharedUnlitMat = TryCreateUnlitSpriteMaterial();

        _shadowT = transform.Find(CHILD);
        if (_shadowT == null)
        {
            var go = new GameObject(CHILD);
            _shadowT = go.transform;
            _shadowT.SetParent(transform, false);
        }

        _shadowSR = _shadowT.GetComponent<SpriteRenderer>();
        if (_shadowSR == null) _shadowSR = _shadowT.gameObject.AddComponent<SpriteRenderer>();

        _shadowSR.sprite = _sharedShadowSprite;

        // Make sure it's visible regardless of lighting.
        // If you have URP 2D and Lit sprites, a Lit material with no 2D lights can go black.
        // So we force Unlit material if we found one; otherwise leave default.
        if (_sharedUnlitMat != null)
            _shadowSR.sharedMaterial = _sharedUnlitMat;

        // Very important: not affected by light / not tinted by sprite lit logic
        _shadowSR.color = new Color(tint.r, tint.g, tint.b, alpha);
    }

    private void Apply()
    {
        // 1) Determine size source
        Bounds b;
        bool ok = TryGetSourceBounds(out b);

        if (!ok)
        {
            // fallback: still show something
            b = new Bounds(transform.position, new Vector3(0.6f, 0.6f, 0f));
        }

        float width = Mathf.Max(0.02f, b.size.x) * sizeMultiplier;
        float height = width * heightFactor;

        // 2) Position under projectile
        float yOff = -Mathf.Max(0.02f, b.size.y) * yOffsetMultiplier;

        _shadowT.localPosition = new Vector3(0f, yOff, 0f);
        _shadowT.localRotation = Quaternion.identity;
        _shadowT.localScale = new Vector3(width, height, 1f);

        // 3) Sorting: under projectile
        if (matchProjectileSorting)
        {
            var srcSR = FindSourceSpriteRenderer();
            if (srcSR != null)
            {
                _shadowSR.sortingLayerID = srcSR.sortingLayerID;
                _shadowSR.sortingOrder = srcSR.sortingOrder - 1;
            }
            else
            {
                _shadowSR.sortingLayerName = fallbackSortingLayer;
                _shadowSR.sortingOrder = fallbackOrder;
            }
        }
        else
        {
            _shadowSR.sortingLayerName = fallbackSortingLayer;
            _shadowSR.sortingOrder = fallbackOrder;
        }
    }

    private bool TryGetSourceBounds(out Bounds b)
    {
        // Prefer collider if asked
        if (preferColliderSize)
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                b = col.bounds;
                return true;
            }
        }

        // Try SpriteRenderer on self or children (projectiles often have child sprite)
        var sr = FindSourceSpriteRenderer();
        if (sr != null && sr.sprite != null)
        {
            b = sr.bounds;
            return true;
        }

        // fallback to collider if exists
        var col2 = GetComponent<Collider2D>();
        if (col2 != null)
        {
            b = col2.bounds;
            return true;
        }

        b = default;
        return false;
    }

    private SpriteRenderer FindSourceSpriteRenderer()
    {
        // First on self
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) return sr;

        // Then in children
        return GetComponentInChildren<SpriteRenderer>(true);
    }

    private static Material TryCreateUnlitSpriteMaterial()
    {
        // Try URP 2D unlit first
        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

        // Fallbacks
        if (sh == null) sh = Shader.Find("Sprites/Default");

        if (sh == null) return null;

        // Shared material once for all shadows (no per-instance allocations)
        var mat = new Material(sh) { name = "VFX_Shadow_Unlit_Shared" };
        return mat;
    }

    private static Sprite CreateSoftCircleSprite(int size, float softness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        tex.name = "proc_shadow_circle";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxR = Mathf.Min(cx, cy);

        // softness controls how wide the fade band is
        float fade = maxR / Mathf.Max(1.5f, softness);

        var cols = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);

                // alpha: 1 at center -> 0 at edge
                float a;
                if (r <= maxR - fade) a = 1f;
                else
                {
                    float t = Mathf.InverseLerp(maxR, maxR - fade, r);
                    a = Mathf.Clamp01(t);
                }

                byte alphaByte = (byte)Mathf.RoundToInt(a * 255f);
                cols[y * size + x] = new Color32(255, 255, 255, alphaByte);
            }
        }

        tex.SetPixels32(cols);
        tex.Apply(false, true); // make it non-readable (memory-friendly)

        var rect = new Rect(0, 0, size, size);
        var pivot = new Vector2(0.5f, 0.5f);

        // Pixels per unit: 100 is fine; we scale transform anyway
        return Sprite.Create(tex, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
    }
}
