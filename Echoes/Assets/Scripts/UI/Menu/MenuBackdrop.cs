using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cenário 3D atrás do menu principal (cena MenuCenario, uma cópia da Pensão Halley sem o roteiro).
/// A câmera faz um movimento lento de ida e volta, com um pouco de respiração e de paralaxe pelo mouse.
/// Há dois enquadramentos para comparar: o quarto 6 visto da porta e o corredor visto da escada.
/// Em build de desenvolvimento e no editor, F2 troca o enquadramento.
/// </summary>
public class MenuBackdrop : MonoBehaviour
{
    public enum Shot { Quarto, Corredor }

    [System.Serializable]
    public class ShotSetup
    {
        [Tooltip("Começo e fim do movimento da câmera.")]
        public Transform from;
        public Transform to;
        [Tooltip("Para onde a câmera vai com o painel de opções aberto (opcional).")]
        public Transform options;
        [Tooltip("Segundos de uma ponta à outra do movimento.")]
        public float duration = 50f;
        public float fieldOfView = 50f;
        public bool policeLights = true;
        public bool roomLight;
        [Tooltip("Lâmpada do fim do corredor (a luz dela entra no quarto pela porta).")]
        public bool corridorLight = true;
        [Tooltip("Abertura da porta 6, em graus (-8 = encostada).")]
        public float door6Angle = -8f;
    }

    /// <summary>O cenário carregado, ou null quando o menu está sem cenário 3D.</summary>
    public static MenuBackdrop Current { get; private set; }

    [SerializeField] private Shot shot = Shot.Quarto;
    [SerializeField] private ShotSetup room = new ShotSetup();
    [SerializeField] private ShotSetup corridor = new ShotSetup();

    [Header("Cena")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private PoliceLights police;
    [SerializeField] private FlickeringLight roomLight;
    [SerializeField] private FlickeringLight corridorLight;
    [SerializeField] private Transform door6;

    [Header("Movimento")]
    [Tooltip("Quanto a câmera acompanha o mouse, em graus.")]
    [SerializeField] private float parallaxDegrees = 1.5f;
    [Tooltip("Respiração da câmera na mão (unidades do mundo). Segue a opção de tremor de câmera.")]
    [SerializeField] private float breathing = 0.05f;
    [SerializeField] private float optionsBlendTime = 1.1f;

    [Header("Som")]
    [SerializeField] private EventReference ambience;

    public Shot CurrentShot => shot;

    /// <summary>Com o painel de opções aberto, a câmera vai para o enquadramento de opções.</summary>
    public bool OptionsOpen { get; set; }

    private ShotSetup Setup => shot == Shot.Quarto ? room : corridor;

    private float startTime;
    private float optionsWeight, optionsVelocity;
    private Vector2 parallax, parallaxVelocity;
    private float push;
    private float seed;
    private FMOD.Studio.EventInstance ambienceInstance;

    private void Awake()
    {
        Current = this;
        seed = Random.value * 100f;
    }

    private void Start()
    {
        Apply(shot);
        if (!ambience.IsNull)
        {
            ambienceInstance = RuntimeManager.CreateInstance(ambience);
            if (menuCamera != null) ambienceInstance.set3DAttributes(RuntimeUtils.To3DAttributes(menuCamera.transform.position));
            ambienceInstance.start();
        }
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambienceInstance.release();
        }
    }

    /// <summary>Troca o enquadramento e o estado da cena (giroflex, luz do quarto, porta 6).</summary>
    public void Apply(Shot newShot)
    {
        shot = newShot;
        ShotSetup setup = Setup;
        if (police != null)
        {
            if (setup.policeLights) police.TurnOn();
            else police.TurnOff();
        }
        if (roomLight != null) roomLight.SetOn(setup.roomLight);
        if (corridorLight != null) corridorLight.SetOn(setup.corridorLight);
        if (door6 != null) door6.rotation = Quaternion.Euler(0f, setup.door6Angle, 0f);
        if (menuCamera != null) menuCamera.fieldOfView = setup.fieldOfView;
        startTime = Time.time;
        UpdateCamera(true);
    }

    private void Update()
    {
        if (Debug.isDebugBuild && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            Apply(shot == Shot.Quarto ? Shot.Corredor : Shot.Quarto);

        UpdateCamera(false);
    }

    private void UpdateCamera(bool instant)
    {
        ShotSetup setup = Setup;
        if (menuCamera == null || setup.from == null || setup.to == null) return;

        // Ida e volta lenta, com as pontas suavizadas
        float t = Mathf.PingPong((Time.time - startTime) / Mathf.Max(1f, setup.duration), 1f);
        t = Mathf.SmoothStep(0f, 1f, t);
        Vector3 position = Vector3.Lerp(setup.from.position, setup.to.position, t);
        Quaternion rotation = Quaternion.Slerp(setup.from.rotation, setup.to.rotation, t);

        float targetWeight = OptionsOpen && setup.options != null ? 1f : 0f;
        optionsWeight = instant ? targetWeight : Mathf.SmoothDamp(optionsWeight, targetWeight, ref optionsVelocity, optionsBlendTime * 0.5f);
        if (setup.options != null && optionsWeight > 0.0001f)
        {
            position = Vector3.Lerp(position, setup.options.position, optionsWeight);
            rotation = Quaternion.Slerp(rotation, setup.options.rotation, optionsWeight);
        }

        position += rotation * Vector3.forward * push;

        // Paralaxe: o mouse puxa o olhar alguns graus
        Vector2 target = Vector2.zero;
        if (Mouse.current != null && Screen.width > 0 && Screen.height > 0)
        {
            Vector2 mouse = Mouse.current.position.ReadValue();
            target = new Vector2(mouse.x / Screen.width * 2f - 1f, mouse.y / Screen.height * 2f - 1f);
            target = Vector2.ClampMagnitude(target, 1.5f);
        }
        parallax = instant ? target : Vector2.SmoothDamp(parallax, target, ref parallaxVelocity, 0.6f);
        rotation *= Quaternion.Euler(-parallax.y * parallaxDegrees, parallax.x * parallaxDegrees, 0f);

        // Respiração de câmera na mão
        float shake = breathing * GameOptions.Current.cameraShake;
        float time = Time.time * 0.25f;
        Vector3 sway = new Vector3(
            Mathf.PerlinNoise(seed, time) - 0.5f,
            Mathf.PerlinNoise(seed + 10f, time) - 0.5f,
            0f) * 2f * shake;
        position += rotation * sway;
        rotation *= Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(seed + 20f, time) - 0.5f) * 40f * shake);

        menuCamera.transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>"Novo jogo": a câmera avança devagar para dentro da cena.</summary>
    public IEnumerator PushIn(float time, float distance = 3f)
    {
        float start = Time.time;
        while (Time.time - start < time)
        {
            float k = (Time.time - start) / time;
            push = distance * k * k;
            yield return null;
        }
        push = distance;
    }

#if UNITY_EDITOR
    public void EditorSetup(Camera cameraComponent, PoliceLights policeLights, FlickeringLight ceilingLight, FlickeringLight hallLight, Transform door, Shot startShot,
        ShotSetup roomShot, ShotSetup corridorShot, EventReference ambienceEvent)
    {
        menuCamera = cameraComponent;
        police = policeLights;
        roomLight = ceilingLight;
        corridorLight = hallLight;
        door6 = door;
        shot = startShot;
        room = roomShot;
        corridor = corridorShot;
        ambience = ambienceEvent;
    }
#endif
}
