using System.Collections.Generic;
using UnityEngine;

public class ArrowsManager : MonoSingleton<ArrowsManager>
{
    [Header("引导线设置")]
    private Material lineMaterial;
    public Material lineMaterial1;
    public Material lineMaterial2;
    public float lineWidth = 0.2f;
    [Tooltip("勾选=图标平铺在地面/楼梯表面（贴地，自然透视）；取消=图标正对相机（广告牌，屏幕方正）")]
    public bool lieFlatOnGround = true;

    [Header("箭头设置")]
    public Transform mArrowsParent; // 箭头父物体

    [Header("目标点列表")]
    public List<Transform> targets;

    [Header("性能优化")]
    public float closeDistanceThreshold = 1f; // 自动关闭距离
    public float yOffset = 0.1f; // 路径Y轴偏移(悬浮高度)
    public Transform playerTransform;

    [Header("中转点设置")]
    [Tooltip("上中转点（一般放在楼梯顶部/高处一侧）")]
    public Transform upTransitPoint;
    [Tooltip("下中转点（一般放在楼梯底部/低处一侧）")]
    public Transform downTransitPoint;
    [Tooltip("上、下中转点之间的高度差达到此值时才启用中转绕行（与玩家位置无关，避免玩家走到楼梯中段被误判为同层）")]
    public float heightDiffThreshold = 2f;

    // 内部变量
    private MeshRenderer lineRenderer;
    private Transform currentTarget;
    public Transform CurrentTarget{get{ return currentTarget; }}

    private Mesh lineMesh;
    private Vector3 targetPos;
    private Vector3 offsetPos;
    private Vector3[] vertices;
    private Vector3[] normals;
    private Vector2[] uvs;
    private int builtSectionCount = -1;            // 当前网格已构建的断面数（用于按需重建拓扑）
    private readonly List<Vector3> pathPoints = new List<Vector3>(4); // 起点/中转点/终点构成的折线
    private readonly float[] segLenBuf = new float[8]; // 各段长度缓存（避免重复计算 Distance）
    private Material cachedMaterial;
    public bool isCan=true;//是否初始化显示模板箭头

    // 脏检查缓存：起点/终点/相机朝向未变时跳过重建，空闲帧零开销
    private Vector3 lastLineStart;
    private Vector3 lastLineEnd;
    private Vector3 lastCamForward;
    private float lastLineWidth = float.NaN;   // 线宽变更也触发重建；否则玩家/目标/相机全静止时改 lineWidth 不生效
    private bool lastLieFlat;                   // 平铺/广告牌模式切换同样需要重建条带
    private Camera cam;                 // 主相机引用（用于条带面向相机）
    private bool pathDirty = true;     // 目标切换/重新显示时置脏，强制下帧重建
    private bool arrowsShown;          // mArrowsParent 当前激活状态（避免每帧 SetActive）


    private void Start()
    {
        lineMaterial = LunaManager.IsGoogle() ? lineMaterial2 : lineMaterial1;
        mArrowsParent.GetChild(0).GetComponent<MeshRenderer>().enabled = !LunaManager.IsGoogle();
        mArrowsParent.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = LunaManager.IsGoogle();
        if (LunaManager.IsGoogle())
            lineWidth = 0.4f;

        InitGuideLine();
        // 同步箭头父物体的初始激活态，保证 SetArrowsActive 门控与实际一致
        arrowsShown = mArrowsParent != null && mArrowsParent.gameObject.activeSelf;
        if(isCan)
            SetArrows(0);
        else
            CloseArrows();
    }

    private void Update()
    {
        if (playerTransform == null || currentTarget == null) return;

        SetArrowsActive(true);

        if (cam == null) cam = Camera.main;
        Vector3 camFwd = cam != null ? cam.transform.forward : Vector3.forward;

        Vector3 tgt = currentTarget.position;
        Vector3 start = playerTransform.position;
        bool targetMoved = tgt != lastLineEnd;

        // 箭头仅在目标移动时跟随重定位（静止帧不写 transform）
        if (targetMoved) CreateArrow(tgt);

        // 引导线仅在起点/终点位移、目标切换、相机旋转或线宽/显示模式变化时重建；相机平移跟随不触发，空闲帧跳过网格上传
        if (pathDirty || targetMoved || start != lastLineStart || camFwd != lastCamForward
            || lineWidth != lastLineWidth || lieFlatOnGround != lastLieFlat)
        {
            CreateGuideLine(start, tgt, camFwd);
            lastLineStart = start;
            lastLineEnd = tgt;
            lastCamForward = camFwd;
            lastLineWidth = lineWidth;
            lastLieFlat = lieFlatOnGround;
            pathDirty = false;
        }
    }

    
    private void InitGuideLine()
    {
        if (lineRenderer != null) return;

        GameObject lineObj = new GameObject("GuideLine");
        lineObj.transform.SetParent(transform);
        lineRenderer = lineObj.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = lineObj.AddComponent<MeshFilter>();

        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
            cachedMaterial = lineRenderer.material;
        }

