using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Cinemachine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private Transform cameraTransform;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Cinemachine")]
    [Tooltip("O GameObject da Cinemachine Camera do jogador.")]
    [SerializeField] private CinemachineCamera playerCinemachineCamera;

    public Transform CameraTransform => cameraTransform;

    private PlayerInputActions inputActions;
    private IInteractable currentInteractable;

    // Esta variável é nossa "trava" principal para o estado de inspeção.
    private bool isInInspectionMode = false;

    private CinemachinePanTilt panTilt;
    private CinemachineInputAxisController inputAxisController;

    private void Awake()
    {
        if (cameraTransform == null) { cameraTransform = transform; }
        inputActions = new PlayerInputActions();

        if (playerCinemachineCamera != null)
        {
            panTilt = playerCinemachineCamera.GetComponentInChildren<CinemachinePanTilt>();
            inputAxisController = playerCinemachineCamera.GetComponentInChildren<CinemachineInputAxisController>();
        }

        if (panTilt == null)
            Debug.LogWarning("Componente CinemachinePanTilt não encontrado.", this);
        if (inputAxisController == null)
            Debug.LogWarning("Componente CinemachineInputAxisController não encontrado.", this);
    }

    private void OnEnable()
    {
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Interact.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Interact.Disable();
    }

    private void Update()
    {
        // Se estamos inspecionando, o PlayerInteractor não faz mais nada.
        // A responsabilidade de sair da inspeção é 100% do item.
        if (isInInspectionMode) return;

        // Se não estamos inspecionando, procuramos por novos itens.
        CheckForInteractable();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        // Só permite a interação se houver um item na mira E não estivermos já inspecionando.
        if (currentInteractable != null && !isInInspectionMode)
        {
            currentInteractable.Interact(transform);
        }
    }

    // Renomeado para maior clareza. Controla o estado global do jogador.
    public void SetInspectionMode(bool state)
    {
        isInInspectionMode = state;

        PlayerMovement.canMove = !state;

        if (panTilt != null)
        {
            panTilt.enabled = !state;
        }
        if (inputAxisController != null)
        {
            inputAxisController.enabled = !state;
        }

        if (state)
        {
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
            // Esconde a UI de "pressione E para interagir" ao entrar na inspeção
            UpdateInteractionUI(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                if (interactable != currentInteractable)
                {
                    currentInteractable = interactable;
                    UpdateInteractionUI(true, interactable.InteractionPrompt);
                }
                return;
            }
        }
        if (currentInteractable != null)
        {
            currentInteractable = null;
            UpdateInteractionUI(false);
        }
    }

    private void UpdateInteractionUI(bool show, string prompt = "")
    {
        if (interactionText != null)
        {
            interactionText.enabled = show;
            interactionText.text = prompt;
        }
    }
}