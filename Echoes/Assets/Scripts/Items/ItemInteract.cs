using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using FMODUnity;

[RequireComponent(typeof(Collider))]
public class ItemInteract : MonoBehaviour, IInteractable
{
    [Header("Localization Data")]
    [Tooltip("Referência à chave do prompt de interação (ex: PROMPT_INSPECT_ITEM). Esta chave deve conter '{itemName}'.")]
    [SerializeField] private LocalizedString promptString;
    [Tooltip("Referência à chave do nome deste item (ex: ITEM_NAME_OLD_PHOTO).")]
    [SerializeField] private LocalizedString itemNameString;
    [Tooltip("Referência à chave da descrição deste item (ex: ITEM_DESC_OLD_PHOTO).")]
    [SerializeField] private LocalizedString itemDescriptionString;

    [Header("UI References")]
    [Tooltip("O GameObject do painel que será ativado.")]
    [SerializeField] private GameObject inspectionPanel;
    [Tooltip("O campo de texto para o nome do item. DEVE ter o componente 'Localize String Event'.")]
    [SerializeField] private TMPro.TextMeshProUGUI itemNameText;
    [Tooltip("O campo de texto para a descrição do item. DEVE ter o componente 'Localize String Event'.")]
    [SerializeField] private TMPro.TextMeshProUGUI itemDescriptionText;

    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este item pode ser interagido")]
    [SerializeField] private float interactionDistance = 2.5f;
    
    [Header("Inspection Settings")]
    [SerializeField] private float inspectionDistance = 0.8f;
    [SerializeField] private float rotationSpeed = 10f;
    [Tooltip("Rotação adicional aplicada APÓS orientar o item para a câmera. Use para corrigir orientação (ex: 0,180,0 para virar de cabeça para baixo).")]
    [SerializeField] private Vector3 customInspectionRotation = Vector3.zero;
    [Tooltip("Se deve aplicar a rotação customizada adicional após orientar para a câmera")]
    [SerializeField] private bool useCustomRotation = false;

    [Header("🔊 Audio Settings")]
    [Tooltip("Evento FMOD tocado quando o item é inspecionado")]
    [SerializeField] private EventReference itemPickupSoundEvent;
    
    [Header("📻 Radio Trigger Settings")]
    [Tooltip("Se este item deve disparar eventos do rádio quando inspecionado")]
    [SerializeField] private bool triggerRadioEvents = false;
    [Tooltip("Tipo de trigger do rádio a ser disparado")]
    [SerializeField] private RadioTriggerType radioTriggerType = RadioTriggerType.None;
    [Tooltip("Se deve disparar apenas uma vez")]
    [SerializeField] private bool triggerOnlyOnce = true;
    
    [Header("🔑 Key Detection Settings")]
    [Tooltip("Se deve detectar chaves dentro deste item durante inspeção")]
    [SerializeField] private bool canContainKeys = false;
    [Tooltip("Distância máxima do raycast para detectar chaves dentro do item")]
    [SerializeField] private float keyDetectionDistance = 2f;
    [Tooltip("Layer das chaves para detecção via raycast")]
    [SerializeField] private LayerMask keyLayer;
    [Tooltip("Texto UI que exibirá o prompt de interação da chave")]
    [SerializeField] private TMPro.TextMeshProUGUI keyInteractionText;
    
    [Header("📺 Subtitle Settings")]
    [Tooltip("Se deve disparar legenda quando interagir com este item")]
    [SerializeField] private bool triggerSubtitleOnInteract = false;
    [Tooltip("Chave de localização da legenda a exibir")]
    [SerializeField] private LocalizedString interactSubtitle;
    [Tooltip("Duração da legenda em segundos")]
    [SerializeField] private float subtitleDuration = 3f;
    [Tooltip("Referência ao TextMeshProUGUI das legendas (mesmo usado pelo SubtitleManager)")]
    [SerializeField] private TMPro.TextMeshProUGUI subtitleTextReference;
    
    public enum RadioTriggerType
    {
        None,           // Não dispara eventos do rádio
        FirstTrigger,   // Dispara OnRadioFirstTrigger (liga rádio primeira vez)
        PaperTrigger    // Dispara OnRadioPaperTrigger (ativa modo puzzle)
    }

