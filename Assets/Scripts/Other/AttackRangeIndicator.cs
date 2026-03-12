using UnityEngine;
using DG.Tweening;

/// <summary>
/// 攻击范围指示器 - 使用Mesh绘制半透明扇形
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AttackRangeIndicator : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    [Header("材质设置")]
    public Material indicatorMaterial; // 可在Inspector中赋值的材质

    [Header("扇形参数")]
    public float radius = 2f;           // 半径
    public float startAngle = -45f;     // 起始角度
    public float endAngle = 45f;        // 结束角度
    public int segments = 20;           // 扇形分段数（越大越平滑）

    [Header("显示设置")]
    public Color indicatorColor = new Color(0, 1, 0, 0.3f); // 半透明绿色
    public float heightOffset = 0.1f;   // 离地高度，避免z-fighting
    public float fadeDuration = 0.3f;   // 淡入淡出时间

    private Tweener currentTween;       // 当前的Tween动画
    private float targetAlpha;          // 目标透明度

    private void Awake()
    {
        // 获取组件
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();


        indicatorMaterial = new Material(indicatorMaterial);
        meshRenderer.material = indicatorMaterial;

        // 设置初始颜色
        indicatorMaterial.color = indicatorColor;

        // 初始化时设为透明（隐藏状态）
        SetAlpha(0f);
    }

    /// <summary>
    /// 更新攻击范围（半径或角度变化时调用）
    /// </summary>
    public void UpdateRange(float newRadius, float newStartAngle, float newEndAngle)
    {
        radius = newRadius;
        startAngle = newStartAngle;
        endAngle = newEndAngle;
        GenerateMesh();
    }

    /// <summary>
    /// 生成扇形Mesh
    /// </summary>
    private void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "AttackRangeMesh";

        // 计算顶点数量：中心点1个 + 扇形边缘点(segments+1)个
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segments * 3];

        // 中心点（相对于物体本地坐标，往后偏移一单位）
        Vector3 center = new Vector3(0, heightOffset, 0);
        vertices[0] = center;
        uvs[0] = new Vector2(0.5f, 0.5f);

        // 生成扇形边缘顶点
        float angleStep = (endAngle - startAngle) / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            // 本地坐标下的点（不需要考虑transform.rotation，因为是本地坐标）
            Vector3 localPoint = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;
            vertices[i + 1] = center + localPoint;

            // UV坐标（可选，用于贴图）
            uvs[i + 1] = new Vector2((localPoint.x / radius + 1f) * 0.5f, (localPoint.z / radius + 1f) * 0.5f);
        }

        // 生成三角形索引（逆时针顺序，法线朝上）
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;           // 中心点
            triangles[i * 3 + 1] = i + 2;   // 下一个边缘点
            triangles[i * 3 + 2] = i + 1;   // 当前边缘点
        }

        // 赋值到Mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }

    /// <summary>
    /// 显示指示器（带淡入效果）
    /// </summary>
    public void Show()
    {
        // 确保Renderer启用
        if (!meshRenderer.enabled)
        {
            meshRenderer.enabled = true;
        }

        // 停止之前的动画
        currentTween?.Kill();

        // 淡入到目标透明度
        targetAlpha = indicatorColor.a;
        currentTween = DOTween.To(() => GetCurrentAlpha(),
                                   alpha => SetAlpha(alpha),
                                   targetAlpha,
                                   fadeDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 隐藏指示器（带淡出效果）
    /// </summary>
    public void Hide()
    {
        // 停止之前的动画
        currentTween?.Kill();

        // 淡出到0透明度
        targetAlpha = 0f;
        currentTween = DOTween.To(() => GetCurrentAlpha(),
                                   alpha => SetAlpha(alpha),
                                   0f,
                                   fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => meshRenderer.enabled = false);
    }

    /// <summary>
    /// 获取当前透明度
    /// </summary>
    private float GetCurrentAlpha()
    {
        if (indicatorMaterial != null)
        {
            return indicatorMaterial.color.a;
        }
        return 0f;
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (indicatorMaterial != null)
        {
            Color color = indicatorMaterial.color;
            color.a = alpha;
            indicatorMaterial.color = color;
        }
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    public void SetColor(Color color)
    {
        indicatorColor = color;
        if (indicatorMaterial != null)
        {
            indicatorMaterial.color = color;
        }
    }
}
