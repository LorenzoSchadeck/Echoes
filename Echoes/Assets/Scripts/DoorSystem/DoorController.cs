using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using FMODUnity;
public class DoorController : MonoBehaviour, IInteractable
{
    public enum DoorState { Unlocked, Locked, Jammed }

    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que esta porta pode ser interagida")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Header("State Settings")]
    [SerializeField] private DoorState currentState = DoorState.Unlocked;

    [Header("Localization")]
    [SerializeField] private LocalizedString openPrompt;
    [SerializeField] private LocalizedString closePrompt;
    [SerializeField] private LocalizedString lockedPrompt;
    [SerializeField] private LocalizedString movingPrompt;

    [Header("Movement Settings")]
    [SerializeField] private float openDuration = 2.368f;
    [SerializeField] private float closeDuration = 2.53f;
    [Tooltip("The absolute angle the door will open (e.g., 90). The direction will be determined automatically.")]
    [SerializeField] private float fullOpenAngle = 90.0f;
    [SerializeField] private float jammedOpenAngle = 25.0f;
    [SerializeField] private bool openToPositiveSide = true;

    [Header("Hierarchy")]
    [Tooltip("The pivot object around which the door rotates. Usually the empty parent.")]
    [SerializeField] private Transform pivot;

    [Header("Sons FMOD")]
    [SerializeField] private EventReference openEvent;
    [SerializeField] private EventReference closeEvent;
    [SerializeField] private EventReference lockedEvent;
    [SerializeField] private EventReference jammedEvent;
    
    [Header("Batida na Porta")]
    [Tooltip("Som 2D que toca quando alguém bate na porta (disparado pelo rádio)")]
    [SerializeField] private EventReference doorKnockEvent;
    [Tooltip("Se deve responder aos eventos de batida na porta")]
    [SerializeField] private bool respondToDoorKnock = false;

    [Header("🎵 Choir System")]
    [Tooltip("Se esta porta faz parte do sistema de choir")]
    [SerializeField] private bool isChoirDoor = false;
    
    [Tooltip("Som alto tocado quando a porta bate ao fechar no choir")]
    [SerializeField] private EventReference choirSlamSound;
    
    [Tooltip("Tempo que a porta fica aberta antes de fechar no choir")]
    [SerializeField] private float choirOpenDuration = 1.5f;
    
    [Tooltip("Delay antes de disparar o flashback após o som da porta")]
    [SerializeField] private float flashbackDelay = 0.5f;
    
    [Header("🔒 Key System")]
    [Tooltip("Se esta porta requer uma chave para ser destrancada")]
    [SerializeField] private bool requiresKey = false;
    [Tooltip("ID da chave necessária para abrir esta porta (deve corresponder ao ID da chave)")]
    [SerializeField] private string requiredKeyID;

    private FMODAudioTrigger audioTrigger;
    private Quaternion initialRotation;
    private bool isOpen = false;
    private bool isMoving = false;
    private bool canChoirActivate = false; // Controle para ativação via choir
    

    public string InteractionPrompt
    {
        get
        {
            if (isMoving) return movingPrompt.GetLocalizedString();
            
            // Prompt especial para choir doors quando ativadas e puzzle não completado
            if (isChoirDoor && canChoirActivate)
            {
                // Verifica se o puzzle foi completado
                bool isChoirComplete = ChoirManager.Instance != null && ChoirManager.Instance.IsChoirComplete;
                if (!isChoirComplete)
                {
                    return openPrompt.GetLocalizedString(); // Usa prompt de abrir durante o puzzle
                }
            }
            
            switch (currentState)
            {
                case DoorState.Locked:
                    return lockedPrompt.GetLocalizedString();
                case DoorState.Jammed:
                    // Portas jammed só mostram prompt se não for choir door ou se choir ativou
                    if (!isChoirDoor || canChoirActivate)
                        return isOpen ? closePrompt.GetLocalizedString() : openPrompt.GetLocalizedString();
                    return string.Empty;
                case DoorState.Unlocked:
                default:
                    return isOpen ? closePrompt.GetLocalizedString() : openPrompt.GetLocalizedString();
            }
        }
    }
    
    public float InteractionDistance => interactionDistance;

    private void Awake()
    {
        audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
        if (pivot == null)
        {
            Debug.LogWarning("Pivot da porta não foi definido, tentando usar o pai.", this);
            pivot = transform.parent;
        }
        initialRotation = pivot.rotation;
    }

    private void OnEnable()
    {
        if (respondToDoorKnock)
        {
            GameEvents.OnDoorKnockTriggered += OnDoorKnockReceived;
        }
    }

    private void OnDisable()
    {
        if (respondToDoorKnock)
        {
            GameEvents.OnDoorKnockTriggered -= OnDoorKnockReceived;
        }
    }

