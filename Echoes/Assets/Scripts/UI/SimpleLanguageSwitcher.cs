using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using TMPro;
using System.Collections;
using FMODUnity;

/// <summary>
/// Switcher simples de idiomas para o menu
/// Apenas cicla entre os idiomas disponíveis quando o botão é clicado
/// Reproduz som de clique igual aos outros botões do menu
/// 
/// CONFIGURAÇÃO:
/// - Opção 1 (Recomendada): Coloque este script diretamente no botão de idioma
/// - Opção 2: Coloque em qualquer GameObject e certifique-se de que o botão tenha a tag "lang_button"
/// - O botão deve ter componente Button
/// - O texto deve estar no botão ou em seus filhos (TextMeshProUGUI)
/// - Configure o som de clique no campo "Button Click Sound" (opcional)
/// </summary>
public class SimpleLanguageSwitcher : MonoBehaviour
{
    [Header("UI Elements")]
    private Button languageButton;
    private TextMeshProUGUI languageButtonText;
    
    [Header("Supported Languages")]
    [SerializeField] private string[] supportedLanguages = { "en", "pt-BR" };
    [SerializeField] private string[] languageDisplayNames = { "English", "Português" };
    
    [Header("Audio")]
    [SerializeField] private EventReference buttonClickSound;
    
    private int currentLanguageIndex = 0;
    
    #region Unity Lifecycle
    
    void Start()
    {
        // Subscreve ao evento de reset de cena
        GameEvents.OnSceneReset += OnSceneReset;
        
        InitializeButtonReferences();
        LoadCurrentLanguage();
        UpdateButtonText();
    }
    
    void OnEnable()
    {
        // Re-inicializa as referências sempre que o objeto for ativado
        // Isso resolve o problema quando a cena é resetada
        // Usa uma pequena delay para garantir que a UI foi totalmente carregada
        StartCoroutine(DelayedInitialization());
    }
    
    void OnDestroy()
    {
        // Remove a subscrição do evento para evitar memory leaks
        GameEvents.OnSceneReset -= OnSceneReset;
    }
    
    #endregion
    
    #region Scene Reset Management
    
    /// <summary>
    /// Callback chamado quando a cena está sendo resetada
    /// </summary>
    private void OnSceneReset()
    {
        // Limpa as referências atuais
        languageButton = null;
        languageButtonText = null;
    }
    
    /// <summary>
    /// Inicialização com delay para garantir que a UI foi carregada
    /// </summary>
    private IEnumerator DelayedInitialization()
    {
        // Espera alguns frames para garantir que a UI foi totalmente inicializada
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        InitializeButtonReferences();
        
        // Se ainda não encontrou as referências, tenta novamente após um delay maior
        if (languageButton == null || languageButtonText == null)
        {
            yield return new WaitForSeconds(0.1f);
            InitializeButtonReferences();
        }
    }
    
    #endregion
    
    #region Button Reference Management
    
    /// <summary>
    /// Inicializa as referências do botão - busca pela tag "lang_button"
    /// </summary>
    private void InitializeButtonReferences()
    {
        // Método 1: Se o script estiver no próprio botão
        languageButton = GetComponent<Button>();
        if (languageButton != null)
        {
            // Busca o texto no mesmo objeto ou filhos
            languageButtonText = GetComponent<TextMeshProUGUI>();
            if (languageButtonText == null)
            {
                languageButtonText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        else
        {
            // Método 2: Busca pela tag "lang_button"
            GameObject buttonObject = GameObject.FindGameObjectWithTag("lang_button");
            if (buttonObject != null)
            {
                languageButton = buttonObject.GetComponent<Button>();
                languageButtonText = buttonObject.GetComponent<TextMeshProUGUI>();
                
                if (languageButtonText == null)
                {
                    languageButtonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
            else
            {
                Debug.LogError("[SimpleLanguageSwitcher] No GameObject found with tag 'lang_button'!");
                return;
            }
        }
        
        // Configura o listener do botão
        if (languageButton != null)
        {
            languageButton.onClick.RemoveListener(CycleLanguage);
            languageButton.onClick.AddListener(CycleLanguage);
        }
        
        if (languageButtonText == null)
        {
            Debug.LogWarning("[SimpleLanguageSwitcher] No TextMeshProUGUI found on button or children");
        }
    }
    

    
    #endregion
    
    #region Audio Management
    
    /// <summary>
    /// Reproduz o som de clique do botão
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (buttonClickSound.IsNull) return;
        RuntimeManager.PlayOneShot(buttonClickSound);
    }
    
    #endregion
    
    #region Language Management
    
    /// <summary>
    /// Cicla para o próximo idioma disponível
    /// </summary>
    public void CycleLanguage()
    {
        // Reproduz o som de clique do botão
        PlayButtonClickSound();
        
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
        // Se a referência do texto foi perdida, tenta re-inicializar
        if (languageButtonText == null)
        {
            InitializeButtonReferences();
        }
        
        if (languageButtonText != null && currentLanguageIndex < languageDisplayNames.Length)
        {
            languageButtonText.text = languageDisplayNames[currentLanguageIndex];
        }
        else if (languageButtonText == null)
        {
            Debug.LogWarning("[SimpleLanguageSwitcher] Could not update button text - TextMeshProUGUI reference is null");
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