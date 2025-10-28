using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FitCameraToBackground2D : MonoBehaviour
{
    [Header("Refs")]
    public Camera targetCamera;
    public SpriteRenderer backgroundRenderer;

    [Header("Mode")]
    public bool fitInPlay = true;
    public bool adjustInEditor = true;
    public bool autoCenterCamera = true;
    public bool fitWholeBackground = true;

    private int _lastW, _lastH;
    private Vector3 _lastBgPos;
    private Vector3 _lastBgSize;

    void OnEnable()
    {
        if (!targetCamera) targetCamera = GetComponent<Camera>();
        FitOnce();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && adjustInEditor)
        {
            FitOnce();
            return;
        }
#endif
        if (Application.isPlaying && fitInPlay)
        {
            int w = Mathf.Max(1, targetCamera ? targetCamera.pixelWidth : Screen.width);
            int h = Mathf.Max(1, targetCamera ? targetCamera.pixelHeight : Screen.height);

            var bgBounds = backgroundRenderer ? backgroundRenderer.bounds : new Bounds();
            var bgSize = bgBounds.size;
            var bgPos = backgroundRenderer ? backgroundRenderer.transform.position : Vector3.zero;

            if (w != _lastW || h != _lastH || bgSize != _lastBgSize || bgPos != _lastBgPos)
                FitOnce();
        }
    }

    private void FitOnce()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera || !targetCamera.orthographic || !backgroundRenderer) return;

        Bounds b = backgroundRenderer.bounds;
        float bgW = b.size.x;
        float bgH = b.size.y;

        float pixelW = Mathf.Max(1f, targetCamera.pixelWidth);
        float pixelH = Mathf.Max(1f, targetCamera.pixelHeight);
        float screenAspect = pixelW / pixelH;
        float targetAspect = bgW / bgH;

        float orthoSize = bgH * 0.5f;
        if (fitWholeBackground && screenAspect < targetAspect)
            orthoSize *= (targetAspect / screenAspect);

        targetCamera.orthographicSize = orthoSize;

        if (autoCenterCamera)
        {
            var p = b.center;
            targetCamera.transform.position = new Vector3(p.x, p.y, targetCamera.transform.position.z);
        }

        _lastW = (int)pixelW;
        _lastH = (int)pixelH;
        _lastBgPos = backgroundRenderer.transform.position;
        _lastBgSize = b.size;

        Debug.Log($"[FitCam] bg={bgW:F2}x{bgH:F2} screen={pixelW}x{pixelH} aspect={screenAspect:F3} size={orthoSize:F3}");
    }
}
