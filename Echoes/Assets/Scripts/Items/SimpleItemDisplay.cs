using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;

/// <summary>
/// Script simples para interação com itens que exibe um texto na tela.
/// O texto permanece ativo enquanto o jogador estiver em contato com o SimpleItemDisplay.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SimpleItemDisplay : MonoBehaviour, IInteractable
{
    [Header("Localization Data")]
    [Tooltip("Referência à chave do prompt de interação (ex: PROMPT_READ_ITEM).")]
    [SerializeField] private LocalizedString promptString;
    [Tooltip("Referência à chave do texto que será exibido (ex: ITEM_TEXT_NOTE).")]
    [SerializeField] private LocalizedString displayTextString;

    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este item pode ser interagido")]
    [SerializeField] private float interactionDistance = 2f;
    
    [Header("Display Settings")]
    [Tooltip("O TextMeshProUGUI que será ativado para exibir o texto.")]
    [SerializeField] private TextMeshProUGUI displayText;
    [Tooltip("Se verdadeiro, permite múltiplas interações. Se falso, só pode ser usado uma vez.")]
    [SerializeField] private bool canInteractMultipleTimes = true;

    // Controle interno
    private bool hasBeenUsed = false;
    private bool isDisplayActive = false;
    private bool wasBeingLookedAt = false;

    // Propriedades da Interface IInteractable
    public string InteractionPrompt
    {
        get
        {
            // VERIFICAÇÃO: Se legendas estão ativas, não mostra prompt
            if (SubtitleManager.IsSubtitleActive)
            {
                wasBeingLookedAt = false;
                return string.Empty;
            }
            
            // Se já foi usado e não pode ser reutilizado, não mostra prompt
            if (hasBeenUsed && !canInteractMultipleTimes)
            {
                wasBeingLookedAt = false;
                return string.Empty;
            }

            // Se está exibindo texto, detecta se está sendo olhado ou não
            if (isDisplayActive)
            {
                // Se este método está sendo chamado, significa que o jogador está olhando
                wasBeingLookedAt = true;
                return string.Empty; // Não mostra prompt para evitar múltiplas interações
            }

            // Se chegou aqui, o jogador está olhando e pode interagir
            wasBeingLookedAt = true;
            return promptString?.GetLocalizedString() ?? "Interagir";
        }
    }
    
    public float InteractionDistance => interactionDistance;

    public bool Interact(Transform interactor)
    {
        // VERIFICAÇÃO: Se legendas estão ativas, bloqueia interação
        if (SubtitleManager.IsSubtitleActive)
        {
            return false;
        }
        
        // Verifica se pode interagir
        if (hasBeenUsed && !canInteractMultipleTimes)
            return false;

        // Verifica se já está exibindo texto
        if (isDisplayActive)
            return false;

        // Verifica se tem o componente de texto
        if (displayText == null)
        {
            return false;
        }

        // Executa a interação
        ShowDisplayText();

        // Marca como usado
        hasBeenUsed = true;

        return true;
    }

    private void Update()
    {
        // Se o texto está ativo, verifica se o jogador parou de olhar
        if (isDisplayActive)
        {
            // CORREÇÃO: Não esconde texto se legendas estão ativas
            if (SubtitleManager.IsSubtitleActive)
            {
                // Mantém o texto ativo mesmo que o prompt não apareça
                return;
            }
            
            // Se na última frame estava sendo olhado, mas agora não está mais
            if (wasBeingLookedAt)
            {
                // Reset o flag para detectar quando parar de ser olhado
                wasBeingLookedAt = false;
            }
            else
            {
                // Se o flag ainda está false, significa que InteractionPrompt não foi chamado
                // ou seja, o jogador não está mais olhando para o objeto
                HideDisplayText();
            }
        }
    }

    private void ShowDisplayText()
    {
        // Obtém o texto localizado
        string textToDisplay = displayTextString?.GetLocalizedString() ?? "Texto não configurado";
        
        // ATIVA o componente e configura o texto
        displayText.enabled = true;
        displayText.text = textToDisplay;
        isDisplayActive = true;
    }

    /// <summary>
    /// Esconde o texto quando o jogador não está mais olhando para o objeto
    /// Chamado automaticamente pelo sistema quando InteractionPrompt retorna string vazia
    /// </summary>
    public void HideDisplayText()
    {
        if (!isDisplayActive) return;

        // DESATIVA o componente completamente
        if (displayText != null)
        {
            displayText.enabled = false;
            displayText.text = "";
        }
        
        isDisplayActive = false;

        // Se permite múltiplas interações, marca como não usado para permitir nova interação
        if (canInteractMultipleTimes)
        {
            hasBeenUsed = false;
        }
    }

    private void Reset()
    {
        // Configurações padrão quando o componente é adicionado
        canInteractMultipleTimes = true;
        interactionDistance = 2f;
        
        // Tenta encontrar automaticamente um TextMeshProUGUI no mesmo GameObject
        if (displayText == null)
        {
            displayText = GetComponent<TextMeshProUGUI>();
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Desenha uma esfera para visualizar a posição do item no editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Desenha o texto do prompt acima do objeto
        UnityEditor.Handles.Label(transform.position + Vector3.up, InteractionPrompt);
    }
    #endif
}