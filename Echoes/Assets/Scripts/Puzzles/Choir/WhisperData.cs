using UnityEngine;

public enum WhisperType { Clue, Distraction }

[System.Serializable]
public struct Whisper
{
    [Tooltip("Apenas para organização no Inspector.")]
    public string description;
    
    [Tooltip("O texto do sussurro que será logado no console.")]
    [TextArea] 
    public string whisperText; 
    
    public WhisperType type;
}