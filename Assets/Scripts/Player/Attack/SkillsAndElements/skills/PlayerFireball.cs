using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerFireball : MonoBehaviour, IProjectile
{
    [Header("Flight")] public float speed = 8f;
    public float lifetime = 2f;

    [Header("Damage")] public int damage = 10;

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

        _startPos = transform.position;
        _traveled = 0f;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        Vector3 delta = (Vector3)(_dir * speed * Time.deltaTime);
        transform.Translate(delta, Space.World);
        _traveled = Vector2.Distance(transform.position, _startPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (_traveled < ignoreEnemiesFirstMeters) return;
            var hp = other.GetComponent<EnemyHealth>();
            if (hp != null) hp.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Border") || other.CompareTag("EnemyLaneLimit"))
            Destroy(gameObject);
    }
}
