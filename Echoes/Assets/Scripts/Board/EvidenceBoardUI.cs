using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument), typeof(EvidenceBoardFocus))]
[DefaultExecutionOrder(1100)]
public class EvidenceBoardUI : MonoBehaviour
{
    [SerializeField] private Evidence[] evidenceCatalog = Array.Empty<Evidence>();
    [SerializeField] private Deduction[] deductionCatalog = Array.Empty<Deduction>();
    public static bool IsOpen { get; private set; }
    private static int closedFrame = -1;
    public static bool BlocksPause => IsOpen || closedFrame == Time.frameCount;
    private PlayerInputActions inputActions;
    private VisualElement root, board, cards, side, tray, empty;
    private VisualElement trayViewport;

    private Label detailText;

    private EvidenceBoardFocus focus;
    private EvidenceBoard physicalBoard;
    private EvidenceBoardWorld world;
    private PlayerInteractor interactor;
    private readonly Dictionary<string,VisualElement> cardOf = new Dictionary<string,VisualElement>();
    private readonly Dictionary<Evidence,VisualElement> trayPreview = new Dictionary<Evidence,VisualElement>();
    private readonly List<InnerVoiceLine> pendingReactions = new List<InnerVoiceLine>();
    private Evidence selectedPin, dragged;
    private BoardLink selectedLink;
    private bool dragWasPinned, ownsOpen, layoutChanged;
    private Vector2 dragOriginalPosition, lastPointer;
    private Coroutine closing;

    private void Awake()
    {
        inputActions=new PlayerInputActions();inputActions.Player.Pause.performed+=_=>Close();
        focus=GetComponent<EvidenceBoardFocus>();if(focus==null)focus=gameObject.AddComponent<EvidenceBoardFocus>();
    }
    private void OnEnable()
    {
        inputActions.Player.Pause.Enable();root=GetComponent<UIDocument>().rootVisualElement;
        root.AddToClassList("board-document");root.AddToClassList("board-hidden");root.focusable=true;
        board=root.Q("board");cards=root.Q("cards");side=root.Q("side");tray=root.Q<ScrollView>("tray").contentContainer;


        trayViewport=root.Q<ScrollView>("tray").contentViewport;
        empty=root.Q("empty");detailText=root.Q<Label>("detail-text");
        root.RegisterCallback<NavigationCancelEvent>(HandleCancel);root.RegisterCallback<KeyDownEvent>(HandleKey);
        root.RegisterCallback<PointerMoveEvent>(TrackPointer);board.RegisterCallback<PointerDownEvent>(SelectWire);
        MenuText.Localize(root);MenuFocusStyle.Track(root);
        LocalizationSettings.SelectedLocaleChanged+=HandleLocaleChanged;CaseFile.OnEvidenceAdded+=HandleEvidenceAdded;
    }
    private void OnDisable()
    {
        inputActions.Player.Pause.Disable();LocalizationSettings.SelectedLocaleChanged-=HandleLocaleChanged;CaseFile.OnEvidenceAdded-=HandleEvidenceAdded;
        if(ownsOpen)FinishClose(false);
        root.UnregisterCallback<NavigationCancelEvent>(HandleCancel);root.UnregisterCallback<KeyDownEvent>(HandleKey);
        root.UnregisterCallback<PointerMoveEvent>(TrackPointer);board.UnregisterCallback<PointerDownEvent>(SelectWire);

    }
    private void OnDestroy()=>inputActions?.Dispose();
    private void HandleCancel(NavigationCancelEvent e)=>Close();
    private void HandleKey(KeyDownEvent e){if(e.keyCode==KeyCode.Delete||e.keyCode==KeyCode.Backspace){RemoveSelectedLink();e.StopPropagation();}}
    private void TrackPointer(PointerMoveEvent e)=>lastPointer=e.position;

