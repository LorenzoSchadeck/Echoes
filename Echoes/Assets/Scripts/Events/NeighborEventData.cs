using FMODUnity;
using UnityEngine;
using System.Collections.Generic;

public enum NeighborEventType
{
    RotationWithBoxesAndAudio,    // Rotação de objeto + caixas + áudio
    SoundWithRotation,            // Som aleatório + rotação
    JumpScare,                    // Susto (objeto habilitado por 1s)
    AudioOnly                     // Evento exclusivo de som
}

[System.Serializable]
public struct NeighborEvent
{
    [Tooltip("Nome do evento para organização.")]
    public string eventName;

    [Tooltip("O tipo de evento que será disparado.")]
    public NeighborEventType type;

    // --- Parâmetros para RotationWithBoxesAndAudio ---
    [Tooltip("Objetos que devem rotacionar (ex: porta).")]
    public List<GameObject> objectsToRotate;
    [Tooltip("Rotação a ser aplicada nos objetos (em graus).")]
    public Vector3 rotationAmount;
    [Tooltip("Duração da rotação em segundos.")]
    public float rotationDuration;
    [Tooltip("Caixas que devem ser habilitadas (simulando mudança).")]
    public List<GameObject> boxesToEnable;
    [Tooltip("Lista de eventos FMOD de sons de mudança.")]
    public List<EventReference> movingSounds;
    [Tooltip("GameObject onde os sons serão tocados.")]
    public GameObject audioTarget;

    // --- Parâmetros para SoundWithRotation ---
    [Tooltip("Lista de eventos FMOD para seleção aleatória.")]
    public List<EventReference> randomSounds;
    [Tooltip("GameObject onde o som será tocado.")]
    public GameObject soundTarget;
    [Tooltip("Objetos que devem rotacionar junto com o som.")]
    public List<GameObject> rotationObjects;
    [Tooltip("Rotação a ser aplicada.")]
    public Vector3 soundRotationAmount;
    [Tooltip("Duração da rotação.")]
    public float soundRotationDuration;

    // --- Parâmetros para JumpScare ---
    [Tooltip("Objeto que será habilitado por 1 segundo.")]
    public GameObject jumpScareObject;
    [Tooltip("Som de susto (opcional).")]
    public EventReference jumpScareSound;
    [Tooltip("GameObject onde o som de susto será tocado.")]
    public GameObject jumpScareSoundTarget;

    // --- Parâmetros para AudioOnly ---
    [Tooltip("Lista de eventos FMOD exclusivos de som.")]
    public List<EventReference> audioOnlyEvents;
    [Tooltip("GameObject onde os sons serão tocados.")]
    public GameObject audioOnlyTarget;
    [Tooltip("Se deve tocar múltiplos sons simultaneamente.")]
    public bool playMultipleSounds;
    [Tooltip("Delay entre sons (se playMultipleSounds for true).")]
    public float soundDelay;
}