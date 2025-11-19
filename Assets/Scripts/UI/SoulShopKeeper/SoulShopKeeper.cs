using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SoulShopKeeper : MonoBehaviour
{
    [Tooltip("Ссылка на скрипт попапа SoulShopKeeperPopup.")]
    public SoulShopKeeperPopup popup;

    private void OnMouseDown()
    {
        if (popup != null)
        {
            popup.Show();
        }
        else
        {
            Debug.LogWarning("SoulShopKeeper: popup не назначен в инспекторе.");
        }
    }
}
