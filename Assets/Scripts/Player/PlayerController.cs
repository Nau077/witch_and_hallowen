using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float leftLimit = -9.5f;
    public float rightLimit = 9.5f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerFireballShooter shooter;

    void Awake()
    {
        shooter = FindObjectOfType<PlayerFireballShooter>(); // ��� GetComponent/������ �� ����������
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float move = Input.GetAxisRaw("Horizontal"); // A/D ��� �������

        // ������� ������
        Vector2 newPos = transform.position;
        newPos.x += move * moveSpeed * Time.deltaTime;

        // ����������� � �������� Ground
        newPos.x = Mathf.Clamp(newPos.x, leftLimit, rightLimit);

        transform.position = newPos;

        // �������� ������ �����/������
        if (move != 0 && shooter != null && !shooter.IsCharging)
            sr.flipX = move < 0;
    }
}
