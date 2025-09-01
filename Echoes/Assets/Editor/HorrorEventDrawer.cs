using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HorrorEvent))]
public class HorrorEventDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Começa a desenhar a propriedade.
        EditorGUI.BeginProperty(position, label, property);

        // Encontra as sub-propriedades da nossa struct pelo nome da variável.
        var eventNameProp = property.FindPropertyRelative("eventName");
        var typeProp = property.FindPropertyRelative("type");
        var minInsanityProp = property.FindPropertyRelative("minLatentInsanity");

        // Desenha os campos que são sempre visíveis.
        EditorGUILayout.PropertyField(eventNameProp);
        EditorGUILayout.PropertyField(typeProp);
        EditorGUILayout.PropertyField(minInsanityProp);
        
        EditorGUILayout.Space(); // Adiciona um pequeno espaço

        // Obtém o valor do enum para decidir o que mais desenhar.
        HorrorEventType eventType = (HorrorEventType)typeProp.enumValueIndex;
        
        // --- A LÓGICA PRINCIPAL ---
        // Desenha campos adicionais com base no tipo de evento selecionado.
        switch (eventType)
        {
            case HorrorEventType.VisualFlash:
                EditorGUILayout.LabelField("Visual Flash Settings", EditorStyles.boldLabel);
                var durationProp = property.FindPropertyRelative("visualFlashDuration");
                var peakProp = property.FindPropertyRelative("visualFlashPeak");
                EditorGUILayout.PropertyField(durationProp);
                EditorGUILayout.PropertyField(peakProp);
                break;

            case HorrorEventType.FalseAlarmClock:
                EditorGUILayout.LabelField("False Alarm Clock Settings", EditorStyles.boldLabel);
                var alarmDurationProp = property.FindPropertyRelative("falseAlarmDuration");
                EditorGUILayout.PropertyField(alarmDurationProp);
                break;
        }

        // Finaliza o desenho da propriedade.
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Retorna a altura padrão. O EditorGUILayout ajustará o espaço conforme necessário.
        return base.GetPropertyHeight(property, label);
    }
}