using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float speed = 3f;
    public float leftLimit = -7f;
    public float rightLimit = 7f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Состояние оглушения
    private bool isStunned = false;
    private Coroutine blinkRoutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // во время оглушения — не двигаемся
        float moveInput = isStunned ? 0f : Input.GetAxisRaw("Horizontal"); // -1 = A, 1 = D

        Vector2 currentPosition = rb.position;
        currentPosition.x += moveInput * speed * Time.deltaTime;
        currentPosition.x = Mathf.Clamp(currentPosition.x, leftLimit, rightLimit);
        rb.MovePosition(currentPosition);

        // Отзеркаливание спрайта при движении
        if (!isStunned)
        {
            if (moveInput < 0) spriteRenderer.flipX = true;
            else if (moveInput > 0) spriteRenderer.flipX = false;
        }
    }

    /// <summary>
    /// Вызывается снарядом при попадании: оглушение + мигание.
    /// </summary>
    public void OnHit(float stunDuration, int blinkCount, float blinkInterval)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(StunAndBlink(stunDuration, blinkCount, blinkInterval));
    }

    private IEnumerator StunAndBlink(float duration, int blinks, float interval)
    {
        isStunned = true;

        // Если уже мигаем — остановим прошлую корутину
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(Blink(blinks, interval));

        yield return new WaitForSeconds(duration);

        isStunned = false;

        // гарантированно включим спрайт после миганий
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    private IEnumerator Blink(int blinks, float interval)
    {
        for (int i = 0; i < blinks; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(interval);
        }
        // убедимся, что в конце спрайт включен
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }
}
