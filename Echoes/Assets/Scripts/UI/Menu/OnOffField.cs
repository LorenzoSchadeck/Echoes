using UnityEngine.UIElements;

/// <summary>
/// Liga/desliga em duas metades ("Ligado | Desligado") no lugar da caixinha do Toggle.
/// Clique numa metade escolhe; com teclado ou controle, esquerda/direita escolhem e Enter/A alterna.
/// Os textos são as chaves M_ON e M_OFF, traduzidas por <see cref="MenuText"/>.
/// </summary>
[UxmlElement]
public partial class OnOffField : BaseField<bool>
{
    private readonly Label onSide;
    private readonly Label offSide;

    public OnOffField() : this(null) { }

    public OnOffField(string label) : base(label, new VisualElement())
    {
        AddToClassList("on-off");
        focusable = true;

        VisualElement input = this.Q(className: inputUssClassName);
        input.AddToClassList("on-off__input");

        onSide = MakeSide(input, "M_ON", true);
        offSide = MakeSide(input, "M_OFF", false);

        RegisterCallback<NavigationMoveEvent>(evt =>
        {
            if (evt.direction == NavigationMoveEvent.Direction.Left) { value = true; evt.StopPropagation(); }
            else if (evt.direction == NavigationMoveEvent.Direction.Right) { value = false; evt.StopPropagation(); }
        });
        RegisterCallback<NavigationSubmitEvent>(evt => { value = !value; evt.StopPropagation(); });

        UpdateSides();
    }

    private Label MakeSide(VisualElement input, string key, bool sideValue)
    {
        var side = new Label(key);
        side.AddToClassList("on-off__side");
        var line = new VisualElement { pickingMode = PickingMode.Ignore };
        line.AddToClassList("pencil-line");
        side.Add(line);
        side.RegisterCallback<ClickEvent>(_ =>
        {
            Focus();
            value = sideValue;
        });
        input.Add(side);
        return side;
    }

    public override void SetValueWithoutNotify(bool newValue)
    {
        base.SetValueWithoutNotify(newValue);
        UpdateSides();
    }

    private void UpdateSides()
    {
        if (onSide == null) return;
        onSide.EnableInClassList("on-off__side--selected", value);
        offSide.EnableInClassList("on-off__side--selected", !value);
    }
}
