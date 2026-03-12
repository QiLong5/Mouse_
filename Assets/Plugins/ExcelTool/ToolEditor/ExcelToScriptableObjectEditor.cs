using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace ExcelTool
{
    /// <summary>
    /// Excel转ScriptableObject编辑器窗口（多表合并-横向配置专用）
    /// </summary>
    public class ExcelToScriptableObjectEditor : EditorWindow
    {
        private string excelFilePath = "Assets/Plugins/ExcelTool/老鼠榨汁.xlsx";
        private List<string> sheetNames = new List<string>();
        private string className = "GameConfig";
        private string outputScriptPath = "Assets/Plugins/ExcelTool/Scripts/Generated";
        private string outputAssetPath = "Assets/Plugins/ExcelTool";
        private List<bool> selectedSheets = new List<bool>();

        private ExcelReader excelReader;
        private List<ExcelHorizontalConfigData> multiSheetData = new List<ExcelHorizontalConfigData>();
        private List<ExcelStructListData> structListData = new List<ExcelStructListData>(); // 新增：结构列表数据
        private Vector2 scrollPosition;
        private Vector2 logScrollPosition;
        private List<string> logMessages = new List<string>();
        private bool autoLoaded = false;

        private List<int> savedSheetIndices = new List<int>();
        private bool showSheetSelection = true;
        private bool manualShowSheetSelection = false; // 手动显示工作表选择的标志
        private bool hasRestoredSelection = false; // 是否已恢复过选择状态

        [MenuItem("Tools/ExcelTool/Excel转ScriptableObject")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExcelToScriptableObjectEditor>("Excel转SO");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            excelReader = new ExcelReader();
            autoLoaded = false;

            // 尝试从现有的GameConfig.asset加载配置
            LoadConfigFromAsset();
        }

        private void OnGUI()
        {
            GUILayout.Label("Excel转ScriptableObject工具（多表合并-横向配置）", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height - 180));

            // Excel文件选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Excel文件:", GUILayout.Width(100));
            string oldPath = excelFilePath;
            excelFilePath = EditorGUILayout.TextField(excelFilePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择Excel文件", "Assets", "xlsx");
                if (!string.IsNullOrEmpty(path))
                {
                    excelFilePath = GetRelativePath(path);
                    autoLoaded = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 自动加载工作表
            if (!autoLoaded && File.Exists(excelFilePath))
            {
                LoadSheetNames();
                autoLoaded = true;
            }

            // 工作表配置区域
            if (sheetNames.Count > 0)
            {
                // 检查资源是否存在
                string assetPath = Path.Combine(outputAssetPath, className + ".asset");
                bool assetExists = File.Exists(assetPath);

                // 根据是否有保存的工作表配置来决定是否显示选择界面
                // 不依赖于GameConfig.asset是否存在
                // 但如果用户手动点击了"重新选择工作表"按钮，要尊重用户的选择
                if (manualShowSheetSelection)
                {
                    // 用户手动点击了按钮，显示选择界面
                    showSheetSelection = true;
                }
                else if (savedSheetIndices.Count > 0)
                {
                    showSheetSelection = false;
                }
                else
                {
                    showSheetSelection = true;
                }

                // 显示当前配置状态
                if (!showSheetSelection && savedSheetIndices.Count > 0)
                {
                    EditorGUILayout.LabelField("当前使用的工作表配置:", EditorStyles.boldLabel);
                    string sheetListText = "";
                    foreach (int index in savedSheetIndices)
                    {
                        if (index > 0 && index <= sheetNames.Count)
                        {
                            sheetListText += sheetNames[index - 1] + ", ";
                        }
                    }
                    if (sheetListText.Length > 0)
                    {
                        sheetListText = sheetListText.Substring(0, sheetListText.Length - 2);
                    }
                    EditorGUILayout.HelpBox($"工作表: {sheetListText}\n点击下方按钮可重新选择", MessageType.Info);

                    if (GUILayout.Button("重新选择工作表", GUILayout.Height(30)))
                    {
                        manualShowSheetSelection = true;
                        showSheetSelection = true;
                    }

                    EditorGUILayout.Space();
                }

                // 工作表选择区域（可折叠）
                if (showSheetSelection)
                {
                    EditorGUILayout.LabelField("选择要合并的工作表（可多选）:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "横向配置表格式：第1列=中文注释，第2列=变量名，第3列=值，第4列=类型\n每一行代表一个字段\n选择多个工作表将合并到一个ScriptableObject中",
                        MessageType.Info);

                    EditorGUILayout.Space();

                    // 确保selectedSheets列表大小匹配
                    while (selectedSheets.Count < sheetNames.Count)
                        selectedSheets.Add(false);
                    while (selectedSheets.Count > sheetNames.Count)
                        selectedSheets.RemoveAt(selectedSheets.Count - 1);

                    // 如果有保存的配置，只在第一次显示时恢复选择状态
                    if (savedSheetIndices.Count > 0 && !hasRestoredSelection)
                    {
                        for (int i = 0; i < selectedSheets.Count; i++)
                        {
                            selectedSheets[i] = savedSheetIndices.Contains(i + 1);
                        }
                        hasRestoredSelection = true;
                    }

                    // 显示工作表复选框列表
                    for (int i = 0; i < sheetNames.Count; i++)
                    {
                        selectedSheets[i] = EditorGUILayout.Toggle(sheetNames[i], selectedSheets[i]);
                    }

                    EditorGUILayout.Space();

                    // 确认选择按钮
                    GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                    if (GUILayout.Button("确认工作表选择", GUILayout.Height(30)))
                    {
                        SaveSheetSelection();
                        showSheetSelection = false;
                        manualShowSheetSelection = false; // 重置手动标志
                        hasRestoredSelection = false; // 重置恢复标志，下次打开时可以恢复新的选择
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.Space();
                }

                // 状态提示
                if (assetExists)
                {
                    EditorGUILayout.HelpBox($"✓ {className}.asset 已存在\n路径: {assetPath}", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"✗ {className}.asset 不存在，将创建新资源", MessageType.Warning);
                }

                EditorGUILayout.Space();

                // 双按钮操作区（只有在确认了工作表选择后才显示）
                if (!showSheetSelection && savedSheetIndices.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();

                    // 快速更新数据按钮
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                    if (GUILayout.Button("快速更新数据（改数值时用）", GUILayout.Height(50)))
                    {
                        QuickUpdateData();
                    }

                    // 重新生成按钮
                    GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
                    if (GUILayout.Button("重新生成（改字段时用）", GUILayout.Height(50)))
                    {
                        RegenerateAll();
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
                else if (showSheetSelection)
                {
                    EditorGUILayout.HelpBox("请先确认工作表选择", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请先选择有效的Excel文件", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();

            // 日志显示区域
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("操作日志:", EditorStyles.boldLabel);

            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(120));
            if (logMessages.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无操作日志", MessageType.None);
            }
            else
            {
                foreach (var log in logMessages)
                {
                    EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 加载工作表名称列表
        /// </summary>
        private void LoadSheetNames()
        {
            if (!File.Exists(excelFilePath))
            {
                AddLog("❌ 错误: Excel文件不存在");
                return;
            }

            sheetNames = excelReader.GetAllSheetNames(excelFilePath);

            if (sheetNames.Count > 0)
            {
                AddLog($"✓ 成功加载 {sheetNames.Count} 个工作表");
            }
            else
            {
                AddLog("❌ 错误: Excel文件中没有工作表");
            }
        }

        /// <summary>
        /// 从现有资源加载Excel路径和工作表配置
        /// </summary>
        private void LoadConfigFromAsset()
        {
            string assetPath = Path.Combine(outputAssetPath, className + ".asset");

            if (File.Exists(assetPath))
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                {
                    // 通过反射读取元数据字段
                    var assetType = asset.GetType();
                    var excelPathField = assetType.GetField("_excelFilePath");
                    var sheetIndicesField = assetType.GetField("_sheetIndices");

                    if (excelPathField != null && sheetIndicesField != null)
                    {
                        string savedExcelPath = excelPathField.GetValue(asset) as string;
                        string savedSheetIndicesStr = sheetIndicesField.GetValue(asset) as string;

                        if (!string.IsNullOrEmpty(savedExcelPath))
                        {
                            excelFilePath = savedExcelPath;
                            Debug.Log($"[ExcelTool] 从资源加载Excel路径: {excelFilePath}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ExcelTool] 资源中的Excel路径为空，可能是旧版本资源");
                        }

                        if (!string.IsNullOrEmpty(savedSheetIndicesStr))
                        {
                            savedSheetIndices.Clear();
                            string[] parts = savedSheetIndicesStr.Split(',');
                            foreach (string part in parts)
                            {
                                if (int.TryParse(part.Trim(), out int index))
                                {
                                    savedSheetIndices.Add(index);
                                }
                            }
                            Debug.Log($"[ExcelTool] 从资源加载工作表索引: {savedSheetIndicesStr}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ExcelTool] 资源中的工作表索引为空，可能是旧版本资源");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ExcelTool] 资源中没有找到元数据字段，请使用「重新生成」更新资源结构");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ExcelTool] 无法加载资源: {assetPath}");
                }
            }
            else
            {
                Debug.Log($"[ExcelTool] 资源不存在，这是首次使用: {assetPath}");
            }
        }

        /// <summary>
        /// 保存工作表选择配置
        /// </summary>
        private void SaveSheetSelection()
        {
            savedSheetIndices.Clear();
            for (int i = 0; i < selectedSheets.Count; i++)
            {
                if (selectedSheets[i])
                {
                    savedSheetIndices.Add(i + 1);
                }
            }

            if (savedSheetIndices.Count > 0)
            {
                string sheetList = string.Join(", ", savedSheetIndices.ConvertAll(i => sheetNames[i - 1]).ToArray());
                AddLog($"✓ 已保存工作表选择: {sheetList}");
            }
            else
            {
                AddLog("❌ 错误: 请至少选择一个工作表");
                EditorUtility.DisplayDialog("错误", "请至少选择一个工作表！", "确定");
            }
        }

        /// <summary>
        /// 快速更新数据（仅更新值，不改变结构）
        /// </summary>
        private void QuickUpdateData()
        {
            AddLog("========== 开始快速更新数据 ==========");

            // 读取选中的工作表
            if (!LoadSelectedSheetData())
            {
                return;
            }

            // 检查资源是否存在
            string assetPath = Path.Combine(outputAssetPath, className + ".asset");
            if (!File.Exists(assetPath))
            {
                AddLog("❌ 资源不存在，请使用「重新生成」按钮创建资源");
                EditorUtility.DisplayDialog("提示", "资源不存在，请先使用「重新生成」按钮", "确定");
                return;
            }

            try
            {
                // 根据是否有结构列表选择不同的更新方法
                if (structListData.Count > 0)
                {
                    UpdateConfigAsset(className);
                    AddLog("✓ 数据更新成功（混合模式：参数表+结构列表）");
                }
                else
                {
                    UpdateMultiSheetConfigAsset(className);
                    AddLog("✓ 数据更新成功（仅参数表）");
                }
                EditorUtility.DisplayDialog("成功", "数据更新成功！", "确定");
            }
            catch (Exception e)
            {
                AddLog($"❌ 更新失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"更新失败：{e.Message}", "确定");
                Debug.LogError($"更新失败：{e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 重新生成（重新生成脚本和资源）
        /// </summary>
        private void RegenerateAll()
        {
            AddLog("========== 开始重新生成 ==========");

            // 读取选中的工作表
            if (!LoadSelectedSheetData())
            {
                return;
            }

            // 确保输出目录存在
            if (!Directory.Exists(outputScriptPath))
            {
                Directory.CreateDirectory(outputScriptPath);
            }
            if (!Directory.Exists(outputAssetPath))
            {
                Directory.CreateDirectory(outputAssetPath);
            }

            try
            {
                // 检查资源是否已存在
                string assetPath = Path.Combine(outputAssetPath, className + ".asset");
                bool assetExists = File.Exists(assetPath);

                // 生成代码（支持混合的参数表和结构列表）
                string code;
                if (structListData.Count > 0)
                {
                    // 有结构列表，使用新的生成方法
                    code = ExcelToScriptableObject.GenerateStructListConfigClass(structListData, multiSheetData, className);
                    AddLog($"✓ 使用混合模式生成代码 (参数表={multiSheetData.Count}, 结构列表={structListData.Count})");
                }
                else
                {
                    // 只有参数表，使用原来的方法
                    code = ExcelToScriptableObject.GenerateMultiSheetConfigClass(multiSheetData, className);
                    AddLog($"✓ 使用参数表模式生成代码 (参数表={multiSheetData.Count})");
                }

                // 保存脚本
                string scriptPath = Path.Combine(outputScriptPath, className + ".cs");
                File.WriteAllText(scriptPath, code);
                AddLog($"✓ 生成脚本: {scriptPath}");

                AssetDatabase.Refresh();

                // 等待编译完成后创建或更新资源
                EditorApplication.delayCall += () =>
                {
                    if (assetExists)
                    {
                        if (structListData.Count > 0)
                        {
                            UpdateConfigAsset(className);
                        }
                        else
                        {
                            UpdateMultiSheetConfigAsset(className);
                        }
                        AddLog("✓ 更新现有资源");
                    }
                    else
                    {
                        if (structListData.Count > 0)
                        {
                            CreateConfigAsset(className);
                        }
                        else
                        {
                            CreateMultiSheetConfigAsset(className);
                        }
                        AddLog("✓ 创建新资源");
                    }
                };

                string message = assetExists ? "ScriptableObject重新生成并更新成功！" : "ScriptableObject生成成功！";
                AddLog("✓ " + message);
                EditorUtility.DisplayDialog("成功", message, "确定");
            }
            catch (Exception e)
            {
                AddLog($"❌ 生成失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}", "确定");
                Debug.LogError($"生成失败：{e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 加载选中的工作表数据（支持参数表和结构列表混合）
        /// </summary>
        private bool LoadSelectedSheetData()
        {
            multiSheetData.Clear();
            structListData.Clear();
            int selectedCount = 0;

            // 使用保存的工作表索引
            if (savedSheetIndices.Count == 0)
            {
                AddLog("❌ 错误: 请先选择工作表");
                EditorUtility.DisplayDialog("错误", "请先选择工作表！", "确定");
                return false;
            }

            foreach (int sheetIndex in savedSheetIndices)
            {
                // 检测表格类型
                string tableType = excelReader.DetectTableType(excelFilePath, sheetIndex);
                AddLog($"  检测工作表 {sheetIndex} 类型: {tableType}");

                if (tableType == "参数表")
                {
                    // 读取参数表
                    var sheetData = excelReader.ReadHorizontalConfig(excelFilePath, sheetIndex);
                    if (sheetData.fields.Count > 0)
                    {
                        multiSheetData.Add(sheetData);
                        selectedCount++;
                        AddLog($"  ✓ 读取参数表: {sheetData.sheetName} ({sheetData.fields.Count} 个字段)");
                    }
                }
                else if (tableType == "结构列表")
                {
                    // 读取结构列表
                    var structData = excelReader.ReadStructList(excelFilePath, sheetIndex);
                    if (structData.parameters.Count > 0)
                    {
                        structListData.Add(structData);
                        selectedCount++;
                        AddLog($"  ✓ 读取结构列表: {structData.sheetName} - {structData.listName} ({structData.parameters.Count} 个参数, {structData.rows.Count} 行数据)");
                    }
                }
                else
                {
                    AddLog($"  ⚠️ 跳过未知类型的工作表: {sheetIndex}");
                }
            }

            if (selectedCount == 0)
            {
                AddLog("❌ 错误: 没有读取到任何数据");
                EditorUtility.DisplayDialog("错误", "没有读取到任何数据！", "确定");
                return false;
            }

            AddLog($"✓ 共读取 {selectedCount} 个工作表 (参数表={multiSheetData.Count}, 结构列表={structListData.Count})");
            return true;
        }

        /// <summary>
        /// 添加日志消息
        /// </summary>
        private void AddLog(string message)
        {
            string timeStamp = System.DateTime.Now.ToString("HH:mm:ss");
            logMessages.Add($"[{timeStamp}] {message}");

            // 限制日志数量，保留最新的50条
            if (logMessages.Count > 50)
            {
                logMessages.RemoveAt(0);
            }

            // 自动滚动到底部
            logScrollPosition = new Vector2(0, float.MaxValue);

            Repaint();
        }

        /// <summary>
        /// 更新多表合并配置资源
        /// </summary>
        private void UpdateMultiSheetConfigAsset(string className)
        {
            string assetPath = Path.Combine(outputAssetPath, className + ".asset");

            // 加载现有资源
            ScriptableObject existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (existingAsset == null)
            {
                Debug.LogWarning("无法加载现有资源，将创建新资源");
                CreateMultiSheetConfigAsset(className);
                return;
            }

            // 通过反射获取类型
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type assetType = null;

            foreach (var assembly in assemblies)
            {
                assetType = assembly.GetType("ExcelTool." + className);
                if (assetType != null) break;
            }

            if (assetType == null)
            {
                Debug.LogWarning("类型尚未编译，请等待编译完成后重新更新");
                return;
            }

            // 更新每个表的数据
            foreach (var sheetData in multiSheetData)
            {
                string fieldName = ConvertToCamelCase(SanitizeFieldName(sheetData.sheetName));
                string nestedClassName = SanitizeClassName(sheetData.sheetName) + "Data";

                // 获取嵌套类型
                Type nestedType = assetType.GetNestedType(nestedClassName);
                if (nestedType == null)
                {
                    Debug.LogWarning($"未找到嵌套类型: {nestedClassName}，可能是新增的表，需要重新编译");
                    continue;
                }

                // 获取或创建嵌套类实例
                var mainFieldInfo = assetType.GetField(fieldName);
                if (mainFieldInfo == null)
                {
                    Debug.LogWarning($"未找到字段: {fieldName}");
                    continue;
                }

                object nestedInstance = mainFieldInfo.GetValue(existingAsset);
                if (nestedInstance == null)
                {
                    // 如果不存在，创建新实例
                    nestedInstance = Activator.CreateInstance(nestedType);
                    mainFieldInfo.SetValue(existingAsset, nestedInstance);
                }

                // 更新字段数据
                foreach (var field in sheetData.fields)
                {
                    var fieldInfo = nestedType.GetField(field.fieldName);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ExcelToScriptableObject.ConvertValue(field.value, field.dataType);
                        fieldInfo.SetValue(nestedInstance, convertedValue);
                    }
                    else
                    {
                        Debug.LogWarning($"字段 {field.fieldName} 在类型 {nestedClassName} 中不存在，可能是新增字段，需要重新编译脚本");
                    }
                }
            }

            // 保存元数据到资源
            SaveMetadataToAsset(existingAsset, assetType);

            // 标记为已修改并保存
            EditorUtility.SetDirty(existingAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"更新资源: {assetPath}");
            EditorGUIUtility.PingObject(existingAsset);
        }

        /// <summary>
        /// 创建多表合并配置资源
        /// </summary>
        private void CreateMultiSheetConfigAsset(string className)
        {
            // 通过反射获取类型
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type assetType = null;

            foreach (var assembly in assemblies)
            {
                assetType = assembly.GetType("ExcelTool." + className);
                if (assetType != null) break;
            }

            if (assetType == null)
            {
                Debug.LogWarning("类型尚未编译，请等待编译完成后手动创建资源或重新生成");
                return;
            }

            // 创建ScriptableObject实例
            ScriptableObject asset = ScriptableObject.CreateInstance(assetType);

            // 填充每个表的数据
            foreach (var sheetData in multiSheetData)
            {
                string fieldName = ConvertToCamelCase(SanitizeFieldName(sheetData.sheetName));
                string nestedClassName = SanitizeClassName(sheetData.sheetName) + "Data";

                // 获取嵌套类型
                Type nestedType = assetType.GetNestedType(nestedClassName);
                if (nestedType == null)
                {
                    Debug.LogWarning($"未找到嵌套类型: {nestedClassName}");
                    continue;
                }

                // 创建嵌套类实例
                object nestedInstance = Activator.CreateInstance(nestedType);

                // 填充字段数据
                foreach (var field in sheetData.fields)
                {
                    var fieldInfo = nestedType.GetField(field.fieldName);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ExcelToScriptableObject.ConvertValue(field.value, field.dataType);
                        fieldInfo.SetValue(nestedInstance, convertedValue);
                    }
                }

                // 将嵌套类实例设置到主对象的字段中
                var mainFieldInfo = assetType.GetField(fieldName);
                if (mainFieldInfo != null)
                {
                    mainFieldInfo.SetValue(asset, nestedInstance);
                }
            }

            // 保存元数据到资源
            SaveMetadataToAsset(asset, assetType);

            // 保存资源
            string assetPath = Path.Combine(outputAssetPath, className + ".asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"生成资源: {assetPath}");
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// 保存元数据到资源
        /// </summary>
        private void SaveMetadataToAsset(ScriptableObject asset, Type assetType)
        {
            // 保存Excel路径
            var excelPathField = assetType.GetField("_excelFilePath");
            if (excelPathField != null)
            {
                excelPathField.SetValue(asset, excelFilePath);
                Debug.Log($"[ExcelTool] 保存Excel路径到资源: {excelFilePath}");
            }
            else
            {
                Debug.LogWarning($"[ExcelTool] 未找到_excelFilePath字段，无法保存Excel路径");
            }

            // 保存工作表索引
            var sheetIndicesField = assetType.GetField("_sheetIndices");
            if (sheetIndicesField != null)
            {
                string indicesStr = string.Join(",", savedSheetIndices.ConvertAll(i => i.ToString()).ToArray());
                sheetIndicesField.SetValue(asset, indicesStr);
                Debug.Log($"[ExcelTool] 保存工作表索引到资源: {indicesStr}");
            }
            else
            {
                Debug.LogWarning($"[ExcelTool] 未找到_sheetIndices字段，无法保存工作表索引");
            }
        }

        // 辅助方法
        private string ConvertToCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "data";
            if (text.Length == 1)
                return text.ToLower();
            return char.ToLower(text[0]) + text.Substring(1);
        }

        private string SanitizeFieldName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "data";
            string result = "";
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    result += c;
            }
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;
            return string.IsNullOrEmpty(result) ? "data" : result;
        }

        private string SanitizeClassName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Config";

            // 移除非字母数字字符
            string result = "";
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    result += c;
            }

            // 确保以字母开头
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "Config" : result;
        }

        /// <summary>
        /// 获取相对路径
        /// </summary>
        private string GetRelativePath(string absolutePath)
        {
            string projectPath = Application.dataPath;
            if (absolutePath.StartsWith(projectPath))
            {
                return "Assets" + absolutePath.Substring(projectPath.Length);
            }
            return absolutePath;
        }

        /// <summary>
        /// 创建混合配置资源（参数表 + 结构列表）
        /// </summary>
        private void CreateConfigAsset(string className)
        {
            // 通过反射获取类型（改用更可靠的查找方法）
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type assetType = null;

            foreach (var assembly in assemblies)
            {
                assetType = assembly.GetType("ExcelTool." + className);
                if (assetType != null) break;
            }

            if (assetType == null)
            {
                Debug.LogError($"无法找到类型: ExcelTool.{className}，请确保脚本已编译");
                return;
            }

            ScriptableObject asset = ScriptableObject.CreateInstance(assetType);

            // 填充参数表数据
            foreach (var sheetData in multiSheetData)
            {
                string fieldName = ConvertToCamelCase(SanitizeFieldName(sheetData.sheetName));
                string nestedClassName = SanitizeClassName(sheetData.sheetName) + "Data";
                Type nestedType = assetType.GetNestedType(nestedClassName);

                if (nestedType == null)
                {
                    Debug.LogWarning($"未找到嵌套类: {nestedClassName}");
                    continue;
                }

                object nestedInstance = Activator.CreateInstance(nestedType);

                foreach (var field in sheetData.fields)
                {
                    var fieldInfo = nestedType.GetField(field.fieldName);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ExcelToScriptableObject.ConvertValue(field.value, field.dataType);
                        fieldInfo.SetValue(nestedInstance, convertedValue);
                    }
                }

                var mainFieldInfo = assetType.GetField(fieldName);
                if (mainFieldInfo != null)
                {
                    mainFieldInfo.SetValue(asset, nestedInstance);
                }
            }

            // 填充结构列表数据
            foreach (var structData in structListData)
            {
                string listFieldName = ConvertToCamelCase(SanitizeFieldName(structData.listName));
                string itemClassName = SanitizeClassName(structData.listName) + "Item";
                Type itemType = assetType.GetNestedType(itemClassName);

                if (itemType == null)
                {
                    Debug.LogWarning($"未找到结构列表项类型: {itemClassName}");
                    continue;
                }

                // 创建List
                Type listType = typeof(List<>).MakeGenericType(itemType);
                object listInstance = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");

                // 添加每一行数据
                foreach (var rowData in structData.rows)
                {
                    object itemInstance = Activator.CreateInstance(itemType);

                    foreach (var param in structData.parameters)
                    {
                        var fieldInfo = itemType.GetField(param.paramName);
                        if (fieldInfo != null && rowData.ContainsKey(param.paramName))
                        {
                            string value = rowData[param.paramName];
                            object convertedValue = ExcelToScriptableObject.ConvertValue(value, param.paramType);
                            fieldInfo.SetValue(itemInstance, convertedValue);
                        }
                    }

                    addMethod.Invoke(listInstance, new[] { itemInstance });
                }

                var listField = assetType.GetField(listFieldName);
                if (listField != null)
                {
                    listField.SetValue(asset, listInstance);
                }
            }

            // 保存元数据
            SaveMetadataToAsset(asset, assetType);

            // 保存资源
            string assetPath = Path.Combine(outputAssetPath, className + ".asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"创建配置资源: {assetPath}");
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// 更新混合配置资源（参数表 + 结构列表）
        /// </summary>
        private void UpdateConfigAsset(string className)
        {
            string assetPath = Path.Combine(outputAssetPath, className + ".asset");
            ScriptableObject existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            if (existingAsset == null)
            {
                Debug.LogError($"找不到资源: {assetPath}");
                return;
            }

            Type assetType = existingAsset.GetType();

            // 更新参数表数据
            foreach (var sheetData in multiSheetData)
            {
                string fieldName = ConvertToCamelCase(SanitizeFieldName(sheetData.sheetName));
                string nestedClassName = SanitizeClassName(sheetData.sheetName) + "Data";
                Type nestedType = assetType.GetNestedType(nestedClassName);

                if (nestedType == null)
                {
                    Debug.LogWarning($"未找到嵌套类: {nestedClassName}");
                    continue;
                }

                var mainFieldInfo = assetType.GetField(fieldName);
                if (mainFieldInfo == null)
                {
                    Debug.LogWarning($"未找到字段: {fieldName}");
                    continue;
                }

                object nestedInstance = mainFieldInfo.GetValue(existingAsset);
                if (nestedInstance == null)
                {
                    nestedInstance = Activator.CreateInstance(nestedType);
                }

                foreach (var field in sheetData.fields)
                {
                    var fieldInfo = nestedType.GetField(field.fieldName);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ExcelToScriptableObject.ConvertValue(field.value, field.dataType);
                        fieldInfo.SetValue(nestedInstance, convertedValue);
                    }
                }

                mainFieldInfo.SetValue(existingAsset, nestedInstance);
            }

            // 更新结构列表数据
            foreach (var structData in structListData)
            {
                string listFieldName = ConvertToCamelCase(SanitizeFieldName(structData.listName));
                string itemClassName = SanitizeClassName(structData.listName) + "Item";
                Type itemType = assetType.GetNestedType(itemClassName);

                if (itemType == null)
                {
                    Debug.LogWarning($"未找到结构列表项类型: {itemClassName}");
                    continue;
                }

                // 创建新的List
                Type listType = typeof(List<>).MakeGenericType(itemType);
                object listInstance = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");

                // 添加每一行数据
                foreach (var rowData in structData.rows)
                {
                    object itemInstance = Activator.CreateInstance(itemType);

                    foreach (var param in structData.parameters)
                    {
                        var fieldInfo = itemType.GetField(param.paramName);
                        if (fieldInfo != null && rowData.ContainsKey(param.paramName))
                        {
                            string value = rowData[param.paramName];
                            object convertedValue = ExcelToScriptableObject.ConvertValue(value, param.paramType);
                            fieldInfo.SetValue(itemInstance, convertedValue);
                        }
                    }

                    addMethod.Invoke(listInstance, new[] { itemInstance });
                }

                var listField = assetType.GetField(listFieldName);
                if (listField != null)
                {
                    listField.SetValue(existingAsset, listInstance);
                }
            }

            // 保存元数据
            SaveMetadataToAsset(existingAsset, assetType);

            // 标记为已修改并保存
            EditorUtility.SetDirty(existingAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"更新配置资源: {assetPath}");
            EditorGUIUtility.PingObject(existingAsset);
        }
    }
}
