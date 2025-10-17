using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerencia o spawn de corpos cobertos para eventos de horror no Limiar 4.
/// Controla a aparição temporária de corpos assombrados em pontos específicos da cena.
/// </summary>
public class CoveredBodySpawner : MonoBehaviour
{
    public static CoveredBodySpawner Instance { get; private set; }

    [Header("Spawn Settings")]
    [Tooltip("Tempo que o corpo permanece na cena antes de desaparecer.")]
    [SerializeField] private float defaultBodyLifetime = 10f;

    [Header("Object Hiding")]
    [Tooltip("Lista de objetos que serão escondidos quando um corpo aparecer.")]
    [SerializeField] private List<GameObject> objectsToHide = new List<GameObject>();

    [Header("Debug")]
    [Tooltip("Mostra logs detalhados do sistema de spawn de corpos.")]
    [SerializeField] private bool enableDebugLogs = true;

    // Estado interno
    private List<SpawnedBodyInfo> activeBodies = new List<SpawnedBodyInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnSpawnCoveredBodyTriggered += OnSpawnCoveredBodyTriggered;
    }

    private void OnDisable()
    {
        GameEvents.OnSpawnCoveredBodyTriggered -= OnSpawnCoveredBodyTriggered;
    }

    private void OnSpawnCoveredBodyTriggered(GameObject bodyPrefab, Transform spawnPoint)
    {
        if (bodyPrefab == null || spawnPoint == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("CoveredBodySpawner: Prefab ou ponto de spawn inválido!");
            return;
        }

        SpawnCoveredBody(bodyPrefab, spawnPoint, defaultBodyLifetime);
    }

    /// <summary>
    /// Spawna um corpo coberto em um ponto específico.
    /// </summary>
    public void SpawnCoveredBody(GameObject bodyPrefab, Transform spawnPoint, float lifetime = -1f)
    {
        if (lifetime < 0) lifetime = defaultBodyLifetime;

        StartCoroutine(SpawnBodyCoroutine(bodyPrefab, spawnPoint, lifetime));
    }

    private IEnumerator SpawnBodyCoroutine(GameObject bodyPrefab, Transform spawnPoint, float lifetime)
    {
        if (enableDebugLogs)
            Debug.Log($"Spawnando corpo coberto em: {spawnPoint.name} por {lifetime} segundos");

        // Spawna o corpo
        GameObject spawnedBody = Instantiate(bodyPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Aplica escala se necessário
        if (spawnPoint.localScale != Vector3.one)
        {
            spawnedBody.transform.localScale = spawnPoint.localScale;
        }

        // Esconde os objetos configurados
        List<bool> originalStates = new List<bool>();
        foreach (GameObject obj in objectsToHide)
        {
            if (obj != null)
            {
                originalStates.Add(obj.activeInHierarchy);
                obj.SetActive(false);
                
                if (enableDebugLogs)
                    Debug.Log($"Objeto escondido: {obj.name}");
            }
        }

        // Armazena informações do corpo spawnado
        SpawnedBodyInfo bodyInfo = new SpawnedBodyInfo
        {
            bodyGameObject = spawnedBody,
            spawnPoint = spawnPoint,
            spawnTime = Time.time,
            lifetime = lifetime,
            hiddenObjectsOriginalStates = originalStates
        };

        activeBodies.Add(bodyInfo);

        // Adiciona componente para detectar quando jogador se aproxima (opcional)
        ProximityTrigger proximityTrigger = spawnedBody.GetComponent<ProximityTrigger>();
        if (proximityTrigger == null)
        {
            proximityTrigger = spawnedBody.AddComponent<ProximityTrigger>();
            proximityTrigger.triggerDistance = 3f;
            proximityTrigger.onPlayerEnter.AddListener(() => OnPlayerNearBody(bodyInfo));
        }

        if (enableDebugLogs)
            Debug.Log($"Corpo coberto spawnado com sucesso: {spawnedBody.name}");

        // Aguarda o tempo de vida do corpo
        yield return new WaitForSeconds(lifetime);

        // Remove o corpo
        DespawnBody(bodyInfo);
    }

    private void OnPlayerNearBody(SpawnedBodyInfo bodyInfo)
    {
        if (bodyInfo.bodyGameObject == null) return;

        // Apenas efeito visual/atmosférico - sem perda de sanidade
        // GameEvents.TriggerSanityLost(0.05f); // REMOVIDO: Eventos não devem afetar sanidade

        if (enableDebugLogs)
            Debug.Log("Jogador se aproximou do corpo coberto - efeito atmosférico ativo");
    }

    /// <summary>
    /// Restaura os objetos escondidos aos seus estados originais.
    /// </summary>
    private void RestoreHiddenObjects(SpawnedBodyInfo bodyInfo)
    {
        if (bodyInfo.hiddenObjectsOriginalStates == null || bodyInfo.hiddenObjectsOriginalStates.Count != objectsToHide.Count)
        {
            if (enableDebugLogs)
                Debug.LogWarning("Estados originais dos objetos não correspondem à lista atual de objetos!");
            return;
        }

        for (int i = 0; i < objectsToHide.Count && i < bodyInfo.hiddenObjectsOriginalStates.Count; i++)
        {
            if (objectsToHide[i] != null)
            {
                objectsToHide[i].SetActive(bodyInfo.hiddenObjectsOriginalStates[i]);
                
                if (enableDebugLogs)
                    Debug.Log($"Objeto restaurado: {objectsToHide[i].name} -> {bodyInfo.hiddenObjectsOriginalStates[i]}");
            }
        }
    }

    private void DespawnBody(SpawnedBodyInfo bodyInfo)
    {
        if (bodyInfo.bodyGameObject == null)
        {
            activeBodies.Remove(bodyInfo);
            return;
        }

        // Restaura os objetos escondidos aos seus estados originais
        RestoreHiddenObjects(bodyInfo);

        // Remove o corpo
        Destroy(bodyInfo.bodyGameObject);
        activeBodies.Remove(bodyInfo);

        if (enableDebugLogs)
            Debug.Log("Corpo coberto removido da cena");
    }

    /// <summary>
    /// Remove todos os corpos ativos imediatamente.
    /// </summary>
    public void DespawnAllBodies()
    {
        StopAllCoroutines();

        foreach (SpawnedBodyInfo bodyInfo in activeBodies.ToArray())
        {
            if (bodyInfo.bodyGameObject != null)
            {
                // Restaura objetos escondidos antes de destruir o corpo
                RestoreHiddenObjects(bodyInfo);
                Destroy(bodyInfo.bodyGameObject);
            }
        }

        activeBodies.Clear();
        
        if (enableDebugLogs)
            Debug.Log("Todos os corpos cobertos foram removidos.");
    }

    /// <summary>
    /// Verifica se há corpos ativos na cena.
    /// </summary>
    public bool HasActiveBodies => activeBodies.Count > 0;

    /// <summary>
    /// Retorna o número de corpos ativos.
    /// </summary>
    public int ActiveBodyCount => activeBodies.Count;

    /// <summary>
    /// Força o despawn de um corpo específico.
    /// </summary>
    public void DespawnBody(GameObject bodyToRemove)
    {
        SpawnedBodyInfo bodyInfo = activeBodies.Find(info => info.bodyGameObject == bodyToRemove);
        if (bodyInfo.bodyGameObject != null)
        {
            DespawnBody(bodyInfo);
        }
    }

    private void OnDestroy()
    {
        // Garante que corpos sejam removidos se o spawner for destruído
        DespawnAllBodies();
    }

#if UNITY_EDITOR
    [Header("Editor Tools")]
    [SerializeField] private bool showDebugInfo = true;

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 440, 300, 120));
        GUILayout.Label($"Corpos Ativos: {activeBodies.Count}");
        GUILayout.Label($"Tempo de Vida Padrão: {defaultBodyLifetime}s");
        
        if (GUILayout.Button("Despawn All Bodies"))
        {
            DespawnAllBodies();
        }
        
        if (GUILayout.Button("Test Spawn (Need Prefab & Point)"))
        {
            // Teste rápido - necessita configuração manual
            if (enableDebugLogs)
                Debug.Log("Configure um prefab e ponto de spawn para testar!");
        }
        
        GUILayout.EndArea();
    }
#endif
}

/// <summary>
/// Estrutura para armazenar informações sobre um corpo spawnado.
/// </summary>
[System.Serializable]
public struct SpawnedBodyInfo
{
    public GameObject bodyGameObject;
    public Transform spawnPoint;
    public float spawnTime;
    public float lifetime;
    public List<bool> hiddenObjectsOriginalStates; // Estados originais dos objetos escondidos
}

/// <summary>
/// Componente simples para detectar proximidade do jogador.
/// </summary>
public class ProximityTrigger : MonoBehaviour
{
    [HideInInspector] public float triggerDistance = 10f;
    [HideInInspector] public UnityEngine.Events.UnityEvent onPlayerEnter = new UnityEngine.Events.UnityEvent();

    private bool playerInRange = false;

    private void Update()
    {
        if (Camera.main != null)
        {
            float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            
            if (!playerInRange && distance <= triggerDistance)
            {
                playerInRange = true;
                onPlayerEnter.Invoke();
            }
            else if (playerInRange && distance > triggerDistance)
            {
                playerInRange = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}