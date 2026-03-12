using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家控制脚本
/// </summary>
public class Player : MonoSingleton<Player>
{
    [Header("Move")]
    public YangJoystick mJoystick;
    
    public Rigidbody mRigidbody;
    //玩家移动速度
    public float speed =>GameDataEditor.instance.playerSpeed;
    //旋转平滑系数
    public float mTurnSmoothTime => GameDataEditor.instance.playerTurnSmoothTime;
    float mTurnSmoothVelocity;
    private bool _isMove;
     public bool IsMove
    {
        get => _isMove;
        set
        {
            if (_isMove!=value)
            {
                _isMove=value;
                mAnimator.SetBool("isMove",_isMove);
            }
        }
    }

    public float mAngleDis;

    public int moneyAmount { get; private set; }
    [Header("动画")]
    public Animator mAnimator;
    public AnimatorStateInfo mStateInfo;
    public Collider mCollider;
    [Header("血条")]
    public float mHp;
    public float mHpMax;
    private bool _isIsDie;
    public bool isDie
     {
        get => _isIsDie;
        set
        {
            if (_isIsDie != value)
            {
                _isIsDie = value;
                mAnimator.SetBool("isDie", _isIsDie);
            }
        }
    }
   
    public UIHealthBar mHpUi;

    [Header("物品堆叠信息")]
    public ItemStackManager itemStackManager; //玩家背后的物品堆管理器
    public GroundItemStackManager targetGroundStackManager = null;  //目标地面物品堆管理器
    public float itemDropOffTimer = -1;
    public float itemDropOffCooldown = 0.1f;  //移除和堆放物品的冷却时间
    public bool isDroppingOffItem  = false;  //正在移除物品
    public bool isCollectingItem  = false;  //正在堆放物品

    public UIFollowerBase maxImg;


    [Header("攻击相关")]

    public SphereCollider mAttackCollider;//敌人检测碰撞体
    public AttackRangeIndicator attackRangeIndicator; // 攻击范围指示器
    bool isrun = false;//是否绘制
    float radius => GameDataEditor.instance.playerAttackRadius;          // 半径
    float startAngle => GameDataEditor.instance.startAngle;      // 起始角度（度）
    public float endAngle => GameDataEditor.instance.endAngle;       // 结束角度（度）
    private int enemyLayerMask;

    float attackInterval=0;
    private bool _isAttack;

    public bool IsAttack
    {
        get => _isAttack;
        set
        {
            if (_isAttack != value)
            {
                _isAttack = value;
                mAnimator.SetBool("isAttack", _isAttack);
            }
        }
    }
    private bool _isAttacking;
    public bool IsAttacking
    {
        get => _isAttacking;
        set
        {
            if (_isAttacking != value)
            {
                _isAttacking = value;
                mAnimator.SetBool("isAttacking", _isAttacking);
            }
        }
    }
   
