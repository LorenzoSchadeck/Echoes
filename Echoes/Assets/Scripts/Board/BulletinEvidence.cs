using System.Collections;
using UnityEngine;

/// <summary>
/// Quando o primeiro boletim do rádio termina, Ethan anota o que ouviu: a anotação vira pista
/// no mural. Não precisa de save: a pista fica no CaseFile e o boletim só toca uma vez.
/// </summary>
public class BulletinEvidence : MonoBehaviour
{
    [SerializeField] private Evidence evidence;
    [Tooltip("Segundos depois do fim do boletim, para não cobrir a fala de Ethan sobre ele.")]
    [SerializeField] private float delay = 5f;
    [Tooltip("O que Ethan pensa antes de anotar.")]
    [SerializeField] private InnerVoiceLine thought = new InnerVoiceLine();

    private void OnEnable() => GameEvents.OnRadioTrack1Completed += HandleTrackCompleted;

    private void OnDisable() => GameEvents.OnRadioTrack1Completed -= HandleTrackCompleted;

    private void HandleTrackCompleted()
    {
        if (!CaseFile.Has(evidence)) StartCoroutine(TakeNote());
    }

    private IEnumerator TakeNote()
    {
        yield return new WaitForSeconds(delay);
        InnerVoice.Say(thought);
        CaseFile.Add(evidence);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Configuração por ferramenta de editor.
    /// </summary>
    public void EditorSetup(Evidence newEvidence, InnerVoiceLine newThought)
    {
        evidence = newEvidence;
        thought = newThought;
    }
#endif
}
