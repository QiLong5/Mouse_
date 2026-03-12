using UnityEngine;
using UnityEditor;

public class CustomURPLitShaderGUI : ShaderGUI
{
    private MaterialProperty renderMode;
    private MaterialProperty receiveShadows;
    private MaterialProperty alphaClip;
    private MaterialProperty cutoff;
    private MaterialProperty doubleSided;

    private MaterialProperty baseMap;
    private MaterialProperty baseColor;

    private MaterialProperty enableOutline;
    private MaterialProperty useVertexColor;
    private MaterialProperty outlineWidth;
    private MaterialProperty outlineColor;

    private MaterialProperty metallicMap;
    private MaterialProperty metallic;
    private MaterialProperty smoothness;
    private MaterialProperty occlusionMap;
    private MaterialProperty occlusionStrength;

    private MaterialProperty useNormalMap;
    private MaterialProperty normalMap;
    private MaterialProperty normalScale;

    private MaterialProperty useEmission;
    private MaterialProperty emissionMap;
    private MaterialProperty emissionColor;

    private MaterialProperty useRimLight;
    private MaterialProperty rimColor;
    private MaterialProperty rimPower;
    private MaterialProperty rimIntensity;

    private MaterialProperty useStylizedLighting;
    private MaterialProperty shadowSteps;
    private MaterialProperty shadowSmoothness;

    private MaterialProperty useCustomLighting;
    private MaterialProperty customLightColor;
    private MaterialProperty customAmbientColor;

    private MaterialProperty useFakePointLight;
    private MaterialProperty fakeLightCount;

    // Arrays for 8 lights
    private MaterialProperty[] fakeLightEnabled = new MaterialProperty[8];
    private MaterialProperty[] fakeLightPos = new MaterialProperty[8];
    private MaterialProperty[] fakeLightColor = new MaterialProperty[8];
    private MaterialProperty[] fakeLightIntensity = new MaterialProperty[8];
    private MaterialProperty[] fakeLightRange = new MaterialProperty[8];
    private MaterialProperty[] fakeLightAttenuationPower = new MaterialProperty[8];

    private MaterialProperty mainTiling;
    private MaterialProperty queueOffset;

    private bool m_FirstTimeApply = true;

    // Foldout states for fake lights (max 8)
    private bool[] fakeLightFoldouts = new bool[8] { true, false, false, false, false, false, false, false };

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        FindProperties(properties);

        // First time setup
        if (m_FirstTimeApply)
        {
            OnOpenGUI(material, materialEditor);
            m_FirstTimeApply = false;
        }

        EditorGUI.BeginChangeCheck();

        // Render Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("渲染设置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        renderMode.floatValue = EditorGUILayout.Popup("渲染模式", (int)renderMode.floatValue, new string[] { "不透明", "透明" });
        if (EditorGUI.EndChangeCheck())
        {
            UpdateRenderMode(material);
        }

        materialEditor.ShaderProperty(receiveShadows, "接受阴影");
        if (receiveShadows.floatValue > 0.5f)
        {
            material.EnableKeyword("_RECEIVE_SHADOWS");
        }
        else
        {
            material.DisableKeyword("_RECEIVE_SHADOWS");
        }

        materialEditor.ShaderProperty(alphaClip, "透明度裁剪");
        if (alphaClip.floatValue > 0.5f)
        {
            material.EnableKeyword("_ALPHATEST_ON");
            materialEditor.ShaderProperty(cutoff, "裁剪阈值");
        }
        else
        {
            material.DisableKeyword("_ALPHATEST_ON");
        }

        materialEditor.ShaderProperty(doubleSided, "双面渲染");
        UpdateCullMode(material);

        // Base
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("基础", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("基础贴图"), baseMap, baseColor);

        // Custom Lighting
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自定义光照", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(useCustomLighting, "使用自定义光照颜色");
        if (useCustomLighting.floatValue > 0.5f)
        {
            material.EnableKeyword("_USE_CUSTOM_LIGHTING");
            materialEditor.ShaderProperty(customLightColor, "自定义光源颜色");
            materialEditor.ShaderProperty(customAmbientColor, "自定义环境光颜色");
            EditorGUILayout.HelpBox("将覆盖场景光照，用于特殊风格化效果", MessageType.Info);
        }
        else
        {
            material.DisableKeyword("_USE_CUSTOM_LIGHTING");
        }

