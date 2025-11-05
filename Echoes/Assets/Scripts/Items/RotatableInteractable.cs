using UnityEngine;
using System.Collections;
using FMODUnity;
using UnityEngine.Localization;

/// <summary>
/// Objeto interativo que pode ser rotacionado uma única vez
/// Exibe texto localizado quando o jogador está olhando para ele
/// Compatível com o sistema de interação do projeto Echoes
/// </summary>
public class RotatableInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LocalizedString interactionPrompt;
    
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationAngle = 90f;
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useLocalRotation = true;
    
    [Header("Audio Settings")]
    [SerializeField] private EventReference rotationStartSound;
    
    [Header("🔗 Dependency System")]
    [Tooltip("Se verdadeiro, este item só pode ser interagido após o keyObject ser usado")]
    [SerializeField] private bool requiresKeyInteraction = false;
    
    [Tooltip("GameObject que deve ser interagido primeiro para liberar este item. Quando não atendido, não mostra prompt nem permite interação")]
    [SerializeField] private GameObject keyObject;
    
    // Internal state
    private bool hasBeenUsed = false;
    private bool isRotating = false;
    private Vector3 initialRotation;
    private Vector3 targetRotation;
    
    #region IInteractable Implementation
    
    /// <summary>
    /// Retorna o texto de prompt de interação localizado
    /// </summary>
    public string InteractionPrompt 
    { 
        get 
        {
            // Não exibe prompt se já foi usado ou está rotacionando
            if (hasBeenUsed || isRotating)
                return string.Empty;
            
            // Verifica dependência se necessário
            if (requiresKeyInteraction && !IsDependencyMet())
            {
                // Se dependência não foi atendida, não mostra prompt algum
                return string.Empty;
            }
                
            try
            {
                return interactionPrompt.GetLocalizedString();
            }
            catch
            {
                return "Rotacionar"; // Fallback text
            }
        } 
    }
    
    /// <summary>
    /// Distância máxima para interação
    /// </summary>
    public float InteractionDistance => interactionDistance;
    
    /// <summary>
    /// Executa a interação de rotação
    /// </summary>
    /// <param name="interactor">Transform do objeto que está interagindo (normalmente o player)</param>
    /// <returns>True se a interação foi bem-sucedida</returns>
    public bool Interact(Transform interactor)
    {
        // Verifica se pode interagir
        if (!CanInteract()) 
            return false;
        
        // Marca como usado para evitar múltiplas interações
        hasBeenUsed = true;
        
        // Inicia a rotação
        StartCoroutine(PerformRotation());
        
        return true;
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Inicialização do componente
    /// </summary>
    void Start()
    {
        InitializeRotationSettings();
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Inicializa as configurações de rotação
    /// </summary>
    private void InitializeRotationSettings()
    {
        // Armazena rotação inicial
        initialRotation = useLocalRotation ? transform.localEulerAngles : transform.eulerAngles;
        
        // Calcula rotação final baseada no eixo e ângulo especificados
        targetRotation = initialRotation + (rotationAxis.normalized * rotationAngle);
        
        // Valida configurações
        if (rotationDuration <= 0f)
        {
            rotationDuration = 1f;
            Debug.LogWarning($"Rotation duration deve ser maior que 0. Usando valor padrão: {rotationDuration}s", this);
        }
        
        if (rotationAxis.magnitude == 0f)
        {
            rotationAxis = Vector3.up;
            Debug.LogWarning($"Rotation axis não pode ser zero. Usando eixo Y como padrão.", this);
        }
    }
    
    /// <summary>
    /// Verifica se o objeto pode ser interagido
    /// </summary>
    /// <returns>True se pode interagir</returns>
    private bool CanInteract()
    {
        // Verifica estado básico
        if (hasBeenUsed || isRotating)
            return false;
        
        // Verifica dependência se necessário
        if (requiresKeyInteraction && !IsDependencyMet())
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Verifica se a dependência foi atendida
    /// </summary>
    /// <returns>True se a dependência foi atendida ou não é necessária</returns>
    private bool IsDependencyMet()
    {
        // Se não requer dependência, sempre retorna true
        if (!requiresKeyInteraction)
            return true;
        
        // Se não há keyObject configurado, considera dependência atendida
        if (keyObject == null)
        {
            Debug.LogWarning($"[RotatableInteractable] {gameObject.name} requer dependência mas keyObject não configurado!", this);
            return true;
        }
        
        // Verifica diferentes tipos de componentes no keyObject
        
        // 1. Verifica se é um ChoirFlashbackItem
        var choirFlashbackItem = keyObject.GetComponent<ChoirFlashbackItem>();
        if (choirFlashbackItem != null)
        {
            return choirFlashbackItem.HasBeenUsed;
        }
        
        // 2. Verifica se é um FlashbackItem (não tem propriedade HasBeenUsed, usa estado interno)
        var flashbackItem = keyObject.GetComponent<FlashbackItem>();
        if (flashbackItem != null)
        {
            // FlashbackItem não expõe estado publicamente, então verifica se o componente está desabilitado
            // Isso indica que foi usado (baseado no código original que desabilita após uso)
            return !flashbackItem.enabled;
        }
        
        // 3. Verifica se é outro RotatableInteractable
        var rotatableItem = keyObject.GetComponent<RotatableInteractable>();
        if (rotatableItem != null)
        {
            return rotatableItem.HasBeenUsed;
        }
        
        // 4. Verifica se implementa IInteractable de forma genérica
        var interactable = keyObject.GetComponent<IInteractable>();
        if (interactable != null)
        {
            // Para componentes genéricos que implementam IInteractable,
            // assumimos que se o GameObject está inativo, foi "usado"
            return !keyObject.activeInHierarchy;
        }
        
        // Se nenhum componente conhecido foi encontrado, log de aviso
        Debug.LogWarning($"[RotatableInteractable] {gameObject.name}: keyObject {keyObject.name} não possui componente de interação conhecido!", this);
        return false;
    }
    
    /// <summary>
    /// Executa a rotação animada do objeto
    /// </summary>
    /// <returns>Coroutine</returns>
    private IEnumerator PerformRotation()
    {
        isRotating = true;
        
        // Toca som de início da rotação
        PlayRotationStartSound();
        
        float elapsedTime = 0f;
        
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / rotationDuration;
            
            // Aplica curva de animação para suavizar movimento
            float curveProgress = rotationCurve.Evaluate(progress);
            
            // Interpola entre rotação inicial e final
            Vector3 currentRotation = Vector3.Lerp(initialRotation, targetRotation, curveProgress);
            
            // Aplica rotação
            if (useLocalRotation)
            {
                transform.localRotation = Quaternion.Euler(currentRotation);
            }
            else
            {
                transform.rotation = Quaternion.Euler(currentRotation);
            }
            
            yield return null;
        }
        
        // Garante que a rotação final seja exata
        if (useLocalRotation)
        {
            transform.localRotation = Quaternion.Euler(targetRotation);
        }
        else
        {
            transform.rotation = Quaternion.Euler(targetRotation);
        }
        
        isRotating = false;
        
        Debug.Log($"Rotação completada em {gameObject.name}. Rotação final: {targetRotation}", this);
    }
    
    /// <summary>
    /// Toca som de início da rotação via FMOD
    /// </summary>
    private void PlayRotationStartSound()
    {
        if (rotationStartSound.IsNull) return;
        
        RuntimeManager.PlayOneShot(rotationStartSound, transform.position);
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Verifica se o objeto já foi usado
    /// </summary>
    public bool HasBeenUsed => hasBeenUsed;
    
    /// <summary>
    /// Verifica se está atualmente rotacionando
    /// </summary>
    public bool IsRotating => isRotating;
    
    /// <summary>
    /// Verifica se a dependência está atendida (se aplicável)
    /// </summary>
    public bool IsDependencyMetPublic => IsDependencyMet();
    
    /// <summary>
    /// Verifica se o sistema de dependência está ativo
    /// </summary>
    public bool HasDependencySystem => requiresKeyInteraction;
    
    /// <summary>
    /// Retorna o nome do objeto chave (se configurado)
    /// </summary>
    public string KeyObjectName => keyObject != null ? keyObject.name : "Nenhum";
    
    /// <summary>
    /// Reseta o estado do objeto (útil para testes)
    /// </summary>
    [ContextMenu("Reset State")]
    public void ResetState()
    {
        hasBeenUsed = false;
        isRotating = false;
        
        // Volta para rotação inicial
        if (useLocalRotation)
        {
            transform.localRotation = Quaternion.Euler(initialRotation);
        }
        else
        {
            transform.rotation = Quaternion.Euler(initialRotation);
        }
        
        Debug.Log($"Estado resetado em {gameObject.name}", this);
    }
    
    /// <summary>
    /// Método de teste para forçar que a dependência seja considerada atendida
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [ContextMenu("Force Dependency Met (Test)")]
    public void ForceDependencyMet()
    {
        if (requiresKeyInteraction)
        {
            requiresKeyInteraction = false;
            Debug.Log($"[RotatableInteractable] TESTE: Dependência forçada como atendida para {gameObject.name}", this);
        }
        else
        {
            Debug.Log($"[RotatableInteractable] TESTE: {gameObject.name} não possui sistema de dependência ativo", this);
        }
    }
    
    #endregion
    
    #region Editor Helpers
    
    #if UNITY_EDITOR
    
    /// <summary>
    /// Desenha gizmos no Scene view para visualização
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Desenha esfera de distância de interação
        if (requiresKeyInteraction && !IsDependencyMet())
        {
            Gizmos.color = Color.red; // Vermelho se dependência não atendida
        }
        else if (hasBeenUsed)
        {
            Gizmos.color = Color.gray; // Cinza se já foi usado
        }
        else
        {
            Gizmos.color = Color.yellow; // Amarelo se disponível
        }
        
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Desenha conexão com objeto chave se configurado
        if (requiresKeyInteraction && keyObject != null)
        {
            Gizmos.color = IsDependencyMet() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, keyObject.transform.position);
            
            // Desenha esfera no objeto chave
            Gizmos.DrawWireSphere(keyObject.transform.position, 0.5f);
        }
        
        // Desenha seta indicando eixo de rotação
        Gizmos.color = Color.blue;
        Vector3 axisDirection = rotationAxis.normalized;
        Vector3 start = transform.position;
        Vector3 end = start + axisDirection * 2f;
        
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
        
        // Desenha informações de rotação e dependência
        if (Application.isPlaying)
        {
            Vector3 currentRotation = useLocalRotation ? transform.localEulerAngles : transform.eulerAngles;
            
            string dependencyInfo = "";
            if (requiresKeyInteraction)
            {
                string keyName = keyObject != null ? keyObject.name : "Nenhum";
                string dependencyStatus = IsDependencyMet() ? "✅ Atendida" : "❌ Não atendida";
                dependencyInfo = $"\nChave: {keyName}\nDependência: {dependencyStatus}";
            }
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                $"Atual: {currentRotation:F1}\nTarget: {targetRotation:F1}\nUsado: {hasBeenUsed}{dependencyInfo}");
        }
    }
    
    /// <summary>
    /// Valida componentes no editor
    /// </summary>
    private void OnValidate()
    {
        // Garante que a distância seja positiva
        if (interactionDistance < 0f)
        {
            interactionDistance = 3f;
        }
        
        // Garante que a duração seja positiva
        if (rotationDuration <= 0f)
        {
            rotationDuration = 2f;
        }
        
        // Normaliza eixo de rotação se necessário
        if (rotationAxis.magnitude > 0f)
        {
            rotationAxis = rotationAxis.normalized;
        }
    }
    
    #endif
    
    #endregion
}