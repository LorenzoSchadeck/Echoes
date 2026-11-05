using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hora do jogo, em minutos desde a meia-noite. Anda em tempo real a partir de <see cref="StartMinutes"/>
/// e salta para a frente quando Ethan perde tempo (<see cref="LostTimeEvent"/>). Salva no checkpoint.
/// </summary>
public static class GameClock
{
    // O capítulo começa às 23:40
    public const float StartMinutes = 22 * 60 + 47; // 22h47, quando Ethan acorda no Prólogo
    private const float MinutesPerDay = 24 * 60;

    // Minutos somados à hora inicial além do tempo jogado (apagões e checkpoints)
    private static float offsetMinutes;
    private static float sceneStartTime;

    public static event Action<float> OnTimeSkipped; // minutos pulados

    /// <summary>
    /// Minutos desde a meia-noite, de 0 a 1440.
    /// </summary>
    public static float Minutes => Mathf.Repeat(StartMinutes + offsetMinutes + (Time.time - sceneStartTime) / 60f, MinutesPerDay);

    public static int Hour => Mathf.FloorToInt(Minutes / 60f);
    public static int Minute => Mathf.FloorToInt(Minutes % 60f);

    /// <summary>
    /// Pula o relógio para a frente (tempo que Ethan não lembra de ter vivido).
    /// </summary>
    public static void Skip(float minutes)
    {
        offsetMinutes += minutes;
        OnTimeSkipped?.Invoke(minutes);
    }

    /// <summary>
    /// Coloca o relógio numa hora salva, ao carregar um checkpoint.
    /// </summary>
    public static void Restore(float minutes)
    {
        sceneStartTime = Time.time;
        offsetMinutes = minutes - StartMinutes;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        offsetMinutes = 0f;
        sceneStartTime = 0f;
        OnTimeSkipped = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // Cada vez que a cena do jogo carrega, o relógio recomeça (o checkpoint o restaura depois)
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        offsetMinutes = 0f;
        sceneStartTime = Time.time;
    }
}
