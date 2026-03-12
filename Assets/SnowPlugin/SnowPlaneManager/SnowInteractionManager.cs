using UnityEngine;

/// <summary>
/// 雪地交互管理器 - 负责管理RenderTexture和踩踏效果
/// 优化用于Luna平台
/// </summary>
public class SnowInteractionManager : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("雪地覆盖的世界空间范围")]
    public Vector2 worldSize = new Vector2(20f, 20f);

    [Tooltip("雪地中心位置（世界坐标）")]
    public Vector3 worldCenter = Vector3.zero;

    [Header("表面噪声")]
    [Tooltip("噪声强度（0=完全平坦，1=明显凹凸）")]
    [Range(0f, 0.5f)]
    public float noiseStrength = 0.15f;

    [Tooltip("噪声缩放（值越大，噪声越细腻）")]
    [Range(1f, 50f)]
    public float noiseScale = 10f;

    [Tooltip("随机种子（改变可生成不同噪声图案）")]
    public int noiseSeed = 12345;

    [Header("边缘设置")]
    [Tooltip("边缘过渡宽度（0-1，相对于雪地尺寸）")]
    [Range(0f, 0.5f)]
    public float edgeFalloff = 0.15f;

    [Tooltip("边缘过渡平滑度")]
    [Range(0.01f, 1f)]
    public float edgeSmoothness = 0.3f;

    [Tooltip("实时更新边缘（会清除踩踏效果）")]
    public bool liveUpdateEdges = true;

    [Header("RenderTexture设置")]
    [Tooltip("高度图分辨率（1024适合小场景）")]
    public int textureResolution = 1024;

    [Tooltip("高度图纹理格式（ARGB32兼容性最好）")]
    public RenderTextureFormat textureFormat = RenderTextureFormat.ARGB32;

    [Header("踩踏效果参数")]
    [Tooltip("踩踏半径（世界单位）")]
    public float footprintRadius = 0.15f;

    [Tooltip("踩踏深度（0-1）")]
    [Range(0f, 1f)]
    public float footprintDepth = 0.3f;

    [Tooltip("踩踏柔和度")]
    [Range(0.1f, 5f)]
    public float footprintSoftness = 1.5f;

    [Tooltip("恢复速度（每秒恢复的深度值）")]
    [Range(0f, 0.5f)]
    public float recoverySpeed = 0.05f;

    [Header("材质引用")]
    [Tooltip("需要应用雪地效果的材质（拖入雪地材质）")]
    public Material[] snowMaterials;

    [Tooltip("足迹绘制材质（使用 SnowFootprintDraw Shader）")]
    public Material footprintDrawMaterial;

    [Tooltip("雪地恢复材质（使用 SnowRecovery Shader）")]
    public Material snowRecoveryMaterial;

    [Header("优化设置")]
    [Tooltip("是否启用自动恢复（禁用可节省性能）")]
    public bool enableRecovery = true;

    [Tooltip("更新间隔（秒，0为每帧更新）")]
    public float updateInterval = 0f;

    [Header("调试")]
    [Tooltip("是否在Scene视图中显示RenderTexture调试")]
    public bool showDebugTexture = true;

    // 内部变量
    private RenderTexture heightMap;
    private RenderTexture initialHeightMapSnapshot; // 初始高度图快照，用于恢复参考
    private Material drawMaterial;
    private Material recoveryMaterial;
    private Camera renderCamera;
    private float lastUpdateTime;

    // Shader属性ID（优化性能）
    private static readonly int WorldToUVMatrixID = Shader.PropertyToID("_WorldToUVMatrix");
    private static readonly int FootprintPosID = Shader.PropertyToID("_FootprintPos");
    private static readonly int FootprintRadiusID = Shader.PropertyToID("_FootprintRadius");
    private static readonly int FootprintDepthID = Shader.PropertyToID("_FootprintDepth");
    private static readonly int FootprintSoftnessID = Shader.PropertyToID("_FootprintSoftness");
    private static readonly int RecoverySpeedID = Shader.PropertyToID("_RecoverySpeed");
    private static readonly int DeltaTimeID = Shader.PropertyToID("_DeltaTime");
    private static readonly int HeightMapID = Shader.PropertyToID("_SnowHeightMap");

    // 边缘设置缓存（用于检测变化）
    private float lastEdgeFalloff;
    private float lastEdgeSmoothness;

    public RenderTexture HeightMap => heightMap;
    public Vector2 WorldSize => worldSize;
    public Vector3 WorldCenter => worldCenter;

    void Start()
    {
        InitializeSystem();
    }

    void InitializeSystem()
    {
        Debug.Log("=== 雪地系统初始化开始 ===");

        // 创建RenderTexture
        heightMap = new RenderTexture(textureResolution, textureResolution, 0, textureFormat);
        heightMap.filterMode = FilterMode.Bilinear;
        heightMap.wrapMode = TextureWrapMode.Clamp;
        heightMap.Create();
        Debug.Log($"✓ RenderTexture创建成功: {textureResolution}x{textureResolution}");

        // 初始化heightmap：中心为白色（未踩踏），边缘渐变到黑色（下沉）
        InitializeHeightMapWithEdgeFalloff();
        Debug.Log("✓ RenderTexture已初始化，边缘区域已设置为下沉状态");

        // 创建初始高度图的快照，用于恢复参考
        if (enableRecovery)
        {
            initialHeightMapSnapshot = new RenderTexture(textureResolution, textureResolution, 0, textureFormat);
            initialHeightMapSnapshot.filterMode = FilterMode.Bilinear;
            initialHeightMapSnapshot.wrapMode = TextureWrapMode.Clamp;
            initialHeightMapSnapshot.Create();
            Graphics.Blit(heightMap, initialHeightMapSnapshot);
            Debug.Log("✓ 初始高度图快照已创建，用于恢复参考");
        }

        // 创建绘制材质（优先使用直接引用，打包后 Shader.Find 可能失败）
        if (footprintDrawMaterial != null)
        {
            drawMaterial = footprintDrawMaterial;
            Debug.Log("✓ 使用引用的足迹绘制材质");
        }
        else
        {
            // 回退：尝试通过 Shader.Find 创建（仅编辑器模式可靠）
            Shader drawShader = Shader.Find("Hidden/SnowFootprintDraw");
            if (drawShader != null)
            {
                drawMaterial = new Material(drawShader);
                Debug.LogWarning("⚠ 使用 Shader.Find 创建绘制材质（打包后可能失败）。建议在 Inspector 中引用 footprintDrawMaterial");
            }
            else
            {
                Debug.LogError("✗ 未找到足迹绘制材质！请在 Inspector 中指定 footprintDrawMaterial，或确保 Hidden/SnowFootprintDraw Shader 存在");
            }
        }

        // 创建恢复材质
        if (enableRecovery)
        {
            if (snowRecoveryMaterial != null)
            {
                recoveryMaterial = snowRecoveryMaterial;
                Debug.Log("✓ 使用引用的雪地恢复材质");
            }
            else
            {
                // 回退：尝试通过 Shader.Find 创建
                Shader recoveryShader = Shader.Find("Hidden/SnowRecovery");
                if (recoveryShader != null)
                {
                    recoveryMaterial = new Material(recoveryShader);
                    Debug.LogWarning("⚠ 使用 Shader.Find 创建恢复材质（打包后可能失败）。建议在 Inspector 中引用 snowRecoveryMaterial");
                }
                else
                {
                    Debug.LogWarning("未找到恢复材质，恢复功能将被禁用");
                }
            }
        }

        // 设置全局Shader属性
        Shader.SetGlobalTexture(HeightMapID, heightMap);
        UpdateWorldToUVMatrix();

        // 强制刷新全局纹理（解决SRP Batcher缓存问题）
        Shader.SetGlobalTexture("_SnowHeightMap", heightMap);

        // 直接设置材质属性（最可靠的方式）
        UpdateSnowMaterials();

        // 缓存初始边缘设置
        lastEdgeFalloff = edgeFalloff;
        lastEdgeSmoothness = edgeSmoothness;

        // 验证纹理是否设置成功
        Debug.Log($"✓ 全局Shader属性已设置 - HeightMap: {(heightMap != null ? "已绑定" : "未绑定")}");
        Debug.Log($"✓ 直接更新了 {(snowMaterials != null ? snowMaterials.Length : 0)} 个材质");
        Debug.Log($"=== 雪地系统初始化完成 === 覆盖范围: {worldSize}, 中心: {worldCenter}");
    }

    void Update()
    {
        // 更新间隔控制
        if (updateInterval > 0)
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return;
            lastUpdateTime = Time.time;
        }

        // 应用恢复效果
        if (enableRecovery && recoverySpeed > 0)
        {
            ApplyRecovery();
        }

        // 更新全局矩阵（如果中心移动）
        UpdateWorldToUVMatrix();

        // 检测边缘设置变化，实时更新
        if (liveUpdateEdges && (edgeFalloff != lastEdgeFalloff || edgeSmoothness != lastEdgeSmoothness))
        {
            InitializeHeightMapWithEdgeFalloff();

            // 更新初始高度图快照
            if (initialHeightMapSnapshot != null)
            {
                Graphics.Blit(heightMap, initialHeightMapSnapshot);
                Debug.Log("<color=cyan>初始高度图快照已更新</color>");
            }

            UpdateSnowMaterials();
            lastEdgeFalloff = edgeFalloff;
            lastEdgeSmoothness = edgeSmoothness;
            Debug.Log($"<color=cyan>边缘设置已更新: Falloff={edgeFalloff}, Smoothness={edgeSmoothness}</color>");
        }

        // === 测试代码：鼠标点击添加足迹 ===
        // 使用方法：运行游戏后，点击雪地即可看到凹陷效果
        // 按空格键清除所有足迹
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null)
            {
                Debug.LogError("✗ 没有找到Main Camera！请给相机添加MainCamera标签");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.Log($"发射射线从: {ray.origin}");

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Debug.Log($"✓ 击中物体: <color=green>{hit.collider.gameObject.name}</color> 在位置: {hit.point}");
                AddFootprint(hit.point);
            }
            else
            {
                Debug.LogWarning("✗ 射线没有击中任何物体！请确保:\n1. 雪地Plane有MeshCollider或BoxCollider\n2. 相机能看到雪地\n3. 点击的是Game视图而不是Scene视图");
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearFootprints();
            Debug.Log("<color=cyan>清除所有足迹</color>");
        }
        // === 测试代码结束 ===
    }

    /// <summary>
    /// 初始化HeightMap，边缘区域为黑色（下沉），中心叠加柏林噪声形成自然凹凸
    /// </summary>
    private void InitializeHeightMapWithEdgeFalloff()
    {
        // 使用CPU方式生成噪声纹理（使用ARGB32格式，与RenderTexture格式匹配）
        // ARGB32: 与heightMap格式一致，避免Graphics.Blit时的格式转换导致量化误差
        Texture2D tempTex = new Texture2D(textureResolution, textureResolution, TextureFormat.ARGB32, false);

        // 设置随机种子
        Random.InitState(noiseSeed);
        Vector2 randomOffset = new Vector2(Random.value * 1000f, Random.value * 1000f);

        // 创建颜色数组用于批量设置（性能更好）
        Color[] pixels = new Color[textureResolution * textureResolution];

        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                // UV坐标 (0-1) - 使用像素中心采样，避免正好落在0.5产生量化误差
                float u = ((float)x + 0.5f) / textureResolution;
                float v = ((float)y + 0.5f) / textureResolution;

                // 中心坐标 (-0.5 到 0.5)
                float cx = u - 0.5f;
                float cy = v - 0.5f;

                // 到边缘的距离（矩形）
                float distX = Mathf.Abs(cx) * 2f; // 0 (中心) 到 1 (边缘)
                float distY = Mathf.Abs(cy) * 2f;
                float distToEdge = Mathf.Max(distX, distY);

                // 计算边缘衰减（使用简单线性插值代替smoothstep，避免8位精度问题）
                float edgeStart = 1f - edgeFalloff;

                // 简单的线性插值，避免smoothstep的复杂计算导致的量化误差
                float edgeMask;
                if (distToEdge < edgeStart)
                {
                    edgeMask = 0f; // 内部区域，完全保持原高度
                }
                else if (distToEdge >= 1f)
                {
                    edgeMask = 1f; // 完全边缘，完全下沉
                }
                else
                {
                    // 线性插值
                    edgeMask = (distToEdge - edgeStart) / edgeFalloff;
                }
                // edgeMask: 0=内部（保持原高度），1=边缘（完全下沉）

                // 生成柏林噪声（多层叠加）
                float noise = 0f;
                float amplitude = 1f;
                float frequency = 1f;
                float maxValue = 0f;

                // 3层噪声叠加
                for (int octave = 0; octave < 3; octave++)
                {
                    float sampleX = (u + randomOffset.x) * noiseScale * frequency;
                    float sampleY = (v + randomOffset.y) * noiseScale * frequency;
                    float perlin = Mathf.PerlinNoise(sampleX, sampleY);

                    noise += perlin * amplitude;
                    maxValue += amplitude;

                    amplitude *= 0.5f;
                    frequency *= 2f;
                }

                // 归一化噪声到0-1
                noise /= maxValue;

                // 使用原始的简单线性映射（Luna上不会产生网格线条）
                // 将噪声映射到高度范围：1.0 - noiseStrength 到 1.0
                // 这样噪声只会创建轻微凹陷，不会有突起
                float noiseHeight = Mathf.Lerp(1f - noiseStrength, 1f, noise);

                // 应用边缘衰减（边缘强制为0）
                float finalHeight = noiseHeight * (1f - edgeMask);

                // ARGB32格式：使用RGBA四通道，RGB存储相同的高度值，Alpha=1
                // Shader会读取R通道
                int index = y * textureResolution + x;
                pixels[index] = new Color(finalHeight, finalHeight, finalHeight, 1f);
            }
        }

        // 批量设置像素（比逐个SetPixel快得多）
        tempTex.SetPixels(pixels);
        tempTex.Apply();

        // 直接Blit到heightMap（格式匹配，避免采样误差）
        Graphics.Blit(tempTex, heightMap);
        Destroy(tempTex);

        Debug.Log($"✓ 初始化雪地高度图(ARGB32): 噪声强度={noiseStrength}, 噪声缩放={noiseScale}, 种子={noiseSeed}, {(noiseStrength > 0.001f ? "已启用噪声" : "纯平面(无噪声)")}");
    }

    /// <summary>
    /// 在世界坐标位置添加踩踏效果
    /// </summary>
    public void AddFootprint(Vector3 worldPosition)
    {
        if (drawMaterial == null)
        {
            Debug.LogError("✗ drawMaterial为空，无法添加足迹！Shader未找到");
            return;
        }

        if (heightMap == null)
        {
            Debug.LogError("✗ heightMap为空，无法添加足迹！RenderTexture未创建");
            return;
        }

        // 检查是否在范围内
        Vector2 localPos = new Vector2(
            worldPosition.x - worldCenter.x,
            worldPosition.z - worldCenter.z
        );

        if (Mathf.Abs(localPos.x) > worldSize.x * 0.5f ||
            Mathf.Abs(localPos.y) > worldSize.y * 0.5f)
        {
            Debug.LogWarning($"✗ 位置超出雪地范围！位置:{worldPosition}, 范围中心:{worldCenter}, 大小:{worldSize}");
            return;
        }

        // 转换为UV坐标（0-1）
        Vector2 uvPos = new Vector2(
            (localPos.x / worldSize.x) + 0.5f,
            (localPos.y / worldSize.y) + 0.5f
        );

        // 计算UV空间的半径
        float radiusU = footprintRadius / worldSize.x;
        float radiusV = footprintRadius / worldSize.y;
        Vector2 uvRadius = new Vector2(radiusU, radiusV);

        Debug.Log($"<color=yellow>添加足迹: 世界位置={worldPosition}, UV={uvPos}, 深度={footprintDepth}</color>");

        // 设置材质参数
        drawMaterial.SetVector(FootprintPosID, uvPos);
        drawMaterial.SetVector(FootprintRadiusID, uvRadius);
        drawMaterial.SetFloat(FootprintDepthID, footprintDepth);
        drawMaterial.SetFloat(FootprintSoftnessID, footprintSoftness);

        // 渲染到临时RT再复制回去（避免读写同一RT）
        RenderTexture tempRT = RenderTexture.GetTemporary(textureResolution, textureResolution, 0, textureFormat);
        Graphics.Blit(heightMap, tempRT, drawMaterial);
        Graphics.Blit(tempRT, heightMap);
        RenderTexture.ReleaseTemporary(tempRT);

        // 强制更新全局纹理（确保Shader能立即看到变化）
        Shader.SetGlobalTexture(HeightMapID, heightMap);
        Shader.SetGlobalTexture("_SnowHeightMap", heightMap);

        // 直接更新材质（最可靠）
        UpdateSnowMaterials();

        Debug.Log("✓ 足迹已绘制到RenderTexture并更新到材质");
    }

    /// <summary>
    /// 应用雪地恢复效果
    /// </summary>
    private void ApplyRecovery()
    {
        if (recoveryMaterial == null)
            return;

        recoveryMaterial.SetFloat(RecoverySpeedID, recoverySpeed);
        recoveryMaterial.SetFloat(DeltaTimeID, Time.deltaTime);
        recoveryMaterial.SetFloat("_EdgeFalloff", edgeFalloff);
        recoveryMaterial.SetFloat("_EdgeSmoothness", edgeSmoothness);

        // 传递初始高度图作为恢复目标
        if (initialHeightMapSnapshot != null)
        {
            recoveryMaterial.SetTexture("_TargetHeightMap", initialHeightMapSnapshot);
        }

        RenderTexture tempRT = RenderTexture.GetTemporary(textureResolution, textureResolution, 0, textureFormat);
        Graphics.Blit(heightMap, tempRT, recoveryMaterial);
        Graphics.Blit(tempRT, heightMap);
        RenderTexture.ReleaseTemporary(tempRT);
    }

    /// <summary>
    /// 更新世界坐标到UV坐标的转换矩阵
    /// </summary>
    private void UpdateWorldToUVMatrix()
    {
        // 构建从世界坐标到UV坐标的矩阵
        Matrix4x4 worldToUV = Matrix4x4.identity;

        // 缩放
        worldToUV.m00 = 1f / worldSize.x;
        worldToUV.m11 = 1f / worldSize.y;

        // 平移
        worldToUV.m03 = -worldCenter.x / worldSize.x + 0.5f;
        worldToUV.m13 = -worldCenter.z / worldSize.y + 0.5f;

        Shader.SetGlobalMatrix(WorldToUVMatrixID, worldToUV);
        Shader.SetGlobalVector("_SnowWorldCenter", worldCenter);
        Shader.SetGlobalVector("_SnowWorldSize", worldSize);
    }

    /// <summary>
    /// 直接更新材质的纹理属性（最可靠的方式）
    /// </summary>
    private void UpdateSnowMaterials()
    {
        if (snowMaterials == null || heightMap == null)
            return;

        foreach (Material mat in snowMaterials)
        {
            if (mat != null)
            {
                mat.SetTexture("_SnowHeightMap", heightMap);
            }
        }
    }

    /// <summary>
    /// 清除所有踩踏效果
    /// </summary>
    public void ClearFootprints()
    {
        if (heightMap != null)
        {
            RenderTexture.active = heightMap;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = null;
        }
    }

    void OnDestroy()
    {
        // 清理资源
        if (heightMap != null)
        {
            heightMap.Release();
            Destroy(heightMap);
        }

        if (initialHeightMapSnapshot != null)
        {
            initialHeightMapSnapshot.Release();
            Destroy(initialHeightMapSnapshot);
        }

        if (drawMaterial != null)
            Destroy(drawMaterial);

        if (recoveryMaterial != null)
            Destroy(recoveryMaterial);
    }

    // 调试绘制
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(worldSize.x, 0.1f, worldSize.y);
        Gizmos.DrawWireCube(worldCenter, size);
    }

    // GUI调试显示（已禁用）
    /*
    void OnGUI()
    {
        if (!showDebugTexture || heightMap == null)
            return;

        // 在屏幕右上角显示RenderTexture
        int size = 256;
        GUI.DrawTexture(new Rect(Screen.width - size - 10, 10, size, size), heightMap);

        // 显示信息
        GUI.Label(new Rect(Screen.width - size - 10, size + 15, size, 30),
            "HeightMap (白色=未踩踏, 黑色=凹陷)");

        // 显示提示
        if (drawMaterial == null)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 300, 30), "错误: DrawMaterial为空!");
        }

        // 显示系统状态
        GUI.color = Color.white;
        int yPos = 10;
        GUI.Label(new Rect(10, yPos, 400, 25), $"World Size: {worldSize}");
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 400, 25), $"World Center: {worldCenter}");
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 400, 25), $"Footprint Depth: {footprintDepth}");
        yPos += 25;

        // 检查全局纹理（直接检查 heightMap 而不是 Shader.GetGlobalTexture）
        GUI.Label(new Rect(10, yPos, 400, 25), $"Global Texture: {(heightMap != null ? "✓ 已设置" : "✗ 未设置")}");
    }
    */
}