        // Outline
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("描边", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(enableOutline, "启用描边");
        if (enableOutline.floatValue > 0.5f)
        {
            material.EnableKeyword("_ENABLE_OUTLINE");
            materialEditor.ShaderProperty(useVertexColor, "使用顶点色平滑法线");
            if (useVertexColor.floatValue > 0.5f)
            {
                material.EnableKeyword("_USE_VERTEX_COLOR");
                EditorGUILayout.HelpBox("描边断裂时使用，需要先烘焙平滑法线到顶点色\n选中模型->右键MeshFilter组件 ->Outline -> Bake Smooth Normals to Color", MessageType.Info);
            }
            else
            {
                material.DisableKeyword("_USE_VERTEX_COLOR");
            }

            materialEditor.ShaderProperty(outlineWidth, "描边粗细");
            materialEditor.ShaderProperty(outlineColor, "描边颜色");
        }
        else
        {
            material.DisableKeyword("_ENABLE_OUTLINE");
            material.DisableKeyword("_USE_VERTEX_COLOR");
        }

        // Surface
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("表面", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("金属度贴图"), metallicMap);
        materialEditor.ShaderProperty(metallic, "金属度");
        materialEditor.ShaderProperty(smoothness, "光滑度");
        materialEditor.TexturePropertySingleLine(new GUIContent("环境光遮蔽 (AO)"), occlusionMap);
        if (occlusionMap.textureValue != null)
        {
            materialEditor.ShaderProperty(occlusionStrength, "AO强度");
        }

        // Normal
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("法线", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(useNormalMap, "使用法线贴图");
        if (useNormalMap.floatValue > 0.5f)
        {
            material.EnableKeyword("_USE_NORMAL_MAP");
            materialEditor.TexturePropertySingleLine(new GUIContent("法线贴图"), normalMap);
            materialEditor.ShaderProperty(normalScale, "法线强度");
        }
        else
        {
            material.DisableKeyword("_USE_NORMAL_MAP");
        }

        // Emission
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自发光", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(useEmission, "使用自发光");
        if (useEmission.floatValue > 0.5f)
        {
            material.EnableKeyword("_USE_EMISSION");
            materialEditor.TexturePropertySingleLine(new GUIContent("自发光贴图"), emissionMap, emissionColor);
        }
        else
        {
            material.DisableKeyword("_USE_EMISSION");
        }

        // Rim Light
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("边缘光", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(useRimLight, "启用边缘光");
        if (useRimLight.floatValue > 0.5f)
        {
            material.EnableKeyword("_USE_RIM_LIGHT");
            materialEditor.ShaderProperty(rimColor, "边缘光颜色");
            materialEditor.ShaderProperty(rimPower, "边缘光范围");
            materialEditor.ShaderProperty(rimIntensity, "边缘光强度");
        }
        else
        {
            material.DisableKeyword("_USE_RIM_LIGHT");
        }

        // Stylized Lighting
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("风格化光照", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(useStylizedLighting, "启用卡通光照");
        if (useStylizedLighting.floatValue > 0.5f)
        {
            material.EnableKeyword("_USE_STYLIZED_LIGHTING");
            materialEditor.ShaderProperty(shadowSteps, "阴影分层数");
            materialEditor.ShaderProperty(shadowSmoothness, "阴影平滑度");
        }
        else
        {
            material.DisableKeyword("_USE_STYLIZED_LIGHTING");
        }

        // Fake Point Lights (Dynamic List)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("假点光源列表", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(useFakePointLight, "启用假点光源系统");
        if (useFakePointLight.floatValue > 0.5f)
        {
            material.EnableKeyword("_USE_FAKE_POINT_LIGHT");

            int currentCount = (int)fakeLightCount.floatValue;
            currentCount = Mathf.Clamp(currentCount, 1, 8);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox($"当前光源数量: {currentCount}/8  (禁用的光源不消耗性能)", MessageType.Info);
            EditorGUILayout.EndHorizontal();

            // Draw existing lights
            for (int i = 0; i < currentCount; i++)
            {
                DrawFakeLightGUI(materialEditor, i, $"光源 {i + 1}",
                    fakeLightEnabled[i], fakeLightPos[i], fakeLightColor[i],
                    fakeLightIntensity[i], fakeLightRange[i], fakeLightAttenuationPower[i]);
            }

            // Add/Remove buttons
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (currentCount < 8)
            {
                if (GUILayout.Button("+ 添加光源", GUILayout.Height(25)))
                {
                    currentCount++;
                    fakeLightCount.floatValue = currentCount;
                    fakeLightEnabled[currentCount - 1].floatValue = 1f;
                }
            }

            if (currentCount > 1)
            {
                if (GUILayout.Button("- 删除最后一个", GUILayout.Height(25)))
                {
                    currentCount--;
                    fakeLightCount.floatValue = currentCount;
                    fakeLightEnabled[currentCount].floatValue = 0f;
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            material.DisableKeyword("_USE_FAKE_POINT_LIGHT");
        }

        // Tiling and Offset
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("平铺和偏移", EditorStyles.boldLabel);
        Vector4 tilingOffset = mainTiling.vectorValue;
        Vector2 tiling = new Vector2(tilingOffset.x, tilingOffset.y);
        Vector2 offset = new Vector2(tilingOffset.z, tilingOffset.w);

        tiling = EditorGUILayout.Vector2Field("平铺", tiling);
        offset = EditorGUILayout.Vector2Field("偏移", offset);
        mainTiling.vectorValue = new Vector4(tiling.x, tiling.y, offset.x, offset.y);

        // Advanced
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("高级", EditorStyles.boldLabel);

        // Queue Offset
        EditorGUI.BeginChangeCheck();
        materialEditor.ShaderProperty(queueOffset, "渲染队列偏移");
        if (EditorGUI.EndChangeCheck())
        {
            UpdateRenderQueue(material);
        }

        // Display current render queue
        EditorGUILayout.LabelField("当前渲染队列", material.renderQueue.ToString());


        if (EditorGUI.EndChangeCheck())
        {
            materialEditor.PropertiesChanged();
        }
    }

    private void FindProperties(MaterialProperty[] properties)
    {
        renderMode = FindProperty("_RenderMode", properties);
        receiveShadows = FindProperty("_ReceiveShadows", properties);
        alphaClip = FindProperty("_AlphaClip", properties);
        cutoff = FindProperty("_Cutoff", properties);
        doubleSided = FindProperty("_DoubleSided", properties);

        baseMap = FindProperty("_BaseMap", properties);
        baseColor = FindProperty("_BaseColor", properties);

        enableOutline = FindProperty("_EnableOutline", properties);
        useVertexColor = FindProperty("_UseVertexColor", properties);
        outlineWidth = FindProperty("_OutlineWidth", properties);
        outlineColor = FindProperty("_OutlineColor", properties);

        metallicMap = FindProperty("_MetallicMap", properties);
        metallic = FindProperty("_Metallic", properties);
        smoothness = FindProperty("_Smoothness", properties);
        occlusionMap = FindProperty("_OcclusionMap", properties);
        occlusionStrength = FindProperty("_OcclusionStrength", properties);

        useNormalMap = FindProperty("_UseNormalMap", properties);
        normalMap = FindProperty("_NormalMap", properties);
        normalScale = FindProperty("_NormalScale", properties);

        useEmission = FindProperty("_UseEmission", properties);
        emissionMap = FindProperty("_EmissionMap", properties);
        emissionColor = FindProperty("_EmissionColor", properties);

        useRimLight = FindProperty("_UseRimLight", properties);
        rimColor = FindProperty("_RimColor", properties);
        rimPower = FindProperty("_RimPower", properties);
        rimIntensity = FindProperty("_RimIntensity", properties);

        useStylizedLighting = FindProperty("_UseStylizedLighting", properties);
        shadowSteps = FindProperty("_ShadowSteps", properties);
        shadowSmoothness = FindProperty("_ShadowSmoothness", properties);

        useCustomLighting = FindProperty("_UseCustomLighting", properties);
        customLightColor = FindProperty("_CustomLightColor", properties);
        customAmbientColor = FindProperty("_CustomAmbientColor", properties);

        useFakePointLight = FindProperty("_UseFakePointLight", properties);
        fakeLightCount = FindProperty("_FakeLightCount", properties);

        // Find all 8 lights
        for (int i = 0; i < 8; i++)
        {
            int lightNum = i + 1;
            fakeLightEnabled[i] = FindProperty($"_FakeLight{lightNum}_Enabled", properties);
            fakeLightPos[i] = FindProperty($"_FakeLight{lightNum}_Pos", properties);
            fakeLightColor[i] = FindProperty($"_FakeLight{lightNum}_Color", properties);
            fakeLightIntensity[i] = FindProperty($"_FakeLight{lightNum}_Intensity", properties);
            fakeLightRange[i] = FindProperty($"_FakeLight{lightNum}_Range", properties);
            fakeLightAttenuationPower[i] = FindProperty($"_FakeLight{lightNum}_AttenuationPower", properties);
        }

        mainTiling = FindProperty("_MainTiling", properties);
        queueOffset = FindProperty("_QueueOffset", properties);
    }

    private void UpdateRenderMode(Material material)
    {
        switch ((int)renderMode.floatValue)
        {
            case 0: // Opaque
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                break;
            case 1: // Transparent
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
        }
        UpdateRenderQueue(material);
    }

    private void UpdateRenderQueue(Material material)
    {
        if (queueOffset == null) return;

        int baseQueue = (int)renderMode.floatValue == 0
            ? (int)UnityEngine.Rendering.RenderQueue.Geometry
            : (int)UnityEngine.Rendering.RenderQueue.Transparent;

        int offset = (int)queueOffset.floatValue;
        material.renderQueue = baseQueue + offset;
    }

    private void UpdateCullMode(Material material)
    {
        if (doubleSided == null) return;

        if (doubleSided.floatValue > 0.5f)
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }
        else
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        // Clear all keywords
        material.shaderKeywords = null;

        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        // Set default render state
        material.SetFloat("_RenderMode", 0);
        UpdateRenderMode(material);

        // Set default queue offset
        material.SetFloat("_QueueOffset", 0);
    }

    private void OnOpenGUI(Material material, MaterialEditor editor)
    {
        // Ensure render state is correct on first open
        UpdateRenderMode(material);
    }

    private void DrawFakeLightGUI(MaterialEditor materialEditor, int index, string title,
        MaterialProperty enabled, MaterialProperty pos, MaterialProperty color,
        MaterialProperty intensity, MaterialProperty range, MaterialProperty attenuationPower)
    {
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");

        // Title bar with toggle
        EditorGUILayout.BeginHorizontal();
        fakeLightFoldouts[index] = EditorGUILayout.Foldout(fakeLightFoldouts[index], title, true, EditorStyles.foldoutHeader);

        GUILayout.FlexibleSpace();

        // Enable toggle
        EditorGUI.BeginChangeCheck();
        bool isEnabled = enabled.floatValue > 0.5f;
        isEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));
        if (EditorGUI.EndChangeCheck())
        {
            enabled.floatValue = isEnabled ? 1f : 0f;
        }

        EditorGUILayout.EndHorizontal();

        // Light properties (only show when foldout is open)
        if (fakeLightFoldouts[index])
        {
            EditorGUI.BeginDisabledGroup(!isEnabled);

            EditorGUILayout.Space(3);

            // Position
            Vector4 currentPos = pos.vectorValue;
            Vector3 pos3 = new Vector3(currentPos.x, currentPos.y, currentPos.z);
            pos3 = EditorGUILayout.Vector3Field("位置", pos3);
            pos.vectorValue = new Vector4(pos3.x, pos3.y, pos3.z, 0);

            // Color
            materialEditor.ShaderProperty(color, "颜色");

            // Intensity
            materialEditor.ShaderProperty(intensity, "强度");

            // Range
            materialEditor.ShaderProperty(range, "范围");

            // Attenuation
            materialEditor.ShaderProperty(attenuationPower, "衰减");

            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndVertical();
    }
}
