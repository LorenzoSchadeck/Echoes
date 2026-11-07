using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Monta a tela de carregamento (menu Echoes > Menu > Criar tela de carregamento): o carimbo da
/// delegacia mais claro (o original é escuro demais sobre o preto), as configurações do painel por cima
/// de tudo e o prefab Resources/Loading/SceneLoadingScreen, usado por <see cref="SceneTransition"/>.
/// Também grava a palavra "Carregando" na tabela do menu. Pode rodar de novo.
/// </summary>
public static class LoadingScreenSetupTools
{
    private const string Folder = "Assets/Resources/Loading";
    private const string StampSource = "Assets/Textures/carimbo.png";
    private const string StampPath = Folder + "/carimbo_carregando.png";
    private const string PanelPath = Folder + "/LoadingPanelSettings.asset";
    private const string PrefabPath = Folder + "/SceneLoadingScreen.prefab";
    private const string MenuPanelPath = "Assets/UI/Menu/EchoesPanelSettings.asset";
    private const string FontPath = "Assets/Fonts/B/atwriter.ttf";

    [MenuItem("Echoes/Menu/Criar tela de carregamento")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Loading");

        BrightenStamp();

        // Painel por cima de tudo (menu 2000, mural, HUD e fades de morte ficam abaixo)
        var menuPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(MenuPanelPath);
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panel, PanelPath);
        }
        panel.themeStyleSheet = menuPanel != null ? menuPanel.themeStyleSheet : panel.themeStyleSheet;
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1920, 1080);
        panel.match = 0.5f;
        panel.sortingOrder = 10000;
        EditorUtility.SetDirty(panel);

        // Prefab
        var root = new GameObject("SceneLoadingScreen");
        var document = root.AddComponent<UIDocument>();
        document.panelSettings = panel;
        var screen = root.AddComponent<SceneLoadingScreen>();
        var data = new SerializedObject(screen);
        data.FindProperty("stamp").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(StampPath);
        data.FindProperty("labelFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        Debug.Log("[LoadingScreenSetupTools] Tela de carregamento criada em " + Folder);
    }

    // O carimbo original é cinza muito escuro (feito para a calibração de brilho). Para a tela de
    // carregamento ele ganha luz, mantendo o alfa e o contraste entre o fundo e as letras.
    private static void BrightenStamp()
    {
        string source = Path.GetFullPath(StampSource);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(source));
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            float r = Mathf.Clamp01(c.r / 255f * 4.6f);
            float g = Mathf.Clamp01(c.g / 255f * 4.6f);
            float b = Mathf.Clamp01(c.b / 255f * 4.5f);
            pixels[i] = new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), c.a);
        }
        texture.SetPixels32(pixels);
        File.WriteAllBytes(Path.GetFullPath(StampPath), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(StampPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(StampPath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }
}
