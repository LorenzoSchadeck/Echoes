using System.Linq;
using UnityEngine;

/// <summary>
/// Lembrança do coro (porta emperrada): teleporta para o ponto com a tag "ChoirTeleport".
/// Só age enquanto o coro está ativo. Ao sair, se o coro não foi concluído, a porta volta a
/// poder ser usada para uma nova tentativa.
/// </summary>
public class ChoirFlashbackController : MemoryTransitionController
{
    protected override string TeleportTag => "ChoirTeleport";

    protected override bool ShouldHandleFlashback()
    {
        return ChoirManager.Instance != null && ChoirManager.Instance.IsChoirActive;
    }

    protected override void OnEntryStarted()
    {
        // Cura a sanidade ao entrar, como na lembrança comum (o FlashbackItem faz isso por lá)
        GameEvents.TriggerRemedyUsed();
    }

    protected override void OnExitCompleted()
    {
        postProcessingManager.ForceResetToSaneState();

        if (ChoirManager.Instance != null && !ChoirManager.Instance.IsChoirComplete)
        {
            DoorController choirDoor = FindObjectsByType<DoorController>(FindObjectsInactive.Exclude)
                .FirstOrDefault(door => door.IsChoirDoor);
            choirDoor?.ResetChoirDoor();
        }
    }
}