    public bool Interact(Transform interactor)
    {
        if (isMoving) return false;

        if (isOpen)
        {
            MoveDoor(0, closeEvent);
            return true;
        }

        float direction = openToPositiveSide ? 1f : -1f;

        switch (currentState)
        {
            case DoorState.Unlocked:
                MoveDoor(fullOpenAngle * direction, openEvent);
                return true;
            case DoorState.Locked:
                // Verifica se está trancada por chave
                if (IsLockedByKey())
                {
                    // Se o jogador tem a chave, destrava
                    if (HasRequiredKey())
                    {
                        Debug.Log($"[DoorController] Chave '{requiredKeyID}' usada! Destrancando porta {gameObject.name}");
                        currentState = DoorState.Unlocked;
                        MoveDoor(fullOpenAngle * direction, openEvent);
                        return true;
                    }
                    else
                    {
                        // Sem a chave, toca som de trancado
                        PlayFMODSound(lockedEvent);
                        return false;
                    }
                }
                else
                {
                    // Trancada por outro motivo
                    PlayFMODSound(lockedEvent);
                    return false;
                }
            case DoorState.Jammed:
                // Lógica especial para choir doors
                if (isChoirDoor)
                {
                    return HandleChoirDoorInteraction(direction);
                }
                else
                {
                    // Comportamento normal para portas jammed
                    MoveDoor(jammedOpenAngle * direction, jammedEvent);
                    return true;
                }
        }
        return false;
    }

    private void MoveDoor(float targetAngle, EventReference movementEvent)
    {
        Quaternion targetRotation = isOpen ? initialRotation : initialRotation * Quaternion.Euler(0, 0, targetAngle);
        float duration = isOpen ? closeDuration : openDuration;
        bool isJammedDoor = (currentState == DoorState.Jammed && !isOpen);
        StartCoroutine(AnimateDoor(targetRotation, movementEvent, duration, isJammedDoor));
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation, EventReference movementEvent, float duration, bool isJammedDoor = false)
    {
        isMoving = true;
        PlayFMODSound(movementEvent);

        Quaternion currentRotation = pivot.rotation;
        float elapsedTime = 0f;
        bool soundStopped = false;

        while (elapsedTime < duration)
        {
            float normalizedTime = elapsedTime / duration;
            pivot.rotation = Quaternion.Slerp(currentRotation, targetRotation, normalizedTime);
            
            // Para portas jammed, para o som quando atingir o ângulo máximo (100% da animação)
            if (isJammedDoor && !soundStopped && normalizedTime >= 1.0f)
            {
                StopFMODSound();
                soundStopped = true;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        pivot.rotation = targetRotation;

        // Garante que o som pare se ainda não parou (para portas jammed)
        if (isJammedDoor && !soundStopped)
        {
            StopFMODSound();
        }

        isOpen = targetRotation != initialRotation;
        isMoving = false;
    }

    /// <summary>
    /// Gerencia a interação especial para portas do choir
    /// </summary>
    private bool HandleChoirDoorInteraction(float direction)
    {
        // Só permite interação se o choir ativou esta porta
        if (!canChoirActivate)
        {
            Debug.Log($"[DoorController] Porta do choir {gameObject.name} ainda não foi ativada pelo sistema");
            return false;
        }

        // Verifica se o choir já foi completado - se sim, bloqueia interação
        if (ChoirManager.Instance != null && ChoirManager.Instance.IsChoirComplete)
        {
            Debug.Log($"[DoorController] Porta do choir {gameObject.name} - Puzzle completado, interação bloqueada");
            return false;
        }

        // Durante o puzzle, permite sempre iniciar o flashback
        Debug.Log($"[DoorController] 🚪 Iniciando sequência do choir na porta {gameObject.name}");

        // Inicia a sequência especial do choir
        StartCoroutine(ChoirDoorSequence(direction));
        
        return true;
    }

    /// <summary>
    /// Sequência especial para portas do choir: abre, aguarda, fecha rapidamente, som alto, flashback
    /// </summary>
    private IEnumerator ChoirDoorSequence(float direction)
    {
        Debug.Log("[DoorController] 🔄 Iniciando sequência da porta do choir");

        // 1. Abre a porta (movimento jammed)
        float targetAngle = jammedOpenAngle * direction;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, targetAngle);
        
        yield return StartCoroutine(AnimateDoor(targetRotation, jammedEvent, openDuration, true));
        
        Debug.Log($"[DoorController] ⬆️ Porta aberta - Aguardando {choirOpenDuration}s");

        // 2. Aguarda o tempo configurado
        yield return new WaitForSeconds(choirOpenDuration);

        // 3. Fecha a porta rapidamente
        Debug.Log("[DoorController] ⬇️ Fechando porta rapidamente");
        yield return StartCoroutine(AnimateDoor(initialRotation, jammedEvent, closeDuration, false));

        // 4. Toca som alto de batida
        Debug.Log("[DoorController] 💥 Tocando som de batida da porta");
        PlayChoirSlamSound();

        // 5. Aguarda delay e dispara flashback
        Debug.Log($"[DoorController] ⏱️ Aguardando {flashbackDelay}s antes do flashback");
        yield return new WaitForSeconds(flashbackDelay);

        // 6. Dispara flashback
        Debug.Log("[DoorController] ✨ Disparando flashback do choir");
        GameEvents.TriggerFlashbackStarted();

        Debug.Log("[DoorController] ✅ Sequência da porta do choir completada");
    }

    /// <summary>
    /// Reproduz o som alto de batida da porta no choir
    /// </summary>
    private void PlayChoirSlamSound()
    {
        if (choirSlamSound.IsNull)
        {
            Debug.LogWarning("[DoorController] Som de batida do choir não configurado!");
            return;
        }

        // Cria instância do som de batida
        var slamInstance = FMODUnity.RuntimeManager.CreateInstance(choirSlamSound);
        
        if (slamInstance.isValid())
        {
            // Define posicionamento 3D
            slamInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            
            // Define range máximo (mesmo do rádio)
            slamInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 70f);
            
            // Aumenta volume para enfatizar o som alto
            slamInstance.setVolume(1.0f);
            
            // Inicia reprodução
            slamInstance.start();
            
            // Auto-release quando terminar
            slamInstance.release();
            
            Debug.Log("[DoorController] 💥 Som de batida do choir tocado com range 70m");
        }
        else
        {
            Debug.LogError("[DoorController] Falha ao criar instância do som de batida do choir!");
        }
    }

