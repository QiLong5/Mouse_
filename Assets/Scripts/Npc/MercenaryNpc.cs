using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 佣兵NPC - 自动出门打怪收集战利品并回家丢放
/// </summary>
public class MercenaryNpc : Npc
{
    [Header("物品管理器")]
    public ItemStackManager itemStackManager;
    public GroundItemStackManager targetGroundStackManager;

    [Header("佣兵设置")]
    public int lootCapacity = 5;  // 最大战利品数量
    public float dropOffDelay = 0.2f;  // 丢物品延迟
    public Transform homePosition;  // 家的位置

    [Header("攻击相关")]
    public SphereCollider attackCollider;  // 攻击碰撞检测
    public AttackRangeIndicator attackRangeIndicator;  // 攻击范围指示器
    public float attackRadius = 3f;  // 固定攻击范围
    public float startAngle = 0f;  // 起始角度
    public float endAngle = 90f;  // 结束角度
    public float attackInterval = 1f;  // 攻击间隔
    public float chaseDistance = 20f;  // 追击距离，只有在这个距离内的敌人才会追击

    [Header("状态")]
    public bool isWorking = false;
    public bool isAtHome = false;

    // 私有变量
    private Enemy currentTarget;
    private float attackTimer = 0f;
    private int enemyLayerMask;
    private WaitForSeconds dropDelay;
    private WaitForSeconds attackDelay;
    private WaitForEndOfFrame waitFrame;
    private MercenaryState currentState;
    private readonly List<Enemy> enemiesInRange = new List<Enemy>();  // 攻击范围内的敌人列表

    // 状态枚举
    private enum MercenaryState
    {
        GoingOut,      // 出门寻找敌人
        Fighting,      // 战斗中
        ReturningHome, // 返回家
        DroppingLoot   // 丢放战利品
    }

