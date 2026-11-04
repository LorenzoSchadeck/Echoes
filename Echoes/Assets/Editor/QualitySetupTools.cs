using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Monta os três níveis de qualidade do jogo (Baixa, Média, Alta), cada um com seu URP Asset,
/// e deixa a iluminação baked pronta para PC fraco. Pode rodar de novo: só reaplica os valores.
/// </summary>
public static class QualitySetupTools
{
    private const string SettingsFolder = "Assets/Settings";
    private const string HighAssetPath = SettingsFolder + "/PC_RPAsset.asset";
    private const string MediumAssetPath = SettingsFolder + "/Medium_RPAsset.asset";
    private const string LowAssetPath = SettingsFolder + "/Low_RPAsset.asset";

    // Nomes internos dos níveis; o menu mostra o texto localizado de M_QUALITY_<índice>
    public static readonly string[] LevelNames = { "Low", "Medium", "High" };

    private static readonly (string key, string pt, string en)[] MenuLines =
    {
        ("M_QUALITY_0", "Baixa", "Low"),
        ("M_QUALITY_1", "Média", "Medium"),
        ("M_QUALITY_2", "Alta", "High"),
    };

    private struct Level
    {
        public string asset;
        public float renderScale;
        public int upscaling;              // 0 Auto, 1 Linear, 2 Point, 3 FSR, 4 STP
        public bool additionalShadows;
        public int additionalShadowRes;
        public int softShadowQuality;      // 1 baixa, 2 média, 3 alta
        public float shadowDistance;
        public int cookieRes;
        public bool probeBlending;
        public int shadowmaskMode;         // 0 Shadowmask (estáticos só baked), 1 Distance Shadowmask
        public int textureMipLimit;        // 0 resolução cheia, 1 metade
        public float lodBias;
    }

    private static readonly Level[] Levels =
    {
        new Level { asset = LowAssetPath, renderScale = 0.75f, upscaling = 3, additionalShadows = false, additionalShadowRes = 512,
            softShadowQuality = 1, shadowDistance = 15f, cookieRes = 512, probeBlending = false, shadowmaskMode = 0, textureMipLimit = 1, lodBias = 0.7f },
        new Level { asset = MediumAssetPath, renderScale = 1f, upscaling = 0, additionalShadows = true, additionalShadowRes = 1024,
            softShadowQuality = 1, shadowDistance = 20f, cookieRes = 1024, probeBlending = true, shadowmaskMode = 0, textureMipLimit = 0, lodBias = 1f },
        new Level { asset = HighAssetPath, renderScale = 1f, upscaling = 0, additionalShadows = true, additionalShadowRes = 2048,
            softShadowQuality = 2, shadowDistance = 30f, cookieRes = 2048, probeBlending = true, shadowmaskMode = 1, textureMipLimit = 0, lodBias = 1.5f },
    };

    [MenuItem("Echoes/Qualidade/Configurar níveis de qualidade")]
    private static void SetupLevels()
    {
        LocalizationSetup.AddStrings("menu", MenuLines);

        var assets = new UniversalRenderPipelineAsset[Levels.Length];
        for (int i = 0; i < Levels.Length; i++)
        {
            assets[i] = GetOrCopyAsset(Levels[i].asset);
            ConfigureAsset(assets[i], Levels[i]);
        }

        ConfigureQualityLevels(assets);
        AssetDatabase.SaveAssets();
        Debug.Log("[QualitySetupTools] Níveis Baixa, Média e Alta configurados.");
    }

    [MenuItem("Echoes/Qualidade/Preparar iluminação para PC fraco")]
    private static void SetupLighting()
    {
        LightingSettings settings = Lightmapping.lightingSettings;
        if (settings == null)
        {
            Debug.LogError("[QualitySetupTools] A cena não tem Lighting Settings.");
            return;
        }

        Undo.RecordObject(settings, "Preparar iluminação");
        // GI em tempo real (Enlighten) custa CPU a cada frame; a casa é estática, o baked basta
        settings.realtimeGI = false;
        settings.bakedGI = true;
        settings.mixedBakeMode = MixedLightingMode.Shadowmask;
        // 40 texels por metro gerava 35 lightmaps 2K (~1,4 GB). 12 cobre um apartamento sem borrar
        settings.lightmapResolution = 12f;
        settings.lightmapMaxSize = 2048;
        // Sem mapas direcionais: metade da memória dos lightmaps
        settings.directionalityMode = LightmapsMode.NonDirectional;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[QualitySetupTools] Iluminação preparada. Rode o bake em Window > Rendering > Lighting.");
    }

    private static UniversalRenderPipelineAsset GetOrCopyAsset(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (asset != null) return asset;

        AssetDatabase.CopyAsset(HighAssetPath, path);
        return AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
    }

    private static void ConfigureAsset(UniversalRenderPipelineAsset asset, Level level)
    {
        var data = new SerializedObject(asset);
        data.FindProperty("m_RenderScale").floatValue = level.renderScale;
        data.FindProperty("m_UpscalingFilter").intValue = level.upscaling;
        data.FindProperty("m_MSAA").intValue = 1;
        data.FindProperty("m_AdditionalLightShadowsSupported").boolValue = level.additionalShadows;
        data.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = level.additionalShadowRes;
        data.FindProperty("m_SoftShadowQuality").intValue = level.softShadowQuality;
        data.FindProperty("m_ShadowDistance").floatValue = level.shadowDistance;
        data.FindProperty("m_AdditionalLightsCookieResolution").intValue = level.cookieRes;
        data.FindProperty("m_ReflectionProbeBlending").boolValue = level.probeBlending;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    private static void ConfigureQualityLevels(UniversalRenderPipelineAsset[] assets)
    {
        var qualityAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
        var data = new SerializedObject(qualityAsset);
        SerializedProperty list = data.FindProperty("m_QualitySettings");
        list.arraySize = Levels.Length;

        for (int i = 0; i < Levels.Length; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("name").stringValue = LevelNames[i];
            entry.FindPropertyRelative("customRenderPipeline").objectReferenceValue = assets[i];
            entry.FindPropertyRelative("shadowmaskMode").intValue = Levels[i].shadowmaskMode;
            entry.FindPropertyRelative("globalTextureMipmapLimit").intValue = Levels[i].textureMipLimit;
            entry.FindPropertyRelative("lodBias").floatValue = Levels[i].lodBias;
            entry.FindPropertyRelative("realtimeReflectionProbes").boolValue = false;
            entry.FindPropertyRelative("vSyncCount").intValue = 1;
            entry.FindPropertyRelative("excludedTargetPlatforms").arraySize = 0;
        }

        // Padrão do PC é Média; o jogo troca na primeira execução conforme a placa de vídeo
        data.FindProperty("m_CurrentQuality").intValue = 1;
        data.ApplyModifiedPropertiesWithoutUndo();
        QualitySettings.SetQualityLevel(1, true);
    }
}
