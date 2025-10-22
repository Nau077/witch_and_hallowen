using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 6f;
    public float lifetime = 3f;

    [Header("On Hit Player")]
    public float stunDuration = 0.25f;   // сколько времени игрок не двигается
    public int blinkCount = 6;       // сколько раз мигать
    public float blinkInterval = 0.06f;  // период мигания

    private Vector2 direction;

    /// <summary>
    /// Вызывается врагом при создании снаряда.
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
        // 1) Попали в игрока -> оглушаем и уничтожаемся
        if (other.CompareTag("Player"))
        {
            var pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.OnHit(stunDuration, blinkCount, blinkInterval);

            Destroy(gameObject);
            return;
        }

        // 2) Пересекли НИЖНИЙ край синей полосы -> дальше не летим
        //    (это тонкий триггер LaneBottom с тегом PlayerLaneLimit)
        if (other.CompareTag("PlayerLaneLimit"))
        {
            Destroy(gameObject);
            return;
        }

        // Дополнительно, если есть бордюры с тегом Border — тоже гасим
        if (other.CompareTag("Border"))
        {
            Destroy(gameObject);
            return;
        }
    }
}
