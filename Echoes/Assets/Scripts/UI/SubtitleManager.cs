using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;

/// <summary>
/// Sistema centralizado de gerenciamento de legendas para eventos do jogo.
/// Bloqueia interação com SimpleItemDisplay enquanto legendas estão ativas.
/// </summary>
public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }
    
    /// <summary>
    /// Indica se uma legenda está ativa no momento.
    /// Usado pelo SimpleItemDisplay para bloquear interações.
    /// </summary>
    public static bool IsSubtitleActive { get; private set; }

    [Header("📺 Subtitle Display")]
    [Tooltip("TextMeshProUGUI onde as legendas serão exibidas")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("📻 Radio Track Events")]
    [Tooltip("Legenda exibida quando a faixa 1 do rádio termina")]
    [SerializeField] private LocalizedString track1CompletedSubtitle;
    [SerializeField] private float track1SubtitleDuration = 3f;
    
    [Tooltip("Legenda exibida quando a faixa 2 do rádio termina")]
    [SerializeField] private LocalizedString track2CompletedSubtitle;
    [SerializeField] private float track2SubtitleDuration = 3f;
    
    [Tooltip("Legenda exibida quando a faixa 3 do rádio termina")]
    [SerializeField] private LocalizedString track3CompletedSubtitle;
    [SerializeField] private float track3SubtitleDuration = 3f;

    [Header("🔑 Key Collection Events")]
    [Tooltip("Legenda exibida quando uma chave de porta é coletada")]
    [SerializeField] private LocalizedString doorKeyCollectedSubtitle;
    [SerializeField] private float doorKeySubtitleDuration = 2f;
    
    [Tooltip("Legenda exibida quando uma chave de gaveta é coletada")]
    [SerializeField] private LocalizedString drawerKeyCollectedSubtitle;
    [SerializeField] private float drawerKeySubtitleDuration = 2f;

    [Header("🧠 Sanity Events")]
    [Tooltip("Legenda exibida quando a sanidade chega a 0%")]
    [SerializeField] private LocalizedString lowSanitySubtitle;
    [SerializeField] private float lowSanitySubtitleDuration = 4f;

    [Header("⚙️ Settings")]
    [Tooltip("Se verdadeiro, legendas entram em fila. Se falso, nova legenda cancela a anterior")]
    [SerializeField] private bool useQueue = true;
    
    [Tooltip("Se verdadeiro, mostra debug logs no console")]
    [SerializeField] private bool debugMode = false;

    // Controle interno
    private Queue<SubtitleData> subtitleQueue = new Queue<SubtitleData>();
    private Coroutine activeSubtitleCoroutine;
    private bool isShowingSubtitle = false;
    private bool hasTriggeredLowSanity = false;

    /// <summary>
    /// Estrutura de dados para armazenar informações de uma legenda
    /// </summary>
    private struct SubtitleData
    {
        public string text;
        public float duration;

        public SubtitleData(string text, float duration)
        {
            this.text = text;
            this.duration = duration;
        }
    }

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[SubtitleManager] Instância duplicada detectada! Destruindo...");
            Destroy(gameObject);
            return;
        }

        // Valida configuração
        ValidateConfiguration();
        
        // Garante que legendas começam ocultas
        HideSubtitleImmediate();
    }

    private void OnEnable()
    {
        // Inscreve em eventos do rádio
        GameEvents.OnRadioTrack1Completed += OnRadioTrack1Completed;
        GameEvents.OnRadioTrack2Completed += OnRadioTrack2Completed;
        GameEvents.OnRadioTrack3Completed += OnRadioTrack3Completed;

        // Inscreve em eventos de coleta de chaves
        GameEvents.OnDoorKeyCollected += OnDoorKeyCollected;
        GameEvents.OnDrawerKeyCollected += OnDrawerKeyCollected;

        // Inscreve em evento de sanidade
        InsanityManager.OnSanityChanged += OnSanityChanged;

        // Inscreve em evento de ItemInteract customizado
        GameEvents.OnItemInteractSubtitle += OnItemInteractSubtitle;

        // Inscreve em evento de reset de cena
        GameEvents.OnSceneReset += OnSceneReset;
    }

    private void OnDisable()
    {
        // Desinscreve de todos os eventos
        GameEvents.OnRadioTrack1Completed -= OnRadioTrack1Completed;
        GameEvents.OnRadioTrack2Completed -= OnRadioTrack2Completed;
        GameEvents.OnRadioTrack3Completed -= OnRadioTrack3Completed;
        GameEvents.OnDoorKeyCollected -= OnDoorKeyCollected;
        GameEvents.OnDrawerKeyCollected -= OnDrawerKeyCollected;
        InsanityManager.OnSanityChanged -= OnSanityChanged;
        GameEvents.OnItemInteractSubtitle -= OnItemInteractSubtitle;
        GameEvents.OnSceneReset -= OnSceneReset;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Chamado quando a faixa 1 do rádio termina
    /// </summary>
    private void OnRadioTrack1Completed()
    {
        if (track1CompletedSubtitle != null && !track1CompletedSubtitle.IsEmpty)
        {
            ShowSubtitle(track1CompletedSubtitle, track1SubtitleDuration);
            
            if (debugMode)
                Debug.Log("[SubtitleManager] Exibindo legenda: Track 1 Completed");
        }
    }

    /// <summary>
    /// Chamado quando a faixa 2 do rádio termina
    /// </summary>
    private void OnRadioTrack2Completed()
    {
        if (track2CompletedSubtitle != null && !track2CompletedSubtitle.IsEmpty)
        {
            ShowSubtitle(track2CompletedSubtitle, track2SubtitleDuration);
            
            if (debugMode)
                Debug.Log("[SubtitleManager] Exibindo legenda: Track 2 Completed");
        }
    }

    /// <summary>
    /// Chamado quando a faixa 3 do rádio termina
    /// </summary>
    private void OnRadioTrack3Completed()
    {
        if (track3CompletedSubtitle != null && !track3CompletedSubtitle.IsEmpty)
        {
            ShowSubtitle(track3CompletedSubtitle, track3SubtitleDuration);
            
            if (debugMode)
                Debug.Log("[SubtitleManager] Exibindo legenda: Track 3 Completed");
        }
    }

    /// <summary>
    /// Chamado quando uma chave de porta é coletada
    /// </summary>
    private void OnDoorKeyCollected()
    {
        if (doorKeyCollectedSubtitle != null && !doorKeyCollectedSubtitle.IsEmpty)
        {
            ShowSubtitle(doorKeyCollectedSubtitle, doorKeySubtitleDuration);
            
            if (debugMode)
                Debug.Log("[SubtitleManager] Exibindo legenda: Door Key Collected");
        }
    }

    /// <summary>
    /// Chamado quando uma chave de gaveta é coletada
    /// </summary>
    private void OnDrawerKeyCollected()
    {
        if (drawerKeyCollectedSubtitle != null && !drawerKeyCollectedSubtitle.IsEmpty)
        {
            ShowSubtitle(drawerKeyCollectedSubtitle, drawerKeySubtitleDuration);
            
            if (debugMode)
                Debug.Log("[SubtitleManager] Exibindo legenda: Drawer Key Collected");
        }
    }

    /// <summary>
    /// Chamado quando a sanidade muda
    /// </summary>
    private void OnSanityChanged(float sanity)
    {
        // Apenas dispara quando sanidade chega a 0% pela primeira vez
        if (sanity <= 0f && !hasTriggeredLowSanity)
        {
            hasTriggeredLowSanity = true;
            
            if (lowSanitySubtitle != null && !lowSanitySubtitle.IsEmpty)
            {
                ShowSubtitle(lowSanitySubtitle, lowSanitySubtitleDuration);
                
                if (debugMode)
                    Debug.Log("[SubtitleManager] Exibindo legenda: Low Sanity");
            }
        }
        
        // Reset do flag quando sanidade volta acima de 0
        if (sanity > 0f)
        {
            hasTriggeredLowSanity = false;
        }
    }

    /// <summary>
    /// Chamado quando um ItemInteract específico quer mostrar legenda
    /// </summary>
    private void OnItemInteractSubtitle(LocalizedString subtitleKey, float duration)
    {
        if (subtitleKey != null && !subtitleKey.IsEmpty)
        {
            ShowSubtitle(subtitleKey, duration);
            
            if (debugMode)
                Debug.Log("[SubtitleManager] Exibindo legenda: ItemInteract Custom");
        }
    }

    /// <summary>
    /// Chamado quando a cena é resetada
    /// </summary>
    private void OnSceneReset()
    {
        // Limpa fila e para legendas ativas
        subtitleQueue.Clear();
        
        if (activeSubtitleCoroutine != null)
        {
            StopCoroutine(activeSubtitleCoroutine);
            activeSubtitleCoroutine = null;
        }
        
        isShowingSubtitle = false;
        IsSubtitleActive = false;
        hasTriggeredLowSanity = false;
        
        HideSubtitleImmediate();
        
        if (debugMode)
            Debug.Log("[SubtitleManager] Sistema resetado");
    }

    #endregion

    #region Subtitle Display Logic

    /// <summary>
    /// Cancela a legenda atualmente ativa (chamado pelo RadioSubtitleManager para priorizar legendas do rádio)
    /// </summary>
    public void CancelCurrentSubtitle()
    {
        // Para a corrotina ativa
        if (activeSubtitleCoroutine != null)
        {
            StopCoroutine(activeSubtitleCoroutine);
            activeSubtitleCoroutine = null;
        }
        
        // Limpa a fila
        subtitleQueue.Clear();
        
        // Desabilita o texto
        if (subtitleText != null)
        {
            subtitleText.enabled = false;
            subtitleText.text = "";
        }
        
        // Reseta estados
        IsSubtitleActive = false;
        isShowingSubtitle = false;
        
        if (debugMode)
            Debug.Log("[SubtitleManager] Legenda cancelada - Rádio tem prioridade");
    }

    /// <summary>
    /// Mostra uma legenda com a chave de localização especificada
    /// </summary>
    private void ShowSubtitle(LocalizedString subtitleKey, float duration)
    {
        if (subtitleKey == null || subtitleKey.IsEmpty)
        {
            Debug.LogWarning("[SubtitleManager] Tentativa de mostrar legenda com chave vazia!");
            return;
        }

        // Obtém o texto localizado
        string localizedText = subtitleKey.GetLocalizedString();
        
        if (string.IsNullOrEmpty(localizedText))
        {
            Debug.LogWarning("[SubtitleManager] Texto localizado está vazio!");
            return;
        }

        // Cria dados da legenda
        SubtitleData subtitleData = new SubtitleData(localizedText, duration);

        if (useQueue)
        {
            // Adiciona à fila
            subtitleQueue.Enqueue(subtitleData);
            
            // Se não está mostrando legenda, inicia processamento da fila
            if (!isShowingSubtitle)
            {
                ProcessNextSubtitle();
            }
        }
        else
        {
            // Cancela legenda anterior e mostra nova imediatamente
            if (activeSubtitleCoroutine != null)
            {
                StopCoroutine(activeSubtitleCoroutine);
            }
            
            activeSubtitleCoroutine = StartCoroutine(SubtitleCoroutine(localizedText, duration));
        }
    }

    /// <summary>
    /// Processa a próxima legenda da fila
    /// </summary>
    private void ProcessNextSubtitle()
    {
        if (subtitleQueue.Count == 0)
        {
            isShowingSubtitle = false;
            return;
        }

        isShowingSubtitle = true;
        SubtitleData nextSubtitle = subtitleQueue.Dequeue();
        
        activeSubtitleCoroutine = StartCoroutine(SubtitleCoroutine(nextSubtitle.text, nextSubtitle.duration));
    }

    /// <summary>
    /// Corrotina que controla a exibição de uma legenda
    /// </summary>
    private IEnumerator SubtitleCoroutine(string text, float duration)
    {
        IsSubtitleActive = true;

        // Configura e exibe o texto
        if (subtitleText != null)
        {
            // Garante que o GameObject está ativo
            if (!subtitleText.gameObject.activeInHierarchy)
            {
                subtitleText.gameObject.SetActive(true);
            }
            
            // Configura o texto e ativa o componente
            subtitleText.text = text;
            subtitleText.enabled = true;
        }

        // Aguarda duração
        yield return new WaitForSeconds(duration);

        // Desabilita texto (mas mantém GameObject ativo)
        if (subtitleText != null)
        {
            subtitleText.enabled = false;
            subtitleText.text = "";
        }

        IsSubtitleActive = false;

        // Processa próxima legenda da fila se estiver usando sistema de fila
        if (useQueue)
        {
            ProcessNextSubtitle();
        }
    }

    /// <summary>
    /// Oculta a legenda imediatamente
    /// </summary>
    private void HideSubtitleImmediate()
    {
        if (subtitleText != null)
        {
            subtitleText.enabled = false;
            subtitleText.text = "";
        }

        IsSubtitleActive = false;
    }

    #endregion

    #region Validation

    /// <summary>
    /// Valida a configuração do componente
    /// </summary>
    private void ValidateConfiguration()
    {
        if (subtitleText == null)
        {
            Debug.LogError("[SubtitleManager] subtitleText não está configurado! Atribua um TextMeshProUGUI no Inspector.", this);
        }
    }

    #endregion

    #region Editor Utilities

    #if UNITY_EDITOR
    
    [ContextMenu("Test Track 1 Subtitle")]
    private void TestTrack1Subtitle()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleManager] Testes só funcionam em runtime!");
            return;
        }

        OnRadioTrack1Completed();
    }

    [ContextMenu("Test Door Key Subtitle")]
    private void TestDoorKeySubtitle()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleManager] Testes só funcionam em runtime!");
            return;
        }

        OnDoorKeyCollected();
    }

    [ContextMenu("Test Low Sanity Subtitle")]
    private void TestLowSanitySubtitle()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleManager] Testes só funcionam em runtime!");
            return;
        }

        hasTriggeredLowSanity = false;
        OnSanityChanged(0f);
    }

    [ContextMenu("Clear Subtitle Queue")]
    private void ClearQueue()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleManager] Este comando só funciona em runtime!");
            return;
        }

        subtitleQueue.Clear();
        Debug.Log("[SubtitleManager] Fila de legendas limpa!");
    }

    #endif

    #endregion
}
