// PlayerFireball.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerFireball : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 8f;       // скорость полЄта
    public float lifetime = 2f;    // сколько жить (корректируетс€ при выстреле)

    [Header("Damage")]
    public int damage = 10;

    private Vector2 _dir = Vector2.up;

    /// <summary>
    /// »нициализаци€: направление, скорость, дистанци€.
    /// ƒистанци€ = speed * lifetime, поэтому подбираем lifetime = distance / speed.
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
        // ѕопали во врага
        if (other.CompareTag("Enemy"))
        {
            // если есть сво€ система HP Ч снимем урон
            var hp = other.GetComponent<EnemyHealth>(); // если у теб€ еЄ нет Ч просто игноритс€
            if (hp != null) hp.TakeDamage(damage);
            // иначе хот€ бы зафиксируем попадание (можно заменить на эффект/анимацию)
            Destroy(gameObject);
            return;
        }

        // ¬резались в границы/пол/что-то ещЄ Ч уничтожаемс€
        if (other.CompareTag("Border") || other.CompareTag("EnemyLaneLimit") )
        {
            Destroy(gameObject);
        }
    }
}
