using UnityEngine;

/// <summary>
/// Giroflex da viatura visto pela janela: luz vermelha e azul se alternando, entrando aos poucos
/// enquanto o carro sobe a rua.
/// </summary>
public class PoliceLights : MonoBehaviour
{
    [SerializeField] private Light red;
    [SerializeField] private Light blue;
    [SerializeField] private float intensity = 40f;
    [Tooltip("Voltas do giroflex por segundo.")]
    [SerializeField] private float speed = 1.4f;
    [Tooltip("Segundos para a viatura chegar (a luz cresce nesse tempo).")]
    [SerializeField] private float arrivalTime = 6f;

    [Header("Clarão na janela")]
    [Tooltip("Planos aditivos lá fora, vistos pela janela, que acendem junto com cada luz.")]
    [SerializeField] private Renderer redGlow;
    [SerializeField] private Renderer blueGlow;
    [SerializeField] private Color redGlowColor = new Color(1f, 0.12f, 0.06f);
    [SerializeField] private Color blueGlowColor = new Color(0.15f, 0.3f, 1f);
    [SerializeField, Range(0f, 1f)] private float glowStrength = 0.85f;

    private MaterialPropertyBlock glowBlock;

    private float startedAt = -1f;

    private void Awake() => SetLights(0f, 0f);

    public void TurnOn()
    {
        if (startedAt < 0f) startedAt = Time.time;
    }

    public void TurnOff()
    {
        startedAt = -1f;
        SetLights(0f, 0f);
    }

    private void Update()
    {
        if (startedAt < 0f) return;

        float arrival = Mathf.Clamp01((Time.time - startedAt) / arrivalTime);
        float phase = Time.time * speed * Mathf.PI * 2f;
        // Cada cor acende numa metade da volta, com um pico curto como o de uma lâmpada girando
        float r = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase)), 3f);
        float b = Mathf.Pow(Mathf.Max(0f, -Mathf.Sin(phase)), 3f);
        float scale = intensity * arrival * (GameOptions.Current.reduceFlashes ? 0.4f : 1f);
        SetLights(r * scale, b * scale);
    }

    private void SetLights(float redIntensity, float blueIntensity)
    {
        if (red != null) { red.intensity = redIntensity; red.enabled = redIntensity > 0.01f; }
        if (blue != null) { blue.intensity = blueIntensity; blue.enabled = blueIntensity > 0.01f; }
        float peak = Mathf.Max(intensity, 0.0001f);
        SetGlow(redGlow, redGlowColor, redIntensity / peak);
        SetGlow(blueGlow, blueGlowColor, blueIntensity / peak);
    }

    private void SetGlow(Renderer glow, Color color, float amount)
    {
        if (glow == null) return;
        glow.enabled = amount > 0.01f;
        if (!glow.enabled) return;
        glowBlock ??= new MaterialPropertyBlock();
        color.a = Mathf.Clamp01(amount * glowStrength);
        glowBlock.SetColor("_BaseColor", color);
        glow.SetPropertyBlock(glowBlock);
    }

#if UNITY_EDITOR
    public void EditorSetup(Light redLight, Light blueLight, float peak)
    {
        red = redLight;
        blue = blueLight;
        intensity = peak;
    }

    public void EditorSetupGlow(Renderer redPlane, Renderer bluePlane)
    {
        redGlow = redPlane;
        blueGlow = bluePlane;
    }
#endif
}
