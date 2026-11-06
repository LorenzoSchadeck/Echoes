using System.Collections;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Clara na abertura de 1995, sem modelo: ela anda sempre atrás de Ethan. O jogador ouve os passos,
/// vê a sombra dela (uma silhueta que só projeta sombra) e o clarão do flash da câmera.
/// </summary>
public class ClaraPresence : MonoBehaviour
{
    [Header("Seguir Ethan")]
    [SerializeField] private Transform player;
    [Tooltip("Câmera do jogador: Clara fica atrás da direção para onde ele olha.")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float distanceBehind = 5.5f;
    [Tooltip("Afastamento para o lado, para a sombra não ficar escondida atrás do próprio Ethan.")]
    [SerializeField] private float sideOffset = 1.2f;
    [SerializeField] private float followSmoothTime = 0.45f;
    [Tooltip("Paredes e móveis que ela não atravessa.")]
    [SerializeField] private LayerMask obstacles = 1;
    [SerializeField] private float bodyRadius = 0.9f;

    [Header("Passos")]
    [SerializeField] private EventReference footstep;
    [Tooltip("Distância andada entre um passo e outro.")]
    [SerializeField] private float strideLength = 2.8f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.6f;

    [Header("Flash da câmera")]
    [SerializeField] private Light flashLight;
    [SerializeField] private EventReference flashSound;
    [SerializeField] private float flashIntensity = 160f;
    [SerializeField] private float flashDuration = 0.08f;
    [Tooltip("Opacidade do clarão branco na tela.")]
    [SerializeField, Range(0f, 1f)] private float screenFlash = 0.55f;

    public int PhotosTaken { get; private set; }

    /// <summary>Quando falso, Clara para onde está (cenas guiadas pelo roteiro).</summary>
    public bool Following { get; set; } = true;

    private Vector3 velocity;
    private float walked;
    private Vector3 lastPosition;

    private void Start()
    {
        if (flashLight != null) flashLight.enabled = false;
        if (player != null) transform.position = DesiredPosition(true);
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (player == null || playerCamera == null) return;

        if (Following)
        {
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(false), ref velocity, followSmoothTime);
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        Vector3 step = transform.position - lastPosition;
        step.y = 0f;
        walked += step.magnitude;
        lastPosition = transform.position;
        if (walked >= strideLength)
        {
            walked = 0f;
            PlayFootstep();
        }
    }

    // Atrás e um pouco ao lado de onde Ethan olha, sem atravessar paredes
    private Vector3 DesiredPosition(bool instant)
    {
        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = player.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 origin = player.position;
        Vector3 offset = -forward * distanceBehind + right * sideOffset;
        float length = offset.magnitude;
        Vector3 direction = offset / length;

        if (Physics.SphereCast(origin, bodyRadius, direction, out RaycastHit hit, length, obstacles, QueryTriggerInteraction.Ignore))
            length = Mathf.Max(0f, hit.distance - 0.1f);

        Vector3 target = origin + direction * length;
        target.y = instant ? player.position.y : Mathf.Lerp(transform.position.y, player.position.y, 0.2f);
        return target;
    }

    private void PlayFootstep()
    {
        if (footstep.IsNull) return;

        FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(footstep);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        instance.setVolume(footstepVolume);
        instance.start();
        instance.release();
    }

    /// <summary>Clara tira uma foto: clarão vindo de trás de Ethan e o som do flash.</summary>
    public void TakePhoto() => StartCoroutine(FlashRoutine(screenFlash, 0f));

    /// <summary>
    /// A última foto da abertura: o clarão toma a tela inteira e fica branco por
    /// <paramref name="holdWhite"/> segundos (o fade de volta fica com quem chamou).
    /// </summary>
    public IEnumerator TakeFinalPhoto(float holdWhite)
    {
        yield return FlashRoutine(1f, holdWhite);
    }

    private IEnumerator FlashRoutine(float screenAlpha, float holdWhite)
    {
        PhotosTaken++;
        if (!flashSound.IsNull) RuntimeManager.PlayOneShot(flashSound, transform.position);

        // Com "Reduzir clarões" ligado, o clarão na tela fica fraco e a luz não estoura
        bool reduced = GameOptions.Current.reduceFlashes;
        float alpha = reduced ? Mathf.Min(screenAlpha, 0.25f) : screenAlpha;
        if (holdWhite > 0f) alpha = screenAlpha;

        if (flashLight != null)
        {
            flashLight.intensity = reduced ? flashIntensity * 0.3f : flashIntensity;
            flashLight.enabled = true;
        }
        GameHud.FlashScreen.alpha = alpha;
        yield return new WaitForSeconds(flashDuration);
        if (flashLight != null) flashLight.enabled = false;

        if (holdWhite > 0f)
        {
            yield return new WaitForSeconds(holdWhite);
            yield break;
        }
        yield return Fade.Alpha(GameHud.FlashScreen, 0f, reduced ? 0.15f : 0.35f);
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform playerBody, Transform cameraTransform, Light flash, EventReference step, EventReference flashEvent)
    {
        player = playerBody;
        playerCamera = cameraTransform;
        flashLight = flash;
        footstep = step;
        flashSound = flashEvent;
    }
#endif
}
