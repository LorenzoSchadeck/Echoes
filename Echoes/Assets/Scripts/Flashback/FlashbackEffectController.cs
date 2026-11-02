using UnityEngine;

/// <summary>
/// Lembranças comuns (FlashbackItem): teleporta para o ponto com a tag "FlashbackTeleport".
/// Ignora os flashbacks do coro, que têm o próprio controlador.
/// </summary>
public class FlashbackEffectController : MemoryTransitionController
{
    protected override string TeleportTag => "FlashbackTeleport";

    protected override bool ShouldHandleFlashback()
    {
        return ChoirManager.Instance == null || !ChoirManager.Instance.IsChoirActive;
    }
}
