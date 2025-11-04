using UnityEngine;

public enum ElementId { Fire = 0, Ice = 1, Earth = 2 }

[CreateAssetMenu(menuName = "Combat/Element", fileName = "ELEM_New")]
public class ElementDefinition : ScriptableObject
{
    public ElementId id;
    public string displayName;
    public Sprite elementIcon;              // иконка для кнопки выбора

    [Header("Skills")]
    public SkillDefinition basicSkill;      // базовый выстрел
    public SkillDefinition skill1;
    public SkillDefinition skill2;
    public SkillDefinition skill3;
    public SkillDefinition skill4;

    [Header("UI")]
    public Sprite cooldownOverlaySprite;    // спрайт сектора КД (огонь/лёд и т.п.)
}
