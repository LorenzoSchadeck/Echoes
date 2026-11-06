using System;
using System.Collections;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

/// <summary>
/// Roteiro da abertura de 1995 na Pensão Halley (aba "Roteiro da demo" do documento de design):
/// tela preta com a central de polícia, o corredor, a porta do quarto 6, a luz que Ethan acende
/// sem procurar, o corpo, as três pistas, a sirene e a última foto de Clara na janela.
/// </summary>
public class OpeningDirector : MonoBehaviour
{
    // Uma pista: o que o jogador examina (às vezes em dois lugares) e o que Ethan e Clara dizem
    private sealed class Clue
    {
        public readonly StoryInteractable[] targets;
        public readonly DialogueLine[] lines;
        public Clue(DialogueLine[] lines, params StoryInteractable[] targets) { this.lines = lines; this.targets = targets; }
    }

    private const string Ethan = "S_ETHAN";
    private const string ClaraSpeaker = "S_CLARA";

    [Header("Jogador e Clara")]
    [SerializeField] private ClaraPresence clara;
    [SerializeField] private Rigidbody playerBody;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private CustomInputAxisController lookInput;

    [Header("Ambiente")]
    [Tooltip("Chuva fina lá fora, em loop até o corte do flash.")]
    [SerializeField] private EventReference rainAmbience;

    [Header("1. Preto")]
    [SerializeField] private EventReference scannerStatic;
    [SerializeField] private DialogueLine dispatch = new DialogueLine("S_DISPATCH", "OP_DISPATCH");
    [SerializeField] private LocalizedString dateCard = new LocalizedString("menu", "M_CARD_1995");

    [Header("2. Corredor")]
    [SerializeField] private DialogueLine claraMinutes = new DialogueLine(ClaraSpeaker, "OP_CLARA_MINUTES");
    [SerializeField] private DialogueLine claraHurry = new DialogueLine(ClaraSpeaker, "OP_CLARA_HURRY");
    [Tooltip("Segundos no corredor antes de Clara apressar Ethan.")]
    [SerializeField] private float hurryAfter = 30f;
    [Tooltip("Topo da escada: Clara está ali e manda Ethan seguir.")]
    [SerializeField] private PlayerZone stairs;
    [Tooltip("Perto da porta 6: Clara não precisa mais apressar.")]
    [SerializeField] private PlayerZone corridorEnd;
    [SerializeField] private PlayerZone door5Zone;
    [Tooltip("O chaveiro vermelho na fechadura da porta 5.")]
    [SerializeField] private Transform door5Key;

    [Header("3. Porta do quarto 6")]
    [SerializeField] private StoryInteractable door6;
    [SerializeField] private Transform door6Pivot;
    [SerializeField] private float door6OpenAngle = -95f;
    [SerializeField] private EventReference doorOpenSound;
    [SerializeField] private DialogueLine[] doorTalk =
    {
        new DialogueLine(ClaraSpeaker, "OP_CLARA_WAIT"),
        new DialogueLine(Ethan, "OP_ETHAN_NOT", 2f),
    };
    [SerializeField] private PlayerZone roomEntry;
    [SerializeField] private GameObject switchOff;
    [SerializeField] private GameObject switchOn;
    [SerializeField] private EventReference switchSound;
    [SerializeField] private FlickeringLight roomLight;
    [SerializeField] private DialogueLine[] switchTalk =
    {
        new DialogueLine(ClaraSpeaker, "OP_CLARA_SWITCH"),
        new DialogueLine(Ethan, "OP_ETHAN_SWITCH"),
    };

    [Header("4. O quarto")]
    [SerializeField] private DialogueLine[] bodyTalk =
    {
        new DialogueLine(ClaraSpeaker, "OP_CLARA_GOD"),
        new DialogueLine(Ethan, "OP_ETHAN_NAME"),
        new DialogueLine(ClaraSpeaker, "OP_CLARA_KNEW"),
        new DialogueLine(Ethan, "OP_ETHAN_WALLET"),
    };

