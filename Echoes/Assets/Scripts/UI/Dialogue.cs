using System;
using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

/// <summary>
/// Uma fala de diálogo: quem fala (tabela "speaker"), o texto (tabela "subtitles"), a voz gravada
/// (opcional) e quanto tempo fica na tela.
/// </summary>
[Serializable]
public class DialogueLine
{
    public LocalizedString speaker = new LocalizedString();
    public LocalizedString text = new LocalizedString();
    [Tooltip("Voz gravada (2D). Opcional: sem ela, só a legenda aparece.")]
    public EventReference voice;
    [Tooltip("Segundos na tela. Zero calcula pelo tamanho do texto.")]
    public float duration;

    public DialogueLine() { }

    public DialogueLine(string speakerKey, string textKey, float duration = 0f)
    {
        speaker = new LocalizedString("speaker", speakerKey);
        text = new LocalizedString("subtitles", textKey);
        this.duration = duration;
    }
}

/// <summary>
/// Toca uma sequência de falas com o nome de quem fala acima da legenda. Dentro de uma corrotina:
/// <c>yield return Dialogue.Play(lines);</c>. Uma conversa nova interrompe a anterior.
/// </summary>
public static class Dialogue
{
    public static bool IsPlaying { get; private set; }

    private static int currentConversation;

    public static IEnumerator Play(params DialogueLine[] lines)
    {
        int conversation = ++currentConversation;
        IsPlaying = true;

        foreach (DialogueLine line in lines)
        {
            if (line == null || line.text == null || line.text.IsEmpty) continue;

            string text = line.text.GetLocalizedString();
            string speaker = line.speaker != null && !line.speaker.IsEmpty ? line.speaker.GetLocalizedString() : string.Empty;

            // O rádio e as legendas soltas cedem a vez para a conversa
            SubtitleManager.Instance?.CancelCurrentSubtitle();
            GameHud.Speaker.text = speaker;
            GameHud.Speaker.enabled = !string.IsNullOrEmpty(speaker);
            GameHud.Subtitle.text = text;
            GameHud.Subtitle.enabled = true;
            if (!line.voice.IsNull) RuntimeManager.PlayOneShot(line.voice);

            float duration = line.duration > 0f ? line.duration : Mathf.Clamp(1.2f + text.Length * 0.06f, 2f, 7f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (conversation != currentConversation) yield break;
                yield return null;
            }
        }

        if (conversation != currentConversation) yield break;
        Hide();
    }

    /// <summary>Encerra a conversa em andamento e limpa a legenda.</summary>
    public static void Stop()
    {
        currentConversation++;
        Hide();
    }

    private static void Hide()
    {
        IsPlaying = false;
        GameHud.Speaker.enabled = false;
        GameHud.Subtitle.enabled = false;
        GameHud.Subtitle.text = string.Empty;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsPlaying = false;
        currentConversation = 0;

        // Uma conversa cortada pela troca de cena não pode deixar o jogo achando que ainda há fala
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        currentConversation++;
        IsPlaying = false;
    }
}
