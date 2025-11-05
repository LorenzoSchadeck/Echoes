using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Item simples no flashback que, quando interagido, encerra o flashback
/// e marca o sistema de choir como completado permanentemente.
/// Diferente do FlashbackItem, este é um item de interação simples que apenas encerra o flashback.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChoirFlashbackItem : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este item pode ser interagido")]
    [SerializeField] private float interactionDistance = 2f;
    
    [Header("Localization")]
    [Tooltip("Prompt exibido quando este item pode ser interagido")]
    [SerializeField] private LocalizedString interactionPrompt;

    [Header("🎭 Configurações do Item")]
    [Tooltip("Se deve desabilitar o GameObject após uso")]
    [SerializeField] private bool disableAfterUse = true;
    
    [Tooltip("Se deve desabilitar apenas este componente após uso")]
    [SerializeField] private bool disableComponentOnly = false;

    private bool hasBeenUsed = false;

    public string InteractionPrompt 
    { 
        get
        {
            // Só mostra prompt se ainda não foi usado
            if (!hasBeenUsed)
            {
                return interactionPrompt.GetLocalizedString();
            }
            return string.Empty;
        }
    }
    
    public float InteractionDistance => interactionDistance;

    /// <summary>
    /// Implementação da interface IInteractable
    /// </summary>
    /// <param name="interactor">Transform do jogador</param>
    /// <returns>True se a interação foi bem-sucedida</returns>
    public bool Interact(Transform interactor)
    {
        // Evita múltiplas interações
        if (hasBeenUsed)
        {
            return false;
        }

        Debug.Log($"[ChoirFlashbackItem] Jogador interagiu com {gameObject.name} - Encerrando flashback");

        hasBeenUsed = true;

        // 1. Notifica o ChoirManager que o item foi usado (completa o choir permanentemente)
        if (ChoirManager.Instance != null)
        {
            ChoirManager.Instance.OnFlashbackItemUsed();
        }
        else
        {
            Debug.LogWarning("[ChoirFlashbackItem] ChoirManager.Instance não encontrado!");
        }

        // 2. Encerra o flashback
        GameEvents.TriggerFlashbackEnded();

        // 3. Desabilita o item conforme configuração
        HandleItemDisabling();

        return true;
    }

    /// <summary>
    /// Gerencia a desabilitação do item após uso
    /// </summary>
    private void HandleItemDisabling()
    {
        if (disableComponentOnly)
        {
            // Desabilita apenas este componente
            this.enabled = false;
            Debug.Log($"[ChoirFlashbackItem] Componente desabilitado: {gameObject.name}");
        }
        else if (disableAfterUse)
        {
            // Desabilita todo o GameObject
            gameObject.SetActive(false);
            Debug.Log($"[ChoirFlashbackItem] GameObject desabilitado: {gameObject.name}");
        }
        else
        {
            Debug.Log($"[ChoirFlashbackItem] Item usado mas permanece ativo: {gameObject.name}");
        }
    }

    /// <summary>
    /// Reset do item para permitir nova interação (para testes ou casos especiais)
    /// </summary>
    public void ResetItem()
    {
        hasBeenUsed = false;
        
        if (disableComponentOnly && !this.enabled)
        {
            this.enabled = true;
        }
        else if (disableAfterUse && !gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        Debug.Log($"[ChoirFlashbackItem] Item resetado: {gameObject.name}");
    }

    /// <summary>
    /// Força o uso do item para testes
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceUseItem()
    {
        if (!hasBeenUsed)
        {
            Debug.Log("[ChoirFlashbackItem] TESTE: Forçando uso do item");
            Interact(null);
        }
        else
        {
            Debug.Log("[ChoirFlashbackItem] TESTE: Item já foi usado");
        }
    }

    /// <summary>
    /// Propriedades públicas para verificação de estado
    /// </summary>
    public bool HasBeenUsed => hasBeenUsed;
}