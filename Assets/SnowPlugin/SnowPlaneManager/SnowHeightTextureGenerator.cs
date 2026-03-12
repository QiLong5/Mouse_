using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 生成雪地高度纹理工具
/// 用于创建黑白灰纹理来初始化雪地的高度状态
/// </summary>
public class SnowHeightTextureGenerator : MonoBehaviour
{
    [Header("纹理设置")]
    [Tooltip("生成的纹理分辨率")]
    public int textureSize = 512;

    [Header("高度分布")]
    [Tooltip("高区域比例（白色，未踩踏）")]
    [Range(0f, 1f)]
    public float highAreaRatio = 0.6f;

    [Tooltip("中等高度区域比例（灰色，轻微凹陷0.7-0.9）")]
    [Range(0f, 1f)]
    public float mediumAreaRatio = 0.3f;

    [Tooltip("低区域比例（黑色，完全凹陷）")]
    [Range(0f, 1f)]
    public float lowAreaRatio = 0.1f;

    [Header("噪声设置")]
    [Tooltip("噪声缩放（值越小，噪声越大）")]
    [Range(1f, 50f)]
    public float noiseScale = 10f;

    [Tooltip("噪声层数（越多越细节）")]
    [Range(1, 5)]
    public int noiseOctaves = 3;

    [Tooltip("随机种子")]
    public int randomSeed = 12345;

    [Header("输出设置")]
    [Tooltip("保存的文件名")]
    public string outputFileName = "SnowHeightTexture";

    private Texture2D generatedTexture;

    [ContextMenu("生成高度纹理")]
    public void GenerateTexture()
    {
        // 创建纹理
        generatedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        // 设置随机种子
        Random.InitState(randomSeed);
        Vector2 randomOffset = new Vector2(Random.value * 1000f, Random.value * 1000f);

        // 归一化比例
        float total = highAreaRatio + mediumAreaRatio + lowAreaRatio;
        float normalizedLow = lowAreaRatio / total;
        float normalizedMedium = mediumAreaRatio / total;
        float normalizedHigh = highAreaRatio / total;

        // 计算阈值（从低到高）
        float threshold1 = normalizedLow; // 低于此值为黑色（低区域）
        float threshold2 = normalizedLow + normalizedMedium; // 介于之间为灰色（中等区域）
        // 高于threshold2为白色（高区域）

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float u = (float)x / textureSize;
                float v = (float)y / textureSize;

                // 生成多层Perlin噪声
                float noiseValue = 0f;
                float amplitude = 1f;
                float frequency = 1f;
                float maxValue = 0f;

                for (int octave = 0; octave < noiseOctaves; octave++)
                {
                    float sampleX = (u + randomOffset.x) * noiseScale * frequency;
                    float sampleY = (v + randomOffset.y) * noiseScale * frequency;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseValue += perlin * amplitude;
                    maxValue += amplitude;

                    amplitude *= 0.5f;
                    frequency *= 2f;
                }

                // 归一化到0-1
                noiseValue /= maxValue;

                // 根据阈值确定高度值
                float height;
                if (noiseValue < threshold1)
                {
                    // 黑色区域（低，完全凹陷）
                    // 根据噪声值在0-threshold1之间映射到0-0.3
                    if (threshold1 > 0)
                    {
                        float t = noiseValue / threshold1;
                        height = t * 0.3f;
                    }
                    else
                    {
                        height = 0.15f; // 如果threshold1为0，使用中间值
                    }
                }
                else if (noiseValue < threshold2)
                {
                    // 灰色区域（中等高度，轻微凹陷 0.7-0.9）
                    if (threshold2 > threshold1)
                    {
                        float t = (noiseValue - threshold1) / (threshold2 - threshold1);
                        height = Mathf.Lerp(0.7f, 0.9f, t);
                    }
                    else
                    {
                        height = 0.8f; // 如果区间为0，使用中间值
                    }
                }
                else
                {
                    // 白色区域（高，未踩踏）
                    height = 1.0f;
                }

                // 添加一些细微的噪声变化
                float fineNoise = Mathf.PerlinNoise(
                    (u + randomOffset.x) * noiseScale * 4f,
                    (v + randomOffset.y) * noiseScale * 4f
                ) * 0.05f;
                height = Mathf.Clamp01(height + fineNoise - 0.025f);

                Color color = new Color(height, height, height, 1f);
                generatedTexture.SetPixel(x, y, color);
            }
        }

        generatedTexture.Apply();
        Debug.Log($"✓ 高度纹理已生成: {textureSize}x{textureSize}");
        Debug.Log($"  - 白色区域(高): {highAreaRatio * 100f}%");
        Debug.Log($"  - 灰色区域(中): {mediumAreaRatio * 100f}%");
        Debug.Log($"  - 黑色区域(低): {lowAreaRatio * 100f}%");
    }

    [ContextMenu("保存纹理到Assets")]
    public void SaveTexture()
    {
#if UNITY_EDITOR
        if (generatedTexture == null)
        {
            Debug.LogError("✗ 没有生成纹理！请先生成纹理");
            return;
        }

        // 创建保存路径
        string folderPath = "Assets/Textures";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Textures");
        }

        // 转换为PNG
        byte[] pngData = generatedTexture.EncodeToPNG();
        if (pngData == null)
        {
            Debug.LogError("✗ 纹理编码失败！");
            return;
        }

        string filePath = $"{folderPath}/{outputFileName}.png";
        System.IO.File.WriteAllBytes(filePath, pngData);

        AssetDatabase.Refresh();

        // 设置导入设置
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.sRGBTexture = false; // 线性空间，不使用sRGB
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log($"✓ 纹理已保存到: {filePath}");
        Debug.Log("  - 已设置为线性空间(sRGBTexture=false)");
        Debug.Log("  - 已设置为Clamp包裹模式");

        // 在Project窗口中高亮显示
        Object savedAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        EditorGUIUtility.PingObject(savedAsset);
#else
        Debug.LogWarning("保存功能仅在编辑器中可用");
#endif
    }

    [ContextMenu("生成并保存")]
    public void GenerateAndSave()
    {
        GenerateTexture();
        SaveTexture();
    }

    // 在Scene视图中预览生成的纹理
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (generatedTexture != null)
        {
            // 在物体位置上方显示纹理预览
            Vector3 previewPos = transform.position + Vector3.up * 2f;
            float previewSize = 3f;

            Handles.BeginGUI();
            Vector3 screenPos = Camera.current.WorldToScreenPoint(previewPos);
            if (screenPos.z > 0)
            {
                Rect previewRect = new Rect(
                    screenPos.x - previewSize * 50,
                    Screen.height - screenPos.y - previewSize * 50,
                    previewSize * 100,
                    previewSize * 100
                );
                GUI.DrawTexture(previewRect, generatedTexture);
                GUI.Label(new Rect(previewRect.x, previewRect.y - 20, 200, 20),
                    "生成的高度纹理预览");
            }
            Handles.EndGUI();
        }
#endif
    }
}
