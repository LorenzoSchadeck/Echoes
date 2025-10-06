using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Estrutura de dados para uma legenda do rádio
/// </summary>
[System.Serializable]
public struct RadioSubtitle
{
    [Tooltip("Texto da legenda que será exibido")]
    public LocalizedString subtitleText;
    
    [Tooltip("Nome/identificação de quem está falando (ex: 'Locutor', 'Operador', 'Voz Misteriosa', etc.)")]
    public LocalizedString speakerName;
    
    [Tooltip("Tempo em segundos quando a legenda deve aparecer (relativo ao início da faixa)")]
    [Min(0f)]
    public float startTime;
    
    [Tooltip("Duração em segundos que a legenda deve permanecer na tela")]
    [Min(0.1f)]
    public float duration;
    
    [Tooltip("Se verdadeiro, esta legenda só aparece se for a primeira vez tocando a faixa")]
    public bool firstTimeOnly;
    
    [Header("Formatação")]
    [Tooltip("Se verdadeiro, aplica formatação em itálico ao texto desta legenda")]
    public bool useItalic;
}

/// <summary>
/// ScriptableObject que contém todas as legendas para uma faixa específica do rádio
/// </summary>
[CreateAssetMenu(fileName = "RadioSubtitleData", menuName = "Echoes/Radio/Subtitle Data")]
public class RadioSubtitleData : ScriptableObject
{
    [Header("Configurações da Faixa")]
    [Tooltip("Nome identificador da faixa (ex: Track 1, Track 2, etc)")]
    public string trackName = "Track";
    
    [Tooltip("Duração total estimada da faixa em segundos")]
    [Min(1f)]
    public float trackDuration = 60f;
    
    [Header("Legendas")]
    [Tooltip("Lista de todas as legendas desta faixa")]
    public RadioSubtitle[] subtitles;
    
    [Header("Configurações de Timing")]
    [Tooltip("Delay em segundos antes das legendas começarem (padrão: 1.2s)")]
    [Min(0f)]
    public float startDelay = 1.2f;
    
    [Tooltip("Tempo máximo em segundos após o qual as legendas cessam (padrão: 32s)")]
    [Min(1f)]
    public float maxDuration = 32f;
    
    /// <summary>
    /// Valida se uma legenda deve ser exibida baseado no timing e configurações
    /// </summary>
    /// <param name="subtitle">A legenda a ser validada</param>
    /// <param name="currentTime">Tempo atual da reprodução</param>
    /// <param name="isFirstTime">Se é a primeira vez tocando esta faixa</param>
    /// <returns>True se a legenda deve ser exibida</returns>
    public bool ShouldDisplaySubtitle(RadioSubtitle subtitle, float currentTime, bool isFirstTime)
    {
        // Verifica se já passou do tempo máximo
        if (currentTime > maxDuration)
            return false;
            
        // Verifica se ainda não chegou no delay inicial
        if (currentTime < startDelay)
            return false;
            
        // Ajusta o tempo considerando o delay inicial
        float adjustedTime = currentTime - startDelay;
        
        // Verifica se está no timing correto da legenda
        bool inTimeRange = adjustedTime >= subtitle.startTime && 
                          adjustedTime <= (subtitle.startTime + subtitle.duration);
        
        // Verifica se deve aparecer apenas na primeira vez
        if (subtitle.firstTimeOnly && !isFirstTime)
            return false;
            
        return inTimeRange;
    }
    
    /// <summary>
    /// Obtém todas as legendas que devem estar ativas no tempo especificado
    /// </summary>
    /// <param name="currentTime">Tempo atual da reprodução</param>
    /// <param name="isFirstTime">Se é a primeira vez tocando esta faixa</param>
    /// <returns>Array das legendas ativas</returns>
    public RadioSubtitle[] GetActiveSubtitles(float currentTime, bool isFirstTime)
    {
        if (subtitles == null || subtitles.Length == 0)
            return new RadioSubtitle[0];
            
        System.Collections.Generic.List<RadioSubtitle> activeSubtitles = 
            new System.Collections.Generic.List<RadioSubtitle>();
            
        foreach (var subtitle in subtitles)
        {
            if (ShouldDisplaySubtitle(subtitle, currentTime, isFirstTime))
            {
                activeSubtitles.Add(subtitle);
            }
        }
        
        return activeSubtitles.ToArray();
    }
    
