using UnityEngine;

/// <summary>
/// Tremor das mãos de Ethan: a sanidade aparece no corpo, não numa barra. Itens segurados
/// (inspeção) tremem cada vez mais conforme a sanidade cai abaixo de <see cref="StartSanity"/>.
/// Respeita a opção de acessibilidade de tremor.
/// </summary>
public static class HandTremor
{
    // Sanidade em que o tremor começa e em que chega ao máximo
    public const float StartSanity = 0.6f;
    private const float MaxSanity = 0.1f;

    private const float MaxPositionOffset = 0.006f; // metros
    private const float MaxRotationOffset = 2.5f;   // graus
    private const float MinFrequency = 3f;          // ciclos de ruído por segundo
    private const float MaxFrequency = 9f;

    private static float sanity = 1f;

    /// <summary>
    /// 0 = mãos firmes, 1 = tremor máximo.
    /// </summary>
    public static float Intensity
    {
        get
        {
            float t = Mathf.InverseLerp(StartSanity, MaxSanity, sanity);
            return t * t * GameOptions.Current.cameraShake; // cresce devagar no começo
        }
    }

    /// <summary>
    /// Deslocamento e rotação do tremor neste instante, em espaço de mundo.
    /// Use um <paramref name="seed"/> diferente por objeto para não tremerem em sincronia.
    /// </summary>
    public static void Sample(float seed, out Vector3 positionOffset, out Quaternion rotationOffset)
    {
        float intensity = Intensity;
        if (intensity <= 0f)
        {
            positionOffset = Vector3.zero;
            rotationOffset = Quaternion.identity;
            return;
        }

        float time = Time.unscaledTime * Mathf.Lerp(MinFrequency, MaxFrequency, intensity);
        positionOffset = new Vector3(Noise(time, seed), Noise(time, seed + 17f), Noise(time, seed + 31f)) * (MaxPositionOffset * intensity);
        rotationOffset = Quaternion.Euler(new Vector3(Noise(time, seed + 47f), Noise(time, seed + 59f), Noise(time, seed + 71f)) * (MaxRotationOffset * intensity));
    }

    // Ruído suave entre -1 e 1
    private static float Noise(float time, float seed) => Mathf.PerlinNoise(time, seed) * 2f - 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        sanity = 1f;
        InsanityManager.OnSanityChanged -= HandleSanityChanged;
        InsanityManager.OnSanityChanged += HandleSanityChanged;
    }

    private static void HandleSanityChanged(float value) => sanity = value;
}
