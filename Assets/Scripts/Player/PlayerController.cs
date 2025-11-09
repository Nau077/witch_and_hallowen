using UnityEngine;

/// <summary>
/// Простой контроллер перемещения по горизонтали + флип спрайта.
/// Обновлён: больше не зависит от PlayerFireballShooter. Использует (если есть) PlayerSkillShooter.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float leftLimit = -9.5f;
    public float rightLimit = 9.5f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // НЕ обязательная ссылка — если стрелка нет, код просто не будет учитывать "заряд".
    private PlayerSkillShooter shooter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        shooter = GetComponent<PlayerSkillShooter>() ?? FindObjectOfType<PlayerSkillShooter>();
    }

    void Update()
    {
        float move = Input.GetAxisRaw("Horizontal"); // A/D или стрелки

        // Двигаем ведьму
        Vector2 newPos = transform.position;
        newPos.x += move * moveSpeed * Time.deltaTime;

        // Ограничение в пределах Ground
        newPos.x = Mathf.Clamp(newPos.x, leftLimit, rightLimit);

        transform.position = newPos;

        // Отражаем спрайт влево/вправо (если нет активного заряда)
        bool isCharging = shooter != null && shooter.IsChargingPublic;
        if (move != 0 && !isCharging && sr != null)
            sr.flipX = move < 0;
    }
}
