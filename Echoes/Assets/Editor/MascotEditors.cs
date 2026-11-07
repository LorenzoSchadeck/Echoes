using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector do Tinker Crow: mostra cada desenho e deixa marcar os pontos clicando na imagem.
/// Clique: ponto do bico (verde). Shift + clique: altura do fio (linha azul).
/// </summary>
[CustomEditor(typeof(CrowMascot))]
public class CrowMascotEditor : Editor
{
    private const float Preview = 220f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var mascot = (CrowMascot)target;
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Clique na imagem para marcar o bico (onde o acessório encaixa). Shift + clique marca a altura do fio.", MessageType.Info);
        DrawClip(mascot, mascot.arrive);
        DrawClip(mascot, mascot.land);
        DrawClip(mascot, mascot.idle);
    }

    private void DrawClip(CrowMascot mascot, MascotClip clip)
    {
        if (clip == null || clip.frames == null) return;
        EditorGUILayout.LabelField(clip.name, EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (MascotFrame frame in clip.frames)
        {
            if (frame?.sprite == null) continue;
            Rect rect = GUILayoutUtility.GetRect(Preview, Preview, GUILayout.Width(Preview), GUILayout.Height(Preview));
            EditorGUI.DrawRect(rect, new Color(0.92f, 0.9f, 0.85f));
            GUI.DrawTexture(rect, frame.sprite.texture, ScaleMode.ScaleToFit);

            float wireY = rect.y + frame.wire * rect.height;
            EditorGUI.DrawRect(new Rect(rect.x, wireY - 1, rect.width, 2), new Color(0.2f, 0.5f, 1f, 0.8f));
            Vector2 beak = new Vector2(rect.x + frame.beak.x * rect.width, rect.y + frame.beak.y * rect.height);
            EditorGUI.DrawRect(new Rect(beak.x - 4, beak.y - 4, 8, 8), Color.green);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                Undo.RecordObject(mascot, "Ponto do mascote");
                Vector2 p = new Vector2((e.mousePosition.x - rect.x) / rect.width, (e.mousePosition.y - rect.y) / rect.height);
                if (e.shift) frame.wire = Mathf.Clamp01(p.y);
                else frame.beak = p;
                EditorUtility.SetDirty(mascot);
                e.Use();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}

/// <summary>Inspector do acessório: clique na imagem para marcar o ponto que encaixa no mascote.</summary>
[CustomEditor(typeof(MascotAccessory))]
public class MascotAccessoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var accessory = (MascotAccessory)target;
        if (accessory.sprite == null) return;
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Clique na imagem para marcar o ponto de encaixe (na chave, a argola que fica no bico).", MessageType.Info);
        Rect rect = GUILayoutUtility.GetRect(220, 220, GUILayout.Width(220), GUILayout.Height(220));
        EditorGUI.DrawRect(rect, new Color(0.92f, 0.9f, 0.85f));
        GUI.DrawTexture(rect, accessory.sprite.texture, ScaleMode.ScaleToFit);
        Vector2 point = new Vector2(rect.x + accessory.attachPoint.x * rect.width, rect.y + accessory.attachPoint.y * rect.height);
        EditorGUI.DrawRect(new Rect(point.x - 4, point.y - 4, 8, 8), Color.green);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            Undo.RecordObject(accessory, "Ponto do acessório");
            accessory.attachPoint = new Vector2((e.mousePosition.x - rect.x) / rect.width, (e.mousePosition.y - rect.y) / rect.height);
            EditorUtility.SetDirty(accessory);
            e.Use();
        }
    }
}