   public bool IsAtHome;
    void Start()
    {
        mHp = GameDataEditor.instance.playerMaxHp;
        mHpMax= GameDataEditor.instance.playerMaxHp;
        mAttackCollider.radius=GameDataEditor.instance.playerAttackRadius;
        mRigidbody = GetComponent<Rigidbody>();
        IsMove=false;
        IsAttack=false;
        isDie=false;
        enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
        isrun =true;
        mHpUi.SetHpFill(1);
        // 初始化攻击范围指示器
        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.UpdateRange(radius, startAngle, endAngle);
            attackRangeIndicator.Hide(); 
        }
    }

    void Update()
    {

    }

    void FixedUpdate()
    {
        if (isDie || !CameraManager.instance.IsCanMove)
        {
            return;
        }
        if (isDroppingOffItem)
        {
            DropItemToGroundStack();
        }

        if (isCollectingItem)
        {

            CollectItemFromGroundStack();
        }
        if (attackInterval>0)
        {
            attackInterval-=Time.fixedDeltaTime;
        }
        Move();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("GroundStack_DropItem"))
        {
            var stackManager = other.GetComponent<GroundItemStackManager>();
            if (stackManager != null)
            {
                targetGroundStackManager = stackManager;
                isDroppingOffItem = true;
                isCollectingItem = false;
            }
        }

        if (other.tag.Equals("GroundStack_CollectItem"))
        {
            var stackManager = other.GetComponent<GroundItemStackManager>();
            if (stackManager != null)
            {
                targetGroundStackManager = stackManager;
                isCollectingItem = true;
                isDroppingOffItem = false;
            }
        }
        if (other.tag.Equals("Home"))
        {
            IsAtHome = true;
            attackRangeIndicator.Hide();
        }
        if (other.tag.Equals("Enemy"))
        {

            if (attackInterval <= 0&&!IsAttacking&&!IsAtHome)
            {
                if (CheckIsAttack())
                {
                    IsAttack = true;
                    attackInterval = GameDataEditor.instance.playerAttackInterval;
                }
              
            }
        }
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("GroundStack_DropItem"))
        {
            if(targetGroundStackManager.gameObject!=other.gameObject)
            {
                var stackManager = other.GetComponent<GroundItemStackManager>();
                if (stackManager != null)
                {
                    targetGroundStackManager = stackManager;
                    isDroppingOffItem = true;
                    isCollectingItem = false;
                }
            }
            
        }

        if (other.tag.Equals("GroundStack_CollectItem"))
        {
            if (targetGroundStackManager.gameObject != other.gameObject)
            {
                var stackManager = other.GetComponent<GroundItemStackManager>();
                if (stackManager != null)
                {
                    targetGroundStackManager = stackManager;
                    isCollectingItem = true;
                    isDroppingOffItem = false;
                }
            }
             
        }
        if (other.tag.Equals("Home"))
        {
            IsAtHome = true;
            if (mHp < mHpMax)
            {
                mHp++;
                mHpUi.SetHpFill((mHp / mHpMax));
                if (mHp==mHpMax)
                {
                    mHpUi.Hide();
                }
               
                UIManager.instance.StopDanger();
            }
        }
        if (other.tag.Equals("Enemy"))
        {
            if (attackInterval<=0 && !IsAttacking && !IsAtHome)
            {
                if (CheckIsAttack())
                {
                    IsAttack = true;
                    attackInterval = GameDataEditor.instance.playerAttackInterval;
                }
                else
                {
                    IsAttack = false;
                    attackInterval = GameDataEditor.instance.playerAttackInterval;
                }
            }
          
        }
     
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("GroundStack_DropItem"))
        {
            itemDropOffTimer = -1;
            isDroppingOffItem = false;
            isCollectingItem = false;
            targetGroundStackManager = null;
        }

        if (other.tag.Equals("GroundStack_CollectItem"))
        {
            itemDropOffTimer = -1;
            isCollectingItem = false;
            isDroppingOffItem = false;
            targetGroundStackManager = null;
        }
        if (other.tag.Equals("Home"))
        {
            IsAtHome = false;
            attackRangeIndicator.Show();
            mHpUi.gameObject.SetActive(true);
        }
    }
   
    public void Move()
    {
        if (LunaManager.instance.isGameOver)
        {
            IsMove=false;
            OnStop();
            return;
        }
        if (mJoystick == null)
        {
            return;
        }
        if (mJoystick.dragging && Input.GetMouseButton(0))
        {
            IsMove = true;
            float targetAngle = Mathf.Atan2(mJoystick.Horizontal, mJoystick.Vertical) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle - mAngleDis, ref mTurnSmoothVelocity, mTurnSmoothTime);
            mRigidbody.velocity = new Vector3((transform.forward * speed).x, mRigidbody.velocity.y, (transform.forward * speed).z);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
        else
        {
            IsMove = false;
            mRigidbody.velocity = new Vector3(0, mRigidbody.velocity.y, 0);
        }
        if (Input.GetMouseButtonUp(0))
        {
            IsMove = false;
            OnStop();
        }
    }

    /// <summary>
    /// 玩家受伤
    /// </summary>
    public void SetHp()
    {
        mHp--;
        mHpUi.SetHpFill(mHp / mHpMax);
        if (mHp<=0)
        {
            isDie = true;
            OnStop();
            UIManager.instance.StopDanger();
        }
        if (!isDie)
        {
            if ((mHp / mHpMax) < 0.05f)
            {
                UIManager.instance.StartDanger();
            }
        }
       
    }

    /// <summary>
    /// 玩家停止移动
    /// </summary>
    public void OnStop()
    {
        IsMove = false;
        mRigidbody.velocity =new Vector3(0, 0,0);
    }


    /// <summary>
    /// 金币数量变化
    /// </summary>
    /// <param name="_value"></param>
    public void MoneyAmountChange(int _value)
    {
        moneyAmount += _value;
        if (moneyAmount < 0)
        {
            moneyAmount = 0;
        }
        UIManager.instance.SetGold(moneyAmount);
    }

    /// <summary>
    /// 从资源框收集物品
    /// </summary>
    private void CollectItemFromGroundStack()
    {
        if (itemDropOffTimer >= 0)
        {
            itemDropOffTimer -= Time.deltaTime;
        }
        else
        {
            if (targetGroundStackManager != null)
            {
                if (targetGroundStackManager.totalStackedItemsAmount <= 0)//捡完了
                {
                    return;
                }
                int stackIndex = itemStackManager.GetStackIndexByItemType(targetGroundStackManager.stackedItemType);
                if (itemStackManager.stackList[stackIndex].stackAmount >= itemStackManager.stackList[stackIndex].maxStackAmount)//装满了
                {
                    maxImg.gameObject.SetActive(true);
                    return;
                }
                int _count = 1;//简单的动态调整捡物品速度
                if (targetGroundStackManager.totalStackedItemsAmount > 30)
                {
                    _count = 2;
                }
                if (targetGroundStackManager.totalStackedItemsAmount > 50)
                {
                    _count = 3;
                }
                for (int i = 0; i < _count; i++)
                {
                    if (targetGroundStackManager.totalStackedItemsAmount > 0)
                    {
                        if (itemStackManager.stackList[stackIndex].stackAmount >= itemStackManager.stackList[stackIndex].maxStackAmount)//装满了
                        {
                            maxImg.gameObject.SetActive(true);
                            break;
                        }
                        else
                        {
                            Item targetItem = targetGroundStackManager.RemoveItem();
                            if (targetItem != null)
                            {

                                targetItem.gameObject.SetActive(true);
                                targetItem.cd.enabled = false;
                                itemStackManager.stackList[targetItem.targetStackListIndex].StackItem(targetItem);
                            }
                        }
                        
                    }
                    else
                    {
                        break;
                    }
                }

            }

            //   itemDropOffTimer = itemDropOffCooldown;
        }
       
    }

    /// <summary>
    /// 丢物品到资源框
    /// </summary>
    private void DropItemToGroundStack()
    {
        if (itemStackManager.amountOfStackInUse <= 0)//丢完了
        {
            return;
        }
        if (itemDropOffTimer >= 0)
        {
            itemDropOffTimer -= Time.deltaTime;
        }
        else
        {

            if (targetGroundStackManager.totalMaxAmount > 0 && targetGroundStackManager.totalStackedItemsAmount >= targetGroundStackManager.totalMaxAmount)
            {
                return;//资源框有上限并达到上限时不能丢
            }
            int stackIndex = itemStackManager.GetStackIndexByItemType(targetGroundStackManager.stackedItemType);
            int _count=1;//简单的动态调整丢物品物品速度
            if (itemStackManager.stackList[stackIndex].stackAmount>30)
            {
                _count=2;
            }
            if (itemStackManager.stackList[stackIndex].stackAmount > 50)
            {
                _count = 3;
            }
            for (int i = 0; i < _count; i++)
            {
                if (targetGroundStackManager.totalMaxAmount > 0 && targetGroundStackManager.totalStackedItemsAmount >= targetGroundStackManager.totalMaxAmount)
                {
                    break;//资源框有上限并达到上限时不能丢
                }
                var targetItem = itemStackManager.stackList[stackIndex].RemoveTopItem();
                if (targetItem != null)
                {
                    maxImg.gameObject.SetActive(false);
                    targetGroundStackManager.StackItem(targetItem);
                }
            }
        
            //   itemDropOffTimer = itemDropOffCooldown;
        }
    }

    

    // private void OnDrawGizmos()
    // {
    //     if (!isrun)
    //     {
    //         return;
    //     }
    //     Gizmos.color = Color.green;

    //     // 中心点（往后移动一单位）
    //     Vector3 center = transform.position - transform.forward;

    //     // 绘制扇形边缘
    //     float angleStep = (endAngle - startAngle) / 10f;
    //     Vector3 prevPoint = center;

    //     for (int i = 0; i <= 10f; i++)
    //     {
    //         float angle = startAngle + angleStep * i;
    //         // 将角度转为弧度
    //         float rad = angle * Mathf.Deg2Rad;

    //         // 本地坐标下的点
    //         Vector3 localPoint = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;
    //         // 转换到世界坐标（考虑物体旋转）
    //         Vector3 worldPoint = center + transform.rotation * localPoint;

    //         // 从中心连线到边缘
    //         Gizmos.DrawLine(center, worldPoint);

    //         // 连边缘点形成扇形弧线
    //         if (i > 0)
    //         {
    //             Gizmos.DrawLine(prevPoint, worldPoint);
    //         }

    //         prevPoint = worldPoint;
    //     }
    // }

    /// <summary>
    /// 攻击
    /// </summary>
    public void Attack()
    {
        Vector3 center = transform.position - transform.forward;
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayerMask);

        foreach (var hit in hits)
        {
            Vector3 dir = hit.transform.position - center;
            dir.y = 0; // 忽略高度差

            // 转换到玩家的本地坐标系
            Vector3 localDir = transform.InverseTransformDirection(dir);

            // 计算角度（相对于玩家前方）
            float angle = Mathf.Atan2(localDir.z, localDir.x) * Mathf.Rad2Deg;

            if (angle >= startAngle && angle <= endAngle)
            {
                //Debug.Log("攻击到敌人 " + hit.name);
                Enemy _enemy = hit.GetComponent<Enemy>();
                if (_enemy!=null)
                {
                    _enemy.SetHp(GameDataEditor.instance.playerAamage);
                }
               
            }
        }
    }

    /// <summary>
    /// 碰撞体碰到敌人时，检测前方是否有敌人，只攻击前方，不自动转身时使用
    /// </summary>
    public bool CheckIsAttack()
    {
        Vector3 center = transform.position - transform.forward;
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayerMask);
        foreach (var hit in hits)
        {
            Vector3 dir = hit.transform.position - center;
            dir.y = 0; // 忽略高度差

            // 转换到玩家的本地坐标系
            Vector3 localDir = transform.InverseTransformDirection(dir);
            // 计算角度（相对于玩家前方）
            float angle = Mathf.Atan2(localDir.z, localDir.x) * Mathf.Rad2Deg;

            if (angle >= startAngle && angle <= endAngle)
            {
              //  Debug.Log("前方有敌人，攻击");
               return true;
            }
        }
       // Debug.Log("前方没敌人");
        return false;
    }
}

