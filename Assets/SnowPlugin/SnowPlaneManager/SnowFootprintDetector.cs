using UnityEngine;

/// <summary>
/// 踩踏检测组件 - 挂载到玩家或其他会踩雪的物体上
/// </summary>
public class SnowFootprintDetector : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("雪地管理器引用（留空则自动查找）")]
    public SnowInteractionManager snowManager;

    [Header("检测设置")]
    [Tooltip("检测移动的最小距离（米）")]
    public float minMoveDistance = 0.1f;

    [Tooltip("是否仅在地面上时踩踏")]
    public bool onlyOnGround = true;

    [Tooltip("地面检测层级")]
    public LayerMask groundLayer = -1;

    [Tooltip("地面检测距离")]
    public float groundCheckDistance = 0.5f;

    [Header("足迹自定义")]
    [Tooltip("覆盖默认的足迹半径（0=使用管理器默认值）")]
    public float customFootprintRadius = 0f;

    [Tooltip("覆盖默认的足迹深度（0=使用管理器默认值）")]
    [Range(0f, 1f)]
    public float customFootprintDepth = 0f;

    [Header("优化")]
    [Tooltip("每秒最多踩踏次数限制")]
    public float maxFootprintsPerSecond = 30f;

    // 内部状态
    private Vector3 lastFootprintPosition;
    private float lastFootprintTime;
    private bool isGrounded;

    void Start()
    {
        // 自动查找雪地管理器
        if (snowManager == null)
        {
            snowManager = FindObjectOfType<SnowInteractionManager>();
            if (snowManager == null)
            {
                Debug.LogWarning($"[SnowFootprintDetector] 未找到 SnowInteractionManager，{gameObject.name} 的雪地交互将不起作用");
            }
        }

        lastFootprintPosition = transform.position;
    }

    void Update()
    {
        if (snowManager == null)
            return;

        // 检测是否在地面上
        if (onlyOnGround)
        {
            CheckGroundStatus();
            if (!isGrounded)
                return;
        }

        // 检测移动距离
        float distanceMoved = Vector3.Distance(transform.position, lastFootprintPosition);

        if (distanceMoved >= minMoveDistance)
        {
            // 频率限制
            if (maxFootprintsPerSecond > 0)
            {
                float minInterval = 1f / maxFootprintsPerSecond;
                if (Time.time - lastFootprintTime < minInterval)
                    return;
            }

            // 添加足迹
            AddFootprint();

            lastFootprintPosition = transform.position;
            lastFootprintTime = Time.time;
        }
    }

    /// <summary>
    /// 检测是否在地面上
    /// </summary>
    void CheckGroundStatus()
    {
        // 使用射线检测或CharacterController状态
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            isGrounded = controller.isGrounded;
        }
        else
        {
            // 使用射线检测
            Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
            isGrounded = Physics.Raycast(ray, groundCheckDistance, groundLayer);
        }
    }

    /// <summary>
    /// 添加足迹到雪地
    /// </summary>
    void AddFootprint()
    {
        // 临时保存管理器的原始参数
        float originalRadius = snowManager.footprintRadius;
        float originalDepth = snowManager.footprintDepth;

        // 如果设置了自定义参数，临时覆盖
        if (customFootprintRadius > 0)
            snowManager.footprintRadius = customFootprintRadius;

        if (customFootprintDepth > 0)
            snowManager.footprintDepth = customFootprintDepth;

        // 添加足迹
        snowManager.AddFootprint(transform.position);

        // 恢复管理器参数
        snowManager.footprintRadius = originalRadius;
        snowManager.footprintDepth = originalDepth;
    }

    /// <summary>
    /// 手动添加足迹（可从外部调用）
    /// </summary>
    public void ManualAddFootprint(Vector3 worldPosition)
    {
        if (snowManager != null)
        {
            snowManager.AddFootprint(worldPosition);
        }
    }

    // 调试可视化
    void OnDrawGizmosSelected()
    {
        if (onlyOnGround)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        }
    }
}
