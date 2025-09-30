using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor customizado para o SurfaceDetector que facilita a configuração no Unity Editor.
/// </summary>
[CustomEditor(typeof(SurfaceDetector))]
public class SurfaceDetectorEditor : Editor
{
    private SerializedProperty surfaceTypeProperty;
    private SerializedProperty surfaceParameterValueProperty;

    private void OnEnable()
    {
        surfaceTypeProperty = serializedObject.FindProperty("surfaceType");
        surfaceParameterValueProperty = serializedObject.FindProperty("surfaceParameterValue");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SurfaceDetector detector = (SurfaceDetector)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Surface Configuration", EditorStyles.boldLabel);
        
        // Surface Type com preview visual
        EditorGUILayout.PropertyField(surfaceTypeProperty, new GUIContent("Surface Type", "Tipo de superfície que afeta o som dos passos"));
        
        // Mostra o valor do parâmetro (somente leitura)
        GUI.enabled = false;
        EditorGUILayout.FloatField(new GUIContent("FMOD Parameter Value", "Valor numérico enviado para o FMOD"), detector.SurfaceParameterValue);
        GUI.enabled = true;

        EditorGUILayout.Space();
        
        // Informações adicionais
        EditorGUILayout.HelpBox(GetSurfaceDescription(detector.Surface), MessageType.Info);
        
        // Botão de teste (apenas em Play Mode)
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Test Surface Sound"))
            {
                TestSurfaceSound(detector);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Retorna uma descrição da superfície selecionada.
    /// </summary>
    private string GetSurfaceDescription(SurfaceType surface)
    {
        return surface switch
        {
            SurfaceType.Carpet => "Som abafado e suave, comum em áreas residenciais com carpete ou tapetes.",
            SurfaceType.Tiles => "Som duro e claro, típico de pisos de ladrilho, cerâmica ou pedra.",
            _ => "Superfície personalizada."
        };
    }

    /// <summary>
    /// Testa o som da superfície (apenas disponível em Play Mode).
    /// </summary>
    private void TestSurfaceSound(SurfaceDetector detector)
    {
        // Procura por um PlayerMovement na cena para testar o som
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            // Simula um passo na posição do detector
            Debug.Log($"[SurfaceDetectorEditor] Testando som da superfície: {detector.Surface} (Valor: {detector.SurfaceParameterValue})");
            
            // Nota: Para implementar o teste real, seria necessário acessar métodos privados
            // ou criar um método público de teste no PlayerMovement
        }
        else
        {
            Debug.LogWarning("[SurfaceDetectorEditor] PlayerMovement não encontrado na cena para teste.");
        }
    }

    /// <summary>
    /// Desenha ícones no Scene View para identificar visualmente as superfícies.
    /// </summary>
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawSurfaceGizmo(SurfaceDetector detector, GizmoType gizmoType)
    {
        if (!detector.enabled) return;

        // Define cor baseada no tipo de superfície
        Gizmos.color = GetSurfaceColor(detector.Surface);
        
        // Desenha um ícone na posição do objeto
        Vector3 position = detector.transform.position;
        Gizmos.DrawIcon(position, "AudioSource Icon", true);
        
        // Desenha um pequeno cubo colorido
        Gizmos.DrawWireCube(position + Vector3.up * 0.1f, Vector3.one * 0.2f);
        
        // Mostra o nome da superfície se selecionado
        if ((gizmoType & GizmoType.Selected) != 0)
        {
            Vector3 labelPos = position + Vector3.up * 0.5f;
            UnityEditor.Handles.Label(labelPos, detector.Surface.ToString());
        }
    }

    /// <summary>
    /// Retorna uma cor representativa para cada tipo de superfície.
    /// </summary>
    private static Color GetSurfaceColor(SurfaceType surface)
    {
        return surface switch
        {
            SurfaceType.Carpet => new Color(0.8f, 0.2f, 0.2f), // Vermelho escuro
            SurfaceType.Tiles => new Color(0.7f, 0.7f, 0.9f), // Azul claro
            _ => Color.magenta
        };
    }
}