    /// <summary>
    /// Valida os dados das legendas e retorna warnings se houver problemas
    /// </summary>
    /// <returns>String com warnings, ou string vazia se tudo estiver ok</returns>
    public string ValidateData()
    {
        System.Text.StringBuilder warnings = new System.Text.StringBuilder();
        
        if (subtitles == null || subtitles.Length == 0)
        {
            warnings.AppendLine("⚠️ Nenhuma legenda configurada");
            return warnings.ToString();
        }
        
        for (int i = 0; i < subtitles.Length; i++)
        {
            var subtitle = subtitles[i];
            
            // Verifica se o texto está configurado
            if (subtitle.subtitleText == null || subtitle.subtitleText.IsEmpty)
            {
                warnings.AppendLine($"⚠️ Legenda {i}: Texto não configurado");
            }
            
            // Verifica se a legenda começa depois do tempo máximo
            float effectiveStartTime = subtitle.startTime + startDelay;
            if (effectiveStartTime >= maxDuration)
            {
                warnings.AppendLine($"⚠️ Legenda {i}: Começa após o tempo máximo ({effectiveStartTime}s >= {maxDuration}s)");
            }
            
            // Verifica se a legenda vai além da duração da faixa
            float endTime = effectiveStartTime + subtitle.duration;
            if (endTime > trackDuration)
            {
                warnings.AppendLine($"⚠️ Legenda {i}: Termina após a duração da faixa ({endTime}s > {trackDuration}s)");
            }
        }
        
        return warnings.ToString();
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Desenha informações de debug no Inspector
    /// </summary>
    [UnityEditor.CustomEditor(typeof(RadioSubtitleData))]
    public class RadioSubtitleDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            RadioSubtitleData data = (RadioSubtitleData)target;
            
            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField("Validação", UnityEditor.EditorStyles.boldLabel);
            
            string warnings = data.ValidateData();
            if (!string.IsNullOrEmpty(warnings))
            {
                UnityEditor.EditorGUILayout.HelpBox(warnings, UnityEditor.MessageType.Warning);
            }
            else
            {
                UnityEditor.EditorGUILayout.HelpBox("✅ Todas as legendas estão configuradas corretamente", UnityEditor.MessageType.Info);
            }
            
            // Mostra timeline das legendas
            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField("Timeline das Legendas", UnityEditor.EditorStyles.boldLabel);
            
            if (data.subtitles != null && data.subtitles.Length > 0)
            {
                for (int i = 0; i < data.subtitles.Length; i++)
                {
                    var subtitle = data.subtitles[i];
                    float effectiveStart = subtitle.startTime + data.startDelay;
                    float effectiveEnd = effectiveStart + subtitle.duration;
                    
                    string timeInfo = $"[{effectiveStart:F1}s - {effectiveEnd:F1}s]";
                    string firstTimeFlag = subtitle.firstTimeOnly ? " (Primeira vez apenas)" : "";
                    string italicFlag = subtitle.useItalic ? " [ITÁLICO]" : "";
                    
                    string speakerInfo = "";
                    if (subtitle.speakerName != null && !subtitle.speakerName.IsEmpty)
                    {
                        speakerInfo = $" - Falante: {subtitle.speakerName.GetLocalizedString()}";
                    }
                    
                    UnityEditor.EditorGUILayout.LabelField($"Legenda {i}: {timeInfo}{firstTimeFlag}{italicFlag}{speakerInfo}");
                }
            }
        }
    }
    #endif
}