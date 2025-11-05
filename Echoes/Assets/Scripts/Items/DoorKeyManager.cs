using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador singleton que rastreia quais chaves de porta foram coletadas pelo jogador.
/// Usado pelo sistema de portas trancadas (DoorController).
/// </summary>
public class DoorKeyManager : MonoBehaviour
{
    private static DoorKeyManager instance;
    public static DoorKeyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DoorKeyManager>();
                
                if (instance == null)
                {
                    GameObject go = new GameObject("DoorKeyManager");
                    instance = go.AddComponent<DoorKeyManager>();
                }
            }
            return instance;
        }
    }
    
    // HashSet para armazenar IDs das chaves coletadas (busca O(1))
    private HashSet<string> collectedKeys = new HashSet<string>();
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Garante que só existe uma instância
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Registra uma chave como coletada
    /// </summary>
    /// <param name="keyID">ID da chave coletada</param>
    public void RegisterCollectedKey(string keyID)
    {
        if (string.IsNullOrEmpty(keyID))
        {
            Debug.LogWarning("[DoorKeyManager] Tentativa de registrar chave com ID vazio!");
            return;
        }
        
        if (collectedKeys.Add(keyID))
        {
            Debug.Log($"[DoorKeyManager] Chave de porta '{keyID}' coletada!");
        }
        else
        {
            Debug.LogWarning($"[DoorKeyManager] Chave de porta '{keyID}' já estava coletada!");
        }
    }
    
    /// <summary>
    /// Verifica se uma chave específica foi coletada
    /// </summary>
    /// <param name="keyID">ID da chave a verificar</param>
    /// <returns>True se a chave foi coletada, false caso contrário</returns>
    public bool HasKey(string keyID)
    {
        if (string.IsNullOrEmpty(keyID))
            return false;
        
        return collectedKeys.Contains(keyID);
    }
    
    /// <summary>
    /// Remove uma chave da coleção (útil para debug/testes)
    /// </summary>
    /// <param name="keyID">ID da chave a remover</param>
    public void RemoveKey(string keyID)
    {
        if (collectedKeys.Remove(keyID))
        {
            Debug.Log($"[DoorKeyManager] Chave de porta '{keyID}' removida!");
        }
    }
    
    /// <summary>
    /// Limpa todas as chaves coletadas
    /// </summary>
    public void ClearAllKeys()
    {
        collectedKeys.Clear();
        Debug.Log("[DoorKeyManager] Todas as chaves de porta foram removidas!");
    }
    
    /// <summary>
    /// Retorna o número de chaves coletadas
    /// </summary>
    public int GetCollectedKeyCount()
    {
        return collectedKeys.Count;
    }
    
    #endregion
    
    #region Editor Utilities
    
    #if UNITY_EDITOR
    
    /// <summary>
    /// Lista todas as chaves coletadas (para debug)
    /// </summary>
    [ContextMenu("List Collected Door Keys")]
    private void ListCollectedKeys()
    {
        if (collectedKeys.Count == 0)
        {
            Debug.Log("[DoorKeyManager] Nenhuma chave de porta coletada.");
            return;
        }
        
        Debug.Log($"[DoorKeyManager] Chaves de porta coletadas ({collectedKeys.Count}):");
        foreach (string keyID in collectedKeys)
        {
            Debug.Log($"  - {keyID}");
        }
    }
    
    /// <summary>
    /// Limpa todas as chaves (para debug)
    /// </summary>
    [ContextMenu("Clear All Door Keys")]
    private void DebugClearAllKeys()
    {
        ClearAllKeys();
    }
    
    #endif
    
    #endregion
}