        lineMesh = new Mesh();
        meshFilter.mesh = lineMesh;
        // 顶点/三角形在首次绘制时按折线断面数构建（见 EnsureMeshTopology）
    }

    /// <summary>
    /// 按段数构建/重建网格拓扑（段数变化时才重建）。
    /// 每段独立 4 顶点（首左/首右/尾左/尾右），段间不共享顶点——这样每段可用“沿本段方向恒定”的
    /// 横向宽度，避免折线拐角处用角平分线缝合时短段被扭曲/剪切（即图标在拐角附近发扁、歪斜的根因）。
    /// segs 段 = segs*4 顶点、segs*2 三角形。
    /// </summary>
    private void EnsureMeshTopology(int segs)
    {
        if (builtSectionCount == segs) return;
        builtSectionCount = segs;

        int vertCount = segs * 4;
        vertices = new Vector3[vertCount];
        normals = new Vector3[vertCount];
        uvs = new Vector2[vertCount];
        for (int s = 0; s < segs; s++)
        {
            int b = s * 4;
            normals[b + 0] = Vector3.up;
            normals[b + 1] = Vector3.up;
            normals[b + 2] = Vector3.up;
            normals[b + 3] = Vector3.up;
            // U：左=0 右=1；uv.y 在 CreateGuideLine 内按累计投影长度逐帧填充
            uvs[b + 0] = new Vector2(0, 0);
            uvs[b + 1] = new Vector2(1, 0);
            uvs[b + 2] = new Vector2(0, 0);
            uvs[b + 3] = new Vector2(1, 0);
        }

        int[] triangles = new int[segs * 6];
        for (int s = 0; s < segs; s++)
        {
            int b = s * 4;
            int t = s * 6;
            triangles[t + 0] = b + 0;
            triangles[t + 1] = b + 2;
            triangles[t + 2] = b + 1;
            triangles[t + 3] = b + 1;
            triangles[t + 4] = b + 2;
            triangles[t + 5] = b + 3;
        }

        lineMesh.Clear();
        lineMesh.vertices = vertices;
        lineMesh.normals = normals;
        lineMesh.uv = uvs;
        lineMesh.triangles = triangles;
    }

    public void SetArrows(int index)
    {
        if (index >= 0 && index < targets.Count)
        {
            SetArrows(targets[index]);
        }
    }

    public void SetArrows(Transform targetPos)
    {
        if (targetPos == null) return;
        if (targetPos == currentTarget) return; // 同目标，无需重设（GuildManager 每帧调用，跳过冗余处理）
        currentTarget = targetPos;
        pathDirty = true;                        // 目标已切换，强制下帧重建引导线
        if (playerTransform == null) return;
        CreateArrow(targetPos.position);
    }

    public void CloseArrows()
    {
        currentTarget = null;
        pathDirty = true;                        // 重新显示时强制重建
        lineRenderer.enabled = false;
        SetArrowsActive(false);
    }

    // 仅在激活状态变化时调用 SetActive，避免每帧的托管→原生调用
    private void SetArrowsActive(bool active)
    {
        if (arrowsShown == active) return;
        arrowsShown = active;
        mArrowsParent.gameObject.SetActive(active);
    }
    /// <summary>
    /// 根据玩家(起点)与目标当前所处高度，决定折线途经的中转点，填充 pathPoints。
    /// 仅当“上、下中转点之间”的通道高度 ≥ 阈值时才启用中转绕行；该判据与玩家位置无关，
    /// 避免玩家走到楼梯中段、与目标高度差变小时被误判为同层。
    /// 启用后，只纳入“高度上位于玩家与目标之间”的中转点，并按行进方向排序：
    ///   目标在下方(玩家向下)：从高到低，玩家已下降越过的中转点自动剔除。
    ///   目标在上方(玩家向上)：从低到高，玩家已上升越过的中转点自动剔除。
    /// 玩家与目标同层(之间无中转点)时直连。所有点统一抬高 yOffset。
    /// </summary>
    private void BuildPath(Vector3 startPos, Vector3 endPos)
    {
        pathPoints.Clear();
        Vector3 lift = Vector3.up * yOffset;

        pathPoints.Add(startPos + lift);

        if (upTransitPoint != null && downTransitPoint != null)
        {
            float playerY = startPos.y;                 // 起点即玩家当前位置
            float endY = endPos.y;
            float upY = upTransitPoint.position.y;
            float downY = downTransitPoint.position.y;

            // 通道(上、下中转点)高度差达到阈值才启用中转绕行——与玩家位置无关，保证整段行程判定一致
            if (Mathf.Abs(upY - downY) >= heightDiffThreshold)
            {
                if (endY < playerY)
                {
                    // 目标在下方，玩家向下走：按从高到低，仅纳入严格位于玩家与目标之间的中转点
                    if (endY < upY && upY < playerY)
                        pathPoints.Add(upTransitPoint.position + lift);
                    if (endY < downY && downY < playerY)
                        pathPoints.Add(downTransitPoint.position + lift);
                }
                else
                {
                    // 目标在上方，玩家向上走：按从低到高，仅纳入严格位于玩家与目标之间的中转点
                    if (playerY < downY && downY < endY)
                        pathPoints.Add(downTransitPoint.position + lift);
                    if (playerY < upY && upY < endY)
                        pathPoints.Add(upTransitPoint.position + lift);
                }
            }
        }

        pathPoints.Add(endPos + lift);
    }

    // 单位化向量；过短时返回零向量
    private static Vector3 Dir3(Vector3 v)
    {
        return v.sqrMagnitude < 1e-8f ? Vector3.zero : v.normalized;
    }

    private void CreateGuideLine(Vector3 startPos, Vector3 endPos, Vector3 viewDir)
    {
        if (lineMesh == null) return;

        BuildPath(startPos, endPos);
        int sections = pathPoints.Count;
        int segs = sections - 1;
        if (segs < 1)
        {
            lineRenderer.enabled = false;
            return;
        }

        EnsureMeshTopology(segs);

        // 视线方向（朝相机模式用；贴地模式仅作退化兜底），退化时回退到俯视
        Vector3 view = Dir3(viewDir);
        if (view == Vector3.zero) view = Vector3.down;

        // 1) 各段长度：
        //    贴地模式按真实 3D 弧长 → 圆点在地面上世界均匀，像地贴随透视自然前缩；
        //    朝相机模式按“投影到屏幕平面（垂直于视线）”的长度 → 抵消倾斜段前缩，圆点屏幕上方正。
        float totalLength = 0f;
        for (int s = 0; s < segs; s++)
        {
            Vector3 seg = pathPoints[s + 1] - pathPoints[s];
            float d = lieFlatOnGround
                ? seg.magnitude
                : (seg - Vector3.Dot(seg, view) * view).magnitude;
            segLenBuf[s] = d;
            totalLength += d;
        }
        if (totalLength < 1e-4f)
        {
            lineRenderer.enabled = false;
            return;
        }

        // 2) 逐段构建独立 quad，横向宽度方向 W 段内恒定（不被拐角角平分线扭曲/剪切）：
        //    贴地模式  W = up × 本段方向（水平、垂直于路径）→ 条带平铺在地面/楼梯表面；
        //    朝相机模式 W = 本段方向 × 视线（⊥视线）→ 条带正对相机，圆点不被压扁。
        Vector3 lastWidth = Vector3.right;
        float halfWidth = lineWidth * 0.5f;
        float accum = 0f;
        for (int s = 0; s < segs; s++)
        {
            Vector3 dir = Dir3(pathPoints[s + 1] - pathPoints[s]);
            Vector3 width = lieFlatOnGround
                ? Dir3(Vector3.Cross(Vector3.up, dir))
                : Dir3(Vector3.Cross(dir, view));
            if (width == Vector3.zero) width = lastWidth;            // 退化（贴地:段近垂直；朝相机:段≈视线），沿用上一段
            if (Vector3.Dot(width, lastWidth) < 0f) width = -width;  // 保持左右一致，避免段间镜像翻转
            lastWidth = width;

            Vector3 offset = width * halfWidth;
            Vector3 pS = pathPoints[s];
            Vector3 pE = pathPoints[s + 1];

            float vS = accum / totalLength;
            accum += segLenBuf[s];
            float vE = accum / totalLength;

            int b = s * 4;
            vertices[b + 0] = pS - offset;
            vertices[b + 1] = pS + offset;
            vertices[b + 2] = pE - offset;
            vertices[b + 3] = pE + offset;
            uvs[b + 0] = new Vector2(0, vS);
            uvs[b + 1] = new Vector2(1, vS);
            uvs[b + 2] = new Vector2(0, vE);
            uvs[b + 3] = new Vector2(1, vE);
        }

        lineMesh.vertices = vertices;
        lineMesh.uv = uvs;

        if (cachedMaterial != null)
        {
            // _MainTex_ST.y(纹理平铺数)= totalLength/lineWidth，供 NavPath.shader 用于把
            // 顶点归一化 uv.y(0玩家→1目标)还原为世界尺度。图标“锚定到目标端、玩家移动不带动”
            // 的逻辑在 shader 内用 (1 - uv.y) 实现，此处只负责传入正确的世界平铺尺度。
            Vector2 currentScale = cachedMaterial.mainTextureScale;
            cachedMaterial.mainTextureScale = new Vector2(currentScale.x, totalLength / lineWidth);
        }

        lineMesh.RecalculateBounds();
        lineRenderer.enabled = true;
    }

    private void CreateArrow(Vector3 targetPos)
    {
        this.targetPos=targetPos;
        mArrowsParent.transform.position = targetPos+offsetPos;
        SetArrowsActive(true);
    }

    public void UpdateOffsetPos(Vector3 offsetPos)
    {
        this.offsetPos=offsetPos;
        mArrowsParent.transform.position = targetPos+offsetPos;
    }
  
}