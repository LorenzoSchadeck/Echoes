using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Uma fala da voz interna de Ethan: texto localizado (tabela "inner_voice"), voz opcional e duração.
/// </summary>
[Serializable]
public class InnerVoiceLine
{
    public LocalizedString text = new LocalizedString();
    [Tooltip("Voz gravada (2D). Opcional: sem ela, só a legenda aparece.")]
    public EventReference voice;
    [Tooltip("Segundos na tela.")]
    public float duration = 4f;
}

/// <summary>
/// Faz Ethan "pensar em voz alta": toca a voz (se houver) e mostra a legenda em itálico,
/// pela mesma fila de legendas do jogo.
/// </summary>
public static class InnerVoice
{
    public const string Table = "inner_voice";

    public static void Say(InnerVoiceLine line)
    {
        if (line == null || line.text == null || line.text.IsEmpty) return;

        string text = line.text.GetLocalizedString();
        if (string.IsNullOrEmpty(text)) return;

        if (!line.voice.IsNull) RuntimeManager.PlayOneShot(line.voice);
        SubtitleManager.Instance?.ShowThought(text, line.duration);
    }
}
