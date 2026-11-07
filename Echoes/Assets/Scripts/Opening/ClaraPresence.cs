using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Clara na abertura de 1995, uma máquina de estados simples:
/// <list type="bullet">
/// <item>Parada: espera virada para Ethan; só sai do lugar quando ele se afasta além da distância.</item>
/// <item>Andando: vai até ele no passo da animação e para assim que volta a ficar a essa distância.</item>
/// <item>Indo para a porta: quando Ethan entra no quarto 6, ela anda até a porta (nunca entra).</item>
/// <item>Na porta: parada, olhando para Ethan, e é dali que tira as fotos.</item>
/// <item>Roteiro: congelada onde está (<see cref="Following"/> falso).</item>
/// </list>
/// As fotos são animadas: ela ergue a câmera, mira e o flash sai no instante em que aperta o disparador
/// (evento do clipe Clara_Photo). Sem modelo animado, continua funcionando com a sombra e o flash.
/// </summary>
public class ClaraPresence : MonoBehaviour
{
    public enum State { Idle, Walking, GoingToDoor, AtDoor, Scripted }

    [Header("Seguir Ethan")]
    [SerializeField] private Transform player;
    [Tooltip("Câmera do jogador (usada para mirar a foto final).")]
    [SerializeField] private Transform playerCamera;
    [Tooltip("Distância que ela mantém de Ethan (unidades).")]
    [FormerlySerializedAs("distanceBehind")]
    [SerializeField] private float followDistance = 5.5f;
    [Tooltip("Quanto Ethan precisa se afastar além da distância para ela voltar a andar (evita parar e andar sem parar).")]
    [SerializeField] private float startSlack = 1.2f;
    [Tooltip("Velocidade do passo dela (Ethan anda a 5).")]
    [SerializeField] private float walkSpeed = 5f;
    [Tooltip("Se ficar muito para trás, apressa o passo até este multiplicador.")]
    [SerializeField] private float catchUpMultiplier = 1.3f;
    [Tooltip("Paredes e móveis que ela não atravessa (e o chão onde pisa).")]
    [SerializeField] private LayerMask obstacles = 1;
    [SerializeField] private float bodyRadius = 0.9f;

    [Header("Passos")]
    [SerializeField] private EventReference footstep;
    [Tooltip("Distância andada entre um passo e outro.")]
    [SerializeField] private float strideLength = 2.8f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.6f;

    [Header("Modelo e animações")]
    [Tooltip("Animator do modelo da Clara (parâmetro Speed e o gatilho Photo).")]
    [SerializeField] private Animator animator;
    [Tooltip("Velocidade (unidades/s) em que a animação de andar fica no passo normal.")]
    [SerializeField] private float walkAnimationSpeed = 5f;
    [SerializeField] private float turnSpeed = 240f;
    [Tooltip("Onde ela para, no corredor, logo antes da soleira do quarto 6.")]
    [SerializeField] private Transform doorSpot;
    [Tooltip("Segundos máximos esperando o disparador (se o evento da animação não vier).")]
    [SerializeField] private float shutterTimeout = 2f;
    [Tooltip("Segundos máximos esperando ela chegar na porta antes de uma foto.")]
    [SerializeField] private float arriveTimeout = 6f;

    [Header("Flash da câmera")]
    [SerializeField] private Light flashLight;
    [SerializeField] private EventReference flashSound;
    [SerializeField] private float flashIntensity = 160f;
    [SerializeField] private float flashDuration = 0.08f;
    [Tooltip("Opacidade do clarão branco na tela.")]
    [SerializeField, Range(0f, 1f)] private float screenFlash = 0.55f;

    public int PhotosTaken { get; private set; }

    public State Current { get; private set; } = State.Idle;

    /// <summary>Falso congela a Clara onde está (cenas guiadas pelo roteiro); verdadeiro volta a seguir.</summary>
    public bool Following
    {
        get => Current == State.Idle || Current == State.Walking;
        set
        {
            if (value) { if (Current == State.Scripted) Current = State.Idle; }
            else if (Current != State.GoingToDoor && Current != State.AtDoor) Current = State.Scripted;
        }
    }