    [Header("5. Pistas, em qualquer ordem")]
    [Tooltip("A gaveta de talheres aberta, na bancada da pia.")]
    [SerializeField] private StoryInteractable drawerClue;
    [SerializeField] private DialogueLine[] drawerLines =
    {
        new DialogueLine(Ethan, "OP_ETHAN_KNIFE"),
    };
    [Tooltip("O rádio-relógio desligado da tomada, parado em 1h47.")]
    [SerializeField] private StoryInteractable clockClue;
    [SerializeField] private DialogueLine[] clockLines =
    {
        new DialogueLine(Ethan, "OP_ETHAN_CLOCK"),
        new DialogueLine(ClaraSpeaker, "OP_CLARA_CLOCK"),
    };
    [Tooltip("O trinco passado por dentro, sem chave na fechadura.")]
    [SerializeField] private StoryInteractable lockClue;
    [Tooltip("A marca sem poeira do chaveiro na cômoda: conta como a mesma pista do trinco.")]
    [SerializeField] private StoryInteractable dustMarkClue;
    [SerializeField] private DialogueLine[] lockLines =
    {
        new DialogueLine(ClaraSpeaker, "OP_CLARA_LOCKED"),
        new DialogueLine(Ethan, "OP_ETHAN_KEY"),
        new DialogueLine(ClaraSpeaker, "OP_CLARA_RED"),
        new DialogueLine(Ethan, "OP_ETHAN_ROOM5"),
    };

    [Header("6. A sirene")]
    [SerializeField] private PoliceLights police;
    [SerializeField] private EventReference sirenSound;
    [SerializeField] private DialogueLine claraPolice = new DialogueLine(ClaraSpeaker, "OP_CLARA_POLICE");
    [SerializeField] private StoryInteractable window;
    [Tooltip("Onde Ethan para no parapeito; o eixo azul aponta para dentro do quarto.")]
    [SerializeField] private Transform windowSpot;
    [SerializeField] private DialogueLine claraPhoto = new DialogueLine(ClaraSpeaker, "OP_CLARA_PHOTO");

    [Header("7. Corte")]
    [SerializeField] private EventReference thunderSound;
    [SerializeField] private LocalizedString laterCard = new LocalizedString("menu", "M_CARD_LATER");
    [SerializeField] private string nextScene = "Game";

    [Header("Pular a abertura")]
    [Tooltip("Segundos segurando Espaço (ou Y no controle) para pular direto para 2003.")]
    [SerializeField] private float skipHoldTime = 1.5f;
    [Tooltip("Segundos em que o aviso de pular aparece no começo.")]
    [SerializeField] private float skipHintAtStart = 8f;
    [SerializeField] private LocalizedString skipHint = new LocalizedString("menu", "M_SKIP_HOLD");

    private float corridorStartedAt = -1f;
    private bool hurrySaid;
    private float lastHurryAt = -100f;
    private bool door5Seen;
    private bool doorOpened;
    private bool lightOn;
    private int cluesFound;
    private FMOD.Studio.EventInstance siren;
    private FMOD.Studio.EventInstance rain;
    private Clue[] clues = Array.Empty<Clue>();
    private float skipHeld;
    private float skipHintUntil;
    private bool skipping;
    private bool exiting;

    private void Awake()
    {
        clues = new[]
        {
            new Clue(drawerLines, drawerClue),
            new Clue(clockLines, clockClue),
            new Clue(lockLines, lockClue, dustMarkClue),
        };
    }

    private IEnumerable AllClueTargets()
    {
        foreach (Clue clue in clues)
        {
            foreach (StoryInteractable target in clue.targets)
            {
                if (target != null) yield return target;
            }
        }
    }

    private void OnEnable()
    {
        if (stairs != null) stairs.Entered += HandleStairs;
        if (corridorEnd != null) corridorEnd.Entered += HandleCorridorEnd;
        if (door5Zone != null) door5Zone.Entered += HandleDoor5;
        if (door6 != null) door6.Used += HandleDoor6;
        if (roomEntry != null) roomEntry.Entered += HandleRoomEntry;
        if (window != null) window.Used += HandleWindow;
        foreach (StoryInteractable target in AllClueTargets()) target.Used += HandleClue;
    }

    private void OnDisable()
    {
        if (stairs != null) stairs.Entered -= HandleStairs;
        if (corridorEnd != null) corridorEnd.Entered -= HandleCorridorEnd;
        if (door5Zone != null) door5Zone.Entered -= HandleDoor5;
        if (door6 != null) door6.Used -= HandleDoor6;
        if (roomEntry != null) roomEntry.Entered -= HandleRoomEntry;
        if (window != null) window.Used -= HandleWindow;
        foreach (StoryInteractable target in AllClueTargets()) target.Used -= HandleClue;
        StopLoops();
    }

