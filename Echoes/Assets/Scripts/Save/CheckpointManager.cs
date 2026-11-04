using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Salva um checkpoint a cada mudança de ato e o carrega depois de um colapso: a cena é recarregada,
/// o estado salvo é aplicado e Ethan acorda na poltrona (CheckpointSpawn), horas depois.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    [Tooltip("Onde Ethan acorda. Se vazio, usa o primeiro CheckpointSpawn da cena; sem nenhum, a posição salva.")]
    [SerializeField] private CheckpointSpawn wakeUpPoint;

    [Tooltip("Minutos que Ethan perde a cada colapso (o relógio avança ao acordar).")]
    [SerializeField] private float collapseMinutesLost = 120f;

    // Pedido feito antes de recarregar a cena; lido pela cena nova
    public static bool PendingRestore { get; private set; }

    // Verdadeiro enquanto o checkpoint está sendo aplicado (nada é salvo nesse meio-tempo)
    public static bool IsRestoring { get; private set; }

    /// <summary>
    /// A cena atual vai carregar (ou está carregando) um checkpoint. Exige um CheckpointManager na
    /// cena: sem ele, ninguém aplicaria o save e a tela ficaria preta.
    /// </summary>
    public static bool WillRestore => IsRestoring || (PendingRestore && FindAnyObjectByType<CheckpointManager>() != null);

    /// <summary>
    /// Chamado antes de recarregar a cena após a morte: a cena nova carregará o último checkpoint, se houver.
    /// </summary>
    public static void PrepareRestore()
    {
        PendingRestore = SaveSystem.HasSave;
    }

    /// <summary>
    /// Carrega o último checkpoint a partir do menu ("Continuar").
    /// </summary>
    public static void LoadLastCheckpoint()
    {
        if (!SaveSystem.TryRead(out SaveData data)) return;

        PendingRestore = true;
        SceneManager.LoadScene(data.sceneName);
    }

    private void OnEnable()
    {
        GameFlow.OnActChanged += HandleActChanged;
    }

    private void OnDisable()
    {
        GameFlow.OnActChanged -= HandleActChanged;
    }

    private void Start()
    {
        if (PendingRestore)
        {
            StartCoroutine(RestoreRoutine());
        }
    }

    private void HandleActChanged(GameAct act)
    {
        if (IsRestoring || act == GameAct.Prologue || act == GameAct.Ending) return;

        StartCoroutine(SaveNextFrame());
    }

    // Espera um frame para os sistemas reagirem à mudança de ato antes de capturar o estado
    private IEnumerator SaveNextFrame()
    {
        yield return null;
        SaveCheckpoint();
    }

    /// <summary>
    /// Grava o estado atual como checkpoint.
    /// </summary>
    public void SaveCheckpoint()
    {
        var data = new SaveData
        {
            sceneName = gameObject.scene.name,
            act = GameFlow.Current,
        };

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
        {
            data.playerPosition = player.transform.position;
            data.playerRotation = player.transform.rotation;
        }

        if (DoorKeyManager.Instance != null) data.doorKeys = DoorKeyManager.Instance.CollectedKeys.ToList();
        if (DrawerKeyManager.Instance != null) data.drawerKeys = DrawerKeyManager.Instance.CollectedKeys.ToList();
        data.clockMinutes = GameClock.Minutes;
        data.evidence = CaseFile.EvidenceIds.ToList();
        data.deductions = CaseFile.DeductionIds.ToList();

        SaveSystem.CaptureObjects(data);
        SaveSystem.Write(data);
    }

    private IEnumerator RestoreRoutine()
    {
        IsRestoring = true;
        PendingRestore = false;

        // Deixa todos os Start() rodarem antes de sobrescrever o estado inicial
        yield return null;

        if (!SaveSystem.TryRead(out SaveData data))
        {
            // Save ilegível: segue do início da cena, mas sem deixar a tela preta
            IsRestoring = false;
            FindAnyObjectByType<DeathManager>()?.PlayWakeUpFade();
            yield break;
        }

        // Estado dos objetos primeiro: os sistemas que reagem ao ato (coro, por exemplo) leem esse estado
        SaveSystem.RestoreObjects(data);

        foreach (string key in data.doorKeys) DoorKeyManager.Instance.RegisterCollectedKey(key);
        foreach (string key in data.drawerKeys) DrawerKeyManager.Instance.RegisterCollectedKey(key);
        CaseFile.Restore(data.evidence, data.deductions);

        GameFlow.Restore(data.act);
        PlaceWakingPlayer(data);

        // Ethan acorda horas depois: o colapso também é tempo perdido
        GameClock.Restore(data.clockMinutes);
        GameClock.Skip(collapseMinutesLost);

        IsRestoring = false;
        Debug.Log($"[CheckpointManager] Checkpoint carregado: {data.act}");

        FindAnyObjectByType<DeathManager>()?.PlayWakeUpFade();
        GameEvents.TriggerWokeFromCollapse();
    }

    private void PlaceWakingPlayer(SaveData data)
    {
        CheckpointSpawn spawn = wakeUpPoint != null ? wakeUpPoint : FindAnyObjectByType<CheckpointSpawn>();
        if (spawn != null)
            PlayerTeleport.To(spawn.transform);
        else
            PlayerTeleport.To(data.playerPosition, data.playerRotation);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PendingRestore = false;
        IsRestoring = false;
    }
}
