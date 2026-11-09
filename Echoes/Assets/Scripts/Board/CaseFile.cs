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
    private static readonly List<BoardPlacement> placements = new List<BoardPlacement>();
    private static readonly List<BoardLink> links = new List<BoardLink>();
    private static readonly Dictionary<string, GameObject> visualSources = new Dictionary<string, GameObject>();
    public static event Action OnBoardChanged;
    public static void RegisterVisualSource(Evidence clue, GameObject source)
    {
        if (clue != null && source != null) visualSources[clue.Id] = source;
    }
    public static GameObject VisualSource(string id) => visualSources.TryGetValue(id, out GameObject source) ? source : null;

    public static event Action<Evidence> OnEvidenceAdded;
    public static event Action<Deduction> OnDeductionMade;

    public static IReadOnlyList<string> EvidenceIds => evidence;
    public static IReadOnlyList<string> DeductionIds => deductions;
    public static IReadOnlyList<BoardPlacement> Placements => placements;
    public static IReadOnlyList<BoardLink> Links => links;

    public static bool IsPinned(string id) => placements.Exists(p => p.evidenceId == id);

    public static void Pin(string id, Vector2 position)
    {
        if (!evidence.Contains(id) || !float.IsFinite(position.x) || !float.IsFinite(position.y)) return;
        var placement = placements.Find(p => p.evidenceId == id);
        if (placement == null) placements.Add(placement = new BoardPlacement { evidenceId = id });
        placement.position = new Vector2(Mathf.Clamp01(position.x), Mathf.Clamp01(position.y));
        OnBoardChanged?.Invoke();
    }

    public static void Unpin(string id)
    {
        placements.RemoveAll(p => p.evidenceId == id);
        links.RemoveAll(l => l.first == id || l.second == id);
        OnBoardChanged?.Invoke();
    }

    public static bool Link(string first, string second)
    {
        if (first == second || !IsPinned(first) || !IsPinned(second) || links.Exists(l => l.Matches(first, second))) return false;
        links.Add(new BoardLink { first = first, second = second });
        OnBoardChanged?.Invoke();
        return true;
    }

    public static void Unlink(BoardLink link)
    {
        if (link == null) return;
        links.RemoveAll(l => l.Matches(link.first, link.second));
        OnBoardChanged?.Invoke();
    }

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
    public static void Restore(IEnumerable<string> evidenceIds, IEnumerable<string> deductionIds,
        IEnumerable<BoardPlacement> boardPlacements = null, IEnumerable<BoardLink> boardLinks = null)
    {
        evidence.Clear();
        deductions.Clear();
        placements.Clear();
        links.Clear();
        if (evidenceIds != null) evidence.AddRange(evidenceIds);
        if (deductionIds != null) deductions.AddRange(deductionIds);
        if (boardPlacements != null)
            foreach (var p in boardPlacements) if (p != null) Pin(p.evidenceId, p.position);
        if (boardLinks != null)
            foreach (var l in boardLinks) if (l != null) Link(l.first, l.second);
        OnBoardChanged?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        evidence.Clear();
        deductions.Clear();
        placements.Clear();
        links.Clear();
        visualSources.Clear();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // Uma cena nova começa do zero; o checkpoint, se houver, restaura depois
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        evidence.Clear();
        deductions.Clear();
        placements.Clear();
        links.Clear();
        visualSources.Clear();
        OnBoardChanged?.Invoke();
    }
}

[Serializable]
public class BoardPlacement
{
    public string evidenceId;
    public Vector2 position;
}

[Serializable]
public class BoardLink
{
    public string first;
    public string second;
    public bool Matches(string a, string b) => (first == a && second == b) || (first == b && second == a);
}
