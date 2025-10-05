using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    float InteractionDistance { get; }
    bool Interact(Transform interactor);
}