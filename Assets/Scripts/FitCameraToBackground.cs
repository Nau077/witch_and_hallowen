using UnityEngine;

[ExecuteAlways]
public class FitCameraToBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Header("Editor Options")]
    [SerializeField] private bool adjustInEditor = true;  // только для подгонки в редакторе
    [SerializeField] private bool autoCenterCamera = true; // выравнивать камеру по центру фона

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && adjustInEditor)
        {
            FitOnce();
        }
    }
#endif

    private void FitOnce()
    {
        if (targetCamera == null || backgroundRenderer == null)
            return;

        float backgroundHeight = backgroundRenderer.bounds.size.y;
        float backgroundWidth = backgroundRenderer.bounds.size.x;

        float screenAspect = (float)Screen.width / Screen.height;
        float targetAspect = backgroundWidth / backgroundHeight;

        float orthoSize = backgroundHeight / 2f;
        if (screenAspect < targetAspect)
        {
            orthoSize *= targetAspect / screenAspect;
        }

        // Применяем ТОЛЬКО в редакторе, не во время Play
        targetCamera.orthographicSize = orthoSize;

        if (autoCenterCamera)
        {
            Vector3 bgPos = backgroundRenderer.transform.position;
            targetCamera.transform.position = new Vector3(bgPos.x, bgPos.y, -10f);
        }
    }
}