    // Referências privadas
    private Transform cameraTransform;
    private bool isInspecting = false;
    private PlayerInteractor playerInteractor;
    private Coroutine activeTransition = null;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    
    // Sistema de áudio FMOD seguindo padrão do projeto
    private FMODAudioTrigger audioTrigger;
    
    // Controle de trigger do rádio
    private bool hasTriggeredRadio = false;

    // Propriedades da Interface IInteractable
    public string InteractionPrompt
    {
        get
        {
            string promptTemplate = promptString.GetLocalizedString();
            string localizedItemName = itemNameString.GetLocalizedString();

            return promptTemplate.Replace("{itemName}", localizedItemName);
        }
    }
    
    public float InteractionDistance => interactionDistance;

    private void Start()
    {
        // Inicializa o sistema de áudio FMOD seguindo o padrão do projeto
        InitializeAudioSystem();
    }
    
    /// <summary>
    /// Inicializa o sistema de áudio FMOD seguindo o padrão estabelecido no projeto
    /// </summary>
    private void InitializeAudioSystem()
    {
        // Cria o componente FMODAudioTrigger seguindo o padrão dos outros scripts
        audioTrigger = gameObject.GetComponent<FMODAudioTrigger>();
        if (audioTrigger == null)
        {
            audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
        }
        
        // Configura o evento FMOD se estiver definido
        if (!itemPickupSoundEvent.IsNull)
        {
            audioTrigger.fmodEvent = itemPickupSoundEvent;
            audioTrigger.playOnStart = false; // Controle manual
        }
    }

