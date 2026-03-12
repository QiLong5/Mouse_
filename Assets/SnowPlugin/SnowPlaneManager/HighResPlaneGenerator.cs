using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 生成高精度细分Plane网格，用于显示细腻的顶点位移效果
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HighResPlaneGenerator : MonoBehaviour
{
    [Header("网格设置")]
    [Tooltip("X方向的顶点数量（推荐100-200）")]
    [Range(10, 500)]
    public int verticesX = 100;

    [Tooltip("Z方向的顶点数量（推荐100-200）")]
    [Range(10, 500)]
    public int verticesZ = 100;

    [Tooltip("Plane的尺寸（世界单位）")]
    public Vector2 planeSize = new Vector2(10f, 10f);

    [Header("优化")]
    [Tooltip("是否合并网格（减少DrawCall）")]
    public bool combineMesh = true;

    [Header("保存设置")]
    [Tooltip("保存的网格名称")]
    public string savedMeshName = "HighResPlane";

    private MeshFilter meshFilter;

    void Start()
    {
        GeneratePlane();
    }

    [ContextMenu("重新生成Plane")]
    public void GeneratePlane()
    {
        meshFilter = GetComponent<MeshFilter>();

        Mesh mesh = GenerateHighResPlaneMesh(verticesX, verticesZ, planeSize);
        meshFilter.mesh = mesh;

        Debug.Log($"✓ 生成高精度Plane: {verticesX}x{verticesZ} 顶点 = {verticesX * verticesZ} 个顶点");
    }

    [ContextMenu("保存Mesh到Assets")]
    public void SaveMeshToAssets()
    {
#if UNITY_EDITOR
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshFilter.sharedMesh == null)
        {
            Debug.LogError("✗ 没有网格可保存！请先生成网格");
            return;
        }

        // 创建保存路径
        string folderPath = "Assets/Meshes";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Meshes");
        }

        string assetPath = $"{folderPath}/{savedMeshName}.asset";

        // 检查是否已存在同名资产
        if (AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) != null)
        {
            if (!EditorUtility.DisplayDialog("覆盖确认",
                $"已存在网格资产: {savedMeshName}\n是否覆盖？",
                "覆盖", "取消"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(assetPath);
        }

        // 创建网格副本（避免保存运行时生成的临时网格）
        Mesh meshToSave = Instantiate(meshFilter.sharedMesh);
        meshToSave.name = savedMeshName;

        // 保存到Assets
        AssetDatabase.CreateAsset(meshToSave, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ 网格已保存到: {assetPath}");

        // 加载保存的资产并替换当前网格
        Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        meshFilter.sharedMesh = savedMesh;

        Debug.Log($"✓ 已切换到保存的网格资产");
#else
        Debug.LogWarning("保存网格功能仅在编辑器中可用");
#endif
    }

    /// <summary>
    /// 生成高精度Plane网格
    /// </summary>
    private Mesh GenerateHighResPlaneMesh(int resX, int resZ, Vector2 size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "HighResPlane";

        // 顶点数组
        int totalVertices = resX * resZ;
        Vector3[] vertices = new Vector3[totalVertices];
        Vector2[] uvs = new Vector2[totalVertices];
        Vector3[] normals = new Vector3[totalVertices];

        // 生成顶点
        float stepX = size.x / (resX - 1);
        float stepZ = size.y / (resZ - 1);
        float offsetX = -size.x * 0.5f;
        float offsetZ = -size.y * 0.5f;

        for (int z = 0; z < resZ; z++)
        {
            for (int x = 0; x < resX; x++)
            {
                int index = z * resX + x;

                // 位置
                vertices[index] = new Vector3(
                    offsetX + x * stepX,
                    0f,
                    offsetZ + z * stepZ
                );

                // UV (0-1)
                uvs[index] = new Vector2(
                    (float)x / (resX - 1),
                    (float)z / (resZ - 1)
                );

                // 法线（向上）
                normals[index] = Vector3.up;
            }
        }

        // 生成三角形索引
        int quadCount = (resX - 1) * (resZ - 1);
        int[] triangles = new int[quadCount * 6]; // 每个quad 2个三角形，每个三角形3个顶点

        int triIndex = 0;
        for (int z = 0; z < resZ - 1; z++)
        {
            for (int x = 0; x < resX - 1; x++)
            {
                int vertIndex = z * resX + x;

                // 第一个三角形
                triangles[triIndex++] = vertIndex;
                triangles[triIndex++] = vertIndex + resX;
                triangles[triIndex++] = vertIndex + 1;

                // 第二个三角形
                triangles[triIndex++] = vertIndex + 1;
                triangles[triIndex++] = vertIndex + resX;
                triangles[triIndex++] = vertIndex + resX + 1;
            }
        }

        // 应用到mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;

        // 重新计算边界
        mesh.RecalculateBounds();

        // 如果需要合并，标记为静态
        if (combineMesh)
        {
            mesh.UploadMeshData(false);
        }

        return mesh;
    }

    // 在Inspector中显示网格信息
    void OnValidate()
    {
        // 限制在合理范围内
        verticesX = Mathf.Clamp(verticesX, 10, 500);
        verticesZ = Mathf.Clamp(verticesZ, 10, 500);
    }
}
