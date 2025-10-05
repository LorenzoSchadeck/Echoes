using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using FMODUnity;

/// <summary>
/// Script simples para portas de armário com interação similar às portas convencionais.
/// A porta abre/fecha em torno de um pivot configurável com ângulo e direção personalizáveis.
/// </summary>
public class CabinetDoor : MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Prompt exibido quando a porta está fechada")]
    [SerializeField] private LocalizedString openPrompt;
    [Tooltip("Prompt exibido quando a porta está aberta")]
    [SerializeField] private LocalizedString closePrompt;

    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este item pode ser interagido")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Header("Movement Settings")]
    [Tooltip("Ângulo em graus que a porta abrirá (ex: 90 para 90 graus)")]
    [SerializeField] private float openAngle = 90f;
    [Tooltip("Se verdadeiro, abre no sentido positivo. Se falso, abre no sentido negativo")]
    [SerializeField] private bool openToPositiveSide = true;
    [Tooltip("Duração em segundos para abrir a porta")]
    [SerializeField] private float openDuration = 1f;
    [Tooltip("Duração em segundos para fechar a porta")]
    [SerializeField] private float closeDuration = 1f;

    [Header("Pivot Setup")]
    [Tooltip("O objeto pivot em torno do qual a porta rotaciona (normalmente um GameObject vazio pai)")]
    [SerializeField] private Transform pivot;
    [Tooltip("Eixo de rotação da porta (Y para horizontal, Z para vertical, X para frente/trás)")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // Y por padrão

    [Header("🔊 Audio Settings")]
    [Tooltip("Som tocado quando a porta abre")]
    [SerializeField] private EventReference openSoundEvent;
    [Tooltip("Som tocado quando a porta fecha")]
    [SerializeField] private EventReference closeSoundEvent;

    // Estado interno
    private Quaternion initialRotation;
    private bool isOpen = false;
    private bool isMoving = false;
    private FMODAudioTrigger audioTrigger;

    // Propriedades públicas
    public bool IsOpen => isOpen;
    
    // Propriedades da interface IInteractable
    public string InteractionPrompt
    {
        get
        {
            if (isMoving) return string.Empty; // Sem prompt durante movimento
            
            if (isOpen)
                return closePrompt?.GetLocalizedString() ?? "Fechar";
            else
                return openPrompt?.GetLocalizedString() ?? "Abrir";
        }
    }
    
    public float InteractionDistance => interactionDistance;

    private void Awake()
    {
        // Configura pivot se não foi definido
        if (pivot == null)
            pivot = transform;

        // Salva rotação inicial
        initialRotation = pivot.rotation;

        // Configura sistema de áudio FMOD
        audioTrigger = gameObject.GetComponent<FMODAudioTrigger>();
        if (audioTrigger == null)
            audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
    }

    public bool Interact(Transform interactor)
    {
        // Não permite interação durante movimento
        if (isMoving) return false;

        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }

        return true;
    }

    /// <summary>
    /// Abre a porta do armário
    /// </summary>
    private void OpenDoor()
    {
        float direction = openToPositiveSide ? 1f : -1f;
        Vector3 eulerRotation = rotationAxis * (openAngle * direction);
        Quaternion targetRotation = initialRotation * Quaternion.Euler(eulerRotation);
        
        StartCoroutine(MoveDoorCoroutine(targetRotation, openDuration, openSoundEvent, true));
    }

    /// <summary>
    /// Fecha a porta do armário
    /// </summary>
    private void CloseDoor()
    {
        StartCoroutine(MoveDoorCoroutine(initialRotation, closeDuration, closeSoundEvent, false));
    }

    /// <summary>
    /// Corrotina que anima o movimento da porta
    /// </summary>
    private IEnumerator MoveDoorCoroutine(Quaternion targetRotation, float duration, EventReference soundEvent, bool willBeOpen)
    {
        isMoving = true;

        // Toca som se configurado
        PlaySound(soundEvent);

        Quaternion startRotation = pivot.rotation;
        float elapsedTime = 0f;

        // Anima a rotação
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Aplica curva de animação suave
            t = Mathf.SmoothStep(0f, 1f, t);
            
            pivot.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        // Garante posição final exata
        pivot.rotation = targetRotation;
        isOpen = willBeOpen;
        isMoving = false;
    }

    /// <summary>
    /// Toca um som FMOD se configurado
    /// </summary>
    private void PlaySound(EventReference soundEvent)
    {
        if (soundEvent.IsNull || audioTrigger == null) return;

        audioTrigger.fmodEvent = soundEvent;
        audioTrigger.PlayAtPosition(transform.position);
    }

    /// <summary>
    /// Abre a porta via código (para uso em scripts)
    /// </summary>
    [ContextMenu("Open Door")]
    public void OpenDoorProgrammatically()
    {
        if (!isMoving && !isOpen)
            OpenDoor();
    }

    /// <summary>
    /// Fecha a porta via código (para uso em scripts)
    /// </summary>
    [ContextMenu("Close Door")]
    public void CloseDoorProgrammatically()
    {
        if (!isMoving && isOpen)
            CloseDoor();
    }

    /// <summary>
    /// Força um estado específico da porta (para inicialização)
    /// </summary>
    public void SetDoorState(bool open, bool immediate = false)
    {
        if (isMoving) return;

        if (immediate)
        {
            isOpen = open;
            if (open)
            {
                float direction = openToPositiveSide ? 1f : -1f;
                Vector3 eulerRotation = rotationAxis * (openAngle * direction);
                pivot.rotation = initialRotation * Quaternion.Euler(eulerRotation);
            }
            else
            {
                pivot.rotation = initialRotation;
            }
        }
        else
        {
            if (open && !isOpen)
                OpenDoor();
            else if (!open && isOpen)
                CloseDoor();
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (pivot == null) return;

        // Desenha o pivot
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot.position, 0.1f);

        // Desenha a direção de abertura
        float direction = openToPositiveSide ? 1f : -1f;
        Vector3 eulerRotation = rotationAxis * (openAngle * direction);
        
        Gizmos.color = Color.green;
        Vector3 forwardDirection = pivot.rotation * Quaternion.Euler(eulerRotation) * Vector3.forward;
        Gizmos.DrawRay(pivot.position, forwardDirection * 0.5f);
        
        // Label informativo
        UnityEditor.Handles.Label(pivot.position + Vector3.up * 0.5f, 
            $"Abertura: {openAngle}° ({(openToPositiveSide ? "+" : "-")})");
    }
    #endif
}