    /// <summary>Verdadeiro depois de <see cref="StayAtDoor"/>: ela está indo para a porta ou já está lá.</summary>
    public bool AtDoor => Current == State.GoingToDoor || Current == State.AtDoor;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int PhotoParam = Animator.StringToHash("Photo");

    private float walked;
    private Vector3 lastPosition;
    private float smoothedSpeed;
    private bool shutter;
    private bool photographing;
    private Vector3? photoTarget;

    private void Start()
    {
        if (flashLight != null) flashLight.enabled = false;
        Ground();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 moveDirection = Vector3.zero;
        switch (Current)
        {
            case State.Idle:
                if (FlatDistance(player.position) > followDistance + startSlack) Current = State.Walking;
                break;

            case State.Walking:
            {
                float distance = FlatDistance(player.position);
                if (distance <= followDistance + 0.02f) { Current = State.Idle; break; }
                float speed = walkSpeed * Mathf.Lerp(1f, catchUpMultiplier, Mathf.InverseLerp(followDistance + 2f, followDistance + 8f, distance));
                moveDirection = MoveTowards(player.position, Mathf.Min(speed * Time.deltaTime, distance - followDistance));
                if (PastDoor()) { Current = State.Idle; moveDirection = Vector3.zero; }
                break;
            }

            case State.GoingToDoor:
            {
                float distance = FlatDistance(doorSpot.position);
                if (distance <= 0.05f) { Current = State.AtDoor; break; }
                moveDirection = MoveTowards(doorSpot.position, Mathf.Min(walkSpeed * Time.deltaTime, distance));
                break;
            }
        }

        Ground();
        Face(moveDirection);
        UpdateAnimationAndSteps();
    }

    // O ponto da porta é o limite: seguindo Ethan, ela nunca passa dele (o quarto fica do outro lado).
    // Se passou neste quadro, volta para o plano da porta.
    private bool PastDoor()
    {
        if (doorSpot == null) return false;
        float beyond = Vector3.Dot(transform.position - doorSpot.position, doorSpot.forward);
        if (beyond <= 0f) return false;
        transform.position -= doorSpot.forward * beyond;
        return true;
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // Anda até "step" unidades na direção do alvo, escorregando pelas paredes em vez de atravessar
    private Vector3 MoveTowards(Vector3 target, float step)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f || step <= 0f) return Vector3.zero;
        direction.Normalize();

        Vector3 origin = transform.position + Vector3.up * (bodyRadius + 0.5f);
        if (Physics.SphereCast(origin, bodyRadius, direction, out RaycastHit hit, step + 0.05f, obstacles, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(player))
        {
            Vector3 slide = Vector3.ProjectOnPlane(direction, hit.normal);
            slide.y = 0f;
            if (slide.sqrMagnitude < 0.01f) return Vector3.zero;
            direction = slide.normalized;
            if (Physics.SphereCast(origin, bodyRadius, direction, out _, step + 0.05f, obstacles, QueryTriggerInteraction.Ignore)) return Vector3.zero;
        }

        transform.position += direction * step;
        return direction;
    }

    // Pés no chão (antes ela seguia a altura do centro de Ethan e ficava flutuando)
    private void Ground()
    {
        Vector3 from = transform.position + Vector3.up * 4f;
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, 12f, obstacles, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (player != null && hit.transform.IsChildOf(player)) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        if (!float.IsNegativeInfinity(best))
        {
            Vector3 p = transform.position;
            p.y = best;
            transform.position = p;
        }
    }

    // Andando, olha para onde vai; parada, para a foto ou para Ethan
    private void Face(Vector3 moveDirection)
    {
        Vector3 forward;
        if (moveDirection.sqrMagnitude > 0.001f && !photographing) forward = moveDirection;
        else if (Current == State.Scripted && !photographing && photoTarget == null) return;
        else
        {
            forward = (photoTarget ?? player.position) - transform.position;
            forward.y = 0f;
        }
        if (forward.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(forward), turnSpeed * Time.deltaTime);
    }

