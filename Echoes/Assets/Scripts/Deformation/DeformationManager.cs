using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Echoes.Deformation
{
    public class DeformationManager : MonoBehaviour
    {
        public static DeformationManager Instance { get; private set; }
        
        [Header("🎭 Sistema de 3 Fases")]
        [Tooltip("Fase 1: 100%-60% = Só Post-Processing")]
        [SerializeField, Range(0f, 1f)] private float textureTransitionStartThreshold = 0.6f;
        
        [Tooltip("Fase 2: 60%-30% = Post-Processing + Texturas")]
        [SerializeField, Range(0f, 1f)] private float meshDeformationStartThreshold = 0.3f;
        
        
        [Header("🌊 Melting Effect Settings")]
        [Tooltip("Velocidade do efeito de derretimento contínuo")]
        [SerializeField, Range(0.1f, 5f)] private float meltingSpeed = 1f;
        
        [Tooltip("Intensidade do deslocamento de derretimento")]
        [SerializeField, Range(0f, 2f)] private float meltingIntensity = 0.5f;
        
        [Tooltip("Direção do derretimento (Y negativo = para baixo)")]
        [SerializeField] private Vector2 meltingDirection = new Vector2(0f, -1f);
        
        [Tooltip("Usar ruído para derretimento orgânico")]
        [SerializeField] private bool useOrganicMelting = true;
        
        [SerializeField] private DeformationValues initialValues = new DeformationValues();
        [SerializeField] private DeformationValues finalValues = new DeformationValues();
        [SerializeField, Range(1f, 60f)] private float updateFrequency = 30f;
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showDebugGUI = false;
        
        private float currentSanity = 1f;
        private float currentDeformationLevel = 0f;
        private List<DeformableObject> registeredObjects = new List<DeformableObject>();
        private Coroutine updateCoroutine;
        private bool isSystemPaused = false;
        private readonly Dictionary<Material, MaterialPropertyBlock> materialBlocks = new Dictionary<Material, MaterialPropertyBlock>();
        
        // Variáveis para efeito de derretimento contínuo
        private float meltingTime = 0f;
        private float meltingPhase = 0f;
        
        // Variáveis para transição de remédio
        private bool isRemedyTransitionActive = false;
        private Coroutine remedyTransitionCoroutine;
        private float remedyTransitionDuration = 3f; // Sincronizado com PostProcessingManager
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            InitializeSystem();
        }
        
        private void OnEnable()
        {
            InsanityManager.OnSanityChanged += OnSanityChanged;
            GameEvents.OnFlashbackStarted += OnFlashbackStarted;
            GameEvents.OnFlashbackEnded += OnFlashbackEnded;
            GameEvents.OnRemedyUsed += OnRemedyUsed;
            GameEvents.OnDeathSequenceCancelled += OnRemedyUsed; // Trata cancelamento de morte como remédio
        }
        
        private void OnDisable()
        {
            InsanityManager.OnSanityChanged -= OnSanityChanged;
            GameEvents.OnFlashbackStarted -= OnFlashbackStarted;
            GameEvents.OnFlashbackEnded -= OnFlashbackEnded;
            GameEvents.OnRemedyUsed -= OnRemedyUsed;
            GameEvents.OnDeathSequenceCancelled -= OnRemedyUsed;
        }
        
        private void OnDestroy()
        {
            InsanityManager.OnSanityChanged -= OnSanityChanged;
            
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            
            if (remedyTransitionCoroutine != null)
            {
                StopCoroutine(remedyTransitionCoroutine);
            }
        }
        
        private void Update()
        {
            // Atualiza o tempo de derretimento contínuo quando na Fase 2 ou 3
            if (currentSanity <= textureTransitionStartThreshold)
            {
                meltingTime += Time.deltaTime * meltingSpeed;
                
                if (useOrganicMelting)
                {
                    // Derretimento orgânico com ruído Perlin
                    meltingPhase = Mathf.PerlinNoise(meltingTime * 0.1f, Time.time * 0.05f);
                }
                else
                {
                    // Derretimento linear suave
                    meltingPhase = Mathf.Sin(meltingTime) * 0.5f + 0.5f;
                }
            }
        }
        
        private void InitializeSystem()
        {
            var deformableObjects = FindObjectsByType<DeformableObject>(FindObjectsSortMode.None);
            foreach (var obj in deformableObjects)
            {
                RegisterObject(obj);
            }
            
            updateCoroutine = StartCoroutine(UpdateDeformationLoop());
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DeformationManager] System initialized with {registeredObjects.Count} objects");
            }
        }
        
        public void RegisterObject(DeformableObject deformableObject)
        {
            if (deformableObject != null && !registeredObjects.Contains(deformableObject))
            {
                registeredObjects.Add(deformableObject);
            }
        }
        
        public void UnregisterObject(DeformableObject deformableObject)
        {
            registeredObjects.Remove(deformableObject);
        }
        
        private void OnSanityChanged(float newSanity)
        {
            // Durante transição de remédio, ignora mudanças de sanidade para não interferir na transição suave
            if (isRemedyTransitionActive)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[DeformationManager] ❌ Sanity change to {newSanity:F2} BLOCKED - remedy transition active");
                }
                return;
            }
            
            currentSanity = newSanity;
            
            // Sistema de 3 Fases:
            // Fase 1 (100%-60%): Nenhuma deformação (só post-processing)
            // Fase 2 (60%-30%): Texturas começam a transicionar
            // Fase 3 (30%-0%): Texturas + Mesh deformação completa
            
            if (newSanity <= meshDeformationStartThreshold)
            {
                // Fase 3: Deformação completa (30%-0%)
                float t = 1f - (newSanity / meshDeformationStartThreshold);
                currentDeformationLevel = Mathf.Clamp01(t);
            }
            else if (newSanity <= textureTransitionStartThreshold)
            {
                // Fase 2: Apenas texturas (60%-30%)
                // Mapeia 60%-30% para 0-1 para texturas
                float textureRange = textureTransitionStartThreshold - meshDeformationStartThreshold;
                float normalizedSanity = newSanity - meshDeformationStartThreshold;
                float t = 1f - (normalizedSanity / textureRange);
                currentDeformationLevel = Mathf.Clamp01(t);
            }
            else
            {
                // Fase 1: Nenhuma deformação (100%-60%)
                currentDeformationLevel = 0f;
            }
            
            if (enableDebugLogs)
            {
                string phase = GetCurrentPhase(newSanity);
                Debug.Log($"[DeformationManager] Sanity: {newSanity:F2} | Phase: {phase} | Deformation: {currentDeformationLevel:F2}");
            }
        }
        
        private string GetCurrentPhase(float sanity)
        {
            if (sanity > textureTransitionStartThreshold)
                return "Phase 1 (Post-Processing Only)";
            else if (sanity > meshDeformationStartThreshold)
                return "Phase 2 (Textures + Post-Processing)";
            else
                return "Phase 3 (Full Deformation)";
        }
        
        private void OnFlashbackStarted()
        {
            isSystemPaused = true;
            
            if (enableDebugLogs)
            {
                Debug.Log("[DeformationManager] System paused for flashback");
            }
        }
        
        private void OnFlashbackEnded()
        {
            isSystemPaused = false;
            
            if (enableDebugLogs)
            {
                Debug.Log("[DeformationManager] System resumed after flashback");
            }
            
            // Se a sanidade foi resetada para 1.0 por um remédio durante o flashback,
            // força uma transição suave para o estado limpo
            if (currentSanity >= 1.0f && currentDeformationLevel > 0f)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[DeformationManager] Detected remedy effect after flashback - executing clean transition");
                }
                
                // IMPORTANTE: Ativa a flag para bloquear mudanças de sanidade
                isRemedyTransitionActive = true;
                if (enableDebugLogs)
                {
                    Debug.Log("[DeformationManager] 🔒 Remedy transition flag activated for flashback end transition");
                }
                
                // Para qualquer transição anterior
                if (remedyTransitionCoroutine != null)
                {
                    StopCoroutine(remedyTransitionCoroutine);
                }
                
                // Inicia a transição suave para o estado limpo
                remedyTransitionCoroutine = StartCoroutine(RemedyTransitionRoutine());
            }
        }
        
        private void OnRemedyUsed()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[DeformationManager] Remedy used - starting smooth transition to clean state");
            }
            
            // IMPORTANTE: Ativa a flag IMEDIATAMENTE para bloquear mudanças de sanidade
            isRemedyTransitionActive = true;
            if (enableDebugLogs)
            {
                Debug.Log("[DeformationManager] 🔒 Remedy transition flag activated - blocking sanity changes");
            }
            
            // Para qualquer transição anterior
            if (remedyTransitionCoroutine != null)
            {
                StopCoroutine(remedyTransitionCoroutine);
            }
            
            // Se o sistema está pausado (flashback), agenda a transição para quando sair do flashback
            if (isSystemPaused)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[DeformationManager] System is paused (flashback) - remedy transition will execute after flashback ends");
                }
                // A transição será executada automaticamente quando OnFlashbackEnded for chamado
                // por causa da sincronização da sanidade no InsanityManager
                return;
            }
            
            // Inicia a transição suave para o estado limpo
            remedyTransitionCoroutine = StartCoroutine(RemedyTransitionRoutine());
        }
        
        private IEnumerator RemedyTransitionRoutine()
        {
            // A flag isRemedyTransitionActive já foi ativada em OnRemedyUsed
            
            // Captura os valores atuais de deformação
            float startDeformationLevel = currentDeformationLevel;
            DeformationValues startValues = InterpolateValues(startDeformationLevel);
            DeformationValues targetValues = initialValues; // Estado limpo
            
            float elapsedTime = 0f;
            
            while (elapsedTime < remedyTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / remedyTransitionDuration;
                
                // Aplica uma curva suave para a transição
                t = Mathf.SmoothStep(0f, 1f, t);
                
                // Interpola todos os valores de deformação
                DeformationValues currentValues = new DeformationValues
                {
                    insanityLevel = Mathf.Lerp(startValues.insanityLevel, targetValues.insanityLevel, t),
                    uvDisplacementStrength = Mathf.Lerp(startValues.uvDisplacementStrength, targetValues.uvDisplacementStrength, t),
                    corruptionInfluence = Mathf.Lerp(startValues.corruptionInfluence, targetValues.corruptionInfluence, t),
                    corruptionNormalStrength = Mathf.Lerp(startValues.corruptionNormalStrength, targetValues.corruptionNormalStrength, t),
                    deformStrength = Mathf.Lerp(startValues.deformStrength, targetValues.deformStrength, t),
                    deformFrequency = Mathf.Lerp(startValues.deformFrequency, targetValues.deformFrequency, t)
                };
                
                // Aplica os valores interpolados a todos os objetos
                ApplyValuesToAllObjects(currentValues);
                
                yield return null;
            }
            
            // Garante o estado final limpo
            ApplyValuesToAllObjects(targetValues);
            
            // Reseta valores internos
            currentDeformationLevel = 0f;
            ResetMeltingTime();
            
            isRemedyTransitionActive = false;
            remedyTransitionCoroutine = null;
            
            if (enableDebugLogs)
            {
                Debug.Log("[DeformationManager] Remedy transition completed - all materials restored to clean state");
            }
        }
        
        private void ApplyValuesToAllObjects(DeformationValues values)
        {
            foreach (var obj in registeredObjects)
            {
                if (obj == null) continue;
                
                var renderer = obj.GetRenderer();
                var config = obj.GetConfiguration();
                
                if (renderer != null)
                {
                    ApplyDeformation(renderer, config, values);
                }
            }
        }
        
        private IEnumerator UpdateDeformationLoop()
        {
            while (true)
            {
                // Não atualiza durante pausas do sistema ou transições de remédio
                if (!isSystemPaused && !isRemedyTransitionActive && currentDeformationLevel > 0f)
                {
                    UpdateObjectsDeformation();
                }
                
                yield return new WaitForSeconds(1f / updateFrequency);
            }
        }
        
        private void UpdateObjectsDeformation()
        {
            // Processa TODOS os objetos registrados a cada frame (sem limitação)
            for (int i = registeredObjects.Count - 1; i >= 0; i--)
            {
                var obj = registeredObjects[i];
                
                if (obj == null)
                {
                    registeredObjects.RemoveAt(i);
                    continue;
                }
                
                UpdateObjectDeformation(obj);
            }
        }
        
        private void UpdateObjectDeformation(DeformableObject deformableObject)
        {
            var config = deformableObject.GetConfiguration();
            var renderer = deformableObject.GetRenderer();
            
            if (renderer == null || renderer.sharedMaterial == null) return;
            
            var currentValues = InterpolateValues(currentDeformationLevel);
            ApplyDeformation(renderer, config, currentValues);
        }
        
        private DeformationValues InterpolateValues(float t)
        {
            // Calcula diferentes fatores de interpolação baseado na fase atual
            float textureT = GetTextureInterpolationFactor();
            float meshT = GetMeshInterpolationFactor();
            
            // Aplica efeito de derretimento contínuo nas propriedades de textura quando ativo
            float meltingModifier = 1f;
            if (currentSanity <= textureTransitionStartThreshold)
            {
                meltingModifier = 1f + (meltingIntensity * meltingPhase);
            }
            
            return new DeformationValues
            {
                // Texturas começam a transicionar em 60% (Fase 2) com efeito de derretimento
                insanityLevel = Mathf.Lerp(initialValues.insanityLevel, finalValues.insanityLevel, textureT) * meltingModifier,
                uvDisplacementStrength = Mathf.Lerp(initialValues.uvDisplacementStrength, finalValues.uvDisplacementStrength, textureT) * meltingModifier,
                corruptionInfluence = Mathf.Lerp(initialValues.corruptionInfluence, finalValues.corruptionInfluence, textureT) * meltingModifier,
                corruptionNormalStrength = Mathf.Lerp(initialValues.corruptionNormalStrength, finalValues.corruptionNormalStrength, textureT) * meltingModifier,
                
                // Mesh só começa a deformar em 30% (Fase 3)
                deformStrength = Mathf.Lerp(initialValues.deformStrength, finalValues.deformStrength, meshT),
                deformFrequency = Mathf.Lerp(initialValues.deformFrequency, finalValues.deformFrequency, meshT)
            };
        }
        
        private float GetTextureInterpolationFactor()
        {
            if (currentSanity > textureTransitionStartThreshold)
            {
                // Fase 1: Sem transição de textura
                return 0f;
            }
            else if (currentSanity > meshDeformationStartThreshold)
            {
                // Fase 2: Textura transiciona de 60% para 30%
                float range = textureTransitionStartThreshold - meshDeformationStartThreshold;
                float progress = textureTransitionStartThreshold - currentSanity;
                return Mathf.Clamp01(progress / range);
            }
            else
            {
                // Fase 3: Textura totalmente transicionada
                return 1f;
            }
        }
        
        private float GetMeshInterpolationFactor()
        {
            if (currentSanity > meshDeformationStartThreshold)
            {
                // Fases 1 e 2: Sem deformação de mesh
                return 0f;
            }
            else
            {
                // Fase 3: Mesh deforma de 30% para 0%
                float t = 1f - (currentSanity / meshDeformationStartThreshold);
                return Mathf.Clamp01(t);
            }
        }
        
        private void ApplyDeformation(Renderer renderer, DeformableObjectConfig config, DeformationValues values)
        {
            // ✅ CORREÇÃO: Usa sharedMaterial para evitar criar instâncias
            var sharedMaterial = renderer.sharedMaterial;
            
            if (!materialBlocks.TryGetValue(sharedMaterial, out MaterialPropertyBlock propertyBlock))
            {
                propertyBlock = new MaterialPropertyBlock();
                materialBlocks[sharedMaterial] = propertyBlock;
            }
            
            renderer.GetPropertyBlock(propertyBlock);
            
            if (config.allowMeshDeformation)
            {
                // ✅ APLICAÇÃO DOS MULTIPLICADORES DE MESH
                float finalDeformStrength = values.deformStrength * config.meshIntensityMultiplier;
                float finalDeformFrequency = values.deformFrequency * config.meshIntensityMultiplier;
                
                propertyBlock.SetFloat("_DeformStrength", finalDeformStrength);
                propertyBlock.SetFloat("_DeformFrequency", finalDeformFrequency);
            }
            
            if (config.allowTextureDeformation)
            {
                // ✅ APLICAÇÃO DOS MULTIPLICADORES DE TEXTURA
                float finalInsanityLevel = values.insanityLevel * config.textureIntensityMultiplier;
                float finalUVDisplacement = values.uvDisplacementStrength * config.textureIntensityMultiplier;
                float finalCorruptionInfluence = values.corruptionInfluence * config.textureIntensityMultiplier;
                float finalCorruptionNormal = values.corruptionNormalStrength * config.textureIntensityMultiplier;
                
                propertyBlock.SetFloat("_InsanityLevel", finalInsanityLevel);
                propertyBlock.SetFloat("_UVDisplacementStrength", finalUVDisplacement);
                propertyBlock.SetFloat("_CorruptionInfluence", finalCorruptionInfluence);
                propertyBlock.SetFloat("_CorruptionNormalStrength", finalCorruptionNormal);
                
                // Propriedades do efeito de derretimento contínuo
                if (currentSanity <= textureTransitionStartThreshold)
                {
                    propertyBlock.SetFloat("_MeltingTime", meltingTime);
                    propertyBlock.SetFloat("_MeltingPhase", meltingPhase);
                    // ✅ MULTIPLICADOR TAMBÉM APLICADO AO MELTING
                    propertyBlock.SetFloat("_MeltingIntensity", meltingIntensity * config.textureIntensityMultiplier);
                    propertyBlock.SetVector("_MeltingDirection", new Vector4(meltingDirection.x, meltingDirection.y, 0, 0));
                }
            }
            
            renderer.SetPropertyBlock(propertyBlock);
        }
        
        public float GetCurrentDeformationLevel()
        {
            return currentDeformationLevel;
        }
        
        public string GetSystemStats()
        {
            string remedyStatus = isRemedyTransitionActive ? "Remedy Transition Active" : "Normal";
            string phase = GetCurrentPhase(currentSanity);
            return $"Sanity: {currentSanity:F2} | Phase: {phase} | Deformation: {currentDeformationLevel:F2} | Objects: {registeredObjects.Count} | Paused: {isSystemPaused} | Status: {remedyStatus} | Melting: {(currentSanity <= textureTransitionStartThreshold ? meltingPhase.ToString("F2") : "Inactive")}";
        }
        
        /// <summary>
        /// Reseta o tempo de derretimento para reiniciar a animação
        /// </summary>
        public void ResetMeltingTime()
        {
            meltingTime = 0f;
            meltingPhase = 0f;
        }
        
        /// <summary>
        /// Força uma atualização do efeito de derretimento
        /// </summary>
        public void UpdateMeltingEffect()
        {
            if (currentSanity <= textureTransitionStartThreshold)
            {
                meltingTime += Time.deltaTime * meltingSpeed;
                
                if (useOrganicMelting)
                {
                    meltingPhase = Mathf.PerlinNoise(meltingTime * 0.1f, Time.time * 0.05f);
                }
                else
                {
                    meltingPhase = Mathf.Sin(meltingTime) * 0.5f + 0.5f;
                }
            }
        }
        
        public void ForceUpdateAll()
        {
            if (isSystemPaused) return;
            
            foreach (var obj in registeredObjects)
            {
                if (obj != null)
                {
                    UpdateObjectDeformation(obj);
                }
            }
        }
        

        

        
        /// <summary>
        /// Lista todos os objetos registrados para debug
        /// </summary>
        [ContextMenu("Debug: List All Registered Objects")]
        public void DebugListAllRegisteredObjects()
        {
            Debug.Log($"=== 🎭 DEFORMATION MANAGER - REGISTERED OBJECTS ({registeredObjects.Count}) ===");
            
            if (registeredObjects.Count == 0)
            {
                Debug.LogWarning("❌ No objects registered! Make sure objects have DeformableObject component and are active.");
                return;
            }
            
            int activeCount = 0;
            int validCount = 0;
            
            for (int i = 0; i < registeredObjects.Count; i++)
            {
                var obj = registeredObjects[i];
                
                if (obj == null)
                {
                    Debug.Log($"{i + 1:D2}. ❌ NULL OBJECT (will be cleaned up)");
                    continue;
                }
                
                validCount++;
                
                bool isActive = obj.gameObject.activeInHierarchy;
                if (isActive) activeCount++;
                
                var renderer = obj.GetRenderer();
                var config = obj.GetConfiguration();
                
                string materialInfo = "NO MATERIAL";
                if (renderer?.sharedMaterial != null)
                {
                    materialInfo = $"'{renderer.sharedMaterial.name}' ({renderer.sharedMaterial.shader.name})";
                }
                
                string status = isActive ? "✅ ACTIVE" : "⚠️ INACTIVE";
                
                Debug.Log($"{i + 1:D2}. {status} '{obj.name}'");
                Debug.Log($"    🎨 Material: {materialInfo}");
                Debug.Log($"    ⚙️ Config: Mesh={config.allowMeshDeformation}, Texture={config.allowTextureDeformation}");
                Debug.Log($"    📍 Position: {obj.transform.position}");
            }
            
            Debug.Log($"📊 Summary: {validCount} valid objects, {activeCount} active, {registeredObjects.Count - validCount} null");
            Debug.Log($"🎛️ System Status: {GetSystemStats()}");
            Debug.Log("=== END OBJECT LIST ===");
        }
        
        private void OnGUI()
        {
            if (!showDebugGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 100, 300, 250));
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label("🎭 Deformation System Debug");
            GUILayout.Label($"Current Sanity: {currentSanity:F2}");
            GUILayout.Label($"Deformation Level: {currentDeformationLevel:F2}");
            GUILayout.Label($"Registered Objects: {registeredObjects.Count}");
            GUILayout.Label($"System Paused: {isSystemPaused}");
            
            // Mostra status da transição de remédio
            if (isRemedyTransitionActive)
            {
                GUILayout.Label("💊 REMEDY TRANSITION ACTIVE", GUI.skin.box);
            }
            
            // Mostra fase atual
            string currentPhase = GetCurrentPhase(currentSanity);
            if (currentSanity <= textureTransitionStartThreshold)
            {
                GUILayout.Label($"⚠️ {currentPhase}", GUI.skin.box);
            }
            else
            {
                GUILayout.Label($"😇 {currentPhase}", GUI.skin.box);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
    
    [System.Serializable]
    public class DeformationValues
    {
        [Range(0f, 1f)] public float insanityLevel = 0f;
        [Range(0f, 2f)] public float uvDisplacementStrength = 0f;
        [Range(0f, 1f)] public float corruptionInfluence = 0f;
        [Range(0f, 5f)] public float corruptionNormalStrength = 0f;
        [Range(0f, 2f)] public float deformStrength = 0f;
        [Range(0f, 5f)] public float deformFrequency = 1f;
    }
}
