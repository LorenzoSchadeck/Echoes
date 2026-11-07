using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

/// <summary>
/// Painel de opções (Options.uxml), usado pelo menu principal e pelo menu de pausa.
/// Lê e escreve em <see cref="GameOptions"/>; cada mudança é aplicada e salva na hora.
/// Abas trocam com Q/E ou LB/RB, a opção destacada é explicada à direita e R/Y restaura os
/// padrões da aba. Também tem a calibração de brilho com o carimbo da polícia.
/// O controlador dono do painel chama <see cref="Tick"/> a cada quadro enquanto ele está aberto.
/// </summary>
public class OptionsPanel
{
    private static readonly string[] TabNames = { "game", "video", "audio", "controls", "access" };

    // Ordem das opções do dropdown de janela
    private static readonly FullScreenMode[] WindowModes = { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
    private static readonly string[] WindowModeKeys = { "M_FULLSCREEN", "M_BORDERLESS", "M_WINDOWED" };

    // Texto explicativo de cada opção (tabela "menu")
    private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        { "dropdown-language", "M_D_LANGUAGE" },
        { "toggle-subtitles", "M_D_SUBTITLES" },
        { "slider-subtitle-size", "M_D_SUB_SIZE" },
        { "dropdown-resolution", "M_D_RESOLUTION" },
        { "dropdown-window", "M_D_WINDOW" },
        { "dropdown-quality", "M_D_QUALITY" },
        { "slider-fov", "M_D_FOV" },
        { "slider-brightness", "M_D_BRIGHTNESS" },
        { "button-calibrate", "M_D_CALIBRATE" },
        { "slider-master", "M_D_MASTER" },
        { "slider-music", "M_D_MUSIC" },
        { "slider-sfx", "M_D_SFX" },
        { "slider-voice", "M_D_VOICE" },
        { "slider-sensitivity", "M_D_SENSITIVITY" },
        { "toggle-invert-y", "M_D_INVERT_Y" },
        { "toggle-reduce-flashes", "M_D_FLASHES" },
        { "slider-camera-shake", "M_D_SHAKE" },
    };

    // O que cada aba restaura com R/Y
    private static readonly Action<GameOptionsData, GameOptionsData>[] TabDefaults =
    {
        (s, d) => { s.subtitles = d.subtitles; s.subtitleScale = d.subtitleScale; },
        (s, d) => { s.fieldOfView = d.fieldOfView; s.brightness = d.brightness; s.qualityLevel = d.qualityLevel; s.fullScreenMode = d.fullScreenMode; },
        (s, d) => { s.masterVolume = d.masterVolume; s.musicVolume = d.musicVolume; s.sfxVolume = d.sfxVolume; s.voiceVolume = d.voiceVolume; },
        (s, d) => { s.mouseSensitivity = d.mouseSensitivity; s.invertY = d.invertY; },
        (s, d) => { s.reduceFlashes = d.reduceFlashes; s.cameraShake = d.cameraShake; },
    };

    private readonly VisualElement root;
    private readonly VisualElement panel;
    private readonly VisualElement calibration;
    private readonly Action onBack;
    private readonly List<Button> tabs = new List<Button>();
    private readonly List<VisualElement> panes = new List<VisualElement>();
    private readonly List<(Slider slider, Label label, Func<float, string> format)> valueLabels = new List<(Slider, Label, Func<float, string>)>();
    private readonly ScrollView paneHost;
    private readonly Label description;

    private Slider masterSlider, musicSlider, sfxSlider, voiceSlider;
    private DropdownField resolutionDropdown, windowDropdown, qualityDropdown, languageDropdown;
    private Slider fovSlider, brightnessSlider, sensitivitySlider, subtitleSizeSlider, cameraShakeSlider, calibrationSlider;
    private OnOffField subtitlesToggle, invertYToggle, reduceFlashesToggle;

