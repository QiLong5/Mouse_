using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Spine.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 挂在 TMP_Text / Image / SkeletonGraphic 所在物体上
/// 选择对应的 LocalizationKey，语言切换时自动从 LanguageTable 查表赋值
/// </summary>
public class LanguageItem : MonoBehaviour
{
    public LocalizationKey key;

    private TMP_Text _tmpText;
    private Image _image;
    private SkeletonGraphic _skeleton;
    private bool _initialized;

    void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (_initialized) return;
        _tmpText = GetComponent<TMP_Text>();
        _image = GetComponent<Image>();
        _skeleton = GetComponent<SkeletonGraphic>();
        _initialized = true;
    }

    public void ApplyLanguage(string langCode, LanguageTable table)
    {
        CacheComponents();

        if (table == null) return;

        LanguageRow row = table.GetRow(key);
        if (row == null) return;

        LanguageEntry entry = row.GetEntry(langCode);
        if (entry == null) return;

        switch (row.type)
        {
            case RowType.Text:
                if (_tmpText != null) _tmpText.text = entry.text;
                break;
            case RowType.Image:
                if (_image != null && entry.sprite != null) _image.sprite = entry.sprite;
                break;
            case RowType.Skeleton:
                if (_skeleton != null && !string.IsNullOrEmpty(entry.animationName))
                {
                    _skeleton.startingAnimation = entry.animationName;
                    _skeleton.Initialize(true);
                }
                break;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LanguageItem))]
public class LanguageItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        LanguageItem item = (LanguageItem)target;

        bool hasTMP = item.GetComponent<TMP_Text>() != null;
        bool hasImage = item.GetComponent<Image>() != null;
        bool hasSkeleton = item.GetComponent<SkeletonGraphic>() != null;

        if (!hasTMP && !hasImage && !hasSkeleton)
        {
            EditorGUILayout.HelpBox("未检测到 TMP_Text / Image / SkeletonGraphic", MessageType.Warning);
        }
        else
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("检测到：");
            if (hasTMP) sb.Append("TMP_Text  ");
            if (hasImage) sb.Append("Image  ");
            if (hasSkeleton) sb.Append("SkeletonGraphic  ");
            EditorGUILayout.HelpBox(sb.ToString(), MessageType.Info);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("key"), new GUIContent("本地化键"));

        // 预览：从表中显示当前行信息
        if (Application.isPlaying && GetLanguage.Instance != null && GetLanguage.Instance.table != null)
        {
            LanguageRow row = GetLanguage.Instance.table.GetRow(item.key);
            if (row != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"类型: {row.type}  |  条目数: {row.entries.Count}", EditorStyles.miniLabel);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
