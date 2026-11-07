using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura a splash screen do jogo (menu Echoes > Abertura do estúdio > Configurar splash): a da Unity
/// e o logo do FMOD, que a licença do FMOD pede, uma depois da outra, sobre o fundo escuro. Em seguida vem
/// a abertura do Tinker Crow (cena PreLoad). Pode rodar de novo.
/// O logo do FMOD (Assets/Textures/Splash/FMOD_Logo_White.png) já traz a legenda exigida pela licença:
/// "Made using FMOD Studio by Firelight Technologies Pty Ltd." Se trocar a imagem, mantenha a legenda.
/// </summary>
public static class SplashSetupTools
{
    private const string Folder = "Assets/Textures/Splash";
    private const string FmodLogoPath = Folder + "/FMOD_Logo_White.png";
    private const float LogoSeconds = 2f; // a Unity não aceita menos de 2 s por logo

    [MenuItem("Echoes/Abertura do estúdio/Configurar splash (Unity + FMOD)")]
    public static void Configure()
    {
        if (!File.Exists(FmodLogoPath)) { Debug.LogError("[SplashSetupTools] Falta " + FmodLogoPath + " (logo do FMOD com a legenda da licença)."); return; }
        AssetDatabase.ImportAsset(FmodLogoPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(FmodLogoPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        var fmodLogo = AssetDatabase.LoadAssetAtPath<Sprite>(FmodLogoPath);
        if (fmodLogo == null) { Debug.LogError("[SplashSetupTools] Não consegui carregar o sprite do FMOD."); return; }

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = true;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        PlayerSettings.SplashScreen.logos = new[]
        {
            PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(LogoSeconds),
            PlayerSettings.SplashScreenLogo.Create(LogoSeconds, fmodLogo),
        };

        AssetDatabase.SaveAssets();
        Debug.Log("[SplashSetupTools] Splash: Unity, depois FMOD, depois a abertura do Tinker Crow.");
    }
}
