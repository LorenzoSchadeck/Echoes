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
            if (showDebugLogs) Debug.Log("CanShowPrompt: FALSE - canInteract = false");
            return false;
        }
        
        // SEGUNDA VERIFICAÇÃO: Sem prompt enquanto o rádio estiver desligado
        if (currentState == RadioState.Off) 
        {
            if (showDebugLogs) Debug.Log("CanShowPrompt: FALSE - currentState = Off");
            return false;
        }
        
        // TERCEIRA VERIFICAÇÃO: Track 2 - nunca mostra prompt (não pode ser desligada)
        if (currentState == RadioState.Track2Playing)
        {
            if (showDebugLogs) Debug.Log("CanShowPrompt: FALSE - Track2Playing não pode ser desligada");
            return false;
        }
        
        // QUARTA VERIFICAÇÃO: Modo puzzle - sempre mostra prompt de sintonizar
        if (currentState == RadioState.PuzzleMode)
        {
            if (showDebugLogs) Debug.Log("CanShowPrompt: TRUE - PuzzleMode ativo");
            return true;
        }
        
        // QUINTA VERIFICAÇÃO: Durante reprodução de tracks, verifica tempo mínimo
        if (IsPlayingTrack() && !HasMinimumPlayTimePassed())
        {
            if (showDebugLogs) Debug.Log($"CanShowPrompt: FALSE - Track tocando mas tempo mínimo não passou ({Time.time - trackStartTime:F1}s/{minPlayTimeBeforeShutdown}s)");
            return false; // Não mostra prompt se ainda não passou tempo mínimo
        }
        
        // SEXTA VERIFICAÇÃO: Track 1 Static e Track 3 após tempo mínimo
        if (currentState == RadioState.Track1Static || 
            (currentState == RadioState.Track3Playing && HasMinimumPlayTimePassed()))
        {
            if (showDebugLogs) Debug.Log($"CanShowPrompt: TRUE - Estado {currentState} permite interação");
            return true;
        }
        
        // SÉTIMA VERIFICAÇÃO: Track 1 após tempo mínimo
        if (currentState == RadioState.Track1Playing && HasMinimumPlayTimePassed())
        {
            if (showDebugLogs) Debug.Log("CanShowPrompt: TRUE - Track1 após tempo mínimo");
            return true;
        }
        
        if (showDebugLogs) Debug.Log($"CanShowPrompt: FALSE - Nenhuma condição atendida (Estado: {currentState})");
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
    
    [Header("🚪 Evento da Porta")]
    [Tooltip("Objeto que será habilitado após a primeira ativação")]
    [SerializeField] private GameObject objectToEnable;
    
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

    private void Update()
    {
        // Cancelamento via botão direito do mouse durante interação com o rádio
        if (isInteracting && isPuzzleMode)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (showDebugLogs) Debug.Log("RadioController: Cancelamento via botão direito do mouse detectado");
                ExitInteraction();
            }
        }
    }

    private void Start()
    {
        currentFrequency = 88.00f;
        UpdateFrequencyDisplay();
        UpdateDialHighlight();
        
        if (showDebugLogs) Debug.Log($"RadioController: Start() chamado - Estado inicial: {currentState}");
        
        // Teste direto do evento para debug
        if (showDebugLogs) 
        {
            Debug.Log("RadioController: Testando se OnRadioPaperTrigger está funcionando...");
            // Não vamos disparar o evento aqui, só confirmar que está inscrito
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
        
        if (showDebugLogs) Debug.Log("RadioController: Eventos inscritos - OnRadioFirstTrigger e OnRadioPaperTrigger");
    }

    private void OnDisable()
    {
        inputActions.Player.SwitchDial.performed -= OnSwitchDial;
        inputActions.Player.Tune.performed -= OnTune;
        inputActions.Player.Interact.performed -= OnExitInteraction;
        
        // Remover eventos do fluxo
        GameEvents.OnRadioFirstTrigger -= OnFirstTrigger;
        GameEvents.OnRadioPaperTrigger -= OnPaperTrigger;
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
                if (showDebugLogs) Debug.Log("RadioController: Track 2 - aguardando 33s para modo puzzle");
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

        currentState = RadioState.Track1Playing;
        canInteract = true; // Permite desligar durante reprodução
        
        // Verifica se é a primeira vez antes de marcar como tocada
        bool isFirstTimeTrack1 = !track1HasBeenPlayed;
        track1HasBeenPlayed = true; // Marca que Track 1 foi tocada

        if (showDebugLogs) Debug.Log($"RadioController: Iniciando Track 1 (primeira vez: {isFirstTimeTrack1}) - Track 1 marcada como tocada");

        // Reproduz a faixa 1 via FMOD usando instância direta
        currentEventInstance = FMODUnity.RuntimeManager.CreateInstance(track1Event);
        
        if (currentEventInstance.isValid())
        {
            // Define posição 3D e range máximo
            currentEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            currentEventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxAudioRange);
            
            FMOD.RESULT result = currentEventInstance.start();
            
            if (showDebugLogs) 
            {
                Debug.Log($"RadioController: Track 1 FMOD start result: {result}");
                Debug.Log($"RadioController: Event instance válida: {currentEventInstance.isValid()}");
                Debug.Log($"RadioController: Audio range definido para: {maxAudioRange}m");
            }
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
            if (showDebugLogs) Debug.Log($"RadioController: CHAMANDO StartTrack1Subtitles (primeira vez: {isFirstTimeTrack1})");
            subtitleManager.StartTrack1Subtitles(isFirstTimeTrack1);
            if (showDebugLogs) Debug.Log($"RadioController: Legendas da Track 1 iniciadas (primeira vez: {isFirstTimeTrack1})");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("RadioController: ERRO - subtitleManager está NULL! Legendas não serão exibidas!");
        }

        // Inicia corrotina para monitorar fim da faixa
        StartCoroutine(MonitorTrackCompletion(OnTrack1Complete));
    }

    /// <summary>
    /// Chamado quando a faixa 1 termina
    /// </summary>
    private void OnTrack1Complete()
    {
        if (showDebugLogs) Debug.Log("RadioController: Track 1 completa, entrando em estática!");
        
        // Marca Track 1 como encerrada (terminou naturalmente)
        track1HasEnded = true;
        
        // CORREÇÃO: Dispara evento para fim do período seguro de sanidade
        GameEvents.TriggerRadioTrack1Completed();
        if (showDebugLogs) Debug.Log("RadioController: Evento OnRadioTrack1Completed disparado - período seguro de sanidade terminado!");
        
        // Entra em estática após Track 1
        currentState = RadioState.Track1Static;
        canInteract = true;

        // Para as legendas da Track 1 quando ela termina naturalmente
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
            if (showDebugLogs) Debug.Log("RadioController: Legendas da Track 1 paradas (track completa)");
        }

        if (showDebugLogs) Debug.Log($"RadioController: Track 1 encerrada naturalmente - papel ainda não pode ser usado (rádio ainda ligado)");

        // Reproduz estática de fundo
        PlayStaticLoop();
    }    /// <summary>
    /// Inicia a segunda faixa após ativação por papel
    /// </summary>
    private void StartTrack2()
    {
        if (showDebugLogs) Debug.Log("RadioController: StartTrack2() chamado - verificando configurações");
        
        if (track2Event.IsNull) 
        {
            Debug.LogError("RadioController: ERRO - Track 2 event não configurado! Verifique no Inspector se track2Event está definido.");
            return;
        }

        if (showDebugLogs) Debug.Log("RadioController: Track 2 event configurado corretamente - iniciando reprodução");

        currentState = RadioState.Track2Playing;
        canInteract = true; // Permite interação (modo puzzle após 33s)

        // Para a estática
        StopStaticLoop();

        // Reproduz a faixa 2 via FMOD usando instância direta
        currentEventInstance = FMODUnity.RuntimeManager.CreateInstance(track2Event);
        
        if (currentEventInstance.isValid())
        {
            // Define posição 3D e range máximo
            currentEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            currentEventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxAudioRange);
            
            FMOD.RESULT result = currentEventInstance.start();
            
            if (showDebugLogs) 
            {
                Debug.Log($"RadioController: Track 2 FMOD start result: {result}");
                Debug.Log($"RadioController: Audio range definido para: {maxAudioRange}m");
            }
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
            if (showDebugLogs) Debug.Log("RadioController: CHAMANDO StartTrack2Subtitles (primeira vez: true)");
            subtitleManager.StartTrack2Subtitles(true);
            if (showDebugLogs) Debug.Log("RadioController: Legendas da Track 2 iniciadas (primeira vez: true)");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("RadioController: ERRO - subtitleManager está NULL! Legendas Track 2 não serão exibidas!");
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
            if (showDebugLogs) Debug.Log("RadioController: 33 segundos passaram - ativando modo puzzle (Track 2 continua tocando)");
            
            // Muda para modo puzzle mas mantém Track 2 tocando
            currentState = RadioState.PuzzleMode;
            isPuzzleMode = true;
            
            // Dispara evento indicando que Track 2 atingiu o tempo necessário (ativa sanidade)
            if (showDebugLogs) Debug.Log("RadioController: DISPARANDO GameEvents.TriggerRadioTrack2Completed() - sanidade deve ativar!");
            GameEvents.TriggerRadioTrack2Completed();
            
            if (showDebugLogs) Debug.Log("RadioController: Modo puzzle ativado - Track 2 continua tocando, jogador pode sintonizar");
        }
    }

    /// <summary>
    /// Chamado quando a faixa 2 termina naturalmente - deve reiniciar em loop
    /// </summary>
    private void OnTrack2Complete()
    {
        if (showDebugLogs) Debug.Log("RadioController: Track 2 terminou naturalmente - reiniciando em loop");
        
        // Se ainda não está em modo puzzle, ativa agora
        if (currentState != RadioState.PuzzleMode)
        {
            currentState = RadioState.PuzzleMode;
            isPuzzleMode = true;
            
            // Dispara sanidade se ainda não foi disparada
            GameEvents.TriggerRadioTrack2Completed();
        }
        
        // REINICIA Track 2 em loop - só para quando puzzle for resolvido
        if (currentState == RadioState.PuzzleMode && !isSolved)
        {
            if (showDebugLogs) Debug.Log("RadioController: Reiniciando Track 2 em loop - puzzle ainda não resolvido");
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
            
            if (showDebugLogs) 
            {
                Debug.Log($"RadioController: Track 2 loop restart - FMOD result: {result}");
            }
            
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

        if (showDebugLogs) Debug.Log("RadioController: PUZZLE RESOLVIDO - parando Track 2 e iniciando Track 3");

        currentState = RadioState.Track3Playing;
        canInteract = true; // Permite desligar durante reprodução
        isPuzzleMode = false; // Sai do modo puzzle

        // Para a estática se estiver tocando
        StopStaticLoop();
        
        // PARA DEFINITIVAMENTE a Track 2 (que estava em loop)
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Para imediatamente
            currentEventInstance.release();
            if (showDebugLogs) Debug.Log("RadioController: Track 2 PARADA DEFINITIVAMENTE - puzzle resolvido");
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
            
            if (showDebugLogs) 
            {
                Debug.Log($"RadioController: Track 3 FMOD start result: {result}");
                Debug.Log($"RadioController: Track 2 substituída por Track 3 - puzzle concluído!");
            }
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
            if (showDebugLogs) Debug.Log("RadioController: CHAMANDO StartTrack3Subtitles (primeira vez: true)");
            subtitleManager.StartTrack3Subtitles(true);
            if (showDebugLogs) Debug.Log("RadioController: Legendas da Track 3 iniciadas (primeira vez: true)");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("RadioController: ERRO - subtitleManager está NULL! Legendas Track 3 não serão exibidas!");
        }

        // Inicia corrotina para monitorar fim da faixa
        StartCoroutine(MonitorTrackCompletion(OnTrack3Complete));
    }

    /// <summary>
    /// Chamado quando a faixa 3 termina - fim da primeira fase
    /// </summary>
    private void OnTrack3Complete()
    {
        if (showDebugLogs) Debug.Log("RadioController: Track 3 completa - FIM DA PRIMEIRA FASE");
        
        // Rádio desliga automaticamente após Track 3 terminar
        currentState = RadioState.Off;
        canInteract = false;
        isPuzzleMode = false;
        
        // Para as legendas se o manager estiver configurado
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
            if (showDebugLogs) Debug.Log("RadioController: Legendas da Track 3 paradas automaticamente");
        }
        
        // Para todos os áudios
        StopStaticLoop();
        audioTrigger.Stop();
        
        // Limpa a instância FMOD
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentEventInstance.release();
            if (showDebugLogs) Debug.Log("RadioController: Track 3 FMOD instance parada e liberada");
        }
        
        if (showDebugLogs) Debug.Log("RadioController: Primeira fase do rádio COMPLETAMENTE concluída - rádio desligado automaticamente!");
    }

    /// <summary>
    /// Corrotina que monitora se o áudio parou de tocar
    /// </summary>
    private IEnumerator MonitorTrackCompletion(System.Action onComplete)
    {
        if (onComplete == null) yield break;

        if (showDebugLogs) Debug.Log("RadioController: Monitorando fim da track via FMOD");
        
        // Aguarda pelo menos 1 segundo antes de começar a verificar
        yield return new WaitForSeconds(1f);
        
        // Monitora o estado do evento FMOD
        while (true)
        {
            if (!currentEventInstance.isValid())
            {
                if (showDebugLogs) Debug.Log("RadioController: Instância FMOD inválida - track completa");
                break;
            }
            
            FMOD.Studio.PLAYBACK_STATE playbackState;
            currentEventInstance.getPlaybackState(out playbackState);
            
            if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
            {
                if (showDebugLogs) Debug.Log("RadioController: FMOD playback parou - track completa");
                break;
            }
            
            yield return new WaitForSeconds(0.1f); // Verifica a cada 100ms
        }

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

        // NOVA LÓGICA: Não desliga rádio se Track 3 estiver tocando
        if (!isPuzzleMode && currentState != RadioState.Track3Playing)
        {
            TurnOffRadio();
        }
        else if (currentState == RadioState.Track3Playing)
        {
            if (showDebugLogs) Debug.Log("RadioController: Saindo da interação mas mantendo Track 3 tocando");
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
        
        if (showDebugLogs) Debug.Log($"RadioController: DESLIGANDO RÁDIO (estado atual: {currentState})");
        
        // Se Track 1 foi reproduzida (mesmo que interrompida), marca como encerrada
        if (track1HasBeenPlayed)
        {
            track1HasEnded = true;
            if (showDebugLogs) Debug.Log("RadioController: Track 1 marcada como encerrada (rádio desligado após Track 1 ter tocado)");
        }
        
        // Verifica se era Track 1 tocando - ativa objeto e evento da porta apenas neste caso
        bool wasTrack1Playing = currentState == RadioState.Track1Playing;
        
        // DESLIGA PERMANENTEMENTE
        currentState = RadioState.Off;
        canInteract = false; // Desabilita interação permanentemente após desligar
        isPuzzleMode = false; // Desativa modo puzzle
        
        if (showDebugLogs) Debug.Log($"RadioController: Estado alterado para OFF, canInteract = {canInteract}");
        
        // Para todas as corrotinas para evitar interferência
        StopAllCoroutines();
        
        // Para as legendas se o manager estiver configurado
        if (subtitleManager != null)
        {
            subtitleManager.StopSubtitles();
            if (showDebugLogs) Debug.Log("RadioController: Legendas paradas");
        }
        
        // Para todos os áudios
        audioTrigger.Stop();
        StopStaticLoop();
        
        // Para a instância FMOD atual se existir
        if (currentEventInstance.isValid())
        {
            currentEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentEventInstance.release();
            if (showDebugLogs) Debug.Log("RadioController: Instância FMOD parada e liberada");
        }
        
        // Se era Track 1 tocando, ativa objeto e dispara evento da porta
        if (wasTrack1Playing)
        {
            // Ativa o GameObject se configurado
            if (objectToEnable != null)
            {
                if (showDebugLogs) Debug.Log($"RadioController: Ativando objeto {objectToEnable.name}");
                objectToEnable.SetActive(true);
            }
            
            // Dispara evento para batida na porta
            if (showDebugLogs) Debug.Log("RadioController: Disparando evento de batida na porta");
            GameEvents.TriggerDoorKnock();
            
            // CORREÇÃO: Dispara evento para fim do período seguro de sanidade quando Track 1 é interrompida
            GameEvents.TriggerRadioTrack1Completed();
            if (showDebugLogs) Debug.Log("RadioController: Evento OnRadioTrack1Completed disparado após interrupção da Track 1 - período seguro de sanidade terminado!");
        }
        
        if (showDebugLogs) Debug.Log("RadioController: RÁDIO COMPLETAMENTE DESLIGADO - sem mais interações possíveis");
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
            // puzzleSolved = true;
            
            if (showDebugLogs) Debug.Log("RadioController: Frequência sintonizada corretamente! Puzzle resolvido!");

            // Desabilita controles do puzzle imediatamente
            inputActions.Player.SwitchDial.Disable();
            inputActions.Player.Tune.Disable();

            // Inicia a Track 3 após resolver o puzzle
            StartTrack3();

            // SAI AUTOMATICAMENTE após sintonização com delay reduzido
            float exitDelay = 1.5f; // Reduzido de 3s para 1.5s
            if (showDebugLogs) Debug.Log("RadioController: Saindo automaticamente do modo sintonização em 1.5s (Track 3 continuará tocando)");
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
        
        if (showDebugLogs)
        {
            Debug.Log($"RadioController: Marcados {paperTriggers.Length} RadioPaperTrigger(s) e {paperInteractCount} ItemInteract(s) PaperTrigger como utilizados com sucesso");
        }
    }
}