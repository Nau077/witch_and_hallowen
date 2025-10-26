using UnityEngine;

public class FitBordersToCamera2D : MonoBehaviour
{
    public Camera cam;
    public BoxCollider2D topBorder, bottomBorder, leftBorder, rightBorder;
    [Min(0.01f)] public float thickness = 0.25f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!cam || !cam.orthographic) return;

        // Если Game-вью временно схлопнулось — пропускаем кадр
        if (cam.pixelRect.width < 1f || cam.pixelRect.height < 1f) return;

        UpdateBorders();
    }

    void UpdateBorders()
    {
        if (!topBorder || !bottomBorder || !leftBorder || !rightBorder) return;

        // Надёжнее брать aspect из pixelWidth/Height
        float pixelW = Mathf.Max(1f, cam.pixelWidth);
        float pixelH = Mathf.Max(1f, cam.pixelHeight);
        float halfH = cam.orthographicSize;
        float halfW = halfH * (pixelW / pixelH);

        Vector3 c = cam.transform.position;

        float left = c.x - halfW;
        float right = c.x + halfW;
        float top = c.y + halfH;
        float bottom = c.y - halfH;

        // Верх/низ
        topBorder.transform.position = new Vector3(c.x, top + thickness * 0.5f, 0f);
        bottomBorder.transform.position = new Vector3(c.x, bottom - thickness * 0.5f, 0f);
        topBorder.size = new Vector2(halfW * 2f + thickness, thickness);
        bottomBorder.size = new Vector2(halfW * 2f + thickness, thickness);

        // Лево/право
        leftBorder.transform.position = new Vector3(left - thickness * 0.5f, c.y, 0f);
        rightBorder.transform.position = new Vector3(right + thickness * 0.5f, c.y, 0f);
        leftBorder.size = new Vector2(thickness, halfH * 2f + thickness);
        rightBorder.size = new Vector2(thickness, halfH * 2f + thickness);
    }
}
