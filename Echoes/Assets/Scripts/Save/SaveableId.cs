using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identificador estável de um objeto da cena para o sistema de save. Gerado automaticamente no
/// Editor; objetos duplicados (Ctrl+D) recebem um ID novo. Prefabs (asset ou modo de edição de
/// prefab) não guardam ID: cada instância na cena tem o seu.
/// </summary>
[DisallowMultipleComponent]
public class SaveableId : MonoBehaviour
{
    [SerializeField] private string id;

    [Tooltip("Salva se o GameObject está ativo ou não (útil para gatilhos e objetos que somem).")]
    [SerializeField] private bool saveActiveState;

    public string Id => id;
    public bool SaveActiveState => saveActiveState;

    private void OnDestroy()
    {
        // Destruído durante o jogo (não na troca de cena): lembra para destruir de novo ao restaurar
        if (Application.isPlaying && gameObject.scene.isLoaded && !string.IsNullOrEmpty(id))
        {
            SaveSystem.RegisterDestroyed(id);
        }
    }

#if UNITY_EDITOR
    private static readonly Dictionary<string, SaveableId> editorRegistry = new Dictionary<string, SaveableId>();

    private void Reset() => OnValidate();

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        bool isPrefabAsset = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)
            || UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null;

        if (isPrefabAsset)
        {
            if (!string.IsNullOrEmpty(id))
            {
                id = string.Empty;
                UnityEditor.EditorUtility.SetDirty(this);
            }
            return;
        }

        bool isDuplicate = !string.IsNullOrEmpty(id)
            && editorRegistry.TryGetValue(id, out SaveableId other)
            && other != null && other != this;

        if (string.IsNullOrEmpty(id) || isDuplicate)
        {
            id = Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }

        editorRegistry[id] = this;
    }
#endif
}
