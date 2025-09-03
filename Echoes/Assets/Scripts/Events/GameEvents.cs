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

    // Evento para quando o jogador perde uma quantidade específica de sanidade.
    public static event Action<float> OnSanityLost;
    public static void TriggerSanityLost(float amount) => OnSanityLost?.Invoke(amount);
}