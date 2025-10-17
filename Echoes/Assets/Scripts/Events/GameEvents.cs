using FMODUnity;
using System;

public static class GameEvents
{
    // Evento disparado quando um flashback começa.
    public static event Action OnFlashbackStarted;
    public static void TriggerFlashbackStarted() => OnFlashbackStarted?.Invoke();

    // Evento disparado quando um flashback termina.
    public static event Action OnFlashbackEnded;
    public static void TriggerFlashbackEnded() => OnFlashbackEnded?.Invoke();

    // Evento para o uso de um remédio.
    public static event Action OnRemedyUsed;
    public static void TriggerRemedyUsed() => OnRemedyUsed?.Invoke();

    // Evento disparado quando a sequência de morte começa.
    public static event Action<float> OnDeathSequenceStarted;
    public static void TriggerDeathSequenceStarted(float duration) => OnDeathSequenceStarted?.Invoke(duration);

    // Evento disparado se o jogador se curar, cancelando a sequência de morte.
    public static event Action OnDeathSequenceCancelled;
    public static void TriggerDeathSequenceCancelled() => OnDeathSequenceCancelled?.Invoke();

    // Evento para a morte do jogador.
    public static event Action OnPlayerDied;
    public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();

    // Envia o pico de insanidade (ex: 1f) e a duração total da animação (ex: 2s)
    public static event Action<float, float> OnTriggerVisualFlash;
    public static void TriggerVisualFlash(float peakInsanity, float duration) => OnTriggerVisualFlash?.Invoke(peakInsanity, duration);

    // Envia a duração do alarme como parâmetro
    public static event Action<float> OnFalseAlarmTriggered;
    public static void TriggerFalseAlarm(float duration) => OnFalseAlarmTriggered?.Invoke(duration);

    // Eventos para horror events específicos
    public static event Action<System.Collections.Generic.List<UnityEngine.Light>, EventReference, UnityEngine.GameObject> OnSimpleLightFlickerTriggered;
    public static event Action<UnityEngine.GameObject, UnityEngine.Transform> OnSpawnCoveredBodyTriggered;

    // --- Horror Event Implementations ---
    // Range padrão para todos os eventos de áudio (mesmo que o rádio)
    private const float AUDIO_MAX_RANGE = 70f;
    
    public static void TriggerSpatialSound(EventReference soundEvent, UnityEngine.GameObject target) 
    { 
        if (target != null && !soundEvent.IsNull)
        {
            PlaySpatialAudioWithRange(soundEvent, target.transform.position);
        }
    }
    
    public static void TriggerRadioStatic(EventReference staticEvent, UnityEngine.GameObject radioTarget, float duration) 
    { 
        if (radioTarget != null && !staticEvent.IsNull)
        {
            PlaySpatialAudioWithDuration(staticEvent, radioTarget.transform.position, duration);
        }
    }
    
