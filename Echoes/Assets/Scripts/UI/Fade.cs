using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fades de interface reutilizáveis. Dentro de uma corrotina:
/// <c>yield return Fade.Alpha(fadeImage, 1f, 2f, smooth: true);</c>
/// </summary>
public static class Fade
{
    /// <summary>
    /// Leva o alpha de uma Image/Text/TMP do valor atual até <paramref name="to"/>.
    /// </summary>
    public static IEnumerator Alpha(Graphic graphic, float to, float duration, bool smooth = false)
    {
        float from = graphic.color.a;
        return Run(duration, smooth, t => SetAlpha(graphic, Mathf.Lerp(from, to, t)));
    }

    /// <summary>
    /// Leva o alpha de um CanvasGroup do valor atual até <paramref name="to"/>.
    /// </summary>
    public static IEnumerator Alpha(CanvasGroup group, float to, float duration, bool smooth = false)
    {
        float from = group.alpha;
        return Run(duration, smooth, t => group.alpha = Mathf.Lerp(from, to, t));
    }

    public static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private static IEnumerator Run(float duration, bool smooth, Action<float> apply)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            apply(smooth ? Mathf.SmoothStep(0f, 1f, t) : t);
            yield return null;
        }

        apply(1f);
    }
}
