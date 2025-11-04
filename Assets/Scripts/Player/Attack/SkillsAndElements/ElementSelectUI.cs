
using UnityEngine;
using UnityEngine.UI;
// простая панель с двумя кнопками (или кликабельные иконки).
public class ElementSelectUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerFireballShooter shooter;
    public ChargeDotsCooldownUI cooldownUi;

    [Header("Buttons/Icons")]
    public Button fireButton;
    public Button iceButton;
    public Image fireIcon;   // необязательно, но удобно подсвечивать активное
    public Image iceIcon;

    [Header("Data")]
    public ElementDefinition fireElement;
    public ElementDefinition iceElement;

    private void Awake()
    {
        if (fireButton) fireButton.onClick.AddListener(() => Select(fireElement));
        if (iceButton) iceButton.onClick.AddListener(() => Select(iceElement));
    }

    private void Start()
    {
        // стартовый элемент
        if (shooter && shooter.currentElement == null && fireElement != null)
            Select(fireElement);
        else
            Select(shooter.currentElement);
    }

    public void Select(ElementDefinition elem)
    {
        if (shooter == null || elem == null) return;
        shooter.SetElement(elem, cooldownUi);

        // лёгкая индикация выбранного
        if (fireIcon) fireIcon.color = (elem == fireElement) ? Color.white : new Color(1, 1, 1, 0.5f);
        if (iceIcon) iceIcon.color = (elem == iceElement) ? Color.white : new Color(1, 1, 1, 0.5f);
    }
}
