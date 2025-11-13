using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerIceShard : MonoBehaviour, IProjectile
{
    [Header("Flight")] public float speed = 7.5f;
    public float lifetime = 2.2f;

    [Header("Damage")] public int damage = 9;

    [HideInInspector] public float ignoreEnemiesFirstMeters = 0f;

    private Vector2 _dir = Vector2.up;
    private Vector2 _startPos;
    private float _traveled;

    public void Init(Vector2 dir, float distance, float speedOverride = -1f, float ignoreFirstMeters = 0f)
    {
        _dir = dir.normalized;
        if (speedOverride > 0f) speed = speedOverride;

        lifetime = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, speed));
        ignoreEnemiesFirstMeters = Mathf.Max(0f, ignoreFirstMeters);

        _startPos = (Vector2)transform.position;
        _traveled = 0f;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        Vector3 d = (Vector3)(_dir * speed * Time.deltaTime);
        transform.Translate(d, Space.World);
        _traveled = Vector2.Distance(transform.position, _startPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (_traveled < ignoreEnemiesFirstMeters) return;
            var hp = other.GetComponent<EnemyHealth>();
            if (hp != null) hp.TakeDamage(damage);

            // Здесь позже можно повесить заморозку/замедление
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Border") || other.CompareTag("EnemyLaneLimit"))
            Destroy(gameObject);
    }
}
