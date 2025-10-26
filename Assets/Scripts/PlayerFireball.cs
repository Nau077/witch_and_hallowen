// PlayerFireball.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerFireball : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 8f;       // �������� �����
    public float lifetime = 2f;    // ������� ���� (�������������� ��� ��������)

    [Header("Damage")]
    public int damage = 10;

    private Vector2 _dir = Vector2.up;

    /// <summary>
    /// �������������: �����������, ��������, ���������.
    /// ��������� = speed * lifetime, ������� ��������� lifetime = distance / speed.
    /// </summary>
    public void Init(Vector2 dir, float distance, float speedOverride = -1f)
    {
        _dir = dir.normalized;

        if (speedOverride > 0f) speed = speedOverride;
        lifetime = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, speed));
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(_dir * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ������ �� �����
        if (other.CompareTag("Enemy"))
        {
            // ���� ���� ���� ������� HP � ������ ����
            var hp = other.GetComponent<EnemyHealth>(); // ���� � ���� � ��� � ������ ���������
            if (hp != null) hp.TakeDamage(damage);
            // ����� ���� �� ����������� ��������� (����� �������� �� ������/��������)
            Destroy(gameObject);
            return;
        }

        // ��������� � �������/���/���-�� ��� � ������������
        if (other.CompareTag("Border") || other.CompareTag("EnemyLaneLimit") )
        {
            Destroy(gameObject);
        }
    }
}
