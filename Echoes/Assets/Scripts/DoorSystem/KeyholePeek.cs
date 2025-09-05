using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

[RequireComponent(typeof(Collider))]
public class KeyholePeek : MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt para 'espiar' (ex: PROMPT_PEEK).")]
    [SerializeField] private LocalizedString interactionPrompt;
    public string InteractionPrompt => interactionPrompt.GetLocalizedString();
    
    [Header("Mechanics")]
    [Tooltip("A quantidade de sanidade (0 a 1) perdida ao começar a espiar.")]
    [SerializeField, Range(0f, 1f)] private float sanityLossAmount = 0.1f;
    
    [Header("Dependencies")]
    [Tooltip("A Câmera Virtual da Cinemachine posicionada na fechadura.")]
    [SerializeField] private CinemachineCamera peekCamera;

    private bool isPeeking = false;
    private PlayerInteractor playerInteractor;

    private void Start()
    {
        playerInteractor = FindAnyObjectByType<PlayerInteractor>();
    }

    private void Update()
    {
        if (isPeeking)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                StopPeeking();
            }
        }
    }

    public bool Interact(Transform interactor)
    {
        if (!isPeeking)
        {
            StartPeeking();
            return true;
        }

        return false; 
    }

    private void StartPeeking()
    {
        isPeeking = true;
        GameEvents.TriggerSanityLost(sanityLossAmount);
        playerInteractor?.SetInspectionMode(true);
        if (peekCamera != null) peekCamera.Priority = 2;
    }

    private void StopPeeking()
    {
        isPeeking = false;
        playerInteractor.SetInspectionMode(false);
        if (peekCamera != null) peekCamera.Priority = 9;
    }
}