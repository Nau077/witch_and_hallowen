using UnityEngine;
using TMPro;

public class SoulPopup : MonoBehaviour
{
    public TMP_Text text;
    public float riseSpeed = 60f;     // px/sec
    public float fadeSpeed = 2f;
    public Color soulColor = new Color(0.8f, 0.6f, 1f);
    public Color goldColor = new Color(1f, 0.85f, 0.3f);

    RectTransform rt;

    void Awake()
    {
        rt = transform as RectTransform;
        if (!text) text = GetComponentInChildren<TMP_Text>(true);
    }

    void Update()
    {
        if (rt != null) rt.anchoredPosition += Vector2.up * riseSpeed * Time.unscaledDeltaTime;

        var c = text.color;
        c.a -= fadeSpeed * Time.unscaledDeltaTime;
        text.color = c;

        if (c.a <= 0) Destroy(gameObject);
    }

    // старый вызов
    public static void Create(Vector3 worldPosition, int amount)
    {
        Create(worldPosition, amount, PopupType.Souls, 0, 0);
    }

    // новый вызов
    public static void Create(Vector3 worldPosition, int amount, PopupType type, int fromValue = 0, int toValue = 0)
    {
        var prefab = Resources.Load<SoulPopup>("SoulPopupPrefab");
        if (!prefab) return;

        // 1) найдём любой Canvas
        var canvas = Object.FindObjectOfType<Canvas>();
        if (!canvas)
        {
            Debug.LogWarning("[SoulPopup] Canvas not found in scene.");
            return;
        }

        // 2) создадим и приатачим
        var popup = Object.Instantiate(prefab, canvas.transform, false);

        // 3) переведём world -> canvas local
        var cam = Camera.main;
        Vector2 screen = cam ? (Vector2)UnityEngine.Camera.main.WorldToScreenPoint(worldPosition)
                             : RectTransformUtility.WorldToScreenPoint(null, worldPosition);

        RectTransform canvasRT = canvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out localPoint);
        (popup.transform as RectTransform).anchoredPosition = localPoint;

        popup.SetPopupText(amount, type, fromValue, toValue);
    }

    private void SetPopupText(int amount, PopupType type, int fromValue, int toValue)
    {
        switch (type)
        {
            case PopupType.Souls:
                text.color = soulColor;
                text.text = $"+{amount}";
                break;
            case PopupType.CursedGold:
                text.color = goldColor;
                text.text = (toValue > 0) ? $"{fromValue} → {toValue}" : $"+{amount}";
                break;
            default:
                text.text = $"+{amount}";
                break;
        }
    }

    public enum PopupType { Souls, CursedGold }
}
