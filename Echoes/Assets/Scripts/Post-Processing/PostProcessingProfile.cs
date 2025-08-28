using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "NewPostProcessingProfile", menuName = "Horror Game/Post-Processing Profile")]
public class PostProcessingProfile : ScriptableObject
{
    [Header("Vignette")]
    [Tooltip("Intensidade da vinheta. Controla o quão escuras as bordas da tela ficam.")]
    [Range(0f, 1f)] public float vignetteIntensity = 0f;

    [Tooltip("Suavidade da transição da vinheta. Valores mais altos criam uma borda mais suave.")]
    [Range(0.01f, 1f)] public float vignetteSmoothness = 0.2f;

    [Header("Bloom")]
    [Tooltip("Intensidade do efeito de brilho (Bloom).")]
    [Range(0f, 10f)] public float bloomIntensity = 0f;

    [Tooltip("Limiar de brilho para um pixel começar a emitir brilho.")]
    [Range(0f, 2f)] public float bloomThreshold = 0.9f;

    [Header("Chromatic Aberration")]
    [Tooltip("Intensidade da aberração cromática.")]
    [Range(0f, 1f)] public float chromaticAberrationIntensity = 0f;

    [Header("Tonemapping")]
    [Tooltip("Modo de mapeamento de tons. ACES é o padrão cinematográfico.")]
    public TonemappingMode tonemappingMode = TonemappingMode.None;

    [Header("Color Adjustments")]
    [Tooltip("Ajusta a exposição geral da imagem (brilho).")]
    [Range(-10f, 10f)] public float postExposure = 0f;

    [Tooltip("Aumenta ou diminui a diferença entre áreas claras e escuras.")]
    [Range(-100f, 100f)] public float contrast = 0f;

    [Tooltip("Aplica uma tonalidade de cor sobre a imagem final.")]
    public Color colorFilter = Color.white;

    [Tooltip("Gira todas as cores da imagem no círculo cromático.")]
    [Range(-180f, 180f)] public float hueShift = 0f;

    [Tooltip("Ajusta a intensidade de todas as cores.")]
    [Range(-100f, 100f)] public float saturation = 0f;
    
    [Header("Lens Distortion")]
    [Tooltip("Intensidade da distorção da lente. Negativo 'puxa para dentro', positivo 'empurra para fora'.")]
    [Range(-1f, 1f)] public float lensDistortionIntensity = 0f;

    [Tooltip("Escala da distorção para evitar bordas pretas.")]
    [Range(0.01f, 1f)] public float lensDistortionScale = 1f;
}