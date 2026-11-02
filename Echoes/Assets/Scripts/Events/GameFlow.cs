using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Atos do capítulo, em ordem. A comparação entre atos (ex: Current >= GameAct.Memories)
/// é usada pelos sistemas para saber o que está liberado.
/// </summary>
public enum GameAct
{
    Prologue,       // Início até a Track 1 do rádio terminar: período seguro, sem dreno de sanidade nem eventos
    Investigation,  // Track 1 encerrada: sanidade e eventos de horror ativos, lembranças ainda bloqueadas
    Memories,       // Track 2 encerrada: lembranças (flashbacks) liberadas
    Choir,          // Track 3 encerrada: mecânica do coro
    Ending          // Gatilho do final alcançado
}

/// <summary>
/// Fonte única da fase atual do jogo. Converte os eventos de progressão (faixas do rádio, final)
/// em um ato e avisa quando ele muda. Os atos só avançam; recarregar a cena volta ao prólogo.
/// </summary>
public static class GameFlow
{
    public static GameAct Current { get; private set; } = GameAct.Prologue;

    // Envia o novo ato
    public static event Action<GameAct> OnActChanged;

    // Disparado só ao carregar um checkpoint, depois do OnActChanged: sistemas que dependem
    // do caminho até o ato (rádio, coro) reconstroem o próprio estado aqui, sem sons nem animações
    public static event Action<GameAct> OnActRestored;

    /// <summary>
    /// Avança para o ato informado. Ignorado se o jogo já estiver nele ou além.
    /// </summary>
    public static void Advance(GameAct act)
    {
        if (act <= Current) return;

        Current = act;
        Debug.Log($"[GameFlow] Ato atual: {Current}");
        OnActChanged?.Invoke(Current);
    }

    /// <summary>
    /// Coloca o jogo direto no ato salvo num checkpoint.
    /// </summary>
    public static void Restore(GameAct act)
    {
        Current = act;
        Debug.Log($"[GameFlow] Ato restaurado do checkpoint: {Current}");
        OnActChanged?.Invoke(Current);
        OnActRestored?.Invoke(Current);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        Current = GameAct.Prologue;
        OnActChanged = null;
        OnActRestored = null;

        GameEvents.OnRadioTrack1Completed -= HandleTrack1Completed;
        GameEvents.OnRadioTrack1Completed += HandleTrack1Completed;
        GameEvents.OnRadioTrack2Completed -= HandleTrack2Completed;
        GameEvents.OnRadioTrack2Completed += HandleTrack2Completed;
        GameEvents.OnRadioTrack3Completed -= HandleTrack3Completed;
        GameEvents.OnRadioTrack3Completed += HandleTrack3Completed;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Recarregar a cena (morte, reinício pelo menu) recomeça o capítulo
        if (mode == LoadSceneMode.Single) Current = GameAct.Prologue;
    }

    private static void HandleTrack1Completed() => Advance(GameAct.Investigation);
    private static void HandleTrack2Completed() => Advance(GameAct.Memories);
    private static void HandleTrack3Completed() => Advance(GameAct.Choir);
}