    // ---------- 1. Preto ----------

    private IEnumerator Start()
    {
        GameFlow.BeginOpening();
        skipHintUntil = Time.time + skipHintAtStart;
        foreach (StoryInteractable target in AllClueTargets()) target.Available = false;
        if (window != null) window.Available = false;
        if (roomLight != null) roomLight.SetOn(false);

        if (!rainAmbience.IsNull)
        {
            rain = RuntimeManager.CreateInstance(rainAmbience);
            Vector3 at = window != null ? window.transform.position : playerCamera.position;
            rain.set3DAttributes(RuntimeUtils.To3DAttributes(at));
            rain.start();
        }

        PlayerControlLock.Acquire(this);
        SetLook(false);
        GameHud.FadeScreen.alpha = 1f;
        GameHud.FlashScreen.alpha = 0f;
        GameHud.EndText.alpha = 0f;

        yield return new WaitForSeconds(1f);
        PlayAt(scannerStatic, playerCamera.position);
        yield return new WaitForSeconds(0.8f);
        yield return Dialogue.Play(dispatch);
        yield return ShowCard(dateCard, 3.5f);

        PlayerControlLock.Release(this);
        SetLook(true);
        GameHud.Crosshair.enabled = true;
        corridorStartedAt = Time.time;
        StartCoroutine(Fade.Alpha(GameHud.FadeScreen, 0f, 2f, smooth: true));
        yield return new WaitForSeconds(1.2f);
        yield return Dialogue.Play(claraMinutes);
    }

    // ---------- 2. Corredor ----------

    private void Update()
    {
        UpdateSkip();
        if (skipping) return;
        if (hurrySaid || doorOpened || corridorStartedAt < 0f) return;
        if (Time.time - corridorStartedAt >= hurryAfter && !Dialogue.IsPlaying) Hurry();
    }

    private void HandleStairs(PlayerZone zone)
    {
        if (!doorOpened && corridorStartedAt >= 0f && Time.time - lastHurryAt > 10f && !Dialogue.IsPlaying) Hurry();
    }

    private void HandleCorridorEnd(PlayerZone zone) => hurrySaid = true;

    private void Hurry()
    {
        hurrySaid = true;
        lastHurryAt = Time.time;
        StartCoroutine(Dialogue.Play(claraHurry));
    }

    // Ao passar pela porta 5, o olhar se demora no chaveiro vermelho
    private void HandleDoor5(PlayerZone zone)
    {
        if (door5Seen || door5Key == null || doorOpened) return;
        door5Seen = true;
        StartCoroutine(LingerOn(door5Key.position, 0.8f, 1f));
    }

    private IEnumerator LingerOn(Vector3 target, float blend, float hold)
    {
        PlayerControlLock.Acquire(this);
        SetLook(false);
        yield return TurnCameraTowards(target, blend);
        yield return new WaitForSeconds(hold);
        SetLook(true);
        PlayerControlLock.Release(this);
    }

    // ---------- 3. Porta do quarto 6 ----------

    private void HandleDoor6(StoryInteractable door) => StartCoroutine(OpenDoorRoutine());

