using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CutObj : MonoBehaviour
{
    [Header("碎片物体列表")]
    public List<GameObject> fragmentObjects = new List<GameObject>();

    [Header("推力设置")]
    public float forceAmount = 500f;
    [Header("随机范围")]
    public float forceRandomRange = 200f; // 推力随机范围
    public float directionRandomRange = 0.3f; // 方向随机范围

    [Header("性能优化")]
    public float rigidbodyDisableDelay = 2f; // 延迟禁用刚体的时间（秒）

    private MeshRenderer meshRenderer;
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();
    private Coroutine disableRigidbodyCoroutine;

    public bool IsCut=false;
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // 保存列表物体的初始位置和旋转
        foreach (GameObject obj in fragmentObjects)
        {
            if (obj != null)
            {
                originalPositions.Add(obj.transform.position);
                originalRotations.Add(obj.transform.rotation);
                obj.GetComponent<MeshFilter>().mesh= GetComponentInParent<MeshFilter>().mesh;
                obj.GetComponent<MeshRenderer>().material=GetComponentInParent<CutObjTest>().material;
                obj.SetActive(false);
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Enemy"))
        {
            TriggerCut();
        }

    }

    /// <summary>
    /// 触发切割效果，隐藏本物体，显示并施加推力到碎片物体
    /// </summary>
    public void TriggerCut()
    {
        if (IsCut)
        {
            return;
        }
        IsCut=true;
        GetComponent<BoxCollider>().enabled=false;
        GetComponent<MeshRenderer>().enabled=false;
        GetComponentInParent<CutObjTest>().ShakeCamera();
    

        // 显示并施加推力到列表物体
        foreach (GameObject obj in fragmentObjects)
        {
            if (obj != null)
            {
                // 开启物体显示
                obj.SetActive(true);

                // 在物体斜前方施加推力（基于物体局部坐标）
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 计算物体局部坐标的斜前方向（前+上）
                    Vector3 localForceDirection = (obj.transform.forward + obj.transform.up).normalized;

                    // 添加随机方向偏移
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-directionRandomRange, directionRandomRange),
                        Random.Range(-directionRandomRange, directionRandomRange),
                        Random.Range(-directionRandomRange, directionRandomRange)
                    );
                    localForceDirection = (localForceDirection + randomOffset).normalized;

                    // 添加随机力度
                    float randomForce = forceAmount + Random.Range(-forceRandomRange, forceRandomRange);

                    rb.AddForce(localForceDirection * randomForce);
                }
            }
        }

        // 启动延迟禁用刚体的协程
        if (rigidbodyDisableDelay > 0)
        {
            if (disableRigidbodyCoroutine != null)
            {
                StopCoroutine(disableRigidbodyCoroutine);
            }
            disableRigidbodyCoroutine = StartCoroutine(DisableRigidbodiesAfterDelay());
        }
    }

    /// <summary>
    /// 延迟禁用碎片刚体以优化性能
    /// </summary>
    private IEnumerator DisableRigidbodiesAfterDelay()
    {
        yield return new WaitForSeconds(rigidbodyDisableDelay);

        foreach (GameObject obj in fragmentObjects)
        {
            if (obj != null && obj.activeSelf)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 将刚体设置为运动学模式，停止物理计算
                    rb.isKinematic = true;
                }
            }
        }

        disableRigidbodyCoroutine = null;
    }

    /// <summary>
    /// 恢复物体状态，重置碎片物体位置旋转，显示本物体
    /// </summary>
    public void RestoreObject()
    {
        if (!IsCut)
        {
            return;
        }
        IsCut = false;
        GetComponent<BoxCollider>().enabled = true;

        // 停止延迟禁用刚体的协程
        if (disableRigidbodyCoroutine != null)
        {
            StopCoroutine(disableRigidbodyCoroutine);
            disableRigidbodyCoroutine = null;
        }

        // 恢复列表物体位置旋转，并关闭显示
        for (int i = 0; i < fragmentObjects.Count; i++)
        {
            if (fragmentObjects[i] != null)
            {
                // 恢复位置和旋转
                fragmentObjects[i].transform.position = originalPositions[i];
                fragmentObjects[i].transform.rotation = originalRotations[i];

                // 重置刚体速度并恢复物理状态
                Rigidbody rb = fragmentObjects[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false; // 恢复物理计算
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // 关闭显示
                fragmentObjects[i].SetActive(false);
            }
        }

        // 开启本物体的网格显示
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }
    }
}
