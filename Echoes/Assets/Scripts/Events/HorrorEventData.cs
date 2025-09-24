using FMODUnity;
using UnityEngine;
using UnityEngine.Video;

public enum HorrorEventType
{
    // Limiar 2: Ansiedade
    PlaySpatialSound,      // Sons de Presença
    RadioStaticBurst,      // Estática Fantasma
    QuickLightChange,      // Luzes Piscando

    // Limiar 3: Angústia
    VisualFlash,           // Piscar de Insanidade
    FalseAlarmClock,       // Alarme Falso
    TemporaryMaterialSwap, // Fotos/Documentos Corrompidos
    PlayVideoOnMaterial,   // Olhos nos Quadros

    // Limiar 4: Colapso
    SpawnCoveredBody,      // Corpo na Mesa
    SpawnHallucination,    // Alucinação Manifestada
    GuiltChorusBurst       // Coro da Culpa - Raro
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

    // --- Parâmetros Limiar 2 ---
    // PlaySpatialSound
    [Tooltip("Evento FMOD a ser tocado no evento PlaySpatialSound.")]
    public EventReference spatialSoundEvent;
    [Tooltip("Posição relativa para o som de presença.")]
    public Vector3 spatialSoundOffset;

    // RadioStaticBurst
    [Tooltip("Evento FMOD de estática para RadioStaticBurst.")]
    public EventReference staticBurstEvent;
    [Tooltip("Duração da estática.")]
    public float staticBurstDuration;

    // QuickLightChange
    [Tooltip("Duração do efeito de luz piscando.")]
    public float lightChangeDuration;
    [Tooltip("Intensidade máxima da luz durante o efeito.")]
    public float lightChangePeakIntensity;

    // --- Parâmetros Limiar 3 ---
    // VisualFlash
    [Tooltip("Duração total da animação de piscar.")]
    public float visualFlashDuration;
    [Tooltip("O pico de insanidade visual a ser atingido (geralmente 1.0).")]
    [Range(0f, 1f)] public float visualFlashPeak;

    // FalseAlarmClock
    [Tooltip("Duração do alarme falso em segundos.")]
    public float falseAlarmDuration;

    // TemporaryMaterialSwap
    [Tooltip("Material temporário para swap em fotos/documentos.")]
    public Material tempSwapMaterial;
    [Tooltip("Duração do swap de material.")]
    public float tempSwapDuration;

    // PlayVideoOnMaterial
    [Tooltip("VideoClip a ser exibido no material.")]
    public VideoClip videoClip;
    [Tooltip("Material alvo para exibir o vídeo.")]
    public Material videoTargetMaterial;
    [Tooltip("Duração do vídeo.")]
    public float videoDuration;

    // --- Parâmetros Limiar 4 ---
    // SpawnCoveredBody
    [Tooltip("Prefab do corpo coberto a ser spawnado.")]
    public GameObject coveredBodyPrefab;
    [Tooltip("Posição relativa para spawn do corpo.")]
    public Vector3 coveredBodyOffset;

    // SpawnHallucination
    [Tooltip("Prefab da alucinação a ser spawnada.")]
    public GameObject hallucinationPrefab;
    [Tooltip("Posição relativa para spawn da alucinação.")]
    public Vector3 hallucinationOffset;

    // GuiltChorusBurst
    [Tooltip("Evento FMOD do Coro da Culpa.")]
    public EventReference guiltChorusEvent;
    [Tooltip("Duração do Coro da Culpa.")]
    public float guiltChorusDuration;
}