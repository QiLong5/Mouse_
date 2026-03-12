using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Light))]
public class FakeLightSetup : MonoBehaviour
{
    [Header("设置选项")]
    [Tooltip("目标 Shader 名称")]
    public string targetShaderName = "Custom/URP Lit Advanced";

    [Tooltip("是否替换不匹配的材质")]
    public bool replaceMaterials = true;

    [Tooltip("位置匹配阈值（米）")]
    public float positionThreshold = 0.1f;

    [Header("光照强度调整")]
    [Tooltip("假光源强度系数（用于匹配真实Light的亮度）")]
    [Range(0.1f, 10f)]
    public float intensityMultiplier = 1.0f;

    [Header("衰减设置")]
    [Tooltip("衰减曲线指数（Unity物理光照使用2.0）")]
    [Range(1f, 4f)]
    public float attenuationPower = 2.0f;

    private Light lightComponent;

    // 用于撤销操作的记录
    [System.Serializable]
    private class MaterialBackup
    {
        public Material material;
        public string originalShaderName;
        public bool wasKeywordEnabled;
        public float wasUseFakePointLight;
        public int lightSlotIndex;
        public bool wasLightEnabled;
        public Vector4 originalPos;
        public Color originalColor;
        public float originalIntensity;
        public float originalRange;
        public float originalAttenuation;
        public int originalLightCount;
    }

    private List<MaterialBackup> lastOperationBackups = new List<MaterialBackup>();

    /// <summary>
    /// 撤销上次的设置操作
    /// </summary>
    public void UndoLastOperation()
    {
        if (lastOperationBackups.Count == 0)
        {
            Debug.LogWarning("没有可撤销的操作！");
            return;
        }

        Debug.Log($"========== 开始撤销操作 ==========");
        Debug.Log($"将撤销 {lastOperationBackups.Count} 个材质的修改");

        int restoredCount = 0;

        foreach (MaterialBackup backup in lastOperationBackups)
        {
            if (backup.material == null)
            {
                Debug.LogWarning("材质已被删除，无法恢复");
                continue;
            }

            Material mat = backup.material;

            // 恢复 Shader
            if (mat.shader.name != backup.originalShaderName)
            {
                Shader originalShader = Shader.Find(backup.originalShaderName);
                if (originalShader != null)
                {
                    mat.shader = originalShader;
                    Debug.Log($"恢复材质 '{mat.name}' 的 Shader 为 '{backup.originalShaderName}'");
                }
            }

            // 恢复 keyword 状态
            if (backup.wasKeywordEnabled)
                mat.EnableKeyword("_USE_FAKE_POINT_LIGHT");
            else
                mat.DisableKeyword("_USE_FAKE_POINT_LIGHT");

            // 恢复假光源开关
            mat.SetFloat("_UseFakePointLight", backup.wasUseFakePointLight);

            // 恢复光源数量
            mat.SetFloat("_FakeLightCount", backup.originalLightCount);

            // 恢复该槽位的原始值
            int lightNum = backup.lightSlotIndex + 1;
            mat.SetFloat($"_FakeLight{lightNum}_Enabled", backup.wasLightEnabled ? 1f : 0f);
            mat.SetVector($"_FakeLight{lightNum}_Pos", backup.originalPos);
            mat.SetColor($"_FakeLight{lightNum}_Color", backup.originalColor);
            mat.SetFloat($"_FakeLight{lightNum}_Intensity", backup.originalIntensity);
            mat.SetFloat($"_FakeLight{lightNum}_Range", backup.originalRange);
            mat.SetFloat($"_FakeLight{lightNum}_AttenuationPower", backup.originalAttenuation);

            restoredCount++;

#if UNITY_EDITOR
            EditorUtility.SetDirty(mat);
#endif
        }

        Debug.Log($"========== 撤销完成 ==========");
        Debug.Log($"已恢复 {restoredCount} 个材质");
        Debug.Log($"=====================================");

        // 清空备份列表
        lastOperationBackups.Clear();

#if UNITY_EDITOR
        // 标记场景为已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
#endif
    }

    /// <summary>
    /// 检查是否有可撤销的操作
    /// </summary>
    public bool HasUndoableOperation()
    {
        return lastOperationBackups.Count > 0;
    }

    void OnValidate()
    {
        lightComponent = GetComponent<Light>();
    }

