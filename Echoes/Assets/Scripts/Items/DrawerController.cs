using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using FMODUnity;

/// <summary>
/// Controlador de gavetas configurável que permite abertura/fechamento suave.
/// Suporta movimento em qualquer eixo com velocidade configurável.
/// Integra com o sistema de áudio FMOD e localização do projeto Echoes.
/// </summary>
public class DrawerController : MonoBehaviour, IInteractable
{
    [Header("🌐 Localization")]
    [Tooltip("Prompt exibido quando a gaveta está fechada")]
    [SerializeField] private LocalizedString openPrompt;
    [Tooltip("Prompt exibido quando a gaveta está aberta")]
    [SerializeField] private LocalizedString closePrompt;

    [Header("⚖️ Interaction Settings")]
    [Tooltip("Distância máxima em que esta gaveta pode ser interagida")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Header("⚙️ Movement Configuration")]
    [Tooltip("Eixo de movimento da gaveta (X, Y ou Z)")]
    [SerializeField] private MovementAxis movementAxis = MovementAxis.Z;
    [Tooltip("Distância em unidades que a gaveta se move quando aberta")]
    [SerializeField] private float openDistance = 0.5f;
    [Tooltip("Se verdadeiro, move no sentido positivo do eixo. Se falso, move no sentido negativo")]
    [SerializeField] private bool moveToPositiveDirection = true;

    [Header("🎛️ Animation Settings")]
    [Tooltip("Duração fixa do movimento em segundos (2s = abertura/fechamento em 2 segundos)")]
    [SerializeField] private float movementDuration = 2f;
    [Tooltip("Curva de animação para o movimento (padrão: suave aceleração/desaceleração)")]
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Permite que a gaveta seja interagida durante o movimento")]
    [SerializeField] private bool allowInteractionDuringMovement = false;

    [Header("🔊 Audio Settings")]
    [Tooltip("Som tocado quando a gaveta abre")]
    [SerializeField] private EventReference openSoundEvent;
    [Tooltip("Som tocado quando a gaveta fecha")]
    [SerializeField] private EventReference closeSoundEvent;
    [Tooltip("Volume relativo dos sons (0.0 a 1.0)")]
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;

    [Header("🚪 Door Dependencies")]
    [Tooltip("Portas do guarda-roupa que devem estar abertas para permitir acesso à gaveta")]
    [SerializeField] private CabinetDoor[] requiredOpenDoors;
    [Tooltip("Mensagem exibida quando as portas não estão abertas")]
    [SerializeField] private LocalizedString doorsClosedPrompt;
    
    [Header("🔄 Door Notifications")]
    [Tooltip("Portas que devem ser notificadas quando esta gaveta abre/fecha (usado apenas para gavetas de armário)")]
    [SerializeField] private CabinetDoor[] doorsToNotify;
    [Tooltip("Se verdadeiro, notifica as portas quando esta gaveta muda de estado")]
    [SerializeField] private bool notifyDoorsOnStateChange = false;
    
    [Header("🔒 Lock System")]
    [Tooltip("Se verdadeiro, esta gaveta requer uma chave para ser aberta")]
    [SerializeField] private bool requiresKey = false;
    [Tooltip("ID da chave necessária para abrir esta gaveta (deve corresponder ao ID da chave)")]
    [SerializeField] private string requiredKeyID;
    [Tooltip("Mensagem exibida quando a gaveta está trancada")]
    [SerializeField] private LocalizedString lockedPrompt;

    [Header("🎯 Debug & Visualization")]
    [Tooltip("Mostra gizmos de debug no editor")]
    [SerializeField] private bool showDebugGizmos = true;
    [Tooltip("Cor dos gizmos de debug")]
    [SerializeField] private Color debugGizmoColor = Color.cyan;

    // Estado interno
    private Vector3 initialPosition;
    private Vector3 targetOpenPosition;
    private bool isOpen = false;
    private bool isMoving = false;
    private FMODAudioTrigger audioTrigger;
    private Coroutine currentMovementCoroutine;

    /// <summary>
    /// Enumeração para os eixos de movimento possíveis
    /// </summary>
    public enum MovementAxis
    {
        X, // Movimento horizontal (direita/esquerda)
        Y, // Movimento vertical (cima/baixo)  
        Z  // Movimento para frente/trás
    }

