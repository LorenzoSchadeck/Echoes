using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador singleton das chaves coletadas de um tipo (porta, gaveta...).
/// Se não houver um na cena, é criado na primeira vez que for pedido.
/// </summary>
public abstract class KeyManagerBase<T> : MonoBehaviour where T : KeyManagerBase<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();
                if (instance == null) instance = new GameObject(typeof(T).Name).AddComponent<T>();
            }
            return instance;
        }
    }

    // HashSet para busca O(1)
    private readonly HashSet<string> collectedKeys = new HashSet<string>();

    public IEnumerable<string> CollectedKeys => collectedKeys;

    private string LogPrefix => $"[{typeof(T).Name}]";

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = (T)this;
    }

    /// <summary>
    /// Registra uma chave como coletada
    /// </summary>
    public void RegisterCollectedKey(string keyID)
    {
        if (string.IsNullOrEmpty(keyID))
        {
            Debug.LogWarning($"{LogPrefix} Tentativa de registrar chave com ID vazio!");
            return;
        }

        if (collectedKeys.Add(keyID))
            Debug.Log($"{LogPrefix} Chave '{keyID}' coletada!");
        else
            Debug.LogWarning($"{LogPrefix} Chave '{keyID}' já estava coletada!");
    }

    /// <summary>
    /// Verifica se uma chave específica foi coletada
    /// </summary>
    public bool HasKey(string keyID)
    {
        return !string.IsNullOrEmpty(keyID) && collectedKeys.Contains(keyID);
    }

    /// <summary>
    /// Remove uma chave da coleção (útil para debug/testes)
    /// </summary>
    public void RemoveKey(string keyID)
    {
        if (collectedKeys.Remove(keyID)) Debug.Log($"{LogPrefix} Chave '{keyID}' removida!");
    }

    /// <summary>
    /// Limpa todas as chaves coletadas
    /// </summary>
    public void ClearAllKeys()
    {
        collectedKeys.Clear();
        Debug.Log($"{LogPrefix} Todas as chaves foram removidas!");
    }

    public int GetCollectedKeyCount() => collectedKeys.Count;

    [ContextMenu("List Collected Keys")]
    protected void ListCollectedKeys()
    {
        Debug.Log(collectedKeys.Count == 0
            ? $"{LogPrefix} Nenhuma chave coletada."
            : $"{LogPrefix} Chaves coletadas ({collectedKeys.Count}): {string.Join(", ", collectedKeys)}");
    }

    [ContextMenu("Clear All Keys")]
    protected void DebugClearAllKeys() => ClearAllKeys();
}
