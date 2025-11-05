using UnityEngine;
using FMODUnity;

/// <summary>
/// Componente para objetos fixos que emitem sons durante a mecânica de choir.
/// Cada objeto tem seu próprio som configurado e pode ser marcado como "guia" ou "distração".
/// O ChoirManager apenas coordena quando os sons devem tocar.
/// </summary>
[RequireComponent(typeof(FMODAudioTrigger))]
public class ChoirAudioSource : MonoBehaviour
{
    [Header("🎵 Configuração do Som")]
    [Tooltip("Som que este objeto tocará durante o choir")]
    [SerializeField] private EventReference choirSoundEvent;
    
    [Tooltip("Se este objeto toca o som que guia o player para a porta correta")]
    [SerializeField] private bool isGuidingSource = false;
    
    [Header("🔊 Configurações de Áudio")]
    [Tooltip("Range máximo do som em metros")]
    [SerializeField] private float audioRange = 50f;
    
    // Componentes
    private FMODAudioTrigger audioTrigger;
    private ChoirManager choirManager;

    private void Awake()
    {
        // Garante que o FMODAudioTrigger existe
        audioTrigger = GetComponent<FMODAudioTrigger>();
        if (audioTrigger == null)
        {
            audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
            Debug.Log($"[ChoirAudioSource] FMODAudioTrigger adicionado automaticamente ao objeto {gameObject.name}");
        }
    }

    private void Start()
    {
        Debug.Log($"[ChoirAudioSource] {gameObject.name} inicializado para choir - Guia: {(isGuidingSource ? "SIM" : "NÃO")}");
        
        // Configura o audioTrigger se o som estiver configurado
        if (HasSoundConfigured)
        {
            audioTrigger.fmodEvent = choirSoundEvent;
            audioTrigger.SetSpatialRange(0f, audioRange);
        }
    }

    /// <summary>
    /// Registra este objeto com o ChoirManager
    /// </summary>
    public void RegisterWithManager(ChoirManager manager)
    {
        choirManager = manager;
        Debug.Log($"[ChoirAudioSource] {gameObject.name} registrado com ChoirManager");
    }

    /// <summary>
    /// Inicia a reprodução do som configurado neste objeto
    /// </summary>
    public void StartChoirSound()
    {
        if (!HasSoundConfigured)
        {
            Debug.LogError($"[ChoirAudioSource] Som não configurado em {gameObject.name}!");
            return;
        }

        if (audioTrigger == null)
        {
            Debug.LogError($"[ChoirAudioSource] FMODAudioTrigger não encontrado em {gameObject.name}!");
            return;
        }

        // Configura e inicia o som usando FMODAudioTrigger
        audioTrigger.fmodEvent = choirSoundEvent;
        audioTrigger.PlayAtPosition(transform.position);
        
        string sourceType = isGuidingSource ? "GUIA" : "DISTRAÇÃO";
        Debug.Log($"[ChoirAudioSource] Som de {sourceType} iniciado em {gameObject.name} com range {audioRange}m");
    }

    /// <summary>
    /// Para a reprodução do som
    /// </summary>
    public void StopSound()
    {
        if (audioTrigger != null)
        {
            audioTrigger.Stop();
            Debug.Log($"[ChoirAudioSource] Som parado em {gameObject.name}");
        }
    }

    /// <summary>
    /// Reset para permitir nova ativação (usado quando choir reinicia)
    /// </summary>
    public void ResetForNewActivation()
    {
        StopSound();
        Debug.Log($"[ChoirAudioSource] {gameObject.name} resetado para nova ativação");
    }

    /// <summary>
    /// Propriedades públicas
    /// </summary>
    public bool IsGuidingSource => isGuidingSource;
    public bool HasSoundConfigured => !choirSoundEvent.IsNull;

    #if UNITY_EDITOR
    /// <summary>
    /// Desenha range do áudio no Scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGuidingSource ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, audioRange);
        
        // Desenha ícone diferente para guia vs distração
        Gizmos.color = isGuidingSource ? Color.green : Color.red;
        Gizmos.DrawCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
    }
    #endif
}