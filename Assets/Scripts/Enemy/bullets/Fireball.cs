using UnityEngine;

public class Fireball : MonoBehaviour { 
    [Header("Flight")] 
    public float speed = 6f; 
    public float lifetime = 3f; 
    [Header("On Hit Player")] 
    public int damage = 10; // урон по ТЗ
    public float stunDuration = 0.25f; // сколько времени игрок не двигается
    public int blinkCount = 6; // сколько раз мигать
    public float blinkInterval = 0.06f;// период мигания
    private Vector2 direction; 
                                        /// <summary> /// Вызывается врагом при создании снаряда. /// </summary> 
    public void Init(Vector2 dir) {
        direction = dir.normalized; Destroy(gameObject, lifetime);
    } 
    
    private void Update() {
        transform.Translate(direction * speed * Time.deltaTime, Space.World); 
    } 
    private void OnTriggerEnter2D(Collider2D other) {
        // 1) Попали в игрока -> оглушаем, наносим урон,
        // уничтожаемся
        if (other.CompareTag("Player")) { 
            var hp = other.GetComponent<PlayerHealth>(); 
            var pm = other.GetComponent<PlayerMovement>();
            if (hp != null) { hp.TakeDamage(damage); // -10 от 50 //
                     if (!hp.IsDead && pm != null) {
                    pm.OnHit(stunDuration, blinkCount, blinkInterval);
                }
            } 
            
            Destroy(gameObject); return; 
        } // 2) Пересекли нижний край синей полосы -> дальше не летим 
        // Это тонкий триггер LaneBottom с тегом PlayerLaneLimit
        if (other.CompareTag("PlayerLaneLimit")) {
            Destroy(gameObject); return;
        } // 3) Столкновение с краями зоны (если добавлен тег Border)
          
          if (other.CompareTag("Border")) {
            Destroy(gameObject); return;
        } 
    } 
}