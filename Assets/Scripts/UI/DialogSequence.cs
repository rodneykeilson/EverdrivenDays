using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogSequence", menuName = "Dialog/DialogSequence", order = 1)]
public class DialogSequence : ScriptableObject
{
    public List<DialogLineData> lines;
}

[System.Serializable]
public class DialogLineData
{
    public string speaker;
    [TextArea]
    public string text;
    public AudioClip voiceClip;
    public bool lockPlayerMovement;
}
