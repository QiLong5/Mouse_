using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text;
#endif

public enum RowType { Text, Image, Skeleton }

[System.Serializable]
public class LanguageEntry
{
    public LanguageType language = LanguageType.English;
    [TextArea(1, 3)] public string text;
    public Sprite sprite;
    public string animationName;
}

[System.Serializable]
public class LanguageRow
{
    public string name;
    public RowType type;
    public List<LanguageEntry> entries = new List<LanguageEntry>();

    public LanguageEntry GetEntry(string langCode)
    {
        LanguageEntry fallback = null;
        foreach (LanguageEntry entry in entries)
        {
            if (GetLanguage.GetLanguageCode(entry.language) == langCode)
                return entry;
            if (entry.language == LanguageType.English)
                fallback = entry;
        }
        return fallback;
    }
}

[CreateAssetMenu(fileName = "LanguageTable", menuName = "Localization/Language Table")]
public class LanguageTable : ScriptableObject
{
    public List<LanguageRow> rows = new List<LanguageRow>();

    public LanguageRow GetRow(LocalizationKey key)
    {
        string keyName = key.ToString();
        foreach (LanguageRow row in rows)
        {
            if (row.name == keyName) return row;
        }
        return null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LanguageTable))]
public class LanguageTableEditor : Editor
{
    private string FoldoutKey(int index) => $"LT_{target.GetInstanceID()}_{index}";
    private bool GetFoldout(int i) => SessionState.GetBool(FoldoutKey(i), false);
    private void SetFoldout(int i, bool v) => SessionState.SetBool(FoldoutKey(i), v);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        LanguageTable table = (LanguageTable)target;

        SerializedProperty rowsProp = serializedObject.FindProperty("rows");

        for (int i = 0; i < rowsProp.arraySize; i++)
        {
            SerializedProperty row = rowsProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = row.FindPropertyRelative("name");
            SerializedProperty typeProp = row.FindPropertyRelative("type");
            SerializedProperty entriesProp = row.FindPropertyRelative("entries");

            RowType rowType = (RowType)typeProp.enumValueIndex;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行：折叠 + 名称输入 + 删除
            EditorGUILayout.BeginHorizontal();
            SetFoldout(i, EditorGUILayout.Foldout(GetFoldout(i), GUIContent.none, true));
            EditorGUILayout.LabelField("键名", GUILayout.Width(30));
            nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue);
            EditorGUILayout.LabelField($"[{rowType}]", EditorStyles.miniLabel, GUILayout.Width(65));
            if (GUILayout.Button("删除", GUILayout.Width(45)))
            {
                rowsProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (GetFoldout(i))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(typeProp, new GUIContent("类型"));
                EditorGUILayout.Space(4);

                for (int j = 0; j < entriesProp.arraySize; j++)
                {
                    SerializedProperty entry = entriesProp.GetArrayElementAtIndex(j);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("language"), GUIContent.none, GUILayout.Width(150));

                    switch (rowType)
                    {
                        case RowType.Text:
                            EditorGUILayout.PropertyField(entry.FindPropertyRelative("text"), GUIContent.none);
                            break;
                        case RowType.Image:
                            EditorGUILayout.PropertyField(entry.FindPropertyRelative("sprite"), GUIContent.none);
                            break;
                        case RowType.Skeleton:
                            EditorGUILayout.PropertyField(entry.FindPropertyRelative("animationName"), GUIContent.none);
                            break;
                    }

                    if (GUILayout.Button("×", GUILayout.Width(22)))
                    {
                        entriesProp.DeleteArrayElementAtIndex(j);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);
                if (GUILayout.Button("+ 添加语言", GUILayout.Height(22)))
                    entriesProp.arraySize++;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("+ 添加行", GUILayout.Height(28)))
        {
            rowsProp.arraySize++;
        }

        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("生成 LocalizationKey 枚举", GUILayout.Height(32)))
        {
            serializedObject.ApplyModifiedProperties();
            GenerateEnum(table);
        }
        GUI.backgroundColor = Color.white;

        serializedObject.ApplyModifiedProperties();
    }

    private static void GenerateEnum(LanguageTable table)
    {
        string[] guids = AssetDatabase.FindAssets("LocalizationKey t:Script");
        string assetPath = guids.Length > 0
            ? AssetDatabase.GUIDToAssetPath(guids[0])
            : "Assets/Scripts/GetLanguage/LocalizationKey.cs";

        string fullPath = Path.Combine(
            Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length),
            assetPath);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 此文件由 LanguageTable 自动生成，请勿手动修改");
        sb.AppendLine("public enum LocalizationKey");
        sb.AppendLine("{");
        sb.AppendLine("    None,");
        foreach (LanguageRow row in table.rows)
        {
            if (!string.IsNullOrWhiteSpace(row.name))
                sb.AppendLine($"    {row.name},");
        }
        sb.AppendLine("}");

        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"[LanguageTable] 枚举已生成 → {assetPath}，共 {table.rows.Count} 个键");
    }
}
#endif
