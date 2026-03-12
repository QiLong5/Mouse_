using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class CutObjTest : MonoBehaviour
{
    public List<CutObj> cubes;

    public float mdelayBetweenPushes=0.2f;
    public float upwardAngle=30f; // 向上的角度（度）

    [Header("摄像机抖动设置")]
    public Camera mainCamera; // 主摄像机
    public float shakeDuration = 0.3f; // 抖动持续时间
    public float shakeIntensity = 0.2f; // 抖动强度

    // 存储每个刚体的初始位置和旋转
    private Coroutine pushCoroutine;
    private Vector3 cameraOriginalPosition;

    public Transform knife;
    public Transform knifepos;

    [Header("性能信息显示")]
    public bool showPerformanceInfo = true; // 是否显示性能信息
    public int fontSize = 20; // 字体大小
    private float fps; // 当前帧率
    private float deltaTime = 0.0f; // 帧时间
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        // 如果没有指定摄像机，使用主摄像机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 保存摄像机初始位置
        if (mainCamera != null)
        {
            cameraOriginalPosition = mainCamera.transform.localPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            knife.GetComponent<Rigidbody>().useGravity=true;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
        //   CameraManager.instance.iscut=true;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAndReset();
            knife.GetComponent<Rigidbody>().useGravity = false;
            knife.GetComponent<Rigidbody>().velocity=Vector3.zero;
            knife.position=knifepos.position;
         
        }

        // 计算帧率
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;
    }

    void OnGUI()
    {
        if (!showPerformanceInfo)
            return;

        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = fontSize
        };
        style.normal.textColor = Color.white;

        Rect rect = new Rect(10, 10, w, h * 2 / 100);

        // 构建性能信息文本
        string text = string.Format("FPS: {0:0.}\n", fps);
        text += string.Format("Frame Time: {0:0.0} ms\n", deltaTime * 1000.0f);

#if UNITY_EDITOR
        // 仅在编辑器中显示详细统计信息
        text += string.Format("Draw Calls: {0}\n", UnityEditor.UnityStats.drawCalls);
        text += string.Format("Batches: {0}\n", UnityEditor.UnityStats.batches);
        text += string.Format("Triangles: {0}\n", UnityEditor.UnityStats.triangles);
        text += string.Format("Vertices: {0}\n", UnityEditor.UnityStats.vertices);
#endif

        // 内存信息（在所有平台可用）
        long totalMemory = Profiler.GetTotalAllocatedMemoryLong() / 1048576; // 转换为MB
        text += string.Format("Memory: {0} MB", totalMemory);

        GUI.Label(rect, text, style);
    }

    /// <summary>
    /// 停止推动协程，恢复所有物体到初始位置和旋转，并清除刚体速度
    /// </summary>
    public void StopAndReset()
    {
        // 停止推动协程
        if (pushCoroutine != null)
        {
            StopCoroutine(pushCoroutine);
            pushCoroutine = null;
        }

        // 恢复所有物体的位置、旋转并清除速度
        foreach (CutObj cube in cubes)
        {
            if (cube != null)
            {
                cube.RestoreObject();
            }
        }
    }

    /// <summary>
    /// 触发摄像机抖动效果
    /// </summary>
    public void ShakeCamera()
    {
        if (mainCamera != null&& isfinshShake)
        {
            isfinshShake = false;
            StartCoroutine(ShakeCameraCoroutine());
        }
    }
    bool isfinshShake=true;
    /// <summary>
    /// 摄像机抖动协程
    /// </summary>
    private IEnumerator ShakeCameraCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            // 生成随机偏移
            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;

            mainCamera.transform.localPosition = cameraOriginalPosition + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 恢复摄像机到原始位置
        mainCamera.transform.localPosition = cameraOriginalPosition;
        isfinshShake=true;
    }
}