    private void PlayFMODSound(EventReference evt)
    {
        if (evt.IsNull) return;
        audioTrigger.fmodEvent = evt;
        audioTrigger.PlayAtPosition(transform.position);
    }

    private void StopFMODSound()
    {
        if (audioTrigger != null)
        {
            audioTrigger.Stop();
        }
    }

    /// <summary>
    /// Responde ao evento de batida na porta disparado pelo rádio
    /// </summary>
    private void OnDoorKnockReceived()
    {
        if (!respondToDoorKnock || doorKnockEvent.IsNull) return;

        Debug.Log($"DoorController: Recebido evento de batida na porta - {gameObject.name}");
        
        // Toca o som de batida na porta com range de 70m
        FMOD.Studio.EventInstance knockInstance = FMODUnity.RuntimeManager.CreateInstance(doorKnockEvent);
        
        if (knockInstance.isValid())
        {
            // Define posição 3D e range máximo de 70m
            knockInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            knockInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 70f);
            knockInstance.start();
            knockInstance.release(); // Libera automaticamente quando terminar
        }
    }

    public void LockDoor() { currentState = DoorState.Locked; }
    public void UnlockDoor() { currentState = DoorState.Unlocked; }
    public void JamDoor() { currentState = DoorState.Jammed; }
    
    /// <summary>
    /// Verifica se a porta está trancada por falta de chave
    /// </summary>
    private bool IsLockedByKey()
    {
        return requiresKey && !string.IsNullOrEmpty(requiredKeyID);
    }
    
    /// <summary>
    /// Verifica se o jogador possui a chave necessária
    /// </summary>
    private bool HasRequiredKey()
    {
        if (!requiresKey || string.IsNullOrEmpty(requiredKeyID))
            return true; // Se não requer chave, sempre tem "acesso"
        
        return DoorKeyManager.Instance.HasKey(requiredKeyID);
    }

    /// <summary>
    /// Ativa esta porta para o sistema de choir (chamado pelo ChoirManager)
    /// </summary>
    public void ActivateChoirDoor()
    {
        if (!isChoirDoor)
        {
            Debug.LogWarning($"[DoorController] Tentativa de ativar porta {gameObject.name} que não é choir door!");
            return;
        }

        if (canChoirActivate)
        {
            Debug.LogWarning($"[DoorController] Porta do choir {gameObject.name} já estava ativada!");
            return;
        }

        // Quando o choir ativa, a porta vira jammed e pode ser parte do puzzle
        currentState = DoorState.Jammed;
        canChoirActivate = true;
        Debug.Log($"[DoorController] 🚪 Porta do choir {gameObject.name} ativada - Agora está jammed e pode ser interagida para o flashback");
    }

    /// <summary>
    /// Reset da porta do choir para permitir nova ativação
    /// </summary>
    public void ResetChoirDoor()
    {
        if (!isChoirDoor) return;

        // A porta permanece jammed e pode ser reutilizada
        // Não precisa resetar estados, pois o comportamento é sempre o mesmo durante o puzzle
        Debug.Log($"[DoorController] 🔄 Porta do choir {gameObject.name} pronta para nova interação");
    }

    /// <summary>
    /// Propriedades públicas para verificação de estado
    /// </summary>
    public bool IsChoirDoor => isChoirDoor;
    public bool CanChoirActivate => canChoirActivate;
}