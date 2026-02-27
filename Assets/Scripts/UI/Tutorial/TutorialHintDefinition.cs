using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialHint", menuName = "UI/Tutorial Hint Definition")]
public class TutorialHintDefinition : ScriptableObject
{
    [Serializable]
    public class HintLine
    {
        [TextArea(1, 4)] public string textEn;
        [TextArea(1, 4)] public string textRu;
        public Sprite icon;

        public string ResolveText()
        {
            bool isRu = Application.systemLanguage == SystemLanguage.Russian;
            if (isRu && !string.IsNullOrWhiteSpace(textRu))
                return textRu;
            return string.IsNullOrWhiteSpace(textEn) ? string.Empty : textEn;
        }
    }

    [Header("Identity")]
    public string hintId = "hint";

    [Header("Title")]
    public string titleEn = "Hint";
    public string titleRu = "Подсказка";

    [Header("Lines")]
    public HintLine[] lines = Array.Empty<HintLine>();

    public string ResolveTitle()
    {
        bool isRu = Application.systemLanguage == SystemLanguage.Russian;
        if (isRu && !string.IsNullOrWhiteSpace(titleRu))
            return titleRu;
        return string.IsNullOrWhiteSpace(titleEn) ? "Hint" : titleEn;
    }
}