    // Propriedades da interface IInteractable
    public string InteractionPrompt
    {
        get
        {
            // Se não permite interação durante movimento e está se movendo
            if (!allowInteractionDuringMovement && isMoving) 
                return string.Empty;
            
            // Verifica se a gaveta está trancada
            if (!isOpen && IsLocked())
                return lockedPrompt?.GetLocalizedString() ?? "Trancado - Encontre a chave";
            
            // Verifica se as portas necessárias estão abertas
            if (!AreRequiredDoorsOpen())
                return doorsClosedPrompt?.GetLocalizedString() ?? "Abra as portas do guarda-roupa primeiro";
            
            if (isOpen)
                return closePrompt?.GetLocalizedString() ?? "Fechar Gaveta";
            else
                return openPrompt?.GetLocalizedString() ?? "Abrir Gaveta";
        }
    }
    
    public float InteractionDistance => interactionDistance;

    // Propriedades públicas para acesso por outros scripts
    public bool IsOpen => isOpen;
    public bool IsMoving => isMoving;
    public float OpenDistance => openDistance;
    public float MovementDuration => movementDuration;
    public MovementAxis Axis => movementAxis;
    public bool CanAccess => AreRequiredDoorsOpen();

    #region Unity Lifecycle

    private void Awake()
    {
        // Salva posição inicial
        initialPosition = transform.position;
        
        // Calcula posição aberta baseada no eixo e direção configurados
        CalculateTargetPosition();

        // Configura sistema de áudio FMOD seguindo padrão do projeto
        SetupAudioSystem();
    }

    private void Start()
    {
        // Validações iniciais
        ValidateConfiguration();
    }

    #endregion

    #region IInteractable Implementation

