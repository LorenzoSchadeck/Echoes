using Unity.Cinemachine;
using UnityEngine;

/// <summary>Enquadra a cortiça física e devolve a câmera ao jogador ao sair.</summary>
[DefaultExecutionOrder(1000)]
public class EvidenceBoardFocus : MonoBehaviour
{
    [SerializeField] private float transitionSeconds = 0.55f;
    private Camera view;
    private CinemachineBrain brain;
    private bool brainWasEnabled;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private EvidenceBoard board;
    private Vector3 blendFrom;
    private Quaternion blendRotation;
    private float blendElapsed;
    private bool returning;
    public Camera View => view;
    public bool IsFocused => board != null && view != null;
    public bool IsTransitioning { get; private set; }

    public void Focus(EvidenceBoard target, PlayerInteractor player)
    {
        if (target == null || player == null) return;
        view = player.CameraTransform.GetComponent<Camera>();
        if (view == null) view = Camera.main;
        if (view == null) return;
        board = target;
        originalParent = view.transform.parent;
        originalLocalPosition = view.transform.localPosition;
        originalLocalRotation = view.transform.localRotation;
        originalPosition = view.transform.position;
        originalRotation = view.transform.rotation;
        brain = view.GetComponent<CinemachineBrain>();
        brainWasEnabled = brain != null && brain.enabled;
        if (brain != null) brain.enabled = false;
        BeginBlend(false);
    }

    private void TargetPose(out Vector3 position, out Quaternion rotation)
    {
        if (returning)
        {
            position = originalParent != null ? originalParent.TransformPoint(originalLocalPosition) : originalPosition;
            rotation = originalParent != null ? originalParent.rotation * originalLocalRotation : originalRotation;
            return;
        }
        Bounds bounds = board.SurfaceBounds;
        Vector3 normal = board.transform.forward;
        if (Vector3.Dot(normal, originalPosition - bounds.center) < 0f) normal = -normal;
        rotation = Quaternion.LookRotation(-normal, Vector3.up);
        Vector3 right = rotation * Vector3.right;
        float halfHeight = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f);
        float distance = Mathf.Max(bounds.size.y / (2f * halfHeight * 0.82f),
            bounds.size.x / (2f * halfHeight * view.aspect * 0.61f)) * 1.04f;
        position = bounds.center + normal * distance + right * (0.30f * distance * halfHeight * view.aspect);
    }

    // A física e o Cinemachine terminam antes de escrever a pose final do quadro.
    // A interface projeta os cartões logo depois, no mesmo frame.
    private void LateUpdate()
    {
        if (view == null || (!IsFocused && !IsTransitioning)) return;
        TargetPose(out Vector3 position, out Quaternion rotation);
        if (IsTransitioning)
        {
            blendElapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(blendElapsed / Mathf.Max(.01f, transitionSeconds)));
            view.transform.SetPositionAndRotation(Vector3.Lerp(blendFrom, position, t), Quaternion.Slerp(blendRotation, rotation, t));
            if (blendElapsed < transitionSeconds) return;
            IsTransitioning = false;
            if (returning) Restore();
        }
        else view.transform.SetPositionAndRotation(position, rotation);
    }

    public void Release(bool animate)
    {
        if (view == null) { Restore(); return; }
        if (animate && isActiveAndEnabled)
        {
            board = null;
            BeginBlend(true);
        }
        else
        {
            returning = true;
            TargetPose(out Vector3 position, out Quaternion rotation);
            view.transform.SetPositionAndRotation(position, rotation);
            Restore();
        }
    }

    private void BeginBlend(bool isReturning)
    {
        returning = isReturning;
        blendFrom = view.transform.position;
        blendRotation = view.transform.rotation;
        blendElapsed = 0f;
        IsTransitioning = true;
    }

    private void Restore()
    {
        if (brain != null) brain.enabled = brainWasEnabled;
        brain = null; view = null; board = null; originalParent = null;
        returning = false; IsTransitioning = false;
    }

    private void OnDisable() => Release(false);
}
