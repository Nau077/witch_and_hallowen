using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
	public int maxHealth = 30;
	private int currentHealth;

	private SpriteRenderer sr;

	private void Start()
	{
		currentHealth = maxHealth;
		sr = GetComponent<SpriteRenderer>();
	}

	public void TakeDamage(int amount)
	{
		currentHealth -= amount;
		if (currentHealth <= 0)
		{
			Die();
		}
		else
		{
			// краткий визуальный отклик: мигание
			if (sr != null) StartCoroutine(Blink());
		}
	}

	private void Die()
	{
		// можно заменить на анимацию, звук и т.п.
		Destroy(gameObject);
	}

	private System.Collections.IEnumerator Blink()
	{
		sr.enabled = false;
		yield return new WaitForSeconds(0.1f);
		sr.enabled = true;
	}
}
