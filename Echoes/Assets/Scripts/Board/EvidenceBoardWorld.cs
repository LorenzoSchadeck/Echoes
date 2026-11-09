using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Cópias visuais das pistas e fios espaciais; nenhum script de gameplay é copiado.</summary>
[RequireComponent(typeof(EvidenceBoard))]
public class EvidenceBoardWorld : MonoBehaviour
{
    [SerializeField] private Evidence[] evidenceCatalog = Array.Empty<Evidence>();
    [SerializeField] private GameObject pinPrefab;
    [SerializeField] private Material pinHeadMaterial, threadMaterial;
    [SerializeField, Min(.001f)] private float pinDiameter = .06f;
    [SerializeField, Min(.0001f)] private float threadDiameter = .0055f;
    private readonly Dictionary<string, Visual> pinned = new Dictionary<string, Visual>();
    private readonly Dictionary<string, Visual> tray = new Dictionary<string, Visual>();
    private readonly Dictionary<string, LineRenderer> wires = new Dictionary<string, LineRenderer>();
    private readonly Dictionary<Material, Material> previewMaterials = new Dictionary<Material, Material>();
    private Transform runtimeRoot;
    private Material red, gold, paper;
    private LineRenderer preview;
    private EvidenceBoard board;
    private string selectedPin;
    private BoardLink selectedWire;
    private bool trayVisible;
    private const int Segments = 24;

    private class Visual
    {
        public GameObject root, model, pin;
        public Vector3 size;
        public float boardScale;
        public Renderer[] renderers;
    }

    private void Awake() => board = GetComponent<EvidenceBoard>();
    private void OnEnable() => CaseFile.OnBoardChanged += Refresh;
    private void Start() => Refresh();
    private void OnDisable()
    {
        CaseFile.OnBoardChanged -= Refresh;
        HideTray();
    }
    public void Configure(Evidence[] catalog) { evidenceCatalog = catalog; Refresh(); }

    public Vector3 Normal => transform.forward.normalized;
    public Vector3 Right => Quaternion.LookRotation(-Normal, Vector3.up) * Vector3.right;
    public Vector2 SurfaceSize => new Vector2(board.SurfaceBounds.size.x * .91f, board.SurfaceBounds.size.y * .87f);
    public Vector3 SurfaceCenter => board.SurfaceBounds.center + Normal * (board.SurfaceBounds.extents.z + .025f);

    public Vector3 Point(Vector2 normalized) => SurfaceCenter + Right * ((normalized.x - .5f) * SurfaceSize.x)
        + Vector3.up * ((.5f - normalized.y) * SurfaceSize.y);

    public Vector2 ClampPosition(Evidence evidence, Vector2 normalized)
    {
        float x = Mathf.Min(.45f, evidence.boardSize.x / SurfaceSize.x * .5f + .015f);
        float y = Mathf.Min(.45f, evidence.boardSize.y / SurfaceSize.y * .5f + .015f);
        return new Vector2(Mathf.Clamp(normalized.x, x, 1 - x), Mathf.Clamp(normalized.y, y, 1 - y));
    }

