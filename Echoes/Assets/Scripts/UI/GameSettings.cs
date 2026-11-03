using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// Fachada antiga das configurações, mantida para os scripts que já a usam.
/// Os valores vivem em <see cref="GameOptions"/>, que aplica e salva tudo.
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public float MasterVolume
    {
        get => GameOptions.Current.masterVolume;
        set => SetMasterVolume(value);
    }

    public float MouseSensitivity
    {
        get => GameOptions.Current.mouseSensitivity;
        set => SetMouseSensitivity(value);
    }

    public string CurrentLanguage
    {
        get => string.IsNullOrEmpty(GameOptions.Current.language) && LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.Identifier.Code
            : GameOptions.Current.language;
        set => SetLanguage(value);
    }

    public void SetMasterVolume(float volume) => GameOptions.Change(s => s.masterVolume = volume);

    public void SetMouseSensitivity(float sensitivity) => GameOptions.Change(s => s.mouseSensitivity = sensitivity);

    public void SetLanguage(string languageCode)
    {
        if (!string.IsNullOrEmpty(languageCode)) GameOptions.Change(s => s.language = languageCode);
    }

    public void ResetToDefaults() => GameOptions.ResetToDefaults();

    // Os CustomInputAxisController leem a sensibilidade do GameOptions por conta própria;
    // estes métodos ficam por compatibilidade com quem ainda os chama.
    public void ApplyMouseSensitivity() { }
    public void SetCustomInputAxisController(CustomInputAxisController controller) { }
    public void RegisterInputAxisController(CustomInputAxisController controller) { }
    public void UnregisterInputAxisController(CustomInputAxisController controller) { }

    public int GetMasterVolumePercentage() => Mathf.RoundToInt(MasterVolume * 100f);

    // 0.01–3.0 mapeado para 1–100%
    public int GetMouseSensitivityPercentage()
    {
        return Mathf.RoundToInt((MouseSensitivity - GameOptions.MinSensitivity) / (GameOptions.MaxSensitivity - GameOptions.MinSensitivity) * 99f + 1f);
    }

    public void SetMasterVolumeFromPercentage(int percentage) => SetMasterVolume(Mathf.Clamp01(percentage / 100f));

    public void SetMouseSensitivityFromPercentage(int percentage)
    {
        float normalized = (Mathf.Clamp(percentage, 1, 100) - 1f) / 99f;
        SetMouseSensitivity(Mathf.Lerp(GameOptions.MinSensitivity, GameOptions.MaxSensitivity, normalized));
    }

    public string GetCurrentLanguageName()
    {
        return LocalizationSettings.SelectedLocale != null ? LocalizationSettings.SelectedLocale.LocaleName : CurrentLanguage;
    }

    public System.Collections.Generic.List<string> GetAvailableLanguages()
    {
        var languages = new System.Collections.Generic.List<string>();
        if (LocalizationSettings.AvailableLocales == null) return languages;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            languages.Add($"{locale.LocaleName} ({locale.Identifier.Code})");
        }
        return languages;
    }

    public void CycleToNextLanguage()
    {
        if (LocalizationSettings.AvailableLocales == null || LocalizationSettings.AvailableLocales.Locales.Count <= 1) return;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        int currentIndex = locales.FindIndex(locale => locale.Identifier.Code == CurrentLanguage);
        SetLanguage(locales[(currentIndex + 1) % locales.Count].Identifier.Code);
    }
}
