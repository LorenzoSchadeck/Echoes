using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build do jogo para Windows (menu Echoes > Build). Usa as cenas ligadas em Build Settings, gera em
/// Downloads\Echoes_Build e grava o relatório (tempo de cada etapa, tamanho e avisos) ao lado do jogo,
/// em build_relatorio.txt, para dar para medir o que pesa na build.
/// </summary>
public static class BuildTools
{
    private static string OutputFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Echoes_Build");

    [MenuItem("Echoes/Build/Windows (Release, em Downloads)")]
    public static void BuildWindows()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[BuildTools] Saia do play mode."); return; }
        if (building) return; // um pedido repetido (por exemplo, agendado duas vezes) não inicia outra build
        building = true;
        try { BuildWindowsNow(); }
        finally { building = false; }
    }

    private static bool building;

    private static void BuildWindowsNow()
    {

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Directory.CreateDirectory(OutputFolder);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(OutputFolder, "Echoes.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        DateTime started = DateTime.Now;
        BuildReport report = BuildPipeline.BuildPlayer(options);
        WriteReport(report, started);
    }

    private static void WriteReport(BuildReport report, DateTime started)
    {
        BuildSummary summary = report.summary;
        var text = new StringBuilder();
        text.AppendLine($"Resultado: {summary.result}");
        text.AppendLine($"Início: {started:HH:mm:ss}  Fim: {DateTime.Now:HH:mm:ss}");
        text.AppendLine($"Tempo total da build: {summary.totalTime}");
        text.AppendLine($"Tamanho total: {summary.totalSize / 1048576f:F0} MB");
        text.AppendLine($"APIs gráficas: {string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64))}");
        text.AppendLine($"Erros: {summary.totalErrors}  Avisos: {summary.totalWarnings}");
        text.AppendLine();
        text.AppendLine("Etapas (maiores primeiro):");
        foreach (BuildStep step in report.steps.OrderByDescending(s => s.duration))
        {
            if (step.duration.TotalSeconds < 1) continue;
            text.AppendLine($"  {step.duration.TotalSeconds,8:F1} s  {new string(' ', step.depth * 2)}{step.name}");
        }

        text.AppendLine();
        text.AppendLine("Maiores arquivos no jogo:");
        foreach (var file in report.GetFiles().OrderByDescending(f => f.size).Take(25))
            text.AppendLine($"  {file.size / 1048576f,8:F1} MB  {file.path}");

        string path = Path.Combine(OutputFolder, "build_relatorio.txt");
        File.WriteAllText(path, text.ToString());
        Debug.Log($"[BuildTools] Build {summary.result} em {summary.totalTime}. Relatório: {path}");
    }
}
