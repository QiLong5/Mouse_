using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 烘焙平滑法线到顶点色，用于描边
/// 使用方法：选择模型 -> 右键 -> Outline/Bake Smooth Normals to Color
/// </summary>
public class OutlineNormalBaker : MonoBehaviour
{
    private const string DEFAULT_OUTPUT_PATH = "Assets/OutlineMesh";

    [MenuItem("CONTEXT/MeshFilter/Outline/Bake Smooth Normals to Color")]
    private static void BakeSmoothNormalsToColor(MenuCommand command)
    {
        MeshFilter meshFilter = command.context as MeshFilter;
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("需要选择包含 MeshFilter 的物体");
            return;
        }

        Mesh originalMesh = meshFilter.sharedMesh;
        Mesh newMesh = BakeSmoothNormals(originalMesh);

        if (newMesh != null)
        {
            // Ensure output directory exists
            EnsureDirectoryExists(DEFAULT_OUTPUT_PATH);

            // Generate file name
            string defaultName = string.IsNullOrEmpty(originalMesh.name) ? "Mesh" : originalMesh.name;
            string defaultPath = $"{DEFAULT_OUTPUT_PATH}/{defaultName}_SmoothOutline.asset";

            // Make sure the filename is unique
            defaultPath = AssetDatabase.GenerateUniqueAssetPath(defaultPath);

            string newPath = EditorUtility.SaveFilePanelInProject(
                "保存平滑法线网格",
                System.IO.Path.GetFileName(defaultPath),
                "asset",
                "选择保存位置（默认保存到 Assets/OutlineMesh）",
                DEFAULT_OUTPUT_PATH
            );

            if (string.IsNullOrEmpty(newPath))
            {
                Debug.Log("用户取消了保存操作");
                return;
            }

            AssetDatabase.CreateAsset(newMesh, newPath);
            AssetDatabase.SaveAssets();

            meshFilter.sharedMesh = newMesh;
            Debug.Log($"平滑法线已烘焙到顶点色，保存至: {newPath}");
        }
    }

    private static Mesh BakeSmoothNormals(Mesh mesh)
    {
        Mesh newMesh = Object.Instantiate(mesh);
        newMesh.name = mesh.name + "_SmoothOutline";

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        // 计算平滑法线（基于位置合并顶点）
        Dictionary<Vector3, Vector3> smoothNormals = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            if (!smoothNormals.ContainsKey(pos))
            {
                smoothNormals[pos] = Vector3.zero;
            }
            smoothNormals[pos] += normals[i];
        }

        // 归一化平滑法线
        Vector3[] keys = new Vector3[smoothNormals.Keys.Count];
        smoothNormals.Keys.CopyTo(keys, 0);
        foreach (Vector3 key in keys)
        {
            smoothNormals[key] = smoothNormals[key].normalized;
        }

        // 将平滑法线存储到顶点色
        Color[] colors = new Color[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 smoothNormal = smoothNormals[vertices[i]];
            // 将法线从 [-1, 1] 映射到 [0, 1]
            colors[i] = new Color(
                smoothNormal.x * 0.5f + 0.5f,
                smoothNormal.y * 0.5f + 0.5f,
                smoothNormal.z * 0.5f + 0.5f,
                1.0f
            );
        }

        newMesh.colors = colors;
        return newMesh;
    }

    [MenuItem("CONTEXT/MeshFilter/Outline/Remove Smooth Normals")]
    private static void RemoveSmoothNormals(MenuCommand command)
    {
        MeshFilter meshFilter = command.context as MeshFilter;
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("需要选择包含 MeshFilter 的物体");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Mesh newMesh = Object.Instantiate(mesh);
        newMesh.colors = null;

        // Ensure output directory exists
        EnsureDirectoryExists(DEFAULT_OUTPUT_PATH);

        // Generate file name
        string defaultName = string.IsNullOrEmpty(mesh.name) ? "Mesh" : mesh.name;
        string defaultPath = $"{DEFAULT_OUTPUT_PATH}/{defaultName}_NoColor.asset";

        // Make sure the filename is unique
        defaultPath = AssetDatabase.GenerateUniqueAssetPath(defaultPath);

        string newPath = EditorUtility.SaveFilePanelInProject(
            "保存清除顶点色的网格",
            System.IO.Path.GetFileName(defaultPath),
            "asset",
            "选择保存位置（默认保存到 Assets/OutlineMesh）",
            DEFAULT_OUTPUT_PATH
        );

        if (string.IsNullOrEmpty(newPath))
        {
            Debug.Log("用户取消了保存操作");
            return;
        }

        AssetDatabase.CreateAsset(newMesh, newPath);
        AssetDatabase.SaveAssets();

        meshFilter.sharedMesh = newMesh;
        Debug.Log($"顶点色已清除，保存至: {newPath}");
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parentFolder = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);

            // If parent doesn't exist, create recursively
            if (!string.IsNullOrEmpty(parentFolder) && parentFolder != "Assets")
            {
                EnsureDirectoryExists(parentFolder);
            }

            // Create the folder
            AssetDatabase.CreateFolder(parentFolder, folderName);
            AssetDatabase.Refresh();
            Debug.Log($"创建目录: {path}");
        }
    }
}
