using UnityEngine;

public enum HorrorEventType
{
    VisualFlash,
    FalseAlarmClock
}

[System.Serializable]
public struct HorrorEvent
{
    [Tooltip("Nome do evento para organização.")]
    public string eventName;

    [Tooltip("O tipo de evento que será disparado.")]
    public HorrorEventType type;

    [Tooltip("O evento só pode ocorrer se a Sanidade do jogador for MENOR que este valor.")]
    [Range(0f, 1f)] public float maxSanityThreshold;

    // Parâmetros do VisualFlash
    [Tooltip("Duração total da animação de piscar.")]
    public float visualFlashDuration;
    [Tooltip("O pico de insanidade visual a ser atingido (geralmente 1.0).")]
    [Range(0f, 1f)] public float visualFlashPeak;

    // Parâmetros do FalseAlarmClock
    [Tooltip("Duração do alarme falso em segundos.")]
    public float falseAlarmDuration;
}