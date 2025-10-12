using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public float speed = 3f;
	public float leftLimit = -7f;
	public float rightLimit = 7f;

	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	void Update()
	{
		float moveInput = Input.GetAxisRaw("Horizontal"); // -1 = A, 1 = D
		Vector2 currentPosition = rb.position;

		// Движение влево/вправо
		currentPosition.x += moveInput * speed * Time.deltaTime;

		// Ограничение движения в пределах поля
		currentPosition.x = Mathf.Clamp(currentPosition.x, leftLimit, rightLimit);

		rb.MovePosition(currentPosition);

		// Отзеркаливание спрайта при движении
		if (moveInput < 0)
			spriteRenderer.flipX = true;
		else if (moveInput > 0)
			spriteRenderer.flipX = false;
	}
}
