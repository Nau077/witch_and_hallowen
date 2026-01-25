using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkullPickup : MonoBehaviour
{
    [Header("Reward")]
    public int clicksRequired = 5;
    public int soulsReward = 5;

    [Header("Distance gate (in grid cells)")]
    [Tooltip("Размер клетки в world units. Например, 1 = 1 юнит = 1 клетка.")]
    public float cellSize = 1f;

    [Tooltip("Сколько клеток по X разрешено от черепа (влево/вправо). 3 = 3 до + череп + 3 после.")]
    public int rangeXCells = 3;

    [Tooltip("Сколько клеток по Y разрешено от черепа (вверх/вниз). 1 = можно кликать на 1 клетку выше/ниже.")]
    public int rangeYCells = 1;

    [Tooltip("Если пусто — возьмём RunLevelManager.Instance.playerTransform или найдём по тегу Player")]
    public Transform player;

    [Header("Refs (optional, can be auto-found)")]
    public SpriteRenderer targetRenderer;

    [Tooltip("Если пусто — найдём Image BarFill по пути ProgressCanvas/BarBG/BarFill")]
    public Image barFillImage;

    [Header("Click feedback")]
    public Color clickFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float clickFlashTime = 0.06f;

    private int _clicks = 0;
    private bool _done = false;

    private Color _baseColor = Color.white;
    private Coroutine _flashRoutine;

    // ======= BLOCK ATTACK THIS FRAME =======
    private static int _consumeFrame = -9999;
    public static bool ClickConsumedThisFrame => _consumeFrame == Time.frameCount;
    private static void ConsumeClickThisFrame() => _consumeFrame = Time.frameCount;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (targetRenderer != null)
            _baseColor = targetRenderer.color;

        if (player == null)
        {
            if (RunLevelManager.Instance != null && RunLevelManager.Instance.playerTransform != null)
                player = RunLevelManager.Instance.playerTransform;
            else
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) player = p.transform;
            }
        }

        if (barFillImage == null)
        {
            var t = transform.Find("ProgressCanvas/BarBG/BarFill");
            if (t != null) barFillImage = t.GetComponent<Image>();
            if (barFillImage == null)
            {
                var all = GetComponentsInChildren<Transform>(true);
                foreach (var tr in all)
                {
                    if (tr.name == "BarFill")
                    {
                        barFillImage = tr.GetComponent<Image>();
                        break;
                    }
                }
            }
        }

        SetBarProgress(0f);
    }

    private void OnMouseDown()
    {
        if (_done) return;

        // 1) дистанция (прямоугольник в клетках)
        if (player != null && !IsPlayerInCellGate())
            return;

        // 2) съедаем клик, чтобы атака не сработала в этом кадре
        ConsumeClickThisFrame();

        // 3) логика клика
        _clicks++;
        FlashClick();

        float p = Mathf.Clamp01((float)_clicks / Mathf.Max(1, clicksRequired));
        SetBarProgress(p);

        if (_clicks >= clicksRequired)
            Complete();
    }

    private bool IsPlayerInCellGate()
    {
        if (cellSize <= 0.0001f) cellSize = 1f;

        Vector2 skullPos = transform.position;
        Vector2 playerPos = player.position;

        float dxCells = Mathf.Abs(playerPos.x - skullPos.x) / cellSize;
        float dyCells = Mathf.Abs(playerPos.y - skullPos.y) / cellSize;

        int rx = Mathf.Max(0, rangeXCells);
        int ry = Mathf.Max(0, rangeYCells);

        // Включая клетку черепа: dxCells <= rx и dyCells <= ry
        return dxCells <= (rx + 0.0001f) && dyCells <= (ry + 0.0001f);
    }

    private void FlashClick()
    {
        if (targetRenderer == null) return;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (targetRenderer == null) yield break;

        targetRenderer.color = clickFlashColor;
        yield return new WaitForSecondsRealtime(clickFlashTime);

        if (targetRenderer != null)
            targetRenderer.color = _baseColor;
    }

    private void SetBarProgress(float p01)
    {
        if (barFillImage == null) return;
        barFillImage.fillAmount = Mathf.Clamp01(p01);
    }

    private void Complete()
    {
        _done = true;

        if (SoulCounter.Instance != null)
        {
            SoulCounter.Instance.AddSouls(Mathf.Max(0, soulsReward));
            SoulCounter.Instance.RefreshUI();

            SoulPopup.Create(transform.position, soulsReward, SoulPopup.PopupType.Souls);
        }
        else
        {
            Debug.LogWarning("[SkullPickup] SoulCounter.Instance is null — souls not added.");
        }

        Destroy(gameObject);
    }
}