    private List<Resolution> resolutions = new List<Resolution>();
    private int currentTab;
    private string descriptionKey;
    private bool usingGamepad;
    private Action calibrationClosed;
    private int lastBackFrame = -1;

    public bool IsVisible => !root.ClassListContains("hidden");
    public bool IsCalibrating => !calibration.ClassListContains("hidden");

    public OptionsPanel(VisualElement root, Action onBack)
    {
        this.root = root;
        this.onBack = onBack;
        panel = root.Q("options-panel");
        calibration = root.Q("calibration");
        paneHost = root.Q<ScrollView>("pane-host");
        description = root.Q<Label>("description");

        foreach (string tabName in TabNames)
        {
            Button tab = root.Q<Button>($"tab-{tabName}");
            int index = tabs.Count;
            tab.clicked += () => SelectTab(index, true);
            tabs.Add(tab);
            panes.Add(root.Q($"pane-{tabName}"));
        }
        root.Q<Button>("tab-previous").clicked += () => StepTab(-1);
        root.Q<Button>("tab-next").clicked += () => StepTab(1);

        BindControls();
        BindDescriptions();

        root.Q<Button>("button-defaults").clicked += ResetTab;
        root.Q<Button>("button-back").clicked += () => Back();
        root.Q<Button>("button-calibrate").clicked += () => ShowCalibration(null);
        root.Q<Button>("button-calibration-done").clicked += CloseCalibration;

        MenuDecor.Apply(root);
        SetGlyphs(Gamepad.current != null && Keyboard.current == null);
    }

    public void Show()
    {
        root.RemoveFromClassList("hidden");
        panel.RemoveFromClassList("hidden");
        calibration.AddToClassList("hidden");
        Refresh();
        SelectTab(0, true);
    }

    public void Hide() => root.AddToClassList("hidden");

    /// <summary>
    /// Volta um passo: fecha a calibração, se estiver aberta; senão, sai das opções.
    /// </summary>
    public void Back()
    {
        // Esc pode chegar duas vezes no mesmo quadro (ação de pausa e navegação do UI Toolkit)
        if (Time.frameCount == lastBackFrame) return;
        lastBackFrame = Time.frameCount;
        if (IsCalibrating) CloseCalibration();
        else onBack();
    }

