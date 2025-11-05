using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using FMODUnity;

/// <summary>
/// Controlador específico para flashbacks do sistema de choir.
/// Similar ao FlashbackEffectController mas com teleporte próprio (tag "ChoirTeleport")
/// e controle independente de objetos da cena.
/// </summary>
public class ChoirFlashbackController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private PostProcessingManager postProcessingManager;

    [Header("Transition Effects")]
    [Tooltip("Duração total da animação de entrada no flashback do choir")]
    [SerializeField] private float animationDuration = 3.0f;
    [Tooltip("Pico máximo do Post Exposure (efeito de 'clarão')")]
    [SerializeField] private float exposurePeak = 2.0f;
    [Tooltip("Curva para a fase de 'puxar' da lente (de 0 a -1). Duração = metade da transição total")]
    [SerializeField] private AnimationCurve lensPullCurve;
    [Tooltip("Curva para a fase de 'empurrar' da lente (de -1 a 1). Duração = metade da transição total")]
    [SerializeField] private AnimationCurve lensPushCurve;

    [Header("🔊 Audio Settings")]
    [Tooltip("Som 2D tocado quando o jogador entra no flashback do choir")]
    [SerializeField] private EventReference choirFlashbackEntrySoundEvent;
    [Tooltip("Som 2D tocado quando o jogador sai do flashback do choir")]
    [SerializeField] private EventReference choirFlashbackExitSoundEvent;

    [Header("📦 Choir GameObject Control")]
    [Tooltip("GameObjects que serão ATIVADOS quando o jogador entrar no flashback do choir")]
    [SerializeField] private GameObject[] choirObjectsToActivate;
    [Tooltip("GameObjects que serão DESATIVADOS quando o jogador entrar no flashback do choir")]
    [SerializeField] private GameObject[] choirObjectsToDeactivate;

    // Componentes de post-processing
    private LensDistortion lensDistortion;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    // Estado e controle
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private Coroutine activeAnimationCoroutine;
    private bool isChoirFlashbackActive = false;

    // Controle de estado dos GameObjects do choir
    private bool[] originalChoirActivateStates;
    private bool[] originalChoirDeactivateStates;

    private void Awake()
    {
        // SEGURANÇA: Garante que o movimento do jogador esteja liberado ao carregar a cena
        PlayerMovement.canMove = true;
        
        // Validação de dependências
        if (postProcessVolume == null || postProcessVolume.profile == null || 
            playerTransform == null || playerRigidbody == null || 
            postProcessingManager == null)
        {
            Debug.LogError("[ChoirFlashbackController] Uma ou mais dependências cruciais não foram atribuídas!", this);
            enabled = false;
            return;
        }

        // Obtém componentes de post-processing
        if (!postProcessVolume.profile.TryGet(out lensDistortion)) 
            Debug.LogWarning("[ChoirFlashbackController] Lens Distortion não encontrado no Volume.");
        if (!postProcessVolume.profile.TryGet(out colorAdjustments)) 
            Debug.LogWarning("[ChoirFlashbackController] Color Adjustments não encontrado no Volume.");
        if (!postProcessVolume.profile.TryGet(out vignette)) 
            Debug.LogWarning("[ChoirFlashbackController] Vignette não encontrado no Volume.");

        // Inicializa arrays de controle de GameObjects
        InitializeChoirGameObjectArrays();
    }

    private void OnEnable()
    {
        // Escuta eventos específicos do choir flashback
        // O ChoirManager pode disparar eventos específicos para o choir
        GameEvents.OnFlashbackStarted += OnChoirFlashbackTriggered;
        GameEvents.OnFlashbackEnded += OnChoirFlashbackEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= OnChoirFlashbackTriggered;
        GameEvents.OnFlashbackEnded -= OnChoirFlashbackEnded;
    }

    /// <summary>
    /// Chamado quando um flashback é disparado - verifica se é do choir
    /// </summary>
    private void OnChoirFlashbackTriggered()
    {
        // Verifica se existe um ponto de teleporte do choir
        GameObject choirTeleportPoint = GameObject.FindWithTag("ChoirTeleport");
        
        // NOVA LÓGICA: Verifica se o ChoirManager está ativo
        bool isChoirActive = ChoirManager.Instance != null && ChoirManager.Instance.IsChoirActive;
        
        // Só processa se:
        // 1. Encontrou o teleporte do choir
        // 2. Não estamos já em flashback do choir  
        // 3. O choir está ativo (indica que veio do sistema de choir)
        if (choirTeleportPoint != null && !isChoirFlashbackActive && isChoirActive)
        {
            Debug.Log("[ChoirFlashbackController] 🎭 Flashback do choir detectado - Iniciando animação");
            
            // CRÍTICO: Salva a posição EXATA do jogador ANTES de qualquer animação
            originalPlayerPosition = playerTransform.position;
            originalPlayerRotation = playerTransform.rotation;
            Debug.Log($"[ChoirFlashbackController] 📍 Posição original salva: {originalPlayerPosition}");
            
            isChoirFlashbackActive = true;
            PlayChoirEntryAnimation(choirTeleportPoint.transform);
        }
        else
        {
            Debug.Log($"[ChoirFlashbackController] ❌ Flashback ignorado - ChoirTeleport: {choirTeleportPoint != null}, " +
                     $"ChoirActive: {isChoirActive}, FlashbackActive: {isChoirFlashbackActive}");
        }
    }

    /// <summary>
    /// Chamado quando o flashback encerra
    /// </summary>
    private void OnChoirFlashbackEnded()
    {
        // Só processa se estivermos em flashback do choir
        if (isChoirFlashbackActive)
        {
            Debug.Log("[ChoirFlashbackController] 🎭 Encerrando flashback do choir");
            isChoirFlashbackActive = false;
            PlayChoirExitAnimation();
        }
        else
        {
            Debug.Log("[ChoirFlashbackController] ❌ Evento de fim de flashback ignorado - não estávamos em flashback do choir");
        }
    }

    /// <summary>
    /// Inicia a animação de entrada no flashback do choir
    /// </summary>
    private void PlayChoirEntryAnimation(Transform teleportDestination)
    {
        Debug.Log("[ChoirFlashbackController] 🎬 Iniciando animação de entrada do choir flashback");
        
        // Dispara evento de remédio para curar sanidade (igual ao flashback normal)
        Debug.Log("[ChoirFlashbackController] 💊 Disparando evento de remédio para curar sanidade");
        GameEvents.TriggerRemedyUsed();
        
        // Toca som de entrada
        PlayChoirFlashbackEntrySound();
        
        // Configura objetos específicos do choir
        SetChoirFlashbackObjects();
        
        // Inicia animação
        StartChoirAnimation(ChoirFlashbackEntryRoutine(teleportDestination));
    }

    /// <summary>
    /// Inicia a animação de saída do flashback do choir
    /// </summary>
    private void PlayChoirExitAnimation()
    {
        Debug.Log("[ChoirFlashbackController] 🎬 Iniciando animação de saída do choir flashback");
        
        // Notifica o PostProcessingManager
        postProcessingManager.NotifyFlashbackExitStarted();
        postProcessingManager.StopAllVisualEffects();
        
        // Toca som de saída
        PlayChoirFlashbackExitSound();
        
        // Restaura objetos do choir
        RestoreOriginalChoirObjects();
        
        // Inicia animação
        StartChoirAnimation(ChoirFlashbackExitRoutine());
    }

    /// <summary>
    /// Inicia uma animação, parando a anterior se existir
    /// </summary>
    private void StartChoirAnimation(IEnumerator routine)
    {
        if (activeAnimationCoroutine != null) 
        {
            StopCoroutine(activeAnimationCoroutine);
        }
        activeAnimationCoroutine = StartCoroutine(routine);
    }

    /// <summary>
    /// Toca o som 2D de entrada no flashback do choir
    /// </summary>
    private void PlayChoirFlashbackEntrySound()
    {
        if (choirFlashbackEntrySoundEvent.IsNull) return;
        
        try
        {
            var entryInstance = RuntimeManager.CreateInstance(choirFlashbackEntrySoundEvent);
            entryInstance.start();
            entryInstance.release();
            Debug.Log("[ChoirFlashbackController] 🔊 Som de entrada do choir flashback reproduzido");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ChoirFlashbackController] Erro ao tocar som de entrada: {e.Message}");
        }
    }

    /// <summary>
    /// Toca o som 2D de saída do flashback do choir
    /// </summary>
    private void PlayChoirFlashbackExitSound()
    {
        if (choirFlashbackExitSoundEvent.IsNull) return;
        
        try
        {
            var exitInstance = RuntimeManager.CreateInstance(choirFlashbackExitSoundEvent);
            exitInstance.start();
            exitInstance.release();
            Debug.Log("[ChoirFlashbackController] 🔊 Som de saída do choir flashback reproduzido");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ChoirFlashbackController] Erro ao tocar som de saída: {e.Message}");
        }
    }

    /// <summary>
    /// Rotina de animação de entrada no flashback do choir
    /// </summary>
    private IEnumerator ChoirFlashbackEntryRoutine(Transform teleportDestination)
    {
        // NOTA: A posição original já foi salva em OnChoirFlashbackTriggered()
        // Não salva novamente aqui para evitar capturar posições durante animações

        // Trava movimento do jogador durante teleporte
        PlayerMovement.canMove = false;
        Debug.Log("[ChoirFlashbackController] 🔒 Movimento do jogador travado");

        float originalExposure = colorAdjustments.postExposure.value;
        float targetExposure = postProcessingManager.GetFlashbackProfileExposure();
        float halfDuration = animationDuration / 2f;
        float elapsedTime = 0f;

        Debug.Log("[ChoirFlashbackController] 🎬 Fase 1: Puxão e clarão");

        // FASE 1: Puxão e clarão
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            lensDistortion.intensity.value = lensPullCurve.Evaluate(t);
            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, exposurePeak, t);
            yield return null;
        }

        // Teleporte para o ponto do choir
        Debug.Log($"[ChoirFlashbackController] 🚀 Teleportando para: {teleportDestination.name}");
        playerRigidbody.position = teleportDestination.position;
        playerRigidbody.rotation = teleportDestination.rotation;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        Debug.Log("[ChoirFlashbackController] 🎬 Fase 2: Empurrão e fade do clarão");

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

        // Finaliza animação
        lensDistortion.intensity.value = 0f;
        
        // Libera movimento do jogador
        PlayerMovement.canMove = true;
        Debug.Log("[ChoirFlashbackController] 🔓 Movimento do jogador liberado");
        
        activeAnimationCoroutine = null;
        
        Debug.Log("[ChoirFlashbackController] ✅ Animação de entrada do choir flashback concluída");
    }

    /// <summary>
    /// Rotina de animação de saída do flashback do choir
    /// </summary>
    private IEnumerator ChoirFlashbackExitRoutine()
    {
        // Trava movimento do jogador durante teleporte de volta
        PlayerMovement.canMove = false;
        Debug.Log("[ChoirFlashbackController] 🔒 Movimento do jogador travado para saída");

        float targetExposure = postProcessingManager.GetSaneProfileExposure();
        float targetVignetteIntensity = postProcessingManager.GetSaneProfileVignetteIntensity();
        float targetLensDistortionScale = postProcessingManager.GetSaneProfileLensDistortionScale();

        float originalExposure = colorAdjustments.postExposure.value;
        float originalVignetteIntensity = vignette.intensity.value;

        float halfDuration = animationDuration / 2f;
        float elapsedTime = 0f;

        Debug.Log("[ChoirFlashbackController] 🎬 Fase 1: Empurrão (saída)");

        // FASE 1: Empurrão (Intensity 0 -> 1)
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;

            lensDistortion.intensity.value = Mathf.Lerp(0f, 1f, t);
            lensDistortion.scale.value = Mathf.Lerp(1f, 1.5f, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, exposurePeak, t);
            vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, targetVignetteIntensity, t);
            yield return null;
        }

        // TELEPORTE DE VOLTA
        Debug.Log($"[ChoirFlashbackController] 🚀 Teleportando de volta à posição original: {originalPlayerPosition}");
        
        // Usa Transform.position para garantir teleporte exato
        playerTransform.position = originalPlayerPosition;
        playerTransform.rotation = originalPlayerRotation;
        
        // Zera velocidades do Rigidbody
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        
        // Força sincronização da posição do Rigidbody com o Transform
        playerRigidbody.position = originalPlayerPosition;
        playerRigidbody.rotation = originalPlayerRotation;

        Debug.Log("[ChoirFlashbackController] 🎬 Fase 2: Resolução (saída)");

        // FASE 2: Resolução (Intensity 1 -> 0)
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

    // Notifica conclusão da saída
    postProcessingManager.NotifyFlashbackExitCompleted();
    // Garante reset completo do perfil de pós-processamento
    postProcessingManager.ForceResetToSaneState();

    // Permite reutilizar a porta do choir se o puzzle não foi completado
    if (ChoirManager.Instance != null && !ChoirManager.Instance.IsChoirComplete)
    {
        // Reset da porta do choir para permitir nova ativação
        var choirDoor = FindFirstObjectByType<DoorController>();
        if (choirDoor != null && choirDoor.IsChoirDoor)
        {
            choirDoor.ResetChoirDoor();
            Debug.Log("[ChoirFlashbackController] 🔄 Porta do choir resetada para nova tentativa");
        }
    }

    // Libera movimento do jogador
    PlayerMovement.canMove = true;
    Debug.Log("[ChoirFlashbackController] 🔓 Movimento do jogador liberado após saída");

    activeAnimationCoroutine = null;
    Debug.Log("[ChoirFlashbackController] ✅ Animação de saída do choir flashback concluída");
    }

    #region Choir GameObject Control Methods

    /// <summary>
    /// Inicializa os arrays para controle de GameObjects do choir
    /// </summary>
    private void InitializeChoirGameObjectArrays()
    {
        // Inicializa array para objetos que serão ativados no choir
        if (choirObjectsToActivate != null)
        {
            originalChoirActivateStates = new bool[choirObjectsToActivate.Length];
            for (int i = 0; i < choirObjectsToActivate.Length; i++)
            {
                if (choirObjectsToActivate[i] != null)
                {
                    originalChoirActivateStates[i] = choirObjectsToActivate[i].activeInHierarchy;
                }
            }
        }

        // Inicializa array para objetos que serão desativados no choir
        if (choirObjectsToDeactivate != null)
        {
            originalChoirDeactivateStates = new bool[choirObjectsToDeactivate.Length];
            for (int i = 0; i < choirObjectsToDeactivate.Length; i++)
            {
                if (choirObjectsToDeactivate[i] != null)
                {
                    originalChoirDeactivateStates[i] = choirObjectsToDeactivate[i].activeInHierarchy;
                }
            }
        }

        Debug.Log($"[ChoirFlashbackController] Inicializados {originalChoirActivateStates?.Length ?? 0} objetos para ativação " +
                  $"e {originalChoirDeactivateStates?.Length ?? 0} objetos para desativação do choir");
    }

    /// <summary>
    /// Configura objetos específicos para o flashback do choir
    /// </summary>
    private void SetChoirFlashbackObjects()
    {
        Debug.Log("[ChoirFlashbackController] 📦 Configurando objetos do choir flashback");

        // Ativa objetos do choir flashback
        if (choirObjectsToActivate != null)
        {
            for (int i = 0; i < choirObjectsToActivate.Length; i++)
            {
                if (choirObjectsToActivate[i] != null)
                {
                    choirObjectsToActivate[i].SetActive(true);
                    Debug.Log($"[ChoirFlashbackController] ✅ Ativado objeto do choir: {choirObjectsToActivate[i].name}");
                }
            }
        }

        // Desativa objetos normais durante choir flashback
        if (choirObjectsToDeactivate != null)
        {
            for (int i = 0; i < choirObjectsToDeactivate.Length; i++)
            {
                if (choirObjectsToDeactivate[i] != null)
                {
                    choirObjectsToDeactivate[i].SetActive(false);
                    Debug.Log($"[ChoirFlashbackController] ❌ Desativado objeto: {choirObjectsToDeactivate[i].name}");
                }
            }
        }
    }

    /// <summary>
    /// Restaura o estado original dos objetos do choir
    /// </summary>
    private void RestoreOriginalChoirObjects()
    {
        Debug.Log("[ChoirFlashbackController] 🔄 Restaurando objetos originais do choir");

        // Restaura objetos que foram ativados no choir flashback
        if (choirObjectsToActivate != null && originalChoirActivateStates != null)
        {
            for (int i = 0; i < choirObjectsToActivate.Length; i++)
            {
                if (choirObjectsToActivate[i] != null && i < originalChoirActivateStates.Length)
                {
                    choirObjectsToActivate[i].SetActive(originalChoirActivateStates[i]);
                    Debug.Log($"[ChoirFlashbackController] 🔄 Restaurado objeto do choir: {choirObjectsToActivate[i].name} para {originalChoirActivateStates[i]}");
                }
            }
        }

        // Restaura objetos que foram desativados no choir flashback
        if (choirObjectsToDeactivate != null && originalChoirDeactivateStates != null)
        {
            for (int i = 0; i < choirObjectsToDeactivate.Length; i++)
            {
                if (choirObjectsToDeactivate[i] != null && i < originalChoirDeactivateStates.Length)
                {
                    choirObjectsToDeactivate[i].SetActive(originalChoirDeactivateStates[i]);
                    Debug.Log($"[ChoirFlashbackController] 🔄 Restaurado objeto: {choirObjectsToDeactivate[i].name} para {originalChoirDeactivateStates[i]}");
                }
            }
        }
    }

    #endregion

    #region Public Properties and Methods

    /// <summary>
    /// Verifica se o flashback do choir está ativo
    /// </summary>
    public bool IsChoirFlashbackActive => isChoirFlashbackActive;

    /// <summary>
    /// Força o início do flashback do choir (para testes)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceStartChoirFlashback()
    {
        GameObject choirTeleportPoint = GameObject.FindWithTag("ChoirTeleport");
        if (choirTeleportPoint != null && !isChoirFlashbackActive)
        {
            Debug.Log("[ChoirFlashbackController] TESTE: Forçando início do choir flashback");
            OnChoirFlashbackTriggered();
        }
        else
        {
            Debug.Log("[ChoirFlashbackController] TESTE: Não foi possível iniciar choir flashback (teleporte não encontrado ou já ativo)");
        }
    }

    /// <summary>
    /// Força o fim do flashback do choir (para testes)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceEndChoirFlashback()
    {
        if (isChoirFlashbackActive)
        {
            Debug.Log("[ChoirFlashbackController] TESTE: Forçando fim do choir flashback");
            OnChoirFlashbackEnded();
        }
        else
        {
            Debug.Log("[ChoirFlashbackController] TESTE: Choir flashback não está ativo");
        }
    }

    #endregion
}
