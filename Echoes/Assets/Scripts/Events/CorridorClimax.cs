using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Fim do Ato 1: passos no corredor param na porta de Ethan, uma sombra aparece na fresta e o olho
/// mágico mostra o 3B entreaberto, com luz acesa e pegadas molhadas. Olhando ou não, a luz cai e
/// Ethan perde a primeira hora (o LostTimeEvent). Dispara quando o jogador fecha as conclusões do
/// mural ou, se ele demorar, depois de um tempo no ato.
/// </summary>
public class CorridorClimax : MonoBehaviour, ISaveable
{
    [Header("Quando acontece")]
    [SerializeField] private GameAct act = GameAct.Investigation;
    [Tooltip("Conclusões do mural que disparam o clímax.")]
    [SerializeField] private int deductionsNeeded = 2;
    [Tooltip("Se o jogador não fechar as conclusões, dispara depois destes segundos no ato.")]
    [SerializeField] private float fallbackSeconds = 480f;

    [Header("Corredor")]
    [Tooltip("Pivô da porta do 3B.")]
    [SerializeField] private Transform neighborDoor;
    [Tooltip("Somado aos ângulos da porta fechada, como faz o NeighborEventManager.")]
    [SerializeField] private Vector3 neighborDoorAjar = new Vector3(0f, -25f, 0f);
    [Tooltip("Luz acesa dentro do 3B e pegadas no corredor.")]
    [SerializeField] private GameObject[] hallwayObjects = Array.Empty<GameObject>();
    [SerializeField] private EventReference footstep;
    [Tooltip("Os passos vão do primeiro ao último ponto (da porta do 3B à porta de Ethan).")]
    [SerializeField] private Transform stepsFrom;
    [SerializeField] private Transform stepsTo;
    [SerializeField] private int steps = 6;
    [SerializeField] private float stepInterval = 0.6f;
    [Tooltip("Fresta de luz sob a porta, sem e com a sombra dos pés.")]
    [SerializeField] private GameObject doorGapLight;
    [SerializeField] private GameObject doorGapShadow;
    [SerializeField] private InnerVoiceLine whoIsThere = new InnerVoiceLine();

    [Header("Olho mágico e queda de luz")]
    [SerializeField] private Peephole peephole;
    [Tooltip("Quanto tempo a cena espera o jogador ir ao olho mágico.")]
    [SerializeField] private float waitForPeek = 25f;
    [Tooltip("Quanto tempo o jogador vê o corredor antes de a luz cair.")]
    [SerializeField] private float peekHold = 4f;
    [SerializeField] private EventReference powerOutSound;
    [SerializeField] private EventReference thunderSound;
    [SerializeField] private LostTimeEvent lostTime;

    private bool done;
    private bool running;
    private bool inFlashback;
    private float actStartTime = -1f;
    private Vector3 doorClosed;

    private void Awake()
    {
        if (neighborDoor != null) doorClosed = neighborDoor.localEulerAngles;
    }

    private void OnEnable()
    {
        GameFlow.OnActChanged += HandleActChanged;
        CaseFile.OnDeductionMade += HandleDeduction;
        GameEvents.OnFlashbackStarted += HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded += HandleFlashbackEnded;
    }

    private void OnDisable()
    {
        GameFlow.OnActChanged -= HandleActChanged;
        CaseFile.OnDeductionMade -= HandleDeduction;
        GameEvents.OnFlashbackStarted -= HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded -= HandleFlashbackEnded;
    }

    private void Start()
    {
        SetHallway(false);
        if (GameFlow.Current >= act) actStartTime = Time.time;
    }

    private void HandleActChanged(GameAct newAct)
    {
        if (newAct >= act && actStartTime < 0f) actStartTime = Time.time;
    }

    private void HandleDeduction(Deduction deduction)
    {
        if (CaseFile.DeductionIds.Count >= deductionsNeeded) TryStart();
    }

    private void HandleFlashbackStarted() => inFlashback = true;
    private void HandleFlashbackEnded() => inFlashback = false;

    private void Update()
    {
        if (!done && !running && actStartTime >= 0f && Time.time - actStartTime >= fallbackSeconds) TryStart();
    }

    private void TryStart()
    {
        if (done || running || GameFlow.Current < act) return;
        running = true;
        StartCoroutine(ClimaxRoutine());
    }

