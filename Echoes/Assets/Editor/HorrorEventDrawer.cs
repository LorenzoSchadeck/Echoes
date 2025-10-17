using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HorrorEvent))]
public class HorrorEventDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Header com nome do evento
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Horror Event Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.PropertyField(property.FindPropertyRelative("eventName"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("type"));

        HorrorEventType eventType = (HorrorEventType)property.FindPropertyRelative("type").enumValueIndex;

        EditorGUILayout.Space(5);
    
        
        EditorGUILayout.LabelField("Event Parameters", EditorStyles.boldLabel);

        switch (eventType)
        {
            // Limiar 2: Ansiedade
            case HorrorEventType.PlaySpatialSound:
                EditorGUILayout.LabelField("Spatial Sound Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("spatialSoundEvent"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("spatialSoundTarget"));
                break;
            case HorrorEventType.RadioStaticBurst:
                EditorGUILayout.LabelField("Radio Static Burst Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("staticBurstEvent"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("radioTarget"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("staticBurstDuration"));
                EditorGUILayout.HelpBox("⚠️ Evento será CANCELADO se o rádio NÃO estiver desligado (Off). Bloqueado durante: Playing, Static e PuzzleMode.", MessageType.Warning);
                break;
            case HorrorEventType.LightFlicker:
                EditorGUILayout.LabelField("Light Flicker Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("flickerLights"));
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Audio Settings (Optional)", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("flickerSoundEvent"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("flickerSoundTarget"));
                EditorGUILayout.HelpBox("Som será tocado A CADA piscada. Se soundTarget especificado: som em posição fixa. Se vazio: som em CADA luz individual. Range: 70m.", MessageType.Info);
                break;

            // Limiar 3
            case HorrorEventType.VisualFlash:
                EditorGUILayout.LabelField("Visual Flash Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("visualFlashDuration"));
                EditorGUILayout.HelpBox("Visual Flash afeta post-processing + TODOS os materiais com _InsanityLevel. Só funciona com sanidade > 70%.", MessageType.Info);
                break;
            case HorrorEventType.FalseAlarmClock:
                EditorGUILayout.LabelField("False Alarm Clock Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("alarmTarget"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("falseAlarmDuration"));
                break;

            // Limiar 4
            case HorrorEventType.SpawnCoveredBody:
                EditorGUILayout.LabelField("Spawn Covered Body Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("coveredBodyPrefab"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("coveredBodySpawnPoint"));
                break;
            case HorrorEventType.GuiltChorusBurst:
                EditorGUILayout.LabelField("Guilt Chorus Burst Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("guiltChorusEvent1"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("guiltChorusTarget1"));
                EditorGUILayout.Space(3);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("guiltChorusEvent2"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("guiltChorusTarget2"));
                EditorGUILayout.HelpBox("Os dois áudios tocarão simultaneamente nos objetos especificados.", MessageType.Info);
                break;
        }

        EditorGUILayout.Space(10);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return -2; // Altura automática
    }
}