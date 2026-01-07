using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Theme")]
    public CursorTheme theme;

    [Header("Popup blocks")]
    [Tooltip("Если true — курсор всегда UI (например открыт попап/магазин).")]
    public bool popupBlocking = false;

    [Header("Debug")]
    public bool debug = false;

    private enum Mode { UI, Combat }

    private Mode _currentMode = (Mode)(-1);
    private bool _currentActive = false;

    // Кэш: Sprite -> Texture2D, чтобы не создавать Texture2D каждый кадр
    private readonly Dictionary<Sprite, Texture2D> _spriteTexCache = new Dictionary<Sprite, Texture2D>(32);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ForceRefresh();
        ApplyCursor(Mode.UI, false); // безопасно: внутри ApplyCursor есть защита от theme == null
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // При смене сцены блокировку попапом сбрасываем:
        // (если в какой-то сцене нужно блокировать — это включит CursorPopupBlocker/попап)
        popupBlocking = false;

        ForceRefresh();

        // Чтобы меню никогда не стартовало с боевым курсором:
        ApplyCursor(Mode.UI, false);
    }

    public void ApplyTheme(CursorTheme t)
    {
        theme = t;
        ForceRefresh();
        ApplyCursor(Mode.UI, false);
    }

    public void SetPopupBlocking(bool blocked)
    {
        popupBlocking = blocked;

        // сразу обновим курсор, не ждём Update
        ForceRefresh();
        ApplyCursor(Mode.UI, Input.GetMouseButton(0));
    }

    private void ForceRefresh()
    {
        _currentMode = (Mode)(-1);
        _currentActive = false;
    }

    private void Update()
    {
        if (theme == null) return;

        bool active = Input.GetMouseButton(0);

        // 1) Меню — ВСЕГДА UI
        string sceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(theme.menuSceneName) && sceneName == theme.menuSceneName)
        {
            ApplyCursor(Mode.UI, active);
            return;
        }

        // 2) Попап/магазин открыт — ВСЕГДА UI
        if (popupBlocking)
        {
            ApplyCursor(Mode.UI, active);
            return;
        }

        // 3) Если мышь над UI (кнопки/попапы/канвасы) — UI
        bool overUI = IsPointerOverUI();
        if (overUI)
        {
            ApplyCursor(Mode.UI, active);
            return;
        }

        // 4) Иначе — Combat
        ApplyCursor(Mode.Combat, active);
    }

    private bool IsPointerOverUI()
    {
        // Если EventSystem нет (например на очень раннем старте) — считаем что НЕ над UI
        if (EventSystem.current == null) return false;

        // Для мыши без параметров нормально работает
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplyCursor(Mode mode, bool active)
    {
        // Важно: theme может быть null во время загрузки/переходов
        if (theme == null) return;

        // Если ничего не изменилось — не трогаем курсор
        if (mode == _currentMode && active == _currentActive) return;

        // Debug: лог только когда реально меняем режим/актив
        if (debug)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool overUI = IsPointerOverUI();
            Debug.Log($"[CursorManager][MODE] scene={sceneName} mode={mode} active={active} popupBlocking={popupBlocking} overUI={overUI}");

            // “подозрительный” кейс: UI-режим, хотя попапа нет и над UI не стоим и это не меню
            if (mode == Mode.UI &&
                !popupBlocking &&
                !overUI &&
                (string.IsNullOrEmpty(theme.menuSceneName) || sceneName != theme.menuSceneName))
            {
                Debug.LogWarning("[CursorManager] UI cursor applied while not over UI and no popupBlocking. Check EventSystem/UI setup.");
            }
        }

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

        Texture2D tex = SpriteToTextureCached(sprite);
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }

    private Texture2D SpriteToTextureCached(Sprite sprite)
    {
        if (sprite == null) return null;

        if (_spriteTexCache.TryGetValue(sprite, out var cached) && cached != null)
            return cached;

        Texture2D tex = SpriteToTexture(sprite);
        _spriteTexCache[sprite] = tex;
        return tex;
    }

    private Texture2D SpriteToTexture(Sprite sprite)
    {
        if (sprite == null) return null;

        // Если это полный Texture2D — можно вернуть напрямую
        if (sprite.rect.width == sprite.texture.width &&
            sprite.rect.height == sprite.texture.height)
        {
            return sprite.texture;
        }

        // Иначе вырезаем из атласа (нужен Read/Write Enabled у текстуры!)
        Rect r = sprite.rect;

        var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        Color[] pixels = sprite.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
