// PlayerFireballShooter.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFireballShooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;                 // точка выстрела (чуть выше головы)
    public GameObject playerFireballPrefab;     // префаб с PlayerFireball + Collider2D (isTrigger)
    public ChargeDotsUI chargeUI;               // скрипт из п.2

    [Header("Charge")]
    public int maxDots = 3;                     // макс. точек (3 = по 1 секунде на точку)
    public float secondsPerDot = 1f;           // каждые N секунд добавл€етс€ точка
    public float minDistance = 2.5f;           // дистанци€ при 1 точке
    public float maxDistance = 9f;             // дистанци€ при maxDots
    public bool autoReleaseAtMax = true;       // автоматически бросать на полном зар€де

    [Header("Sprites")]
    public Sprite idleSprite;                   // обычный idle (твой базовый)
    public Sprite windupSprite;                 // witch_3_3 (замах/бросок)

    [Header("Direction")]
    public bool shootAlwaysUp = true;           // летит строго вверх (как по лейну)
    public bool useFacingForHorizontal = false; // если false Ч тоже вверх; если true Ч вправо/влево по flipX

    private SpriteRenderer _sr;
    private Coroutine _chargeRoutine;
    private int _currentDots;
    private bool _isCharging;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (chargeUI != null) chargeUI.Clear();
        if (idleSprite == null && _sr != null) idleSprite = _sr.sprite; // авто-берЄм стартовый
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // нажали Ћ ћ Ч начинаем зар€д, если ещЄ не зар€жаемс€
        if (Input.GetMouseButtonDown(0))
        {
            StartCharging();
        }

        // отпустили Ћ ћ Ч бросаем, если шЄл зар€д
        if (Input.GetMouseButtonUp(0))
        {
            ReleaseIfCharging();
        }
    }

    private void StartCharging()
    {
        if (_isCharging) return;
        _isCharging = true;
        _currentDots = 0;
        if (chargeUI != null) chargeUI.Clear();

        // переключаем спрайт на witch_3_3 (замах)
        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        _chargeRoutine = StartCoroutine(ChargeTick());
    }

    private IEnumerator ChargeTick()
    {
        // каждые secondsPerDot добавл€ем точку, максимум maxDots
        while (_isCharging && _currentDots < maxDots)
        {
            yield return new WaitForSeconds(secondsPerDot);
            _currentDots++;
            if (chargeUI != null) chargeUI.SetCount(_currentDots);

            if (autoReleaseAtMax && _currentDots >= maxDots)
            {
                // автоматически бросаем на полном зар€де
                ReleaseThrow();
                yield break;
            }
        }
    }

    private void ReleaseIfCharging()
    {
        if (!_isCharging) return;
        ReleaseThrow();
    }

    private void ReleaseThrow()
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);

        int dots = Mathf.Clamp(_currentDots, 1, Mathf.Max(1, maxDots));
        float t = (maxDots == 1) ? 1f : (dots - 1) / (float)(maxDots - 1); // 1 точка = min, maxDots = max
        float distance = Mathf.Lerp(minDistance, maxDistance, t);

        // выстрел
        if (playerFireballPrefab != null && firePoint != null)
        {
            var go = Instantiate(playerFireballPrefab, firePoint.position, Quaternion.identity);
            var pf = go.GetComponent<PlayerFireball>();
            if (pf != null)
            {
                var cam = Camera.main;
                Vector2 dir = Vector2.up; // дефолт

                if (!shootAlwaysUp)
                {
                    if (cam != null)
                    {
                        Vector3 mp = Input.mousePosition;
                        // рассто€ние от камеры до плоскости выстрела (firePoint)
                        float depth = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
                        if (depth < cam.nearClipPlane + 0.01f) depth = cam.nearClipPlane + 0.01f;

                        mp.z = depth; // ¬ј∆Ќќ: z должен быть > nearClipPlane
                        Vector3 mouseWorld = cam.ScreenToWorldPoint(mp);
                        mouseWorld.z = firePoint.position.z;

                        dir = (mouseWorld - firePoint.position).normalized;
                    }
                }
                else if (useFacingForHorizontal && _sr != null)
                {
                    dir = _sr.flipX ? Vector2.left : Vector2.right;
                }
                // дальше: pf.Init(dir, distance, pf.speed);

                pf.Init(dir, distance, pf.speed); // lifetime подстроитс€ под distance
            }
        }

        // сброс UI + возврат idle
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;

        if (_sr != null && idleSprite != null) _sr.sprite = idleSprite;
    }
}
