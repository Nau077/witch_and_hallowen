using UnityEngine;
using UnityEngine.EventSystems;

public class CursorUIOverrideZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static bool IsMouseOver { get; private set; }

    void OnDisable()
    {
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
