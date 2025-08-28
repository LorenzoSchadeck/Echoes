using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FlashbackEffectController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Rigidbody playerRigidbody; 

    [Header("Transition Effects")]
    [Tooltip("Duração total da animação de entrada no flashback.")]
    [SerializeField] private float animationDuration = 3.0f;
    [Tooltip("Pico máximo do Post Exposure (efeito de 'clarão').")]
    [SerializeField] private float exposurePeak = 2.0f;
    [Tooltip("Curva para a fase de 'puxar' da lente (de 0 a -1). Duração = metade da transição total.")]
    [SerializeField] private AnimationCurve lensPullCurve;
    [Tooltip("Curva para a fase de 'empurrar' da lente (de -1 a 1). Duração = metade da transição total.")]
    [SerializeField] private AnimationCurve lensPushCurve;
    
    [Header("Profile Dependencies")]
    [Tooltip("Referência ao PostProcessingManager para obter valores de perfil.")]
    [SerializeField] private PostProcessingManager postProcessingManager;

    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

    private LensDistortion lensDistortion;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private Coroutine activeAnimationCoroutine;

    private void Awake()
    {
        if (postProcessVolume == null || postProcessVolume.profile == null || playerTransform == null || playerRigidbody == null || postProcessingManager == null)
        {
            Debug.LogError("Uma ou mais dependências cruciais não foram atribuídas no FlashbackEffectController!", this);
            enabled = false;
            return;
        }

        if (!postProcessVolume.profile.TryGet(out lensDistortion)) Debug.LogWarning("Lens Distortion not found on Volume.");
        if (!postProcessVolume.profile.TryGet(out colorAdjustments)) Debug.LogWarning("Color Adjustments not found on Volume.");
        if (!postProcessVolume.profile.TryGet(out vignette)) Debug.LogWarning("Vignette not found on Volume.");
    }

    private void OnEnable()
    {
        GameEvents.OnFlashbackStarted += PlayEntryAnimation;
        GameEvents.OnFlashbackEnded += PlayExitAnimation; 
    }

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= PlayEntryAnimation;
        GameEvents.OnFlashbackEnded -= PlayExitAnimation;
    }

    private void PlayEntryAnimation()
    {
        GameObject teleportPoint = GameObject.FindWithTag("FlashbackTeleport");
        if (teleportPoint == null) return;
        
        StartAnimation(FlashbackEntryRoutine(teleportPoint.transform));
    }
    
    private void PlayExitAnimation()
    {
        // Para a saída, não precisamos de um ponto de teleporte de destino.
        StartAnimation(FlashbackExitRoutine());
    }
    
    private void StartAnimation(IEnumerator routine)
    {
        if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
        activeAnimationCoroutine = StartCoroutine(routine);
    }

    private IEnumerator FlashbackEntryRoutine(Transform teleportDestination)
    {
        Debug.Log("Iniciando animação de ENTRADA do flashback...");

        originalPlayerPosition = playerRigidbody.position;
        originalPlayerRotation = playerRigidbody.rotation;

        float originalExposure = colorAdjustments.postExposure.value;
        float targetExposure = postProcessingManager.GetFlashbackProfileExposure();
        float halfDuration = animationDuration / 2f;
        float elapsedTime = 0f;

        // FASE 1: Puxão e clarão
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            lensDistortion.intensity.value = lensPullCurve.Evaluate(t);
            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, exposurePeak, t);
            yield return null;
        }

        // Teleporte
        playerRigidbody.position = teleportDestination.position;
        playerRigidbody.rotation = teleportDestination.rotation;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        // FASE 2: Empurrão e fade do clarão
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            lensDistortion.intensity.value = lensPushCurve.Evaluate(t);
            colorAdjustments.postExposure.value = Mathf.Lerp(exposurePeak, targetExposure, t);
            yield return null;
        }

        lensDistortion.intensity.value = 0f;
        activeAnimationCoroutine = null;
    }

    private IEnumerator FlashbackExitRoutine()
    {
        Debug.Log("Iniciando animação de SAÍDA do flashback...");

        float targetExposure = postProcessingManager.GetSaneProfileExposure();
        float targetVignetteIntensity = postProcessingManager.GetSaneProfileVignetteIntensity();
        float targetLensDistortionScale = postProcessingManager.GetSaneProfileLensDistortionScale();

        float originalExposure = colorAdjustments.postExposure.value;
        float originalVignetteIntensity = vignette.intensity.value;

        float halfDuration = animationDuration / 2f;
        float elapsedTime = 0f;

        // --- FASE 1: Empurrão (Intensity 0 -> 1) ---
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;

            lensDistortion.intensity.value = Mathf.Lerp(0f, 1f, t);

            // CORRETO: Scale < 1 para dar zoom e esconder as bordas pretas
            lensDistortion.scale.value = Mathf.Lerp(1f, 1.5f, t);

            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, exposurePeak, t);
            vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, targetVignetteIntensity, t);
            yield return null;
        }

        // TELEPORTE DE VOLTA
        playerRigidbody.position = originalPlayerPosition;
        playerRigidbody.rotation = originalPlayerRotation;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        // --- FASE 2: Resolução (Intensity 1 -> 0) ---
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;

            lensDistortion.intensity.value = Mathf.Lerp(1f, 0f, t);
            lensDistortion.scale.value = Mathf.Lerp(1.5f, targetLensDistortionScale, t);

            colorAdjustments.postExposure.value = Mathf.Lerp(exposurePeak, targetExposure, t);
            yield return null;
        }

        // FINALIZAÇÃO
        lensDistortion.intensity.value = 0f;
        lensDistortion.scale.value = targetLensDistortionScale;
        colorAdjustments.postExposure.value = targetExposure;
        vignette.intensity.value = targetVignetteIntensity;

        activeAnimationCoroutine = null;
        Debug.Log("Sequência de saída do flashback concluída.");
    }

}