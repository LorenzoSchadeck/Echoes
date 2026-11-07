using System.Linq;
using FMODUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Monta o Tinker Crow (mascote e acessório de Echoes) a partir dos desenhos em Assets/Textures/Intro
/// e coloca a abertura do estúdio na cena PreLoad, antes dos avisos. Pode rodar de novo.
/// </summary>
public static class MascotSetupTools
{
    private const string TextureFolder = "Assets/Textures/Intro";
    private const string DataFolder = "Assets/Settings/TinkerCrow";
    private const string MascotPath = DataFolder + "/Tinker Crow.asset";
    private const string AccessoryPath = DataFolder + "/Acessorio Echoes - chave do quarto 5.asset";
    private const string PreLoadPath = "Assets/Scenes/PreLoad.unity";

    // Pontos medidos nos desenhos: bico aberto e altura do fio desenhado em cada imagem
    private static readonly (string file, Vector2 beak, float wire)[] Frames =
    {
        ("corvo_chegando_1", new Vector2(0.810f, 0.400f), 0.821f),
        ("corvo_chegando_2", new Vector2(0.846f, 0.336f), 0.840f),
        ("corvo_chegando_3", new Vector2(0.881f, 0.387f), 0.787f),
        ("corvo_pousado_1", new Vector2(0.928f, 0.484f), 0.755f),
        ("corvo_pousado_2", new Vector2(0.836f, 0.133f), 0.774f),
        ("corvo_pousado_3", new Vector2(0.861f, 0.113f), 0.752f),
    };

    [MenuItem("Echoes/Abertura do estúdio/Montar Tinker Crow e abertura")]
    private static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[MascotSetupTools] Saia do play mode."); return; }

        foreach (var frame in Frames) MakeSprite($"{TextureFolder}/{frame.file}.png");
        MakeSprite($"{TextureFolder}/red_key.png");

        if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/Settings", "TinkerCrow");

        var mascot = AssetDatabase.LoadAssetAtPath<CrowMascot>(MascotPath);
        if (mascot == null) { mascot = ScriptableObject.CreateInstance<CrowMascot>(); AssetDatabase.CreateAsset(mascot, MascotPath); }
        MascotFrame F(int i) => new MascotFrame
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TextureFolder}/{Frames[i].file}.png"),
            beak = Frames[i].beak,
            wire = Frames[i].wire,
        };
        mascot.arrive = new MascotClip { name = "Chegando", fps = 9f, loop = true, frames = new[] { F(0), F(1) } };
        mascot.land = new MascotClip { name = "Pousando", fps = 7f, loop = false, frames = new[] { F(2), F(3) } };
        // Parado: um desenho só; alternar os dois desenhos de pouso mexia o corvo demais
        mascot.idle = new MascotClip { name = "Parado", fps = 6f, loop = true, frames = new[] { F(5) } };
        EditorUtility.SetDirty(mascot);

        var accessory = AssetDatabase.LoadAssetAtPath<MascotAccessory>(AccessoryPath);
        if (accessory == null) { accessory = ScriptableObject.CreateInstance<MascotAccessory>(); AssetDatabase.CreateAsset(accessory, AccessoryPath); }
        accessory.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TextureFolder}/red_key.png");
        accessory.slot = MascotAccessory.Slot.Beak;
        accessory.attachPoint = new Vector2(0.555f, 0.075f);
        accessory.size = 0.2f;
        accessory.swings = true;
        EditorUtility.SetDirty(accessory);
        AssetDatabase.SaveAssets();

        // ---------- Cena PreLoad ----------
        Scene current = SceneManager.GetActiveScene();
        bool openedHere = current.path != PreLoadPath;
        if (openedHere && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = openedHere ? EditorSceneManager.OpenScene(PreLoadPath, OpenSceneMode.Additive) : current;

        GameObject old = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Abertura_TinkerCrow");
        if (old != null) Object.DestroyImmediate(old);

        var warnings = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<Echoes.UI.WarningScreen.WarningScreenManager>(true)).FirstOrDefault(w => w != null);
        var warningDoc = warnings != null ? warnings.GetComponent<UIDocument>() : null;

        var introObject = new GameObject("Abertura_TinkerCrow");
        SceneManager.MoveGameObjectToScene(introObject, scene);
        introObject.transform.SetAsFirstSibling();
        var document = introObject.AddComponent<UIDocument>();
        document.panelSettings = warningDoc != null ? warningDoc.panelSettings : null;
        document.sortingOrder = 100; // por cima dos avisos
        var intro = introObject.AddComponent<StudioIntro>();

        if (warnings != null) warnings.enabled = false; // os avisos começam quando o corvo sai
        intro.EditorSetup(mascot, accessory, AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/B/atwriter.ttf"), warnings,
            Sound("event:/Menu/Typewritter"));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);

        // O splash (Unity e FMOD) é configurado em Echoes > Abertura do estúdio > Configurar splash

        Debug.Log("[MascotSetupTools] Tinker Crow montado e abertura colocada na cena PreLoad.");
    }

    private static void MakeSprite(string path)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning("[MascotSetupTools] Falta " + path); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }

    private static EventReference Sound(string path)
    {
        var found = EventManager.Events.FirstOrDefault(e => string.Equals(e.Path, path, System.StringComparison.OrdinalIgnoreCase));
        return found == null ? default : new EventReference { Path = found.Path, Guid = found.Guid };
    }
}
