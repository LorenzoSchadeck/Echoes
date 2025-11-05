using UnityEngine;
using UnityEngine.Localization;
using FMODUnity;

/// <summary>
/// Chave que pode ser coletada para desbloquear portas específicas.
/// Implementa IInteractable para permitir coleta direta através do sistema de interação padrão.
/// </summary>
public class DoorKey : MonoBehaviour, IInteractable
{
    [Header("🔑 Key Settings")]
    [Tooltip("ID único desta chave - deve corresponder ao ID da porta que ela desbloqueia")]
    [SerializeField] private string keyID;
    
    [Header("🌐 Localization")]
    [Tooltip("Texto exibido quando o jogador olha para a chave")]
    [SerializeField] private LocalizedString pickupPrompt;
    
    [Header("⚖️ Interaction Settings")]
    [Tooltip("Distância máxima para coletar esta chave")]
    [SerializeField] private float interactionDistance = 2f;
    
    [Header("🔊 Audio Settings")]
    [Tooltip("Som tocado quando a chave é coletada")]
    [SerializeField] private EventReference pickupSoundEvent;
    [Tooltip("Volume do som de coleta (0.0 a 1.0)")]
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;
    
    [Header("🎨 Visual Feedback")]
    [Tooltip("Se verdadeiro, destrói o objeto após ser coletado")]
    [SerializeField] private bool destroyOnPickup = true;
    
    private bool isCollected = false;
    private FMODAudioTrigger audioTrigger;
    
    // Propriedades da interface IInteractable
    public string InteractionPrompt => 
        pickupPrompt?.GetLocalizedString() ?? "Pegar Chave da Porta";
    
    public float InteractionDistance => interactionDistance;
    
    // Propriedades públicas
    public string KeyID => keyID;
    public bool IsCollected => isCollected;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Configura sistema de áudio FMOD
        SetupAudioSystem();
        
        // Valida configuração
        ValidateConfiguration();
    }
    
    #endregion
    
    #region IInteractable Implementation
    
    public bool Interact(Transform interactor)
    {
        // Não permite interagir se já foi coletada
        if (isCollected) return false;
        
        // Marca como coletada
        CollectKey();
        
        return true;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Coleta a chave e notifica o gerenciador
    /// </summary>
    private void CollectKey()
    {
        isCollected = true;
        
        // Toca som de coleta
        PlayPickupSound();
        
        // Notifica o gerenciador de chaves de porta
        DoorKeyManager.Instance?.RegisterCollectedKey(keyID);
        
        // Dispara evento de legenda
        GameEvents.TriggerDoorKeyCollected();
        
        Debug.Log($"[DoorKey] Chave '{keyID}' coletada!");
        
        // Desabilita visual
        DisableVisuals();
        
        // Destrói o objeto se configurado
        if (destroyOnPickup)
        {
            Destroy(gameObject, 0.5f); // Pequeno delay para permitir o som tocar
        }
    }
    
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
    /// Toca o som de coleta
    /// </summary>
    private void PlayPickupSound()
    {
        if (pickupSoundEvent.IsNull || audioTrigger == null) return;
        
        audioTrigger.fmodEvent = pickupSoundEvent;
        
        if (audioVolume != 1f)
        {
            audioTrigger.SetParameter("Volume", audioVolume);
        }
        
        audioTrigger.PlayAtPosition(transform.position);
    }
    
    /// <summary>
    /// Desabilita os renderizadores e collider do objeto
    /// </summary>
    private void DisableVisuals()
    {
        // Desabilita renderizadores
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Desabilita o collider para não ser detectado novamente
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
    
    /// <summary>
    /// Valida a configuração do componente
    /// </summary>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(keyID))
        {
            Debug.LogWarning($"[DoorKey] {gameObject.name}: keyID não foi configurado! Esta chave não desbloqueará nenhuma porta.", this);
        }
        
        if (interactionDistance <= 0f)
        {
            Debug.LogWarning($"[DoorKey] {gameObject.name}: interactionDistance deve ser maior que zero!", this);
        }
    }
    
    #endregion
    
    #region Editor Utilities
    
    #if UNITY_EDITOR
    
    private void OnDrawGizmosSelected()
    {
        // Desenha esfera de interação
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Ciano transparente
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Label com informações
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, 
            $"🔑 Chave Porta: {(string.IsNullOrEmpty(keyID) ? "SEM ID!" : keyID)}");
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"Dist: {interactionDistance:F2}m");
    }
    
    /// <summary>
    /// Testa a coleta da chave no editor
    /// </summary>
    [ContextMenu("Test Collect Key")]
    private void TestCollectKey()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[DoorKey] Teste de coleta só funciona em runtime!");
            return;
        }
        
        CollectKey();
        Debug.Log($"[DoorKey] {gameObject.name} com ID '{keyID}' foi coletada!");
    }
    
    #endif
    
    #endregion
}
