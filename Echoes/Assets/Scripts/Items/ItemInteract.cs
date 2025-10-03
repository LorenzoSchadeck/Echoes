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

    [Header("Inspection Settings")]
    [SerializeField] private float inspectionDistance = 0.8f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("🔊 Audio Settings")]
    [Tooltip("Evento FMOD tocado quando o item é inspecionado")]
    [SerializeField] private EventReference itemPickupSoundEvent;

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

    // Propriedade da Interface: Monta o prompt dinamicamente com os textos localizados
    public string InteractionPrompt
    {
        get
        {
            string promptTemplate = promptString.GetLocalizedString();
            string localizedItemName = itemNameString.GetLocalizedString();
            
            // LOG DE DEPURAÇÃO
            if (string.IsNullOrEmpty(promptTemplate)) Debug.LogError("Prompt Template está vazio ou nulo!");
            if (string.IsNullOrEmpty(localizedItemName)) Debug.LogError("Localized Item Name está vazio ou nulo!");

            return promptTemplate.Replace("{itemName}", localizedItemName);
        }
    }

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
                
                StartInspection();
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
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ItemInteract] {name}: Erro ao tocar som de pickup: {e.Message}");
        }
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
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ItemInteract] {name}: Erro ao tocar som de soltura: {e.Message}");
        }
    }

    private void Update()
    {
        if (!isInspecting) return;

        if (Mouse.current.leftButton.isPressed)
        {
            RotateItem();
        }

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
        Quaternion inspectionRotation = cameraTransform.rotation * Quaternion.Euler(0, 180, 0);

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(MoveToTarget(inspectionPosition, inspectionRotation));
        
        ShowInspectionPanel();
    }

    private void ExitInspection()
    {
        if (!isInspecting) return;
        isInspecting = false;
        playerInteractor.SetInspectionMode(false);

        // Toca o som de soltura (com delay de 0.5s)
        PlayDropSound();

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(MoveToTarget(originalPosition, originalRotation, true));
        
        HideInspectionPanel();
    }

    private void ShowInspectionPanel()
    {
        if (inspectionPanel == null || itemNameText == null || itemDescriptionText == null) return;
        
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
}