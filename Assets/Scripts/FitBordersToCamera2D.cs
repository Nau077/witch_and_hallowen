using UnityEngine;

[ExecuteAlways]
public class FitBordersToCamera2D : MonoBehaviour
{
    public Camera cam;
    public BoxCollider2D topBorder, bottomBorder, leftBorder, rightBorder;
    [Min(0.01f)] public float thickness = 0.25f;   // Толщина рамки

    void OnEnable() { if (!cam) cam = Camera.main; UpdateBorders(); }
    void Update()
    {
        // В редакторе обновляемся сразу при изменении окна/камеры
        if (!Application.isPlaying) UpdateBorders();
    }

    void UpdateBorders()
    {
        if (!cam || !topBorder || !bottomBorder || !leftBorder || !rightBorder) return;

        // Берём видимые края камеры в мировых координатах
        float z = -cam.transform.position.z; // глубина до плоскости XY=0
        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z)); // bottom-left
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z)); // top-right

        float width = tr.x - bl.x;
        float height = tr.y - bl.y;
        float cx = (bl.x + tr.x) * 0.5f;
        float cy = (bl.y + tr.y) * 0.5f;

        // Верх/низ
        topBorder.transform.position = new Vector3(cx, tr.y + thickness * 0.5f, 0f);
        bottomBorder.transform.position = new Vector3(cx, bl.y - thickness * 0.5f, 0f);
        topBorder.size = new Vector2(width, thickness);
        bottomBorder.size = new Vector2(width, thickness);

        // Лево/право
        leftBorder.transform.position = new Vector3(bl.x - thickness * 0.5f, cy, 0f);
        rightBorder.transform.position = new Vector3(tr.x + thickness * 0.5f, cy, 0f);
        leftBorder.size = new Vector2(thickness, height);
        rightBorder.size = new Vector2(thickness, height);
    }
}
