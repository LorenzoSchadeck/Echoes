using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Conteúdo de um checkpoint. Serializado com JsonUtility, por isso só listas e tipos simples.
/// </summary>
[Serializable]
public class SaveData
{
    // Incrementar quando o formato mudar de forma incompatível
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string savedAt;
    public string sceneName;
    public GameAct act;

    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public float clockMinutes = GameClock.StartMinutes;

    public List<string> doorKeys = new List<string>();
    public List<string> drawerKeys = new List<string>();
    public List<string> evidence = new List<string>();
    public List<string> deductions = new List<string>();
    public List<BoardPlacement> boardPlacements = new List<BoardPlacement>();
    public List<BoardLink> boardLinks = new List<BoardLink>();
    public List<string> destroyedIds = new List<string>();
    public List<SaveEntry> entries = new List<SaveEntry>();
}

[Serializable]
public class SaveEntry
{
    public string id;     // SaveableId do GameObject
    public string type;   // Componente dono do estado ("GameObject" para o estado ativo)
    public string json;
}

/// <summary>
/// Lê, grava e aplica checkpoints. Um único slot, em JSON, na pasta de dados do jogador.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "checkpoint.json";
    private const string ActiveStateType = "GameObject";

    // IDs de objetos destruídos durante o jogo (chaves coletadas, por exemplo)
    private static readonly HashSet<string> destroyedIds = new HashSet<string>();

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave => File.Exists(FilePath);

    public static void RegisterDestroyed(string id) => destroyedIds.Add(id);

    public static void Write(SaveData data)
    {
        data.version = SaveData.CurrentVersion;
        data.savedAt = DateTime.Now.ToString("o");

        // Grava num arquivo temporário e troca, para não corromper o save se o jogo fechar no meio
        string tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
        if (File.Exists(FilePath)) File.Delete(FilePath);
        File.Move(tempPath, FilePath);

        Debug.Log($"[SaveSystem] Checkpoint salvo ({data.act}, {data.entries.Count} objetos) em {FilePath}");
    }

    public static bool TryRead(out SaveData data)
    {
        data = null;
        if (!HasSave) return false;

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save ilegível, ignorado: {e.Message}");
            return false;
        }

        if (data == null || data.version != SaveData.CurrentVersion)
        {
            Debug.LogWarning($"[SaveSystem] Save de outra versão ({data?.version}), ignorado.");
            data = null;
            return false;
        }

        return true;
    }

    public static void Delete()
    {
        if (HasSave) File.Delete(FilePath);
    }

    /// <summary>
    /// Coleta o estado de todos os SaveableId da cena (inclusive inativos) e os objetos destruídos.
    /// </summary>
    public static void CaptureObjects(SaveData data)
    {
        data.entries.Clear();
        foreach (SaveableId saveable in UnityEngine.Object.FindObjectsByType<SaveableId>(FindObjectsInactive.Include))
        {
            if (string.IsNullOrEmpty(saveable.Id))
            {
                Debug.LogWarning($"[SaveSystem] {saveable.name} tem SaveableId sem ID e não será salvo.", saveable);
                continue;
            }

            if (saveable.SaveActiveState)
            {
                data.entries.Add(new SaveEntry { id = saveable.Id, type = ActiveStateType, json = saveable.gameObject.activeSelf ? "1" : "0" });
            }

            foreach (ISaveable component in saveable.GetComponents<ISaveable>())
            {
                data.entries.Add(new SaveEntry { id = saveable.Id, type = component.GetType().Name, json = component.CaptureState() });
            }
        }

        data.destroyedIds = new List<string>(destroyedIds);
    }

    /// <summary>
    /// Aplica o estado salvo aos objetos da cena e destrói os que tinham sido destruídos.
    /// </summary>
    public static void RestoreObjects(SaveData data)
    {
        var byId = new Dictionary<string, SaveableId>();
        foreach (SaveableId saveable in UnityEngine.Object.FindObjectsByType<SaveableId>(FindObjectsInactive.Include))
        {
            if (!string.IsNullOrEmpty(saveable.Id)) byId[saveable.Id] = saveable;
        }

        foreach (SaveEntry entry in data.entries)
        {
            if (!byId.TryGetValue(entry.id, out SaveableId saveable)) continue;

            if (entry.type == ActiveStateType)
            {
                saveable.gameObject.SetActive(entry.json == "1");
                continue;
            }

            foreach (ISaveable component in saveable.GetComponents<ISaveable>())
            {
                if (component.GetType().Name != entry.type) continue;

                try
                {
                    component.RestoreState(entry.json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Falha ao restaurar {entry.type} em {saveable.name}: {e.Message}", saveable);
                }
            }
        }

        foreach (string id in data.destroyedIds)
        {
            destroyedIds.Add(id);
            if (byId.TryGetValue(id, out SaveableId saveable)) UnityEngine.Object.Destroy(saveable.gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        destroyedIds.Clear();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) destroyedIds.Clear();
    }
}
