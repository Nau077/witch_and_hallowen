using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SoulShopKeeper : MonoBehaviour
{
    [Tooltip("Ссылка на скрипт попапа SoulShopKeeperPopup.")]
    public SoulShopKeeperPopup popup;

    private void OnMouseDown()
    {
        // Сначала скажем шутеру, что этот клик был «служебный»
        var shooter = FindObjectOfType<PlayerSkillShooter>();
        if (shooter != null)
        {
            shooter.SkipNextClickFromUI();
        }

        // Потом уже открываем попап
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
