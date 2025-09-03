using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class KeyholePeek : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string _peekPrompt = "Espiar";
    [SerializeField] private string _stopPeekingPrompt = "Parar de Espiar";

    [Header("Mechanics")]
    [Tooltip("A quantidade de sanidade (0 a 1) perdida ao começar a espiar.")]
    [SerializeField, Range(0f, 1f)] private float sanityLossAmount = 0.1f;
    
    [Header("Dependencies")]
    [Tooltip("A Câmera Virtual da Cinemachine posicionada na fechadura.")]
    [SerializeField] private CinemachineCamera peekCamera;

    private bool isPeeking = false;
    private PlayerInteractor playerInteractor;

    // A propriedade do prompt agora é dinâmica com base no estado
    public string InteractionPrompt => isPeeking ? _stopPeekingPrompt : _peekPrompt;

    private void Start()
    {
        // Encontra o interator do jogador para poder travar/destravar o movimento
        playerInteractor = FindAnyObjectByType<PlayerInteractor>();
    }

    private void Update()
    {
        // Só faz a verificação se estivermos ativamente espiando
        if (isPeeking)
        {
            // Verifica se o botão direito do mouse foi pressionado neste frame
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
        Debug.Log("Começando a espiar...");
        
        GameEvents.TriggerSanityLost(sanityLossAmount);
        playerInteractor?.SetInspectionMode(true);
        if (peekCamera != null) peekCamera.Priority = 2;
    }

    private void StopPeeking()
    {
        isPeeking = false;
        Debug.Log("Parando de espiar...");
        
        playerInteractor?.SetInspectionMode(false);
        if (peekCamera != null) peekCamera.Priority = 0;
    }
}