using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TMPro;
using UnityEngine.Localization;
using FMODUnity;
using System.Collections;

public class RadioController : MonoBehaviour, IInteractable
{
    private enum SelectedDial { Fine, Coarse }

    [Header("Configuração da Interação")]
    [SerializeField] private CinemachineCamera radioCamera;
    [SerializeField] private CinemachineCamera playerCamera;

    [Header("Localization")]
    [SerializeField] private LocalizedString puzzleInteractionPrompt;
    [SerializeField] private LocalizedString normalInteractionPrompt;
    public string InteractionPrompt => (isSolved || !canInteract) ? string.Empty : 
        (isPuzzleMode ? puzzleInteractionPrompt.GetLocalizedString() : normalInteractionPrompt.GetLocalizedString());

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

    [Header("Sistema de Transmissões")]
    [SerializeField] private RadioTransmission[] availableTransmissions;
    private FMODAudioTrigger audioTrigger;
    private Coroutine radioCoroutine;
    private bool isRadioOn = false;
    private bool canInteract = true; // Controla se o rádio pode ser interagido
    [SerializeField] private bool isPuzzleMode = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI frequencyDisplayText;

    private PlayerInputActions inputActions;
    private PlayerInteractor playerInteractor;
    private bool isInteracting = false;
    private SelectedDial currentDial = SelectedDial.Coarse;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        audioTrigger = gameObject.GetComponent<FMODAudioTrigger>();
        if (audioTrigger == null)
            audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
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
        GameEvents.OnRadioActivated += OnRadioActivated;
        GameEvents.OnRadioTransmissionStarted += OnRadioTransmissionStarted;
    }

    private void OnDisable()
    {
        inputActions.Player.SwitchDial.performed -= OnSwitchDial;
        inputActions.Player.Tune.performed -= OnTune;
        inputActions.Player.Interact.performed -= OnExitInteraction;
        GameEvents.OnRadioActivated -= OnRadioActivated;
        GameEvents.OnRadioTransmissionStarted -= OnRadioTransmissionStarted;
    }

    public bool Interact(Transform interactor)
    {
        if (isSolved || !canInteract) return false;

        playerInteractor = interactor.GetComponent<PlayerInteractor>();
        if (playerInteractor == null) return false;

        // Se não for puzzle, apenas desliga o rádio (se estiver ligado)
        if (!isPuzzleMode)
        {
            if (isRadioOn)
            {
                TurnOffRadio();
                canInteract = false; // Desabilita interação até próxima transmissão
            }
            return true;
        }

        // Modo puzzle: comportamento completo com câmera e controles
        if (isInteracting) return false;

        isInteracting = true;
        PlayerMovement.canMove = false;
        playerInteractor.SetInspectionMode(true);

        if (radioCamera != null) radioCamera.Priority.Value = 20;
        if (playerCamera != null) playerCamera.Priority.Value = -1;

        inputActions.Player.SwitchDial.Enable();
        inputActions.Player.Tune.Enable();
        inputActions.Player.Interact.Enable();

        UpdateDialHighlight();

        if (frequencyDisplayText != null)
            frequencyDisplayText.enabled = true;

        return true;
    }

    // Método chamado pelo GameEvent quando o rádio deve ser ativado (primeira vez)
    private void OnRadioActivated()
    {
        if (showDebugLogs) Debug.Log($"RadioController: Evento OnRadioActivated recebido! isPuzzleMode: {isPuzzleMode}");
        if (!isPuzzleMode && availableTransmissions.Length > 0)
        {
            if (showDebugLogs) Debug.Log("RadioController: Iniciando primeira transmissão...");
            StartTransmission(availableTransmissions[0]);
        }
    }

    // Método chamado quando uma transmissão específica deve ser iniciada
    private void OnRadioTransmissionStarted(int transmissionIndex)
    {
        if (showDebugLogs) Debug.Log($"RadioController: Transmissão {transmissionIndex} solicitada!");
        
        if (transmissionIndex >= 0 && transmissionIndex < availableTransmissions.Length)
        {
            StartTransmission(availableTransmissions[transmissionIndex]);
        }
        else
        {
            Debug.LogWarning($"RadioController: Índice de transmissão inválido: {transmissionIndex}");
        }
    }

    private void ExitInteraction()
    {
        isInteracting = false;
        PlayerMovement.canMove = true;
        playerInteractor?.SetInspectionMode(false);

        if (radioCamera != null) radioCamera.Priority.Value = 9;
        if (playerCamera != null) playerCamera.Priority.Value = 10;

        if (!isPuzzleMode)
        {
            TurnOffRadio();
        }
        // else: lógica de puzzle será implementada depois

        inputActions.Player.SwitchDial.Disable();
        inputActions.Player.Tune.Disable();
        inputActions.Player.Interact.Disable();

        UpdateDialHighlight();

        if (frequencyDisplayText != null && !isSolved)
            frequencyDisplayText.enabled = false;
    }
    
    // --- Sistema Modular de Transmissões ---
    private void StartTransmission(RadioTransmission transmission)
    {
        if (transmission == null)
        {
            Debug.LogWarning("RadioController: Transmissão nula!");
            return;
        }

        if (isRadioOn)
        {
            TurnOffRadio();
        }

        canInteract = true; // Habilita interação quando uma nova transmissão inicia
        isRadioOn = true;
        
        // Configura o audioTrigger com o evento da transmissão
        audioTrigger.fmodEvent = transmission.radioEvent;
        
        if (showDebugLogs) Debug.Log($"RadioController: Iniciando transmissão '{transmission.transmissionName}'");
        
        if (radioCoroutine != null) StopCoroutine(radioCoroutine);
        radioCoroutine = StartCoroutine(TransmissionRoutine(transmission));
    }

    private void TurnOffRadio()
    {
        if (!isRadioOn) return;
        
        if (showDebugLogs) Debug.Log("RadioController: Desligando rádio...");
        
        isRadioOn = false;
        if (radioCoroutine != null)
        {
            StopCoroutine(radioCoroutine);
            radioCoroutine = null;
        }
        audioTrigger.Stop();
    }

    // Rotina completa de uma transmissão: startup -> mumble -> estática
    private IEnumerator TransmissionRoutine(RadioTransmission transmission)
    {
        if (showDebugLogs) Debug.Log($"RadioController: Iniciando rotina da transmissão '{transmission.transmissionName}'");
        
        // 1. "Startup" - toca completamente antes de começar mumble
        if (showDebugLogs) Debug.Log($"RadioController: Startup - parâmetro {transmission.startupParameterValue}");
        audioTrigger.SetParameter(transmission.radioParameter, transmission.startupParameterValue);
        audioTrigger.PlayAtPosition(transform.position);
        yield return new WaitForSeconds(transmission.startupDuration);

        // 2. "Mumble" - cada parâmetro toca por 1 segundo completo
        if (showDebugLogs) Debug.Log($"RadioController: Mumble por {transmission.transmissionDuration}s");
        float timer = 0f;
        while (timer < transmission.transmissionDuration)
        {
            int mumbleValue = Random.Range(transmission.mumbleMinValue, transmission.mumbleMaxValue + 1);
            if (showDebugLogs) Debug.Log($"RadioController: Mumble - parâmetro {mumbleValue}");
            audioTrigger.SetParameterRealTime(transmission.radioParameter, mumbleValue);
            yield return new WaitForSeconds(transmission.mumbleChangeInterval);
            timer += transmission.mumbleChangeInterval;
        }

        // 3. "Estática" - permanece até o player desligar
        if (showDebugLogs) Debug.Log($"RadioController: Estática - parâmetro {transmission.staticParameterValue}");
        audioTrigger.SetParameterRealTime(transmission.radioParameter, transmission.staticParameterValue);
    }

    private void OnExitInteraction(InputAction.CallbackContext context)
    {
        if (isInteracting)
        {
            if (!isPuzzleMode || !isSolved)
            {
                ExitInteraction();
            }
        }
    }

    private void OnSwitchDial(InputAction.CallbackContext context)
    {
        if (!isInteracting || !isPuzzleMode) return;

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
        if (!isInteracting || !isPuzzleMode) return;

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

        if (currentDial == SelectedDial.Fine)
        {
            int intPart = (int)currentFrequency;
            int decimalPart = Mathf.RoundToInt((currentFrequency - intPart) * 100);

            if (decimalPart % 2 == 0)
            {
                if (direction > 0)
                    decimalPart += 1;
                else
                    decimalPart -= 1;
            }

            decimalPart = Mathf.Clamp(decimalPart, 0, 99);

            currentFrequency = intPart + (decimalPart / 100f);
        }

        RotateDial(dialToRotate, direction);
        UpdateFrequencyDisplay();
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

    // --- Fim da lógica de áudio do rádio ---
    private void CheckForSolution()
    {
        if (isSolved) return;

        if (IsFrequencyCorrect())
        {
            isSolved = true;
            Debug.Log("Frequência sintonizada corretamente!");

            // Aqui você pode tocar um som de sucesso ou mensagem, se desejar
            // Exemplo: radioInstance.setParameterByName(radioParameter, <label_sucesso>);

            inputActions.Player.SwitchDial.Disable();
            inputActions.Player.Tune.Disable();

            // FMOD: Não há como saber a duração do evento diretamente, ajuste conforme necessário
            float exitDelay = 5f;
            Invoke(nameof(ExitInteraction), exitDelay);
        }
    }
}