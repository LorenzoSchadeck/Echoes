using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;

/// <summary>
/// Gerenciador da mecânica de choir que inicia após Track 3 do rádio terminar.
/// Reproduz sons em objetos espalhados, onde um deles guia o jogador até uma porta jammed.
/// A porta abre/fecha com som alto e dispara um flashback.
/// Sistema repetível até que o item no flashback seja usado.
/// 
/// RESPONSABILIDADES:
/// - Gerenciar áudio do choir (sons, timing, coordenação)
/// - Controlar porta do choir (abertura/fechamento)
/// - Detectar conclusão via ChoirFlashbackItem
/// 
/// NOTA: O controle de objetos durante flashback é responsabilidade do ChoirFlashbackController
/// </summary>
public class ChoirManager : MonoBehaviour
{
    public static ChoirManager Instance { get; private set; }

    [Header("🎵 Configuração do Choir")]
    [Tooltip("Lista de todos os objetos que podem emitir sons durante o choir")]
    [SerializeField] private List<ChoirAudioSource> audioSources;
    
    // REMOVIDO: Sons não são mais configurados no manager
    // Cada ChoirAudioSource tem seu próprio som configurado

    [Header("🚪 Porta do Choir")]
    [Tooltip("A porta jammed (DoorController) que será ativada quando o jogador encontrar o som correto")]
    [SerializeField] private DoorController choirDoorController;

    [Header("⏱️ Configurações de Timing")]
    [Tooltip("Delay antes de iniciar os sons após Track 3 terminar")]
    [SerializeField] private float startDelay = 2f;
    
    [Tooltip("Intervalo entre cada fonte de áudio no início")]
    [SerializeField] private float intervalBetweenSources = 0.5f;

