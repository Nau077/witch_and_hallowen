// PlayerFireballShooter.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFireballShooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;                 // ����� �������� (���� ���� ������)
    public GameObject playerFireballPrefab;     // ������ � PlayerFireball + Collider2D (isTrigger)
    public ChargeDotsUI chargeUI;               // ������ �� �.2

    [Header("Charge")]
    public int maxDots = 3;                     // ����. ����� (3 = �� 1 ������� �� �����)
    public float secondsPerDot = 1f;           // ������ N ������ ����������� �����
    public float minDistance = 2.5f;           // ��������� ��� 1 �����
    public float maxDistance = 9f;             // ��������� ��� maxDots
    public bool autoReleaseAtMax = true;       // ������������� ������� �� ������ ������

    [Header("Sprites")]
    public Sprite idleSprite;                   // ������� idle (���� �������)
    public Sprite windupSprite;                 // witch_3_3 (�����/������)

    [Header("Direction")]
    public bool shootAlwaysUp = true;           // ����� ������ ����� (��� �� �����)
    public bool useFacingForHorizontal = false; // ���� false � ���� �����; ���� true � ������/����� �� flipX

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
        if (idleSprite == null && _sr != null) idleSprite = _sr.sprite; // ����-���� ���������
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // ������ ��� � �������� �����, ���� ��� �� ����������
        if (Input.GetMouseButtonDown(0))
        {
            StartCharging();
        }

        // ��������� ��� � �������, ���� ��� �����
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

        // ����������� ������ �� witch_3_3 (�����)
        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        _chargeRoutine = StartCoroutine(ChargeTick());
    }

    private IEnumerator ChargeTick()
    {
        // ������ secondsPerDot ��������� �����, �������� maxDots
        while (_isCharging && _currentDots < maxDots)
        {
            yield return new WaitForSeconds(secondsPerDot);
            _currentDots++;
            if (chargeUI != null) chargeUI.SetCount(_currentDots);

            if (autoReleaseAtMax && _currentDots >= maxDots)
            {
                // ������������� ������� �� ������ ������
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
        float t = (maxDots == 1) ? 1f : (dots - 1) / (float)(maxDots - 1); // 1 ����� = min, maxDots = max
        float distance = Mathf.Lerp(minDistance, maxDistance, t);

        // �������
        if (playerFireballPrefab != null && firePoint != null)
        {
            var go = Instantiate(playerFireballPrefab, firePoint.position, Quaternion.identity);
            var pf = go.GetComponent<PlayerFireball>();
            if (pf != null)
            {
                var cam = Camera.main;
                Vector2 dir = Vector2.up; // ������

                if (!shootAlwaysUp)
                {
                    if (cam != null)
                    {
                        Vector3 mp = Input.mousePosition;
                        // ���������� �� ������ �� ��������� �������� (firePoint)
                        float depth = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
                        if (depth < cam.nearClipPlane + 0.01f) depth = cam.nearClipPlane + 0.01f;

                        mp.z = depth; // �����: z ������ ���� > nearClipPlane
                        Vector3 mouseWorld = cam.ScreenToWorldPoint(mp);
                        mouseWorld.z = firePoint.position.z;

                        dir = (mouseWorld - firePoint.position).normalized;
                    }
                }
                else if (useFacingForHorizontal && _sr != null)
                {
                    dir = _sr.flipX ? Vector2.left : Vector2.right;
                }
                // ������: pf.Init(dir, distance, pf.speed);

                pf.Init(dir, distance, pf.speed); // lifetime ����������� ��� distance
            }
        }

        // ����� UI + ������� idle
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;

        if (_sr != null && idleSprite != null) _sr.sprite = idleSprite;
    }
}
