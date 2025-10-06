using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Localization;

/// <summary>
/// Gerenciador de legendas para o sistema de rádio
/// Controla a exibição sincronizada de legendas com as faixas do rádio
/// </summary>
public class RadioSubtitleManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshProUGUI onde as legendas serão exibidas (pode ser o mesmo do SimpleItemDisplay)")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    
    [Tooltip("TextMeshProUGUI onde o nome do falante será exibido (opcional - deixe em branco se não quiser usar)")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    
    [Header("Controle de Conflitos")]
    [Tooltip("Se verdadeiro, desabilita interação com SimpleItemDisplay enquanto legendas estão ativas")]
    [SerializeField] private bool blockSimpleItemDisplay = true;
    
    [Header("Subtitle Data")]
    [Tooltip("Dados das legendas para a Track 1")]
    [SerializeField] private RadioSubtitleData track1Subtitles;
    
    [Tooltip("Dados das legendas para a Track 2")]
    [SerializeField] private RadioSubtitleData track2Subtitles;
    
    [Tooltip("Dados das legendas para a Track 3")]
    [SerializeField] private RadioSubtitleData track3Subtitles;
    
    [Header("Debug")]
    [Tooltip("Se verdadeiro, mostra logs detalhados do sistema de legendas")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Estado interno
    private Coroutine currentSubtitleCoroutine;
    private RadioSubtitleData currentTrackData;
    private float trackStartTime;
    private bool isFirstTimePlayingTrack;
    private bool isSubtitlesActive = false;
    
    // Controle de conflitos com SimpleItemDisplay
    private static bool isRadioSubtitlesActive = false; // Estado global
    private string originalTextContent = ""; // Backup do texto original
    
    // Cache para evitar garbage collection
    private WaitForSeconds updateInterval = new WaitForSeconds(0.1f);
    
    private void Awake()
    {
        // Valida componentes essenciais
        if (subtitleText == null)
        {
            Debug.LogError("RadioSubtitleManager: subtitleText não está configurado!", this);
            enabled = false;
            return;
        }
        
        // IMPORTANTE: Texto inicia DESABILITADO - só ativa quando necessário
        subtitleText.enabled = false;
        subtitleText.text = "";
        
        // Inicializa o texto do falante se estiver configurado
        if (speakerNameText != null)
        {
            speakerNameText.enabled = false;
            speakerNameText.text = "";
            if (showDebugLogs) Debug.Log("RadioSubtitleManager: Speaker name text inicializado como DESABILITADO");
        }
        
        if (showDebugLogs) Debug.Log("RadioSubtitleManager: TextMeshPro inicializado como DESABILITADO");
    }
    
    private void Start()
    {
        // Valida dados das legendas
        ValidateSubtitleData();
    }
    
    /// <summary>
    /// Inicia as legendas para uma faixa específica
    /// </summary>
    /// <param name="trackNumber">Número da faixa (1, 2 ou 3)</param>
    /// <param name="isFirstTime">Se é a primeira vez tocando esta faixa</param>
    public void StartSubtitles(int trackNumber, bool isFirstTime = false)
    {
        if (showDebugLogs)
            Debug.Log($"RadioSubtitleManager: StartSubtitles() chamado - Track {trackNumber}, Primeira vez: {isFirstTime}");
        
        // Para legendas anteriores se estiverem rodando
        StopSubtitles();
        
        // Seleciona os dados da faixa
        switch (trackNumber)
        {
            case 1:
                currentTrackData = track1Subtitles;
                if (showDebugLogs) Debug.Log($"RadioSubtitleManager: Track1Subtitles = {(track1Subtitles != null ? track1Subtitles.name : "NULL")}");
                break;
            case 2:
                currentTrackData = track2Subtitles;
                if (showDebugLogs) Debug.Log($"RadioSubtitleManager: Track2Subtitles = {(track2Subtitles != null ? track2Subtitles.name : "NULL")}");
                break;
            case 3:
                currentTrackData = track3Subtitles;
                if (showDebugLogs) Debug.Log($"RadioSubtitleManager: Track3Subtitles = {(track3Subtitles != null ? track3Subtitles.name : "NULL")}");
                break;
            default:
                Debug.LogWarning($"RadioSubtitleManager: Número de faixa inválido: {trackNumber}");
                return;
        }
        
        if (currentTrackData == null)
        {
            Debug.LogWarning($"RadioSubtitleManager: ERRO - Nenhum dado de legenda configurado para Track {trackNumber}!");
            return;
        }
        
        // Configura estado
        trackStartTime = Time.time;
        isFirstTimePlayingTrack = isFirstTime;
        isSubtitlesActive = true;
        isRadioSubtitlesActive = true; // Marca globalmente que legendas estão ativas
        
        // Faz backup do texto atual se existir
        if (subtitleText != null && !string.IsNullOrEmpty(subtitleText.text))
        {
            originalTextContent = subtitleText.text;
            if (showDebugLogs) Debug.Log($"RadioSubtitleManager: Backup do texto atual: '{originalTextContent}'");
        }
        
        // Bloqueia SimpleItemDisplay se configurado
        if (blockSimpleItemDisplay)
        {
            BlockSimpleItemDisplayInteraction(true);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"RadioSubtitleManager: Iniciando legendas para Track {trackNumber} " +
                     $"(Primeira vez: {isFirstTime}, Total legendas: {currentTrackData.subtitles?.Length ?? 0})");
        }
        
        // Inicia a corrotina de controle das legendas
        currentSubtitleCoroutine = StartCoroutine(SubtitleUpdateCoroutine());
    }
    
    /// <summary>
    /// Para todas as legendas
    /// </summary>
    public void StopSubtitles()
    {
        if (currentSubtitleCoroutine != null)
        {
            StopCoroutine(currentSubtitleCoroutine);
            currentSubtitleCoroutine = null;
        }
        
        isSubtitlesActive = false;
        isRadioSubtitlesActive = false; // Marca globalmente que legendas pararam
        currentTrackData = null;
        
        // Restaura texto original se havia backup
        if (!string.IsNullOrEmpty(originalTextContent) && subtitleText != null)
        {
            subtitleText.enabled = true; // Reativa para SimpleItemDisplay
            subtitleText.text = originalTextContent;
            if (showDebugLogs) Debug.Log($"RadioSubtitleManager: Texto original restaurado e componente HABILITADO: '{originalTextContent}'");
            originalTextContent = "";
        }
        else
        {
            // Desabilita completamente se não há backup
            if (subtitleText != null)
            {
                subtitleText.enabled = false;
                subtitleText.text = "";
                if (showDebugLogs) Debug.Log("RadioSubtitleManager: Componente DESABILITADO (sem backup)");
            }
        }
        
        // Sempre esconde o nome do falante quando para as legendas
        if (speakerNameText != null)
        {
            speakerNameText.enabled = false;
            speakerNameText.text = "";
            if (showDebugLogs) Debug.Log("RadioSubtitleManager: Speaker name text DESABILITADO");
        }
        
        // Desbloqueia SimpleItemDisplay se estava bloqueado
        if (blockSimpleItemDisplay)
        {
            BlockSimpleItemDisplayInteraction(false);
        }
        
        if (showDebugLogs)
            Debug.Log("RadioSubtitleManager: Legendas paradas");
    }
    
    /// <summary>
    /// Corrotina principal que gerencia a exibição das legendas
    /// </summary>
    private IEnumerator SubtitleUpdateCoroutine()
    {
        if (showDebugLogs)
        {
            Debug.Log($"RadioSubtitleManager: SubtitleUpdateCoroutine INICIADA!");
            Debug.Log($"RadioSubtitleManager: TrackData = {currentTrackData?.name}");
            Debug.Log($"RadioSubtitleManager: Legendas no ScriptableObject = {currentTrackData?.subtitles?.Length ?? 0}");
            Debug.Log($"RadioSubtitleManager: Start Delay = {currentTrackData?.startDelay}s");
            Debug.Log($"RadioSubtitleManager: Max Duration = {currentTrackData?.maxDuration}s");
        }
        
        string currentDisplayedText = "";
        string currentDisplayedSpeaker = "";
        
        while (isSubtitlesActive && currentTrackData != null)
        {
            float currentTime = Time.time - trackStartTime;
            
            // Verifica se passou do tempo máximo
            if (currentTime > currentTrackData.maxDuration)
            {
                if (showDebugLogs)
                    Debug.Log($"RadioSubtitleManager: Tempo máximo atingido ({currentTime:F1}s > {currentTrackData.maxDuration}s) - parando legendas");
                break;
            }
            
            // Obtém legendas ativas no momento atual
            RadioSubtitle[] activeSubtitles = currentTrackData.GetActiveSubtitles(currentTime, isFirstTimePlayingTrack);
            
            // Debug das legendas verificadas
            if (showDebugLogs && currentTime < 5f) // Só mostra nos primeiros 5 segundos para não spammar
            {
                Debug.Log($"RadioSubtitleManager: Tempo {currentTime:F1}s - Legendas ativas: {activeSubtitles.Length}");
                if (currentTime > currentTrackData.startDelay && activeSubtitles.Length == 0)
                {
                    Debug.Log($"RadioSubtitleManager: AVISO - Passou do delay ({currentTrackData.startDelay}s) mas nenhuma legenda ativa!");
                }
            }
            
            // Determina o texto a ser exibido
            string newText = "";
            string newSpeakerName = "";
            if (activeSubtitles.Length > 0)
            {
                // Se há múltiplas legendas ativas, concatena com quebra de linha
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.Text.StringBuilder speakerSb = new System.Text.StringBuilder();
                
                for (int i = 0; i < activeSubtitles.Length; i++)
                {
                    if (i > 0) 
                    {
                        sb.AppendLine();
                        speakerSb.AppendLine();
                    }
                    
                    string localizedText = activeSubtitles[i].subtitleText?.GetLocalizedString() ?? "";
                    string localizedSpeaker = activeSubtitles[i].speakerName?.GetLocalizedString() ?? "";
                    
                    // Aplica formatação em itálico se solicitado
                    if (activeSubtitles[i].useItalic && !string.IsNullOrEmpty(localizedText))
                    {
                        localizedText = $"<i>{localizedText}</i>";
                    }
                    
                    sb.Append(localizedText);
                    speakerSb.Append(localizedSpeaker);
                }
                
                newText = sb.ToString();
                newSpeakerName = speakerSb.ToString();
            }
            
            // Atualiza a exibição se o texto ou nome do falante mudou
            if (newText != currentDisplayedText || newSpeakerName != currentDisplayedSpeaker)
            {
                currentDisplayedText = newText;
                currentDisplayedSpeaker = newSpeakerName;
                
                if (string.IsNullOrEmpty(newText))
                {
                    // Esconde legendas - desabilita o componente
                    subtitleText.enabled = false;
                    subtitleText.text = "";
                    
                    // Esconde também o nome do falante se configurado
                    if (speakerNameText != null)
                    {
                        speakerNameText.enabled = false;
                        speakerNameText.text = "";
                    }
                    
                    if (showDebugLogs)
                        Debug.Log($"RadioSubtitleManager: Escondendo legendas - componente DESABILITADO (tempo: {currentTime:F1}s)");
                }
                else
                {
                    // Mostra legendas com novo texto - ATIVA o componente
                    subtitleText.enabled = true;
                    subtitleText.text = newText;
                    
                    // Atualiza o nome do falante se configurado
                    if (speakerNameText != null)
                    {
                        if (!string.IsNullOrEmpty(newSpeakerName))
                        {
                            speakerNameText.enabled = true;
                            speakerNameText.text = newSpeakerName;
                        }
                        else
                        {
                            speakerNameText.enabled = false;
                            speakerNameText.text = "";
                        }
                    }
                    
                    if (showDebugLogs)
                    {
                        // Verifica se alguma legenda ativa usa itálico para mostrar no debug
                        bool hasItalic = false;
                        for (int i = 0; i < activeSubtitles.Length; i++)
                        {
                            if (activeSubtitles[i].useItalic)
                            {
                                hasItalic = true;
                                break;
                            }
                        }
                        
                        string italicInfo = hasItalic ? " [ITÁLICO]" : "";
                        Debug.Log($"RadioSubtitleManager: Exibindo: \"{newText}\" [Falante: \"{newSpeakerName}\"]{italicInfo} - componente HABILITADO (tempo: {currentTime:F1}s)");
                    }
                }
            }
            
            yield return updateInterval;
        }
        
        // Esconde legendas ao terminar - desabilita o componente
        if (subtitleText != null)
        {
            subtitleText.enabled = false;
            subtitleText.text = "";
        }
        
        // Esconde também o nome do falante
        if (speakerNameText != null)
        {
            speakerNameText.enabled = false;
            speakerNameText.text = "";
        }
        
        isSubtitlesActive = false;
        
        if (showDebugLogs)
            Debug.Log("RadioSubtitleManager: Corrotina de legendas finalizada");
    }
    

    
    /// <summary>
    /// Valida se os dados das legendas estão configurados corretamente
    /// </summary>
    private void ValidateSubtitleData()
    {
        if (showDebugLogs)
        {
            ValidateTrackData("Track 1", track1Subtitles);
            ValidateTrackData("Track 2", track2Subtitles);
            ValidateTrackData("Track 3", track3Subtitles);
        }
    }
    
    /// <summary>
    /// Valida dados de uma faixa específica
    /// </summary>
    private void ValidateTrackData(string trackName, RadioSubtitleData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"RadioSubtitleManager: {trackName} - Nenhum dado de legenda configurado");
            return;
        }
        
        string warnings = data.ValidateData();
        if (!string.IsNullOrEmpty(warnings))
        {
            Debug.LogWarning($"RadioSubtitleManager: {trackName} - Problemas encontrados:\n{warnings}");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"RadioSubtitleManager: {trackName} - Legendas validadas com sucesso " +
                     $"({data.subtitles?.Length ?? 0} legendas configuradas)");
        }
    }
    
    /// <summary>
    /// Métodos públicos para controle externo - compatibilidade com o RadioController
    /// </summary>
    public void StartTrack1Subtitles(bool isFirstTime = false) => StartSubtitles(1, isFirstTime);
    public void StartTrack2Subtitles(bool isFirstTime = false) => StartSubtitles(2, isFirstTime);
    public void StartTrack3Subtitles(bool isFirstTime = false) => StartSubtitles(3, isFirstTime);
    
    /// <summary>
    /// Obtém informações do estado atual das legendas (para debug/UI)
    /// </summary>
    public string GetCurrentStatus()
    {
        if (!isSubtitlesActive)
            return "Legendas inativas";
            
        if (currentTrackData == null)
            return "Nenhum dado de faixa carregado";
            
        float currentTime = Time.time - trackStartTime;
        var activeSubtitles = currentTrackData.GetActiveSubtitles(currentTime, isFirstTimePlayingTrack);
        
        return $"Faixa: {currentTrackData.trackName} | Tempo: {currentTime:F1}s | Legendas ativas: {activeSubtitles.Length}";
    }
    
    /// <summary>
    /// Controla o bloqueio de interação com SimpleItemDisplay
    /// O bloqueio real é feito através da verificação estática AreRadioSubtitlesActive()
    /// que é chamada diretamente no SimpleItemDisplay
    /// </summary>
    /// <param name="block">Se deve bloquear (true) ou desbloquear (false)</param>
    private void BlockSimpleItemDisplayInteraction(bool block)
    {
        // O controle de bloqueio é feito via estado estático
        // SimpleItemDisplay verifica AreRadioSubtitlesActive() automaticamente
        if (showDebugLogs)
        {
            Debug.Log($"RadioSubtitleManager: SimpleItemDisplay {(block ? "bloqueado" : "desbloqueado")} " +
                     $"via verificação estática AreRadioSubtitlesActive()");
        }
    }
    
    /// <summary>
    /// Método estático para outros scripts verificarem se legendas estão ativas
    /// </summary>
    public static bool AreRadioSubtitlesActive()
    {
        return isRadioSubtitlesActive;
    }
    
    /// <summary>
    /// Método estático para forçar reset do estado (útil para debug)
    /// </summary>
    public static void ForceResetSubtitleState()
    {
        isRadioSubtitlesActive = false;
        Debug.Log("RadioSubtitleManager: Estado de legendas resetado forçadamente");
    }
    
    private void OnDisable()
    {
        // Garante que as corrotinas são paradas quando o componente é desabilitado
        StopSubtitles();
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Desenha informações de debug no Inspector
    /// </summary>
    [UnityEditor.CustomEditor(typeof(RadioSubtitleManager))]
    public class RadioSubtitleManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            RadioSubtitleManager manager = (RadioSubtitleManager)target;
            
            if (Application.isPlaying)
            {
                UnityEditor.EditorGUILayout.Space();
                UnityEditor.EditorGUILayout.LabelField("Status Runtime", UnityEditor.EditorStyles.boldLabel);
                UnityEditor.EditorGUILayout.LabelField("Estado:", manager.GetCurrentStatus());
                
                // Botões de teste
                UnityEditor.EditorGUILayout.Space();
                UnityEditor.EditorGUILayout.LabelField("Testes", UnityEditor.EditorStyles.boldLabel);
                
                UnityEditor.EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Test Track 1"))
                    manager.StartTrack1Subtitles(true);
                if (GUILayout.Button("Test Track 2"))
                    manager.StartTrack2Subtitles(true);
                if (GUILayout.Button("Test Track 3"))
                    manager.StartTrack3Subtitles(true);
                UnityEditor.EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Stop Subtitles"))
                    manager.StopSubtitles();
            }
        }
    }
    #endif
}