using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;

/// <summary>
/// Script simples para interação com itens que exibe um texto temporário na tela.
/// Quando o jogador interage, um TextMeshProUGUI é ativado por um tempo determinado.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SimpleItemDisplay : MonoBehaviour, IInteractable
{
    [Header("Localization Data")]
    [Tooltip("Referência à chave do prompt de interação (ex: PROMPT_READ_ITEM).")]
    [SerializeField] private LocalizedString promptString;
    [Tooltip("Referência à chave do texto que será exibido temporariamente (ex: ITEM_TEXT_NOTE).")]
    [SerializeField] private LocalizedString displayTextString;

    [Header("Display Settings")]
    [Tooltip("O TextMeshProUGUI que será ativado para exibir o texto.")]
    [SerializeField] private TextMeshProUGUI displayText;
    [Tooltip("Tempo em segundos que o texto ficará visível.")]
    [SerializeField] private float displayDuration = 3f;
    [Tooltip("Se verdadeiro, permite múltiplas interações. Se falso, só pode ser usado uma vez.")]
    [SerializeField] private bool canInteractMultipleTimes = true;

    // Controle interno
    private bool hasBeenUsed = false;
    private Coroutine currentDisplayCoroutine = null;

    // Propriedade da Interface
    public string InteractionPrompt
    {
        get
        {
            // Se já foi usado e não pode ser reutilizado, não mostra prompt
            if (hasBeenUsed && !canInteractMultipleTimes)
                return string.Empty;

            return promptString?.GetLocalizedString() ?? "Interagir";
        }
    }

    public bool Interact(Transform interactor)
    {
        // Verifica se pode interagir
        if (hasBeenUsed && !canInteractMultipleTimes)
            return false;

        // Verifica se já está exibindo texto
        if (currentDisplayCoroutine != null)
            return false;

        // Verifica se tem o componente de texto
        if (displayText == null)
        {
            Debug.LogWarning($"TextMeshProUGUI não está configurado em {gameObject.name}!", this);
            return false;
        }

        // Executa a interação
        ShowDisplayText();

        // Marca como usado
        hasBeenUsed = true;

        return true;
    }

    private void ShowDisplayText()
    {
        // Para qualquer corrotina anterior
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }

        // Inicia nova exibição
        currentDisplayCoroutine = StartCoroutine(DisplayTextCoroutine());
    }

    private IEnumerator DisplayTextCoroutine()
    {
        // Obtém o texto localizado
        string textToDisplay = displayTextString?.GetLocalizedString() ?? "Texto não configurado";
        
        // Configura e ativa o texto
        displayText.text = textToDisplay;
        displayText.enabled = true;

        // LOG para debug
        Debug.Log($"Exibindo texto: {textToDisplay} por {displayDuration} segundos", this);

        // Aguarda o tempo especificado
        yield return new WaitForSeconds(displayDuration);

        // Desativa o texto
        displayText.enabled = false;
        currentDisplayCoroutine = null;
    }

    private void OnValidate()
    {
        // Garante que a duração seja positiva
        if (displayDuration <= 0)
            displayDuration = 1f;
    }

    private void Reset()
    {
        // Configurações padrão quando o componente é adicionado
        displayDuration = 3f;
        canInteractMultipleTimes = true;
        
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