    /// <summary>
    /// Teclas que o UI Toolkit não entrega como navegação: Q/E e LB/RB trocam de aba, R/Y restaura
    /// a aba. Também troca os desenhos das teclas entre teclado e controle.
    /// </summary>
    public void Tick()
    {
        if (!IsVisible) return;

        Gamepad pad = Gamepad.current;
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (pad != null && !usingGamepad && PadButtonPressed(pad)) SetGlyphs(true);
        else if (usingGamepad && ((keyboard != null && keyboard.anyKey.wasPressedThisFrame) || (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 4f))) SetGlyphs(false);

        if (IsCalibrating) return;

        if ((keyboard != null && keyboard.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) StepTab(-1);
        if ((keyboard != null && keyboard.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) StepTab(1);
        if ((keyboard != null && keyboard.rKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame)) ResetTab();
    }

    private static bool PadButtonPressed(Gamepad pad) =>
        pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
        pad.buttonWest.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
        pad.dpad.ReadValue().sqrMagnitude > 0.5f || pad.leftStick.ReadValue().sqrMagnitude > 0.5f;

    private void SetGlyphs(bool gamepad)
    {
        usingGamepad = gamepad;
        Glyph("tab-previous", gamepad ? "LB" : "Q");
        Glyph("tab-next", gamepad ? "RB" : "E");
        Glyph("glyph-change", gamepad ? "A" : "Enter");
        Glyph("glyph-defaults", gamepad ? "Y" : "R");
        Glyph("glyph-back", gamepad ? "B" : "Esc");
        Glyph("glyph-adjust", gamepad ? "◀ ▶" : "← →");
        Glyph("glyph-confirm", gamepad ? "A" : "Enter");
    }

    private void Glyph(string name, string text)
    {
        TextElement element = root.Q<TextElement>(name);
        if (element != null) element.text = text;
    }

    // ---------- Abas ----------

    private void StepTab(int direction) => SelectTab((currentTab + direction + tabs.Count) % tabs.Count, true);

    private void SelectTab(int index, bool focusFirst)
    {
        currentTab = index;
        for (int i = 0; i < tabs.Count; i++)
        {
            tabs[i].EnableInClassList("tab--selected", i == index);
            panes[i].EnableInClassList("pane--visible", i == index);
        }
        paneHost.scrollOffset = Vector2.zero;
        if (!focusFirst) return;

        VisualElement first = panes[index].Query(className: "option").Where(e => e.resolvedStyle.display != DisplayStyle.None).First();
        if (first != null) root.schedule.Execute(() => first.Focus());
        ShowDescription(first != null ? first.name : null);
    }

    private void ResetTab()
    {
        var defaults = new GameOptionsData();
        GameOptions.Change(s => TabDefaults[currentTab](s, defaults));
        Refresh();
    }

    // ---------- Descrição ----------

    private void BindDescriptions()
    {
        foreach (string name in Descriptions.Keys)
        {
            VisualElement element = root.Q(name);
            if (element == null) continue;
            element.RegisterCallback<FocusInEvent>(_ =>
            {
                ShowDescription(name);
                paneHost.ScrollTo(element);
            });
            element.RegisterCallback<PointerEnterEvent>(_ => ShowDescription(name));
        }
    }

    private void ShowDescription(string elementName)
    {
        descriptionKey = elementName != null && Descriptions.TryGetValue(elementName, out string key) ? key : null;
        description.text = descriptionKey != null ? MenuText.Get(descriptionKey) : string.Empty;
        description.parent.style.visibility = descriptionKey != null ? Visibility.Visible : Visibility.Hidden;
    }

    // ---------- Controles ----------

    private void BindControls()
    {
        string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
        string Whole(float v) => Mathf.RoundToInt(v * 100f).ToString(CultureInfo.InvariantCulture);

        masterSlider = BindSlider("slider-master", 0.05f, Whole, (s, v) => s.masterVolume = v);
        musicSlider = BindSlider("slider-music", 0.05f, Whole, (s, v) => s.musicVolume = v);
        sfxSlider = BindSlider("slider-sfx", 0.05f, Whole, (s, v) => s.sfxVolume = v);
        voiceSlider = BindSlider("slider-voice", 0.05f, Whole, (s, v) => s.voiceVolume = v);

        fovSlider = BindSlider("slider-fov", 1f, v => Mathf.RoundToInt(v) + "°", (s, v) => s.fieldOfView = Mathf.Round(v));
        brightnessSlider = BindSlider("slider-brightness", 0.05f, Signed, (s, v) => s.brightness = v);
        sensitivitySlider = BindSlider("slider-sensitivity", 0.05f, v => v.ToString("0.00", CultureInfo.InvariantCulture), (s, v) => s.mouseSensitivity = v);
        subtitleSizeSlider = BindSlider("slider-subtitle-size", 0.05f, Percent, (s, v) => s.subtitleScale = v);
        cameraShakeSlider = BindSlider("slider-camera-shake", 0.1f, Percent, (s, v) => s.cameraShake = v);

        subtitlesToggle = BindToggle("toggle-subtitles", (s, v) => s.subtitles = v);
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

        // Calibração: o mesmo brilho, com o carimbo como referência
        calibrationSlider = root.Q<Slider>("slider-calibration");
        StepWithArrows(calibrationSlider, 0.05f);
        calibrationSlider.RegisterValueChangedCallback(evt =>
        {
            GameOptions.Change(s => s.brightness = evt.newValue);
            UpdateCalibration();
        });
        calibrationSlider.RegisterCallback<NavigationSubmitEvent>(evt => { CloseCalibration(); evt.StopPropagation(); });
    }

    private static string Signed(float v)
    {
        int n = Mathf.RoundToInt(v * 10f);
        return n > 0 ? "+" + n : n.ToString(CultureInfo.InvariantCulture);
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

    private Slider BindSlider(string name, float step, Func<float, string> format, Action<GameOptionsData, float> apply)
    {
        Slider slider = root.Q<Slider>(name);
        var label = new Label { pickingMode = PickingMode.Ignore };
        label.AddToClassList("slider-value");
        slider.Add(label);
        valueLabels.Add((slider, label, format));

        StepWithArrows(slider, step);
        slider.RegisterValueChangedCallback(evt =>
        {
            label.text = format(evt.newValue);
            GameOptions.Change(s => apply(s, evt.newValue));
        });
        return slider;
    }

    // Esquerda/direita andam um passo de tamanho próprio (o padrão do Slider é 1% da faixa)
    private static void StepWithArrows(Slider slider, float step)
    {
        slider.RegisterCallback<NavigationMoveEvent>(evt =>
        {
            float direction = evt.direction == NavigationMoveEvent.Direction.Left ? -1f
                : evt.direction == NavigationMoveEvent.Direction.Right ? 1f : 0f;
            if (direction == 0f) return;
            slider.value = Mathf.Clamp(Mathf.Round((slider.value + direction * step) / step) * step, slider.lowValue, slider.highValue);
            evt.StopImmediatePropagation();
            slider.focusController?.IgnoreEvent(evt);
        }, TrickleDown.TrickleDown);
    }

    private OnOffField BindToggle(string name, Action<GameOptionsData, bool> apply)
    {
        OnOffField toggle = root.Q<OnOffField>(name);
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
        subtitlesToggle.SetValueWithoutNotify(options.subtitles);
        invertYToggle.SetValueWithoutNotify(options.invertY);
        reduceFlashesToggle.SetValueWithoutNotify(options.reduceFlashes);
        foreach (var (slider, label, format) in valueLabels) label.text = format(slider.value);

        RefreshResolutions(options);
        RefreshWindowModes(options);
        RefreshQuality(options);
        RefreshLanguages();

        if (descriptionKey != null) description.text = MenuText.Get(descriptionKey);
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

    // ---------- Calibração de brilho ----------

    /// <summary>
    /// Abre a tela do carimbo. Pode abrir sozinha (primeira vez no menu, com <paramref name="onClosed"/>)
    /// ou de dentro das opções (Vídeo > Calibrar brilho), quando volta para a lista ao fechar.
    /// </summary>
    public void ShowCalibration(Action onClosed)
    {
        calibrationClosed = onClosed;
        root.RemoveFromClassList("hidden");
        panel.EnableInClassList("hidden", onClosed != null);
        calibration.RemoveFromClassList("hidden");

        calibrationSlider.SetValueWithoutNotify(GameOptions.Current.brightness);
        UpdateCalibration();
        root.schedule.Execute(() => calibrationSlider.Focus());
    }

    private void CloseCalibration()
    {
        if (!IsCalibrating) return;
        GameOptions.Change(s => s.brightnessCalibrated = true);
        calibration.AddToClassList("hidden");

        if (calibrationClosed != null)
        {
            Action done = calibrationClosed;
            calibrationClosed = null;
            Hide();
            done();
        }
        else
        {
            Refresh();
            Button calibrate = root.Q<Button>("button-calibrate");
            root.schedule.Execute(() => calibrate.Focus());
        }
    }

    // O brilho do jogo só mexe na câmera 3D; a interface não é afetada. Então o carimbo imita a
    // mesma curva de gamma: no padrão fica quase sumindo no preto, mais claro ele aparece bem.
    private void UpdateCalibration()
    {
        float brightness = GameOptions.Current.brightness;
        float level = Mathf.Pow(0.16f, 1f / Mathf.Max(0.1f, 1f + 0.6f * brightness));
        root.Q("calibration-stamp").style.unityBackgroundImageTintColor = new Color(level, level, level);
    }
}
