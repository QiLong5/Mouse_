using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using System.Linq;
using System;
using System.Threading.Tasks;

#if UNITY_EDITOR

using System.IO;
using System.IO.Compression;
using UnityEditor;

#endif

namespace GetLanguagePlugins
{
    public class GetLanguage : MonoBehaviour
    {
        // 子物体数组，对应语言列表顺序（从上到下）
        public GameObject[] languageObjects;
        // 语言与地区映射
        private Dictionary<string, string> defaultRegions = new Dictionary<string, string>
        {
            { "en", "US" }, { "es", "ES" }, { "pt", "PT" }, { "de", "DE" }, { "fr", "FR" },
            { "ja", "JP" }, { "ko", "KR" }, { "ru", "RU" }, { "ar", "SA" },
            { "bg", "BG" }, { "cs", "CZ" }, { "da", "DK" }, { "el", "GR" }, { "fi", "FI" },
            { "he", "IL" }, { "hr", "HR" }, { "hu", "HU" }, { "id", "ID" }, { "it", "IT" },
            { "lt", "LT" }, { "ms", "MY" }, { "nl", "NL" }, { "pl", "PL" }, { "ro", "RO" },
            { "sk", "SK" }, { "sv", "SE" }, { "th", "TH" }, { "tr", "TR" }, { "uk", "UA" },
            { "vi", "VN" }, { "zh", "CN" }, { "nb", "NO" }, { "az", "AZ" }, { "be", "BY" },
            { "kk", "KZ" }, { "uz", "UZ" }, { "tl", "PH" }, { "zh-TW", "TW" }, { "is", "IS" },
            { "hi", "IN" }
        };

        public List<string> supportedLanguages = new List<string>
        {
            "en-US", "es-ES", "pt-PT", "de-DE", "fr-FR", "ja-JP", "ko-KR", "ru-RU", "ar-SA",
            "bg-BG", "cs-CZ", "da-DK", "el-GR", "fi-FI", "he-IL", "hr-HR", "hu-HU", "id-ID",
            "it-IT", "lt-LT", "ms-MY", "nl-NL", "pl-PL", "ro-RO", "sk-SK", "sv-SE", "th-TH",
            "tr-TR", "uk-UA", "vi-VN", "zh-CN", "nb-NO", "az-AZ", "be-BY", "kk-KZ", "uz-UZ",
            "tl-PH", "zh-TW", "is-IS", "hi-IN"
        };

        // 初始化语言检测并激活对应子物体
        void Start()
        {
            string language = GetCountryName();  // 语言代码，例如 "en" 或 "en-US"
            ActivateObjectForLanguage(language);
        }

        // 根据语言代码激活对应子物体
        private void ActivateObjectForLanguage(string language)
        {
            // 获取主要语言和地区
            language = "zh-TW";
            string[] parts = language.Split('-');
            string lang = parts[0];                      // 语言代码
            string region = parts.Length > 1 ? parts[1] : GetDefaultRegion(lang); // 地区代码

            // 匹配语言列表中的索引并激活对应子物体
            int index = GetLanguageIndex($"{lang}-{region}");
            if (index == -1) index = GetLanguageIndex(lang); // 尝试使用仅语言代码匹配
            Debug.Log(index);
            if (index == -1)
            {
                index = 0;
            }
            for (int i = 0; i < languageObjects.Length; i++)
            {
                for (int j = 0; j < languageObjects[i].transform.childCount; j++)
                {
                    languageObjects[i].transform.GetChild(j).gameObject.SetActive(false);
                }
            }
            for (int i = 0; i < languageObjects.Length; i++)
            {
                languageObjects[i].transform.GetChild(index).gameObject.SetActive(true);
            }



        }

        // 获取默认地区
        private string GetDefaultRegion(string language)
        {
            return defaultRegions.ContainsKey(language) ? defaultRegions[language] : "US";
        }
        // 特殊映射表
        private Dictionary<string, string> specialMapping = new Dictionary<string, string>
        {
            { "zh-HK", "zh-TW" },
            { "zh-MO", "zh-TW" },
            { "pt-BR", "pt-PT" },
        };

        // 获取语言索引
        private int GetLanguageIndex(string languageCode)
        {
            if (specialMapping.ContainsKey(languageCode))
            {
                languageCode = specialMapping[languageCode];
            }

            return supportedLanguages.IndexOf(languageCode);
        }

        public string name = "en-US";

        public string GetCountryName()
        {

    #if UNITY_LUNA
    name = Luna.Unity.Playable.GetPreferredLanguage();
    #endif
            Debug.Log(name + "英文名称");
            return name;
        }
    }
}

