using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

/// <summary>
/// Componente simples para textos localizáveis do menu
/// Referência do texto + chave de localização que atualiza quando o idioma muda
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class MenuLocalizedText : MonoBehaviour
{
    [Header("Localization Settings")]
    [SerializeField] private LocalizedString localizedString;
    
    private TextMeshProUGUI textComponent;
    
    #region Unity Lifecycle
    
    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }
    
    void Start()
    {
        UpdateText();
        
        // Registra para atualizações automáticas quando o idioma mudar
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }
    
    void OnDestroy()
    {
        // Remove o listener para evitar memory leaks
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
    
    #endregion
    
    #region Localization Events
    
    /// <summary>
    /// Callback chamado quando o idioma muda
    /// </summary>
    private void OnLocaleChanged(Locale locale)
    {
        UpdateText();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Atualiza o texto baseado na LocalizedString configurada
    /// </summary>
    public void UpdateText()
    {
        if (localizedString == null || localizedString.IsEmpty || textComponent == null)
        {
            Debug.LogWarning($"[MenuLocalizedText] LocalizedString is not configured or text component missing for: {gameObject.name}");
            return;
        }
        
        // Usa o sistema nativo do Unity Localization para obter o texto
        var operation = localizedString.GetLocalizedStringAsync();
        operation.Completed += (asyncOperation) =>
        {
            if (asyncOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                if (textComponent != null)
                {
                    textComponent.text = asyncOperation.Result;
                }
            }
            else
            {
                Debug.LogWarning($"[MenuLocalizedText] Failed to get localized string for: {gameObject.name}");
                if (textComponent != null)
                {
                    textComponent.text = localizedString.TableEntryReference.ToString(); // Fallback
                }
            }
        };
    }
    

    
    /// <summary>
    /// Define uma nova LocalizedString
    /// </summary>
    public void SetLocalizedString(LocalizedString newLocalizedString)
    {
        localizedString = newLocalizedString;
        UpdateText();
    }
    
    /// <summary>
    /// Obtém a LocalizedString atual
    /// </summary>
    public LocalizedString GetLocalizedString()
    {
        return localizedString;
    }
    
    #endregion
    
    #region Editor Helper Methods
    
    #if UNITY_EDITOR
    /// <summary>
    /// Testa a atualização do texto no Editor
    /// </summary>
    [ContextMenu("Test Update Text")]
    private void TestUpdateText()
    {
        UpdateText();
        string keyInfo = localizedString != null ? localizedString.TableEntryReference.ToString() : "None";
        Debug.Log($"[MenuLocalizedText] Updated text for: {gameObject.name} with LocalizedString: {keyInfo}");
    }
    
    /// <summary>
    /// Valida a configuração do componente
    /// </summary>
    [ContextMenu("Validate Configuration")]
    private void ValidateConfiguration()
    {
        if (localizedString == null || localizedString.IsEmpty)
        {
            Debug.LogWarning($"[MenuLocalizedText] LocalizedString is not configured for: {gameObject.name}");
        }
        else
        {
            string keyInfo = localizedString.TableEntryReference.ToString();
            Debug.Log($"[MenuLocalizedText] Configuration is valid for: {gameObject.name}, LocalizedString: {keyInfo}");
        }
        
        if (textComponent == null)
        {
            Debug.LogError($"[MenuLocalizedText] TextMeshProUGUI component not found for: {gameObject.name}");
        }
    }
    #endif
    
    #endregion
}