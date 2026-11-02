using UnityEngine;

/// <summary>
/// Chave que destranca portas (DoorController com o mesmo ID).
/// </summary>
public class DoorKey : KeyPickup
{
    protected override void RegisterKey(string id) => DoorKeyManager.Instance.RegisterCollectedKey(id);

    protected override void NotifyCollected() => GameEvents.TriggerDoorKeyCollected();

    protected override Color GizmoColor => new Color(0f, 1f, 1f, 0.3f);
}
