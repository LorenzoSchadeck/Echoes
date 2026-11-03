using TMPro;

/// <summary>
/// Aplica a opção de tamanho das legendas a um texto, a partir do tamanho original dele.
/// </summary>
public static class SubtitleScaling
{
    public static void Apply(TMP_Text text, float baseFontSize)
    {
        if (text == null || baseFontSize <= 0f) return;

        float size = baseFontSize * GameOptions.Current.subtitleScale;
        if (text.enableAutoSizing)
            text.fontSizeMax = size;
        else
            text.fontSize = size;
    }

    /// <summary>
    /// Tamanho de referência do texto (o máximo, quando o auto-size está ligado).
    /// </summary>
    public static float BaseSize(TMP_Text text)
    {
        if (text == null) return 0f;
        return text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
    }
}
