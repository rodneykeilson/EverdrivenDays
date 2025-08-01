using UnityEngine;

[System.Serializable]
public class DialogLine
{
    public string speaker;
    [TextArea] public string text;
    public AudioClip voiceClip;
    public bool lockPlayerMovement;
    public string playerResponse; // Leave empty for no response
}
