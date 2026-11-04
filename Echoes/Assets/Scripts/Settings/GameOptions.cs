using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Configurações do jogador. Valores padrão aqui; persistidos em JSON nas PlayerPrefs.
/// </summary>
[Serializable]
public class GameOptionsData
{
    [Header("Áudio")]
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public float voiceVolume = 1f;

    [Header("Vídeo")]
    public int resolutionWidth;            // 0 = resolução atual da tela
    public int resolutionHeight;
    public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
    public int qualityLevel = -1;          // -1 = nível padrão do projeto
    public float fieldOfView = 70f;
    public float brightness;               // -1 a 1

    [Header("Controles")]
    public float mouseSensitivity = 1f;
    public bool invertY;

    [Header("Acessibilidade")]
    public float subtitleScale = 1f;
    public bool reduceFlashes;
    public float cameraShake = 1f;         // 0 = sem tremor, 1 = intensidade normal

    [Header("Idioma")]
    public string language = "";           // vazio = idioma escolhido pelo Localization
}

/// <summary>
/// Acesso global às configurações. Mudanças passam por <see cref="Change"/>, que aplica, salva e
/// avisa os sistemas (<see cref="Changed"/>). Áudio, vídeo, idioma e brilho são aplicados aqui;
/// sensibilidade, FOV, legendas, flashes e tremor são lidos pelos próprios sistemas.
/// </summary>
public static class GameOptions
{
    public const float MinSensitivity = 0.01f, MaxSensitivity = 3f;
    public const float MinFieldOfView = 60f, MaxFieldOfView = 80f;
    public const float MinSubtitleScale = 0.75f, MaxSubtitleScale = 1.5f;

    // Caminhos dos buses no projeto FMOD; um bus que não existir é ignorado (e sua opção some do menu)
    public const string MasterBus = "bus:/";
    public const string MusicBus = "bus:/Music";
    public const string SfxBus = "bus:/SFX";
    public const string VoiceBus = "bus:/Voice";

    private const string PrefsKey = "Echoes.Settings";

    private static GameOptionsData current;
    private static Volume brightnessVolume;

    public static event Action Changed;

    public static GameOptionsData Current
    {
        get
        {
            if (current == null) Load();
            return current;
        }
    }

    /// <summary>
    /// Altera configurações: <c>GameOptions.Change(s => s.masterVolume = 0.5f);</c>
    /// </summary>
    public static void Change(Action<GameOptionsData> change)
    {
        change(Current);
        Clamp(current);
        Save();
        ApplyGlobal();
        Changed?.Invoke();
    }

    public static void ResetToDefaults() => Change(s => JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new GameOptionsData()), s));

    public static bool AudioBusExists(string path)
    {
        return RuntimeManager.IsInitialized && RuntimeManager.StudioSystem.getBus(path, out _) == FMOD.RESULT.OK;
    }

    // Zera o estado estático (Enter Play Mode sem domain reload) antes de qualquer Awake
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        Changed = null;
        brightnessVolume = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (current == null) Load();
        ApplyGlobal();

        // O idioma só pode ser trocado depois que o Localization inicializa
        LocalizationSettings.InitializationOperation.Completed += _ => ApplyLanguage();
    }

    private static void Load()
    {
        current = new GameOptionsData();

        if (PlayerPrefs.HasKey(PrefsKey))
        {
            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), current);
        }
        else
        {
            // Migra as chaves do GameSettings antigo
            current.masterVolume = PlayerPrefs.GetFloat("MasterVolume", current.masterVolume);
            current.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", current.mouseSensitivity);
            current.language = PlayerPrefs.GetString("CurrentLanguage", current.language);
        }

        Clamp(current);
    }

    private static void Save()
    {
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(current));
        PlayerPrefs.Save();
    }

    private static void Clamp(GameOptionsData s)
    {
        s.masterVolume = Mathf.Clamp01(s.masterVolume);
        s.musicVolume = Mathf.Clamp01(s.musicVolume);
        s.sfxVolume = Mathf.Clamp01(s.sfxVolume);
        s.voiceVolume = Mathf.Clamp01(s.voiceVolume);
        s.fieldOfView = Mathf.Clamp(s.fieldOfView, MinFieldOfView, MaxFieldOfView);
        s.brightness = Mathf.Clamp(s.brightness, -1f, 1f);
        s.mouseSensitivity = Mathf.Clamp(s.mouseSensitivity, MinSensitivity, MaxSensitivity);
        s.subtitleScale = Mathf.Clamp(s.subtitleScale, MinSubtitleScale, MaxSubtitleScale);
        s.cameraShake = Mathf.Clamp01(s.cameraShake);
    }

    private static void ApplyGlobal()
    {
        ApplyAudio();
        ApplyDisplay();
        ApplyBrightness();
        if (LocalizationSettings.InitializationOperation.IsDone) ApplyLanguage();
    }

    private static void ApplyAudio()
    {
        SetBusVolume(MasterBus, current.masterVolume);
        SetBusVolume(MusicBus, current.musicVolume);
        SetBusVolume(SfxBus, current.sfxVolume);
        SetBusVolume(VoiceBus, current.voiceVolume);
    }

    private static void SetBusVolume(string path, float volume)
    {
        if (RuntimeManager.StudioSystem.getBus(path, out FMOD.Studio.Bus bus) == FMOD.RESULT.OK)
        {
            bus.setVolume(volume);
        }
    }

    private static void ApplyDisplay()
    {
        if (Application.isEditor) return; // No Editor a janela do Game view manda

        if (current.resolutionWidth > 0 && current.resolutionHeight > 0)
            Screen.SetResolution(current.resolutionWidth, current.resolutionHeight, current.fullScreenMode);
        else
            Screen.fullScreenMode = current.fullScreenMode;

        int quality = current.qualityLevel >= 0 ? current.qualityLevel : QualityForHardware();
        if (quality < QualitySettings.names.Length && quality != QualitySettings.GetQualityLevel())
            QualitySettings.SetQualityLevel(quality, true);
    }

    // Enquanto o jogador não escolhe, a qualidade segue a memória da placa de vídeo: Baixa, Média ou Alta
    private static int QualityForHardware()
    {
        int vram = SystemInfo.graphicsMemorySize;
        int level = vram < 2500 ? 0 : vram < 5000 ? 1 : 2;
        return Mathf.Min(level, QualitySettings.names.Length - 1);
    }

    // Brilho = gamma de um Volume global próprio, com prioridade acima dos volumes da cena
    private static void ApplyBrightness()
    {
        if (brightnessVolume == null)
        {
            var go = new GameObject("[Settings] Brightness");
            UnityEngine.Object.DontDestroyOnLoad(go);
            brightnessVolume = go.AddComponent<Volume>();
            brightnessVolume.isGlobal = true;
            brightnessVolume.priority = 1000f;
            brightnessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            brightnessVolume.profile.Add<LiftGammaGain>(true);
        }

        if (brightnessVolume.profile.TryGet(out LiftGammaGain liftGammaGain))
        {
            liftGammaGain.gamma.value = new Vector4(1f, 1f, 1f, current.brightness * 0.4f);
        }
    }

    private static void ApplyLanguage()
    {
        if (string.IsNullOrEmpty(current.language) || LocalizationSettings.AvailableLocales == null) return;

        var locale = LocalizationSettings.AvailableLocales.GetLocale(current.language);
        if (locale != null && LocalizationSettings.SelectedLocale != locale)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
    }
}
