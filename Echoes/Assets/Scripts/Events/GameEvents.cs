using System;

public static class GameEvents
{
    // Evento disparado quando um flashback começa.
    public static event Action OnFlashbackStarted;
    public static void TriggerFlashbackStarted() => OnFlashbackStarted?.Invoke();

    // Evento disparado quando um flashback termina.
    public static event Action OnFlashbackEnded;
    public static void TriggerFlashbackEnded() => OnFlashbackEnded?.Invoke();

    // Evento para a morte do jogador.
    public static event Action OnPlayerDied;
    public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();

    // Evento para o uso de um remédio.
    public static event Action OnRemedyUsed;
    public static void TriggerRemedyUsed() => OnRemedyUsed?.Invoke();
}