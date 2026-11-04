using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Torna o vizinho concreto: um tempo depois do ato indicado, alguém bate na porta de entrada
/// e passa um bilhete por baixo dela. Quanto mais real ele parecer agora, mais pesa descobrir
/// depois que o apartamento ao lado está vazio. Salvo no checkpoint: o bilhete chega uma vez só.
/// </summary>
public class NeighborNoteDelivery : MonoBehaviour, ISaveable
{
    [Tooltip("Ato a partir do qual o bilhete pode chegar.")]
    [SerializeField] private GameAct act = GameAct.Investigation;
    [Tooltip("Segundos depois do ato até baterem na porta.")]
    [SerializeField] private float delaySeconds = 45f;

    [Tooltip("Onde as batidas e o papel deslizando são ouvidos (a porta de entrada).")]
    [SerializeField] private Transform door;
    [SerializeField] private EventReference knockSound;
    [Tooltip("Segundos entre as batidas e o bilhete passar por baixo da porta.")]
    [SerializeField] private float noteDelay = 2.5f;
    [SerializeField] private EventReference slideSound;

    [Tooltip("O bilhete no chão, atrás da porta. Fica inativo até chegar.")]
    [SerializeField] private GameObject note;
    [Tooltip("O que Ethan pensa ao ouvir o papel passar. Opcional.")]
    [SerializeField] private InnerVoiceLine thought = new InnerVoiceLine();

    private bool delivered;
    private bool inFlashback;
    private Coroutine routine;

    private void OnEnable()
    {
        GameFlow.OnActChanged += HandleActChanged;
        GameEvents.OnFlashbackStarted += HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded += HandleFlashbackEnded;
    }

    private void OnDisable()
    {
        GameFlow.OnActChanged -= HandleActChanged;
        GameEvents.OnFlashbackStarted -= HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded -= HandleFlashbackEnded;
    }

    private void Start()
    {
        if (note != null && !delivered) note.SetActive(false);
        if (GameFlow.Current >= act) Schedule();
    }

    private void HandleActChanged(GameAct newAct)
    {
        if (newAct >= act) Schedule();
    }

    private void HandleFlashbackStarted() => inFlashback = true;
    private void HandleFlashbackEnded() => inFlashback = false;

    private void Schedule()
    {
        if (delivered || routine != null) return;
        routine = StartCoroutine(DeliverRoutine());
    }

    private IEnumerator DeliverRoutine()
    {
        yield return new WaitForSeconds(delaySeconds);

        // Ninguém bate na porta no meio de uma lembrança ou de um apagão
        while (inFlashback || LostTimeEvent.IsActive || CheckpointManager.IsRestoring) yield return null;

        Vector3 doorPosition = door != null ? door.position : transform.position;
        if (!knockSound.IsNull) RuntimeManager.PlayOneShot(knockSound, doorPosition);

        yield return new WaitForSeconds(noteDelay);

        if (!slideSound.IsNull) RuntimeManager.PlayOneShot(slideSound, doorPosition);
        Deliver();

        yield return new WaitForSeconds(1.5f);
        InnerVoice.Say(thought);
        routine = null;
    }

    private void Deliver()
    {
        delivered = true;
        if (note != null) note.SetActive(true);
    }

    [Serializable]
    private struct SaveState { public bool delivered; }

    public string CaptureState() => JsonUtility.ToJson(new SaveState { delivered = delivered });

    public void RestoreState(string json)
    {
        if (!JsonUtility.FromJson<SaveState>(json).delivered) return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        Deliver();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Configuração por ferramenta de editor.
    /// </summary>
    public void EditorSetup(Transform newDoor, GameObject newNote, EventReference knock, EventReference slide, InnerVoiceLine newThought)
    {
        door = newDoor;
        note = newNote;
        knockSound = knock;
        slideSound = slide;
        thought = newThought;
    }
#endif
}
