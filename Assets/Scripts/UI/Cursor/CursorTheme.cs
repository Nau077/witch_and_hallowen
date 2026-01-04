using UnityEngine;

[CreateAssetMenu(menuName = "Game/Cursor Theme", fileName = "CursorTheme")]
public class CursorTheme : ScriptableObject
{
    [Header("UI (Blue)")]
    public Sprite uiIdle;
    public Sprite uiActive;

    [Header("Combat (Fire)")]
    public Sprite combatIdle;
    public Sprite combatActive;

    [Header("Hotspots")]
    public Vector2 uiHotspot = Vector2.zero;
    public Vector2 combatHotspot = Vector2.zero;

    [Header("Combat zones")]
    public LayerMask combatZoneMask;

    [Header("Menu scene")]
    public string menuSceneName = "MainMenu";
}
