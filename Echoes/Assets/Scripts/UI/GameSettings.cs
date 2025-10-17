using UnityEngine;
using Unity.Cinemachine;
using FMODUnity;
using UnityEngine.Localization.Settings;

/// <summary>
/// Singleton para gerenciar configurações do jogo
/// Controla volume geral e sensibilidade do mouse
/// </summary>
public class GameSettings : MonoBehaviour
{
    #region Singleton Pattern
    
    public static GameSettings Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    #region Settings Properties
    
    [Header("Audio Settings")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1.0f;
    
    [Header("Input Settings")]
    [SerializeField] [Range(0.01f, 3f)] private float mouseSensitivity = 1.0f;
    
    [Header("Localization Settings")]
    [SerializeField] private string currentLanguage = "en";
    
    [Header("References")]
    [SerializeField] private CustomInputAxisController customInputAxisController;
    
    // Lista de todos os controllers ativos para aplicar sensibilidade
    private System.Collections.Generic.List<CustomInputAxisController> registeredControllers = new();
    
    // Public properties for external access
    public float MasterVolume 
    { 
        get => masterVolume; 
        set => SetMasterVolume(value); 
    }
    
    public float MouseSensitivity 
    { 
        get => mouseSensitivity; 
        set => SetMouseSensitivity(value); 
    }
    
    public string CurrentLanguage 
    { 
        get => currentLanguage; 
        set => SetLanguage(value); 
    }
    
    #endregion
    
    #region FMOD Integration
    
    private FMOD.Studio.Bus masterBus;
    private bool fmodInitialized = false;
    
    /// <summary>
    /// Initializes FMOD bus reference
    /// </summary>
    private void InitializeFMOD()
    {
        try
        {
            // Get Master Bus reference
            masterBus = RuntimeManager.GetBus("bus:/");
            fmodInitialized = true;
            
            // Apply current volume setting
            ApplyMasterVolume();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to initialize FMOD Master Bus: {e.Message}");
            fmodInitialized = false;
        }
    }
    
    /// <summary>
    /// Applies master volume to FMOD Master Bus
    /// </summary>
    private void ApplyMasterVolume()
    {
        if (!fmodInitialized) return;
        
        try
        {
            masterBus.setVolume(masterVolume);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to set FMOD Master Bus volume: {e.Message}");
        }
    }
    
    #endregion
    
    #region Settings Management
    
    /// <summary>
    /// Initializes settings system - loads saved values or sets defaults
    /// </summary>
    private void InitializeSettings()
    {
        // Load saved settings or use defaults
        LoadSettings();
        
        // Initialize FMOD after a short delay to ensure it's ready
        Invoke(nameof(InitializeFMOD), 0.1f);
        
        // Apply sensitivity if inputAxisController is assigned
        ApplyMouseSensitivity();
        
        // Apply language after localization system initializes
        StartCoroutine(InitializeLanguageWhenReady());
    }
    
    /// <summary>
    /// Initializes language after localization system is ready
    /// </summary>
    private System.Collections.IEnumerator InitializeLanguageWhenReady()
    {
        // Wait for localization system to be initialized
        yield return LocalizationSettings.InitializationOperation;
        
        // Apply saved language
        ApplyLanguage();
    }
    
    /// <summary>
    /// Sets master volume and saves to PlayerPrefs
    /// </summary>
    /// <param name="volume">Volume value (0.0 - 1.0)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyMasterVolume();
        SaveSettings();
    }
    
