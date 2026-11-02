using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

/// <summary>
/// Transição de entrada e saída de uma lembrança (flashback): puxão da lente e clarão, teleporte do
/// jogador para o ponto da lembrança, troca de objetos da cena e o caminho de volta.
/// Cada tipo de lembrança é uma subclasse que diz quando age, para onde teleporta e o que faz ao sair.
/// </summary>
public abstract class MemoryTransitionController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] protected PostProcessingManager postProcessingManager;

    [Header("Transition Effects")]
    [Tooltip("Duração total da animação de entrada (e de saída) da lembrança.")]
    [SerializeField] private float animationDuration = 3.0f;
    [Tooltip("Pico máximo do Post Exposure (efeito de 'clarão').")]
    [SerializeField] private float exposurePeak = 2.0f;
    [Tooltip("Curva para a fase de 'puxar' da lente (de 0 a -1). Duração = metade da transição total.")]
    [SerializeField] private AnimationCurve lensPullCurve;
    [Tooltip("Curva para a fase de 'empurrar' da lente (de -1 a 1). Duração = metade da transição total.")]
    [SerializeField] private AnimationCurve lensPushCurve;

    [Header("🔊 Audio Settings")]
    [Tooltip("Som 2D tocado quando o jogador entra na lembrança.")]
    [FormerlySerializedAs("flashbackEntrySoundEvent"), FormerlySerializedAs("choirFlashbackEntrySoundEvent")]
    [SerializeField] private EventReference entrySoundEvent;
    [Tooltip("Som 2D tocado quando o jogador sai da lembrança.")]
    [FormerlySerializedAs("flashbackExitSoundEvent"), FormerlySerializedAs("choirFlashbackExitSoundEvent")]
    [SerializeField] private EventReference exitSoundEvent;

    [Header("📦 GameObject Control")]
    [Tooltip("GameObjects ATIVADOS quando o jogador entra na lembrança.")]
    [FormerlySerializedAs("objectsToActivateInFlashback"), FormerlySerializedAs("choirObjectsToActivate")]
    [SerializeField] private GameObject[] objectsToActivate;
    [Tooltip("GameObjects DESATIVADOS quando o jogador entra na lembrança.")]
    [FormerlySerializedAs("objectsToDeactivateInFlashback"), FormerlySerializedAs("choirObjectsToDeactivate")]
    [SerializeField] private GameObject[] objectsToDeactivate;

    private LensDistortion lensDistortion;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private Coroutine activeAnimationCoroutine;

    // Estado ativo original dos objetos controlados, para restaurar na saída
    private bool[] originalActivateStates;
    private bool[] originalDeactivateStates;

    public bool IsActive { get; private set; }

    /// <summary>
    /// Tag do ponto para onde o jogador é teleportado ao entrar.
    /// </summary>
    protected abstract string TeleportTag { get; }

    /// <summary>
    /// Se esta lembrança deve reagir ao flashback que acabou de começar.
    /// </summary>
    protected abstract bool ShouldHandleFlashback();

    /// <summary>
    /// Chamado no início da entrada, antes da animação.
    /// </summary>
    protected virtual void OnEntryStarted() { }

    /// <summary>
    /// Chamado ao fim da saída, com o jogador de volta e o pós-processo restaurado.
    /// </summary>
    protected virtual void OnExitCompleted() { }

    protected virtual void Awake()
    {
        if (postProcessVolume == null || postProcessVolume.profile == null || playerTransform == null || playerRigidbody == null || postProcessingManager == null)
        {
            Debug.LogError($"[{GetType().Name}] Uma ou mais dependências cruciais não foram atribuídas!", this);
            enabled = false;
            return;
        }

        if (!postProcessVolume.profile.TryGet(out lensDistortion)) Debug.LogWarning($"[{GetType().Name}] Lens Distortion não encontrado no Volume.");
        if (!postProcessVolume.profile.TryGet(out colorAdjustments)) Debug.LogWarning($"[{GetType().Name}] Color Adjustments não encontrado no Volume.");
        if (!postProcessVolume.profile.TryGet(out vignette)) Debug.LogWarning($"[{GetType().Name}] Vignette não encontrado no Volume.");

        originalActivateStates = CaptureActiveStates(objectsToActivate);
        originalDeactivateStates = CaptureActiveStates(objectsToDeactivate);
    }

    protected virtual void OnEnable()
    {
        GameEvents.OnFlashbackStarted += HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded += HandleFlashbackEnded;
    }

    protected virtual void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded -= HandleFlashbackEnded;
    }

    private void HandleFlashbackStarted()
    {
        if (IsActive || !ShouldHandleFlashback()) return;

        GameObject teleportPoint = GameObject.FindWithTag(TeleportTag);
        if (teleportPoint == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Ponto de teleporte com a tag '{TeleportTag}' não encontrado - flashback ignorado");
            return;
        }

        // Salva a posição exata do jogador antes de qualquer animação
        originalPlayerPosition = playerTransform.position;
        originalPlayerRotation = playerTransform.rotation;
        IsActive = true;

        OnEntryStarted();
        PlaySound2D(entrySoundEvent);
        SetActive(objectsToActivate, true);
        SetActive(objectsToDeactivate, false);
        StartAnimation(EntryRoutine(teleportPoint.transform));
    }

    private void HandleFlashbackEnded()
    {
        if (!IsActive) return;
        IsActive = false;

        postProcessingManager.NotifyFlashbackExitStarted();
        postProcessingManager.StopAllVisualEffects();
        PlaySound2D(exitSoundEvent);
        RestoreActiveStates(objectsToActivate, originalActivateStates);
        RestoreActiveStates(objectsToDeactivate, originalDeactivateStates);
        StartAnimation(ExitRoutine());
    }

    private void StartAnimation(IEnumerator routine)
    {
        if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
        activeAnimationCoroutine = StartCoroutine(routine);
    }

    private IEnumerator EntryRoutine(Transform teleportDestination)
    {
        PlayerControlLock.Acquire(this);

        float originalExposure = colorAdjustments.postExposure.value;
        float targetExposure = postProcessingManager.GetFlashbackProfileExposure();
        float halfDuration = animationDuration / 2f;

        // FASE 1: puxão da lente e clarão
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = elapsed / halfDuration;
            lensDistortion.intensity.value = lensPullCurve.Evaluate(t);
            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, exposurePeak, t);
            yield return null;
        }

        TeleportPlayer(teleportDestination.position, teleportDestination.rotation);

        // FASE 2: empurrão da lente e fim do clarão
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = elapsed / halfDuration;
            lensDistortion.intensity.value = lensPushCurve.Evaluate(t);
            colorAdjustments.postExposure.value = Mathf.Lerp(exposurePeak, targetExposure, t);
            yield return null;
        }

        lensDistortion.intensity.value = 0f;

        PlayerControlLock.Release(this);
        activeAnimationCoroutine = null;
    }

    private IEnumerator ExitRoutine()
    {
        PlayerControlLock.Acquire(this);

        float targetExposure = postProcessingManager.GetSaneProfileExposure();
        float targetVignetteIntensity = postProcessingManager.GetSaneProfileVignetteIntensity();
        float targetLensDistortionScale = postProcessingManager.GetSaneProfileLensDistortionScale();

        float originalExposure = colorAdjustments.postExposure.value;
        float originalVignetteIntensity = vignette.intensity.value;
        float halfDuration = animationDuration / 2f;

        // FASE 1: empurrão (intensidade 0 -> 1); scale > 1 dá zoom e esconde as bordas pretas
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = elapsed / halfDuration;
            lensDistortion.intensity.value = Mathf.Lerp(0f, 1f, t);
            lensDistortion.scale.value = Mathf.Lerp(1f, 1.5f, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, exposurePeak, t);
            vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, targetVignetteIntensity, t);
            yield return null;
        }

        TeleportPlayer(originalPlayerPosition, originalPlayerRotation);

        // FASE 2: resolução (intensidade 1 -> 0)
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = elapsed / halfDuration;
            lensDistortion.intensity.value = Mathf.Lerp(1f, 0f, t);
            lensDistortion.scale.value = Mathf.Lerp(1.5f, targetLensDistortionScale, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(exposurePeak, targetExposure, t);
            yield return null;
        }

        lensDistortion.intensity.value = 0f;
        lensDistortion.scale.value = targetLensDistortionScale;
        colorAdjustments.postExposure.value = targetExposure;
        vignette.intensity.value = targetVignetteIntensity;

        postProcessingManager.NotifyFlashbackExitCompleted();
        OnExitCompleted();

        PlayerControlLock.Release(this);
        activeAnimationCoroutine = null;
    }

    // Move Transform e Rigidbody juntos para o teleporte ser exato
    private void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        playerTransform.SetPositionAndRotation(position, rotation);
        playerRigidbody.position = position;
        playerRigidbody.rotation = rotation;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    private void PlaySound2D(EventReference soundEvent)
    {
        if (soundEvent.IsNull) return;

        var instance = RuntimeManager.CreateInstance(soundEvent);
        instance.start();
        instance.release();
    }

    private static bool[] CaptureActiveStates(GameObject[] objects)
    {
        if (objects == null) return null;
        return System.Array.ConvertAll(objects, obj => obj != null && obj.activeInHierarchy);
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject obj in objects)
        {
            if (obj != null) obj.SetActive(active);
        }
    }

    private static void RestoreActiveStates(GameObject[] objects, bool[] states)
    {
        if (objects == null || states == null) return;
        for (int i = 0; i < objects.Length && i < states.Length; i++)
        {
            if (objects[i] != null) objects[i].SetActive(states[i]);
        }
    }
}