    /// <summary>
    /// 设置假光源到范围内的所有物体
    /// </summary>
    public void SetupFakeLights()
    {
        lightComponent = GetComponent<Light>();

        if (lightComponent == null)
        {
            Debug.LogError("没有找到 Light 组件！");
            return;
        }

        if (lightComponent.type != LightType.Point)
        {
            Debug.LogWarning("该脚本仅支持点光源（Point Light）！");
            return;
        }

        // 清空上次的备份记录
        lastOperationBackups.Clear();

        Vector3 lightPos = transform.position;
        float lightRange = lightComponent.range;
        Color lightColor = lightComponent.color;
        float lightIntensity = lightComponent.intensity;

        // 使用 Light 组件的 cullingMask（光照遮罩）来过滤物体
        int lightCullingMask = lightComponent.cullingMask;

        Debug.Log($"========== 开始设置假光源 ==========");
        Debug.Log($"光源位置: {lightPos}");
        Debug.Log($"光源范围: {lightRange}");
        Debug.Log($"光源颜色: {lightColor}");
        Debug.Log($"光源强度: {lightIntensity}");
        Debug.Log($"强度系数: {intensityMultiplier}x (实际强度: {lightIntensity * intensityMultiplier})");
        Debug.Log($"衰减指数: {attenuationPower}");

        List<Renderer> renderersToProcess = new List<Renderer>();

        // 遍历场景中所有 Renderer，使用 Light 组件的 cullingMask 过滤
        Renderer[] allRenderers = FindObjectsOfType<Renderer>(true);
        Debug.Log($"场景中共找到 {allRenderers.Length} 个渲染器");

        int layerFiltered = 0;
        int rangeFiltered = 0;

        foreach (Renderer renderer in allRenderers)
        {
            // 跳过自己
            if (renderer.gameObject == gameObject)
                continue;

            // 检查物体的层级是否在光照遮罩内
            int objectLayer = renderer.gameObject.layer;
            if ((lightCullingMask & (1 << objectLayer)) == 0)
            {
                // 物体不在光照遮罩内，跳过
                layerFiltered++;
                Debug.Log($"[层级过滤] 跳过 {renderer.gameObject.name} - Layer {objectLayer} ({LayerMask.LayerToName(objectLayer)}) 不在cullingMask中");
                continue;
            }

            // 检查是否在光源范围内
            // 使用Unity Light的检测方式：检查Bounds是否与Light的范围球体相交
            Bounds rendererBounds = renderer.bounds;
            float closestDistance = ClosestPointOnBounds(rendererBounds, lightPos);

            if (closestDistance <= lightRange)
            {
                Debug.Log($"[✓ 通过检测] {renderer.gameObject.name} - Layer {objectLayer}, 最近距离 {closestDistance:F2}m, Bounds中心距离 {Vector3.Distance(lightPos, rendererBounds.center):F2}m, Shader: {renderer.sharedMaterial?.shader.name ?? "null"}");
                renderersToProcess.Add(renderer);
            }
            else
            {
                rangeFiltered++;
                Debug.Log($"[范围过滤] 跳过 {renderer.gameObject.name} - 最近距离 {closestDistance:F2}m > 范围 {lightRange}m");
            }
        }

        Debug.Log($"========== 检测统计 ==========");
        Debug.Log($"总渲染器: {allRenderers.Length}");
        Debug.Log($"层级过滤掉: {layerFiltered}");
        Debug.Log($"范围过滤掉: {rangeFiltered}");
        Debug.Log($"通过检测: {renderersToProcess.Count}");
        Debug.Log($"================================");

        int processedCount = 0;
        int addedCount = 0;
        int skippedCount = 0;

        foreach (Renderer renderer in renderersToProcess)
        {
            Material[] materials = renderer.sharedMaterials;
            bool materialsChanged = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null)
                    continue;

                // 创建备份
                MaterialBackup backup = new MaterialBackup();
                backup.material = mat;
                backup.originalShaderName = mat.shader.name;
                backup.wasKeywordEnabled = mat.IsKeywordEnabled("_USE_FAKE_POINT_LIGHT");
                backup.wasUseFakePointLight = mat.HasProperty("_UseFakePointLight") ? mat.GetFloat("_UseFakePointLight") : 0f;
                backup.originalLightCount = mat.HasProperty("_FakeLightCount") ? (int)mat.GetFloat("_FakeLightCount") : 1;

                // 检查并替换 Shader
                if (mat.shader.name != targetShaderName)
                {
                    if (replaceMaterials)
                    {
                        Shader targetShader = Shader.Find(targetShaderName);
                        if (targetShader != null)
                        {
                            Debug.Log($"将 {renderer.gameObject.name} 的材质 '{mat.name}' 的 Shader 从 '{mat.shader.name}' 替换为 '{targetShaderName}'");
                            mat.shader = targetShader;
                        }
                        else
                        {
                            Debug.LogError($"找不到 Shader: {targetShaderName}");
                            continue;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"跳过 {renderer.gameObject.name} 的材质 '{mat.name}'（Shader 不匹配且未启用替换）");
                        skippedCount++;
                        continue;
                    }
                }

                // 启用假点光源功能
                mat.EnableKeyword("_USE_FAKE_POINT_LIGHT");
                mat.SetFloat("_UseFakePointLight", 1f);

                // 查找或添加假光源（返回槽位索引和是否为新添加）
                var (lightSlot, isNewLight) = FindOrAddFakeLightSlot(mat, lightPos);

                if (lightSlot >= 0)
                {
                    // 记录该槽位的原始值
                    backup.lightSlotIndex = lightSlot;
                    int lightNum = lightSlot + 1;
                    backup.wasLightEnabled = mat.GetFloat($"_FakeLight{lightNum}_Enabled") > 0.5f;
                    backup.originalPos = mat.GetVector($"_FakeLight{lightNum}_Pos");
                    backup.originalColor = mat.GetColor($"_FakeLight{lightNum}_Color");
                    backup.originalIntensity = mat.GetFloat($"_FakeLight{lightNum}_Intensity");
                    backup.originalRange = mat.GetFloat($"_FakeLight{lightNum}_Range");
                    backup.originalAttenuation = mat.GetFloat($"_FakeLight{lightNum}_AttenuationPower");

                    // 设置假光源参数（应用强度系数和衰减参数）
                    SetFakeLightParameters(mat, lightSlot, lightPos, lightColor, lightIntensity * intensityMultiplier, lightRange, attenuationPower);
                    processedCount++;

                    if (isNewLight)
                    {
                        addedCount++;
                        Debug.Log($"✓ [新增] 已将假光源添加到 {renderer.gameObject.name} 的材质 '{mat.name}' 的槽位 {lightSlot + 1}");
                    }
                    else
                    {
                        Debug.Log($"✓ [更新] 已覆盖 {renderer.gameObject.name} 的材质 '{mat.name}' 槽位 {lightSlot + 1} 的参数");
                    }

                    // 添加到备份列表
                    lastOperationBackups.Add(backup);
                }
                else
                {
                    Debug.LogWarning($"✗ {renderer.gameObject.name} 的材质 '{mat.name}' 的假光源槽位已满（8个）");
                    skippedCount++;
                }

                materialsChanged = true;
            }