    private void UpdateAnimationAndSteps()
    {
        Vector3 step = transform.position - lastPosition;
        step.y = 0f;
        if (animator != null && Time.deltaTime > 0f)
        {
            float speed = step.magnitude / Time.deltaTime;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed < 0.15f ? 0f : speed, 1f - Mathf.Exp(-12f * Time.deltaTime));
            animator.SetFloat(SpeedParam, smoothedSpeed / Mathf.Max(0.1f, walkAnimationSpeed));
        }
        walked += step.magnitude;
        lastPosition = transform.position;
        if (walked >= strideLength)
        {
            walked = 0f;
            PlayFootstep();
        }
    }

    private void PlayFootstep()
    {
        if (footstep.IsNull || !RuntimeManager.IsInitialized) return;

        FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(footstep);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        instance.setVolume(footstepVolume);
        instance.start();
        instance.release();
    }

    /// <summary>
    /// Ethan entrou no quarto: Clara vai até a porta e fica ali, olhando para ele. Ela nunca entra.
    /// </summary>
    public void StayAtDoor()
    {
        if (AtDoor) return;
        Current = doorSpot != null ? State.GoingToDoor : State.Scripted;
    }

    /// <summary>Coloca a Clara na porta na hora, sem andar até lá (cortes do roteiro).</summary>
    public void SnapToDoor()
    {
        StayAtDoor();
        if (doorSpot == null) return;
        transform.position = doorSpot.position;
        Ground();
        lastPosition = transform.position;
        Current = State.AtDoor;
    }

    /// <summary>Clara tira uma foto: ergue a câmera, mira e dispara (clarão e som do flash).</summary>
    public void TakePhoto(Vector3? target = null) => StartCoroutine(PhotoRoutine(target));

    /// <summary>A foto inteira, para quem quer esperar o flash: ergue, mira e dispara.</summary>
    public IEnumerator PhotoRoutine(Vector3? target = null)
    {
        yield return RaiseAndShoot(target);
        yield return FlashRoutine(screenFlash, 0f);
    }

    /// <summary>
    /// A última foto da abertura: o clarão toma a tela inteira e fica branco por
    /// <paramref name="holdWhite"/> segundos (o fade de volta fica com quem chamou).
    /// </summary>
    public IEnumerator TakeFinalPhoto(float holdWhite, Vector3? target = null)
    {
        yield return RaiseAndShoot(target);
        yield return FlashRoutine(1f, holdWhite);
    }

    /// <summary>Chamado pelo evento do clipe Clara_Photo (ver <see cref="ClaraAnimationEvents"/>).</summary>
    public void ShutterPressed() => shutter = true;

    // Dispara a animação e espera o dedo apertar o botão
    private IEnumerator RaiseAndShoot(Vector3? target)
    {
        // Indo para a porta: termina de chegar antes de erguer a câmera
        for (float t = 0f; Current == State.GoingToDoor && t < arriveTimeout; t += Time.deltaTime) yield return null;
        if (Current == State.GoingToDoor) SnapToDoor();

        photoTarget = target;
        if (animator == null || !animator.isActiveAndEnabled) yield break;

        photographing = true;
        shutter = false;
        animator.ResetTrigger(PhotoParam);
        animator.SetTrigger(PhotoParam);
        for (float t = 0f; !shutter && t < shutterTimeout; t += Time.deltaTime) yield return null;
        photographing = false;
        StartCoroutine(ClearPhotoTarget());
    }

    // Depois da foto ela volta a olhar para Ethan, quando a câmera já desceu
    private IEnumerator ClearPhotoTarget()
    {
        yield return new WaitForSeconds(1.2f);
        if (!photographing) photoTarget = null;
    }

    private IEnumerator FlashRoutine(float screenAlpha, float holdWhite)
    {
        PhotosTaken++;
        if (!flashSound.IsNull && RuntimeManager.IsInitialized) RuntimeManager.PlayOneShot(flashSound, transform.position);

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
    public void EditorSetupModel(Animator modelAnimator, Transform door)
    {
        animator = modelAnimator;
        doorSpot = door;
    }

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
