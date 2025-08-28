using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PostProcessingManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Profiles")]
    [SerializeField] private PostProcessingProfile saneProfile;
    [SerializeField] private PostProcessingProfile insaneProfile;
    [SerializeField] private PostProcessingProfile flashbackProfile;

    [Header("Transition Settings")]
    [Tooltip("Velocidade com que a insanidade visual acompanha a insanidade do jogador.")]
    [SerializeField] private float insanityTransitionSpeed = 1.0f;
    [Tooltip("Duração da transição ao entrar/sair de um flashback.")]
    [SerializeField] private float stateTransitionDuration = 1.0f;
    [Tooltip("Duração da transição de cura ao usar um remédio.")]
    [SerializeField] private float remedyTransitionDuration = 3.0f;

    // Referências cacheadas
    private Bloom bloom;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Tonemapping tonemapping;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    // Estado atual
    private PostProcessingProfile currentBaseProfile;
    private PostProcessingProfile currentInsanityProfile;
    private float targetInsanity = 0f;
    private float currentBlendedInsanity = 0f;

    private Coroutine activeVisualEffectCoroutine;

    private void Awake()
    {
        if (postProcessVolume == null || postProcessVolume.profile == null) { enabled = false; return; }
        if (!postProcessVolume.profile.TryGet(out vignette)) Debug.LogWarning("Vignette not found.");
        if (!postProcessVolume.profile.TryGet(out bloom)) Debug.LogWarning("Bloom not found.");
        if (!postProcessVolume.profile.TryGet(out chromaticAberration)) Debug.LogWarning("Chromatic Aberration not found.");
        if (!postProcessVolume.profile.TryGet(out lensDistortion)) Debug.LogWarning("Lens Distortion not found.");
        if (!postProcessVolume.profile.TryGet(out tonemapping)) Debug.LogWarning("Tonemapping not found.");
        if (!postProcessVolume.profile.TryGet(out colorAdjustments)) Debug.LogWarning("Color Adjustments not found.");
    }

    private void OnEnable()
    {
        InsanityManager.OnInsanityChanged += HandleInsanityChange;
        GameEvents.OnFlashbackStarted += OnFlashbackStarted;
        GameEvents.OnFlashbackEnded += OnFlashbackEnded;
        GameEvents.OnDeathSequenceStarted += OnDeathSequenceStarted;
        GameEvents.OnDeathSequenceCancelled += OnDeathSequenceCancelled;
    }

    private void OnDisable()
    {
        InsanityManager.OnInsanityChanged -= HandleInsanityChange;
        GameEvents.OnFlashbackStarted -= OnFlashbackStarted;
        GameEvents.OnFlashbackEnded -= OnFlashbackEnded;
        GameEvents.OnDeathSequenceStarted -= OnDeathSequenceStarted;
        GameEvents.OnDeathSequenceCancelled -= OnDeathSequenceCancelled;
    }

    private void Start()
    {
        currentBaseProfile = saneProfile;
        currentInsanityProfile = insaneProfile;
        ApplyBlendedProfile(0);
        currentBlendedInsanity = 0;
    }

    private void HandleInsanityChange(float newInsanityValue)
    {
        targetInsanity = newInsanityValue;
    }

    private void Update()
    {
        if (activeVisualEffectCoroutine == null)
        {
            currentBlendedInsanity = Mathf.Lerp(currentBlendedInsanity, targetInsanity, Time.deltaTime * insanityTransitionSpeed);
            ApplyBlendedProfile(currentBlendedInsanity);
        }
    }

    // --- Disparadores de Efeitos ---

    private void OnFlashbackStarted() => StartVisualEffect(TransitionToProfileRoutine(flashbackProfile, stateTransitionDuration));
    private void OnFlashbackEnded() => StartVisualEffect(TransitionToProfileRoutine(saneProfile, stateTransitionDuration));
    private void OnDeathSequenceCancelled() => StartVisualEffect(TransitionToProfileRoutine(saneProfile, remedyTransitionDuration));
    private void OnDeathSequenceStarted(float duration) => StartVisualEffect(DeathEffectRoutine(duration));

    // --- Gerenciador e Coroutines ---

    private void StartVisualEffect(IEnumerator effectRoutine)
    {
        if (activeVisualEffectCoroutine != null) StopCoroutine(activeVisualEffectCoroutine);
        activeVisualEffectCoroutine = StartCoroutine(effectRoutine);
    }

    private IEnumerator TransitionToProfileRoutine(PostProcessingProfile targetProfile, float duration)
    {
        Debug.Log($"Iniciando transição para o perfil: {targetProfile.name} em {duration}s");

        if (targetProfile == saneProfile)
        {
            currentBaseProfile = saneProfile;
            currentInsanityProfile = insaneProfile;
        }
        else if (targetProfile == flashbackProfile)
        {
            currentBaseProfile = flashbackProfile;
            currentInsanityProfile = insaneProfile;
        }

        // Captura o estado inicial de TODOS os valores gerenciados
        float startVignetteIntensity = vignette.intensity.value;
        float startBloomIntensity = bloom.intensity.value;
        float startBloomThreshold = bloom.threshold.value;
        float startChromaIntensity = chromaticAberration.intensity.value;
        float startLensDistortionIntensity = lensDistortion.intensity.value;
        float startLensDistortionScale = lensDistortion.scale.value;
        float startExposure = colorAdjustments.postExposure.value;
        float startContrast = colorAdjustments.contrast.value;
        Color startColorFilter = colorAdjustments.colorFilter.value;
        float startHueShift = colorAdjustments.hueShift.value;
        float startSaturation = colorAdjustments.saturation.value;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Interpola cada valor do estado ATUAL para o estado de DESTINO (o perfil alvo)
            vignette.intensity.value = Mathf.Lerp(startVignetteIntensity, targetProfile.vignetteIntensity, t);
            bloom.intensity.value = Mathf.Lerp(startBloomIntensity, targetProfile.bloomIntensity, t);
            bloom.threshold.value = Mathf.Lerp(startBloomThreshold, targetProfile.bloomThreshold, t);
            chromaticAberration.intensity.value = Mathf.Lerp(startChromaIntensity, targetProfile.chromaticAberrationIntensity, t);
            lensDistortion.intensity.value = Mathf.Lerp(startLensDistortionIntensity, targetProfile.lensDistortionIntensity, t);
            lensDistortion.scale.value = Mathf.Lerp(startLensDistortionScale, targetProfile.lensDistortionScale, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetProfile.postExposure, t);
            colorAdjustments.contrast.value = Mathf.Lerp(startContrast, targetProfile.contrast, t);
            colorAdjustments.colorFilter.value = Color.Lerp(startColorFilter, targetProfile.colorFilter, t);
            colorAdjustments.hueShift.value = Mathf.Lerp(startHueShift, targetProfile.hueShift, t);
            colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetProfile.saturation, t);

            yield return null;
        }

        // Garante o estado final e reseta a insanidade visual
        ApplyBlendedProfile(0f);
        currentBlendedInsanity = 0f;
        targetInsanity = 0f;

        activeVisualEffectCoroutine = null;
        Debug.Log("Transição concluída.");
    }

    private IEnumerator DeathEffectRoutine(float duration)
    {
        float startSaturation = colorAdjustments.saturation.value;
        float startVignetteIntensity = vignette.intensity.value;
        float startExposure = colorAdjustments.postExposure.value;
        float targetExposure = (insaneProfile != null ? insaneProfile.postExposure : 0f) - 1.5f;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            if (colorAdjustments != null) { colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, -100f, t); colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetExposure, t); }
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(startVignetteIntensity, 1f, t);
            yield return null;
        }
        if (colorAdjustments != null) { colorAdjustments.saturation.value = -100f; colorAdjustments.postExposure.value = targetExposure; }
        if (vignette != null) vignette.intensity.value = 1f;
    }

    private void ApplyBlendedProfile(float t)
    {
        if (currentBaseProfile == null || currentInsanityProfile == null) return;
        t = Mathf.Clamp01(t);

        if (vignette != null) vignette.intensity.value = Mathf.Lerp(currentBaseProfile.vignetteIntensity, currentInsanityProfile.vignetteIntensity, t);
        if (bloom != null) { bloom.intensity.value = Mathf.Lerp(currentBaseProfile.bloomIntensity, currentInsanityProfile.bloomIntensity, t); bloom.threshold.value = Mathf.Lerp(currentBaseProfile.bloomThreshold, currentInsanityProfile.bloomThreshold, t); }
        if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(currentBaseProfile.chromaticAberrationIntensity, currentInsanityProfile.chromaticAberrationIntensity, t);
        if (tonemapping != null) tonemapping.mode.value = t > 0.1f ? currentInsanityProfile.tonemappingMode : currentBaseProfile.tonemappingMode;
        if (lensDistortion != null) { lensDistortion.intensity.value = Mathf.Lerp(currentBaseProfile.lensDistortionIntensity, currentInsanityProfile.lensDistortionIntensity, t); lensDistortion.scale.value = Mathf.Lerp(currentBaseProfile.lensDistortionScale, currentInsanityProfile.lensDistortionScale, t); }
        if (colorAdjustments != null) { colorAdjustments.postExposure.value = Mathf.Lerp(currentBaseProfile.postExposure, currentInsanityProfile.postExposure, t); colorAdjustments.contrast.value = Mathf.Lerp(currentBaseProfile.contrast, currentInsanityProfile.contrast, t); colorAdjustments.colorFilter.value = Color.Lerp(currentBaseProfile.colorFilter, currentInsanityProfile.colorFilter, t); colorAdjustments.hueShift.value = Mathf.Lerp(currentBaseProfile.hueShift, currentInsanityProfile.hueShift, t); colorAdjustments.saturation.value = Mathf.Lerp(currentBaseProfile.saturation, currentInsanityProfile.saturation, t); }
    }

    public float GetFlashbackProfileExposure()
    {
        return flashbackProfile != null ? flashbackProfile.postExposure : 0f;
    }

    /// <summary>
    /// Retorna o valor de Post Exposure do perfil são.
    /// </summary>
    public float GetSaneProfileExposure()
    {
        return saneProfile != null ? saneProfile.postExposure : 0f;
    }

    public float GetSaneProfileVignetteIntensity()
    {
        return saneProfile != null ? saneProfile.vignetteIntensity : 0f;
    }
    
    public float GetSaneProfileLensDistortionScale()
    {
        return saneProfile != null ? saneProfile.lensDistortionScale : 1f;
    }
}