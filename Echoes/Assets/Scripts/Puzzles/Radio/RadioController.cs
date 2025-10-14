using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TMPro;
using UnityEngine.Localization;
using FMODUnity;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RadioController : MonoBehaviour, IInteractable
{
    private enum SelectedDial { Fine, Coarse }

    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este rádio pode ser interagido")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Header("Configuração da Interação")]
    [SerializeField] private CinemachineCamera radioCamera;
    [SerializeField] private CinemachineCamera playerCamera;

    [Header("Localization")]
    [SerializeField] private LocalizedString puzzleInteractionPrompt;
    [SerializeField] private LocalizedString normalInteractionPrompt;
        public string InteractionPrompt 
    {
        get => CanShowPrompt() ? GetCurrentPrompt() : string.Empty;
    }
    
    public float InteractionDistance => interactionDistance;
    
    private bool CanShowPrompt()
    {
        // PRIMEIRA VERIFICAÇÃO: Sem prompt se não pode interagir (desligado permanentemente)
        if (!canInteract) 
        {
            return false;
        }
        
        // SEGUNDA VERIFICAÇÃO: Sem prompt enquanto o rádio estiver desligado
        if (currentState == RadioState.Off) 
        {
            return false;
        }
        
        // TERCEIRA VERIFICAÇÃO: Track 2 - nunca mostra prompt (não pode ser desligada)
        if (currentState == RadioState.Track2Playing)
        {
            return false;
        }
        
        // QUARTA VERIFICAÇÃO: Modo puzzle - sempre mostra prompt de sintonizar
        if (currentState == RadioState.PuzzleMode)
        {
            return true;
        }
        
        // QUINTA VERIFICAÇÃO: Durante reprodução de tracks, verifica tempo mínimo
        if (IsPlayingTrack() && !HasMinimumPlayTimePassed())
        {
            return false; // Não mostra prompt se ainda não passou tempo mínimo
        }
        
        // SEXTA VERIFICAÇÃO: Track 1 Static e Track 3 após tempo mínimo
        if (currentState == RadioState.Track1Static || 
            (currentState == RadioState.Track3Playing && HasMinimumPlayTimePassed()))
        {
            return true;
        }
        
        // SÉTIMA VERIFICAÇÃO: Track 1 após tempo mínimo
        if (currentState == RadioState.Track1Playing && HasMinimumPlayTimePassed())
        {
            return true;
        }
        
        return false;
    }
    
    private bool IsPlayingTrack()
    {
        return currentState == RadioState.Track1Playing || 
               currentState == RadioState.Track2Playing || 
               currentState == RadioState.Track3Playing;
    }
    
    private bool HasMinimumPlayTimePassed()
    {
        return Time.time >= trackStartTime + minPlayTimeBeforeShutdown;
    }
    
    private string GetCurrentPrompt()
    {
        switch (currentState)
        {
            case RadioState.Track1Playing:
                // Track 1 - pode desligar após tempo mínimo
                if (HasMinimumPlayTimePassed())
                    return normalInteractionPrompt.GetLocalizedString();
                return ""; // Sem prompt se ainda não passou tempo mínimo
                
            case RadioState.Track1Static:
                // Em estática após Track 1 - pode desligar
                return normalInteractionPrompt.GetLocalizedString();
                
            case RadioState.Track2Playing:
                // Track 2 - NÃO pode ser desligada
                return ""; // Sem prompt
                
            case RadioState.Track2Static:
                // Estado removido - não deveria existir mais
                return "";
                
            case RadioState.PuzzleMode:
                // Modo puzzle - usa prompt de puzzle (sintonizar)
                return puzzleInteractionPrompt.GetLocalizedString();
                
            case RadioState.Track3Playing:
                // Track 3 - pode desligar APENAS após tempo mínimo (33s)
                if (HasMinimumPlayTimePassed())
                    return normalInteractionPrompt.GetLocalizedString();
                return ""; // Sem prompt se ainda não passou tempo mínimo
                
            case RadioState.Off:
                // Rádio desligado - nunca mostra prompt
                return "";
                
            default:
                return "";
        }
    }
    
    private void OnFirstTrigger()
    {
        if (currentState == RadioState.Off)
        {
            StartTrack1();
        }
    }
    
    private IEnumerator PlayFirstTrack()
    {
        // Método de compatibilidade - apenas chama StartTrack1
        StartTrack1();
        yield return null;
    }
    
    private void OnPaperTrigger()
    {
        Debug.Log($"[RadioController] OnPaperTrigger CHAMADO! Estado: {currentState}, Track1 encerrada: {track1HasEnded}");
        
        // REGRA RESTRITIVA: Papel só pode ser usado quando Track 1 terminou E rádio está desligado
        if (!track1HasEnded)
        {
            Debug.LogWarning("[RadioController] PAPEL NEGADO - Track 1 ainda não foi encerrada completamente!");
            return;
        }
        
        if (currentState != RadioState.Off)
        {
            Debug.LogWarning($"[RadioController] PAPEL NEGADO - rádio ainda está ligado! Estado: {currentState}");
            Debug.LogWarning("[RadioController] DESLIGUE O RÁDIO primeiro, depois use o papel!");
            return;
        }
        
        Debug.Log("[RadioController] PAPEL ACEITO! Track 1 encerrada e rádio desligado. Ligando rádio e iniciando Track 2");
        
        // Liga o rádio novamente e inicia Track 2
        currentState = RadioState.Track2Playing;
        canInteract = true;
        
        // Marca os triggers como bem-sucedidos
        MarkPaperTriggersAsUsed();
        
        StartTrack2();
    }

    [Header("Controles do Rádio")]
    [SerializeField] private Transform dialLeft;
    [SerializeField] private Transform dialRight;
    [SerializeField] private float rotationPerClick = 15f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Header("Lógica de Frequência")]
    [SerializeField] private float targetFrequency = 95.45f;
    [Tooltip("Frequência mínima do rádio (MHz)")]
    [SerializeField] private float minFrequency = 88.0f;
    [Tooltip("Frequência máxima do rádio (MHz)")]
    [SerializeField] private float maxFrequency = 108.0f;
    private float currentFrequency;
    private bool isSolved = false;

    [Header("Highlight")]
    [SerializeField] private GameObject fineDialOutlineObject;
    [SerializeField] private GameObject coarseDialOutlineObject;

    private FMODAudioTrigger audioTrigger;
    // private bool isRadioOn = false;
    private bool canInteract = true; // Controla se o rádio pode ser interagido
    [SerializeField] private bool isPuzzleMode = false;
    
    [Header("🔊 Sistema de Faixas")]
    [Tooltip("Primeira faixa - toca na primeira ativação")]
    [SerializeField] private EventReference track1Event;
    
    [Tooltip("Segunda faixa - toca na segunda ativação (modo puzzle)")]
    [SerializeField] private EventReference track2Event;
    
    [Tooltip("Terceira faixa - toca após resolver o puzzle")]
    [SerializeField] private EventReference track3Event;
    
    [Header("⏱️ Proteção de Desligamento")]
    [Tooltip("Tempo mínimo em segundos antes de permitir desligar o rádio")]
    [SerializeField] private float minPlayTimeBeforeShutdown = 33f;
    
    [Header("🔊 Configurações de Áudio")]
    [Tooltip("Distância máxima em que o áudio do rádio pode ser ouvido")]
    [SerializeField] private float maxAudioRange = 70f;
    
    [Header("🚪 Eventos após Track 1")]
    [Tooltip("Lista de objetos que serão habilitados após a primeira ativação (Track 1)")]
    [SerializeField] private GameObject[] objectsToEnable;
    
    [Header("📝 Sistema de Legendas")]
    [Tooltip("Gerenciador de legendas do rádio (opcional)")]
    [SerializeField] private RadioSubtitleManager subtitleManager;
    
    // Estado do sistema de faixas - NOVO FLUXO
    public enum RadioState { Off, Track1Playing, Track1Static, Track2Playing, Track2Static, PuzzleMode, Track3Playing }
    private RadioState currentState = RadioState.Off;
    // private bool hasBeenTriggeredFirst = false; // Se foi ativado pelo primeiro trigger
    // private bool hasBeenTriggeredSecond = false; // Se foi ativado pelo papel
    // private bool puzzleSolved = false;
    
    // Controle de proteção de desligamento
    private float trackStartTime = 0f; // Quando a track atual começou
    private FMOD.Studio.EventInstance currentEventInstance; // Instância atual do evento FMOD
    
    // Controle de progresso das tracks
    private bool track1HasBeenPlayed = false; // Se Track 1 já foi tocada
    private bool track1HasEnded = false; // Se Track 1 foi encerrada (terminada ou desligada)
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI frequencyDisplayText;
    
    [Header("🎬 DEMO ONLY - Fade & Reset (Track 3) - REMOVER NA VERSÃO FINAL")]
    [Tooltip("Canvas com a imagem preta para fade (DEMO ONLY)")]
    [SerializeField] private Canvas fadeCanvas;
    [Tooltip("Imagem preta para o fade (DEMO ONLY)")]
    [SerializeField] private Image fadeImage;
    [Tooltip("Duração do fade para preto (DEMO ONLY)")]
    [SerializeField] private float fadeDuration = 2f;

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
            
        // CORREÇÃO: Reset completo do estado do rádio quando a cena carrega
        ResetRadioStateOnSceneLoad();
    }

    private void Update()
    {
        // Cancelamento via botão direito do mouse durante interação com o rádio
        if (isInteracting && isPuzzleMode)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                ExitInteraction();
            }
        }
    }

    private void Start()
    {
        // Validação da frequência alvo
        ValidateFrequencySettings();
        
        currentFrequency = minFrequency; // Inicia na frequência mínima (88.00 MHz)
        UpdateFrequencyDisplay();
        UpdateDialHighlight();
        
        // Garantir que a câmera inicia com prioridade -1 (inativa)
        if (radioCamera != null)
        {
            radioCamera.Priority.Value = -1;
            Debug.Log($"[RadioController] Câmera inicializada com Priority: {radioCamera.Priority.Value}");
        }
    }

    private void OnEnable()
    {
        inputActions.Player.SwitchDial.performed += OnSwitchDial;
        inputActions.Player.Tune.performed += OnTune;
        inputActions.Player.Interact.performed += OnExitInteraction;
        
        // Novos eventos do fluxo
        GameEvents.OnRadioFirstTrigger += OnFirstTrigger;
        GameEvents.OnRadioPaperTrigger += OnPaperTrigger;
        
        // CORREÇÃO: Listeners para reset completo do sistema
        GameEvents.OnSceneReset += OnSceneResetTriggered;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        inputActions.Player.SwitchDial.performed -= OnSwitchDial;
        inputActions.Player.Tune.performed -= OnTune;
        inputActions.Player.Interact.performed -= OnExitInteraction;
        
        // Remover eventos do fluxo
        GameEvents.OnRadioFirstTrigger -= OnFirstTrigger;
        GameEvents.OnRadioPaperTrigger -= OnPaperTrigger;
        
        // CORREÇÃO: Remove listeners de reset do sistema
        GameEvents.OnSceneReset -= OnSceneResetTriggered;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public bool Interact(Transform interactor)
    {
        if (!canInteract) return false;

        playerInteractor = interactor.GetComponent<PlayerInteractor>();
        if (playerInteractor == null) return false;

        // Sistema baseado em estados
        switch (currentState)
        {
            case RadioState.Off:
                // Rádio desligado - sem interação manual
                return false;
                
            case RadioState.Track1Playing:
                // Tocando Track 1 - pode desligar apenas após tempo mínimo (UMA VEZ)
                if (HasMinimumPlayTimePassed())
                {
                    TurnOffRadio(); // Já desabilita canInteract internamente
                    return true;
                }
                return false;
                
            case RadioState.Track1Static:
                // Em estática após Track 1 - pode desligar (UMA VEZ)
                TurnOffRadio(); // Já desabilita canInteract internamente
                return true;
                
            case RadioState.Track2Playing:
                // Track 2 - não pode ser desligada, mas após 33s vira modo puzzle
                return false;
                
            case RadioState.Track2Static:
                // Estado removido
                return false;
                
            case RadioState.PuzzleMode:
                // Modo puzzle - entra na interação de sintonização
                return EnterPuzzleMode(interactor);
                
            case RadioState.Track3Playing:
                // Tocando Track 3 - pode desligar apenas após tempo mínimo (UMA VEZ)
                if (HasMinimumPlayTimePassed())
                {
                    TurnOffRadio(); // Já desabilita canInteract internalmente
                    return true;
                }
                return false;
                
            default:
                return false;
        }
    }

    private bool EnterPuzzleMode(Transform interactor)
    {
        if (isInteracting) return false;

        isInteracting = true;
        PlayerMovement.canMove = false;
        playerInteractor.SetInspectionMode(true);

        if (radioCamera != null) 
        {
            radioCamera.Priority.Value = 15; // Prioridade alta quando ativo
            Debug.Log($"[RadioController] Câmera ativada - Priority: {radioCamera.Priority.Value}");
        }
        else
        {
            Debug.LogError("[RadioController] radioCamera é null! Verifique a atribuição no Inspector.");
        }
        
        if (playerCamera != null) 
        {
            playerCamera.Priority.Value = 1; // Player camera mantém prioridade padrão
        }

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

        currentState = RadioState.Track1Playing;
        canInteract = true; // Permite desligar durante reprodução
        
        // Verifica se é a primeira vez antes de marcar como tocada
        bool isFirstTimeTrack1 = !track1HasBeenPlayed;
        track1HasBeenPlayed = true; // Marca que Track 1 foi tocada

        // Reproduz a faixa 1 via FMOD usando instância direta
        currentEventInstance = FMODUnity.RuntimeManager.CreateInstance(track1Event);
        
        if (currentEventInstance.isValid())
        {
            // Define posição 3D e range máximo
            currentEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            currentEventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxAudioRange);
            
            FMOD.RESULT result = currentEventInstance.start();
        }
        else
        {
            Debug.LogError("RadioController: Falha ao criar instância FMOD para Track 1!");
        }
        
        // Marca o tempo de início
        trackStartTime = Time.time;

        // Inicia legendas se o manager estiver configurado
        if (subtitleManager != null)
        {
            subtitleManager.StartTrack1Subtitles(isFirstTimeTrack1);
        }

        // Inicia corrotina para monitorar fim da faixa
        StartCoroutine(MonitorTrackCompletion(OnTrack1Complete));
    }

    /// <summary>
    /// Chamado quando a faixa 1 termina
    /// </summary>
    private void OnTrack1Complete()
    {
        // Marca Track 1 como encerrada (terminou naturalmente)
        track1HasEnded = true;

        // CORREÇÃO: Dispara evento para fim do período seguro de sanidade
        GameEvents.TriggerRadioTrack1Completed();

        // Entra em estática após Track 1
        currentState = RadioState.Track1Static;
        canInteract = true;

        // Para as legendas da Track 1 quando ela termina naturalmente
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
        }

        // Ativa objetos após término natural da Track 1
        if (objectsToEnable != null && objectsToEnable.Length > 0)
        {
            for (int i = 0; i < objectsToEnable.Length; i++)
            {
                if (objectsToEnable[i] != null)
                {
                    objectsToEnable[i].SetActive(true);
                }
            }
        }

        // Inicia corrotina para disparar evento da porta com delay de 1 segundo (término natural)
        StartCoroutine(TriggerDoorKnockAfterDelay());

    }    
    
    /// <summary>
    /// Inicia a segunda faixa após ativação por papel
    /// </summary>
    private void StartTrack2()
    {
        if (track2Event.IsNull) 
        {
            Debug.LogError("RadioController: ERRO - Track 2 event não configurado! Verifique no Inspector se track2Event está definido.");
            return;
        }

        currentState = RadioState.Track2Playing;
        canInteract = true; // Permite interação (modo puzzle após 33s)

        // Reproduz a faixa 2 via FMOD usando instância direta
        currentEventInstance = FMODUnity.RuntimeManager.CreateInstance(track2Event);
        
        if (currentEventInstance.isValid())
        {
            // Define posição 3D e range máximo
            currentEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            currentEventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxAudioRange);
            
            FMOD.RESULT result = currentEventInstance.start();
        }
        else
        {
            Debug.LogError("RadioController: Falha ao criar instância FMOD para Track 2!");
        }
        
        // Marca o tempo de início
        trackStartTime = Time.time;

        // Inicia legendas se o manager estiver configurado
        if (subtitleManager != null)
        {
            subtitleManager.StartTrack2Subtitles(true);
        }

        // Inicia corrotina para ativar modo puzzle após 33 segundos
        StartCoroutine(ActivatePuzzleModeAfterDelay());
        
        // Inicia corrotina para monitorar fim da faixa (quando Track 2 terminar naturalmente)
        StartCoroutine(MonitorTrackCompletion(OnTrack2Complete));
    }

    /// <summary>
    /// Ativa o modo puzzle após 33 segundos da Track 2, mantendo o áudio tocando
    /// </summary>
    private IEnumerator ActivatePuzzleModeAfterDelay()
    {
        // Aguarda 33 segundos
        yield return new WaitForSeconds(minPlayTimeBeforeShutdown);
        
        if (currentState == RadioState.Track2Playing)
        {
            // Muda para modo puzzle mas mantém Track 2 tocando
            currentState = RadioState.PuzzleMode;
            isPuzzleMode = true;
            
            // Inicia corrotina para disparar evento Track 2 completada com delay de 1 segundo
            StartCoroutine(TriggerRadioTrack2CompletedAfterDelay());
        }
    }

    /// <summary>
    /// Chamado quando a faixa 2 termina naturalmente - deve reiniciar em loop
    /// </summary>
    private void OnTrack2Complete()
    {
        // Se ainda não está em modo puzzle, ativa agora
        if (currentState != RadioState.PuzzleMode)
        {
            currentState = RadioState.PuzzleMode;
            isPuzzleMode = true;
            
            // Inicia corrotina para disparar evento Track 2 completada com delay de 1 segundo
            StartCoroutine(TriggerRadioTrack2CompletedAfterDelay());
        }
        
        // REINICIA Track 2 em loop - só para quando puzzle for resolvido
        if (currentState == RadioState.PuzzleMode && !isSolved)
        {
            RestartTrack2Loop();
        }
    }
    
    /// <summary>
    /// Reinicia a Track 2 em loop durante o modo puzzle
    /// </summary>
    private void RestartTrack2Loop()
    {
        if (track2Event.IsNull) return;
        
        // Cria nova instância da Track 2
        currentEventInstance = FMODUnity.RuntimeManager.CreateInstance(track2Event);
        
        if (currentEventInstance.isValid())
        {
            // Define posição 3D e range máximo
            currentEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            currentEventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxAudioRange);

            FMOD.RESULT result = currentEventInstance.start();
            
            // Continua monitorando para próximo loop
            StartCoroutine(MonitorTrackCompletion(OnTrack2Complete));
        }
        else
        {
            Debug.LogError("RadioController: Falha ao reiniciar Track 2 em loop!");
        }
    }

    /// <summary>
    /// Inicia a terceira faixa após puzzle resolvido - para definitivamente a Track 2
    /// </summary>
    private void StartTrack3()
    {
        if (track3Event.IsNull) 
        {
            Debug.LogWarning("RadioController: Track 3 event não configurado!");
            return;
        }

        currentState = RadioState.Track3Playing;
        canInteract = true; // Permite desligar durante reprodução
        isPuzzleMode = false; // Sai do modo puzzle
        
        // PARA DEFINITIVAMENTE a Track 2 (que estava em loop)
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Para imediatamente
            currentEventInstance.release();
        }

        // Para todas as corrotinas de monitoramento da Track 2
        StopAllCoroutines();

        // Reproduz a faixa 3 via FMOD usando nova instância
        currentEventInstance = FMODUnity.RuntimeManager.CreateInstance(track3Event);
        
        if (currentEventInstance.isValid())
        {
            // Define posição 3D e range máximo
            currentEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            currentEventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxAudioRange);
            
            FMOD.RESULT result = currentEventInstance.start();
        }
        else
        {
            Debug.LogError("RadioController: Falha ao criar instância FMOD para Track 3!");
        }
        
        // Marca o tempo de início
        trackStartTime = Time.time;

        // Inicia legendas se o manager estiver configurado
        if (subtitleManager != null)
        {
            subtitleManager.StartTrack3Subtitles(true);
        }

        // Inicia corrotina para monitorar fim da faixa
        StartCoroutine(MonitorTrackCompletion(OnTrack3Complete));
        
        // 🎬 DEMO ONLY: Inicia monitoramento para fade automático após 33 segundos
        // TODO: REMOVER esta linha na versão final do jogo
        StartCoroutine(MonitorTrack3FadeTimer());
    }

    /// <summary>
    /// Chamado quando a faixa 3 termina - fim da primeira fase
    /// </summary>
    private void OnTrack3Complete()
    { 
        // Rádio desliga automaticamente após Track 3 terminar
        currentState = RadioState.Off;
        canInteract = false;
        isPuzzleMode = false;
        
        // Para as legendas se o manager estiver configurado
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
        }
        
        audioTrigger.Stop();
        
        // Limpa a instância FMOD
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentEventInstance.release();
        }
    }

    /// <summary>
    /// Corrotina que monitora se o áudio parou de tocar
    /// </summary>
    private IEnumerator MonitorTrackCompletion(System.Action onComplete)
    {
        if (onComplete == null) yield break;
        
        // Aguarda pelo menos 1 segundo antes de começar a verificar
        yield return new WaitForSeconds(1f);
        
        // Monitora o estado do evento FMOD
        while (true)
        {
            if (!currentEventInstance.isValid()) break;
            
            FMOD.Studio.PLAYBACK_STATE playbackState;
            currentEventInstance.getPlaybackState(out playbackState);
            
            if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED) break;
            
            yield return new WaitForSeconds(0.1f); // Verifica a cada 100ms
        }

        onComplete?.Invoke();
    }

    #endregion

    #region Frequency Validation

    /// <summary>
    /// Valida as configurações de frequência no Inspector
    /// </summary>
    private void ValidateFrequencySettings()
    {
        if (minFrequency >= maxFrequency)
        {
            Debug.LogError($"[RadioController] ERRO: minFrequency ({minFrequency}) deve ser menor que maxFrequency ({maxFrequency})!");
            minFrequency = 88.0f;
            maxFrequency = 108.0f;
        }

        if (targetFrequency < minFrequency || targetFrequency > maxFrequency)
        {
            Debug.LogWarning($"[RadioController] AVISO: targetFrequency ({targetFrequency}) está fora do range {minFrequency}-{maxFrequency} MHz!");
            Debug.LogWarning($"[RadioController] Ajustando targetFrequency para estar dentro do range permitido.");
            targetFrequency = Mathf.Clamp(targetFrequency, minFrequency, maxFrequency);
        }
    }

    #endregion

    private void ExitInteraction()
    {
        isInteracting = false;
        PlayerMovement.canMove = true;
        playerInteractor?.SetInspectionMode(false);

        if (radioCamera != null) 
        {
            radioCamera.Priority.Value = -1; // Inativo - Priority -1
            Debug.Log($"[RadioController] Câmera desativada - Priority: {radioCamera.Priority.Value}");
        }
        
        if (playerCamera != null) 
        {
            playerCamera.Priority.Value = 1; // Player camera volta à prioridade padrão
        }

        // NOVA LÓGICA: Não desliga rádio se Track 3 estiver tocando
        if (!isPuzzleMode && currentState != RadioState.Track3Playing)
        {
            TurnOffRadio();
        }
        else if (currentState == RadioState.Track3Playing)
        {
            Debug.Log("RadioController: Saindo da interação mas mantendo Track 3 tocando");
        }

        inputActions.Player.SwitchDial.Disable();
        inputActions.Player.Tune.Disable();
        inputActions.Player.Interact.Disable();

        UpdateDialHighlight();

        if (frequencyDisplayText != null && !isSolved)
            frequencyDisplayText.enabled = false;
    }

    private void TurnOffRadio()
    {
        if (currentState == RadioState.Off) return;
        
        // Se Track 1 foi reproduzida (mesmo que interrompida), marca como encerrada
        if (track1HasBeenPlayed)
        {
            track1HasEnded = true;
        }
        
        // Verifica se era Track 1 tocando - ativa objeto e evento da porta apenas neste caso
        bool wasTrack1Playing = currentState == RadioState.Track1Playing;
        
        // 🎬 DEMO ONLY: Verifica se era Track 3 tocando - faz fade e reseta cena
        // TODO: REMOVER esta funcionalidade na versão final do jogo
        bool wasTrack3Playing = currentState == RadioState.Track3Playing;
        
        // DESLIGA PERMANENTEMENTE
        currentState = RadioState.Off;
        canInteract = false; // Desabilita interação permanentemente após desligar
        isPuzzleMode = false; // Desativa modo puzzle
        
        // Para todas as corrotinas para evitar interferência
        StopAllCoroutines();
        
        // Para as legendas se o manager estiver configurado
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
        }
        
        // Para todos os áudios
        audioTrigger.Stop();
        
        // Para a instância FMOD atual se existir
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentEventInstance.release();
        }
        
        // Se era Track 1 tocando, ativa objetos e dispara evento da porta com delay
        if (wasTrack1Playing)
        {
            // Ativa todos os GameObjects configurados na lista imediatamente
            if (objectsToEnable != null && objectsToEnable.Length > 0)
            {
                for (int i = 0; i < objectsToEnable.Length; i++)
                {
                    if (objectsToEnable[i] != null)
                    {
                        objectsToEnable[i].SetActive(true);
                    }
                }
            }
            
            // Inicia corrotina para disparar evento da porta com delay de 1 segundo
            StartCoroutine(TriggerDoorKnockAfterDelay());
            
            // CORREÇÃO: Dispara evento para fim do período seguro de sanidade quando Track 1 é interrompida (imediato)
            GameEvents.TriggerRadioTrack1Completed();
        }
        
        // 🎬 DEMO ONLY: Se era Track 3 tocando, inicia fade e reset da cena
        // TODO: REMOVER todo este bloco na versão final do jogo
        if (wasTrack3Playing)
        {
            StartCoroutine(FadeOutAndResetScene());
            return; // Não executa o log final pois a cena será resetada
        }
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
        currentFrequency = Mathf.Clamp(currentFrequency, minFrequency, maxFrequency);

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
            // Aplica clamp final para garantir que não saia do range permitido
            currentFrequency = Mathf.Clamp(currentFrequency, minFrequency, maxFrequency);
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

    private void CheckForSolution()
    {
        if (isSolved) return;

        if (IsFrequencyCorrect())
        {
            isSolved = true;
            // puzzleSolved = true;

            // Desabilita controles do puzzle imediatamente
            inputActions.Player.SwitchDial.Disable();
            inputActions.Player.Tune.Disable();

            // Inicia a Track 3 após resolver o puzzle
            StartTrack3();

            // SAI AUTOMATICAMENTE após sintonização com delay reduzido
            float exitDelay = 1.5f; // Reduzido de 3s para 1.5s
            Invoke(nameof(ExitInteraction), exitDelay);
        }
    }

    /// <summary>
    /// Marca todos os RadioPaperTrigger e ItemInteract da cena como utilizados com sucesso
    /// </summary>
    private void MarkPaperTriggersAsUsed()
    {
        // Marca RadioPaperTriggers
        RadioPaperTrigger[] paperTriggers = FindObjectsByType<RadioPaperTrigger>(FindObjectsSortMode.None);
        foreach (RadioPaperTrigger trigger in paperTriggers)
        {
            trigger.MarkAsSuccessfullyUsed();
        }
        
        // Marca ItemInteracts com PaperTrigger
        ItemInteract[] itemInteracts = FindObjectsByType<ItemInteract>(FindObjectsSortMode.None);
        int paperInteractCount = 0;
        foreach (ItemInteract item in itemInteracts)
        {
            // Só marca os que são PaperTrigger
            if (item.IsPaperTrigger())
            {
                item.MarkRadioTriggerAsUsed();
                paperInteractCount++;
            }
        }
    }
    
    /// <summary>
    /// Corrotina que dispara o evento da porta bater com delay de 1 segundo
    /// </summary>
    private IEnumerator TriggerDoorKnockAfterDelay()
    {
        // Aguarda 1 segundo
        yield return new WaitForSeconds(1f);
        
        // Dispara evento para batida na porta
        GameEvents.TriggerDoorKnock();
    }
    
    /// <summary>
    /// Corrotina que dispara o evento da Track 2 completada com delay de 1 segundo
    /// </summary>
    private IEnumerator TriggerRadioTrack2CompletedAfterDelay()
    {
        // Aguarda 1 segundo
        yield return new WaitForSeconds(1f);
        
        // Dispara evento para Track 2 completada (ativa sanidade)
        GameEvents.TriggerRadioTrack2Completed();
    }
    
    #region 🎬 DEMO ONLY - Fade & Reset System - TODO: REMOVER NA VERSÃO FINAL
    
    /// <summary>
    /// DEMO ONLY: Monitora a Track 3 e executa fade automático após 33 segundos
    /// TODO: REMOVER este método inteiro na versão final do jogo
    /// </summary>
    private IEnumerator MonitorTrack3FadeTimer()
    {
        // Aguarda exatos 33 segundos
        yield return new WaitForSeconds(minPlayTimeBeforeShutdown);
        
        // Verifica se ainda está tocando Track 3 (pode ter sido desligada manualmente)
        if (currentState == RadioState.Track3Playing && currentEventInstance.isValid())
        {
            // Para todas as corrotinas para evitar conflitos
            StopAllCoroutines();
            
            // Executa o fade e reset
            StartCoroutine(FadeOutAndResetScene());
        }
    }
    
    /// <summary>
    /// DEMO ONLY: Corrotina que faz fade para preto e reseta a cena atual
    /// TODO: REMOVER este método inteiro na versão final do jogo
    /// </summary>
    private IEnumerator FadeOutAndResetScene()
    {
        // Configurar canvas de fade se não estiver configurado
        if (fadeCanvas == null || fadeImage == null)
        {
            yield return new WaitForSeconds(1f); // Pequena pausa antes do reset
            ResetCurrentScene();
            yield break;
        }
        
        // Garantir que o canvas está ativo
        fadeCanvas.gameObject.SetActive(true);
        
        // Começar com transparente
        Color startColor = new Color(0f, 0f, 0f, 0f);
        Color targetColor = new Color(0f, 0f, 0f, 1f); // Preto opaco
        fadeImage.color = startColor;
        
        // Fazer fade to black
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / fadeDuration);
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        // Garantir que terminou completamente preto
        fadeImage.color = targetColor;
        
        // Pequena pausa antes do reset
        yield return new WaitForSeconds(0.5f);
        
        // Resetar a cena
        ResetCurrentScene();
    }
    
    /// <summary>
    /// DEMO ONLY: Reseta a cena atual
    /// TODO: REMOVER este método inteiro na versão final do jogo
    /// </summary>
    private void ResetCurrentScene()
    {
        try
        {
            // 1. Dispara evento para todos os sistemas se prepararem para o reset
            GameEvents.TriggerSceneReset();
            
            // 2. Para e libera a instância atual antes do reset
            if (currentEventInstance.isValid())
            {
                currentEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                currentEventInstance.release();
            }
            
            // 3. Para todos os eventos do master bus para garantir total limpeza
            if (RuntimeManager.IsInitialized)
            {
                FMOD.RESULT result = RuntimeManager.StudioSystem.getBus("bus:/", out FMOD.Studio.Bus masterBus);
                if (result == FMOD.RESULT.OK)
                {
                    masterBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
                }
                
                // Force um update para aplicar as mudanças
                RuntimeManager.StudioSystem.update();
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            
            SceneManager.LoadScene(currentSceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RadioController: ❌ Erro ao resetar cena: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Scene Reset Management
    
    /// <summary>
    /// Callback chamado quando o sistema dispara reset de cena - limpeza imediata
    /// </summary>
    private void OnSceneResetTriggered()
    {
        // Para e libera qualquer instância FMOD ativa IMEDIATAMENTE
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentEventInstance.release();
        }
        
        // Para todas as corrotinas que possam estar rodando
        StopAllCoroutines();
        
        // Para sistema de legendas
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
        }
        
        // Para áudio trigger
        if (audioTrigger != null)
        {
            audioTrigger.Stop();
        }
    }
    
    /// <summary>
    /// Callback chamado quando uma cena é carregada - garante reset adicional do rádio
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Só executa para carregamento single (reset de cena)
        if (mode == LoadSceneMode.Single)
        {
            ResetRadioStateOnSceneLoad();
        }
    }
    
    /// <summary>
    /// Reseta completamente o estado do rádio quando a cena é carregada
    /// Garante que o rádio inicia sempre no estado OFF, sem áudios tocando
    /// </summary>
    private void ResetRadioStateOnSceneLoad()
    {
        // 1. Para qualquer instância FMOD que possa estar tocando
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentEventInstance.release();
        }
        
        // 2. Reseta todos os estados do sistema
        currentState = RadioState.Off;
        canInteract = true; // Permite interação inicial
        isPuzzleMode = false;
        isInteracting = false;
        isSolved = false;
        
        // 3. Reseta flags de progresso das tracks
        track1HasBeenPlayed = false;
        track1HasEnded = false;
        
        // 4. Reseta controles de tempo
        trackStartTime = 0f;
        
        // 5. Para qualquer áudio trigger que possa estar ativo
        if (audioTrigger != null)
        {
            audioTrigger.Stop();
        }
        
        // 6. Para qualquer corrotina que possa estar rodando
        StopAllCoroutines();
        
        // 7. Reseta sistema de legendas se configurado
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
        }
    }
    
    #endregion
}