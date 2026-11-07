using UnityEngine.UIElements;

/// <summary>
/// Acrescenta o destaque dos menus: o "eco". Cada item e aba ganha duas cópias fantasmas do texto
/// (classes "echo--near" e "echo--far") que, no hover/foco, escorregam para o lado e somem aos poucos,
/// como uma reverberação. O que aparece e quando fica no Menu.uss.
/// </summary>
public static class MenuDecor
{
    public static void Apply(VisualElement root)
    {
        root.Query<Button>(className: "menu-item").ForEach(item =>
        {
            MoveTextToLabel(item);
        });
        root.Query<Button>(className: "tab").ForEach(tab =>
        {
            MoveTextToLabel(tab);
        });
    }

    // Um botão com filhos deixa de medir o próprio texto (fica do tamanho do padding). O texto vai
    // para um Label dentro dele, e a chave de tradução vai junto (MenuText guarda no userData).
    private static void MoveTextToLabel(Button button)
    {
        string text = button.userData as string ?? button.text;
        if (string.IsNullOrEmpty(text)) return;

        // Os ecos vêm antes, para ficarem atrás do texto
        button.Add(Echo(text, "echo--far"));
        button.Add(Echo(text, "echo--near"));

        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.AddToClassList("menu-item__text");
        button.userData = null;
        button.text = string.Empty;
        button.Add(label);
        if (text.StartsWith("M_") && button.panel != null) MenuText.Localize(button);
    }

    private static Label Echo(string text, string className)
    {
        var echo = new Label(text) { pickingMode = PickingMode.Ignore };
        echo.AddToClassList("echo");
        echo.AddToClassList(className);
        return echo;
    }
}