    public bool Interact(Transform interactor)
    {
        if (isInspecting) return false;

        if (playerInteractor == null) playerInteractor = interactor.GetComponent<PlayerInteractor>();
        
        if (playerInteractor != null)
        {
            cameraTransform = playerInteractor.CameraTransform;
            if (cameraTransform != null)
            {
                // Toca o som de pickup se configurado
                PlayPickupSound();
                
                // Dispara eventos do rádio se configurado
                TriggerRadioEvent();
                
                StartInspection();
                
                // Dispara evento de legenda APÓS abrir o painel (para não ser desabilitada)
                TriggerSubtitleEvent();
                
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Toca o som de pickup do item usando o sistema FMOD integrado
    /// </summary>
    private void PlayPickupSound()
    {
        if (itemPickupSoundEvent.IsNull || audioTrigger == null) return;
        
        try
        {
            // Usa o sistema FMODAudioTrigger para tocar o som na posição do item
            audioTrigger.PlayAtPosition(transform.position);
        }
        catch (System.Exception)
        {
            // Silent error handling
        }
    }
    
    /// <summary>
    /// Dispara eventos do rádio baseado na configuração
    /// </summary>
    private void TriggerRadioEvent()
    {
        // Verifica se deve disparar eventos do rádio
        if (!triggerRadioEvents || radioTriggerType == RadioTriggerType.None) 
        {
            return;
        }
        
        // Verifica se já disparou e deve disparar apenas uma vez
        if (triggerOnlyOnce && hasTriggeredRadio) 
        {
            return;
        }
        
        // Marca como disparado
        hasTriggeredRadio = true;
        
        // Dispara o evento baseado no tipo
        switch (radioTriggerType)
        {
            case RadioTriggerType.FirstTrigger:
                GameEvents.TriggerRadioFirstTrigger();
                break;
                
            case RadioTriggerType.PaperTrigger:
                // Para PaperTrigger, usa o componente RadioPaperTrigger se existir
                RadioPaperTrigger paperTrigger = GetComponent<RadioPaperTrigger>();
                if (paperTrigger != null && paperTrigger.CanTrigger())
                {
                    bool success = paperTrigger.TriggerRadioPaperEvent();
                    if (success)
                    {
                        // Note: hasTriggeredRadio será marcado apenas quando o RadioController confirmar sucesso
                        // Por isso, resetamos aqui para permitir novas tentativas até o sucesso
                        hasTriggeredRadio = false;
                    }
                    else
                    {
                        hasTriggeredRadio = false; // Permite tentar novamente
                    }
                }
                else
                {
                    // Fallback: dispara diretamente se não houver componente RadioPaperTrigger
                    GameEvents.TriggerRadioPaperTrigger();
                    hasTriggeredRadio = false; // Permite tentar novamente até confirmar sucesso
                }
                break;
        }
    }

    /// <summary>
    /// Dispara evento de legenda se configurado
    /// </summary>
    private void TriggerSubtitleEvent()
    {
        if (!triggerSubtitleOnInteract) return;
        
        if (interactSubtitle == null || interactSubtitle.IsEmpty)
        {
            Debug.LogWarning($"[ItemInteract] {gameObject.name}: triggerSubtitleOnInteract está ativo mas interactSubtitle não foi configurado!", this);
            return;
        }
        
        // Dispara o evento de legenda
        GameEvents.TriggerItemInteractSubtitle(interactSubtitle, subtitleDuration);
    }

    /// <summary>
    /// Toca o som de soltura do item com delay de 0.5 segundos
    /// </summary>
    private void PlayDropSound()
    {
        if (itemPickupSoundEvent.IsNull || audioTrigger == null) return;
        
        StartCoroutine(PlayDropSoundDelayed());
    }

    /// <summary>
    /// Corrotina para tocar o som de soltura com delay
    /// </summary>
    private IEnumerator PlayDropSoundDelayed()
    {
        yield return new WaitForSeconds(0.2f);
        
        try
        {
            // Toca o mesmo som na posição original do item
            audioTrigger.PlayAtPosition(originalPosition);
        }
        catch (System.Exception)
        {
            // Silent error handling
        }
    }

    private void Update()
    {
        if (!isInspecting) return;

        // Rotação do item
        if (Mouse.current.leftButton.isPressed)
        {
            RotateItem();
        }
        
        // Detecção de chaves via raycast durante inspeção
        if (canContainKeys)
        {
            CheckForKeyInteraction();
        }

        // Sair da inspeção
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ExitInspection();
        }
    }

    private void StartInspection()
    {
        isInspecting = true;
        playerInteractor.SetInspectionMode(true);

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        
        GetComponent<Collider>().enabled = false;

        Vector3 inspectionPosition = cameraTransform.position + cameraTransform.forward * inspectionDistance;
        Quaternion inspectionRotation;
        
        // Define a rotação baseada na configuração
        if (useCustomRotation)
        {
            // Combina a orientação da câmera com a rotação customizada
            // Primeiro orienta para a câmera, depois aplica a rotação customizada
            Quaternion cameraOrientation = cameraTransform.rotation * Quaternion.Euler(0, 180, 0);
            Quaternion customOffset = Quaternion.Euler(customInspectionRotation);
            inspectionRotation = cameraOrientation * customOffset;
        }
        else
        {
            // Usa a orientação padrão baseada na câmera
            inspectionRotation = cameraTransform.rotation * Quaternion.Euler(0, 180, 0);
        }

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(MoveToTarget(inspectionPosition, inspectionRotation));
        
        ShowInspectionPanel();
    }

    private void ExitInspection()
    {
        if (!isInspecting) return;
        isInspecting = false;
        playerInteractor.SetInspectionMode(false);

        // Esconde o prompt de chave ao sair da inspeção
        UpdateKeyInteractionUI(false);

        // Toca o som de soltura (com delay de 0.5s)
        PlayDropSound();

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(MoveToTarget(originalPosition, originalRotation, true));
        
        HideInspectionPanel();
    }

    private void ShowInspectionPanel()
    {
        if (inspectionPanel == null || itemNameText == null || itemDescriptionText == null) return;
        
        // Desabilita texto de legenda APENAS se não houver legendas ativas
        // Se SubtitleManager estiver ativo, mantém o texto habilitado
        if (subtitleTextReference != null && !SubtitleManager.IsSubtitleActive)
        {
            subtitleTextReference.enabled = false;
        }
        
        // Ativa o painel primeiro
        inspectionPanel.SetActive(true);

        // Busca as traduções e as define DIRETAMENTE no campo .text
        itemNameText.text = itemNameString.GetLocalizedString();
        itemDescriptionText.text = itemDescriptionString.GetLocalizedString();
    }

    private void HideInspectionPanel()
    {
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }
        
        // Reabilita texto de legenda apenas se não há legendas ativas
        // Se há legendas ativas, o SubtitleManager já cuidou de ativar o componente
        if (subtitleTextReference != null && !SubtitleManager.IsSubtitleActive)
        {
            // Desabilita o componente quando não há legendas e o painel foi fechado
            subtitleTextReference.enabled = false;
        }
    }

    private IEnumerator MoveToTarget(Vector3 targetPos, Quaternion targetRot, bool isReturning = false)
    {
        if (!isReturning) transform.SetParent(null);
        
        float time = 0;
        float duration = 0.4f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        if (isReturning)
        {
            transform.SetParent(originalParent);
            GetComponent<Collider>().enabled = true;
        }
        activeTransition = null;
    }

    private void RotateItem()
    {
        float rotationX = Mouse.current.delta.x.ReadValue() * rotationSpeed * Time.deltaTime;
        float rotationY = Mouse.current.delta.y.ReadValue() * rotationSpeed * Time.deltaTime;
        
        transform.Rotate(cameraTransform.up, -rotationX, Space.World);
        transform.Rotate(cameraTransform.right, rotationY, Space.World);
    }
    
    /// <summary>
    /// Verifica se o jogador está olhando para uma chave dentro do item inspecionado
    /// e permite coletá-la com o botão de interação padrão
    /// </summary>
    private void CheckForKeyInteraction()
    {
        // Dispara raycast do centro da câmera
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, keyDetectionDistance, keyLayer))
        {
            // Verifica se acertou uma chave
            DrawerKey key = hit.collider.GetComponent<DrawerKey>();
            
            if (key != null && !key.IsCollected)
            {
                // Mostra o prompt de interação da chave
                UpdateKeyInteractionUI(true, key.InteractionPrompt);
                
                // Debug: mostra que a chave foi detectada
                Debug.Log($"[ItemInteract] Chave detectada: {key.KeyID} - Pressione E para coletar");
                
                // Verifica se o jogador pressionou o botão de interação
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    Debug.Log($"[ItemInteract] Coletando chave: {key.KeyID}");
                    
                    // Coleta a chave
                    bool collected = key.Interact(playerInteractor.transform);
                    
                    if (collected)
                    {
                        Debug.Log($"[ItemInteract] Chave {key.KeyID} coletada com sucesso!");
                        
                        // Esconde o prompt após coletar
                        UpdateKeyInteractionUI(false);
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemInteract] Falha ao coletar chave {key.KeyID}");
                    }
                }
                
