using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using FMODUnity;

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

    [Header("🔊 Audio Settings")]
    [Tooltip("Som 2D tocado quando o jogador entra no flashback (duração: 3.0s)")]
    [SerializeField] private EventReference flashbackEntrySoundEvent;
    
    [Tooltip("Som 2D tocado quando o jogador sai do flashback (duração: 3.0s)")]
    [SerializeField] private EventReference flashbackExitSoundEvent;

    [Header("📦 GameObject Control")]
    [Tooltip("GameObjects que serão ATIVADOS quando o jogador entrar na lembrança")]
    [SerializeField] private GameObject[] objectsToActivateInFlashback;
    
    [Tooltip("GameObjects que serão DESATIVADOS quando o jogador entrar na lembrança")]
    [SerializeField] private GameObject[] objectsToDeactivateInFlashback;

    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

    private LensDistortion lensDistortion;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private Coroutine activeAnimationCoroutine;
    
    // Controle de estado dos GameObjects
    private bool[] originalActivateStates;  // Estado original dos objetos que serão ativados
    private bool[] originalDeactivateStates; // Estado original dos objetos que serão desativados

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
        
        // Inicializa os arrays para controle de GameObjects
        InitializeGameObjectArrays();
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
        
        // Toca o som de entrada do flashback (2D)
        PlayFlashbackEntrySound();
        
        // Configura GameObjects para o flashback
        SetFlashbackGameObjects();
        
        StartAnimation(FlashbackEntryRoutine(teleportPoint.transform));
    }
    
    private void PlayExitAnimation()
    {
        // Notifica o PostProcessingManager que a saída do flashback começou
        postProcessingManager.NotifyFlashbackExitStarted();
        postProcessingManager.StopAllVisualEffects();
        
        // Toca o som de saída do flashback (2D)
        PlayFlashbackExitSound();
        
        // Restaura GameObjects ao estado original
        RestoreOriginalGameObjects();
        
        StartAnimation(FlashbackExitRoutine());
    }
    
    private void StartAnimation(IEnumerator routine)
    {
        if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
        activeAnimationCoroutine = StartCoroutine(routine);
    }

    /// <summary>
    /// Toca o som 2D de entrada no flashback
    /// </summary>
    private void PlayFlashbackEntrySound()
    {
        if (flashbackEntrySoundEvent.IsNull) return;
        
        try
        {
            // Cria uma instância 2D do evento FMOD (não espacial)
            var entryInstance = RuntimeManager.CreateInstance(flashbackEntrySoundEvent);
            entryInstance.start();
            entryInstance.release();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FlashbackEffectController] Erro ao tocar som de entrada do flashback: {e.Message}");
        }
    }

    /// <summary>
    /// Toca o som 2D de saída do flashback
    /// </summary>
    private void PlayFlashbackExitSound()
    {
        if (flashbackExitSoundEvent.IsNull) return;
        
        try
        {
            // Cria uma instância 2D do evento FMOD (não espacial)
            var exitInstance = RuntimeManager.CreateInstance(flashbackExitSoundEvent);
            exitInstance.start();
            exitInstance.release();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FlashbackEffectController] Erro ao tocar som de saída do flashback: {e.Message}");
        }
    }

    private IEnumerator FlashbackEntryRoutine(Transform teleportDestination)
    {
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

        // Notifica o PostProcessingManager que a saída do flashback foi concluída
        postProcessingManager.NotifyFlashbackExitCompleted();

        activeAnimationCoroutine = null;
    }

    #region GameObject Control Methods

    /// <summary>
    /// Inicializa os arrays para controle de GameObjects, salvando seus estados originais
    /// </summary>
    private void InitializeGameObjectArrays()
    {
        // Inicializa array para objetos que serão ativados
        if (objectsToActivateInFlashback != null)
        {
            originalActivateStates = new bool[objectsToActivateInFlashback.Length];
            for (int i = 0; i < objectsToActivateInFlashback.Length; i++)
            {
                if (objectsToActivateInFlashback[i] != null)
                {
                    originalActivateStates[i] = objectsToActivateInFlashback[i].activeInHierarchy;
                }
            }
        }

        // Inicializa array para objetos que serão desativados
        if (objectsToDeactivateInFlashback != null)
        {
            originalDeactivateStates = new bool[objectsToDeactivateInFlashback.Length];
            for (int i = 0; i < objectsToDeactivateInFlashback.Length; i++)
            {
                if (objectsToDeactivateInFlashback[i] != null)
                {
                    originalDeactivateStates[i] = objectsToDeactivateInFlashback[i].activeInHierarchy;
                }
            }
        }

        Debug.Log($"[FlashbackEffectController] Inicializados {originalActivateStates?.Length ?? 0} objetos para ativação e {originalDeactivateStates?.Length ?? 0} objetos para desativação");
    }

    /// <summary>
    /// Ativa os objetos configurados para o flashback e desativa os objetos normais
    /// </summary>
    private void SetFlashbackGameObjects()
    {
        // Ativa objetos do flashback
        if (objectsToActivateInFlashback != null)
        {
            for (int i = 0; i < objectsToActivateInFlashback.Length; i++)
            {
                if (objectsToActivateInFlashback[i] != null)
                {
                    objectsToActivateInFlashback[i].SetActive(true);
                    Debug.Log($"[FlashbackEffectController] Ativado objeto: {objectsToActivateInFlashback[i].name}");
                }
            }
        }

        // Desativa objetos normais
        if (objectsToDeactivateInFlashback != null)
        {
            for (int i = 0; i < objectsToDeactivateInFlashback.Length; i++)
            {
                if (objectsToDeactivateInFlashback[i] != null)
                {
                    objectsToDeactivateInFlashback[i].SetActive(false);
                    Debug.Log($"[FlashbackEffectController] Desativado objeto: {objectsToDeactivateInFlashback[i].name}");
                }
            }
        }
    }

    /// <summary>
    /// Restaura o estado original de todos os GameObjects controlados
    /// </summary>
    private void RestoreOriginalGameObjects()
    {
        // Restaura objetos que foram ativados no flashback
        if (objectsToActivateInFlashback != null && originalActivateStates != null)
        {
            for (int i = 0; i < objectsToActivateInFlashback.Length; i++)
            {
                if (objectsToActivateInFlashback[i] != null && i < originalActivateStates.Length)
                {
                    objectsToActivateInFlashback[i].SetActive(originalActivateStates[i]);
                    Debug.Log($"[FlashbackEffectController] Restaurado objeto: {objectsToActivateInFlashback[i].name} para {originalActivateStates[i]}");
                }
            }
        }

        // Restaura objetos que foram desativados no flashback
        if (objectsToDeactivateInFlashback != null && originalDeactivateStates != null)
        {
            for (int i = 0; i < objectsToDeactivateInFlashback.Length; i++)
            {
                if (objectsToDeactivateInFlashback[i] != null && i < originalDeactivateStates.Length)
                {
                    objectsToDeactivateInFlashback[i].SetActive(originalDeactivateStates[i]);
                    Debug.Log($"[FlashbackEffectController] Restaurado objeto: {objectsToDeactivateInFlashback[i].name} para {originalDeactivateStates[i]}");
                }
            }
        }
    }

    #endregion

}