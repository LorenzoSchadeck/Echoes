using FMODUnity;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class HorrorEventManager : MonoBehaviour
{
    public static HorrorEventManager Instance { get; private set; }

    [Header("Event Library")]
    [Tooltip("A lista de todos os possíveis eventos de horror que podem acontecer.")]
    [SerializeField] private List<HorrorEvent> eventLibrary;

    [Header("Trigger Logic")]
    [Tooltip("Intervalo em segundos entre cada 'rolagem de dados' para um evento.")]
    [SerializeField] private float checkInterval = 10.0f;
    [Tooltip("Chance base de um evento ocorrer, mesmo com insanidade zero. Aumenta a imprevisibilidade.")]
    [Range(0f, 1f)]
    [SerializeField] private float baseChance = 0.05f; // 5% de chance base
    private float currentSanity = 1.0f;
    private bool isInFlashback = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        InsanityManager.OnSanityChanged += HandleSanityChange;
        GameEvents.OnFlashbackStarted += OnFlashbackStart;
        GameEvents.OnFlashbackEnded += OnFlashbackEnd;
    }

    private void OnDisable()
    {
        InsanityManager.OnSanityChanged -= HandleSanityChange;
        GameEvents.OnFlashbackStarted -= OnFlashbackStart;
        GameEvents.OnFlashbackEnded -= OnFlashbackEnd;
    }

    private void Start()
    {
        StartCoroutine(HorrorCheckRoutine());
    }

    private void HandleSanityChange(float newSanity)
    {
        currentSanity = newSanity;
    }

    private void OnFlashbackStart()
    {
        isInFlashback = true;
    }

    private void OnFlashbackEnd()
    {
        isInFlashback = false;
    } 

    private IEnumerator HorrorCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        if (isInFlashback) return;

        // A chance de um evento acontecer aumenta conforme a sanidade DIMINUI.
        float insanityFactor = 1f - currentSanity; // Invertido
        float chance = baseChance + insanityFactor * (1f - baseChance);

        if (Random.value < chance)
        {
            TriggerRandomEvent();
        }
    }

    private void TriggerRandomEvent()
    {
        // 1. Filtra a biblioteca para pegar apenas os eventos que podem acontecer AGORA
        List<HorrorEvent> possibleEvents = eventLibrary
            .Where(evt => currentSanity <= evt.maxSanityThreshold)
            .ToList();

        if (possibleEvents.Count == 0) return;

        // 2. Sorteia um evento da lista de possibilidades
        HorrorEvent selectedEvent = possibleEvents[Random.Range(0, possibleEvents.Count)];

        // 3. Dispara o evento correspondente
        switch (selectedEvent.type)
        {
            // Limiar 2: Ansiedade
            case HorrorEventType.PlaySpatialSound:
                GameEvents.TriggerSpatialSound(selectedEvent.spatialSoundEvent, selectedEvent.spatialSoundOffset);
                break;
            case HorrorEventType.RadioStaticBurst:
                GameEvents.TriggerRadioStatic(selectedEvent.staticBurstEvent, selectedEvent.staticBurstDuration);
                break;
            case HorrorEventType.QuickLightChange:
                GameEvents.TriggerQuickLightChange(selectedEvent.lightChangeDuration, selectedEvent.lightChangePeakIntensity);
                break;

            // Limiar 3: Angústia
            case HorrorEventType.VisualFlash:
                GameEvents.TriggerVisualFlash(selectedEvent.visualFlashPeak, selectedEvent.visualFlashDuration);
                break;
            case HorrorEventType.FalseAlarmClock:
                GameEvents.TriggerFalseAlarm(selectedEvent.falseAlarmDuration);
                break;
            case HorrorEventType.TemporaryMaterialSwap:
                GameEvents.TriggerTemporaryMaterialSwap(selectedEvent.tempSwapMaterial, selectedEvent.tempSwapDuration);
                break;
            case HorrorEventType.PlayVideoOnMaterial:
                GameEvents.TriggerPlayVideoOnMaterial(selectedEvent.videoClip, selectedEvent.videoTargetMaterial, selectedEvent.videoDuration);
                break;

            // Limiar 4: Colapso
            case HorrorEventType.SpawnCoveredBody:
                GameEvents.TriggerSpawnCoveredBody(selectedEvent.coveredBodyPrefab, selectedEvent.coveredBodyOffset);
                break;
            case HorrorEventType.SpawnHallucination:
                GameEvents.TriggerSpawnHallucination(selectedEvent.hallucinationPrefab, selectedEvent.hallucinationOffset);
                break;
            case HorrorEventType.GuiltChorusBurst:
                GameEvents.TriggerGuiltChorus(selectedEvent.guiltChorusEvent, selectedEvent.guiltChorusDuration);
                break;
        }
    }
}