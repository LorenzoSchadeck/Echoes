using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Uma ligação válida entre duas pistas. O barbante só fica no mural quando o jogador liga
/// exatamente este par; a conclusão passa a valer para a história.
/// O nome do asset é o ID salvo no checkpoint.
/// </summary>
[CreateAssetMenu(menuName = "Echoes/Mural/Dedução", fileName = "DED_")]
public class Deduction : ScriptableObject
{
    public Evidence first;
    public Evidence second;
    public LocalizedString conclusion = new LocalizedString();
    [Tooltip("O que Ethan pensa ao fazer a ligação. Opcional.")]
    public InnerVoiceLine reaction = new InnerVoiceLine();

    public string Id => name;

    public bool Links(Evidence a, Evidence b) => (a == first && b == second) || (a == second && b == first);
}
