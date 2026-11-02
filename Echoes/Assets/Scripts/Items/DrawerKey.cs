using UnityEngine;

/// <summary>
/// Chave que destranca gavetas (DrawerController com o mesmo ID).
/// </summary>
public class DrawerKey : KeyPickup
{
    protected override void RegisterKey(string id) => DrawerKeyManager.Instance.RegisterCollectedKey(id);

    protected override void NotifyCollected() => GameEvents.TriggerDrawerKeyCollected();

    protected override Color GizmoColor => new Color(1f, 1f, 0f, 0.3f);
}
