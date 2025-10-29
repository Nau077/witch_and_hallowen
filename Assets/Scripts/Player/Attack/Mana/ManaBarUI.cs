using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ManaBarUI : MonoBehaviour
{
    public PlayerMana mana;   // перетащи Player
    public Image fill;        // перетащи синий Image (внутренний)

    private void Reset()
    {
        if (mana == null) mana = FindObjectOfType<PlayerMana>();
        if (fill == null) fill = GetComponentInChildren<Image>();
    }

    private void Update()
    {
        if (mana == null || fill == null) return;
        if (fill.type != Image.Type.Filled) fill.type = Image.Type.Filled;
        if (fill.fillMethod != Image.FillMethod.Horizontal) fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = mana.Normalized; // 0..1
    }
}