    public bool Interact(Transform interactor)
    {
        // Verifica se pode interagir
        if (!allowInteractionDuringMovement && isMoving) 
            return false;
        
        // Verifica se está trancada (só ao tentar abrir)
        if (!isOpen && IsLocked())
            return false;

        // Verifica se as portas necessárias estão abertas (só para abrir gavetas)
        if (!isOpen && !AreRequiredDoorsOpen())
            return false;

        // Interrompe movimento atual se permitido
        if (isMoving && allowInteractionDuringMovement)
        {
            StopCurrentMovement();
        }

        // Alterna estado da gaveta
        if (isOpen)
            CloseDrawer();
        else
            OpenDrawer();

        return true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Abre a gaveta programaticamente
    /// </summary>
    [ContextMenu("Open Drawer")]
    public void OpenDrawer()
    {
        if (isOpen || (!allowInteractionDuringMovement && isMoving)) return;
        
        // Verifica se está trancada
        if (IsLocked()) return;
        
        // Verifica se as portas necessárias estão abertas
        if (!AreRequiredDoorsOpen()) return;

        StartMovement(targetOpenPosition, openSoundEvent, true);
    }

    /// <summary>
    /// Fecha a gaveta programaticamente
    /// </summary>
    [ContextMenu("Close Drawer")]
    public void CloseDrawer()
    {
        if (!isOpen || (!allowInteractionDuringMovement && isMoving)) return;

        StartMovement(initialPosition, closeSoundEvent, false);
    }

    /// <summary>
    /// Define o estado da gaveta imediatamente sem animação
    /// </summary>
    /// <param name="open">Estado desejado (aberto/fechado)</param>
    /// <param name="playSound">Se deve tocar som da transição</param>
    public void SetDrawerState(bool open, bool playSound = false)
    {
        StopCurrentMovement();

        bool stateChanged = isOpen != open;
        isOpen = open;
        transform.position = open ? targetOpenPosition : initialPosition;
        
        if (playSound)
        {
            EventReference soundToPlay = open ? openSoundEvent : closeSoundEvent;
            PlaySound(soundToPlay);
        }
        
        // Notifica portas se o estado realmente mudou
        if (stateChanged)
        {
            NotifyDoorsOfStateChange();
        }
    }

    /// <summary>
    /// Força a recalculação da posição alvo (útil se os parâmetros mudaram)
    /// </summary>
    public void RecalculateTargetPosition()
    {
        CalculateTargetPosition();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Configura o sistema de áudio FMOD
    /// </summary>
    private void SetupAudioSystem()
    {
        audioTrigger = gameObject.GetComponent<FMODAudioTrigger>();
        if (audioTrigger == null)
            audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
    }

    /// <summary>
    /// Calcula a posição alvo baseada no eixo e configurações
    /// </summary>
    private void CalculateTargetPosition()
    {
        Vector3 movementVector = Vector3.zero;
        float direction = moveToPositiveDirection ? 1f : -1f;
        float actualDistance = openDistance * direction;

        switch (movementAxis)
        {
            case MovementAxis.X:
                movementVector = Vector3.right * actualDistance;
                break;
            case MovementAxis.Y:
                movementVector = Vector3.up * actualDistance;
                break;
            case MovementAxis.Z:
                movementVector = Vector3.forward * actualDistance;
                break;
        }

        targetOpenPosition = initialPosition + movementVector;
    }

    /// <summary>
    /// Inicia o movimento da gaveta
    /// </summary>
    private void StartMovement(Vector3 targetPosition, EventReference soundEvent, bool willBeOpen)
    {
        StopCurrentMovement();
        currentMovementCoroutine = StartCoroutine(MoveDrawerCoroutine(targetPosition, soundEvent, willBeOpen));
    }

    /// <summary>
    /// Para o movimento atual se existir
    /// </summary>
    private void StopCurrentMovement()
    {
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
            currentMovementCoroutine = null;
            isMoving = false;
        }
    }

    /// <summary>
    /// Corrotina que executa o movimento suave da gaveta
    /// </summary>
    private IEnumerator MoveDrawerCoroutine(Vector3 targetPosition, EventReference soundEvent, bool willBeOpen)
    {
        isMoving = true;

        // Toca som se configurado
        PlaySound(soundEvent);

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        // Anima a posição usando duração fixa
        while (elapsedTime < movementDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / movementDuration;
            
            // Aplica curva de animação
            float curveValue = movementCurve.Evaluate(t);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            yield return null;
        }

        // Garante posição final exata
        transform.position = targetPosition;
        isOpen = willBeOpen;
        isMoving = false;
        currentMovementCoroutine = null;
        
        // Notifica portas sobre mudança de estado se configurado
        NotifyDoorsOfStateChange();
    }

    /// <summary>
    /// Toca um som FMOD se configurado
    /// </summary>
    private void PlaySound(EventReference soundEvent)
    {
        if (soundEvent.IsNull || audioTrigger == null) return;

        audioTrigger.fmodEvent = soundEvent;
        
        // Aplica volume se diferente de 1.0
        if (audioVolume != 1f)
        {
            audioTrigger.SetParameter("Volume", audioVolume);
        }
        
        audioTrigger.PlayAtPosition(transform.position);
    }

    /// <summary>
    /// Notifica as portas configuradas sobre mudança de estado da gaveta
    /// </summary>
    private void NotifyDoorsOfStateChange()
    {
        // Só notifica se estiver configurado para isso
        if (!notifyDoorsOnStateChange || doorsToNotify == null || doorsToNotify.Length == 0)
            return;

        foreach (CabinetDoor door in doorsToNotify)
        {
            if (door == null)
            {
                Debug.LogWarning($"[DrawerController] {gameObject.name}: Porta nula encontrada na lista de portas para notificar!", this);
                continue;
            }

            // A porta vai re-verificar suas gavetas automaticamente na próxima interação
            // Não precisamos fazer nada específico, apenas garantir que ela saiba que algo mudou
        }
    }

    /// <summary>
    /// Verifica se todas as portas necessárias estão abertas
    /// </summary>
    private bool AreRequiredDoorsOpen()
    {
        // Se não há portas configuradas, sempre permite acesso
        if (requiredOpenDoors == null || requiredOpenDoors.Length == 0)
            return true;

        // Verifica se todas as portas estão abertas
        foreach (CabinetDoor door in requiredOpenDoors)
        {
            if (door == null)
            {
                Debug.LogWarning($"[DrawerController] {gameObject.name}: Porta nula encontrada na lista de portas necessárias!", this);
                continue;
            }

            if (!door.IsOpen)
                return false;
        }

        return true;
    }
    
    /// <summary>
    /// Verifica se esta gaveta está trancada e requer uma chave
    /// </summary>
    private bool IsLocked()
    {
        // Se não requer chave, nunca está trancada
        if (!requiresKey)
            return false;
        
        // Se não tem ID de chave configurado, trata como não trancada (com warning)
        if (string.IsNullOrEmpty(requiredKeyID))
        {
            Debug.LogWarning($"[DrawerController] {gameObject.name}: requiresKey está ativo mas requiredKeyID está vazio!", this);
            return false;
        }
        
        // Verifica se o jogador tem a chave
        return !DrawerKeyManager.Instance.HasKey(requiredKeyID);
    }

    /// <summary>
    /// Valida a configuração do componente
    /// </summary>
    private void ValidateConfiguration()
    {
        if (openDistance <= 0f)
        {
            Debug.LogWarning($"[DrawerController] {gameObject.name}: openDistance deve ser maior que zero!", this);
        }

        if (movementDuration <= 0f)
        {
            Debug.LogWarning($"[DrawerController] {gameObject.name}: movementDuration deve ser maior que zero!", this);
            movementDuration = 2f;
        }

        if (movementCurve == null || movementCurve.keys.Length == 0)
        {
            Debug.LogWarning($"[DrawerController] {gameObject.name}: movementCurve está vazia, usando curva padrão!", this);
            movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
        
        // Valida configuração das portas
        if (requiredOpenDoors != null && requiredOpenDoors.Length > 0)
        {
            int nullDoors = 0;
            foreach (var door in requiredOpenDoors)
            {
                if (door == null) nullDoors++;
            }
            
            if (nullDoors > 0)
            {
                Debug.LogWarning($"[DrawerController] {gameObject.name}: {nullDoors} porta(s) nula(s) encontrada(s) no array de portas necessárias!", this);
            }
        }
        
        // Valida configuração das portas para notificação
        if (notifyDoorsOnStateChange && (doorsToNotify == null || doorsToNotify.Length == 0))
        {
            Debug.LogWarning($"[DrawerController] {gameObject.name}: Notificação de portas ativada mas nenhuma porta configurada para notificar!", this);
        }
        
        if (doorsToNotify != null && doorsToNotify.Length > 0)
        {
            int nullNotifyDoors = 0;
            foreach (var door in doorsToNotify)
            {
                if (door == null) nullNotifyDoors++;
            }
            
            if (nullNotifyDoors > 0)
            {
                Debug.LogWarning($"[DrawerController] {gameObject.name}: {nullNotifyDoors} porta(s) nula(s) encontrada(s) no array de portas para notificar!", this);
            }
        }
        
        // Valida configuração de chaves
        if (requiresKey && string.IsNullOrEmpty(requiredKeyID))
        {
            Debug.LogWarning($"[DrawerController] {gameObject.name}: requiresKey está ativo mas requiredKeyID está vazio!", this);
        }
    }

    #endregion

    #region Editor Utilities

    #if UNITY_EDITOR
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Calcula posições se necessário
        if (Application.isPlaying)
        {
            // Em runtime, usa as posições calculadas
            DrawDebugGizmos(initialPosition, targetOpenPosition);
        }
        else
        {
            // No editor, calcula posições temporariamente
            Vector3 editorInitialPos = transform.position;
            Vector3 movementVector = Vector3.zero;
            float direction = moveToPositiveDirection ? 1f : -1f;
            float actualDistance = openDistance * direction;

            switch (movementAxis)
            {
                case MovementAxis.X:
                    movementVector = Vector3.right * actualDistance;
                    break;
                case MovementAxis.Y:
                    movementVector = Vector3.up * actualDistance;
                    break;
                case MovementAxis.Z:
                    movementVector = Vector3.forward * actualDistance;
                    break;
            }

            Vector3 editorTargetPos = editorInitialPos + movementVector;
            DrawDebugGizmos(editorInitialPos, editorTargetPos);
        }
    }

    private void DrawDebugGizmos(Vector3 closedPos, Vector3 openPos)
    {
        Gizmos.color = debugGizmoColor;
        
        // Posição fechada
        Gizmos.DrawWireCube(closedPos, Vector3.one * 0.1f);
        
        // Posição aberta
        Gizmos.color = Color.Lerp(debugGizmoColor, Color.white, 0.5f);
        Gizmos.DrawWireCube(openPos, Vector3.one * 0.1f);
        
        // Linha de movimento
        Gizmos.color = debugGizmoColor;
        Gizmos.DrawLine(closedPos, openPos);
        
        // Seta indicando direção
        Vector3 direction = (openPos - closedPos).normalized;
        Vector3 arrowPos = closedPos + direction * (openDistance * 0.7f);
        
        Gizmos.DrawRay(arrowPos, direction * 0.1f);
        
        // Labels informativos
        UnityEditor.Handles.Label(closedPos + Vector3.up * 0.2f, "Fechado");
        UnityEditor.Handles.Label(openPos + Vector3.up * 0.2f, "Aberto");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, 
            $"Eixo: {movementAxis} | Dist: {openDistance:F2}m | Duração: {movementDuration:F1}s");
        
        // Status das portas necessárias
        if (requiredOpenDoors != null && requiredOpenDoors.Length > 0)
        {
            string doorStatus = Application.isPlaying ? (AreRequiredDoorsOpen() ? "Portas: ABERTAS" : "Portas: FECHADAS") : $"Portas: {requiredOpenDoors.Length} configuradas";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, doorStatus);
        }
        
