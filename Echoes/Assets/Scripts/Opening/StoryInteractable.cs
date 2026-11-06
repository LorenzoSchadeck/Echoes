using System;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Objeto que o roteiro libera para o jogador usar uma vez (examinar uma pista, abrir a porta,
/// sair pela janela). Quem escuta o evento <see cref="Used"/> decide o que acontece.
/// </summary>
[RequireComponent(typeof(Collider))]
public class StoryInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private LocalizedString prompt = new LocalizedString("interaction_prompts", "PROMPT_EXAMINE");
    [SerializeField] private float interactionDistance = 6f;
    [Tooltip("Começa disponível? Senão, o roteiro libera com Available = true.")]
    [SerializeField] private bool availableOnStart = true;

    public event Action<StoryInteractable> Used;

    public bool Available { get; set; }
    public bool WasUsed { get; private set; }

    public string InteractionPrompt => CanUse ? InteractionText.Get(prompt, "PROMPT_INSPECTION") : string.Empty;
    public float InteractionDistance => interactionDistance;

    // Não interrompe uma conversa em andamento
    private bool CanUse => Available && !WasUsed && !Dialogue.IsPlaying && !PlayerControlLock.IsLocked;

    private void Awake() => Available = availableOnStart;

    public bool Interact(Transform interactor)
    {
        if (!CanUse) return false;
        WasUsed = true;
        Used?.Invoke(this);
        return true;
    }

#if UNITY_EDITOR
    public void EditorSetup(string promptKey, float distance, bool startAvailable)
    {
        prompt = new LocalizedString("interaction_prompts", promptKey);
        interactionDistance = distance;
        availableOnStart = startAvailable;
    }
#endif
}
