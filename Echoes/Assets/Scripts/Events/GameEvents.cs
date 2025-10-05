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

    // --- Horror Event Stubs ---
    public static void TriggerSpatialSound(EventReference soundEvent, UnityEngine.Vector3 offset) { /* TODO: Implementar lógica FMOD */ }
    public static void TriggerRadioStatic(EventReference staticEvent, float duration) { /* TODO: Implementar lógica FMOD */ }
    public static void TriggerQuickLightChange(float duration, float peakIntensity) { /* TODO: Implementar lógica */ }
    public static void TriggerTemporaryMaterialSwap(UnityEngine.Material mat, float duration) { /* TODO: Implementar lógica */ }
    public static void TriggerPlayVideoOnMaterial(UnityEngine.Video.VideoClip clip, UnityEngine.Material target, float duration) { /* TODO: Implementar lógica */ }
    public static void TriggerSpawnCoveredBody(UnityEngine.GameObject prefab, UnityEngine.Vector3 offset) { /* TODO: Implementar lógica */ }
    public static void TriggerSpawnHallucination(UnityEngine.GameObject prefab, UnityEngine.Vector3 offset) { /* TODO: Implementar lógica */ }
    public static void TriggerGuiltChorus(EventReference chorusEvent, float duration) { /* TODO: Implementar lógica FMOD */ }

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
}