    void Start()
    {
        // 初始化
        mMoveSpeed = GameDataEditor.instance.playerSpeed;  // 使用和玩家一样的速度
        enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");

        // 初始化缓存对象
        dropDelay = new WaitForSeconds(dropOffDelay);
        attackDelay = new WaitForSeconds(0.5f);
        waitFrame = new WaitForEndOfFrame();

        // 设置攻击范围
        if (attackCollider != null)
        {
            attackCollider.radius = attackRadius;
        }

        // 初始化攻击范围指示器
        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.UpdateRange(attackRadius, startAngle, endAngle);
            attackRangeIndicator.Hide();  // 初始隐藏
        }

       
            StartWork();
        
    }

    void FixedUpdate()
    {
        // 更新攻击计时器
        if (attackTimer > 0)
        {
            attackTimer -= Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// 开始工作
    /// </summary>
    public void StartWork()
    {
        if (!isWorking && !LunaManager.instance.isGameOver)
        {
            isWorking = true;
            StartCoroutine(WorkCycle());
        }
    }

    /// <summary>
    /// 停止工作
    /// </summary>
    public void StopWork()
    {
        isWorking = false;
        StopAllCoroutines();
        StopMovement();
    }

    /// <summary>
    /// 工作循环协程
    /// </summary>
    private IEnumerator WorkCycle()
    {
        while (isWorking && !LunaManager.instance.isGameOver)
        {
            // 1. 出门寻找敌人
            yield return GoingOutPhase();

            // 2. 战斗直到背包满或没有敌人
            yield return FightingPhase();

            // 3. 返回家
            yield return ReturningHomePhase();

            // 4. 丢放战利品
            yield return DroppingLootPhase();
        }
    }

    /// <summary>
    /// 出门阶段 - 寻找最近的敌人
    /// </summary>
    private IEnumerator GoingOutPhase()
    {
        currentState = MercenaryState.GoingOut;
        Debug.Log($"[{name}] 出门寻找敌人");

        // 离开家时显示攻击范围指示器
     

        isAtHome = false;
        mAnimator.Play("Run");

        // 寻找最近的敌人
        Enemy nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;
            Debug.Log($"[{name}] 找到目标敌人: {currentTarget.name}");
            // 移动到敌人附近
            yield return MoveToTargetCoroutine(currentTarget.transform.position);
        }
        else
        {
            Debug.Log($"[{name}] 没有找到敌人，等待中...");
            // 没有敌人，等待一段时间
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// 战斗阶段
    /// </summary>
    private IEnumerator FightingPhase()
    {
        currentState = MercenaryState.Fighting;
        Debug.Log($"[{name}] 进入战斗状态");

        while (isWorking && !LunaManager.instance.isGameOver)
        {
            // 检查背包是否已满
            if (IsBagFull())
            {
                Debug.Log($"[{name}] 背包已满，结束战斗");
                yield break;  // 背包满了，结束战斗阶段
            }

            // 优先检查攻击范围内是否有敌人
            Enemy enemyInRange = FindEnemyInAttackRange();
            if (enemyInRange != null)
            {
                currentTarget = enemyInRange;
            }
            else
            {
                // 如果攻击范围内没有敌人，寻找追击距离内的敌人
                Enemy nearestEnemy = FindNearestEnemy();
                if (nearestEnemy == null)
                {
                    Debug.Log($"[{name}] 没有敌人了，结束战斗");
                    yield break;  // 没有敌人了，结束战斗阶段
                }
                currentTarget = nearestEnemy;
            }

            // 移动到敌人附近并攻击
            bool reachedTarget = false;
            MoveToTarget(currentTarget.transform, () => reachedTarget = true);

            // 等待接近敌人或敌人死亡
            while (!reachedTarget && currentTarget != null && !currentTarget.isDie)
            {
                // 检查是否在攻击范围内
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distance <= attackRadius)
                {
                    // 停止移动，准备攻击
                    StopMovement();

                    // 转向敌人
                    Vector3 direction = currentTarget.transform.position - transform.position;
                    direction.y = 0;
                    if (direction.magnitude > 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
                    }

                    // 检查是否可以攻击
                    if (attackTimer <= 0 && CheckCanAttack())
                    {
                        // 播放攻击动画
                        mAnimator.Play("Attack");

                        // 攻击
                        Attack();
                        Debug.Log($"[{name}] 攻击 {currentTarget.name}");

                        attackTimer = attackInterval;

                        // 等待攻击动画完成
                        yield return attackDelay;

                        // 检查背包是否已满
                        if (IsBagFull())
                        {
                            yield break;
                        }

                        // 攻击后跳出循环，重新寻找下一个目标
                        break;
                    }
                }

                yield return waitFrame;
            }

            // 等待一小段时间再寻找下一个目标
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 返回家阶段
    /// </summary>
    private IEnumerator ReturningHomePhase()
    {
        currentState = MercenaryState.ReturningHome;
        Debug.Log($"[{name}] 返回家中");

        mAnimator.Play("Run");

        // 移动到家的位置
        yield return MoveToTargetCoroutine(homePosition.position);

        isAtHome = true;
        mAnimator.Play("Idle");
        Debug.Log($"[{name}] 到家了");
    }

    /// <summary>
    /// 丢放战利品阶段
    /// </summary>
    private IEnumerator DroppingLootPhase()
    {
        currentState = MercenaryState.DroppingLoot;
        Debug.Log($"[{name}] 开始丢放战利品");

        // 获取物品栈（假设战利品是RawMaterial类型）
        ItemStack lootStack = itemStackManager.stackList[0];
        int totalLoots = lootStack.stackAmount;

        // 丢放所有战利品
        while (lootStack.stackAmount > 0)
        {
            Item item = lootStack.RemoveTopItem();
            if (item != null && targetGroundStackManager != null)
            {
                targetGroundStackManager.StackItem(item);
                yield return dropDelay;
            }
        }

        Debug.Log($"[{name}] 完成丢放战利品 (共{totalLoots}个)");

        // 丢完后等待一小段时间
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 移动到目标位置的协程
    /// </summary>
    private IEnumerator MoveToTargetCoroutine(Vector3 targetPos)
    {
        bool reached = false;
        MoveToTarget(targetPos, () => reached = true);

        while (!reached)
        {
            yield return waitFrame;
        }

        mRigidbody.velocity = Vector3.zero;
    }

    /// <summary>
    /// 寻找最近的敌人
    /// </summary>
    private Enemy FindNearestEnemy()
    {
        if (NpcManager.instance == null || NpcManager.instance.mEnemies == null)
        {
            return null;
        }

        // 遍历NpcManager中的敌人列表
        foreach (Enemy enemy in NpcManager.instance.mEnemies)
        {
            if (enemy == null || enemy.isDie || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // 找到第一个在追击距离内的敌人就直接返回
            if (distance <= chaseDistance)
            {
                return enemy;
            }
        }

        return null;
    }

    /// <summary>
    /// 检查背包是否已满
    /// </summary>
    private bool IsBagFull()
    {
        ItemStack lootStack = itemStackManager.stackList[0];
        return lootStack.stackAmount >= lootCapacity;
    }

    /// <summary>
    /// 检查是否可以攻击（前方有敌人）
    /// </summary>
    private bool CheckCanAttack()
    {
        Vector3 center = transform.position - transform.forward;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, enemyLayerMask);

        foreach (var hit in hits)
        {
            Vector3 dir = hit.transform.position - center;
            dir.y = 0;

            // 转换到本地坐标系
            Vector3 localDir = transform.InverseTransformDirection(dir);

            // 计算角度
            float angle = Mathf.Atan2(localDir.z, localDir.x) * Mathf.Rad2Deg;

            if (angle >= startAngle && angle <= endAngle)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 查找攻击范围内的敌人
    /// </summary>
    private Enemy FindEnemyInAttackRange()
    {
        // 从列表中找到第一个存活的敌人
        for (int i = enemiesInRange.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemiesInRange[i];

            // 清理无效或已死亡的敌人
            if (enemy == null || enemy.isDie)
            {
                enemiesInRange.RemoveAt(i);
                continue;
            }

            return enemy;
        }

        return null;
    }

    /// <summary>
    /// 攻击方法（和Player一样）
    /// </summary>
    public void Attack()
    {
        Vector3 center = transform.position - transform.forward;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, enemyLayerMask);

        int maxTargets = lootCapacity - itemStackManager.stackList[0].stackAmount;
        int attackedCount = 0;

        foreach (var hit in hits)
        {
            if (attackedCount >= maxTargets)
            {
                break;
            }

            Vector3 dir = hit.transform.position - center;
            dir.y = 0;

            // 转换到本地坐标系
            Vector3 localDir = transform.InverseTransformDirection(dir);

            // 计算角度
            float angle = Mathf.Atan2(localDir.z, localDir.x) * Mathf.Rad2Deg;

            if (angle >= startAngle && angle <= endAngle)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDie)
                {
                 //   enemy.SetHp(GameDataEditor.instance.playerAamage,false,transform);
                    attackedCount++;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检测攻击范围内的敌人
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out Enemy enemy) && !enemy.isDie && !enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Add(enemy);
            }
        }

        if (other.CompareTag("Home"))
        {
            if (attackRangeIndicator != null)
            {
                attackRangeIndicator.Hide();
            }
            isAtHome = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 移除离开攻击范围的敌人
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out Enemy enemy))
            {
                enemiesInRange.Remove(enemy);
            }
        }

        if (other.CompareTag("Home"))
        {
            if (attackRangeIndicator != null)
            {
                attackRangeIndicator.Show();
            }
        }
    }
}