    public void Open(PlayerInteractor player,EvidenceBoard target=null)
    {
        if(IsOpen||closing!=null||target==null)return;
        IsOpen=ownsOpen=true;layoutChanged=false;interactor=player;physicalBoard=target;
        world=target.GetComponent<EvidenceBoardWorld>();if(world==null)world=target.gameObject.AddComponent<EvidenceBoardWorld>();
        world.Configure(evidenceCatalog);
        if(interactor!=null)interactor.SetInspectionMode(true);PlayerControlLock.Acquire(this);
        UnityEngine.Cursor.lockState=CursorLockMode.None;UnityEngine.Cursor.visible=true;
        focus.Focus(target,player);root.RemoveFromClassList("board-hidden");Rebuild();
        root.schedule.Execute(()=>root.Focus());
    }
    public void Close()
    {
        if(!ownsOpen||closing!=null)return;
        CancelDrag();ClearSelection();world.EndInteraction();root.AddToClassList("board-hidden");focus.Release(true);
        closing=StartCoroutine(ReturnToPlayer());
    }
    private IEnumerator ReturnToPlayer(){while(focus.IsTransitioning)yield return null;FinishClose(true);}
    private void FinishClose(bool save)
    {
        if(closing!=null)StopCoroutine(closing);closing=null;
        CancelDrag();ClearSelection();if(world!=null)world.EndInteraction();focus.Release(false);root.AddToClassList("board-hidden");
        IsOpen=ownsOpen=false;closedFrame=Time.frameCount;
        if(interactor!=null)interactor.SetInspectionMode(false);interactor=null;physicalBoard=null;PlayerControlLock.Release(this);
        if(save&&layoutChanged&&!CheckpointManager.IsRestoring&&!LostTimeEvent.IsActive)FindAnyObjectByType<CheckpointManager>()?.SaveCheckpoint();
        if(save)foreach(InnerVoiceLine line in pendingReactions)InnerVoice.Say(line);pendingReactions.Clear();
    }

    private Evidence Resolve(string id)=>evidenceCatalog.FirstOrDefault(e=>e!=null&&e.Id==id);
    private void Rebuild()
    {
        CancelDrag();ClearSelection();cards.Clear();tray.Clear();cardOf.Clear();trayPreview.Clear();world.Refresh();
        foreach(string id in CaseFile.EvidenceIds)
        {
            Evidence evidence=Resolve(id);if(evidence==null)continue;
            if(CaseFile.IsPinned(id)){var hit=CreateHit(evidence);cards.Add(hit);cardOf[id]=hit;world.HideTrayItem(id);}
            else tray.Add(CreateTrayItem(evidence));
        }
        empty.EnableInClassList("board-hidden",cardOf.Count>0);ShowDetail(null);
        if(tray.childCount==0){var label=new Label(MenuText.Get(CaseFile.EvidenceIds.Count==0?"M_BOARD_EMPTY":"M_BOARD_TRAY_EMPTY"));label.AddToClassList("tray-empty");tray.Add(label);}
    }

    private VisualElement CreateTrayItem(Evidence evidence)
    {
        var row=new VisualElement{focusable=true};row.AddToClassList("evidence-row");
        var preview=new VisualElement{pickingMode=PickingMode.Ignore};preview.AddToClassList("evidence-preview");row.Add(preview);trayPreview[evidence]=preview;
        var info=new VisualElement{pickingMode=PickingMode.Ignore};info.AddToClassList("evidence-info");
        info.Add(new Label(Text(evidence.thought)){pickingMode=PickingMode.Ignore});
        row.Add(info);
        AttachDrag(row,evidence);row.RegisterCallback<PointerEnterEvent>(_=>ShowDetail(evidence));row.RegisterCallback<FocusInEvent>(_=>ShowDetail(evidence));
        row.RegisterCallback<NavigationSubmitEvent>(e=>{TogglePin(evidence);e.StopPropagation();});return row;
    }
    private VisualElement CreateHit(Evidence evidence)
    {
        var hit=new VisualElement{focusable=true,usageHints=UsageHints.DynamicTransform};hit.AddToClassList("evidence-hit");
        var pin=new VisualElement{focusable=true,tooltip=MenuText.Get("M_BOARD_PIN_HINT")};pin.AddToClassList("pin-hit");hit.Add(pin);
        pin.RegisterCallback<PointerDownEvent>(e=>{if(e.button==0){lastPointer=e.position;SelectPin(evidence);e.StopPropagation();}});
        pin.RegisterCallback<PointerUpEvent>(e=>e.StopPropagation());
        pin.RegisterCallback<NavigationSubmitEvent>(e=>{SelectPin(evidence);e.StopPropagation();});
        AttachDrag(hit,evidence);hit.RegisterCallback<PointerEnterEvent>(_=>ShowDetail(evidence));hit.RegisterCallback<FocusInEvent>(_=>ShowDetail(evidence));
        hit.RegisterCallback<NavigationSubmitEvent>(e=>{if(e.target==hit){TogglePin(evidence);e.StopPropagation();}});return hit;
    }
    private void AttachDrag(VisualElement target,Evidence evidence)=>target.AddManipulator(new EvidenceDragManipulator(p=>BeginDrag(evidence,p),MoveDrag,Drop,CancelDrag,()=>TogglePin(evidence)));