    public bool PointerPosition(Camera camera, Vector2 screen, out Vector2 normalized)
    {
        normalized = default;
        var plane = new Plane(Normal, SurfaceCenter);
        Ray ray = camera.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float distance)) return false;
        Vector3 delta = ray.GetPoint(distance) - SurfaceCenter;
        normalized = new Vector2(.5f + Vector3.Dot(delta, Right) / SurfaceSize.x, .5f - delta.y / SurfaceSize.y);
        return normalized.x >= 0 && normalized.x <= 1 && normalized.y >= 0 && normalized.y <= 1;
    }

    private void EnsureResources()
    {
        if (board == null) board = GetComponent<EvidenceBoard>();
        if (runtimeRoot != null) return;
        var go = new GameObject("Mural_Visuais_Runtime");
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        go.transform.localScale = Vector3.one;
        runtimeRoot = go.transform;
        red = MakeMaterial("Barbante vermelho", new Color(.62f,.055f,.035f));
        gold = MakeMaterial("Ligação selecionada", new Color(.95f,.6f,.12f));
        paper = MakeMaterial("Papel",new Color(.82f,.77f,.64f));
    }

    private static Material MakeMaterial(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
        mat.SetColor("_BaseColor", color);
        return mat;
    }

    public void Refresh()
    {
        if (!Application.isPlaying) return;
        EnsureResources();
        foreach (var entry in pinned)
            if (!CaseFile.IsPinned(entry.Key) && entry.Value.root.activeSelf) entry.Value.root.SetActive(false);
        foreach (BoardPlacement placement in CaseFile.Placements)
        {
            Evidence evidence = evidenceCatalog.FirstOrDefault(e => e != null && e.Id == placement.evidenceId);
            if (evidence == null) continue;
            if (!pinned.TryGetValue(evidence.Id, out Visual visual))
                pinned[evidence.Id] = visual = Create(evidence, true);
            if (!visual.root.activeSelf) visual.root.SetActive(true);
            visual.root.transform.localScale = Vector3.one * visual.boardScale;
            visual.root.transform.SetPositionAndRotation(Point(ClampPosition(evidence, placement.position)), Quaternion.LookRotation(Normal,Vector3.up));
            Bounds b = BoundsOf(visual.model);
            visual.pin.transform.position = b.center + Vector3.up * (b.extents.y - .05f) + Normal * (b.extents.z + .002f);
            visual.pin.transform.rotation = Quaternion.LookRotation(Normal, Vector3.up);
            visual.pin.transform.localScale = Vector3.one * ((pinPrefab != null ? pinDiameter / .0098f : pinDiameter) / visual.boardScale);
            foreach (Renderer renderer in visual.pin.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                materials[0] = selectedPin == evidence.Id ? gold : (pinHeadMaterial != null ? pinHeadMaterial : red);
                renderer.sharedMaterials = materials;
            }
        }
        foreach (var entry in wires)
            if (!CaseFile.Links.Any(link => Key(link) == entry.Key) && entry.Value.gameObject.activeSelf)
                entry.Value.gameObject.SetActive(false);
        foreach (BoardLink link in CaseFile.Links)
        {
            if (!PinPosition(link.first,out Vector3 a) || !PinPosition(link.second,out Vector3 b)) continue;
            string key = Key(link);
            if (!wires.TryGetValue(key,out LineRenderer line)) wires[key] = line = CreateLine("Fio_"+key);
            line.gameObject.SetActive(true);
            line.sharedMaterial = selectedWire != null && selectedWire.Matches(link.first,link.second) ? gold : red;
            DrawLine(line,a,b);
        }
        foreach (var pair in tray) pair.Value.root.SetActive(trayVisible && CaseFile.EvidenceIds.Contains(pair.Key) && !CaseFile.IsPinned(pair.Key));
    }

    public bool ModelBounds(string id, out Bounds bounds)
    {
        bounds = default;
        if (!pinned.TryGetValue(id,out Visual visual) || !visual.root.activeSelf) return false;
        bounds = visual.renderers[0].bounds;
        for(int i=1;i<visual.renderers.Length;i++)bounds.Encapsulate(visual.renderers[i].bounds);
        return true;
    }
    public bool PinPosition(string id, out Vector3 position)
    {
        position = default;
        if (!pinned.TryGetValue(id,out Visual visual) || !visual.root.activeSelf) return false;
        position = visual.pin.transform.position + Normal * (pinPrefab != null ? .003f * pinDiameter / .0098f : 0); return true;
    }
    public void SelectPin(string id) { selectedPin = id; Refresh(); }
    public void SelectWire(BoardLink link) { selectedWire = link; Refresh(); }

    public void PositionTray(Evidence evidence, Camera camera, Vector2 viewportCenter, Vector2 viewportSize)
    {
        EnsureResources(); trayVisible = true;
        if (!tray.TryGetValue(evidence.Id, out Visual visual)) tray[evidence.Id] = visual = Create(evidence,false);
        visual.root.SetActive(true);
        const float depth = .8f;
        float height = 2 * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
        float width = height * camera.aspect;
        float scale = Mathf.Min(width * viewportSize.x / visual.size.x, height * viewportSize.y / visual.size.y);
        visual.root.transform.localScale = Vector3.one * scale;
        visual.root.transform.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(viewportCenter.x,1-viewportCenter.y,depth)), camera.transform.rotation * Quaternion.Euler(0,180,0));
    }
    public void HideTrayItem(string id) { if(tray.TryGetValue(id,out Visual v))v.root.SetActive(false); }
    public void HideTray()
    {
        trayVisible = false;
        foreach(var v in tray.Values)v.root.SetActive(false);
        if(preview!=null)preview.gameObject.SetActive(false);
    }
    /// <summary>Ao sair, recolhe apenas a bandeja; o caso montado continua no cenário.</summary>
    public void EndInteraction()
    {
        selectedPin = null;
        selectedWire = null;
        HideTray();
        Refresh();
    }
    public void Preview(string from, Vector2? normalized)
    {
        if (normalized == null || !PinPosition(from,out Vector3 start))
        { if(preview!=null)preview.gameObject.SetActive(false); return; }
        if(preview==null){preview=CreateLine("Fio_Em_Construcao");preview.sharedMaterial=gold;}
        preview.gameObject.SetActive(true); DrawLine(preview,start,Point(normalized.Value)+Normal*.12f);
    }

    public BoardLink PickWire(Camera camera, Vector2 screen, float tolerance)
    {
        BoardLink nearest = null; float distance = tolerance;
        foreach(BoardLink link in CaseFile.Links)
        {
            if(!wires.TryGetValue(Key(link),out LineRenderer line)||!line.gameObject.activeSelf)continue;
            Vector2 a=camera.WorldToScreenPoint(line.GetPosition(0));
            for(int i=1;i<Segments;i++)
            {
                Vector2 b=camera.WorldToScreenPoint(line.GetPosition(i));Vector2 d=b-a;
                float t=Mathf.Clamp01(Vector2.Dot(screen-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));
                float gap=Vector2.Distance(screen,a+t*d);
                if(gap<distance){distance=gap;nearest=link;} a=b;
            }
        }
        return nearest;
    }

    private Visual Create(Evidence evidence, bool onBoard)
    {
        var root = new GameObject((onBoard?"Pista_":"Bandeja_")+evidence.Id);root.layer=2;
        root.transform.SetParent(runtimeRoot,false);
        GameObject source=evidence.boardPrefab!=null?evidence.boardPrefab:CaseFile.VisualSource(evidence.Id);
        GameObject model=source!=null?CopyGeometry(source,root.transform):Fallback(evidence,root.transform);
        if (model.GetComponentsInChildren<Renderer>().Length == 0)
        {
            Destroy(model);
            model = Fallback(evidence, root.transform);
        }
        LocalizeDocument(evidence, model);
        Bounds b=BoundsOf(model);model.transform.position-=b.center;
        var visual=new Visual{root=root,model=model,size=b.size,renderers=model.GetComponentsInChildren<Renderer>(),boardScale=Mathf.Min(evidence.boardSize.x/Mathf.Max(.01f,b.size.x),evidence.boardSize.y/Mathf.Max(.01f,b.size.y))};
        if(onBoard)
        {
            visual.pin=pinPrefab != null ? Instantiate(pinPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.pin.name="Alfinete";
            foreach(Transform child in visual.pin.GetComponentsInChildren<Transform>())child.gameObject.layer=2;
            foreach(Collider collider in visual.pin.GetComponentsInChildren<Collider>())Destroy(collider);
            visual.pin.transform.SetParent(root.transform,false);
        }
        else
        {
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.GetComponent<TMP_Text>()!=null)continue;
                renderer.sharedMaterials=renderer.sharedMaterials.Select(PreviewMaterial).ToArray();
            }
        }
        return visual;
    }

    public void LocalizeModels()
    {
        foreach (var entry in pinned)
        {
            Evidence evidence = evidenceCatalog.FirstOrDefault(e => e != null && e.Id == entry.Key);
            if (evidence != null) LocalizeDocument(evidence, entry.Value.model);
        }
        foreach (var entry in tray)
        {
            Evidence evidence = evidenceCatalog.FirstOrDefault(e => e != null && e.Id == entry.Key);
            if (evidence != null) LocalizeDocument(evidence, entry.Value.model);
        }
    }

    private static void LocalizeDocument(Evidence evidence, GameObject model)
    {
        foreach (TextMeshPro text in model.GetComponentsInChildren<TextMeshPro>(true))
        {
            if (text.name == "Título") text.text = evidence.title.GetLocalizedString();
            else if (text.name == "Data") text.text = evidence.date;
            else if (text.name == "Conteúdo")
            {
                string detail = evidence.details.GetLocalizedString();
                text.text = detail.Length > 190 ? detail.Substring(0,187) + "..." : detail;
            }
            text.ForceMeshUpdate(true,true);
        }
    }

    private GameObject CopyGeometry(GameObject source, Transform parent)
    {
        var model = new GameObject("Modelo");
        model.layer = 2;
        model.transform.SetParent(parent, false);
        model.transform.localRotation = source.transform.rotation;
        model.transform.localScale = source.transform.lossyScale;
        foreach (MeshFilter mesh in source.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled || mesh.sharedMesh == null || mesh.GetComponent<TMP_Text>() != null) continue;
            var go = CopyTransform(mesh.transform, source.transform, model.transform);
            go.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        // O MeshFilter gerado pelo TMP pode não estar serializado no prefab.
        foreach (TextMeshPro original in source.GetComponentsInChildren<TextMeshPro>(true))
        {
            var go = CopyTransform(original.transform, source.transform, model.transform);
            var text = go.AddComponent<TextMeshPro>();
            text.font = original.font;
            text.fontSize = original.fontSize;
            text.fontSizeMin = original.fontSizeMin;
            text.fontSizeMax = original.fontSizeMax;
            text.enableAutoSizing = original.enableAutoSizing;
            text.text = original.text;
            text.color = original.color;
            text.alignment = original.alignment;
            text.overflowMode = original.overflowMode;
            text.rectTransform.sizeDelta = original.rectTransform.sizeDelta;
            text.ForceMeshUpdate(true, true);
        }
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return model;
    }

    private static GameObject CopyTransform(Transform original, Transform source, Transform parent)
    {
        var go = new GameObject(original.name);
        go.layer = 2;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = source.InverseTransformPoint(original.position);
        go.transform.localRotation = Quaternion.Inverse(source.rotation) * original.rotation;
        Vector3 a = original.lossyScale, b = source.lossyScale;
        go.transform.localScale = new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        return go;
    }
    private Material PreviewMaterial(Material source)
    {
        if(source==null)return paper;
        if(previewMaterials.TryGetValue(source,out Material result))return result;
        Color color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.color:Color.white;
        result=MakeMaterial(source.name+" prévia",color);
        Texture texture=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.mainTexture;
        if(texture!=null)result.SetTexture("_BaseMap",texture);
        previewMaterials[source]=result;return result;
    }
    private GameObject Fallback(Evidence evidence,Transform parent)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Papel";go.layer=2;Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent,false);go.transform.localScale=new Vector3(1.3f,1.75f,.035f);go.GetComponent<Renderer>().sharedMaterial=paper;
        var label=new GameObject("Título");label.layer=2;label.transform.SetParent(go.transform,false);label.transform.localPosition=new Vector3(0,0,.55f);label.transform.localRotation=Quaternion.Euler(0,180,0);
        label.transform.localScale=new Vector3(1/1.3f,1/1.75f,1/.035f);
        var text=label.AddComponent<TextMeshPro>();text.text=evidence.title.GetLocalizedString();text.fontSize=2;text.color=Color.black;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(1.15f,1.5f);text.ForceMeshUpdate(true,true);
        return go;
    }
    private static Bounds BoundsOf(GameObject root)
    {
        Renderer[] renderers=root.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0)return new Bounds(root.transform.position,Vector3.one);
        Bounds b=renderers[0].bounds;foreach(Renderer r in renderers)b.Encapsulate(r.bounds);return b;
    }
    private LineRenderer CreateLine(string name)
    {
        var go=new GameObject(name);go.layer=2;go.transform.SetParent(runtimeRoot,false);
        var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=Segments;line.widthMultiplier=threadDiameter;
        line.numCapVertices=4;line.numCornerVertices=3;line.alignment=LineAlignment.View;
        line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.sharedMaterial=red;
        // Mantém a linha como trajetória para seleção; a aparência é uma malha tubular 3D.
        line.enabled=false;
        go.AddComponent<BoardThreadGeometry>().Configure(threadMaterial != null ? threadMaterial : red, gold);
        return line;
    }
    private static void DrawLine(LineRenderer line,Vector3 a,Vector3 b)
    {
        Vector3 control=(a+b)*.5f+Vector3.down*(Vector3.Distance(a,b)*.065f);
        for(int i=0;i<Segments;i++){float t=i/(float)(Segments-1);line.SetPosition(i,(1-t)*(1-t)*a+2*(1-t)*t*control+t*t*b);}
        line.GetComponent<BoardThreadGeometry>().Rebuild(line);
    }
    private static string Key(BoardLink link)=>string.CompareOrdinal(link.first,link.second)<0?link.first+"_"+link.second:link.second+"_"+link.first;
    private void OnDestroy()
    {
        foreach(var material in previewMaterials.Values)if(material!=null)Destroy(material);
        if(red!=null)Destroy(red);if(gold!=null)Destroy(gold);if(paper!=null)Destroy(paper);
    }
}