            // 刷新材质
            if (materialsChanged)
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(renderer);
#endif
            }
        }

        Debug.Log($"========== 设置完成 ==========");
        Debug.Log($"处理物体数: {processedCount}");
        Debug.Log($"添加光源数: {addedCount}");
        Debug.Log($"跳过物体数: {skippedCount}");
        Debug.Log($"=====================================");

#if UNITY_EDITOR
        // 标记场景为已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
#endif
    }

    /// <summary>
    /// 查找或添加假光源槽位
    /// 返回 (槽位索引(0-7), 是否为新添加的灯光)，如果已满则返回 (-1, false)
    /// </summary>
    private (int slotIndex, bool isNewLight) FindOrAddFakeLightSlot(Material mat, Vector3 lightPos)
    {
        // 获取当前光源数量
        int lightCount = mat.HasProperty("_FakeLightCount") ? (int)mat.GetFloat("_FakeLightCount") : 1;
        lightCount = Mathf.Clamp(lightCount, 1, 8);

        // Shader 中光源1的默认值
        Vector3 defaultLightPos = new Vector3(0, 5, 0);

        // 首先检查光源1是否是默认值（未被用户修改过）
        if (mat.GetFloat("_FakeLight1_Enabled") > 0.5f)
        {
            Vector4 light1Pos = mat.GetVector("_FakeLight1_Pos");
            Vector3 light1Pos3 = new Vector3(light1Pos.x, light1Pos.y, light1Pos.z);

            // 如果光源1的位置是默认值（0,5,0），直接覆盖它
            if (Vector3.Distance(light1Pos3, defaultLightPos) < positionThreshold)
            {
                Debug.Log($"光源1为默认值，将使用新光源覆盖");
                return (0, false); // 覆盖默认值，不算新增
            }
        }

        // 检查是否已有相同位置的光源（优先级最高：直接覆盖）
        for (int i = 0; i < lightCount; i++)
        {
            if (mat.GetFloat($"_FakeLight{i + 1}_Enabled") > 0.5f)
            {
                Vector4 existingPos = mat.GetVector($"_FakeLight{i + 1}_Pos");
                Vector3 existingPos3 = new Vector3(existingPos.x, existingPos.y, existingPos.z);

                if (Vector3.Distance(existingPos3, lightPos) < positionThreshold)
                {
                    Debug.Log($"找到相同位置的假光源在槽位 {i + 1}，将覆盖参数");
                    return (i, false); // 找到相同位置，覆盖参数
                }
            }
        }

        // 没有找到相同位置的光源，查找空槽位来添加新光源
        for (int i = 0; i < 8; i++)
        {
            if (mat.GetFloat($"_FakeLight{i + 1}_Enabled") < 0.5f)
            {
                // 找到空槽，更新光源数量
                if (i >= lightCount)
                {
                    mat.SetFloat("_FakeLightCount", i + 1);
                }
                Debug.Log($"在空槽位 {i + 1} 添加新的假光源");
                return (i, true); // 新增光源
            }
        }

        // 所有槽位都已启用
        return (-1, false);
    }

    /// <summary>
    /// 设置假光源参数
    /// </summary>
    private void SetFakeLightParameters(Material mat, int slotIndex, Vector3 pos, Color color, float intensity, float range, float attenuation)
    {
        int lightNum = slotIndex + 1;

        mat.SetFloat($"_FakeLight{lightNum}_Enabled", 1f);
        mat.SetVector($"_FakeLight{lightNum}_Pos", new Vector4(pos.x, pos.y, pos.z, 0));
        mat.SetColor($"_FakeLight{lightNum}_Color", color);
        mat.SetFloat($"_FakeLight{lightNum}_Intensity", intensity);
        mat.SetFloat($"_FakeLight{lightNum}_Range", range);
        mat.SetFloat($"_FakeLight{lightNum}_AttenuationPower", attenuation); // 使用参数化的衰减曲线
    }

    /// <summary>
    /// 计算点到Bounds的最近距离（模拟Unity Light的检测方式）
    /// </summary>
    private float ClosestPointOnBounds(Bounds bounds, Vector3 point)
    {
        // 找到Bounds上离点最近的点
        Vector3 closestPoint = bounds.ClosestPoint(point);
        // 返回距离
        return Vector3.Distance(point, closestPoint);
    }

    void OnDrawGizmosSelected()
    {
        if (lightComponent == null)
            lightComponent = GetComponent<Light>();

        if (lightComponent != null && lightComponent.type == LightType.Point)
        {
            // 绘制光源范围
            Gizmos.color = new Color(lightComponent.color.r, lightComponent.color.g, lightComponent.color.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, lightComponent.range);

            Gizmos.color = lightComponent.color;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FakeLightSetup))]
