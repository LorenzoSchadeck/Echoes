using FMODUnity;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Chave coletável. Cada tipo (porta, gaveta) diz em qual gerenciador registrar e qual evento disparar.
/// </summary>
public abstract class KeyPickup : MonoBehaviour, IInteractable, ISaveable
{
    [Header("🔑 Key Settings")]
    [Tooltip("ID único desta chave - deve corresponder ao ID da porta ou gaveta que ela destranca")]
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
    [Tooltip("Se verdadeiro, esconde o objeto (renderizadores e collider) ao ser coletado")]
    [SerializeField] private bool disableRenderersOnPickup = true;

    private bool isCollected = false;
    private FMODAudioTrigger audioTrigger;

    public string InteractionPrompt => InteractionText.Get(pickupPrompt, "PROMPT_TAKE");
    public float InteractionDistance => interactionDistance;

    public string KeyID => keyID;
    public bool IsCollected => isCollected;

    /// <summary>
    /// Registra a chave no gerenciador do seu tipo.
    /// </summary>
    protected abstract void RegisterKey(string id);

    /// <summary>
    /// Avisa o resto do jogo (legendas, por exemplo) que a chave foi coletada.
    /// </summary>
    protected abstract void NotifyCollected();

    protected abstract Color GizmoColor { get; }

    private void Awake()
    {
        audioTrigger = GetComponent<FMODAudioTrigger>();
        if (audioTrigger == null) audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();

        if (string.IsNullOrEmpty(keyID))
            Debug.LogWarning($"[{GetType().Name}] {gameObject.name}: keyID não foi configurado! Esta chave não destranca nada.", this);
        if (interactionDistance <= 0f)
            Debug.LogWarning($"[{GetType().Name}] {gameObject.name}: interactionDistance deve ser maior que zero!", this);
    }

    public bool Interact(Transform interactor)
    {
        if (isCollected) return false;

        CollectKey();
        return true;
    }

    [System.Serializable]
    private struct SaveState { public bool collected; }

    public string CaptureState() => JsonUtility.ToJson(new SaveState { collected = isCollected });

    public void RestoreState(string json)
    {
        if (!JsonUtility.FromJson<SaveState>(json).collected) return;

        // Já coletada: o gerenciador de chaves é restaurado à parte pelo checkpoint
        isCollected = true;
        if (disableRenderersOnPickup) DisableVisuals();
        if (destroyOnPickup) Destroy(gameObject);
    }

    private void CollectKey()
    {
        isCollected = true;

        PlayPickupSound();
        RegisterKey(keyID);
        NotifyCollected();

        if (disableRenderersOnPickup) DisableVisuals();

        // Pequeno atraso para o som tocar
        if (destroyOnPickup) Destroy(gameObject, 0.5f);
    }

    private void PlayPickupSound()
    {
        if (pickupSoundEvent.IsNull || audioTrigger == null) return;

        audioTrigger.fmodEvent = pickupSoundEvent;
        if (audioVolume != 1f) audioTrigger.SetParameter("Volume", audioVolume);
        audioTrigger.PlayAtPosition(transform.position);
    }

    // Esconde a chave e desliga o collider para não ser detectada de novo
    private void DisableVisuals()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;

        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider != null) ownCollider.enabled = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f,
            $"{GetType().Name}: {(string.IsNullOrEmpty(keyID) ? "SEM ID!" : keyID)}");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Dist: {interactionDistance:F2}m");
    }

    [ContextMenu("Test Collect Key")]
    private void TestCollectKey()
    {
        if (!Application.isPlaying)
        {
            Debug.Log($"[{GetType().Name}] Teste de coleta só funciona em runtime!");
            return;
        }

        CollectKey();
    }
#endif
}
