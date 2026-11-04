using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Uma peça do HUD (texto ou painel). Guarda o estado mesmo antes de o HUD carregar,
/// então scripts podem escrever nela no Awake sem se preocupar com a ordem de inicialização.
/// A interface imita a do TextMeshPro (<c>text</c>, <c>enabled</c>) para os scripts antigos.
/// </summary>
public sealed class HudElement
{
    private VisualElement element;
    private string currentText = string.Empty;
    private bool visible;
    private float opacity = 1f;
    private float fontScale = 1f;
    private readonly float baseFontSize;

    public HudElement(bool visibleByDefault, float baseFontSize = 0f)
    {
        visible = visibleByDefault;
        this.baseFontSize = baseFontSize;
    }

    public string text
    {
        get => currentText;
        set { currentText = value ?? string.Empty; Apply(); }
    }

    /// <summary>Mostra ou esconde a peça.</summary>
    public bool enabled
    {
        get => visible;
        set { visible = value; Apply(); }
    }

    /// <summary>Opacidade de 0 a 1, para fades.</summary>
    public float alpha
    {
        get => opacity;
        set { opacity = Mathf.Clamp01(value); Apply(); }
    }

    /// <summary>Multiplica o tamanho da fonte (opção de tamanho das legendas).</summary>
    public float FontScale
    {
        get => fontScale;
        set { fontScale = value; Apply(); }
    }

    internal void Bind(VisualElement newElement)
    {
        element = newElement;
        Apply();
    }

    private void Apply()
    {
        if (element == null) return;

        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        element.style.opacity = opacity;
        if (element is Label label)
        {
            label.text = currentText;
            if (baseFontSize > 0f) label.style.fontSize = baseFontSize * fontScale;
        }
    }
}
