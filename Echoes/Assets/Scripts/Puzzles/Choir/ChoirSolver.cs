using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(Collider))]
public class ChoirSolver : MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Prompt para quando o puzzle está ATIVO e este item pode ser usado para resolvê-lo.")]
    [SerializeField] private LocalizedString activePrompt;

    [Tooltip("Prompt para quando o puzzle está INATIVO.")]
    [SerializeField] private LocalizedString inactivePrompt;

    private bool isSolved = false;

    public string InteractionPrompt
    {
        get
        {
            // Se o puzzle já foi resolvido por este item, não mostra prompt.
            if (isSolved) return string.Empty;

            // Verifica se o puzzle está ativo para decidir qual prompt mostrar.
            if (DirectionalAudioPuzzleManager.Instance != null && DirectionalAudioPuzzleManager.Instance.IsPuzzleActive)
            {
                return activePrompt.GetLocalizedString();
            }
            else
            {
                return inactivePrompt.GetLocalizedString();
            }
        }
    }

    public bool Interact(Transform interactor)
    {
        if (isSolved) return false;

        // Só permite a interação se o puzzle estiver ativo.
        if (DirectionalAudioPuzzleManager.Instance != null && 
            DirectionalAudioPuzzleManager.Instance.IsPuzzleActive)
        {
            isSolved = true;
            DirectionalAudioPuzzleManager.Instance.SolvePuzzle();
            
            Debug.Log($"Item '{gameObject.name}' foi usado para resolver o puzzle.");
            // O item cumpriu seu propósito. A lógica do que acontece depois
            // (abrir a porta, etc.) seria acionada pelo evento OnAudioPuzzleSolved.
            
            return true;
        }
        else
        {
            Debug.Log("Este item não parece ter utilidade no momento.");
            // Opcional: Tocar um som de "não funciona"
            return false;
        }
    }
}