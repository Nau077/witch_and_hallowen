using UnityEngine;

public class CursorZoneToggle : MonoBehaviour
{
    public GameObject combatCursorZone; // CombatCursorZone (UI Image)
    public GameObject vendorUIZone;     // опционально

    public void SetShopOpen(bool open)
    {
        // Когда магазин открыт — боевой слой выключаем
        if (combatCursorZone) combatCursorZone.SetActive(!open);

        // Продавец можно оставить включенным, но если хочешь — тоже можно включать/выключать
        if (vendorUIZone) vendorUIZone.SetActive(true);
    }
}
