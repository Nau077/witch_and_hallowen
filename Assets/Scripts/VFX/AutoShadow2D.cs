using UnityEngine;

[DisallowMultipleComponent]
public class AutoShadow2D : MonoBehaviour
{
    [Header("Sizing source")]
    [Tooltip("Usually best for projectiles: collider size is stable across prefabs.")]
    public bool preferColliderSize = true;

    [Tooltip("Overall scale multiplier.")]
    public float sizeMultiplier = 1.25f;

    [Tooltip("Prevents tiny dot on small sprites.")]
    public float minWidth = 0.35f;

    [Range(0.15f, 0.8f)]
    [Tooltip("0.35-0.45 looks like a blob shadow.")]
    public float heightFactor = 0.38f;

    [Header("Offset (cast shadow look)")]
    [Tooltip("Right/down offset in LOCAL space.")]
    public Vector2 baseLocalOffset = new Vector2(0.28f, -0.32f);

    [Tooltip("Extra down offset based on object height.")]
    public float yOffsetMultiplier = 0.55f;

    [Header("Look")]
    [Range(0f, 1f)] public float alpha = 0.30f;
    public Color tint = Color.black;

    [Header("Sorting")]
    [Tooltip("If true: put shadow on SAME sorting layer as projectile sprite.")]
    public bool matchProjectileSortingLayer = true;

    [Tooltip("If true: shadow order = projectileOrder - 1 (recommended).")]
    public bool matchProjectileSortingOrder = true;

    [Tooltip("Used only if matchProjectileSorting* is false.")]
    public string fallbackSortingLayer = "FX";
    public int fallbackSortingOrder = 0;

    [Header("Procedural sprite quality")]
    [Range(64, 512)] public int textureSize = 256;
    [Range(2f, 10f)] public float edgeSoftness = 6f;

    [Header("Debug")]
    public bool debugRed = false;

    private const string CHILD_NAME = "Shadow";

    private static Sprite _sharedShadowSprite;
    private static int _lastTex;
    private static float _lastSoft;

    private static Material _sharedSpriteDefaultMat;

    private Transform _shadowT;
    private SpriteRenderer _shadowSR;

    private void Awake()
    {
        EnsureShadow();
        ApplyShadow();
    }

    private void OnEnable()
    {
        EnsureShadow();
        ApplyShadow();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!gameObject.scene.IsValid()) return;
        EnsureShadow();
        ApplyShadow();
    }
#endif

    private void EnsureShadow()
    {
        // regenerate if settings changed (so edge doesn't look "square")
        if (_sharedShadowSprite == null || _lastTex != textureSize || Mathf.Abs(_lastSoft - edgeSoftness) > 0.001f)
        {
            _sharedShadowSprite = CreateSoftCircleSprite(textureSize, edgeSoftness);
            _lastTex = textureSize;
            _lastSoft = edgeSoftness;
        }

        if (_sharedSpriteDefaultMat == null)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh != null) _sharedSpriteDefaultMat = new Material(sh) { name = "VFX_Shadow_SpritesDefault_Shared" };
        }

        _shadowT = transform.Find(CHILD_NAME);
        if (_shadowT == null)
        {
            var go = new GameObject(CHILD_NAME);
            _shadowT = go.transform;
            _shadowT.SetParent(transform, false);
        }

        _shadowT.gameObject.layer = gameObject.layer;

        _shadowSR = _shadowT.GetComponent<SpriteRenderer>();
        if (_shadowSR == null) _shadowSR = _shadowT.gameObject.AddComponent<SpriteRenderer>();

        _shadowSR.sprite = _sharedShadowSprite;

        // ✅ ключевой фикс против "квадрата": всегда Sprites/Default материал
        if (_sharedSpriteDefaultMat != null)
            _shadowSR.sharedMaterial = _sharedSpriteDefaultMat;

        _shadowSR.enabled = true;
    }

    private void ApplyShadow()
    {
        var srcSR = FindMainSpriteRenderer();
        var col = GetComponent<Collider2D>();

        // --- size ---
        Vector2 size = new Vector2(0.6f, 0.6f);

        if (preferColliderSize && col != null)
        {
            size = col.bounds.size;
        }
        else if (srcSR != null && srcSR.sprite != null)
        {
            // sprite bounds * lossy scale
            Vector3 s = srcSR.transform.lossyScale;
            Vector2 local = srcSR.sprite.bounds.size;
            size = new Vector2(local.x * Mathf.Abs(s.x), local.y * Mathf.Abs(s.y));
        }
        else if (col != null)
        {
            size = col.bounds.size;
        }

        float width = Mathf.Max(minWidth, size.x) * sizeMultiplier;
        float height = width * heightFactor;

        float yUnder = -Mathf.Max(0.10f, size.y) * yOffsetMultiplier;

        _shadowT.localPosition = new Vector3(baseLocalOffset.x, baseLocalOffset.y + yUnder, -0.01f);
        _shadowT.localRotation = Quaternion.identity;
        _shadowT.localScale = new Vector3(width, height, 1f);

        // --- color ---
        if (debugRed)
            _shadowSR.color = new Color(1f, 0f, 0f, 0.9f);
        else
            _shadowSR.color = new Color(tint.r, tint.g, tint.b, alpha);

        // --- sorting ---
        if (srcSR != null && matchProjectileSortingLayer)
            _shadowSR.sortingLayerID = srcSR.sortingLayerID;
        else
            _shadowSR.sortingLayerName = fallbackSortingLayer;

        if (srcSR != null && matchProjectileSortingOrder)
            _shadowSR.sortingOrder = srcSR.sortingOrder - 1;   // ✅ не уезжает за фон
        else
            _shadowSR.sortingOrder = fallbackSortingOrder;
    }

    private SpriteRenderer FindMainSpriteRenderer()
    {
        // Берём любой SpriteRenderer у префаба, который не "Shadow"
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] == null) continue;
            if (_shadowT != null && srs[i].transform == _shadowT) continue;
            return srs[i];
        }
        return GetComponent<SpriteRenderer>();
    }

    private static Sprite CreateSoftCircleSprite(int size, float softness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxR = Mathf.Min(cx, cy);

        float fade = maxR / Mathf.Max(2f, softness);

        var cols = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);

                float a;
                if (r <= maxR - fade) a = 1f;
                else a = Mathf.Clamp01(Mathf.InverseLerp(maxR, maxR - fade, r));

                cols[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }

        tex.SetPixels32(cols);
        tex.Apply(false, true);

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
