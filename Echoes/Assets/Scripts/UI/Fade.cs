using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Fades de interface reutilizáveis. Dentro de uma corrotina:
/// <c>yield return Fade.Alpha(GameHud.FadeScreen, 1f, 2f, smooth: true);</c>
/// </summary>
public static class Fade
{
    /// <summary>
    /// Leva a opacidade de uma peça do HUD do valor atual até <paramref name="to"/>.
    /// </summary>
    public static IEnumerator Alpha(HudElement element, float to, float duration, bool smooth = false)
    {
        float from = element.alpha;
        return Run(duration, smooth, t => element.alpha = Mathf.Lerp(from, to, t));
    }

    /// <summary>
    /// Leva a opacidade de um elemento de UI Toolkit do valor atual até <paramref name="to"/>.
    /// </summary>
    public static IEnumerator Alpha(VisualElement element, float to, float duration, bool smooth = false)
    {
        // O valor do estilo inline vale mesmo antes do primeiro layout; sem ele, usa o da USS
        StyleFloat inline = element.style.opacity;
        float from = inline.keyword == StyleKeyword.Undefined ? element.resolvedStyle.opacity : inline.value;
        return Run(duration, smooth, t => element.style.opacity = Mathf.Lerp(from, to, t));
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
