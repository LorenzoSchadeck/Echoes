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
    [SerializeField] private float entryDuration = 3.0f;
    [Tooltip("Pico máximo do Post Exposure (efeito de 'clarão').")]
    [SerializeField] private float exposurePeak = 2.0f;

    [Tooltip("Curva para a fase de 'puxar' da lente (de 0 a -1). Duração = metade da transição total.")]
    [SerializeField] private AnimationCurve lensPullCurve;
    [Tooltip("Curva para a fase de 'empurrar' da lente (de -1 a 1). Duração = metade da transição total.")]
    [SerializeField] private AnimationCurve lensPushCurve;
    
    [Header("Profile Dependencies")]
    [Tooltip("Referência ao PostProcessingManager para obter valores de perfil.")]
    [SerializeField] private PostProcessingManager postProcessingManager;

    private LensDistortion lensDistortion;
    private ColorAdjustments colorAdjustments;

    private Coroutine activeFlashbackRoutine;

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
    }

    private void OnEnable()
    {
        GameEvents.OnFlashbackStarted += PlayFlashbackEntrySequence;
    }

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= PlayFlashbackEntrySequence;
    }

    private void PlayFlashbackEntrySequence()
    {
        GameObject teleportPointObject = GameObject.FindWithTag("FlashbackTeleport");
        if (teleportPointObject == null)
        {
            Debug.LogError("Nenhum GameObject com a tag 'FlashbackTeleport' foi encontrado na cena!");
            return;
        }

        if (activeFlashbackRoutine != null) StopCoroutine(activeFlashbackRoutine);
        activeFlashbackRoutine = StartCoroutine(FlashbackEntryRoutine(teleportPointObject.transform));
    }

    private IEnumerator FlashbackEntryRoutine(Transform teleportDestination)
    {
        Debug.Log("Iniciando sequência de entrada do flashback...");

        float originalExposure = colorAdjustments.postExposure.value;
        float targetFlashbackExposure = postProcessingManager.GetFlashbackProfileExposure();

        float halfDuration = entryDuration / 2f;
        float elapsedTime = 0f;

        // --- FASE 1: EFEITO DE PUXAR (PULL) ---
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration; // Progresso de 0 a 1 para a primeira fase

            // Lens Distortion usa a primeira curva
            lensDistortion.intensity.value = lensPullCurve.Evaluate(t);
            
            // Exposure vai para o pico (clarão)
            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, exposurePeak, t);
            
            yield return null;
        }

        // --- TELEPORTE ---
        playerRigidbody.position = teleportDestination.position;
        // ... (resto da lógica de teleporte)

        // --- FASE 2: EFEITO DE EMPURRAR (PUSH) ---
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration; // Progresso de 0 a 1 para a segunda fase

            // Lens Distortion usa a segunda curva
            lensDistortion.intensity.value = lensPushCurve.Evaluate(t);

            // Exposure vai do pico para o valor final
            colorAdjustments.postExposure.value = Mathf.Lerp(exposurePeak, targetFlashbackExposure, t);

            yield return null;
        }

        // --- FINALIZAÇÃO ---
        lensDistortion.intensity.value = 0f;
        colorAdjustments.postExposure.value = targetFlashbackExposure;
        activeFlashbackRoutine = null;
        Debug.Log("Sequência de entrada do flashback concluída.");
    }
}