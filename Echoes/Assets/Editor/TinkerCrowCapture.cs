#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class TinkerCrowCapture
{
    [InitializeOnLoadMethod]
    static void CheckRequest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("TinkerCrow.RestoreStartScene",false)) return;
            var path=SessionState.GetString("TinkerCrow.PreviousStartScene", "");
            EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.SetBool("TinkerCrow.RestoreStartScene",false);
        };
        EditorApplication.delayCall += () =>
        {
            var request=Path.GetFullPath(Path.Combine(Application.dataPath,"../../tmp/native-capture-request"));
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            Start();
        };
    }

    [MenuItem("Echoes/Exportar abertura original Tinker Crow")]
    public static void Start()
    {
        SessionState.SetBool("TinkerCrow.NativeCapture", true);
        var previous=EditorSceneManager.playModeStartScene;
        SessionState.SetString("TinkerCrow.PreviousStartScene", previous==null ? "" : AssetDatabase.GetAssetPath(previous));
        SessionState.SetBool("TinkerCrow.RestoreStartScene",true);
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/PreLoad.unity");
        var view = EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
        view.Show();
        view.Focus();
        EditorApplication.isPlaying = true;
    }
}
#endif
