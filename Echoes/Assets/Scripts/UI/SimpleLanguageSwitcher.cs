using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using TMPro;

/// <summary>
/// Switcher simples de idiomas para o menu
/// Apenas cicla entre os idiomas disponíveis quando o botão é clicado
/// </summary>
public class SimpleLanguageSwitcher : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button languageButton;
    [SerializeField] private TextMeshProUGUI languageButtonText;
    
    [Header("Supported Languages")]
    [SerializeField] private string[] supportedLanguages = { "en", "pt-BR" };
    [SerializeField] private string[] languageDisplayNames = { "English", "Português" };
    
    private int currentLanguageIndex = 0;
    
    #region Unity Lifecycle
    
    void Start()
    {
        if (languageButton != null)
        {
            languageButton.onClick.AddListener(CycleLanguage);
        }
        
        LoadCurrentLanguage();
        UpdateButtonText();
    }
    
    #endregion
    
    #region Language Management
    
    /// <summary>
    /// Cicla para o próximo idioma disponível
    /// </summary>
    public void CycleLanguage()
    {
        currentLanguageIndex = (currentLanguageIndex + 1) % supportedLanguages.Length;
        
        string newLanguage = supportedLanguages[currentLanguageIndex];
        
        // Salva no GameSettings se disponível
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetLanguage(newLanguage);
        }
        else
        {
            // Fallback: salva diretamente
            PlayerPrefs.SetString("CurrentLanguage", newLanguage);
            PlayerPrefs.Save();
            
            // Aplica o idioma diretamente no Unity Localization
            var targetLocale = LocalizationSettings.AvailableLocales.GetLocale(newLanguage);
            if (targetLocale != null)
            {
                LocalizationSettings.SelectedLocale = targetLocale;
            }
        }
        
        UpdateButtonText();
        
        Debug.Log($"[SimpleLanguageSwitcher] Language changed to: {newLanguage}");
    }
    
    /// <summary>
    /// Carrega o idioma atual salvo
    /// </summary>
    private void LoadCurrentLanguage()
    {
        string savedLanguage = "";
        
        if (GameSettings.Instance != null)
        {
            savedLanguage = GameSettings.Instance.CurrentLanguage;
        }
        else
        {
            savedLanguage = PlayerPrefs.GetString("CurrentLanguage", "en");
        }
        
        // Encontra o índice do idioma salvo
        for (int i = 0; i < supportedLanguages.Length; i++)
        {
            if (supportedLanguages[i] == savedLanguage)
            {
                currentLanguageIndex = i;
                break;
            }
        }
    }
    
    /// <summary>
    /// Atualiza o texto do botão com o idioma atual
    /// </summary>
    private void UpdateButtonText()
    {
        if (languageButtonText != null && currentLanguageIndex < languageDisplayNames.Length)
        {
            languageButtonText.text = languageDisplayNames[currentLanguageIndex];
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Obtém o código do idioma atual
    /// </summary>
    public string GetCurrentLanguageCode()
    {
        return supportedLanguages[currentLanguageIndex];
    }
    
    /// <summary>
    /// Obtém o nome de exibição do idioma atual
    /// </summary>
    public string GetCurrentLanguageName()
    {
        return languageDisplayNames[currentLanguageIndex];
    }
    
    #endregion
}