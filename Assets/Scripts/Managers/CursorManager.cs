using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Theme")]
    public CursorTheme theme;

    [Header("Scene object names (auto-find)")]
    [Tooltip("RectTransform боевой зоны. В Level_1 создай UI->Image (или пустой RectTransform) с таким именем.")]
    public string combatZoneName = "CombatCursorZone";

    [Tooltip("RectTransform зоны, где курсор должен быть UI (например продавец/база). Можно оставить пустым.")]
    public string uiOverrideName = "";

    [Header("Popup blocks")]
    [Tooltip("Если true — курсор всегда UI (например открыт попап/магазин).")]
    public bool popupBlocking = false;

    [Header("Debug")]
    public bool debug = true;

    // Эти ссылки НЕ показываем в инспекторе (чтобы в MainMenu не бесили)
    RectTransform _combatZoneRect;
    RectTransform _uiOverrideRect;

    enum Mode { UI, Combat }
    Mode _currentMode = (Mode)(-1);
    bool _currentActive;

    Canvas _canvas;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        RefreshSceneRefs();
        ForceRefresh();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // чтобы не было "залипания" блокировки после смен сцен
        popupBlocking = false;

        RefreshSceneRefs();
        ForceRefresh();
    }

    void RefreshSceneRefs()
    {
        // Canvas ищем заново в каждой сцене
        _canvas = FindFirstObjectByType<Canvas>();

        // Rect'ы валидны только в пределах сцены → ищем заново
        _combatZoneRect = FindRectInScene(combatZoneName);
        _uiOverrideRect = string.IsNullOrEmpty(uiOverrideName) ? null : FindRectInScene(uiOverrideName);

        if (debug)
        {
            string scene = SceneManager.GetActiveScene().name;
            string canvasName = _canvas ? _canvas.name : "NULL";
            string combatName = _combatZoneRect ? _combatZoneRect.name : "NULL";
            string combatCanvas = _combatZoneRect ? (_combatZoneRect.GetComponentInParent<Canvas>()?.name ?? "NULL") : "NULL";

            Debug.Log($"[CursorManager] RefreshSceneRefs scene={scene} canvas={canvasName} combatRect={combatName} combatParentCanvas={combatCanvas}");
        }
    }

    RectTransform FindRectInScene(string objName)
    {
        if (string.IsNullOrEmpty(objName)) return null;

        var all = Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == objName)
                return all[i];
        }

        if (debug)
            Debug.LogWarning($"[CursorManager] FindRectInScene: '{objName}' NOT FOUND in scene '{SceneManager.GetActiveScene().name}'");

        return null;
    }

    public void ApplyTheme(CursorTheme t)
    {
        theme = t;
        RefreshSceneRefs();
        ForceRefresh();
        ApplyCursor(Mode.UI, false);
    }

    public void SetPopupBlocking(bool blocked)
    {
        popupBlocking = blocked;

        // сразу принудительно ставим UI курсор (не ждём Update)
        if (theme != null)
        {
            bool active = Input.GetMouseButton(0);
            ApplyCursor(Mode.UI, active);
        }
    }

    void ForceRefresh()
    {
        _currentMode = (Mode)(-1);
        _currentActive = false;
    }

    void Update()
    {
        if (theme == null) return;

        bool active = Input.GetMouseButton(0);

        // раз в секунду печатаем состояние (не спамим)
        if (debug && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[CursorManager] tick scene={SceneManager.GetActiveScene().name} popupBlocking={popupBlocking} combatRectIsNull={(_combatZoneRect == null)}");
        }

        // Меню — всегда UI
        if (SceneManager.GetActiveScene().name == theme.menuSceneName)
        {
            ApplyCursor(Mode.UI, active);
            return;
        }

        // Открыт попап — всегда UI
        if (popupBlocking)
        {
            ApplyCursor(Mode.UI, active);
            return;
        }

        // Если нет боевой зоны в сцене — по умолчанию UI
        if (_combatZoneRect == null)
        {
            ApplyCursor(Mode.UI, active);
            return;
        }

        // UI override — всегда UI (если задан)
        if (_uiOverrideRect != null && IsPointerInsideRect(_uiOverrideRect, isCombat: false))
        {
            ApplyCursor(Mode.UI, active);
            return;
        }

        // Боевая зона — Combat
        if (IsPointerInsideRect(_combatZoneRect, isCombat: true))
        {
            ApplyCursor(Mode.Combat, active);
            return;
        }

        // Иначе UI
        ApplyCursor(Mode.UI, active);
    }

    bool IsPointerInsideRect(RectTransform rect, bool isCombat)
    {
        if (rect == null) return false;

        // Берём canvas конкретно этого rect'а (не общий _canvas)
        var canvasForRect = rect.GetComponentInParent<Canvas>();

        Camera uiCam = null;

        // Если Canvas не Overlay — пробуем взять worldCamera.
        if (canvasForRect != null && canvasForRect.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCam = canvasForRect.worldCamera;

            // ✅ КЛЮЧ: если worldCamera не назначена, используем Camera.main
            if (uiCam == null) uiCam = Camera.main;
        }

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, uiCam);

        if (debug && isCombat && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[CursorManager] contains? rect={rect.name} inside={inside} canvasMode={canvasForRect?.renderMode} " +
                      $"uiCam={(uiCam ? uiCam.name : "NULL")} mouse={Input.mousePosition}");
        }

        return inside;
    }


    void ApplyCursor(Mode mode, bool active)
    {
        if (mode == _currentMode && active == _currentActive) return;

        _currentMode = mode;
        _currentActive = active;

        Sprite sprite;
        Vector2 hotspot;

        if (mode == Mode.UI)
        {
            sprite = active ? theme.uiActive : theme.uiIdle;
            hotspot = theme.uiHotspot;
        }
        else
        {
            sprite = active ? theme.combatActive : theme.combatIdle;
            hotspot = theme.combatHotspot;
        }

        if (sprite == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Cursor.SetCursor(SpriteToTexture(sprite), hotspot, CursorMode.Auto);
    }

    Texture2D SpriteToTexture(Sprite sprite)
    {
        if (sprite == null) return null;

        // если это полный Texture2D — можно вернуть напрямую (без GetPixels)
        if (sprite.rect.width == sprite.texture.width &&
            sprite.rect.height == sprite.texture.height)
            return sprite.texture;

        // иначе вырезаем из атласа (нужен Read/Write Enabled у текстуры!)
        Rect r = sprite.rect;
        var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);

        Color[] pixels = sprite.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
