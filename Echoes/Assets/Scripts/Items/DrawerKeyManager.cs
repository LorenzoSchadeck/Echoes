using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador singleton que rastreia quais chaves foram coletadas pelo jogador.
/// Usado pelo sistema de gavetas trancadas.
/// </summary>
public class DrawerKeyManager : MonoBehaviour
{
    private static DrawerKeyManager instance;
    public static DrawerKeyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DrawerKeyManager>();
                
                if (instance == null)
                {
                    GameObject go = new GameObject("DrawerKeyManager");
                    instance = go.AddComponent<DrawerKeyManager>();
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
            Debug.LogWarning("[DrawerKeyManager] Tentativa de registrar chave com ID vazio!");
            return;
        }
        
        if (collectedKeys.Add(keyID))
        {
            Debug.Log($"[DrawerKeyManager] Chave '{keyID}' coletada!");
        }
        else
        {
            Debug.LogWarning($"[DrawerKeyManager] Chave '{keyID}' já estava coletada!");
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
            Debug.Log($"[DrawerKeyManager] Chave '{keyID}' removida!");
        }
    }
    
    /// <summary>
    /// Limpa todas as chaves coletadas
    /// </summary>
    public void ClearAllKeys()
    {
        collectedKeys.Clear();
        Debug.Log("[DrawerKeyManager] Todas as chaves foram removidas!");
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
    [ContextMenu("List Collected Keys")]
    private void ListCollectedKeys()
    {
        if (collectedKeys.Count == 0)
        {
            Debug.Log("[DrawerKeyManager] Nenhuma chave coletada.");
            return;
        }
        
        Debug.Log($"[DrawerKeyManager] Chaves coletadas ({collectedKeys.Count}):");
        foreach (string keyID in collectedKeys)
        {
            Debug.Log($"  - {keyID}");
        }
    }
    
    /// <summary>
    /// Limpa todas as chaves (para debug)
    /// </summary>
    [ContextMenu("Clear All Keys")]
    private void DebugClearAllKeys()
    {
        ClearAllKeys();
    }
    
    #endif
    
    #endregion
}
