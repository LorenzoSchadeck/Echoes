using UnityEngine;

/// <summary>
/// Componente simples que apenas dispara o evento do rádio quando solicitado.
/// A interação real é feita pelo ItemInteract.
/// Este componente serve apenas como um lançador de evento.
/// </summary>
public class RadioPaperTrigger : MonoBehaviour
{
    [Header("Configurações do Trigger")]
    [Tooltip("Se o trigger só pode ser ativado uma vez")]
    [SerializeField] private bool onlyOnce = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool hasTriggered = false;

    /// <summary>
    /// Dispara o evento do rádio. Deve ser chamado pelo ItemInteract.
    /// Retorna true se o evento foi processado com sucesso.
    /// </summary>
    public bool TriggerRadioPaperEvent()
    {
        // Verifica se já foi ativado (se onlyOnce estiver ativo)
        if (onlyOnce && hasTriggered) 
        {
            return false;
        }
        
        // Sempre tenta disparar o evento - o RadioController decidirá se aceita ou não
        // Se o RadioController rejeitar, não marcamos como hasTriggered
        
        // Dispara evento para ativar o Track 2 do rádio
        GameEvents.TriggerRadioPaperTrigger();
        
        // IMPORTANTE: NÃO marca como hasTriggered aqui!
        // Só será marcado quando o RadioController confirmar sucesso via MarkAsSuccessfullyUsed()
        
        return true;
    }
    
    /// <summary>
    /// Verifica se o trigger pode ser ativado
    /// </summary>
    public bool CanTrigger()
    {
        return !(onlyOnce && hasTriggered);
    }
    
    /// <summary>
    /// Marca o trigger como utilizado com sucesso (chamado pelo RadioController)
    /// </summary>
    public void MarkAsSuccessfullyUsed()
    {
        hasTriggered = true;
    }

    /// <summary>
    /// Reseta o papel para permitir nova ativação (útil para debug/testes)
    /// </summary>
    [ContextMenu("Reset Paper Trigger")]
    public void ResetPaperTrigger()
    {
        hasTriggered = false;
    }
}