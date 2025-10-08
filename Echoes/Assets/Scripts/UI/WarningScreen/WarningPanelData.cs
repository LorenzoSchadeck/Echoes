using UnityEngine;

namespace Echoes.UI.WarningScreen
{
    [CreateAssetMenu(fileName = "New Warning Panel", menuName = "Echoes/Warning Panel")]
    public class WarningPanelData : ScriptableObject
    {
        [Header("Panel Content")]
        [Tooltip("Título do painel")]
        public string title = "";
        
        [TextArea(3, 6)]
        [Tooltip("Texto do painel")]
        public string text = "";
    }
}
