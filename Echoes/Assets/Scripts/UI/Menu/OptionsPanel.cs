using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

/// <summary>
/// Painel de opções (Options.uxml), usado pelo menu principal e pelo menu de pausa.
/// Lê e escreve em <see cref="GameOptions"/>; cada mudança é aplicada e salva na hora.
/// </summary>
public class OptionsPanel
{
    private static readonly string[] TabNames = { "audio", "video", "controls", "access", "language" };

    // Ordem das opções do dropdown de janela
    private static readonly FullScreenMode[] WindowModes = { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
    private static readonly string[] WindowModeKeys = { "M_FULLSCREEN", "M_BORDERLESS", "M_WINDOWED" };

    private readonly VisualElement root;
    private readonly Action onBack;
    private readonly List<Button> tabs = new List<Button>();
    private readonly List<VisualElement> panes = new List<VisualElement>();

    private Slider masterSlider, musicSlider, sfxSlider, voiceSlider;
    private DropdownField resolutionDropdown, windowDropdown, qualityDropdown, languageDropdown;
    private Slider fovSlider, brightnessSlider, sensitivitySlider, subtitleSizeSlider, cameraShakeSlider;
    private Toggle invertYToggle, reduceFlashesToggle;

    private List<Resolution> resolutions = new List<Resolution>();

    public bool IsVisible => !root.ClassListContains("hidden");

    public OptionsPanel(VisualElement root, Action onBack)
    {
        this.root = root;
        this.onBack = onBack;

        foreach (string tabName in TabNames)
        {
            Button tab = root.Q<Button>($"tab-{tabName}");
            int index = tabs.Count;
            tab.clicked += () => SelectTab(index);
            tabs.Add(tab);
            panes.Add(root.Q($"pane-{tabName}"));
        }

        BindControls();
        root.Q<Button>("button-defaults").clicked += () =>
        {
            GameOptions.ResetToDefaults();
            Refresh();
        };
        root.Q<Button>("button-back").clicked += () => onBack();
    }

    public void Show()
    {
        root.RemoveFromClassList("hidden");
        Refresh();
        SelectTab(0);
        root.schedule.Execute(() => tabs[0].Focus());
    }

    public void Hide() => root.AddToClassList("hidden");

    private void SelectTab(int index)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            tabs[i].EnableInClassList("tab--selected", i == index);
            panes[i].EnableInClassList("pane--visible", i == index);
        }
    }

    private void BindControls()
    {
        masterSlider = BindSlider("slider-master", (s, v) => s.masterVolume = v);
        musicSlider = BindSlider("slider-music", (s, v) => s.musicVolume = v);
        sfxSlider = BindSlider("slider-sfx", (s, v) => s.sfxVolume = v);
        voiceSlider = BindSlider("slider-voice", (s, v) => s.voiceVolume = v);

        fovSlider = BindSlider("slider-fov", (s, v) => s.fieldOfView = v);
        brightnessSlider = BindSlider("slider-brightness", (s, v) => s.brightness = v);
        sensitivitySlider = BindSlider("slider-sensitivity", (s, v) => s.mouseSensitivity = v);
        subtitleSizeSlider = BindSlider("slider-subtitle-size", (s, v) => s.subtitleScale = v);
        cameraShakeSlider = BindSlider("slider-camera-shake", (s, v) => s.cameraShake = v);

        invertYToggle = BindToggle("toggle-invert-y", (s, v) => s.invertY = v);
        reduceFlashesToggle = BindToggle("toggle-reduce-flashes", (s, v) => s.reduceFlashes = v);

        resolutionDropdown = BindDropdown("dropdown-resolution", index => GameOptions.Change(s =>
        {
            s.resolutionWidth = resolutions[index].width;
            s.resolutionHeight = resolutions[index].height;
        }));

        windowDropdown = BindDropdown("dropdown-window", index => GameOptions.Change(s => s.fullScreenMode = WindowModes[index]));
        qualityDropdown = BindDropdown("dropdown-quality", index => GameOptions.Change(s => s.qualityLevel = index));
        languageDropdown = BindDropdown("dropdown-language", index =>
            GameOptions.Change(s => s.language = LocalizationSettings.AvailableLocales.Locales[index].Identifier.Code));
    }

    private DropdownField BindDropdown(string name, Action<int> onChosen)
    {
        DropdownField dropdown = root.Q<DropdownField>(name);
        dropdown.RegisterValueChangedCallback(evt =>
        {
            // ChangeEvent<string> também chega quando o texto do rótulo do campo muda; só vale o do próprio campo
            if (evt.target != dropdown || dropdown.index < 0) return;
            onChosen(dropdown.index);
        });
        return dropdown;
    }

    private Slider BindSlider(string name, Action<GameOptionsData, float> apply)
    {
        Slider slider = root.Q<Slider>(name);
        slider.RegisterValueChangedCallback(evt => GameOptions.Change(s => apply(s, evt.newValue)));
        return slider;
    }

    private Toggle BindToggle(string name, Action<GameOptionsData, bool> apply)
    {
        Toggle toggle = root.Q<Toggle>(name);
        toggle.RegisterValueChangedCallback(evt => GameOptions.Change(s => apply(s, evt.newValue)));
        return toggle;
    }

    /// <summary>
    /// Mostra os valores atuais sem disparar mudanças.
    /// </summary>
    public void Refresh()
    {
        GameOptionsData options = GameOptions.Current;

        masterSlider.SetValueWithoutNotify(options.masterVolume);
        SetAudioSlider(musicSlider, GameOptions.MusicBus, options.musicVolume);
        SetAudioSlider(sfxSlider, GameOptions.SfxBus, options.sfxVolume);
        SetAudioSlider(voiceSlider, GameOptions.VoiceBus, options.voiceVolume);

        fovSlider.SetValueWithoutNotify(options.fieldOfView);
        brightnessSlider.SetValueWithoutNotify(options.brightness);
        sensitivitySlider.SetValueWithoutNotify(options.mouseSensitivity);
        subtitleSizeSlider.SetValueWithoutNotify(options.subtitleScale);
        cameraShakeSlider.SetValueWithoutNotify(options.cameraShake);
        invertYToggle.SetValueWithoutNotify(options.invertY);
        reduceFlashesToggle.SetValueWithoutNotify(options.reduceFlashes);

        RefreshResolutions(options);
        RefreshWindowModes(options);
        RefreshQuality(options);
        RefreshLanguages();
    }

    // Um bus que não existe no projeto FMOD não tem o que controlar: a opção some
    private static void SetAudioSlider(Slider slider, string busPath, float value)
    {
        bool exists = GameOptions.AudioBusExists(busPath);
        slider.style.display = exists ? DisplayStyle.Flex : DisplayStyle.None;
        slider.SetValueWithoutNotify(value);
    }

    private void RefreshResolutions(GameOptionsData options)
    {
        // Uma entrada por tamanho, com a maior taxa de atualização
        resolutions = Screen.resolutions
            .GroupBy(r => (r.width, r.height))
            .Select(g => g.OrderByDescending(r => r.refreshRateRatio.value).First())
            .OrderByDescending(r => r.width).ThenByDescending(r => r.height)
            .ToList();

        if (resolutions.Count == 0) resolutions.Add(Screen.currentResolution);

        int width = options.resolutionWidth > 0 ? options.resolutionWidth : Screen.width;
        int height = options.resolutionHeight > 0 ? options.resolutionHeight : Screen.height;
        int current = resolutions.FindIndex(r => r.width == width && r.height == height);

        resolutionDropdown.choices = resolutions.Select(r => $"{r.width} × {r.height}").ToList();
        resolutionDropdown.SetValueWithoutNotify(resolutionDropdown.choices[Mathf.Max(0, current)]);
    }

    private void RefreshWindowModes(GameOptionsData options)
    {
        windowDropdown.choices = WindowModeKeys.Select(MenuText.Get).ToList();
        int current = Mathf.Max(0, Array.IndexOf(WindowModes, options.fullScreenMode));
        windowDropdown.SetValueWithoutNotify(windowDropdown.choices[current]);
    }

    private void RefreshQuality(GameOptionsData options)
    {
        // Os níveis se chamam Low/Medium/High no projeto; o jogador vê o nome traduzido
        qualityDropdown.choices = QualitySettings.names.Select((_, i) => MenuText.Get("M_QUALITY_" + i)).ToList();
        int current = options.qualityLevel >= 0 ? options.qualityLevel : QualitySettings.GetQualityLevel();
        qualityDropdown.SetValueWithoutNotify(qualityDropdown.choices[Mathf.Clamp(current, 0, qualityDropdown.choices.Count - 1)]);
    }

    private void RefreshLanguages()
    {
        var locales = LocalizationSettings.AvailableLocales?.Locales;
        if (locales == null || locales.Count == 0) return;

        languageDropdown.choices = locales.Select(LanguageName).ToList();
        int current = Mathf.Max(0, locales.IndexOf(LocalizationSettings.SelectedLocale));
        languageDropdown.SetValueWithoutNotify(languageDropdown.choices[current]);
    }

    // Nome do idioma nele mesmo ("Português (Brasil)", "English")
    private static string LanguageName(UnityEngine.Localization.Locale locale)
    {
        string name = locale.Identifier.CultureInfo?.NativeName ?? locale.LocaleName;
        return string.IsNullOrEmpty(name) ? locale.LocaleName : char.ToUpper(name[0]) + name.Substring(1);
    }
}
