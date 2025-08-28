using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class RadioController : MonoBehaviour, IInteractable
{
    private enum SelectedDial { Fine, Coarse }

    [Header("Configuração da Interação")]
    [SerializeField] private string interactionPrompt = "(E) Sintonizar Rádio";
    [SerializeField] private CinemachineCamera radioCamera;
    [SerializeField] private CinemachineCamera playerCamera;

    [Header("Controles do Rádio")]
    [SerializeField] private Transform dialLeft;
    [SerializeField] private Transform dialRight;
    [SerializeField] private float rotationPerClick = 15f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Header("Lógica de Frequência")]
    [SerializeField] private float targetFrequency = 95.45f;
    private float currentFrequency;
    private bool isSolved = false;

    [Header("Highlight")]
    [SerializeField] private GameObject fineDialOutlineObject;
    [SerializeField] private GameObject coarseDialOutlineObject;

    [Header("Áudio")]
    [SerializeField] private AudioClip staticClip;
    [SerializeField] private AudioClip messageClip;
    private AudioSource audioSource;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI frequencyDisplayText;

    public string InteractionPrompt => isSolved ? string.Empty : interactionPrompt;

    private PlayerInputActions inputActions;
    private PlayerInteractor playerInteractor;
    private bool isInteracting = false;
    private SelectedDial currentDial = SelectedDial.Coarse;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        currentFrequency = 88.00f;
        UpdateFrequencyDisplay();
        UpdateDialHighlight();
    }

    private void OnEnable()
    {
        inputActions.Player.SwitchDial.performed += OnSwitchDial;
        inputActions.Player.Tune.performed += OnTune;
        inputActions.Player.Interact.performed += OnExitInteraction;
    }

    private void OnDisable()
    {
        inputActions.Player.SwitchDial.performed -= OnSwitchDial;
        inputActions.Player.Tune.performed -= OnTune;
        inputActions.Player.Interact.performed -= OnExitInteraction;
    }

    public bool Interact(Transform interactor)
    {
        // Permite interagir quantas vezes quiser, desde que não esteja resolvido
        if (isSolved) return false;
        if (isInteracting) return false; // Evita múltiplas entradas simultâneas

        playerInteractor = interactor.GetComponent<PlayerInteractor>();
        if (playerInteractor == null) return false;

        isInteracting = true;
        PlayerMovement.canMove = false; // Impede movimento e passos
        playerInteractor.SetInspectionMode(true);

        if (radioCamera != null) radioCamera.Priority.Value = 20;
        if (playerCamera != null) playerCamera.Priority.Value = -1;

        PlayAudioClip(staticClip);
        audioSource.loop = true;

        inputActions.Player.SwitchDial.Enable();
        inputActions.Player.Tune.Enable();
        inputActions.Player.Interact.Enable();

        UpdateDialHighlight();

        // Mostra o texto ao interagir
        if (frequencyDisplayText != null)
            frequencyDisplayText.enabled = true;

        return true;
    }

    private void ExitInteraction()
    {
        isInteracting = false;
        PlayerMovement.canMove = true; // Libera movimento e passos
        playerInteractor?.SetInspectionMode(false);

        if (radioCamera != null) radioCamera.Priority.Value = 9;
        if (playerCamera != null) playerCamera.Priority.Value = 10;

        audioSource.Stop();

        inputActions.Player.SwitchDial.Disable();
        inputActions.Player.Tune.Disable();
        inputActions.Player.Interact.Disable();

        UpdateDialHighlight();

        // Esconde o texto se não estiver resolvido
        if (frequencyDisplayText != null && !isSolved)
            frequencyDisplayText.enabled = false;
    }

    private void OnExitInteraction(InputAction.CallbackContext context)
    {
        if (isInteracting && !isSolved)
        {
            ExitInteraction();
        }
    }

    private void OnSwitchDial(InputAction.CallbackContext context)
    {
        if (!isInteracting) return;

        currentDial = (currentDial == SelectedDial.Coarse) ? SelectedDial.Fine : SelectedDial.Coarse;
        Debug.Log("Botão trocado para: " + currentDial);
        UpdateDialHighlight();
    }
    
    private void UpdateDialHighlight()
    {
        if (fineDialOutlineObject == null || coarseDialOutlineObject == null) return;
        
        bool shouldShowOutline = isInteracting && !isSolved;

        if (!shouldShowOutline)
        {
            fineDialOutlineObject.SetActive(false);
            coarseDialOutlineObject.SetActive(false);
            return;
        }

        if (currentDial == SelectedDial.Fine)
        {
            fineDialOutlineObject.SetActive(true);
            coarseDialOutlineObject.SetActive(false);
        }
        else 
        {
            fineDialOutlineObject.SetActive(false);
            coarseDialOutlineObject.SetActive(true);
        }
    }

    private void OnTune(InputAction.CallbackContext context)
    {
        if (!isInteracting) return;

        float direction = context.ReadValue<float>();
        float frequencyChange = 0;
        Transform dialToRotate = null;

        if (currentDial == SelectedDial.Coarse)
        {
            frequencyChange = 1.0f * direction;
            dialToRotate = dialRight;
        }
        else
        {
            frequencyChange = 0.01f * direction;
            dialToRotate = dialLeft;
        }

        currentFrequency = Mathf.Round((currentFrequency + frequencyChange) * 100f) / 100f;

        currentFrequency = Mathf.Clamp(currentFrequency, 88.0f, 108.0f);
        RotateDial(dialToRotate, direction);

        UpdateFrequencyDisplay();
        UpdateAudioFeedback();
        CheckForSolution();
    }
    
    private bool IsFrequencyCorrect()
    {
        return Mathf.Approximately(currentFrequency, targetFrequency);
    }

    private void RotateDial(Transform dial, float direction)
    {
        if (dial != null)
        {
            dial.Rotate(rotationAxis, rotationPerClick * direction, Space.Self);
        }
    }

    private void UpdateFrequencyDisplay()
    {
        if (frequencyDisplayText != null)
        {
            frequencyDisplayText.text = $"{currentFrequency:F2} MHz";
        }
    }

    private void PlayAudioClip(AudioClip clip)
    {
        if (isSolved) return;
        if (audioSource.clip != clip || !audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void UpdateAudioFeedback()
    {
        if (isSolved) return;

        if (IsFrequencyCorrect())
        {
            PlayAudioClip(messageClip);
            audioSource.loop = false;
        }
        else
        {
            PlayAudioClip(staticClip);
            audioSource.loop = true;

            // float distance = Mathf.Abs(currentFrequency - targetFrequency);
            // float maxAudibleDistance = 10.0f;
            // float staticVolume = Mathf.Clamp(distance / maxAudibleDistance, 0.1f, 1.0f);
            // audioSource.volume = staticVolume;
            audioSource.volume = 0.5f;
        }
    }

    private void CheckForSolution()
    {
        if (isSolved) return;

        if (IsFrequencyCorrect())
        {
            isSolved = true;
            Debug.Log("Frequência sintonizada corretamente!");

            audioSource.Stop();
            audioSource.clip = messageClip;
            audioSource.volume = 1.0f;
            audioSource.loop = false;
            audioSource.Play();

            inputActions.Player.SwitchDial.Disable();
            inputActions.Player.Tune.Disable();

            float exitDelay = (messageClip != null) ? messageClip.length + 0.5f : 5f;
            Invoke(nameof(ExitInteraction), exitDelay);
        }
    }
}