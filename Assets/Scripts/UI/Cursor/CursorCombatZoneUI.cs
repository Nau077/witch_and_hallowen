using UnityEngine;
using UnityEngine.EventSystems;

public class CursorCombatZoneUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static bool IsMouseOver { get; private set; }

    void OnDisable()
    {
        // если выключили зону (например, открыли магазин) — считаем что мышь не над боем
        IsMouseOver = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsMouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsMouseOver = false;
    }
}
