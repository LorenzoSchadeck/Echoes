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
    [Tooltip("Duração da transição ao entrar/sair de um flashback.")]
    [SerializeField] private float stateTransitionDuration = 1.0f;
    [Tooltip("Duração da transição de cura ao usar um remédio.")]
    [SerializeField] private float remedyTransitionDuration = 3.0f;

    [Header("Sanity Thresholds")]
    [Tooltip("A sanidade precisa cair ABAIXO deste valor para que os efeitos visuais comecem a aparecer.")]
    [SerializeField, Range(0f, 1f)] private float visualEffectStartThreshold = 0.5f; // Começa em 50%

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
    private float currentSanity = 1.0f;

    private Coroutine activeVisualEffectCoroutine;
    
    // Sistema de override temporário
    private bool hasLensDistortionOverride = false;
    private float overrideLensDistortionIntensity = 0f;
    private float overrideLensDistortionScale = 1f;
    
    // Sistema de coordenação flashback-remédio
    private bool isFlashbackExitInProgress = false;
    private bool hasPendingRemedyTransition = false;
    private bool isRemedyTransitionActive = false;

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

    private void Start()
    {
        currentBaseProfile = saneProfile;
        currentInsanityProfile = insaneProfile;
        ApplyBlendedProfile(0);
    }

    private void Update()
    {
        // Não aplica mudanças automáticas durante transições ativas ou de remédio
        if (activeVisualEffectCoroutine == null && !isRemedyTransitionActive)
        {
            // Calcula o 't' (0 a 1) para a interpolação com base no limiar.
            float t = Mathf.InverseLerp(visualEffectStartThreshold, 0f, currentSanity);

            // Usa o 't' calculado para aplicar a mistura dos perfis.
            ApplyBlendedProfile(t);
        }
    }

    private void OnEnable()
    {
        InsanityManager.OnSanityChanged += HandleInsanityChange;
        GameEvents.OnFlashbackStarted += OnFlashbackStarted;
        GameEvents.OnFlashbackEnded += OnFlashbackEnded;
        GameEvents.OnDeathSequenceStarted += OnDeathSequenceStarted;
        GameEvents.OnDeathSequenceCancelled += OnDeathSequenceCancelled;
    }

    private void OnDisable()
    {
        InsanityManager.OnSanityChanged -= HandleInsanityChange;
        GameEvents.OnFlashbackStarted -= OnFlashbackStarted;
        GameEvents.OnFlashbackEnded -= OnFlashbackEnded;
        GameEvents.OnDeathSequenceStarted -= OnDeathSequenceStarted;
        GameEvents.OnDeathSequenceCancelled -= OnDeathSequenceCancelled;
    }

    private void HandleInsanityChange(float newInsanityValue)
    {
        // Durante transição de remédio, ignora mudanças de sanidade para não interferir na transição suave
        if (isRemedyTransitionActive) return;
        
        // Sempre atualiza a sanidade interna
        currentSanity = newInsanityValue;
    }

    /// <summary>
    /// Interrompe qualquer coroutine de efeito visual que esteja em andamento.
    /// Chamado por controladores externos (como o FlashbackEffectController) para assumir a prioridade.
    /// NOTA: Este método NÃO remove overrides de lente - eles devem ser removidos explicitamente.
    /// </summary>
    public void StopAllVisualEffects()
    {
        if (activeVisualEffectCoroutine != null)
        {
            StopCoroutine(activeVisualEffectCoroutine);
            activeVisualEffectCoroutine = null;
        }
        
        // Reset da flag de transição de remédio se necessário
        if (isRemedyTransitionActive)
        {
            isRemedyTransitionActive = false;
        }
    }
    
    /// <summary>
    /// Marca que uma saída de flashback está em progresso.
    /// Usado para coordenar com transições de remédio pendentes.
    /// </summary>
    public void NotifyFlashbackExitStarted()
    {
        isFlashbackExitInProgress = true;
    }
    
    /// <summary>
    /// Marca que uma saída de flashback foi concluída.
    /// Se houver uma transição de remédio pendente, ela será executada agora.
    /// </summary>
    public void NotifyFlashbackExitCompleted()
    {
        isFlashbackExitInProgress = false;
        
        // Se há uma transição de remédio pendente, executa agora
        if (hasPendingRemedyTransition)
        {
            hasPendingRemedyTransition = false;
            
            // IMPORTANTE: Ativa a flag antes da transição
            isRemedyTransitionActive = true;

            StartVisualEffect(SmoothRemedyTransitionRoutine());
        }
    }

    // --- Disparadores de Efeitos ---
    private void OnFlashbackStarted() => StartVisualEffect(TransitionToProfileRoutine(flashbackProfile, stateTransitionDuration));
    private void OnFlashbackEnded() => StartVisualEffect(TransitionToProfileRoutine(saneProfile, stateTransitionDuration));
    private void OnDeathSequenceCancelled() 
    {     
        // Se uma saída de flashback está em progresso, agenda a transição de remédio para depois
        if (isFlashbackExitInProgress)
        {
            hasPendingRemedyTransition = true;
        }
        else
        {
            // IMPORTANTE: Marca imediatamente que a transição de remédio começou
            // para bloquear mudanças de sanidade que possam interferir
            isRemedyTransitionActive = true;
            
            // Executa transição suave para o perfil são
            StartVisualEffect(SmoothRemedyTransitionRoutine());
        }
    }
    private void OnDeathSequenceStarted(float duration) => StartVisualEffect(DeathEffectRoutine(duration));

    // --- Gerenciador e Coroutines ---
    private void StartVisualEffect(IEnumerator effectRoutine)
    {
        if (activeVisualEffectCoroutine != null) StopCoroutine(activeVisualEffectCoroutine);
        activeVisualEffectCoroutine = StartCoroutine(effectRoutine);
    }

    private IEnumerator TransitionToProfileRoutine(PostProcessingProfile targetProfile, float duration)
    {
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

        // Garante o estado final preciso
        vignette.intensity.value = targetProfile.vignetteIntensity;
        bloom.intensity.value = targetProfile.bloomIntensity;
        bloom.threshold.value = targetProfile.bloomThreshold;
        chromaticAberration.intensity.value = targetProfile.chromaticAberrationIntensity;
        lensDistortion.intensity.value = targetProfile.lensDistortionIntensity;
        lensDistortion.scale.value = targetProfile.lensDistortionScale;
        colorAdjustments.postExposure.value = targetProfile.postExposure;
        colorAdjustments.contrast.value = targetProfile.contrast;
        colorAdjustments.colorFilter.value = targetProfile.colorFilter;
        colorAdjustments.hueShift.value = targetProfile.hueShift;
        colorAdjustments.saturation.value = targetProfile.saturation;

        // Atualiza perfis para o novo estado
        if (targetProfile == saneProfile)
        {
            currentBaseProfile = saneProfile;
            currentInsanityProfile = insaneProfile;
            currentSanity = 1.0f; // Sincroniza apenas se for transição para são
        }
        else if (targetProfile == flashbackProfile)
        {
            currentBaseProfile = flashbackProfile;
            currentInsanityProfile = insaneProfile;
            currentSanity = 1.0f; // No flashback também reseta a sanidade
        }

        activeVisualEffectCoroutine = null;
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

    private IEnumerator SmoothRemedyTransitionRoutine()
    { 
        // A flag isRemedyTransitionActive já foi ativada em OnDeathSequenceCancelled
        // Apenas confirma que está ativa para garantir
        
        // Captura o estado atual de TODOS os valores
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

        // Valores alvo do perfil são
        float targetVignetteIntensity = saneProfile.vignetteIntensity;
        float targetBloomIntensity = saneProfile.bloomIntensity;
        float targetBloomThreshold = saneProfile.bloomThreshold;
        float targetChromaIntensity = saneProfile.chromaticAberrationIntensity;
        float targetLensDistortionIntensity = saneProfile.lensDistortionIntensity;
        float targetLensDistortionScale = saneProfile.lensDistortionScale;
        float targetExposure = saneProfile.postExposure;
        float targetContrast = saneProfile.contrast;
        Color targetColorFilter = saneProfile.colorFilter;
        float targetHueShift = saneProfile.hueShift;
        float targetSaturation = saneProfile.saturation;

        float elapsedTime = 0f;
        while (elapsedTime < remedyTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / remedyTransitionDuration;
            
            // Aplicar curva suave
            t = Mathf.SmoothStep(0f, 1f, t);

            // Interpola cada valor suavemente
            vignette.intensity.value = Mathf.Lerp(startVignetteIntensity, targetVignetteIntensity, t);
            bloom.intensity.value = Mathf.Lerp(startBloomIntensity, targetBloomIntensity, t);
            bloom.threshold.value = Mathf.Lerp(startBloomThreshold, targetBloomThreshold, t);
            chromaticAberration.intensity.value = Mathf.Lerp(startChromaIntensity, targetChromaIntensity, t);
            lensDistortion.intensity.value = Mathf.Lerp(startLensDistortionIntensity, targetLensDistortionIntensity, t);
            lensDistortion.scale.value = Mathf.Lerp(startLensDistortionScale, targetLensDistortionScale, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetExposure, t);
            colorAdjustments.contrast.value = Mathf.Lerp(startContrast, targetContrast, t);
            colorAdjustments.colorFilter.value = Color.Lerp(startColorFilter, targetColorFilter, t);
            colorAdjustments.hueShift.value = Mathf.Lerp(startHueShift, targetHueShift, t);
            colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, t);

            yield return null;
        }

        // Garante os valores finais exatos
        vignette.intensity.value = targetVignetteIntensity;
        bloom.intensity.value = targetBloomIntensity;
        bloom.threshold.value = targetBloomThreshold;
        chromaticAberration.intensity.value = targetChromaIntensity;
        lensDistortion.intensity.value = targetLensDistortionIntensity;
        lensDistortion.scale.value = targetLensDistortionScale;
        colorAdjustments.postExposure.value = targetExposure;
        colorAdjustments.contrast.value = targetContrast;
        colorAdjustments.colorFilter.value = targetColorFilter;
        colorAdjustments.hueShift.value = targetHueShift;
        colorAdjustments.saturation.value = targetSaturation;

        // Atualiza o estado para perfil são
        currentBaseProfile = saneProfile;
        currentInsanityProfile = insaneProfile;
        currentSanity = 1.0f;

        // DESBLOQUEIA mudanças de sanidade após a transição
        isRemedyTransitionActive = false;
        
        activeVisualEffectCoroutine = null;
    }

    private void ApplyBlendedProfile(float t)
    {
        if (currentBaseProfile == null || currentInsanityProfile == null) return;
        t = Mathf.Clamp01(t);

        if (vignette != null) vignette.intensity.value = Mathf.Lerp(currentBaseProfile.vignetteIntensity, currentInsanityProfile.vignetteIntensity, t);
        if (bloom != null) { bloom.intensity.value = Mathf.Lerp(currentBaseProfile.bloomIntensity, currentInsanityProfile.bloomIntensity, t); bloom.threshold.value = Mathf.Lerp(currentBaseProfile.bloomThreshold, currentInsanityProfile.bloomThreshold, t); }
        if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(currentBaseProfile.chromaticAberrationIntensity, currentInsanityProfile.chromaticAberrationIntensity, t);
        if (tonemapping != null) tonemapping.mode.value = t > 0.1f ? currentInsanityProfile.tonemappingMode : currentBaseProfile.tonemappingMode;
        if (lensDistortion != null) 
        { 
            // Se há um override ativo, usa os valores do override ao invés dos valores baseados na sanidade
            if (hasLensDistortionOverride)
            {
                lensDistortion.intensity.value = overrideLensDistortionIntensity;
                lensDistortion.scale.value = overrideLensDistortionScale;
            }
            else
            {
                lensDistortion.intensity.value = Mathf.Lerp(currentBaseProfile.lensDistortionIntensity, currentInsanityProfile.lensDistortionIntensity, t); 
                lensDistortion.scale.value = Mathf.Lerp(currentBaseProfile.lensDistortionScale, currentInsanityProfile.lensDistortionScale, t); 
            }
        }
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
    
    /// <summary>
    /// Obtém o valor da distorção de lente que deveria estar ativo baseado na sanidade atual,
    /// considerando se há uma transição visual ativa ou não.
    /// </summary>
    public float GetSanityBasedLensDistortionIntensity()
    {
        if (currentBaseProfile == null || currentInsanityProfile == null) 
            return 0f;
            
        // Se há uma transição ativa, usa o perfil base atual
        if (activeVisualEffectCoroutine != null)
        {
            return currentBaseProfile.lensDistortionIntensity;
        }
        
        // Calcula baseado na sanidade atual
        float t = Mathf.InverseLerp(visualEffectStartThreshold, 0f, currentSanity);
        t = Mathf.Clamp01(t);
        
        return Mathf.Lerp(currentBaseProfile.lensDistortionIntensity, currentInsanityProfile.lensDistortionIntensity, t);
    }
    
    /// <summary>
    /// Verifica se há um override ativo na distorção de lente.
    /// </summary>
    public bool HasLensDistortionOverride()
    {
        return hasLensDistortionOverride;
    }
    
    /// <summary>
    /// Obtém o valor atual da intensidade da distorção de lente.
    /// </summary>
    public float GetCurrentLensDistortionIntensity()
    {
        return lensDistortion != null ? lensDistortion.intensity.value : 0f;
    }
    
    /// <summary>
    /// Obtém o valor atual da escala da distorção de lente.
    /// </summary>
    public float GetCurrentLensDistortionScale()
    {
        return lensDistortion != null ? lensDistortion.scale.value : 1f;
    }
    
    /// <summary>
    /// Aplica uma distorção de lente temporária, sobrescrevendo o sistema de sanidade.
    /// Este override permanece ativo até ser removido explicitamente.
    /// </summary>
    /// <param name="intensity">Intensidade da distorção (-1 a 1)</param>
    /// <param name="scale">Escala da distorção (0.01 a 1)</param>
    public void ApplyTemporaryLensDistortion(float intensity, float scale = 1f)
    {
        hasLensDistortionOverride = true;
        overrideLensDistortionIntensity = Mathf.Clamp(intensity, -1f, 1f);
        overrideLensDistortionScale = Mathf.Clamp(scale, 0.01f, 1f);
        
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = overrideLensDistortionIntensity;
            lensDistortion.scale.value = overrideLensDistortionScale;
        }
    }
    
    /// <summary>
    /// Interpola suavemente a distorção de lente temporária de um valor para outro.
    /// </summary>
    /// <param name="startIntensity">Intensidade inicial da distorção</param>
    /// <param name="targetIntensity">Intensidade alvo da distorção</param>
    /// <param name="duration">Duração da interpolação em segundos</param>
    /// <param name="startScale">Escala inicial da distorção</param>
    /// <param name="targetScale">Escala alvo da distorção</param>
    /// <returns>Coroutine da interpolação</returns>
    public IEnumerator InterpolateLensDistortion(float startIntensity, float targetIntensity, float duration, float startScale = 1f, float targetScale = 1f)
    {
        if (duration <= 0f) yield break;
        
        hasLensDistortionOverride = true;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Aplica curva suave (ease-in-out)
            t = Mathf.SmoothStep(0f, 1f, t);
            
            // Interpola os valores
            float currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            float currentScale = Mathf.Lerp(startScale, targetScale, t);
            
            // Atualiza os valores de override
            overrideLensDistortionIntensity = Mathf.Clamp(currentIntensity, -1f, 1f);
            overrideLensDistortionScale = Mathf.Clamp(currentScale, 0.01f, 1f);
            
            // Aplica os valores interpolados
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = overrideLensDistortionIntensity;
                lensDistortion.scale.value = overrideLensDistortionScale;
            }
            
            yield return null;
        }
        
        // Garante os valores finais exatos
        overrideLensDistortionIntensity = Mathf.Clamp(targetIntensity, -1f, 1f);
        overrideLensDistortionScale = Mathf.Clamp(targetScale, 0.01f, 1f);
        
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = overrideLensDistortionIntensity;
            lensDistortion.scale.value = overrideLensDistortionScale;
        }
    }
    
    /// <summary>
    /// Remove o override da distorção de lente e restaura ao estado baseado na sanidade atual.
    /// </summary>
    public void RestoreLensDistortionToSanityState()
    {
        hasLensDistortionOverride = false;
        
        // Força a aplicação imediata do estado baseado na sanidade
        if (currentBaseProfile != null && currentInsanityProfile != null)
        {
            float t = activeVisualEffectCoroutine == null ? 
                Mathf.InverseLerp(visualEffectStartThreshold, 0f, currentSanity) : 0f;
            t = Mathf.Clamp01(t);
            
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = Mathf.Lerp(currentBaseProfile.lensDistortionIntensity, currentInsanityProfile.lensDistortionIntensity, t);
                lensDistortion.scale.value = Mathf.Lerp(currentBaseProfile.lensDistortionScale, currentInsanityProfile.lensDistortionScale, t);
            }
        }
    }

    /// <summary>
    /// Força uma transição imediata para o estado são sem animação.
    /// Usado para situações de emergência.
    /// </summary>
    public void ForceResetToSaneState()
    {
        if (activeVisualEffectCoroutine != null)
        {
            StopCoroutine(activeVisualEffectCoroutine);
            activeVisualEffectCoroutine = null;
        }

        // Reset do estado de transição de remédio
        isRemedyTransitionActive = false;

        // Aplica valores do perfil são imediatamente
        if (saneProfile != null)
        {
            vignette.intensity.value = saneProfile.vignetteIntensity;
            bloom.intensity.value = saneProfile.bloomIntensity;
            bloom.threshold.value = saneProfile.bloomThreshold;
            chromaticAberration.intensity.value = saneProfile.chromaticAberrationIntensity;
            lensDistortion.intensity.value = saneProfile.lensDistortionIntensity;
            lensDistortion.scale.value = saneProfile.lensDistortionScale;
            colorAdjustments.postExposure.value = saneProfile.postExposure;
            colorAdjustments.contrast.value = saneProfile.contrast;
            colorAdjustments.colorFilter.value = saneProfile.colorFilter;
            colorAdjustments.hueShift.value = saneProfile.hueShift;
            colorAdjustments.saturation.value = saneProfile.saturation;

            currentBaseProfile = saneProfile;
            currentInsanityProfile = insaneProfile;
            currentSanity = 1.0f;
        }

        // Remove qualquer override de lens distortion
        hasLensDistortionOverride = false;
    }


}