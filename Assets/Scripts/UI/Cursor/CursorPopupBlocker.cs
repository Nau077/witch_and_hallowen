using UnityEngine;

public class CursorPopupBlocker : MonoBehaviour
{
    private void OnEnable()
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.SetPopupBlocking(true);
    }

    private void OnDisable()
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.SetPopupBlocking(false);
    }
}
