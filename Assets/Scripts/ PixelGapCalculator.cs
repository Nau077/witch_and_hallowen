using UnityEngine;

[ExecuteAlways]
public class PixelGapCalculator : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                       // Main Camera (Orthographic)
    public SpriteRenderer platform;          // твоя основная площадка (Ground)

    [Header("Pixel Settings")]
    public int pixelsPerUnit = 32;           // твой PPU (должен совпадать с импортом спрайтов)

    [Header("Optional Fillers (Tiled sprites)")]
    public bool sizeFillersAutomatically = false;
    public SpriteRenderer fillerLeft;        // тайловый спрайт слева
    public SpriteRenderer fillerRight;       // тайловый спрайт справа
    public SpriteRenderer fillerTop;         // тайловый спрайт сверху

    [Header("Debug (read-only)")]
    public Vector2 cameraSizeWU;             // ширина/высота камеры (world units)
    public float leftGapWU, rightGapWU, topGapWU;   // зазоры в мире
    public int leftGapPX, rightGapPX, topGapPX;     // зазоры в пикселях

    void Reset()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null || platform == null || !cam.orthographic) return;

        // --- Camera world rect ---
        float h = 2f * cam.orthographicSize;
        float w = h * cam.aspect;
        cameraSizeWU = new Vector2(w, h);

        float camMinX = cam.transform.position.x - w * 0.5f;
        float camMaxX = cam.transform.position.x + w * 0.5f;
        float camMaxY = cam.transform.position.y + h * 0.5f;

        // --- Platform world bounds ---
        var pb = platform.bounds;
        float platMinX = pb.min.x;
        float platMaxX = pb.max.x;
        float platMaxY = pb.max.y;

        // --- Gaps (world units) ---
        leftGapWU = Mathf.Max(0f, platMinX - camMinX);
        rightGapWU = Mathf.Max(0f, camMaxX - platMaxX);
        topGapWU = Mathf.Max(0f, camMaxY - platMaxY);

        // --- To pixels ---
        leftGapPX = Mathf.RoundToInt(leftGapWU * pixelsPerUnit);
        rightGapPX = Mathf.RoundToInt(rightGapWU * pixelsPerUnit);
        topGapPX = Mathf.RoundToInt(topGapWU * pixelsPerUnit);

        if (sizeFillersAutomatically)
        {
            // Подгоняем size тайловых спрайтов (Draw Mode = Tiled).
            // Высоты/ширины берём в МИРОВЫХ единицах, не в пикселях.
            if (fillerLeft != null)
            {
                SetTiledSize(fillerLeft, widthWU: leftGapWU, heightWU: pb.size.y);
                // позиционирование вплотную к платформе слева:
                Vector3 p = fillerLeft.transform.position;
                p.y = platform.transform.position.y; // выровнять по Y (по желанию)
                p.x = platMinX - leftGapWU * 0.5f;   // центр левого блока
                fillerLeft.transform.position = p;
            }

            if (fillerRight != null)
            {
                SetTiledSize(fillerRight, widthWU: rightGapWU, heightWU: pb.size.y);
                Vector3 p = fillerRight.transform.position;
                p.y = platform.transform.position.y;
                p.x = platMaxX + rightGapWU * 0.5f;
                fillerRight.transform.position = p;
            }

            if (fillerTop != null)
            {
                SetTiledSize(fillerTop, widthWU: w, heightWU: topGapWU);
                Vector3 p = fillerTop.transform.position;
                p.y = platMaxY + topGapWU * 0.5f;
                p.x = cam.transform.position.x; // по центру камеры
                fillerTop.transform.position = p;
            }
        }
    }

    private void SetTiledSize(SpriteRenderer sr, float widthWU, float heightWU)
    {
        if (sr.drawMode != SpriteDrawMode.Tiled)
            sr.drawMode = SpriteDrawMode.Tiled;

        // Size — в мировых единицах.
        sr.size = new Vector2(Mathf.Max(0.001f, widthWU), Mathf.Max(0.001f, heightWU));
    }

    void OnDrawGizmos()
    {
        if (cam == null || !cam.orthographic) return;

        float h = 2f * cam.orthographicSize;
        float w = h * cam.aspect;

        Vector3 center = cam.transform.position;
        var prev = Gizmos.color;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, new Vector3(w, h, 0.1f));

        if (platform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(platform.bounds.center, platform.bounds.size);
        }

        Gizmos.color = prev;
    }
}