                return; // Sai para não esconder o prompt
            }
            else if (key == null)
            {
                Debug.LogWarning($"[ItemInteract] Raycast acertou objeto '{hit.collider.name}' mas não tem componente DrawerKey!");
            }
        }
        
        // Se não está olhando para nenhuma chave, esconde o prompt
        UpdateKeyInteractionUI(false);
    }
    
    /// <summary>
    /// Atualiza a UI do prompt de interação da chave
    /// </summary>
    private void UpdateKeyInteractionUI(bool show, string prompt = "")
    {
        if (keyInteractionText != null)
        {
            keyInteractionText.enabled = show;
            keyInteractionText.text = prompt;
        }
    }
    
    /// <summary>
    /// Verifica se este ItemInteract é do tipo PaperTrigger
    /// </summary>
    public bool IsPaperTrigger()
    {
        return triggerRadioEvents && radioTriggerType == RadioTriggerType.PaperTrigger;
    }

    /// <summary>
    /// Marca o trigger do rádio como utilizado com sucesso (chamado pelo RadioController)
    /// </summary>
    public void MarkRadioTriggerAsUsed()
    {
        hasTriggeredRadio = true;
    }

    /// <summary>
    /// Reseta o trigger do rádio para permitir nova ativação (útil para debug/testes)
    /// </summary>
    [ContextMenu("Reset Radio Trigger")]
    public void ResetRadioTrigger()
    {
        hasTriggeredRadio = false;
    }
    
    /// <summary>
    /// Captura a diferença entre a rotação atual e a orientação da câmera como offset customizado
    /// </summary>
    [ContextMenu("Capture Current Rotation as Custom Offset")]
    public void CaptureCurrentRotationAsCustomOffset()
    {
        if (cameraTransform != null)
        {
            // Calcula o offset necessário para atingir a rotação atual a partir da orientação da câmera
            Quaternion cameraOrientation = cameraTransform.rotation * Quaternion.Euler(0, 180, 0);
            Quaternion currentRotation = transform.rotation;
            Quaternion offset = Quaternion.Inverse(cameraOrientation) * currentRotation;
            customInspectionRotation = offset.eulerAngles;
            useCustomRotation = true;
        }
    }
    
    /// <summary>
    /// Define orientações pré-definidas comuns para facilitar a configuração
    /// </summary>
    [ContextMenu("Set No Additional Rotation")]
    public void SetNoAdditionalRotation()
    {
        customInspectionRotation = Vector3.zero;
        useCustomRotation = false;
    }
    
    [ContextMenu("Set Flip Vertically (180° on Y)")]
    public void SetFlipVertically()
    {
        customInspectionRotation = new Vector3(0, 180, 0);
        useCustomRotation = true;
    }
    
    [ContextMenu("Set Flip Horizontally (180° on Z)")]
    public void SetFlipHorizontally()
    {
        customInspectionRotation = new Vector3(0, 0, 180);
        useCustomRotation = true;
    }
    
    #if UNITY_EDITOR
    
    /// <summary>
    /// Testa a detecção de chaves filhas (para debug)
    /// </summary>
    [ContextMenu("Debug: List Child Keys")]
    private void DebugListChildKeys()
    {
        DrawerKey[] keys = GetComponentsInChildren<DrawerKey>(true);
        
        if (keys.Length == 0)
        {
            Debug.Log($"[ItemInteract] {gameObject.name}: Nenhuma chave encontrada como filha deste objeto.");
        }
        else
        {
            Debug.Log($"[ItemInteract] {gameObject.name}: {keys.Length} chave(s) encontrada(s):");
            foreach (DrawerKey key in keys)
            {
                Debug.Log($"  - {key.gameObject.name} (ID: {key.KeyID}, Layer: {LayerMask.LayerToName(key.gameObject.layer)})");
            }
        }
        
        // Verifica configuração
        if (canContainKeys)
        {
            Debug.Log($"[ItemInteract] Can Contain Keys: ATIVADO");
            Debug.Log($"[ItemInteract] Key Detection Distance: {keyDetectionDistance}m");
            Debug.Log($"[ItemInteract] Key Layer Mask: {LayerMaskToString(keyLayer)}");
        }
        else
        {
            Debug.LogWarning($"[ItemInteract] Can Contain Keys: DESATIVADO - Ative para detectar chaves!");
        }
    }
    
    /// <summary>
    /// Desenha o raycast de detecção de chaves durante inspeção (apenas para debug)
    /// </summary>
    private void OnDrawGizmos()
    {
        // Só desenha se está inspecionando e pode conter chaves
        if (!isInspecting || !canContainKeys || cameraTransform == null) return;
        
        // Desenha o raycast
        Vector3 rayOrigin = cameraTransform.position;
        Vector3 rayDirection = cameraTransform.forward;
        Vector3 rayEnd = rayOrigin + rayDirection * keyDetectionDistance;
        
        // Cor verde se acertou algo, vermelho se não acertou
        Ray ray = new Ray(rayOrigin, rayDirection);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, keyDetectionDistance, keyLayer);
        
        Gizmos.color = hit ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, hit ? hitInfo.point : rayEnd);
        
        // Desenha uma esfera no ponto de impacto
        if (hit)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitInfo.point, 0.05f);
            
            // Verifica se é uma chave
            DrawerKey key = hitInfo.collider.GetComponent<DrawerKey>();
            if (key != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(hitInfo.point, 0.1f);
                
                UnityEditor.Handles.Label(hitInfo.point + Vector3.up * 0.1f, 
                    $"CHAVE DETECTADA!\nID: {key.KeyID}");
            }
        }
        
        // Label com informações do raycast
        UnityEditor.Handles.Label(rayOrigin + Vector3.up * 0.2f, 
            $"Key Detection Raycast\nDistância: {keyDetectionDistance:F2}m\nLayer: {LayerMaskToString(keyLayer)}");
    }
    
    /// <summary>
    /// Converte LayerMask para string legível
    /// </summary>
    private string LayerMaskToString(LayerMask mask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                if (result.Length > 0) result += ", ";
                result += LayerMask.LayerToName(i);
            }
        }
        return string.IsNullOrEmpty(result) ? "Nothing" : result;
    }
    
    #endif
}