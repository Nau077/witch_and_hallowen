using UnityEngine;

public class CursorThemeInstaller : MonoBehaviour
{
    public CursorTheme theme;

    private void Start()
    {
        if (CursorManager.Instance != null && theme != null)
            CursorManager.Instance.ApplyTheme(theme);
    }
}
