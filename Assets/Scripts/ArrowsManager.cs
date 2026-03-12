using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowsManager : MonoSingleton<ArrowsManager>
{
    [Header("引导线设置")]
    public Material lineMaterial;
    public float lineWidth = 0.2f;

    [Header("箭头设置")]
    public Transform mArrowsParent; // 箭头父物体

    [Header("目标点列表")]
    public List<Transform> targets;

    [Header("性能优化")]
    public float closeDistanceThreshold = 1f; // 自动关闭距离
    public float yOffset = 0.1f; // 路径Y轴偏移(悬浮高度)
    public Transform playerTransform; 

    // 内部变量
    private MeshRenderer lineRenderer;
    private Transform currentTarget;

    private Mesh lineMesh;
    private Vector3 targetPos;
    private Vector3 offsetPos;
    private Vector3[] vertices;
    private Vector3[] normals;
    private Material cachedMaterial;
    public bool isCan=true;//是否初始化显示模板箭头


    void Start()
    {
        InitGuideLine();
        if(isCan)
            SetArrows(0);
        else
            CloseArrows();
    }

    private void Update()
    {
        if (playerTransform == null || currentTarget == null) return;

        Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        Vector3 targetPosFlat = new Vector3(currentTarget.position.x, 0, currentTarget.position.z);
        float distance = Vector3.Distance(playerPosFlat, targetPosFlat);

        if (distance < closeDistanceThreshold)
        {
            CloseArrows();
            return;
        }

        mArrowsParent.gameObject.SetActive(true);
        Vector3 currentStart = playerTransform.position + Vector3.up * yOffset;
        Vector3 currentEnd = new Vector3(currentTarget.position.x, currentStart.y, currentTarget.position.z);
        CreateGuideLine(currentStart, currentEnd);
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
        vertices = new Vector3[4];
        normals = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        int[] triangles = new int[6]
        {
            0, 2, 1,
            1, 2, 3
        };

        lineMesh.vertices = vertices;
        lineMesh.normals = normals;
        lineMesh.uv = uvs;
        lineMesh.triangles = triangles;
        meshFilter.mesh = lineMesh;
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
        currentTarget = targetPos;
        if (playerTransform == null) return;
        CreateArrow(targetPos.position);
    }

    public void CloseArrows()
    {
        currentTarget=null;
        lineRenderer.enabled = false;
        mArrowsParent.gameObject.SetActive(false);
    }

    private void CreateGuideLine(Vector3 startPos, Vector3 endPos)
    {
        if (lineMesh == null) return;

        Vector3 direction = (endPos - startPos).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized * (lineWidth * 0.5f);

        float totalLength = Vector3.Distance(startPos, endPos);
        if (cachedMaterial != null)
        {
            Vector2 currentScale = cachedMaterial.mainTextureScale;
            cachedMaterial.mainTextureScale = new Vector2(currentScale.x, totalLength / lineWidth);
        }

        vertices[0] = startPos - perpendicular;
        vertices[1] = startPos + perpendicular;
        vertices[2] = endPos - perpendicular;
        vertices[3] = endPos + perpendicular;

        lineMesh.vertices = vertices;
        lineMesh.RecalculateBounds();

        lineRenderer.enabled = true;
    }

    private void CreateArrow(Vector3 targetPos)
    {
        this.targetPos=targetPos;
        mArrowsParent.transform.position = targetPos+offsetPos;
        mArrowsParent.gameObject.SetActive(true);
    }

    public void UpdateOffsetPos(Vector3 offsetPos)
    {
        this.offsetPos=offsetPos;
        mArrowsParent.transform.position = targetPos+offsetPos;
    }
  
}