    /// <summary>
    /// Reproduz um evento FMOD com range específico igual ao rádio
    /// </summary>
    private static void PlaySpatialAudioWithRange(EventReference audioEvent, UnityEngine.Vector3 position)
    {
        var eventInstance = FMODUnity.RuntimeManager.CreateInstance(audioEvent);
        
        if (eventInstance.isValid())
        {
            // Define posição 3D
            eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
            
            // Define range máximo igual ao rádio
            eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, AUDIO_MAX_RANGE);
            
            // Inicia reprodução
            eventInstance.start();
            
            // Libera automaticamente quando terminar
            eventInstance.release();
        }
    }
    
    /// <summary>
    /// Reproduz um evento FMOD com range específico e duração limitada
    /// </summary>
    private static void PlaySpatialAudioWithDuration(EventReference audioEvent, UnityEngine.Vector3 position, float duration)
    {
        var eventInstance = FMODUnity.RuntimeManager.CreateInstance(audioEvent);
        
        if (eventInstance.isValid())
        {
            // Define posição 3D
            eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
            
            // Define range máximo igual ao rádio
            eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, AUDIO_MAX_RANGE);
            
            // Inicia reprodução
            eventInstance.start();
            
            // Inicia corrotina para parar o áudio após a duração especificada
            if (HorrorEventManager.Instance != null)
            {
                HorrorEventManager.Instance.StartCoroutine(StopAudioAfterDuration(eventInstance, duration));
            }
            else
            {
                // Fallback: libera imediatamente se não há manager
                eventInstance.release();
            }
        }
    }
    
    /// <summary>
    /// Corrotina que para o áudio após a duração especificada
    /// </summary>
    private static System.Collections.IEnumerator StopAudioAfterDuration(FMOD.Studio.EventInstance eventInstance, float duration)
    {
        UnityEngine.Debug.Log($"[GameEvents] Áudio de rádio estático iniciado - será encerrado em {duration} segundos");
        
        yield return new UnityEngine.WaitForSeconds(duration);
        
        if (eventInstance.isValid())
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
            UnityEngine.Debug.Log($"[GameEvents] Áudio de rádio estático encerrado após {duration} segundos");
        }
    }

    public static void TriggerSimpleLightFlicker(System.Collections.Generic.List<UnityEngine.Light> lights, EventReference flickerSound = default, UnityEngine.GameObject soundTarget = null)
    {
        OnSimpleLightFlickerTriggered?.Invoke(lights, flickerSound, soundTarget);
    }

    public static void TriggerDualGuiltChorus(EventReference event1, EventReference event2, UnityEngine.GameObject target1, UnityEngine.GameObject target2)
    {
        if (target1 != null && !event1.IsNull)
        {
            PlaySpatialAudioWithRange(event1, target1.transform.position);
        }
        
        if (target2 != null && !event2.IsNull)
        {
            PlaySpatialAudioWithRange(event2, target2.transform.position);
        }
    }
    
    public static void TriggerSpawnCoveredBody(UnityEngine.GameObject prefab, UnityEngine.Transform spawnPoint) 
    { 
        OnSpawnCoveredBodyTriggered?.Invoke(prefab, spawnPoint);
    }
    


    // Evento para quando o jogador perde uma quantidade específica de sanidade.
    public static event Action<float> OnSanityLost;
    public static void TriggerSanityLost(float amount) => OnSanityLost?.Invoke(amount);

    // Evento para ativar o rádio pela primeira vez
    public static event Action OnRadioActivated;
    public static void TriggerRadioActivation() => OnRadioActivated?.Invoke();

    // Evento para iniciar uma transmissão específica do rádio
    public static event Action<int> OnRadioTransmissionStarted;
    public static void TriggerRadioTransmission(int transmissionIndex) => OnRadioTransmissionStarted?.Invoke(transmissionIndex);

    // Evento de ativacao do puzzle sonoro
    public static event Action OnAudioPuzzleStarted;
    public static void TriggerAudioPuzzleStarted() => OnAudioPuzzleStarted?.Invoke();

    public static event Action OnAudioPuzzleSolved;
    public static void TriggerAudioPuzzleSolved() => OnAudioPuzzleSolved?.Invoke();

    // Evento para batida na porta (quando Track 1 do rádio é ativada)
    public static event Action OnDoorKnockTriggered;
    public static void TriggerDoorKnock() => OnDoorKnockTriggered?.Invoke();

    // Evento para quando a Track 1 do rádio termina (fim do período seguro)
    public static event Action OnRadioTrack1Completed;
    public static void TriggerRadioTrack1Completed() => OnRadioTrack1Completed?.Invoke();

    // Evento para quando a Track 2 do rádio é desligada (libera lembranças)
    public static event Action OnRadioTrack2Completed;
    public static void TriggerRadioTrack2Completed() => OnRadioTrack2Completed?.Invoke();

    // Evento para ativação inicial do rádio via trigger
    public static event Action OnRadioFirstTrigger;
    public static void TriggerRadioFirstTrigger() => OnRadioFirstTrigger?.Invoke();

    // Evento para ativação da segunda faixa via papel
    public static event Action OnRadioPaperTrigger;
    public static void TriggerRadioPaperTrigger() => OnRadioPaperTrigger?.Invoke();
    
    // Evento para reset completo de todos os sistemas quando a cena é resetada
    public static event Action OnSceneReset;
    public static void TriggerSceneReset() => OnSceneReset?.Invoke();
}