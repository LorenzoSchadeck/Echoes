using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Echoes.PsychSystem.Editor
{
    /// <summary>
    /// Utilitário para migração automática do sistema antigo para o novo sistema psicológico.
    /// Use apenas uma vez durante a migração.
    /// </summary>
    public class PsychSystemMigrationTool : EditorWindow
    {
        private bool showOldSystemObjects = true;
        private bool showMigrationSteps = true;
        private Vector2 scrollPosition;
        
        [MenuItem("Echoes/Psych System Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<PsychSystemMigrationTool>("Psych System Migration");
            window.minSize = new Vector2(500, 600);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🎭 Horror Psych System Migration", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "Este utilitário irá migrar automaticamente do sistema antigo para o novo sistema psicológico.\n" +
                "⚠️ IMPORTANTE: Faça backup da cena antes de prosseguir!", 
                MessageType.Warning
            );
            
            GUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawSystemAnalysis();
            GUILayout.Space(20);
            DrawMigrationActions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSystemAnalysis()
        {
            showOldSystemObjects = EditorGUILayout.Foldout(showOldSystemObjects, "📊 Análise do Sistema Atual");
            
            if (showOldSystemObjects)
            {
                EditorGUI.indentLevel++;
                
                // Encontra componentes do sistema antigo
                var oldManagers = FindObjectsByType<Echoes.Effects.CorruptionEffectsManager>(FindObjectsSortMode.None);
                var simpleControllers = FindObjectsByType<Echoes.Effects.SimpleCorruptionController>(FindObjectsSortMode.None);
                var enhancedControllers = FindObjectsByType<Echoes.Effects.EnhancedCorruptionController>(FindObjectsSortMode.None);
                
                // Encontra componentes do sistema novo
                var newSystem = FindFirstObjectByType<HorrorPsychSystem>();
                var newControllers = FindObjectsByType<PsychCorruptionController>(FindObjectsSortMode.None);
                
                EditorGUILayout.LabelField("🔍 Sistema Antigo Encontrado:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"• CorruptionEffectsManager: {oldManagers.Length}");
                EditorGUILayout.LabelField($"• SimpleCorruptionController: {simpleControllers.Length}");
                EditorGUILayout.LabelField($"• EnhancedCorruptionController: {enhancedControllers.Length}");
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("✨ Sistema Novo Encontrado:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"• HorrorPsychSystem: {(newSystem != null ? 1 : 0)}");
                EditorGUILayout.LabelField($"• PsychCorruptionController: {newControllers.Length}");
                
                if (oldManagers.Length > 0 || simpleControllers.Length > 0 || enhancedControllers.Length > 0)
                {
                    EditorGUILayout.HelpBox("Sistema antigo detectado! Migração recomendada.", MessageType.Info);
                }
                else if (newSystem != null)
                {
                    EditorGUILayout.HelpBox("✅ Sistema novo já implementado!", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawMigrationActions()
        {
            showMigrationSteps = EditorGUILayout.Foldout(showMigrationSteps, "🔄 Ações de Migração");
            
            if (showMigrationSteps)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.Space(10);
                
                // Botão para criar novo sistema
                if (GUILayout.Button("🆕 1. Criar HorrorPsychSystem", GUILayout.Height(30)))
                {
                    CreateNewPsychSystem();
                }
                
                GUILayout.Space(5);
                
                // Botão para migrar controladores
                if (GUILayout.Button("🔄 2. Migrar Controladores", GUILayout.Height(30)))
                {
                    MigrateControllers();
                }
                
                GUILayout.Space(5);
                
                // Botão para validar migração
                if (GUILayout.Button("✅ 3. Validar Migração", GUILayout.Height(30)))
                {
                    ValidateMigration();
                }
                
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Execute os passos em ordem!", MessageType.Info);
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void CreateNewPsychSystem()
        {
            // Verifica se já existe
            var existing = FindFirstObjectByType<HorrorPsychSystem>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Sistema Existe", "HorrorPsychSystem já existe na cena!", "OK");
                return;
            }
            
            // Cria o GameObject
            var psychSystemGO = new GameObject("HorrorPsychSystem");
            var psychSystem = psychSystemGO.AddComponent<HorrorPsychSystem>();
            
            // Cria thresholds padrão baseados no sistema antigo
            var oldManager = FindFirstObjectByType<Echoes.Effects.CorruptionEffectsManager>();
            
            Debug.Log("[Migration] HorrorPsychSystem criado com sucesso!");
            EditorUtility.DisplayDialog("Sucesso", "HorrorPsychSystem criado!\n\nConfigure os thresholds no Inspector.", "OK");
            
            // Seleciona o objeto criado
            Selection.activeGameObject = psychSystemGO;
        }
        
        private void MigrateControllers()
        {
            int migratedCount = 0;
            
            // Migra SimpleCorruptionControllers
            var simpleControllers = FindObjectsByType<Echoes.Effects.SimpleCorruptionController>(FindObjectsSortMode.None);
            foreach (var oldController in simpleControllers)
            {
                MigrateSimpleController(oldController);
                migratedCount++;
            }
            
            // Migra EnhancedCorruptionControllers
            var enhancedControllers = FindObjectsByType<Echoes.Effects.EnhancedCorruptionController>(FindObjectsSortMode.None);
            foreach (var oldController in enhancedControllers)
            {
                MigrateEnhancedController(oldController);
                migratedCount++;
            }
            
            Debug.Log($"[Migration] {migratedCount} controladores migrados!");
            EditorUtility.DisplayDialog("Migração Concluída", $"{migratedCount} controladores migrados com sucesso!", "OK");
        }
        
        private void MigrateSimpleController(Echoes.Effects.SimpleCorruptionController oldController)
        {
            var gameObject = oldController.gameObject;
            
            // Adiciona novo controlador
            var newController = gameObject.AddComponent<PsychCorruptionController>();
            
            // Cria perfil baseado no controlador antigo (usando reflection se necessário)
            var profile = new CorruptionProfile();
            profile.allowTextureCorruption = true;
            profile.allowMeshDeformation = true; // SimpleController permitia ambos
            
            // Remove o controlador antigo
            DestroyImmediate(oldController);
            
            Debug.Log($"[Migration] Migrado SimpleCorruptionController em {gameObject.name}");
        }
        
        private void MigrateEnhancedController(Echoes.Effects.EnhancedCorruptionController oldController)
        {
            var gameObject = oldController.gameObject;
            
            // Adiciona novo controlador
            var newController = gameObject.AddComponent<PsychCorruptionController>();
            
            // Cria perfil baseado no controlador antigo
            var profile = new CorruptionProfile();
            profile.allowTextureCorruption = true;
            profile.allowMeshDeformation = true; // Enhanced permitia ambos
            
            // Remove o controlador antigo
            DestroyImmediate(oldController);
            
            Debug.Log($"[Migration] Migrado EnhancedCorruptionController em {gameObject.name}");
        }
        
        private void ValidateMigration()
        {
            var issues = new List<string>();
            
            // Verifica se novo sistema existe
            var newSystem = FindFirstObjectByType<HorrorPsychSystem>();
            if (newSystem == null)
            {
                issues.Add("❌ HorrorPsychSystem não encontrado");
            }
            
            // Verifica controladores antigos restantes
            var oldSimple = FindObjectsByType<Echoes.Effects.SimpleCorruptionController>(FindObjectsSortMode.None);
            var oldEnhanced = FindObjectsByType<Echoes.Effects.EnhancedCorruptionController>(FindObjectsSortMode.None);
            
            if (oldSimple.Length > 0)
            {
                issues.Add($"⚠️ {oldSimple.Length} SimpleCorruptionController ainda existem");
            }
            
            if (oldEnhanced.Length > 0)
            {
                issues.Add($"⚠️ {oldEnhanced.Length} EnhancedCorruptionController ainda existem");
            }
            
            // Verifica novos controladores
            var newControllers = FindObjectsByType<PsychCorruptionController>(FindObjectsSortMode.None);
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("✅ Migração Válida", 
                    $"Migração concluída com sucesso!\n\n" +
                    $"• HorrorPsychSystem: ✅\n" +
                    $"• PsychCorruptionControllers: {newControllers.Length}\n" +
                    $"• Controladores antigos: 0", "OK");
            }
            else
            {
                string issueList = string.Join("\n", issues);
                EditorUtility.DisplayDialog("⚠️ Problemas Encontrados", 
                    $"Problemas na migração:\n\n{issueList}", "OK");
            }
        }
    }
}