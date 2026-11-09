#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

// Opt-in export harness only. StudioIntro, scene, timing and artwork stay unchanged.
public class TinkerCrowNativeCapture : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Marketing/TinkerCrow/Native"));
    RenderTexture target;
    PanelSettings panel;
    byte[] lastClean;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (!SessionState.GetBool("TinkerCrow.NativeCapture", false)) return;
        Application.runInBackground = true;
        Application.targetFrameRate = 30;
        // Record the actual game's mixer during the real-time screen capture.
        var settings = FMODUnity.Settings.Instance;
        var platform = (FMODUnity.Platform)typeof(FMODUnity.Settings).GetMethod("FindCurrentPlatform", Private).Invoke(settings, null);
        var properties = (FMODUnity.Platform.PropertyStorage)typeof(FMODUnity.Platform).GetField("Properties", Private).GetValue(platform);
        properties.CallbackHandler.Value = ScriptableObject.CreateInstance<TinkerCrowCaptureAudio>();
        properties.CallbackHandler.HasValue = true;
        Directory.CreateDirectory(Output);
        var go = new GameObject("Native intro capture (export only)");
        DontDestroyOnLoad(go);
        go.AddComponent<TinkerCrowNativeCapture>();
    }

    IEnumerator Start()
    {
        var intro = FindFirstObjectByType<StudioIntro>();
        if (intro == null) throw new Exception("StudioIntro not found in original PreLoad scene");
        var doc = intro.GetComponent<UIDocument>();
        panel = doc.panelSettings;
        target = new RenderTexture(3840,2160,24,RenderTextureFormat.ARGB32);
        target.Create();
        panel.targetTexture = target;
        var read = new Texture2D(3840,2160,TextureFormat.RGBA32,false);
        var encoder=new System.Diagnostics.Process();
        encoder.StartInfo=new System.Diagnostics.ProcessStartInfo
        {
            FileName=Path.GetFullPath(Path.Combine(Application.dataPath,"../../tmp/tinker-crow-tools/ffmpeg.exe")),
            Arguments="-hide_banner -loglevel error -y -f rawvideo -pix_fmt rgba -s 3840x2160 -r 30 -i pipe:0 -vf vflip -c:v libx264 -preset ultrafast -crf 0 -pix_fmt yuv444p \""+Path.Combine(Output,"native-frames.mkv")+"\"",
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true
        };
        encoder.Start();
        var queue=new BlockingCollection<byte[]>();
        var writer=Task.Run(() => { foreach(var data in queue.GetConsumingEnumerable()) encoder.StandardInput.BaseStream.Write(data,0,data.Length); encoder.StandardInput.Close(); encoder.WaitForExit(); });
        var times=new System.Collections.Generic.List<double>();
        double began=Time.realtimeSinceStartupAsDouble;
        var blackField = typeof(StudioIntro).GetField("black", Private);
        var titleField = typeof(StudioIntro).GetField("title", Private);
        var underlineField = typeof(StudioIntro).GetField("underline", Private);
        var phaseField = typeof(StudioIntro).GetField("phase", Private);
        var finishedField = typeof(StudioIntro).GetField("finished", Private);
        int frame=0;
        int pending=0;
        while (frame<1000)
        {
            yield return new WaitForEndOfFrame();
            if ((bool)finishedField.GetValue(intro)) break;
            times.Add(Time.realtimeSinceStartupAsDouble-began);
            var black = (VisualElement)blackField.GetValue(intro);
            var title = (Label)titleField.GetValue(intro);
            var underline = (VisualElement)underlineField.GetValue(intro);
            bool clean=title.text=="TINKER CROW" && black.resolvedStyle.opacity<.001f && underline.resolvedStyle.width>400f && phaseField.GetValue(intro).ToString()=="Idle";
            pending++;
            AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32, request =>
            {
                if (request.hasError) Debug.LogError("Native capture GPU readback failed");
                else
                {
                    var bytes=request.GetData<byte>().ToArray();
                    queue.Add(bytes);
                    if(clean) lastClean=bytes;
                }
                pending--;
            });
            if (frame%30==0) Debug.Log($"[NativeCapture] frame={frame} phase={phaseField.GetValue(intro)} size={doc.rootVisualElement.layout.size}");
            frame++;
        }
        while(pending>0) yield return null;
        if (lastClean==null) throw new Exception("No fully visible final title frame captured");
        queue.CompleteAdding();
        while (!writer.IsCompleted) yield return null;
        read.LoadRawTextureData(lastClean); read.Apply();
        File.WriteAllBytes(Path.Combine(Output,"tinker-crow-frame-final-4k.png"),read.EncodeToPNG());
        File.WriteAllText(Path.Combine(Output,"capture.json"),$"{{\"frames\":{frame},\"fps\":30,\"width\":3840,\"height\":2160,\"source\":\"PreLoad.unity / StudioIntro unmodified\",\"timestamps\":[{string.Join(",",times.ConvertAll(x=>x.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)))}]}}");
        panel.targetTexture=null;
        Time.captureFramerate=0;
        SessionState.SetBool("TinkerCrow.NativeCapture", false);
        Debug.Log("[NativeCapture] COMPLETE "+Output);
        EditorApplication.isPlaying=false;
    }

}

public class TinkerCrowCaptureAudio : FMODUnity.PlatformCallbackHandler
{
    public override void PreInitialize(FMOD.Studio.System system, Action<FMOD.RESULT,string> reportResult)
    {
        system.getCoreSystem(out var core);
        reportResult(core.setOutput(FMOD.OUTPUTTYPE.WAVWRITER), "Capture WAV output");
        reportResult(core.setSoftwareFormat(48000, FMOD.SPEAKERMODE.STEREO,0), "Capture sample rate");
        reportResult(core.setDSPBufferSize(512,4), "Capture buffer");
    }
}
#endif
