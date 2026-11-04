using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

/// <summary>
/// Tela do mural (EvidenceBoard.uxml). Mostra as pistas que Ethan já achou; o jogador escolhe duas
/// e, se formarem uma dedução, o barbante fica e a conclusão entra no caso.
/// O que Ethan pensa sobre cada ligação é dito ao fechar o mural, quando a legenda fica visível.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class EvidenceBoardUI : MonoBehaviour
{
    [Tooltip("Todas as pistas do capítulo. Uma pista fora desta lista não aparece no mural.")]
    [SerializeField] private Evidence[] evidenceCatalog = Array.Empty<Evidence>();
    [Tooltip("Todas as ligações válidas do capítulo.")]
    [SerializeField] private Deduction[] deductionCatalog = Array.Empty<Deduction>();

    public static bool IsOpen { get; private set; }

    // O mesmo Esc que fecha o mural não deve abrir a pausa
    private static int closedFrame = -1;
    public static bool BlocksPause => IsOpen || closedFrame == Time.frameCount;

    private PlayerInputActions inputActions;
    private VisualElement root;
    private VisualElement cards;
    private VisualElement emptyLabel;
    private StringLayer strings;
    private Label detailTitle;
    private Label detailMeta;
    private Label detailText;
    private Label status;
    private VisualElement conclusions;

    private PlayerInteractor interactor;
    private readonly Dictionary<Evidence, VisualElement> cardOf = new Dictionary<Evidence, VisualElement>();
    private readonly List<InnerVoiceLine> pendingReactions = new List<InnerVoiceLine>();
    private Evidence selected;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Pause.performed += _ => Close();
    }

    private void OnEnable()
    {
        inputActions.Player.Pause.Enable();

        root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = DisplayStyle.None;

        cards = root.Q("cards");
        emptyLabel = root.Q("empty");
        detailTitle = root.Q<Label>("detail-title");
        detailMeta = root.Q<Label>("detail-meta");
        detailText = root.Q<Label>("detail-text");
        status = root.Q<Label>("status");
        conclusions = root.Q("conclusions");

        strings = new StringLayer(LinkedCards);
        root.Q("board").Add(strings);

        root.Q<Button>("button-close").clicked += Close;
        root.RegisterCallback<NavigationCancelEvent>(_ => Close());

        MenuText.Localize(root);
        MenuFocusStyle.Track(root);
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        inputActions.Player.Pause.Disable();
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        Close();
    }

    public void Open(PlayerInteractor player)
    {
        if (IsOpen) return;

        IsOpen = true;
        interactor = player;
        if (interactor != null) interactor.SetInspectionMode(true);

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        Rebuild();
        status.text = string.Empty;
        root.style.display = DisplayStyle.Flex;
        root.schedule.Execute(() => (cards.childCount > 0 ? cards[0] : root.Q("button-close")).Focus());
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        closedFrame = Time.frameCount;
        Deselect();
        root.style.display = DisplayStyle.None;

        // Devolve o controle e trava o cursor de novo
        if (interactor != null) interactor.SetInspectionMode(false);
        interactor = null;

        foreach (InnerVoiceLine line in pendingReactions) InnerVoice.Say(line);
        pendingReactions.Clear();
    }

    private void Rebuild()
    {
        cards.Clear();
        cardOf.Clear();
        selected = null;

        foreach (string id in CaseFile.EvidenceIds)
        {
            Evidence evidence = evidenceCatalog.FirstOrDefault(e => e != null && e.Id == id);
            if (evidence == null) continue;

            VisualElement card = CreateCard(evidence);
            cards.Add(card);
            cardOf[evidence] = card;
        }

        emptyLabel.style.display = cardOf.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        ShowDetail(null);
        RefreshConclusions();
        strings.MarkDirtyRepaint();
    }

    private VisualElement CreateCard(Evidence evidence)
    {
        var card = new Button(() => Select(evidence));
        card.AddToClassList("card");
        card.style.left = Length.Percent(evidence.boardPosition.x * 100f);
        card.style.top = Length.Percent(evidence.boardPosition.y * 100f);

        if (evidence.photo != null)
        {
            var photo = new VisualElement();
            photo.AddToClassList("card__photo");
            photo.style.backgroundImage = new StyleBackground(evidence.photo);
            card.Add(photo);
        }

        var title = new Label(Text(evidence.title));
        title.AddToClassList("card__title");
        card.Add(title);

        var meta = new Label(Meta(evidence));
        meta.AddToClassList("card__meta");
        card.Add(meta);

        card.RegisterCallback<PointerEnterEvent>(_ => ShowDetail(evidence));
        card.RegisterCallback<FocusInEvent>(_ => ShowDetail(evidence));
        // O barbante segue o cartão quando o layout muda
        card.RegisterCallback<GeometryChangedEvent>(_ => strings.MarkDirtyRepaint());
        return card;
    }

    private void Select(Evidence evidence)
    {
        if (selected == null)
        {
            selected = evidence;
            cardOf[evidence].AddToClassList("card--selected");
            status.text = MenuText.Get("M_BOARD_PICK_SECOND");
            return;
        }

        Evidence first = selected;
        Deselect();
        if (first == evidence)
        {
            status.text = string.Empty;
            return;
        }

        Deduction deduction = deductionCatalog.FirstOrDefault(d => d != null && d.Links(first, evidence));
        if (deduction == null)
        {
            status.text = MenuText.Get("M_BOARD_NO_MATCH");
        }
        else if (!CaseFile.Connect(deduction))
        {
            status.text = MenuText.Get("M_BOARD_ALREADY");
        }
        else
        {
            status.text = Text(deduction.conclusion);
            if (deduction.reaction != null) pendingReactions.Add(deduction.reaction);
            RefreshConclusions();
            strings.MarkDirtyRepaint();
        }
    }

    private void Deselect()
    {
        if (selected != null && cardOf.TryGetValue(selected, out VisualElement card)) card.RemoveFromClassList("card--selected");
        selected = null;
    }

    private void ShowDetail(Evidence evidence)
    {
        detailTitle.text = evidence != null ? Text(evidence.title) : string.Empty;
        detailMeta.text = evidence != null ? Meta(evidence) : string.Empty;
        detailText.text = evidence != null ? Text(evidence.details) : MenuText.Get("M_BOARD_HINT");
    }

    private void RefreshConclusions()
    {
        conclusions.Clear();

        foreach (Deduction deduction in MadeDeductions())
        {
            var label = new Label(Text(deduction.conclusion));
            label.AddToClassList("board-conclusion");
            conclusions.Add(label);
        }

        if (conclusions.childCount == 0)
        {
            var none = new Label(MenuText.Get("M_BOARD_NO_CONCLUSIONS"));
            none.AddToClassList("note");
            conclusions.Add(none);
        }
    }

    private IEnumerable<Deduction> MadeDeductions() =>
        CaseFile.DeductionIds
            .Select(id => deductionCatalog.FirstOrDefault(d => d != null && d.Id == id))
            .Where(d => d != null);

    private IEnumerable<(VisualElement, VisualElement)> LinkedCards()
    {
        foreach (Deduction deduction in MadeDeductions())
        {
            if (cardOf.TryGetValue(deduction.first, out VisualElement a) && cardOf.TryGetValue(deduction.second, out VisualElement b))
                yield return (a, b);
        }
    }

    private static string Text(LocalizedString text) => text == null || text.IsEmpty ? string.Empty : text.GetLocalizedString();

    private static string Meta(Evidence evidence)
    {
        string place = Text(evidence.place);
        if (string.IsNullOrEmpty(evidence.date)) return place;
        return string.IsNullOrEmpty(place) ? evidence.date : $"{evidence.date}  ·  {place}";
    }

    private void HandleLocaleChanged(Locale _)
    {
        MenuText.Localize(root);
        if (IsOpen) Rebuild();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsOpen = false;
        closedFrame = -1;
    }

    /// <summary>
    /// Barbante vermelho entre os cartões ligados, com uma leve barriga, e um alfinete em cada ligação.
    /// </summary>
    private class StringLayer : VisualElement
    {
        private static readonly Color StringColor = new Color(0.62f, 0.12f, 0.12f);
        private const float PinOffset = 10f;
        private const float PinRadius = 5f;

        private readonly Func<IEnumerable<(VisualElement, VisualElement)>> links;

        public StringLayer(Func<IEnumerable<(VisualElement, VisualElement)>> links)
        {
            this.links = links;
            pickingMode = PickingMode.Ignore;
            AddToClassList("strings");
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            painter.strokeColor = StringColor;
            painter.fillColor = StringColor;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;

            foreach (var (a, b) in links())
            {
                Vector2 from = PinPoint(a);
                Vector2 to = PinPoint(b);
                Vector2 sag = (from + to) / 2f + Vector2.up * (Vector2.Distance(from, to) * 0.08f);

                painter.BeginPath();
                painter.MoveTo(from);
                painter.QuadraticCurveTo(sag, to);
                painter.Stroke();

                DrawPin(painter, from);
                DrawPin(painter, to);
            }
        }

        private Vector2 PinPoint(VisualElement card) =>
            this.WorldToLocal(new Vector2(card.worldBound.center.x, card.worldBound.yMin + PinOffset));

        private static void DrawPin(Painter2D painter, Vector2 at)
        {
            painter.BeginPath();
            painter.Arc(at, PinRadius, 0f, 360f);
            painter.Fill();
        }
    }
}
