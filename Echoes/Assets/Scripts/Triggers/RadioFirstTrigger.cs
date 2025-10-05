using UnityEngine;

/// <summary>
/// Trigger que ativa o rádio pela primeira vez quando o jogador entra na área.
/// Usado para implementar o novo fluxo linear do sistema de rádio.
/// </summary>
public class RadioFirstTrigger : MonoBehaviour
{
    [Header("Configurações do Trigger")]
    [Tooltip("Se o trigger só pode ser ativado uma vez")]
    [SerializeField] private bool onlyOnce = true;
    
    [Tooltip("Tag do jogador para detecção")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se é o jogador
        if (!other.CompareTag(playerTag)) return;
        
        // Verifica se já foi ativado (se onlyOnce estiver ativo)
        if (onlyOnce && hasTriggered) return;
        
        if (showDebugLogs)
        {
            Debug.Log($"RadioFirstTrigger: Jogador entrou no trigger - ativando rádio pela primeira vez");
        }
        
        // Marca como ativado
        hasTriggered = true;
        
        // Dispara evento para ativar o rádio
        GameEvents.TriggerRadioFirstTrigger();
        
        // Se é apenas uma vez, desabilita o trigger
        if (onlyOnce)
        {
            gameObject.SetActive(false);
            if (showDebugLogs)
            {
                Debug.Log("RadioFirstTrigger: Trigger desabilitado após primeira ativação");
            }
        }
    }
    
    /// <summary>
    /// Reseta o trigger para permitir nova ativação (útil para debug/testes)
    /// </summary>
    [ContextMenu("Reset Trigger")]
    public void ResetTrigger()
    {
        hasTriggered = false;
        gameObject.SetActive(true);
        
        if (showDebugLogs)
        {
            Debug.Log("RadioFirstTrigger: Trigger resetado");
        }
    }
}