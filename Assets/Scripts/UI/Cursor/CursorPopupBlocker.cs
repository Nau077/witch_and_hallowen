using UnityEngine;

public class CursorPopupBlocker : MonoBehaviour
{
    void OnEnable()
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.SetPopupBlocking(true);
    }

    void OnDisable()
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.SetPopupBlocking(false);
    }
}