public class FakeLightSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FakeLightSetup script = (FakeLightSetup)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("点击按钮将此光源设置为假光源，影响范围内的所有物体\n会根据Light组件的cullingMask自动过滤物体层级", MessageType.Info);

        // 设置假光源按钮
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("设置假光源", GUILayout.Height(40)))
        {
            script.SetupFakeLights();
        }
        GUI.backgroundColor = Color.white;

        // 撤销按钮
        EditorGUILayout.Space(5);
        GUI.enabled = script.HasUndoableOperation();
        GUI.backgroundColor = new Color(1f, 0.7f, 0.3f); // 橙色
        if (GUILayout.Button("撤销上次操作", GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("确认撤销",
                "确定要撤销上次的假光源设置操作吗？\n这将恢复所有受影响材质的原始状态。",
                "确定", "取消"))
            {
                script.UndoLastOperation();
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (!script.HasUndoableOperation())
        {
            EditorGUILayout.HelpBox("没有可撤销的操作", MessageType.Info);
        }

        EditorGUILayout.Space();

        Light light = script.GetComponent<Light>();
        if (light != null)
        {
            EditorGUILayout.LabelField("光源信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("类型", light.type.ToString());
            EditorGUILayout.LabelField("范围", light.range.ToString("F2") + " 米");
            EditorGUILayout.LabelField("强度", light.intensity.ToString("F2"));
            EditorGUILayout.LabelField("衰减指数", script.attenuationPower.ToString("F2"));
            EditorGUILayout.LabelField("颜色", "");
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 20), light.color);
        }
    }
}
#endif
