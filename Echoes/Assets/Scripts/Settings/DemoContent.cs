using UnityEngine;

/// <summary>
/// Esconde na demo os objetos que pertencem ao capítulo completo.
/// </summary>
public class DemoContent : MonoBehaviour
{
    [Tooltip("Objetos que não aparecem na demo.")]
    [SerializeField] private GameObject[] hiddenInDemo = new GameObject[0];

    private void Awake()
    {
        if (!DemoSettings.IsDemo) return;

        foreach (GameObject target in hiddenInDemo)
        {
            if (target != null) target.SetActive(false);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(GameObject[] targets) => hiddenInDemo = targets;
#endif
}
