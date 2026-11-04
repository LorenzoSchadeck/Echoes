using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Tempo perdido (dissociação): a tela apaga, o relógio salta e, quando Ethan "volta", coisas mudaram
/// de lugar. Dispara ao chegar num ato, quando o jogador entra no collider (trigger) deste objeto,
/// ou por código (<see cref="Trigger"/>). Fica salvo no checkpoint e as mudanças são reaplicadas ao carregar.
/// </summary>
public class LostTimeEvent : MonoBehaviour, ISaveable
{
    [Serializable]
    public class Change
    {
        [Tooltip("Objeto afetado.")]
        public GameObject target;
        [Tooltip("Se definido, o objeto vai para a posição e rotação deste marcador.")]
        public Transform moveTo;
        [Tooltip("Liga ou desliga o objeto.")]
        public bool changeActive;
        public bool active = true;
        [Tooltip("Deixa aberta a porta, gaveta ou armário que estiver no objeto.")]
        public bool open;
    }

    [Header("Quando acontece")]
    [Tooltip("Dispara quando o jogo chega a este ato.")]
    [SerializeField] private bool triggerOnAct;
    [SerializeField] private GameAct act = GameAct.Memories;
    [Tooltip("Dispara quando o jogador entra no collider (marcado como Is Trigger) deste objeto.")]
    [SerializeField] private bool triggerOnPlayerEnter;

    [Header("Apagão")]
    [Tooltip("Quanto tempo Ethan perde, em minutos (aparece nos relógios da cena).")]
    [SerializeField] private float minutesLost = 60f;
    [Tooltip("Quanto tempo a tela fica preta, em segundos.")]
    [SerializeField] private float blackoutSeconds = 3f;
    [Tooltip("Som 2D durante o apagão (zumbido, respiração).")]
    [SerializeField] private EventReference blackoutSound;
    [Tooltip("Se definido, Ethan volta a si neste ponto (ex.: na poltrona).")]
    [SerializeField] private Transform wakeUpPoint;

    [Header("O que mudou quando Ethan volta")]
    [SerializeField] private Change[] changes = Array.Empty<Change>();

    private bool hasTriggered;
    private bool isRunning;

    public static bool IsActive { get; private set; }

    private void OnEnable() => GameFlow.OnActChanged += HandleActChanged;
    private void OnDisable() => GameFlow.OnActChanged -= HandleActChanged;

    private void HandleActChanged(GameAct newAct)
    {
        // Ao carregar um checkpoint o ato também "muda": não é hora de apagar de novo
        if (triggerOnAct && newAct == act && !CheckpointManager.IsRestoring) Trigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnPlayerEnter && other.GetComponentInParent<PlayerMovement>() != null) Trigger();
    }

    /// <summary>
    /// Ethan perde tempo agora.
    /// </summary>
    public void Trigger()
    {
        if (hasTriggered || isRunning) return;
        StartCoroutine(BlackoutRoutine());
    }

    private IEnumerator BlackoutRoutine()
    {
        isRunning = true;
        IsActive = true;
        PlayerControlLock.Acquire(this);

        DeathManager screen = FindAnyObjectByType<DeathManager>();
        if (screen != null) yield return screen.FadeToBlack();

        if (!blackoutSound.IsNull) RuntimeManager.PlayOneShot(blackoutSound);
        yield return new WaitForSeconds(blackoutSeconds);

        ApplyChanges();
        GameClock.Skip(minutesLost);
        if (wakeUpPoint != null) PlayerTeleport.To(wakeUpPoint);

        hasTriggered = true;
        GameEvents.TriggerLostTime(minutesLost);

        if (screen != null) screen.PlayWakeUpFade();
        PlayerControlLock.Release(this);
        IsActive = false;
        isRunning = false;
    }

    private void ApplyChanges()
    {
        foreach (Change change in changes)
        {
            if (change.target == null) continue;

            if (change.moveTo != null)
            {
                change.target.transform.SetPositionAndRotation(change.moveTo.position, change.moveTo.rotation);
            }

            if (change.changeActive) change.target.SetActive(change.active);

            if (change.open)
            {
                change.target.GetComponentInChildren<DoorController>(true)?.OpenImmediately();
                change.target.GetComponentInChildren<DrawerController>(true)?.SetDrawerState(true);
                change.target.GetComponentInChildren<CabinetDoor>(true)?.SetDoorState(true, immediate: true);
            }
        }
    }

    [Serializable]
    private struct SaveState { public bool triggered; }

    public string CaptureState() => JsonUtility.ToJson(new SaveState { triggered = hasTriggered });

    public void RestoreState(string json)
    {
        if (!JsonUtility.FromJson<SaveState>(json).triggered) return;

        // Já aconteceu antes do checkpoint: o mundo continua como Ethan o encontrou
        hasTriggered = true;
        ApplyChanges();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => IsActive = false;
}
