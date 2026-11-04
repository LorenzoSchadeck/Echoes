using FMODUnity;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class HorrorEventManager : MonoBehaviour
{
    public static HorrorEventManager Instance { get; private set; }

    [Header("Event Library")]
    [Tooltip("A lista de todos os possíveis eventos de horror que podem acontecer.")]
    [SerializeField] private List<HorrorEvent> eventLibrary;

    [Header("Trigger Logic")]
    [Tooltip("Intervalo em segundos entre cada rolagem de evento.")]
    [SerializeField] private float checkInterval = 20.0f;

    [Header("Diretor de eventos")]
    [Tooltip("Chance de evento por rolagem com a sanidade cheia.")]
    [SerializeField, Range(0f, 1f)] private float chanceAtFullSanity = 0.3f;
    [Tooltip("Chance de evento por rolagem com a sanidade em zero.")]
    [SerializeField, Range(0f, 1f)] private float chanceAtZeroSanity = 0.8f;
    [Tooltip("Abaixo desta sanidade, eventos de Angústia (nível 2) são liberados.")]
    [SerializeField, Range(0f, 1f)] private float tier2Sanity = 0.7f;
    [Tooltip("Abaixo desta sanidade, eventos de Colapso (nível 3) são liberados.")]
    [SerializeField, Range(0f, 1f)] private float tier3Sanity = 0.4f;
    [Tooltip("Tensão acumulada (soma dos níveis dos eventos recentes) que inicia um período de alívio sem eventos.")]
    [SerializeField] private float maxTension = 4f;
    [Tooltip("Quanto a tensão cai por segundo.")]
    [SerializeField] private float tensionDecayPerSecond = 0.03f;
    
    [Header("Anti-Repetition System")]
    [Tooltip("Quantos eventos diferentes devem tocar antes de poder repetir o mesmo.")]
    [SerializeField] private int antiRepetitionBuffer = 3;
    
    private bool isInFlashback = false;
    private Queue<HorrorEventType> recentEvents = new Queue<HorrorEventType>();
    private float sanity = 1f;
    private float tension;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnFlashbackStarted += OnFlashbackStart;
        GameEvents.OnFlashbackEnded += OnFlashbackEnd;
        GameEvents.OnSceneReset += HandleSceneReset;
        InsanityManager.OnSanityChanged += HandleSanityChanged;
    }

    private void HandleSanityChanged(float value) => sanity = value;

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= OnFlashbackStart;
        GameEvents.OnFlashbackEnded -= OnFlashbackEnd;
        GameEvents.OnSceneReset -= HandleSceneReset;
        InsanityManager.OnSanityChanged -= HandleSanityChanged;
    }

    private void Start()
    {
        StartCoroutine(HorrorCheckRoutine());
    }

    private void OnFlashbackStart()
    {
        isInFlashback = true;
        Debug.Log("[HorrorEventManager] Flashback iniciado - Eventos de horror BLOQUEADOS");
    }

    private void OnFlashbackEnd()
    {
        isInFlashback = false;
        Debug.Log("[HorrorEventManager] Flashback finalizado - Eventos de horror LIBERADOS");
    }
    
    /// <summary>
    /// Verifica se eventos de rádio podem ser executados (rádio deve estar OFF).
    /// </summary>
    private bool CanExecuteRadioEvent(string eventName)
    {
        if (RadioController.Instance != null && !RadioController.Instance.IsRadioOff)
        {
            Debug.Log($"[HorrorEventManager] ❌ Evento {eventName} cancelado - rádio não está desligado (estado necessário: Off)");
            return false;
        }
        return true;
    } 

    private IEnumerator HorrorCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            tension = Mathf.Max(0f, tension - tensionDecayPerSecond * checkInterval);
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        // 1. BLOQUEIO: Flashback
        if (isInFlashback) 
        {
            Debug.Log("[HorrorEventManager] Tentativa de evento bloqueada - Player está em flashback");
            return;
        }

        // 2. BLOQUEIO: ainda no prólogo (Track 1 do rádio não foi encerrada)
        if (GameFlow.Current < GameAct.Investigation)
        {
            Debug.Log("[HorrorEventManager] Tentativa de evento bloqueada - ainda no prólogo");
            return;
        }

        // 3. ALÍVIO: tensão alta demais, o jogador ganha um respiro antes do próximo pico
        if (tension >= maxTension)
        {
            Debug.Log($"[HorrorEventManager] Período de alívio (tensão {tension:F1}/{maxTension})");
            return;
        }

        // Quanto menor a sanidade, maior a chance de evento
        float chance = Mathf.Lerp(chanceAtZeroSanity, chanceAtFullSanity, sanity);
        bool eventTriggered = Random.value < chance;
        Debug.Log($"[HorrorEventManager] Rolagem: {(eventTriggered ? "SIM" : "NÃO")} (chance {chance:P0}, sanidade {sanity:F2}, tensão {tension:F1}, nível máx. {MaxAllowedTier()})");

        if (eventTriggered)
        {
            TriggerRandomEvent();
        }
    }

    /// <summary>
    /// Nível de evento mais intenso permitido agora: limitado pela sanidade e pelo ato.
    /// </summary>
    private int MaxAllowedTier()
    {
        int bySanity = sanity < tier3Sanity ? 3 : sanity < tier2Sanity ? 2 : 1;
        int byAct = GameFlow.Current switch
        {
            GameAct.Investigation => 2, // Ato 1: no máximo Angústia
            GameAct.Ending => 0,
            _ => 3,
        };
        return Mathf.Min(bySanity, byAct);
    }
    
    private void TriggerRandomEvent()
    {
        // Verificação de segurança adicional
        if (isInFlashback) 
        {
            Debug.LogWarning("[HorrorEventManager] AVISO: TriggerRandomEvent chamado durante flashback!");
            return;
        }

        // 1. Só eventos do nível permitido agora, fora do buffer de repetição recente
        int maxTier = MaxAllowedTier();
        List<HorrorEvent> allowedEvents = eventLibrary
            .Where(evt => HorrorEventTiers.TierOf(evt.type) <= maxTier)
            .ToList();
        List<HorrorEvent> availableEvents = allowedEvents
            .Where(evt => !recentEvents.Contains(evt.type))
            .ToList();

        // 2. Se todos os permitidos foram usados recentemente, libera a repetição
        if (availableEvents.Count == 0)
        {
            Debug.Log("[HorrorEventManager] Todos os eventos permitidos foram usados recentemente, resetando buffer");
            recentEvents.Clear();
            availableEvents = allowedEvents;
        }

        if (availableEvents.Count == 0) 
        {
            Debug.LogWarning($"[HorrorEventManager] Nenhum evento de nível até {maxTier} na biblioteca!");
            return;
        }

        // 3. Sorteia um evento da lista disponível
        HorrorEvent selectedEvent = availableEvents[Random.Range(0, availableEvents.Count)];

        // 4. Adiciona ao buffer de repetição
        recentEvents.Enqueue(selectedEvent.type);
        if (recentEvents.Count > antiRepetitionBuffer)
        {
            recentEvents.Dequeue(); // Remove o mais antigo
        }

        Debug.Log($"[HorrorEventManager] Evento selecionado: {selectedEvent.eventName} (Tipo: {selectedEvent.type})");
        Debug.Log($"[HorrorEventManager] Buffer atual: [{string.Join(", ", recentEvents)}]");

        // 5. Executa o evento selecionado e soma a tensão do seu nível
        tension += HorrorEventTiers.TierOf(selectedEvent.type);
        ExecuteEvent(selectedEvent);
    }

    /// <summary>
    /// Propriedade para verificar se o player está em flashback
    /// </summary>
    public bool IsInFlashback => isInFlashback;

    /// <summary>
    /// Método público para forçar parada de eventos durante situações especiais
    /// </summary>
    public void SetEventsBlocked(bool blocked)
    {
        isInFlashback = blocked;
        Debug.Log($"[HorrorEventManager] Eventos de horror {(blocked ? "BLOQUEADOS" : "LIBERADOS")} manualmente");
    }

    /// <summary>
    /// Verifica se é seguro disparar eventos (sempre true agora que eventos não afetam sanidade)
    /// </summary>
    public bool IsSafeToTriggerEvents()
    {
        // Eventos agora são puramente atmosféricos, sempre é seguro dispará-los
        // A única restrição é não estar em flashback
        return !isInFlashback;
    }

    /// <summary>
    /// Força uma rolagem imediata para testes (ignora o timer de 20s)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceEventRoll()
    {
        Debug.Log("[HorrorEventManager] TESTE: Forçando rolagem de evento...");
        TryTriggerEvent();
    }

    /// <summary>
    /// Força disparo de um evento específico para testes
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceSpecificEvent(HorrorEventType eventType)
    {
        if (isInFlashback)
        {
            Debug.LogWarning("[HorrorEventManager] TESTE: Não é possível forçar evento durante flashback");
            return;
        }

        HorrorEvent? eventToTrigger = eventLibrary.FirstOrDefault(evt => evt.type == eventType);
        if (eventToTrigger.HasValue)
        {
            Debug.Log($"[HorrorEventManager] TESTE: Forçando evento {eventType}");
            
            // Adiciona ao buffer mesmo sendo forçado
            recentEvents.Enqueue(eventType);
            if (recentEvents.Count > antiRepetitionBuffer)
            {
                recentEvents.Dequeue();
            }
            
            ExecuteEvent(eventToTrigger.Value);
        }
        else
        {
            Debug.LogWarning($"[HorrorEventManager] TESTE: Evento {eventType} não encontrado na biblioteca");
        }
    }
    
    /// <summary>
    /// Chamado quando a cena é resetada - limpa o buffer de eventos recentes
    /// </summary>
    private void HandleSceneReset()
    {
        Debug.Log("[HorrorEventManager] 🔄 Limpando buffer de eventos recentes");
        
        // Limpa o buffer de eventos recentes e a tensão acumulada
        recentEvents.Clear();
        tension = 0f;
        
        // Reseta flag de flashback
        isInFlashback = false;
        
        Debug.Log("[HorrorEventManager] ✅ Sistema de eventos de horror resetado");
    }

    /// <summary>
    /// Executa um evento específico
    /// </summary>
    private void ExecuteEvent(HorrorEvent selectedEvent)
    {
        // 5. Dispara o evento correspondente
        switch (selectedEvent.type)
        {
            // Limiar 2: Ansiedade
            case HorrorEventType.PlaySpatialSound:
                GameEvents.TriggerSpatialSound(selectedEvent.spatialSoundEvent, selectedEvent.spatialSoundTarget);
                break;
            case HorrorEventType.RadioStaticBurst:
                // Verifica se pode executar evento de rádio
                if (!CanExecuteRadioEvent(selectedEvent.eventName))
                    return; // Cancela o evento
                    
                GameEvents.TriggerRadioStatic(selectedEvent.staticBurstEvent, selectedEvent.radioTarget, selectedEvent.staticBurstDuration);
                break;
            case HorrorEventType.LightFlicker:
                GameEvents.TriggerSimpleLightFlicker(selectedEvent.flickerLights, selectedEvent.flickerSoundEvent, selectedEvent.flickerSoundTarget);
                break;

            // Limiar 3: Angústia
            case HorrorEventType.VisualFlash:
                GameEvents.TriggerVisualFlash(1.0f, selectedEvent.visualFlashDuration); // Pico máximo de insanidade
                break;
            case HorrorEventType.FalseAlarmClock:
                GameEvents.TriggerFalseAlarm(selectedEvent.falseAlarmDuration);
                break;

            // Limiar 4: Colapso
            case HorrorEventType.SpawnCoveredBody:
                GameEvents.TriggerSpawnCoveredBody(selectedEvent.coveredBodyPrefab, selectedEvent.coveredBodySpawnPoint);
                break;
            case HorrorEventType.GuiltChorusBurst:
                GameEvents.TriggerDualGuiltChorus(selectedEvent.guiltChorusEvent1, selectedEvent.guiltChorusEvent2, 
                                                 selectedEvent.guiltChorusTarget1, selectedEvent.guiltChorusTarget2);
                break;
        }
    }
}