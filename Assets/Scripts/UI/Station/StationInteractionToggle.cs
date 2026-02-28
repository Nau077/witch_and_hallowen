using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class StationInteractionToggle : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float interactionDistance = 4f;
    [SerializeField] private bool bypassDistanceCheck = true;

    [Header("Scene References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera stationCamera;
    [SerializeField] private GameObject mainGameRoot;
    [SerializeField] private GameObject soulKeeperObject;

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Hover Tooltip")]
    [SerializeField] private bool enableHoverTooltip = true;
    [SerializeField] private string tooltipTitleEn = "Station";
    [SerializeField] private string tooltipDescriptionEn = "Farm station. First, you need to find the keeper...";
    [SerializeField, Min(0f)] private float tooltipDelay = 0.2f;
    [SerializeField, Min(0f)] private float mouseHitPaddingWorld = 0.25f;
    [SerializeField] private bool logDistanceBlock = true;

    private HoverTooltipTrigger _hoverTooltipTrigger;
    private Coroutine _transitionRoutine;
    private CanvasGroup _fadeGroup;
    private Image _fadeImage;
    private bool _stationViewActive;
    private bool _hoverActive;
    private int _lastHandledClickFrame = -1;
    private Collider2D _ownCollider;
    private SpriteRenderer _ownSpriteRenderer;

    private void Awake()
    {
        AutoResolveReferences();
        _ownCollider = GetComponent<Collider2D>();
        _ownSpriteRenderer = GetComponent<SpriteRenderer>();

        if (!enableHoverTooltip)
            return;

        _hoverTooltipTrigger = GetComponent<HoverTooltipTrigger>();
        if (_hoverTooltipTrigger == null)
            _hoverTooltipTrigger = gameObject.AddComponent<HoverTooltipTrigger>();

        _hoverTooltipTrigger.Bind(BuildHoverTooltipData, tooltipDelay);
    }

    private void Update()
    {
        bool over = IsMouseOverStation();
        if (over && !_hoverActive)
            EnterHover();
        else if (!over && _hoverActive)
            ExitHover();

        if (over && Input.GetMouseButtonDown(0))
            HandleStationClick();
    }

    private void OnMouseDown()
    {
        HandleStationClick();
    }

    private void OnMouseEnter()
    {
        EnterHover();
    }

    private void OnMouseExit()
    {
        ExitHover();
    }

    private void OnDisable()
    {
        ExitHover();
    }

    private IEnumerator TransitionRoutine(bool toStationView)
    {
        EnsureFadeOverlay();
        yield return Fade(0f, 1f, transitionDuration * 0.5f);

        ApplyStationViewState(toStationView);
        _stationViewActive = toStationView;

        yield return Fade(1f, 0f, transitionDuration * 0.5f);
        _transitionRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (_fadeGroup == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);
        _fadeImage.raycastTarget = true;

        float elapsed = 0f;
        _fadeGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _fadeGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        _fadeGroup.alpha = to;
        _fadeImage.raycastTarget = to > 0.01f;
    }

    private void ApplyStationViewState(bool stationViewActive)
    {
        if (mainCamera != null)
            SetCameraActive(mainCamera, !stationViewActive);
        else
            Debug.LogWarning("[StationInteractionToggle] Main Camera is not assigned.");

        if (stationCamera != null)
            SetCameraActive(stationCamera, stationViewActive);
        else
            Debug.LogWarning("[StationInteractionToggle] Main Camera_2 is not assigned.");

        if (mainGameRoot != null)
            mainGameRoot.SetActive(!stationViewActive);

        if (soulKeeperObject != null)
            soulKeeperObject.SetActive(!stationViewActive);
    }

    private bool IsPlayerCloseEnough()
    {
        if (player == null)
        {
            Debug.LogWarning("[StationInteractionToggle] Player is not assigned. Click is blocked.");
            return false;
        }

        float distance = Vector2.Distance(player.position, transform.position);
        return distance <= interactionDistance;
    }

    private void AutoResolveReferences()
    {
        if (player == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                player = playerGo.transform;
        }

        if (mainCamera == null)
        {
            GameObject cameraGo = GameObject.Find("Main Camera");
            if (cameraGo != null)
                mainCamera = cameraGo.GetComponent<Camera>();
        }

        if (stationCamera == null)
        {
            GameObject cameraGo = GameObject.Find("Main Camera_2");
            if (cameraGo != null)
                stationCamera = cameraGo.GetComponent<Camera>();
        }
    }

    private void EnsureFadeOverlay()
    {
        if (_fadeGroup != null && _fadeImage != null)
            return;

        GameObject canvasGO = new GameObject("StationTransitionFadeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 2;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rt = imageGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _fadeImage = imageGO.AddComponent<Image>();
        _fadeImage.color = fadeColor;
        _fadeImage.raycastTarget = false;

        _fadeGroup = imageGO.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
    }

    private HoverTooltipData BuildHoverTooltipData()
    {
        return new HoverTooltipData
        {
            title = tooltipTitleEn,
            levelLine = string.Empty,
            priceLine = string.Empty,
            description = tooltipDescriptionEn
        };
    }

    private void HandleStationClick()
    {
        if (_lastHandledClickFrame == Time.frameCount)
            return;
        _lastHandledClickFrame = Time.frameCount;

        var shooter = FindObjectOfType<PlayerSkillShooter>();
        if (shooter != null)
            shooter.SkipNextClickFromUI();

        AutoResolveReferences();

        bool toStationView = !_stationViewActive;
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(TransitionRoutine(toStationView));
    }

    private void EnterHover()
    {
        _hoverActive = true;
        if (!enableHoverTooltip || _hoverTooltipTrigger == null)
            return;

        var tooltipUi = HoverTooltipUI.Instance;
        if (tooltipUi != null)
            tooltipUi.ShowFrom(_hoverTooltipTrigger);
    }

    private void ExitHover()
    {
        _hoverActive = false;
        if (!enableHoverTooltip || _hoverTooltipTrigger == null)
            return;

        if (!HoverTooltipUI.HasInstance)
            return;

        var tooltipUi = HoverTooltipUI.Instance;
        if (tooltipUi != null)
            tooltipUi.HideFrom(_hoverTooltipTrigger);
    }

    private bool IsMouseOverStation()
    {
        if (!TryGetInputCamera(out var cam))
            return false;

        if (_ownCollider != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, 1000f);
            if (hit.collider == _ownCollider)
                return true;
        }

        Vector3 mp = Input.mousePosition;
        float z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, z));

        if (_ownCollider != null && _ownCollider.OverlapPoint(world))
            return true;

        if (_ownSpriteRenderer != null)
        {
            Bounds b = _ownSpriteRenderer.bounds;
            b.Expand(new Vector3(mouseHitPaddingWorld, mouseHitPaddingWorld, 0f));
            return b.Contains(world);
        }

        return false;
    }

    private bool TryGetInputCamera(out Camera cam)
    {
        if (_stationViewActive && stationCamera != null && stationCamera.isActiveAndEnabled)
        {
            cam = stationCamera;
            return true;
        }

        if (!_stationViewActive && mainCamera != null && mainCamera.isActiveAndEnabled)
        {
            cam = mainCamera;
            return true;
        }

        var all = Camera.allCameras;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].isActiveAndEnabled)
            {
                cam = all[i];
                return true;
            }
        }

        cam = null;
        return false;
    }

    private void SetCameraActive(Camera cam, bool active)
    {
        if (cam == null)
            return;

        if (active)
        {
            if (!cam.gameObject.activeSelf)
                cam.gameObject.SetActive(true);
            cam.enabled = true;
            return;
        }

        cam.enabled = false;
        if (cam.gameObject.activeSelf)
            cam.gameObject.SetActive(false);
    }
}