    // Estado do sistema
    private bool isChoirActive = false;
    private bool isChoirComplete = false; // Marca se o choir foi completado permanentemente
    private ChoirAudioSource guidingSource;
    private Coroutine choirRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Escuta quando Track 3 do rádio termina
        GameEvents.OnRadioTrack3Completed += OnRadioTrack3Completed;
    }

    private void OnDisable()
    {
        GameEvents.OnRadioTrack3Completed -= OnRadioTrack3Completed;
    }

    private void Start()
    {
        // Validação inicial
        ValidateConfiguration();
        
        // Registra todas as fontes de áudio
        RegisterAudioSources();
    }

    /// <summary>
    /// Valida se a configuração está correta
    /// </summary>
    private void ValidateConfiguration()
    {
        if (audioSources == null || audioSources.Count == 0)
        {
            Debug.LogError("[ChoirManager] Nenhuma fonte de áudio configurada!", this);
            enabled = false;
            return;
        }

        // Verifica se há pelo menos uma fonte marcada como guia
        var guidingSources = audioSources.Where(source => source != null && source.IsGuidingSource && source.HasSoundConfigured).ToList();
        if (guidingSources.Count == 0)
        {
            Debug.LogError("[ChoirManager] Nenhuma fonte de guia configurada! Marque pelo menos um ChoirAudioSource como 'isGuidingSource' e configure seu som.", this);
            enabled = false;
            return;
        }

        // Verifica se há fontes de distração
        var distractionSources = audioSources.Where(source => source != null && !source.IsGuidingSource && source.HasSoundConfigured).ToList();
        if (distractionSources.Count == 0)
        {
            Debug.LogWarning("[ChoirManager] Nenhuma fonte de distração configurada! Recomenda-se ter pelo menos 2 fontes de distração.", this);
        }

        if (choirDoorController == null)
        {
            Debug.LogError("[ChoirManager] DoorController do choir não configurado!", this);
            enabled = false;
            return;
        }

        // Validação específica para choir door
        if (!choirDoorController.IsChoirDoor)
        {
            Debug.LogError("[ChoirManager] DoorController configurado não está marcado como choir door!", this);
            enabled = false;
            return;
        }

        Debug.Log($"[ChoirManager] Configuração validada: {audioSources.Count} fontes ({guidingSources.Count} guias, {distractionSources.Count} distrações)");
    }

    /// <summary>
    /// Registra todas as fontes de áudio com este manager
    /// </summary>
    private void RegisterAudioSources()
    {
        foreach (var audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.RegisterWithManager(this);
            }
        }
    }

    /// <summary>
    /// Chamado quando Track 3 do rádio termina
    /// </summary>
    private void OnRadioTrack3Completed()
    {
        Debug.Log("[ChoirManager] 🎵 EVENTO RECEBIDO: Track 3 do rádio completada!");
        Debug.Log($"[ChoirManager] Estado atual - Ativo: {isChoirActive}, Completo: {isChoirComplete}");
        Debug.Log($"[ChoirManager] Configuração - AudioSources: {audioSources?.Count ?? 0}, Porta: {(choirDoorController != null ? "OK" : "NULA")}");
        
        // Só inicia se não foi completado permanentemente
        if (!isChoirComplete)
        {
            Debug.Log($"[ChoirManager] ✅ Track 3 terminou - Iniciando choir em {startDelay} segundos");
            Invoke(nameof(StartChoir), startDelay);
        }
        else
        {
            Debug.Log("[ChoirManager] ❌ Choir já foi completado - Sistema desabilitado");
        }
    }

    /// <summary>
    /// Inicia a mecânica do choir
    /// </summary>
    private void StartChoir()
    {
        Debug.Log("[ChoirManager] 🎪 StartChoir CHAMADO!");
        Debug.Log($"[ChoirManager] Verificações - Ativo: {isChoirActive}, Completo: {isChoirComplete}");
        
        if (isChoirActive || isChoirComplete) 
        {
            Debug.LogWarning($"[ChoirManager] ❌ StartChoir cancelado - Ativo: {isChoirActive}, Completo: {isChoirComplete}");
            return;
        }

        isChoirActive = true;
        Debug.Log("[ChoirManager] ✅ Estado alterado para ATIVO");
        
        // Seleciona aleatoriamente qual fonte tocará o som de guia
        SelectGuidingSource();
        
        if (guidingSource == null)
        {
            Debug.LogError("[ChoirManager] ❌ ERRO CRÍTICO: Nenhuma fonte guia selecionada! Cancelando inicio do choir.");
            isChoirActive = false;
            return;
        }
        
        Debug.Log($"[ChoirManager] 🎵 Choir iniciado - Fonte guia: {guidingSource.name}");
        Debug.Log($"[ChoirManager] 🚪 Porta do choir ativada - jogador deve encontrá-la pelo som");
        
        // Ativa a porta do choir imediatamente quando o choir inicia
        if (choirDoorController != null)
        {
            Debug.Log("[ChoirManager] 🚪 Ativando porta do choir...");
            choirDoorController.ActivateChoirDoor();
        }
        else
        {
            Debug.LogWarning("[ChoirManager] ⚠️ Porta do choir não configurada!");
        }
        
        // Inicia a rotina para começar os sons
        Debug.Log("[ChoirManager] 🎵 Iniciando rotina de sons...");
        choirRoutine = StartCoroutine(ChoirStartRoutine());
    }

    /// <summary>
    /// Seleciona aleatoriamente a fonte que tocará o som de guia
    /// </summary>
    private void SelectGuidingSource()
    {
        // Filtra apenas fontes marcadas como guia que têm som configurado
        var availableGuidingSources = audioSources.Where(source => 
            source != null && 
            source.gameObject.activeInHierarchy && 
            source.IsGuidingSource && 
            source.HasSoundConfigured).ToList();
        
        if (availableGuidingSources.Count > 0)
        {
            guidingSource = availableGuidingSources[Random.Range(0, availableGuidingSources.Count)];
        }
        else
        {
            Debug.LogError("[ChoirManager] Nenhuma fonte de guia disponível! Verifique se há ChoirAudioSources marcados como 'isGuidingSource' com sons configurados.");
        }
    }

    /// <summary>
    /// Rotina principal para iniciar os sons do choir uma única vez
    /// </summary>
    private IEnumerator ChoirStartRoutine()
    {
        Debug.Log("[ChoirManager] 🎵 Iniciando sons do choir - tocarão continuamente até o puzzle ser resolvido");
        
        // Aguarda o delay inicial
        yield return new WaitForSeconds(startDelay);
        
        // Inicia os sons uma única vez - eles ficam em loop
        PlaySoundCycle();
        
        Debug.Log("[ChoirManager] ✅ Sons do choir iniciados e tocando continuamente");
    }

    /// <summary>
    /// Executa o ciclo completo de reprodução dos sons do coro
    /// </summary>
    private void PlaySoundCycle()
    {
        Debug.Log("[ChoirManager] Iniciando ciclo de reprodução dos sons");
        
        // 1. Reproduz o som guia primeiro
        if (guidingSource != null && guidingSource.HasSoundConfigured)
        {
            guidingSource.StartChoirSound();
        }
        else
        {
            Debug.LogError("[ChoirManager] Fonte guia não disponível ou sem som configurado!");
            return;
        }
        
        // 2. Reproduz os sons de distração após o intervalo configurado
        StartCoroutine(PlayDistractionSoundsAfterDelay());
    }
    
    /// <summary>
    /// Reproduz os sons de distração após um intervalo
    /// </summary>
    private IEnumerator PlayDistractionSoundsAfterDelay()
    {
        yield return new WaitForSeconds(intervalBetweenSources);
        
        var distractionSources = audioSources.Where(source => 
            source != null && 
            source != guidingSource && 
            source.gameObject.activeInHierarchy &&
            source.HasSoundConfigured).ToList();
        
        foreach (var source in distractionSources)
        {
            source.StartChoirSound();
            yield return new WaitForSeconds(intervalBetweenSources);
        }
        
        Debug.Log($"[ChoirManager] Ciclo completo - {distractionSources.Count} sons de distração reproduzidos");
    }

    /// <summary>
    /// Para todos os sons ativos
    /// </summary>
    private void StopAllSounds()
    {
        foreach (var audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.StopSound();
            }
        }
    }

    /// <summary>
    /// Para o choir (pode ser reiniciado)
    /// </summary>
    private void StopChoir()
    {
        if (!isChoirActive) return;

        isChoirActive = false;
        
        if (choirRoutine != null)
        {
            StopCoroutine(choirRoutine);
            choirRoutine = null;
        }

        StopAllSounds();
        Debug.Log("[ChoirManager] 🛑 Choir parado");
    }

    /// <summary>
    /// Marca o choir como completado permanentemente
    /// </summary>
    private void CompleteChoir()
    {
        isChoirComplete = true;
        StopChoir();
        Debug.Log("[ChoirManager] ✅ Choir completado permanentemente");
    }

    /// <summary>
    /// Chamado quando o item no flashback é usado - completa o choir permanentemente
    /// </summary>
    public void OnFlashbackItemUsed()
    {
        Debug.Log("[ChoirManager] 🏁 Item do flashback usado - Choir completado permanentemente");
        CompleteChoir();
    }

    /// <summary>
    /// MÉTODO DE TESTE: Força o início do choir (apenas para debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceStartChoir()
    {
        if (!isChoirComplete)
        {
            Debug.Log("[ChoirManager] TESTE: Forçando início do choir");
            StartChoir();
        }
        else
        {
            Debug.Log("[ChoirManager] TESTE: Choir já foi completado");
        }
    }
    
    /// <summary>
    /// Propriedades públicas para verificação de estado
    /// </summary>
    public bool IsChoirActive => isChoirActive;
    public bool IsChoirComplete => isChoirComplete;
    public ChoirAudioSource GuidingSource => guidingSource;
}