using System;
using UnityEngine;

/// <summary>
/// Dispara falas da voz interna num momento do jogo. As falas saem em sequência: a primeira vez
/// diz a primeira, a segunda vez a segunda (útil para uma dica) e assim por diante.
/// Salvo no checkpoint: o que Ethan já pensou não se repete depois de um colapso.
/// </summary>
public class InnerVoiceTrigger : MonoBehaviour, ISaveable
{
    public enum Moment
    {
        PlayerEnters,      // o jogador entra no collider (Is Trigger) deste objeto
        ItemExamined,      // o jogador examina o item indicado
        ActReached,        // o jogo chega ao ato indicado
        WokeFromCollapse,  // Ethan acorda num checkpoint depois de um colapso
        LostTime,          // Ethan perde tempo (apagão)
        RemedyUsed,        // Ethan toma um remédio
        SanityBelow,       // a sanidade cai abaixo do limite indicado
    }

    [SerializeField] private Moment when = Moment.PlayerEnters;

    [Tooltip("Para 'Item Examined': o item que dispara a fala.")]
    [SerializeField] private ItemInteract item;
    [Tooltip("Para 'Act Reached'.")]
    [SerializeField] private GameAct act = GameAct.Investigation;
    [Tooltip("Para 'Sanity Below': dispara ao cruzar este limite para baixo.")]
    [SerializeField, Range(0f, 1f)] private float sanityThreshold = 0.3f;

    [Tooltip("Falas em ordem: 1ª vez, 2ª vez... Depois da última, para (ou repete a última).")]
    [SerializeField] private InnerVoiceLine[] lines = Array.Empty<InnerVoiceLine>();
    [SerializeField] private bool repeatLastLine;
    [Tooltip("Tempo mínimo entre duas falas deste gatilho, em segundos.")]
    [SerializeField] private float cooldownSeconds = 15f;

    private int timesSaid;
    private float lastSaidTime = float.NegativeInfinity;
    private bool belowThreshold;

    private void OnEnable()
    {
        GameEvents.OnItemExamined += HandleItemExamined;
        GameEvents.OnWokeFromCollapse += HandleWokeFromCollapse;
        GameEvents.OnLostTime += HandleLostTime;
        GameEvents.OnRemedyUsed += HandleRemedyUsed;
        GameFlow.OnActChanged += HandleActChanged;
        InsanityManager.OnSanityChanged += HandleSanityChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnItemExamined -= HandleItemExamined;
        GameEvents.OnWokeFromCollapse -= HandleWokeFromCollapse;
        GameEvents.OnLostTime -= HandleLostTime;
        GameEvents.OnRemedyUsed -= HandleRemedyUsed;
        GameFlow.OnActChanged -= HandleActChanged;
        InsanityManager.OnSanityChanged -= HandleSanityChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (when == Moment.PlayerEnters && other.GetComponentInParent<PlayerMovement>() != null) Speak();
    }

    private void HandleItemExamined(ItemInteract examined)
    {
        if (when == Moment.ItemExamined && examined == item) Speak();
    }

    private void HandleWokeFromCollapse() { if (when == Moment.WokeFromCollapse) Speak(); }
    private void HandleLostTime(float _) { if (when == Moment.LostTime) Speak(); }
    private void HandleRemedyUsed() { if (when == Moment.RemedyUsed) Speak(); }

    private void HandleActChanged(GameAct newAct)
    {
        if (when == Moment.ActReached && newAct == act && !CheckpointManager.IsRestoring) Speak();
    }

    private void HandleSanityChanged(float sanity)
    {
        if (when != Moment.SanityBelow) return;

        if (!belowThreshold && sanity < sanityThreshold)
        {
            belowThreshold = true;
            Speak();
        }
        // Rearma com folga, para não falar de novo a cada oscilação perto do limite
        else if (belowThreshold && sanity > sanityThreshold + 0.1f)
        {
            belowThreshold = false;
        }
    }

    /// <summary>
    /// Fala a próxima linha da sequência, respeitando o tempo mínimo entre falas.
    /// </summary>
    public void Speak()
    {
        if (lines.Length == 0 || Time.time - lastSaidTime < cooldownSeconds) return;
        if (timesSaid >= lines.Length && !repeatLastLine) return;

        InnerVoice.Say(lines[Mathf.Min(timesSaid, lines.Length - 1)]);
        timesSaid++;
        lastSaidTime = Time.time;
    }

    [Serializable]
    private struct SaveState { public int timesSaid; }

    public string CaptureState() => JsonUtility.ToJson(new SaveState { timesSaid = timesSaid });

    public void RestoreState(string json) => timesSaid = JsonUtility.FromJson<SaveState>(json).timesSaid;

#if UNITY_EDITOR
    /// <summary>
    /// Configuração por ferramenta de editor.
    /// </summary>
    public void EditorSetup(Moment moment, InnerVoiceLine[] newLines, bool repeatLast, float cooldown = 15f, float threshold = 0.3f)
    {
        when = moment;
        lines = newLines;
        repeatLastLine = repeatLast;
        cooldownSeconds = cooldown;
        sanityThreshold = threshold;
    }
#endif
}