    /// <summary>
    /// Sets mouse sensitivity and saves to PlayerPrefs
    /// </summary>
    /// <param name="sensitivity">Sensitivity value (0.01 - 3.0)</param>
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.01f, 3.0f);
        ApplyMouseSensitivity();
        SaveSettings();
    }
    
    /// <summary>
    /// Sets current language and applies it to localization system
    /// </summary>
    /// <param name="languageCode">Language code (e.g., "en", "pt-BR")</param>
    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode)) return;
        
        currentLanguage = languageCode;
        ApplyLanguage();
        SaveSettings();
    }
    

    
    /// <summary>
    /// Applies mouse sensitivity to all registered CustomInputAxisController instances
    /// </summary>
    public void ApplyMouseSensitivity()
    {
        // Aplica no controller principal (compatibilidade com código anterior)
        if (customInputAxisController != null)
        {
            customInputAxisController.UpdateSensitivity(mouseSensitivity);
        }
        
        // Aplica em todos os controllers registrados
        for (int i = registeredControllers.Count - 1; i >= 0; i--)
        {
            if (registeredControllers[i] != null)
            {
                registeredControllers[i].UpdateSensitivity(mouseSensitivity);
            }
            else
            {
                // Remove referências nulas
                registeredControllers.RemoveAt(i);
            }
        }
        
        Debug.Log($"[GameSettings] Sensibilidade {mouseSensitivity:F2} aplicada a {registeredControllers.Count + (customInputAxisController != null ? 1 : 0)} controller(s)");
    }
    
    /// <summary>
    /// Applies current language to Unity Localization System
    /// </summary>
    private void ApplyLanguage()
    {
        if (LocalizationSettings.AvailableLocales == null) return;
        
        var targetLocale = LocalizationSettings.AvailableLocales.GetLocale(currentLanguage);
        
        if (targetLocale != null)
        {
            LocalizationSettings.SelectedLocale = targetLocale;
        }
    }
    

    
    /// <summary>
    /// Sets the custom input axis controller reference (called from MenuUIManager or Inspector)
    /// </summary>
    /// <param name="controller">CustomInputAxisController reference</param>
    public void SetCustomInputAxisController(CustomInputAxisController controller)
    {
        customInputAxisController = controller;
        ApplyMouseSensitivity();
    }
    
    /// <summary>
    /// Registra um CustomInputAxisController para receber atualizações de sensibilidade
    /// Usado para câmeras adicionais como peephole, zoom, etc.
    /// </summary>
    /// <param name="controller">Controller para registrar</param>
    public void RegisterInputAxisController(CustomInputAxisController controller)
    {
        if (controller == null) return;
        
        // Evita duplicatas
        if (!registeredControllers.Contains(controller))
        {
            registeredControllers.Add(controller);
            
            // Aplica a sensibilidade atual imediatamente
            controller.UpdateSensitivity(mouseSensitivity);
            
            Debug.Log($"[GameSettings] Controller registrado: {controller.name} - Total: {registeredControllers.Count}");
        }
    }
    
    /// <summary>
    /// Remove um CustomInputAxisController da lista de registrados
    /// </summary>
    /// <param name="controller">Controller para remover</param>
    public void UnregisterInputAxisController(CustomInputAxisController controller)
    {
        if (controller != null && registeredControllers.Remove(controller))
        {
            Debug.Log($"[GameSettings] Controller removido: {controller.name} - Total: {registeredControllers.Count}");
        }
    }
    

    
    #endregion
    
    #region Save/Load System
    
    /// <summary>
    /// Saves current settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.SetString("CurrentLanguage", currentLanguage);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Loads settings from PlayerPrefs or sets defaults
    /// </summary>
    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        currentLanguage = PlayerPrefs.GetString("CurrentLanguage", "en");
        
        // Ensure values are within valid ranges
        masterVolume = Mathf.Clamp01(masterVolume);
        mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.01f, 3.0f);
    }
    
    /// <summary>
    /// Resets all settings to default values
    /// </summary>
    public void ResetToDefaults()
    {
        SetMasterVolume(1.0f);
        SetMouseSensitivity(1.0f);
        SetLanguage("en");
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Gets master volume as percentage (0-100)
    /// </summary>
    public int GetMasterVolumePercentage()
    {
        return Mathf.RoundToInt(masterVolume * 100f);
    }
    
    /// <summary>
    /// Gets mouse sensitivity as percentage (1-100)
    /// </summary>
    public int GetMouseSensitivityPercentage()
    {
        // Mapeia de 0.01-3.0 para 1-100%
        // 0.01 = 1%, 3.0 = 100%
        return Mathf.RoundToInt(((mouseSensitivity - 0.01f) / (3.0f - 0.01f)) * 99f + 1f);
    }
    
    /// <summary>
    /// Sets master volume from percentage (0-100)
    /// </summary>
    public void SetMasterVolumeFromPercentage(int percentage)
    {
        float volume = Mathf.Clamp01(percentage / 100f);
        SetMasterVolume(volume);
    }
    
    /// <summary>
    /// Sets mouse sensitivity from percentage (1-100, where 1% = 0.01 sensitivity, 100% = 3.0 sensitivity)
    /// </summary>
    public void SetMouseSensitivityFromPercentage(int percentage)
    {
        // Mapeia de 1-100% para 0.01-3.0
        // 1% = 0.01, 100% = 3.0
        float normalizedPercentage = Mathf.Clamp(percentage, 1, 100);
        float sensitivity = 0.01f + ((normalizedPercentage - 1f) / 99f) * (3.0f - 0.01f);
        SetMouseSensitivity(sensitivity);
    }
    
    /// <summary>
    /// Gets current language name for display
    /// </summary>
    public string GetCurrentLanguageName()
    {
        if (LocalizationSettings.SelectedLocale != null)
        {
            return LocalizationSettings.SelectedLocale.LocaleName;
        }
        return currentLanguage;
    }
    
    /// <summary>
    /// Gets all available languages
    /// </summary>
    public System.Collections.Generic.List<string> GetAvailableLanguages()
    {
        var languages = new System.Collections.Generic.List<string>();
        
        if (LocalizationSettings.AvailableLocales != null)
        {
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                languages.Add($"{locale.LocaleName} ({locale.Identifier.Code})");
            }
        }
        
        return languages;
    }
    
    /// <summary>
    /// Cycles to next available language
    /// </summary>
    public void CycleToNextLanguage()
    {
        if (LocalizationSettings.AvailableLocales == null || LocalizationSettings.AvailableLocales.Locales.Count <= 1)
            return;
            
        var locales = LocalizationSettings.AvailableLocales.Locales;
        int currentIndex = -1;
        
        // Find current locale index
        for (int i = 0; i < locales.Count; i++)
        {
            if (locales[i].Identifier.Code == currentLanguage)
            {
                currentIndex = i;
                break;
            }
        }
        
        // Move to next locale
        int nextIndex = (currentIndex + 1) % locales.Count;
        SetLanguage(locales[nextIndex].Identifier.Code);
    }
    
    #endregion
}