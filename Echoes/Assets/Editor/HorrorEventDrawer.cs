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
            case HorrorEventType.VisualFlash:
                EditorGUILayout.LabelField("Visual Flash Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("visualFlashDuration"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("visualFlashPeak"));
                break;

            case HorrorEventType.FalseAlarmClock:
                EditorGUILayout.LabelField("False Alarm Clock Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("falseAlarmDuration"));
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return -2; // Altura automática
    }
}