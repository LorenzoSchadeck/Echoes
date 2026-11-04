using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// O caso que Ethan monta: pistas presas no mural e ligações feitas. Guarda só IDs, para o
/// checkpoint gravar listas simples; o mural resolve os IDs com o próprio catálogo.
/// </summary>
public static class CaseFile
{
    private static readonly List<string> evidence = new List<string>();
    private static readonly List<string> deductions = new List<string>();

    public static event Action<Evidence> OnEvidenceAdded;
    public static event Action<Deduction> OnDeductionMade;

    public static IReadOnlyList<string> EvidenceIds => evidence;
    public static IReadOnlyList<string> DeductionIds => deductions;

    public static bool Has(Evidence item) => item != null && evidence.Contains(item.Id);
    public static bool Has(Deduction item) => item != null && deductions.Contains(item.Id);

    /// <summary>
    /// Prende a pista no mural. Falso se ela já estava lá.
    /// </summary>
    public static bool Add(Evidence item)
    {
        if (item == null || Has(item)) return false;

        evidence.Add(item.Id);
        OnEvidenceAdded?.Invoke(item);
        return true;
    }

    /// <summary>
    /// Registra a ligação. Falso se ela já tinha sido feita.
    /// </summary>
    public static bool Connect(Deduction item)
    {
        if (item == null || Has(item)) return false;

        deductions.Add(item.Id);
        OnDeductionMade?.Invoke(item);
        return true;
    }

    /// <summary>
    /// Aplica o estado do checkpoint, sem disparar eventos.
    /// </summary>
    public static void Restore(IEnumerable<string> evidenceIds, IEnumerable<string> deductionIds)
    {
        evidence.Clear();
        deductions.Clear();
        evidence.AddRange(evidenceIds);
        deductions.AddRange(deductionIds);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        evidence.Clear();
        deductions.Clear();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // Uma cena nova começa do zero; o checkpoint, se houver, restaura depois
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        evidence.Clear();
        deductions.Clear();
    }
}
