using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 6f;
    public float lifetime = 3f;

    [Header("On Hit Player")]
    public int damage = 10;           // урон по “«
    public float stunDuration = 0.25f;  // сколько времени игрок не двигаетс€
    public int blinkCount = 6;      // сколько раз мигать
    public float blinkInterval = 0.06f; // период мигани€

    private Vector2 direction;

    /// <summary>
    /// ¬ызываетс€ врагом при создании снар€да.
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
        // 1) ѕопали в игрока -> оглушаем, наносим урон, уничтожаемс€
        if (other.CompareTag("Player"))
        {
            // оглушение/мигание
            var pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.OnHit(stunDuration, blinkCount, blinkInterval);

            // урон и обновление полосы
            var hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.TakeDamage(damage); // -10 от 50

            Destroy(gameObject);
            return;
        }

        // 2) ѕересекли нижний край синей полосы -> дальше не летим
        //    Ёто тонкий триггер LaneBottom с тегом PlayerLaneLimit
        if (other.CompareTag("PlayerLaneLimit"))
        {
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Border")) { Destroy(gameObject); return; }
    }
}