    private IEnumerator ClimaxRoutine()
    {
        // Espera um momento calmo: fora de lembranças, apagões, mural e carregamentos
        while (inFlashback || LostTimeEvent.IsActive || CheckpointManager.IsRestoring || EvidenceBoardUI.IsOpen)
            yield return null;

        // Os eventos aleatórios do vizinho param: daqui em diante o corredor é deste momento
        NeighborEventManager.Instance?.StopNeighborEvents();
        SetHallway(true);
        yield return OpenNeighborDoor();

        // Alguém atravessa o corredor e para na porta de Ethan
        for (int i = 0; i < steps; i++)
        {
            float t = steps > 1 ? i / (float)(steps - 1) : 1f;
            Vector3 position = Vector3.Lerp(stepsFrom.position, stepsTo.position, t);
            if (!footstep.IsNull) RuntimeManager.PlayOneShot(footstep, position);
            yield return new WaitForSeconds(stepInterval);
        }

        if (doorGapLight != null) doorGapLight.SetActive(false);
        if (doorGapShadow != null) doorGapShadow.SetActive(true);
        yield return new WaitForSeconds(0.8f);
        InnerVoice.Say(whoIsThere);

        // O jogador decide se olha; olhando, vê o corredor por alguns segundos
        float waited = 0f;
        while (waited < waitForPeek && (peephole == null || !peephole.IsPeeking))
        {
            waited += Time.deltaTime;
            yield return null;
        }
        if (peephole != null && peephole.IsPeeking) yield return new WaitForSeconds(peekHold);

        // A tempestade derruba a luz do prédio
        if (!powerOutSound.IsNull) RuntimeManager.PlayOneShot(powerOutSound);
        if (!thunderSound.IsNull) RuntimeManager.PlayOneShot(thunderSound);
        GameHud.FadeScreen.alpha = 1f;
        if (peephole != null) peephole.StopPeekingIfActive();
        if (doorGapShadow != null) doorGapShadow.SetActive(false);
        if (doorGapLight != null) doorGapLight.SetActive(true);
        yield return new WaitForSeconds(1.5f);

        done = true;
        running = false;
        if (lostTime != null) lostTime.Trigger();
    }

    private IEnumerator OpenNeighborDoor()
    {
        if (neighborDoor == null) yield break;

        Quaternion closed = Quaternion.Euler(doorClosed);
        Quaternion target = Quaternion.Euler(doorClosed + neighborDoorAjar);
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            neighborDoor.localRotation = Quaternion.Slerp(closed, target, Mathf.SmoothStep(0f, 1f, t / 1.5f));
            yield return null;
        }
        neighborDoor.localRotation = target;
    }

    private void SetHallway(bool on)
    {
        foreach (GameObject item in hallwayObjects)
        {
            if (item != null) item.SetActive(on);
        }
        if (doorGapShadow != null) doorGapShadow.SetActive(false);
        if (doorGapLight != null) doorGapLight.SetActive(true);
        if (neighborDoor != null && !on) neighborDoor.localEulerAngles = doorClosed;
    }

    [Serializable]
    private struct SaveState { public bool done; }

    public string CaptureState() => JsonUtility.ToJson(new SaveState { done = done });

    public void RestoreState(string json)
    {
        if (!JsonUtility.FromJson<SaveState>(json).done) return;

        // Já aconteceu: o corredor fica como Ethan o deixou
        done = true;
        SetHallway(true);
        if (neighborDoor != null) neighborDoor.localEulerAngles = doorClosed + neighborDoorAjar;
    }

#if UNITY_EDITOR
    public void EditorSetup(Transform door, GameObject[] hallway, EventReference step, Transform from, Transform to,
        GameObject gapLight, GameObject gapShadow, InnerVoiceLine line, Peephole hole, EventReference powerOut,
        EventReference thunder, LostTimeEvent lost)
    {
        neighborDoor = door;
        hallwayObjects = hallway;
        footstep = step;
        stepsFrom = from;
        stepsTo = to;
        doorGapLight = gapLight;
        doorGapShadow = gapShadow;
        whoIsThere = line;
        peephole = hole;
        powerOutSound = powerOut;
        thunderSound = thunder;
        lostTime = lost;
    }
#endif
}
