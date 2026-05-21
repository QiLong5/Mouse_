using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum LanguageType
{
    ChineseSimplified,      // zh-CN 简体中文
    ChineseTraditional,     // zh-TW 繁体中文（台湾/香港/澳门通用）
    English,                // en-US 英文
    Japanese,               // ja-JP 日文
    Korean,                 // ko-KR 韩文
    French,                 // fr-FR 法文
    Russian,                // ru-RU 俄文
    Arabic,                 // ar-SA 阿拉伯文
    Thai,                   // th-TH 泰文
    German                  // de-DE 德文
}

public class GetLanguage : MonoBehaviour
{
    [Header("语言数据表")]
    public LanguageTable table;

    [Header("当前语言")]
    public string currentLanguage = "en-US";

    [Header("Editor测试 - 选择语言后点击下方按钮")]
    public LanguageType editorLanguage = LanguageType.English;

    public static GetLanguage Instance { get; private set; }

    private static readonly Dictionary<string, string> languageMapping = new Dictionary<string, string>
    {
        { "zh",    "zh-CN" }, { "zh-cn", "zh-CN" }, { "zh-CN", "zh-CN" },
        { "zh-tw", "zh-TW" }, { "zh-TW", "zh-TW" },
        { "zh-hk", "zh-TW" }, { "zh-HK", "zh-TW" },
        { "zh-mo", "zh-TW" }, { "zh-MO", "zh-TW" },
        { "en",    "en-US" }, { "en-us", "en-US" }, { "en-US", "en-US" },
        { "en-gb", "en-US" }, { "en-GB", "en-US" },
        { "ja",    "ja-JP" }, { "ja-jp", "ja-JP" }, { "ja-JP", "ja-JP" },
        { "ko",    "ko-KR" }, { "ko-kr", "ko-KR" }, { "ko-KR", "ko-KR" },
        { "fr",    "fr-FR" }, { "fr-fr", "fr-FR" }, { "fr-FR", "fr-FR" },
        { "fr-ca", "fr-FR" }, { "fr-CA", "fr-FR" },
        { "fr-be", "fr-FR" }, { "fr-BE", "fr-FR" },
        { "ru",    "ru-RU" }, { "ru-ru", "ru-RU" }, { "ru-RU", "ru-RU" },
        { "ar",    "ar-SA" }, { "ar-sa", "ar-SA" }, { "ar-SA", "ar-SA" },
        { "ar-ae", "ar-SA" }, { "ar-AE", "ar-SA" },
        { "ar-eg", "ar-SA" }, { "ar-EG", "ar-SA" },
        { "th",    "th-TH" }, { "th-th", "th-TH" }, { "th-TH", "th-TH" },
        { "de",    "de-DE" }, { "de-de", "de-DE" }, { "de-DE", "de-DE" },
        { "de-at", "de-DE" }, { "de-AT", "de-DE" },
        { "de-ch", "de-DE" }, { "de-CH", "de-DE" },
    };

    public static string GetLanguageCode(LanguageType lang)
    {
        switch (lang)
        {
            case LanguageType.ChineseSimplified:  return "zh-CN";
            case LanguageType.ChineseTraditional: return "zh-TW";
            case LanguageType.English:            return "en-US";
            case LanguageType.Japanese:           return "ja-JP";
            case LanguageType.Korean:             return "ko-KR";
            case LanguageType.French:             return "fr-FR";
            case LanguageType.Russian:            return "ru-RU";
            case LanguageType.Arabic:             return "ar-SA";
            case LanguageType.Thai:               return "th-TH";
            case LanguageType.German:             return "de-DE";
            default:                              return "en-US";
        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return; 
        }
    }

    void Start()
    {
#if UNITY_EDITOR
        SetLanguage(editorLanguage);
        Debug.Log($"[Editor模式] 当前语言: {currentLanguage}");
#else
        LoadLanguageFromSystem();
#endif
    }

    private void LoadLanguageFromSystem()
    {
#if UNITY_LUNA
        try
        {
            string systemLang = Luna.Unity.Playable.GetPreferredLanguage();
            currentLanguage = NormalizeLanguageCode(systemLang);
            Debug.Log($"[Luna] 语言: {systemLang} → {currentLanguage}");
            OnLanguageChanged();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"获取系统语言失败: {e.Message}，使用默认语言");
            currentLanguage = "en-US";
            OnLanguageChanged();
        }
#else
        Debug.Log($"非Luna环境，使用默认语言: {currentLanguage}");
        OnLanguageChanged();
#endif
    }

    private string NormalizeLanguageCode(string lang)
    {
        if (string.IsNullOrEmpty(lang)) return "en-US";

        if (languageMapping.TryGetValue(lang, out string normalized)) return normalized;

        string lower = lang.ToLower();
        if (languageMapping.TryGetValue(lower, out normalized)) return normalized;

        if (lang.Contains("-")) return lang;

        Debug.LogWarning($"未识别的语言代码: {lang}，使用默认英文");
        return "en-US";
    }

    public void SetLanguage(LanguageType langType)
    {
        currentLanguage = GetLanguageCode(langType);
        Debug.Log($"语言切换为: {currentLanguage}");
        OnLanguageChanged();
    }

    public void SetLanguage(string langCode)
    {
        if (!string.IsNullOrEmpty(langCode))
        {
            currentLanguage = NormalizeLanguageCode(langCode);
            Debug.Log($"语言设置为: {currentLanguage}");
            OnLanguageChanged();
        }
    }

    private void OnLanguageChanged()
    {
        RefreshAllLanguageItems();
        Debug.Log($"=== 语言已更新: {currentLanguage} ===");
    }

    private void RefreshAllLanguageItems()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        List<LanguageItem> allItems = new List<LanguageItem>();

        foreach (GameObject root in rootObjects)
        {
            LanguageItem[] items = root.GetComponentsInChildren<LanguageItem>(true);
            allItems.AddRange(items);
        }

        foreach (LanguageItem item in allItems)
        {
            item.ApplyLanguage(currentLanguage, table);
#if UNITY_EDITOR
            EditorUtility.SetDirty(item.gameObject);
#endif
        }

#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif

        Debug.Log($"[RefreshLanguage] 刷新 {allItems.Count} 个 LanguageItem，语言: {currentLanguage}");
    }

    public string GetCurrentLanguage()
    {
        return currentLanguage;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GetLanguage))]
public class GetLanguageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GetLanguage script = (GetLanguage)target;

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("应用选择的语言", GUILayout.Height(35)))
        {
            script.SetLanguage(script.editorLanguage);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "使用说明：\n" +
            "在需要多语言的物体上挂载 LanguageItem 组件\n" +
            "同一物体上有 TMP_Text / Image / SkeletonGraphic 时会自动检测\n" +
            "在 LanguageItem 的 entries 列表里配置各语言的文本、图片或动画名\n\n" +
            "打包后会自动从 Luna 系统获取用户的偏好语言",
            MessageType.Info);
    }
}
#endif
