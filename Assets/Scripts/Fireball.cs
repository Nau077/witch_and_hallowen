using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 6f;
    public float lifetime = 3f;

    [Header("On Hit Player")]
    public int damage = 10;           // ���� �� ��
    public float stunDuration = 0.25f;  // ������� ������� ����� �� ���������
    public int blinkCount = 6;      // ������� ��� ������
    public float blinkInterval = 0.06f; // ������ �������

    private Vector2 direction;

    /// <summary>
    /// ���������� ������ ��� �������� �������.
    /// </summary>
    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1) ������ � ������ -> ��������, ������� ����, ������������
        if (other.CompareTag("Player"))
        {
            // ���������/�������
            var pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.OnHit(stunDuration, blinkCount, blinkInterval);

            // ���� � ���������� ������
            var hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.TakeDamage(damage); // -10 �� 50

            Destroy(gameObject);
            return;
        }

        // 2) ��������� ������ ���� ����� ������ -> ������ �� �����
        //    ��� ������ ������� LaneBottom � ����� PlayerLaneLimit
        if (other.CompareTag("PlayerLaneLimit"))
        {
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Border")) { Destroy(gameObject); return; }
    }
}