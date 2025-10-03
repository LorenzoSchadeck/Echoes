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

    private FMODAudioTrigger audioTrigger;
    private bool isRadioOn = false;
    private bool canInteract = true; // Controla se o rádio pode ser interagido
    [SerializeField] private bool isPuzzleMode = false;
    
    [Header("🔊 Sistema de Faixas")]
    [Tooltip("Primeira faixa - toca na primeira ativação")]
    [SerializeField] private EventReference track1Event;
    
    [Tooltip("Segunda faixa - toca na segunda ativação (modo puzzle)")]
    [SerializeField] private EventReference track2Event;
    
    [Tooltip("Terceira faixa - toca após resolver o puzzle")]
    [SerializeField] private EventReference track3Event;
    
    [Header("⏱️ Durações das Faixas")]
    [Tooltip("Duração da primeira faixa em segundos")]
    [SerializeField] private float track1Duration = 30f;
    
    [Tooltip("Duração da segunda faixa em segundos")]
    [SerializeField] private float track2Duration = 25f;
    
    [Tooltip("Duração da terceira faixa em segundos")]
    [SerializeField] private float track3Duration = 20f;
    
    [Header("🚪 Evento da Porta")]
    [Tooltip("Objeto que será habilitado após a primeira ativação")]
    [SerializeField] private GameObject objectToEnable;
    
    // Estado do sistema de faixas
    private int currentTrack = 0; // 0 = não iniciado, 1 = primeira faixa, 2 = segunda faixa, 3 = terceira faixa
    private bool isInStaticLoop = false;
    private bool puzzleSolved = false;
    
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
    }

    private void OnDisable()
    {
        inputActions.Player.SwitchDial.performed -= OnSwitchDial;
        inputActions.Player.Tune.performed -= OnTune;
        inputActions.Player.Interact.performed -= OnExitInteraction;
    }

    public bool Interact(Transform interactor)
    {
        if (!canInteract) return false;

        playerInteractor = interactor.GetComponent<PlayerInteractor>();
        if (playerInteractor == null) return false;

        // Primeira ativação - Faixa 1
        if (currentTrack == 0)
        {
            StartTrack1();
            return true;
        }

        // Segunda ativação - Faixa 2 (quando Track 1 terminou e está em estática)
        if (currentTrack == 1 && isInStaticLoop)
        {
            StartTrack2();
            return true;
        }

        // Se está em estática e não é modo puzzle, pode desligar
        if (!isPuzzleMode && isInStaticLoop)
        {
            TurnOffRadio();
            canInteract = false;
            return true;
        }

        // Se é modo puzzle, entra no modo de resolução
        if (isPuzzleMode && !puzzleSolved)
        {
            return EnterPuzzleMode(interactor);
        }

        // Se puzzle foi resolvido e está em estática, pode desligar
        if (puzzleSolved && isInStaticLoop)
        {
            TurnOffRadio();
            canInteract = false;
            return true;
        }

        return false;
    }

    private bool EnterPuzzleMode(Transform interactor)
    {
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

    #region Track System Methods

    /// <summary>
    /// Inicia a primeira faixa e toca o evento de batida na porta
    /// </summary>
    private void StartTrack1()
    {
        if (track1Event.IsNull) 
        {
            Debug.LogWarning("RadioController: Track 1 event não configurado!");
            return;
        }

        currentTrack = 1;
        isRadioOn = true;
        canInteract = false; // Desabilita durante a reprodução

        if (showDebugLogs) Debug.Log("RadioController: Iniciando Track 1");

        // Reproduz a faixa 1 via FMOD
        audioTrigger.fmodEvent = track1Event;
        audioTrigger.PlayAtPosition(transform.position);

        // Dispara evento para batida na porta
        if (showDebugLogs) Debug.Log("RadioController: Disparando evento de batida na porta");
        GameEvents.TriggerDoorKnock();

        // Ativa o GameObject se configurado
        if (objectToEnable != null)
        {
            if (showDebugLogs) Debug.Log($"RadioController: Ativando objeto {objectToEnable.name}");
            objectToEnable.SetActive(true);
        }

        // Inicia corrotina para gerenciar fim da faixa
        StartCoroutine(SimpleTrackCoroutine(track1Duration, OnTrack1Complete));
    }

    /// <summary>
    /// Chamado quando a faixa 1 termina
    /// </summary>
    private void OnTrack1Complete()
    {
        if (showDebugLogs) Debug.Log("RadioController: Track 1 completa, fim do período seguro!");
        
        // Dispara evento indicando que o período seguro terminou
        GameEvents.TriggerRadioTrack1Completed();
        
        // Entra em loop de estática
        isInStaticLoop = true;
        canInteract = true;

        // Reproduz estática de fundo
        PlayStaticLoop();
    }

    /// <summary>
    /// Inicia a segunda faixa após segunda ativação
    /// </summary>
    private void StartTrack2()
    {
        if (track2Event.IsNull) 
        {
            Debug.LogWarning("RadioController: Track 2 event não configurado!");
            return;
        }

        if (showDebugLogs) Debug.Log("RadioController: Iniciando Track 2");

        currentTrack = 2;
        isInStaticLoop = false;
        canInteract = false;

        // Para a estática
        StopStaticLoop();

        // Reproduz a faixa 2 via FMOD
        audioTrigger.fmodEvent = track2Event;
        audioTrigger.PlayAtPosition(transform.position);

        // Inicia corrotina para gerenciar fim da faixa
        StartCoroutine(SimpleTrackCoroutine(track2Duration, OnTrack2Complete));
    }

    /// <summary>
    /// Chamado quando a faixa 2 termina - entra em modo puzzle
    /// </summary>
    private void OnTrack2Complete()
    {
        if (showDebugLogs) Debug.Log("RadioController: Track 2 completa, ativando modo puzzle");
        
        isPuzzleMode = true;
        isInStaticLoop = true;
        canInteract = true;

        // Reproduz estática de fundo
        PlayStaticLoop();
    }

    /// <summary>
    /// Inicia a terceira faixa após puzzle resolvido
    /// </summary>
    private void StartTrack3()
    {
        if (track3Event.IsNull) 
        {
            Debug.LogWarning("RadioController: Track 3 event não configurado!");
            return;
        }

        if (showDebugLogs) Debug.Log("RadioController: Iniciando Track 3");

        currentTrack = 3;
        isInStaticLoop = false;
        canInteract = false;
        puzzleSolved = true;

        // Para a estática
        StopStaticLoop();

        // Reproduz a faixa 3 via FMOD
        audioTrigger.fmodEvent = track3Event;
        audioTrigger.PlayAtPosition(transform.position);

        // Inicia corrotina para gerenciar fim da faixa
        StartCoroutine(SimpleTrackCoroutine(track3Duration, OnTrack3Complete));
    }

    /// <summary>
    /// Chamado quando a faixa 3 termina
    /// </summary>
    private void OnTrack3Complete()
    {
        if (showDebugLogs) Debug.Log("RadioController: Track 3 completa, retornando à estática");
        
        isInStaticLoop = true;
        canInteract = true;

        // Reproduz estática de fundo
        PlayStaticLoop();
    }

    /// <summary>
    /// Corrotina simplificada para gerenciar reprodução de faixas
    /// </summary>
    private IEnumerator SimpleTrackCoroutine(float duration, System.Action onComplete)
    {
        if (onComplete == null) yield break;

        if (showDebugLogs) Debug.Log($"RadioController: Aguardando {duration}s para completar track");
        
        yield return new WaitForSeconds(duration);

        onComplete?.Invoke();
    }

    /// <summary>
    /// Reproduz loop de estática
    /// </summary>
    private void PlayStaticLoop()
    {
        if (showDebugLogs) Debug.Log("RadioController: Iniciando loop de estática");
        // TODO: Implementar reprodução de estática em loop usando FMOD
    }

    /// <summary>
    /// Para o loop de estática
    /// </summary>
    private void StopStaticLoop()
    {
        if (showDebugLogs) Debug.Log("RadioController: Parando loop de estática");
        // TODO: Implementar parada da estática
    }

    #endregion

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

    private void TurnOffRadio()
    {
        if (!isRadioOn) return;
        
        if (showDebugLogs) Debug.Log("RadioController: Desligando rádio...");
        
        isRadioOn = false;
        audioTrigger.Stop();
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
            puzzleSolved = true;
            
            if (showDebugLogs) Debug.Log("RadioController: Frequência sintonizada corretamente! Puzzle resolvido!");

            // Desabilita controles do puzzle
            inputActions.Player.SwitchDial.Disable();
            inputActions.Player.Tune.Disable();

            // Inicia a Track 3 após resolver o puzzle
            StartTrack3();

            // Sai da interação após um delay
            float exitDelay = 3f;
            Invoke(nameof(ExitInteraction), exitDelay);
        }
    }
}