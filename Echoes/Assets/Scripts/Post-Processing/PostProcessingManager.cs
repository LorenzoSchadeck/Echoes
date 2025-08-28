using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    [Tooltip("Duração em segundos da transição RÁPIDA ao entrar ou sair de um flashback.")]
    [SerializeField] private float stateTransitionDuration = 0.5f;

    private Bloom bloom;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Tonemapping tonemapping;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private PostProcessingProfile currentBaseProfile;
    private PostProcessingProfile currentInsanityProfile;
    private float targetInsanity = 0f;
    private float currentBlendedInsanity = 0f;

    private Coroutine activeStateTransition;

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
    }

    private void OnDisable()
    {
        InsanityManager.OnInsanityChanged -= HandleInsanityChange;
        GameEvents.OnFlashbackStarted -= OnFlashbackStarted;
        GameEvents.OnFlashbackEnded -= OnFlashbackEnded;
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
        if (activeStateTransition != null) return; // Pausa a lógica de insanidade durante a transição de estado

        currentBlendedInsanity = Mathf.Lerp(currentBlendedInsanity, targetInsanity, Time.deltaTime * insanityTransitionSpeed);
        ApplyBlendedProfile(currentBlendedInsanity);
    }

    private void OnFlashbackStarted() => TransitionToNewState(flashbackProfile, insaneProfile);

    private void OnFlashbackEnded() => TransitionToNewState(saneProfile, insaneProfile);

    private void TransitionToNewState(PostProcessingProfile newBase, PostProcessingProfile newInsane)
    {
        if (activeStateTransition != null) StopCoroutine(activeStateTransition);
        activeStateTransition = StartCoroutine(StateTransitionRoutine(newBase, newInsane));
    }

    private IEnumerator StateTransitionRoutine(PostProcessingProfile newBase, PostProcessingProfile newInsane)
    {
        float startingInsanityBlend = currentBlendedInsanity;

        // Leva a insanidade visual de volta para o perfil base atual.
        float elapsedTime = 0f;
        while (elapsedTime < stateTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / stateTransitionDuration);
            currentBlendedInsanity = Mathf.Lerp(startingInsanityBlend, 0f, t);
            ApplyBlendedProfile(currentBlendedInsanity); // Aplica usando o perfil base ANTIGO
            yield return null;
        }

        // Define o novo estado e força a atualização final.
        currentBaseProfile = newBase;
        currentInsanityProfile = newInsane;
        currentBlendedInsanity = 0f;
        targetInsanity = 0f;
        ApplyBlendedProfile(0f); // Aplica o novo perfil base puro.

        activeStateTransition = null; // Libera o Update
    }

    /// <summary>
    /// Define os perfis que serão usados para a interpolação de insanidade.
    /// </summary>
    /// <param name="baseProfile">O perfil visual para sanidade = 0.</param>
    /// <param name="insanityProfile">O perfil visual para insanidade = 1.</param>
    public void SetTransitionProfiles(PostProcessingProfile baseProfile, PostProcessingProfile insanityProfile)
    {
        currentBaseProfile = baseProfile;
        currentInsanityProfile = insanityProfile;
    }

    private void ApplyBlendedProfile(float t)
    {
        // Se os perfis não estiverem definidos, não faz nada
        if (currentBaseProfile == null || currentInsanityProfile == null) return;

        t = Mathf.Clamp01(t);

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(currentBaseProfile.vignetteIntensity, currentInsanityProfile.vignetteIntensity, t);
            vignette.smoothness.value = Mathf.Lerp(currentBaseProfile.vignetteSmoothness, currentInsanityProfile.vignetteSmoothness, t);
        }

        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(currentBaseProfile.bloomIntensity, currentInsanityProfile.bloomIntensity, t);
            bloom.threshold.value = Mathf.Lerp(currentBaseProfile.bloomThreshold, currentInsanityProfile.bloomThreshold, t);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(currentBaseProfile.chromaticAberrationIntensity, currentInsanityProfile.chromaticAberrationIntensity, t);
        }

        if (tonemapping != null)
        {
            tonemapping.mode.value = t > 0.1f ? currentInsanityProfile.tonemappingMode : currentBaseProfile.tonemappingMode;
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = Mathf.Lerp(currentBaseProfile.lensDistortionIntensity, currentInsanityProfile.lensDistortionIntensity, t);
            lensDistortion.scale.value = Mathf.Lerp(currentBaseProfile.lensDistortionScale, currentInsanityProfile.lensDistortionScale, t);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = Mathf.Lerp(currentBaseProfile.postExposure, currentInsanityProfile.postExposure, t);
            colorAdjustments.contrast.value = Mathf.Lerp(currentBaseProfile.contrast, currentInsanityProfile.contrast, t);
            colorAdjustments.colorFilter.value = Color.Lerp(currentBaseProfile.colorFilter, currentInsanityProfile.colorFilter, t);
            colorAdjustments.hueShift.value = Mathf.Lerp(currentBaseProfile.hueShift, currentInsanityProfile.hueShift, t);
            colorAdjustments.saturation.value = Mathf.Lerp(currentBaseProfile.saturation, currentInsanityProfile.saturation, t);
        }
    }
    
    /// <summary>
    /// Retorna o valor de Post Exposure do perfil de flashback.
    /// Usado pelo FlashbackEffectController para uma transição suave.
    /// </summary>
    public float GetFlashbackProfileExposure()
    {
        if (flashbackProfile != null)
        {
            return flashbackProfile.postExposure;
        }
        return 0f; // Retorna 0 se o perfil não estiver definido
    }
}