    private IEnumerator OpenDoorRoutine()
    {
        doorOpened = true;
        yield return Dialogue.Play(doorTalk);

        PlayAt(doorOpenSound, door6Pivot.position);
        Quaternion from = door6Pivot.localRotation;
        Quaternion to = from * Quaternion.Euler(0f, door6OpenAngle, 0f);
        for (float t = 0f; t < 1.8f; t += Time.deltaTime)
        {
            door6Pivot.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / 1.8f));
            yield return null;
        }
        door6Pivot.localRotation = to;
    }

    // Ethan estica a mão para a direita e acende a luz, sem procurar
    private void HandleRoomEntry(PlayerZone zone)
    {
        if (!doorOpened || lightOn) return;
        lightOn = true;
        StartCoroutine(LightRoutine());
    }

    private IEnumerator LightRoutine()
    {
        PlayAt(switchSound, switchOn != null ? switchOn.transform.position : playerCamera.position);
        if (switchOff != null) switchOff.SetActive(false);
        if (switchOn != null) switchOn.SetActive(true);
        if (roomLight != null) roomLight.SetOn(true);

        yield return new WaitForSeconds(0.7f);
        yield return Dialogue.Play(switchTalk);

        // ---------- 4. O quarto ----------
        yield return new WaitForSeconds(0.8f);
        yield return Dialogue.Play(bodyTalk);

        foreach (StoryInteractable target in AllClueTargets()) target.Available = true;
    }

    // ---------- 5. Pistas ----------

    private void HandleClue(StoryInteractable target)
    {
        foreach (Clue clue in clues)
        {
            if (Array.IndexOf(clue.targets, target) < 0) continue;

            // A mesma pista vista em outro lugar não conta de novo
            foreach (StoryInteractable other in clue.targets)
            {
                if (other != null) other.Available = false;
            }
            StartCoroutine(ClueRoutine(clue));
            return;
        }
    }

    // Cada pista examinada: o flash de Clara, a foto vai para o bloco dela e os dois comentam
    private IEnumerator ClueRoutine(Clue clue)
    {
        if (clara != null) clara.TakePhoto();
        yield return new WaitForSeconds(0.6f);
        yield return Dialogue.Play(clue.lines);

        cluesFound++;
        if (cluesFound == clues.Length) StartCoroutine(SirenRoutine());
    }

    // ---------- 6. A sirene ----------

    private IEnumerator SirenRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (police != null) police.TurnOn();
        if (!sirenSound.IsNull)
        {
            siren = RuntimeManager.CreateInstance(sirenSound);
            Vector3 at = police != null ? police.transform.position : playerCamera.position;
            siren.set3DAttributes(RuntimeUtils.To3DAttributes(at));
            siren.start();
        }

        yield return new WaitForSeconds(2.5f);
        yield return Dialogue.Play(claraPolice);
        if (window != null) window.Available = true;
    }

    private void HandleWindow(StoryInteractable target)
    {
        exiting = true;
        StartCoroutine(ExitRoutine());
    }

    // ---------- Pular ----------

    // Segurar Espaço (ou Y) pula a abertura; o aviso aparece no começo e enquanto a tecla está apertada
    private void UpdateSkip()
    {
        if (skipping || exiting) return;

        bool held = Time.timeScale > 0f
            && ((Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                || (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed));
        if (held && skipHeld <= 0f) skipHintUntil = Mathf.Max(skipHintUntil, Time.time + 3f);
        skipHeld = held ? skipHeld + Time.unscaledDeltaTime : Mathf.Max(0f, skipHeld - Time.unscaledDeltaTime * 2f);

        float progress = Mathf.Clamp01(skipHeld / Mathf.Max(0.1f, skipHoldTime));
        bool show = progress > 0f || Time.time < skipHintUntil;
        GameHud.SkipHint.enabled = show;
        if (show)
        {
            GameHud.SkipHint.text = skipHint.GetLocalizedString();
            GameHud.SkipHint.alpha = 0.35f + 0.65f * progress;
        }

        if (progress >= 1f)
        {
            skipping = true;
            StopAllCoroutines();
            StartCoroutine(SkipRoutine());
        }
    }

    private IEnumerator SkipRoutine()
    {
        GameHud.SkipHint.enabled = false;
        Dialogue.Stop();
        PlayerControlLock.Acquire(this);
        SetLook(false);
        GameHud.Prompt.enabled = false;
        GameHud.Crosshair.enabled = false;
        if (clara != null) clara.Following = false;

        GameHud.EndText.alpha = 0f;
        yield return Fade.Alpha(GameHud.FadeScreen, 1f, 0.8f, smooth: true);
        GameHud.FlashScreen.alpha = 0f;
        StopLoops();
        yield return ShowCard(laterCard, 1.5f);

        PlayerControlLock.Release(this);
        SceneManager.LoadScene(nextScene);
    }

    // Ethan para no parapeito, de costas para a viatura, e Clara tira a foto da capa
    private IEnumerator ExitRoutine()
    {
        PlayerControlLock.Acquire(this);
        SetLook(false);
        GameHud.Crosshair.enabled = false;
        GameHud.Prompt.enabled = false;

        if (clara != null && windowSpot != null)
        {
            clara.Following = false;
            clara.transform.position = windowSpot.position + windowSpot.forward * 7f;
            clara.transform.rotation = Quaternion.LookRotation(-windowSpot.forward);
        }

        if (playerBody != null && windowSpot != null)
        {
            playerBody.isKinematic = true;
            Vector3 from = playerBody.position;
            Vector3 to = new Vector3(windowSpot.position.x, from.y, windowSpot.position.z);
            Vector3 lookAt = windowSpot.position + windowSpot.forward * 10f + Vector3.up * (playerCamera.position.y - windowSpot.position.y);
            StartCoroutine(TurnCameraTowards(lookAt, 1.4f));
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                playerBody.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / 1.4f));
                yield return null;
            }
            playerBody.transform.position = to;
        }

        yield return new WaitForSeconds(0.4f);
        yield return Dialogue.Play(claraPhoto);
        yield return new WaitForSeconds(0.5f);

        // ---------- 7. Corte ----------
        // O branco do flash vira o branco de um relâmpago
        if (clara != null) yield return clara.TakeFinalPhoto(0.3f);
        else GameHud.FlashScreen.alpha = 1f;
        StopLoops();
        PlayAt(thunderSound, playerCamera.position);
        yield return new WaitForSeconds(1.8f);

        GameHud.FadeScreen.alpha = 1f;
        yield return Fade.Alpha(GameHud.FlashScreen, 0f, 1.6f, smooth: true);
        yield return ShowCard(laterCard, 2.5f);

        PlayerControlLock.Release(this);
        SceneManager.LoadScene(nextScene);
    }

    // ---------- Apoio ----------

    private IEnumerator ShowCard(LocalizedString card, float hold)
    {
        if (card == null || card.IsEmpty) yield break;
        GameHud.EndText.text = card.GetLocalizedString();
        yield return Fade.Alpha(GameHud.EndText, 1f, 1.2f);
        yield return new WaitForSeconds(hold);
        yield return Fade.Alpha(GameHud.EndText, 0f, 1.2f);
    }

    private void SetLook(bool enabled)
    {
        if (lookInput != null) lookInput.enabled = enabled;
    }

    // Gira o olhar até o ponto, pelos eixos do Pan/Tilt (que controlam a câmera do jogador)
    private IEnumerator TurnCameraTowards(Vector3 target, float duration)
    {
        if (panTilt == null || playerCamera == null) yield break;

        Vector3 direction = target - playerCamera.position;
        float worldYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float worldPitch = -Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

        // O Pan/Tilt pode estar num referencial diferente do mundo: compensa a diferença atual
        float panOffset = Mathf.DeltaAngle(playerCamera.eulerAngles.y, panTilt.PanAxis.Value);
        float tiltOffset = panTilt.TiltAxis.Value - Mathf.DeltaAngle(0f, playerCamera.eulerAngles.x);

        float startPan = panTilt.PanAxis.Value;
        float startTilt = panTilt.TiltAxis.Value;
        float endPan = worldYaw + panOffset;
        float endTilt = worldPitch + tiltOffset;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float s = Mathf.SmoothStep(0f, 1f, t / duration);
            panTilt.PanAxis.Value = Mathf.DeltaAngle(0f, Mathf.LerpAngle(startPan, endPan, s));
            panTilt.TiltAxis.Value = Mathf.Lerp(startTilt, endTilt, s);
            yield return null;
        }
        panTilt.PanAxis.Value = Mathf.DeltaAngle(0f, endPan);
        panTilt.TiltAxis.Value = endTilt;
    }

    private static void PlayAt(EventReference sound, Vector3 position)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound, position);
    }

    // Sirene e chuva param no corte para 2003
    private void StopLoops()
    {
        Stop(ref siren);
        Stop(ref rain);
    }

    private static void Stop(ref FMOD.Studio.EventInstance instance)
    {
        if (!instance.isValid()) return;
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
        instance.clearHandle();
    }

#if UNITY_EDITOR
    /// <summary>Referências do jogador e de Clara (o resto é ligado pelo OpeningSetupTools).</summary>
    public void EditorSetupPlayer(ClaraPresence claraPresence, Rigidbody body, Transform cameraTransform, CinemachinePanTilt look, CustomInputAxisController input)
    {
        clara = claraPresence;
        playerBody = body;
        playerCamera = cameraTransform;
        panTilt = look;
        lookInput = input;
    }
#endif
}
