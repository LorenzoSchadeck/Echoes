using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NeighborEvent))]
public class NeighborEventDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Header com nome do evento
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🏠 Neighbor Event Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.PropertyField(property.FindPropertyRelative("eventName"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("type"));

        NeighborEventType eventType = (NeighborEventType)property.FindPropertyRelative("type").enumValueIndex;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Event Parameters", EditorStyles.boldLabel);

        switch (eventType)
        {
            case NeighborEventType.RotationWithBoxesAndAudio:
                EditorGUILayout.LabelField("🔄 Rotation + Boxes + Audio Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Rotation Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("objectsToRotate"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("rotationAmount"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("rotationDuration"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Moving Boxes Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("boxesToEnable"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Audio Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("movingSounds"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("audioTarget"));
                
                EditorGUILayout.HelpBox("Rotaciona objetos, habilita caixas de mudança e toca sons aleatórios simultaneamente.", MessageType.Info);
                break;

            case NeighborEventType.SoundWithRotation:
                EditorGUILayout.LabelField("🔊 Sound + Rotation Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Audio Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("randomSounds"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("soundTarget"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Rotation Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("rotationObjects"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("soundRotationAmount"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("soundRotationDuration"));
                
                EditorGUILayout.HelpBox("Toca um som aleatório da lista e rotaciona objetos simultaneamente.", MessageType.Info);
                break;

            case NeighborEventType.JumpScare:
                EditorGUILayout.LabelField("👻 Jump Scare Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Scare Object Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("jumpScareObject"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Audio Settings (Optional)", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("jumpScareSound"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("jumpScareSoundTarget"));
                
                EditorGUILayout.HelpBox("⚠️ O objeto será habilitado por EXATAMENTE 1 segundo e desabilitado automaticamente. Som de susto é opcional.", MessageType.Warning);
                break;

            case NeighborEventType.AudioOnly:
                EditorGUILayout.LabelField("🎵 Audio Only Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Audio Events", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("audioOnlyEvents"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("audioOnlyTarget"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Playback Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("playMultipleSounds"));
                
                bool playMultiple = property.FindPropertyRelative("playMultipleSounds").boolValue;
                if (playMultiple)
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("soundDelay"));
                    EditorGUILayout.HelpBox("Modo múltiplos sons: Todos os sons da lista serão tocados sequencialmente com o delay especificado.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Modo som aleatório: Apenas um som aleatório da lista será selecionado e tocado.", MessageType.Info);
                }
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