    private void TogglePin(Evidence evidence)
    {
        if(CaseFile.IsPinned(evidence.Id))CaseFile.Unpin(evidence.Id);
        else CaseFile.Pin(evidence.Id,world.ClampPosition(evidence,FreePosition(evidence)));
        layoutChanged=true;Rebuild();
    }
    private Vector2 FreePosition(Evidence evidence)
    {
        Vector2 initial=world.ClampPosition(evidence,evidence.boardPosition);
        if(!CaseFile.Placements.Any(p=>Vector2.Distance(p.position,initial)<.24f))return initial;
        foreach(Vector2 point in new[]{new Vector2(.22f,.25f),new Vector2(.7f,.25f),new Vector2(.22f,.73f),new Vector2(.7f,.73f),new Vector2(.5f,.5f)})
            if(!CaseFile.Placements.Any(p=>Vector2.Distance(p.position,point)<.24f))return point;
        return initial;
    }
    private void BeginDrag(Evidence evidence,Vector2 point)
    {
        CancelDrag();dragged=evidence;dragWasPinned=CaseFile.IsPinned(evidence.Id);
        dragOriginalPosition=CaseFile.Placements.FirstOrDefault(p=>p.evidenceId==evidence.Id)?.position??evidence.boardPosition;
        ClearSelection();MoveDrag(point);
    }
    private Vector2 ScreenPoint(Vector2 panelPoint)
    {
        Rect r=root.worldBound;return new Vector2((panelPoint.x-r.x)/r.width*Screen.width,(1-(panelPoint.y-r.y)/r.height)*Screen.height);
    }
    private bool BoardPosition(Vector2 point,out Vector2 normalized)
    {
        normalized=default;return focus.View!=null&&!side.worldBound.Contains(point)&&world.PointerPosition(focus.View,ScreenPoint(point),out normalized);
    }
    private void MoveDrag(Vector2 point)
    {
        lastPointer=point;if(dragged==null)return;
        if(BoardPosition(point,out Vector2 p)){CaseFile.Pin(dragged.Id,world.ClampPosition(dragged,p));world.HideTrayItem(dragged.Id);}
    }
    private void Drop(Vector2 point)
    {
        if(dragged==null)return;Evidence evidence=dragged;
        if(BoardPosition(point,out Vector2 p)){CaseFile.Pin(evidence.Id,world.ClampPosition(evidence,p));layoutChanged=true;dragged=null;}
        else if(side.worldBound.Contains(point)){CaseFile.Unpin(evidence.Id);layoutChanged=true;dragged=null;}
        else CancelDrag();
        root.schedule.Execute(()=>{if(ownsOpen)Rebuild();});
    }
    private void CancelDrag()
    {
        if(dragged==null)return;
        if(dragWasPinned)CaseFile.Pin(dragged.Id,dragOriginalPosition);else CaseFile.Unpin(dragged.Id);
        dragged=null;
    }

