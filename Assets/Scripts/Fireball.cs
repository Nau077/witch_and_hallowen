using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 6f;
    public float lifetime = 3f;

    private Vector2 direction;

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
        if (other.CompareTag("Player"))
        {
            // потом добавим эффект (звёздочки над игроком)
            Destroy(gameObject);
        }
    }
}
