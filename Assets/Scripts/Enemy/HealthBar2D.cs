using UnityEngine;

public class HealthBar2D : MonoBehaviour
{
    [Header("Refs")]
    public Transform fill; // �������� ������������� ��������

    [Header("Look & Pos")]
    public Vector3 offset = new Vector3(0f, 1.0f, 0f); // ��� �������
    public float width = 1.0f; // ������� ������ ��� 100%
    public bool faceCamera = false; // ���� 2.5D/3D � ����� ��������� � ������

    private int max = 1;
    private int cur = 1;
    private Transform target;

    void Awake()
    {
        target = transform.parent; // ������ ��� � �������� ������ �����
    }

    void LateUpdate()
    {
        if (!target) return;

        transform.position = target.position + offset;

        if (faceCamera && Camera.main)
            transform.rotation = Camera.main.transform.rotation;
        else
            transform.rotation = Quaternion.identity;
    }

    public void SetMax(int value)
    {
        max = Mathf.Max(1, value);
        SetValue(value);
    }

    public void SetValue(int value)
    {
        cur = Mathf.Clamp(value, 0, max);
        if (fill)
        {
            float k = (float)cur / max;
            var s = fill.localScale;
            s.x = Mathf.Max(0f, k) * width;
            fill.localScale = s;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
