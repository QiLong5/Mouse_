using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
[CanEditMultipleObjects]
public class AudioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        if (GUILayout.Button("生成 SK 枚举"))
        {
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();

            var mgr = (AudioManager)target;
            SKGenerator.Generate(mgr.audioEntries);
            Debug.Log($"[SKGenerator] 已生成 {mgr.audioEntries.Count} 个 key");
        }
    }
}
