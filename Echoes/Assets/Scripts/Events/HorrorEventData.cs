using FMODUnity;
using UnityEngine;
using System.Collections.Generic;

public enum HorrorEventType
{
    // Limiar 2: Ansiedade
    PlaySpatialSound,      // Sons de Presença
    RadioStaticBurst,      // Estática Fantasma
    LightFlicker,          // Piscadas de Luz

    // Limiar 3: Angústia
    VisualFlash,           // Piscar de Insanidade
    FalseAlarmClock,       // Alarme Falso

    // Limiar 4: Colapso
    SpawnCoveredBody,      // Corpo na Mesa
    GuiltChorusBurst       // Coro da Culpa - Raro
}

[System.Serializable]
public struct HorrorEvent
{
    [Tooltip("Nome do evento para organização.")]
    public string eventName;

    [Tooltip("O tipo de evento que será disparado.")]
    public HorrorEventType type;

    // --- Parâmetros Limiar 2 ---
    // PlaySpatialSound
    [Tooltip("Evento FMOD a ser tocado no evento PlaySpatialSound.")]
    public EventReference spatialSoundEvent;
    [Tooltip("GameObject onde o som será tocado.")]
    public GameObject spatialSoundTarget;

    // RadioStaticBurst
    [Tooltip("Evento FMOD de estática para RadioStaticBurst.")]
    public EventReference staticBurstEvent;
    [Tooltip("GameObject do rádio onde o som será tocado.")]
    public GameObject radioTarget;
    [Tooltip("Duração da estática.")]
    public float staticBurstDuration;

    // LightFlicker
    [Tooltip("Lista de luzes que devem piscar de forma sincronizada.")]
    public List<Light> flickerLights;
    [Tooltip("Evento FMOD de som para cada piscada (opcional).")]
    public EventReference flickerSoundEvent;
    [Tooltip("GameObject onde o som da piscada será posicionado (opcional - se vazio, usa posição da primeira luz).")]
    public GameObject flickerSoundTarget;

    // --- Parâmetros Limiar 3 ---
    // VisualFlash - Piscar instantâneo (1 segundo) para insanidade máxima
    [Tooltip("Sempre será 1 segundo - piscar instantâneo para insanidade máxima.")]
    public float visualFlashDuration;

    // FalseAlarmClock
    [Tooltip("GameObject do alarme onde o som será tocado.")]
    public GameObject alarmTarget;
    [Tooltip("Duração do alarme falso em segundos.")]
    public float falseAlarmDuration;

    // --- Parâmetros Limiar 4 ---
    // SpawnCoveredBody
    [Tooltip("Prefab do corpo coberto a ser spawnado.")]
    public GameObject coveredBodyPrefab;
    [Tooltip("Transform do ponto onde o corpo será spawnado.")]
    public Transform coveredBodySpawnPoint;

    // GuiltChorusBurst
    [Tooltip("Primeiro evento FMOD do Coro da Culpa.")]
    public EventReference guiltChorusEvent1;
    [Tooltip("Segundo evento FMOD do Coro da Culpa.")]
    public EventReference guiltChorusEvent2;
    [Tooltip("Primeiro objeto onde o áudio 1 será tocado.")]
    public GameObject guiltChorusTarget1;
    [Tooltip("Segundo objeto onde o áudio 2 será tocado.")]
    public GameObject guiltChorusTarget2;
}