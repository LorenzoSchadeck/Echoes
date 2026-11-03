using UnityEngine.UIElements;

/// <summary>
/// Mostra o destaque de foco só quando o jogador navega por teclado ou gamepad.
/// Com o mouse, o destaque segue o ponteiro (:hover) e o item focado não fica aceso à toa.
/// No USS, o estilo de foco fica sob a classe <see cref="KeyboardNavigationClass"/>.
/// </summary>
public static class MenuFocusStyle
{
    public const string KeyboardNavigationClass = "nav-keyboard";

    public static void Track(VisualElement root)
    {
        root.RegisterCallback<NavigationMoveEvent>(_ => root.AddToClassList(KeyboardNavigationClass), TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationSubmitEvent>(_ => root.AddToClassList(KeyboardNavigationClass), TrickleDown.TrickleDown);
        root.RegisterCallback<PointerMoveEvent>(_ => root.RemoveFromClassList(KeyboardNavigationClass), TrickleDown.TrickleDown);
    }
}
