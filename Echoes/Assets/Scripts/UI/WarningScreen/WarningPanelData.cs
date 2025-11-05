using UnityEngine;

namespace Echoes.UI.WarningScreen
{
    /// <summary>
    /// Idiomas suportados para os painéis de aviso
    /// </summary>
    public enum PanelLanguage
    {
        English,
        Portuguese
    }
    
    [CreateAssetMenu(fileName = "New Warning Panel", menuName = "Echoes/Warning Panel")]
    public class WarningPanelData : ScriptableObject
    {
        [Header("Language Settings")]
        [Tooltip("Idioma deste painel")]
        public PanelLanguage language = PanelLanguage.English;
        
        [Header("Panel Content")]
        [Tooltip("Título do painel")]
        public string title = "";
        
        [TextArea(3, 6)]
        [Tooltip("Texto do painel")]
        public string text = "";
        
        [Header("Display Settings")]
        [Tooltip("Duração em segundos que este painel ficará visível")]
        [Min(0.1f)]
        public float displayDuration = 3f;
        
        [Tooltip("Se marcado, este painel pode ser pulado após o tempo mínimo")]
        public bool isSkippable = true;
    }
}
