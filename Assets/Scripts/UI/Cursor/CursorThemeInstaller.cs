using UnityEngine;

public class CursorThemeInstaller : MonoBehaviour
{
    public CursorTheme theme;

    void Start()
    {
        if (theme == null) return;
        if (CursorManager.Instance == null) return;

        CursorManager.Instance.ApplyTheme(theme);
    }
}
