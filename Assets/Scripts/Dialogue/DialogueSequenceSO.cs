using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueSpeakerSide
{
    Left,   // Dialog_group_1
    Right   // Dialog_group_2
}

[CreateAssetMenu(menuName = "DeadWitchy/Dialogue/Dialogue Sequence", fileName = "DialogueSequence")]
public class DialogueSequenceSO : ScriptableObject
{
    [Serializable]
    public class Line
    {
        [Tooltip("Имя говорящего (для будущего, если захочешь показывать имя).")]
        public string speakerName;

        [Tooltip("Сторона: Left=Dialog_group_1, Right=Dialog_group_2.")]
        public DialogueSpeakerSide side = DialogueSpeakerSide.Left;

        [TextArea(2, 6)]
        public string text;

        [Tooltip("Портрет говорящего (можно пусто, тогда не меняем).")]
        public Sprite portraitOverride;
    }

    [Header("Lines")]
    public List<Line> lines = new List<Line>();

    public int Count => lines == null ? 0 : lines.Count;

    public Line GetLine(int index)
    {
        if (lines == null || index < 0 || index >= lines.Count) return null;
        return lines[index];
    }
}