        // Status de trava por chave
        if (requiresKey)
        {
            string lockStatus;
            if (Application.isPlaying)
            {
                lockStatus = IsLocked() ? $"🔒 TRANCADO (Chave: {requiredKeyID})" : $"🔓 DESTRANCADO";
            }
            else
            {
                lockStatus = $"🔒 Requer Chave: {(string.IsNullOrEmpty(requiredKeyID) ? "SEM ID!" : requiredKeyID)}";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, lockStatus);
        }
        
        // Status das portas para notificação
        if (notifyDoorsOnStateChange && doorsToNotify != null && doorsToNotify.Length > 0)
        {
            string notifyStatus = $"Notifica: {doorsToNotify.Length} porta(s)";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.0f, notifyStatus);
        }
    }

    /// <summary>
    /// Método para resetar a posição inicial no editor
    /// </summary>
    [ContextMenu("Reset Initial Position")]
    private void ResetInitialPosition()
    {
        if (Application.isPlaying)
        {
            initialPosition = transform.position;
            CalculateTargetPosition();
        }
    }

    /// <summary>
    /// Verifica o status das portas necessárias (para debug)
    /// </summary>
    [ContextMenu("Check Door Status")]
    private void CheckDoorStatus()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[DrawerController] Verificação de status só funciona em runtime!");
            return;
        }

        Debug.Log($"[DrawerController] {gameObject.name}: Status atual da gaveta: {(isOpen ? "ABERTA" : "FECHADA")}");
        
        // Status de trava
        if (requiresKey)
        {
            bool locked = IsLocked();
            Debug.Log($"[DrawerController] Sistema de chave: ATIVO");
            Debug.Log($"  - ID da chave necessária: {requiredKeyID}");
            Debug.Log($"  - Status: {(locked ? "TRANCADA" : "DESTRANCADA")}");
            Debug.Log($"  - Jogador tem a chave: {(DrawerKeyManager.Instance.HasKey(requiredKeyID) ? "SIM" : "NÃO")}");
        }
        else
        {
            Debug.Log($"[DrawerController] Sistema de chave: DESATIVADO");
        }

        if (requiredOpenDoors == null || requiredOpenDoors.Length == 0)
        {
            Debug.Log($"[DrawerController] Nenhuma porta necessária configurada.");
        }
        else
        {
            Debug.Log($"[DrawerController] Status das portas necessárias:");
            for (int i = 0; i < requiredOpenDoors.Length; i++)
            {
                var door = requiredOpenDoors[i];
                if (door == null)
                {
                    Debug.Log($"  Porta {i}: NULA");
                }
                else
                {
                    Debug.Log($"  Porta {i} ({door.gameObject.name}): {(door.IsOpen ? "ABERTA" : "FECHADA")}");
                }
            }
        }
        
        if (notifyDoorsOnStateChange)
        {
            if (doorsToNotify == null || doorsToNotify.Length == 0)
            {
                Debug.Log($"[DrawerController] Notificação ativada mas nenhuma porta configurada para notificar.");
            }
            else
            {
                Debug.Log($"[DrawerController] Portas que serão notificadas sobre mudanças:");
                for (int i = 0; i < doorsToNotify.Length; i++)
                {
                    var door = doorsToNotify[i];
                    if (door == null)
                    {
                        Debug.Log($"  Porta para notificar {i}: NULA");
                    }
                    else
                    {
                        Debug.Log($"  Porta para notificar {i} ({door.gameObject.name}): {(door.IsOpen ? "ABERTA" : "FECHADA")}");
                    }
                }
            }
        }
        else
        {
            Debug.Log($"[DrawerController] Notificação de portas desativada.");
        }
    }

    #endif

    #endregion
}