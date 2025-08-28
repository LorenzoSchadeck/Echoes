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
}