using UnityEngine;

/// <summary>
/// Fica no modelo da Clara (junto do Animator) e repassa os eventos das animações para o
/// <see cref="ClaraPresence"/> do objeto pai. O clipe Clara_Photo chama OnPhotoShutter no instante
/// em que o dedo aperta o disparador.
/// </summary>
public class ClaraAnimationEvents : MonoBehaviour
{
    [SerializeField] private ClaraPresence clara;

    private void Awake()
    {
        if (clara == null) clara = GetComponentInParent<ClaraPresence>();
    }

    // Chamado pelo evento do clipe Clara_Photo
    public void OnPhotoShutter()
    {
        if (clara != null) clara.ShutterPressed();
    }
}