    private void LateUpdate()
    {
        if(!ownsOpen||closing!=null||!focus.IsFocused||root.panel==null)return;
        Camera camera=focus.View;
        foreach(var pair in cardOf)
        {
            if(!world.ModelBounds(pair.Key,out Bounds b))continue;
            Rect rect=ProjectedBounds(camera,b);VisualElement hit=pair.Value;
            hit.style.translate=new Translate(rect.x,rect.y);hit.style.width=rect.width;hit.style.height=rect.height;
            if(world.PinPosition(pair.Key,out Vector3 pin))
            {
                Vector2 p=Project(camera,pin);var pinHit=hit[0];pinHit.style.translate=new Translate(p.x-rect.x-14,p.y-rect.y-14);
            }
        }
        Rect rootRect=root.worldBound;
        foreach(var pair in trayPreview)
        {
            if(CaseFile.IsPinned(pair.Key.Id)){world.HideTrayItem(pair.Key.Id);continue;}
            Rect rect=pair.Value.worldBound;
            Rect clip=trayViewport.worldBound;
            if(!clip.Overlaps(rect)||rect.width<1||rect.height<1){world.HideTrayItem(pair.Key.Id);continue;}
            // Oculta a miniatura parcialmente cortada para não desenhá-la fora da bandeja.
            if(rect.yMin<clip.yMin||rect.yMax>clip.yMax){world.HideTrayItem(pair.Key.Id);continue;}
            world.PositionTray(pair.Key,camera,new Vector2((rect.center.x-rootRect.x)/rootRect.width,(rect.center.y-rootRect.y)/rootRect.height),new Vector2(rect.width/rootRect.width*.85f,rect.height/rootRect.height*.85f));
        }
        if(selectedPin!=null&&BoardPosition(lastPointer,out Vector2 target))world.Preview(selectedPin.Id,target);else world.Preview(null,null);
    }
    private Vector2 Project(Camera camera,Vector3 point)
    {
        Vector3 screen=camera.WorldToScreenPoint(point);
        return root.WorldToLocal(RuntimePanelUtils.ScreenToPanel(root.panel,new Vector2(screen.x,Screen.height-screen.y)));
    }
    private Rect ProjectedBounds(Camera camera,Bounds b)
    {
        Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
        for(int i=0;i<8;i++){Vector2 p=Project(camera,b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }

    private void SelectPin(Evidence evidence)
    {
        ShowDetail(evidence);selectedLink=null;world.SelectWire(null);
        if(selectedPin==null){selectedPin=evidence;world.SelectPin(evidence.Id);return;}
        Evidence first=selectedPin;selectedPin=null;world.SelectPin(null);world.Preview(null,null);
        if(first==evidence){return;}
        if(!CaseFile.Link(first.Id,evidence.Id)){return;}
        layoutChanged=true;Deduction deduction=deductionCatalog.FirstOrDefault(d=>d!=null&&d.Links(first,evidence));
        if(deduction!=null&&CaseFile.Connect(deduction)){if(deduction.reaction!=null)pendingReactions.Add(deduction.reaction);}
    }
    private void ClearSelection()
    {
        selectedPin=null;selectedLink=null;if(world==null)return;world.SelectPin(null);world.SelectWire(null);world.Preview(null,null);
    }
    private void SelectWire(PointerDownEvent e)
    {
        if(cards.Contains(e.target as VisualElement)||focus.View==null)return;
        selectedLink=world.PickWire(focus.View,ScreenPoint(e.position),10f*Screen.width/root.worldBound.width);world.SelectWire(selectedLink);
        if(selectedLink==null)return;
        if(e.button==1)RemoveSelectedLink();e.StopPropagation();
    }
    private void RemoveSelectedLink()
    {
        if(selectedLink==null)return;CaseFile.Unlink(selectedLink);selectedLink=null;world.SelectWire(null);layoutChanged=true;
    }
    private void ShowDetail(Evidence evidence)
    {
        detailText.text = evidence != null && CaseFile.IsPinned(evidence.Id) ? Text(evidence.thought) : string.Empty;
        detailText.EnableInClassList("board-hidden", string.IsNullOrEmpty(detailText.text));
    }
    private static string Text(LocalizedString text)=>text==null||text.IsEmpty?string.Empty:text.GetLocalizedString();
    private static string Meta(Evidence evidence)=>string.IsNullOrEmpty(evidence.date)?Text(evidence.place):evidence.date+" · "+Text(evidence.place);
    private void HandleLocaleChanged(Locale _){MenuText.Localize(root);if(world!=null)world.LocalizeModels();if(ownsOpen)Rebuild();}
    private void HandleEvidenceAdded(Evidence _){if(ownsOpen)Rebuild();}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetStatics(){IsOpen=false;closedFrame=-1;}
}
