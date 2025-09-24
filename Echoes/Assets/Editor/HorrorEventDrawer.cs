using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HorrorEvent))]
public class HorrorEventDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUILayout.PropertyField(property.FindPropertyRelative("eventName"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("type"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("maxSanityThreshold"));

        HorrorEventType eventType = (HorrorEventType)property.FindPropertyRelative("type").enumValueIndex;

        switch (eventType)
        {
            // Limiar 2
            case HorrorEventType.PlaySpatialSound:
                EditorGUILayout.LabelField("Spatial Sound Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("spatialSoundEvent"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("spatialSoundOffset"));
                break;
            case HorrorEventType.RadioStaticBurst:
                EditorGUILayout.LabelField("Radio Static Burst Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("staticBurstEvent"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("staticBurstDuration"));
                break;
            case HorrorEventType.QuickLightChange:
                EditorGUILayout.LabelField("Quick Light Change Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("lightChangeDuration"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("lightChangePeakIntensity"));
                break;

            // Limiar 3
            case HorrorEventType.VisualFlash:
                EditorGUILayout.LabelField("Visual Flash Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("visualFlashDuration"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("visualFlashPeak"));
                break;
            case HorrorEventType.FalseAlarmClock:
                EditorGUILayout.LabelField("False Alarm Clock Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("falseAlarmDuration"));
                break;
            case HorrorEventType.TemporaryMaterialSwap:
                EditorGUILayout.LabelField("Temporary Material Swap Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("tempSwapMaterial"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("tempSwapDuration"));
                break;
            case HorrorEventType.PlayVideoOnMaterial:
                EditorGUILayout.LabelField("Play Video On Material Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("videoClip"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("videoTargetMaterial"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("videoDuration"));
                break;

            // Limiar 4
            case HorrorEventType.SpawnCoveredBody:
                EditorGUILayout.LabelField("Spawn Covered Body Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("coveredBodyPrefab"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("coveredBodyOffset"));
                break;
            case HorrorEventType.SpawnHallucination:
                EditorGUILayout.LabelField("Spawn Hallucination Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("hallucinationPrefab"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("hallucinationOffset"));
                break;
            case HorrorEventType.GuiltChorusBurst:
                EditorGUILayout.LabelField("Guilt Chorus Burst Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("guiltChorusEvent"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("guiltChorusDuration"));
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return -2; // Altura automática
    }
}