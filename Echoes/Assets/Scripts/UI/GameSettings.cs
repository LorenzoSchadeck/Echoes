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
        
        Debug.Log("[GameSettings] Language system initialized");
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
        
        Debug.Log($"[GameSettings] Language changed to: {languageCode}");
    }
    

    
    /// <summary>
    /// Applies mouse sensitivity to CustomInputAxisController
    /// </summary>
    public void ApplyMouseSensitivity()
    {
        if (customInputAxisController != null)
        {
            customInputAxisController.UpdateSensitivity(mouseSensitivity);
            Debug.Log($"[GameSettings] ✅ Applied sensitivity {mouseSensitivity:F2} to CustomInputAxisController");
        }
        else
        {
            Debug.LogWarning("[GameSettings] No CustomInputAxisController assigned! Please assign one in the References section.");
        }
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
            Debug.Log($"[GameSettings] ✅ Applied language: {targetLocale.LocaleName} ({currentLanguage})");
        }
        else
        {
            Debug.LogWarning($"[GameSettings] Language '{currentLanguage}' not found in available locales. Using default.");
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
        Debug.Log($"[GameSettings] ✅ CustomInputAxisController assigned and sensitivity applied");
    }
    
    /// <summary>
    /// Debug method - shows current CustomInputAxisController state
    /// </summary>
    [ContextMenu("Debug Custom Input Axis Controller")]
    public void DebugCustomInputAxisController()
    {
        if (customInputAxisController == null)
        {
            Debug.LogWarning("[GameSettings] No CustomInputAxisController assigned.");
            return;
        }
        
        Debug.Log($"[GameSettings] === CustomInputAxisController Debug ===");
        Debug.Log($"  GameObject: {customInputAxisController.gameObject.name}");
        Debug.Log($"  Current Sensitivity: {mouseSensitivity:F2}");
        Debug.Log($"  Controller Sensitivity: {customInputAxisController.MouseSensitivity:F2}");
        Debug.Log($"  Look Action: {(customInputAxisController.LookAction != null ? "✅ Assigned" : "❌ Missing")}");
        Debug.Log($"  Controllers Count: {customInputAxisController.Controllers?.Count ?? 0}");
        Debug.Log($"  Invert Y: {customInputAxisController.InvertY}");
        
        // Trigger the controller's own debug method
        customInputAxisController.DebugCurrentState();
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
        
        Debug.Log($"[GameSettings] Settings saved: Volume={GetMasterVolumePercentage()}%, Sensitivity={GetMouseSensitivityPercentage()}%, Language={currentLanguage}");
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
        
        Debug.Log($"[GameSettings] Settings loaded: Volume={GetMasterVolumePercentage()}%, Sensitivity={GetMouseSensitivityPercentage()}%, Language={currentLanguage}");
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
    
    #region Debug Methods
    
    /// <summary>
    /// Logs current settings values (for debugging)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogCurrentSettings()
    {
        Debug.Log($"GameSettings - Master Volume: {masterVolume:F2} ({GetMasterVolumePercentage()}%), " +
                  $"Mouse Sensitivity: {mouseSensitivity:F2} ({GetMouseSensitivityPercentage()}%), " +
                  $"Language: {GetCurrentLanguageName()} ({currentLanguage})");
    }
    
    /// <summary>
    /// Debug method for language system
    /// </summary>
    [ContextMenu("Debug Language System")]
    public void DebugLanguageSystem()
    {
        Debug.Log($"[GameSettings] === Language System Debug ===");
        Debug.Log($"  Current Language Code: {currentLanguage}");
        Debug.Log($"  Current Language Name: {GetCurrentLanguageName()}");
        Debug.Log($"  Available Languages: {string.Join(", ", GetAvailableLanguages())}");
        
        if (LocalizationSettings.SelectedLocale != null)
        {
            Debug.Log($"  Unity Selected Locale: {LocalizationSettings.SelectedLocale.LocaleName}");
        }
        else
        {
            Debug.Log($"  Unity Selected Locale: ❌ None");
        }
    }
    
    /// <summary>
    /// Context menu method to cycle language for testing
    /// </summary>
    [ContextMenu("Cycle Language")]
    public void DebugCycleLanguage()
    {
        CycleToNextLanguage();
    }
    
    #endregion
}