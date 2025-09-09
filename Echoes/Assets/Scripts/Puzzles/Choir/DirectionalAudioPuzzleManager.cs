using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DirectionalAudioPuzzleManager : MonoBehaviour
{
    public static DirectionalAudioPuzzleManager Instance { get; private set; }

    [Header("Puzzle Configuration")]
    [Tooltip("Todos os possíveis sussurros para este puzzle. Deve haver pelo menos uma 'Clue'.")]
    [SerializeField] private List<Whisper> whisperPool;
    [Tooltip("Intervalo em segundos entre cada 'transmissão' de sussurros.")]
    [SerializeField] private float whisperInterval = 5.0f;

    private bool isPuzzleActive = false;
    private Whisper correctClue;
    private List<WhisperSource> whisperSources = new List<WhisperSource>();
    private Coroutine whisperRoutine;

    public bool IsPuzzleActive => isPuzzleActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    private void OnEnable()
    {
        GameEvents.OnAudioPuzzleStarted += StartPuzzle;
    }

    private void OnDisable()
    {
        GameEvents.OnAudioPuzzleStarted -= StartPuzzle;
    }

    public void RegisterWhisperSource(WhisperSource source)
    {
        if (!whisperSources.Contains(source))
        {
            whisperSources.Add(source);
        }
    }

    private void StartPuzzle()
    {
        if (isPuzzleActive) return;
        isPuzzleActive = true;
        Debug.Log("<color=purple>--- PUZZLE DE ÁUDIO INICIADO ---</color>");

        correctClue = whisperPool.FirstOrDefault(w => w.type == WhisperType.Clue);
        if (string.IsNullOrEmpty(correctClue.whisperText)) {
            Debug.LogError("Nenhuma pista (Clue) configurada no Whisper Pool!");
            isPuzzleActive = false;
            return;
        }
        
        Debug.Log($"Pista Correta Selecionada: '{correctClue.whisperText}'");
        whisperRoutine = StartCoroutine(WhisperBroadcastRoutine());
    }

    private IEnumerator WhisperBroadcastRoutine()
    {
        yield return new WaitForSeconds(2f);
        while (isPuzzleActive)
        {
            BroadcastWhispers();
            yield return new WaitForSeconds(whisperInterval);
        }
    }

    private void BroadcastWhispers()
    {
        if (whisperSources.Count == 0) return;

        List<WhisperSource> shuffledSources = whisperSources.OrderBy(s => Random.value).ToList();
        List<Whisper> distractions = whisperPool.Where(w => w.type == WhisperType.Distraction).OrderBy(w => Random.value).ToList();

        Debug.Log("--- Nova transmissão de sussurros ---");
        
        int clueSourceIndex = Random.Range(0, shuffledSources.Count);
        shuffledSources[clueSourceIndex].PlayWhisper(correctClue);

        int distractionIndex = 0;
        for (int i = 0; i < shuffledSources.Count; i++)
        {
            if (i == clueSourceIndex) continue;
            if(distractionIndex < distractions.Count)
            {
                shuffledSources[i].PlayWhisper(distractions[distractionIndex]);
                distractionIndex++;
            }
        }
    }

    public void SolvePuzzle()
    {
        if (!isPuzzleActive) return;
        isPuzzleActive = false;
        if(whisperRoutine != null) StopCoroutine(whisperRoutine);
        
        Debug.Log("<color=green>--- PUZZLE DE ÁUDIO RESOLVIDO ---</color>");
        GameEvents.TriggerAudioPuzzleSolved();
    }
}