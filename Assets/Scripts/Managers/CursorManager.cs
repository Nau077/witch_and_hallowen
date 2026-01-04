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

    [Header("Stability")]
    [Tooltip("Сколько секунд держать боевой курсор после попадания в боевую зону (страховка от мерцаний).")]
    public float combatStickySeconds = 0.08f;

    [Tooltip("Сколько кадров подряд курсор должен быть ВНЕ боевой зоны, чтобы выйти из Combat. 3–6 обычно идеально.")]
    public int combatExitFrames = 5;

    [Header("Debug")]
    public bool debug = false;

    // Scene refs (не показываем в инспекторе)
    RectTransform _combatZoneRect;
    RectTransform _uiOverrideRect;

    // Камера, которой проверяем WorldSpace canvas (фикс для WorldSpace)
    Camera _combatUICam;

    enum Mode { UI, Combat }
    Mode _currentMode = (Mode)(-1);
    bool _currentActive;

    float _combatStickyUntil = 0f;
    int _combatExitCounter = 0;

    // Debug state
    Mode _lastDbgMode = (Mode)(-1);
    bool _lastDbgActive;
    bool _lastDbgInsideCombat;
    bool _lastDbgPopup;

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
        // в меню сразу ставим UI (чтобы не стартовать с Combat из прошлой сцены)
        if (theme != null && SceneManager.GetActiveScene().name == theme.menuSceneName)
            ApplyCursor(Mode.UI, false);
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Смена сцены: не переносим блокировку попапа
        popupBlocking = false;

        RefreshSceneRefs();
        ForceRefresh();

        // На входе в меню — всегда UI
        if (theme != null && s.name == theme.menuSceneName)
            ApplyCursor(Mode.UI, false);
    }

    void ForceRefresh()
    {
        _currentMode = (Mode)(-1);
        _currentActive = false;

        _combatStickyUntil = 0f;
        _combatExitCounter = 0;

        _lastDbgMode = (Mode)(-1);
        _lastDbgActive = false;
        _lastDbgInsideCombat = false;
        _lastDbgPopup = false;
    }

    void RefreshSceneRefs()
    {
        _combatZoneRect = FindRectInScene(combatZoneName);
        _uiOverrideRect = string.IsNullOrEmpty(uiOverrideName) ? null : FindRectInScene(uiOverrideName);

        // Камеру берем из Canvas, где лежит CombatCursorZone (или Main Camera как fallback)
        _combatUICam = null;
        if (_combatZoneRect != null)
        {
            var canvas = _combatZoneRect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _combatUICam = canvas.worldCamera;

            if (_combatUICam == null)
                _combatUICam = Camera.main;
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
        return null;
    }

    public void ApplyTheme(CursorTheme t)
    {
        theme = t;
        RefreshSceneRefs();
        ForceRefresh();

        // В меню — строго UI
        if (theme != null && SceneManager.GetActiveScene().name == theme.menuSceneName)
            ApplyCursor(Mode.UI, false);
        else
            ApplyCursor(Mode.UI, false);
    }

    public void SetPopupBlocking(bool blocked)
    {
        popupBlocking = blocked;

        if (theme == null) return;

        // меню — всегда UI
        if (SceneManager.GetActiveScene().name == theme.menuSceneName)
        {
            ApplyCursor(Mode.UI, false);
            return;
        }

        bool active = Input.GetMouseButton(0);

        if (blocked)
        {
            // попап открылся -> принудительно UI
            ApplyCursor(Mode.UI, active);
            return;
        }

        // попап закрылся -> пересчёт по позиции мыши прямо сейчас
        if (_combatZoneRect != null)
        {
            bool insideCombat = IsPointerInsideRect(_combatZoneRect, _combatUICam);
            if (insideCombat)
            {
                _combatExitCounter = Mathf.Max(1, combatExitFrames);
                _combatStickyUntil = Time.unscaledTime + combatStickySeconds;
                ApplyCursor(Mode.Combat, active);
                return;
            }
        }

        ApplyCursor(Mode.UI, active);
    }

    void Update()
    {
        if (theme == null) return;

        bool active = Input.GetMouseButton(0);
        string sceneName = SceneManager.GetActiveScene().name;

        // Меню — всегда UI (и никаких Combat даже если что-то мигнет)
        if (sceneName == theme.menuSceneName)
        {
            ApplyCursor(Mode.UI, active);
            DebugModeChange(Mode.UI, active, insideCombat: false);
            return;
        }

        // Попап — всегда UI
        if (popupBlocking)
        {
            ApplyCursor(Mode.UI, active);
            DebugModeChange(Mode.UI, active, insideCombat: false);
            return;
        }

        // Если нет боевой зоны — UI
        if (_combatZoneRect == null)
        {
            ApplyCursor(Mode.UI, active);
            DebugModeChange(Mode.UI, active, insideCombat: false);
            return;
        }

        // UI override — всегда UI (если задан и курсор там)
        if (_uiOverrideRect != null && IsPointerInsideRect(_uiOverrideRect, _combatUICam))
        {
            ApplyCursor(Mode.UI, active);
            DebugModeChange(Mode.UI, active, insideCombat: false);
            return;
        }

        // --- КЛЮЧЕВОЙ ФИКС: debounce выхода из Combat ---
        bool insideCombatRaw = IsPointerInsideRect(_combatZoneRect, _combatUICam);

        // Внутри Combat — сразу Combat, сбрасываем счетчик выхода
        if (insideCombatRaw)
        {
            _combatExitCounter = Mathf.Max(1, combatExitFrames);
            _combatStickyUntil = Time.unscaledTime + combatStickySeconds;

            ApplyCursor(Mode.Combat, active);
            DebugModeChange(Mode.Combat, active, insideCombat: true);
            return;
        }

        // Снаружи Combat — держим Combat N кадров, чтобы игнорировать 1-кадровые провалы
        if (_combatExitCounter > 0)
        {
            _combatExitCounter--;

            ApplyCursor(Mode.Combat, active);
            DebugModeChange(Mode.Combat, active, insideCombat: false);
            return;
        }

        // Доп. страховка по времени (можно оставить)
        if (Time.unscaledTime < _combatStickyUntil)
        {
            ApplyCursor(Mode.Combat, active);
            DebugModeChange(Mode.Combat, active, insideCombat: false);
            return;
        }

        // Иначе UI
        ApplyCursor(Mode.UI, active);
        DebugModeChange(Mode.UI, active, insideCombat: false);
    }

    bool IsPointerInsideRect(RectTransform rect, Camera uiCam)
    {
        if (rect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, uiCam);
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

        // полный Texture2D
        if (sprite.rect.width == sprite.texture.width &&
            sprite.rect.height == sprite.texture.height)
            return sprite.texture;

        // вырезаем из атласа (нужен Read/Write Enabled)
        Rect r = sprite.rect;
        var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);

        Color[] pixels = sprite.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Логи только при изменении режима/active/popup/insideCombat — не спамит
    void DebugModeChange(Mode mode, bool active, bool insideCombat)
    {
        if (!debug) return;

        bool changed =
            mode != _lastDbgMode ||
            active != _lastDbgActive ||
            popupBlocking != _lastDbgPopup ||
            insideCombat != _lastDbgInsideCombat;

        if (!changed) return;

        _lastDbgMode = mode;
        _lastDbgActive = active;
        _lastDbgPopup = popupBlocking;
        _lastDbgInsideCombat = insideCombat;

        float stickyLeft = _combatStickyUntil - Time.unscaledTime;

        Debug.Log($"[CursorManager][MODE] scene={SceneManager.GetActiveScene().name} " +
                  $"mode={mode} active={active} popupBlocking={popupBlocking} insideCombat={insideCombat} " +
                  $"exitCounter={_combatExitCounter} stickyLeft={stickyLeft:0.000}");
    }
}
