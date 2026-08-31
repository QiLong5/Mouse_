using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NpcType : int
{
    Customer,//顾客
    Enemy,//普通敌人
}
public class Npc : MonoBehaviour
{
    [Header("组件引用")]
    public NpcType npcType;
    public Animator mAnimator;
    public AnimatorStateInfo mStateInfo;
    public Collider mCollider;

    public float mMoveSpeed;
    public float mTurnSmoothTime = 0.1f;
    float mTurnSmoothVelocity;

    protected virtual void Awake()
    {
        // 移动改为直接位移 transform，若预制体上还残留 Rigidbody，
        // 把它设为运动学并关闭重力，避免物理与位移互相干扰产生抖动
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    // 停止移动
    public virtual void StopMovement()
    {
        if (moveToTargerIE != null)
        {
            StopCoroutine(moveToTargerIE);
            moveToTargerIE = null;
        }
    }

    /// <summary>
    /// 动态跟随目标，如果是目标玩家的话，玩家在家就跳出移动，并调用跳出事件；
    /// </summary>
    /// <param name="target">目标</param>
    /// <param name="isplayer">玩家在家判断</param>
    /// <param name="breakAcion">跳出事件</param>
    public void MoveToTarget(Transform target, bool isplayer = false, Action breakAcion = null)
    {
        if (moveToTargerIE != null)
        {
            StopCoroutine(moveToTargerIE);
            moveToTargerIE = null;
        }
        moveToTargerIE = MoveToTargetIE(target, isplayer, breakAcion);
        StartCoroutine(moveToTargerIE);
    }
    public void MoveToTarget(Transform target, Action targetAcion = null)
    {
        if (moveToTargerIE != null)
        {
            StopCoroutine(moveToTargerIE);
            moveToTargerIE = null;
        }
        moveToTargerIE = MoveToTargetIE(target, targetAcion);
        StartCoroutine(moveToTargerIE);
    }
    public void MoveToTarget(Vector3 target, Action targetAcion = null)
    {
        if (moveToTargerIE != null)
        {
            StopCoroutine(moveToTargerIE);
            moveToTargerIE = null;
        }
        moveToTargerIE = MoveToTargetIE(target, targetAcion);
        StartCoroutine(moveToTargerIE);
    }

    public IEnumerator moveToTargerIE;
    IEnumerator MoveToTargetIE(Transform target, bool isplayer, Action breakAcion)
    {

        while (Vector3.Distance(target.position, transform.position) > 0.3f)
        {
            if (isplayer)
            {
                if (Player.instance.IsAtHome || Player.instance.isDie)
                {
                    breakAcion?.Invoke();
                    yield break;
                }

            }
            MoveStep(target.position);
            yield return null;
        }

    }
    /// <summary>
    /// 移动到指定位置，并移动完成后执行事件
    /// </summary>
    /// <param name="target"></param>
    /// <param name="targetAciton">执行的事件</param>
    /// <returns></returns>
    IEnumerator MoveToTargetIE(Vector3 target, Action targetAciton = null)
    {

        while (Vector3.Distance(target, transform.position) > 0.3f)
        {
            MoveStep(target);
            yield return null;
        }
        targetAciton?.Invoke();

    }
    IEnumerator MoveToTargetIE(Transform target, Action targetAciton = null)
    {

        while (Vector3.Distance(target.position, transform.position) > 0.3f)
        {
            MoveStep(target.position);
            yield return null;
        }
        targetAciton?.Invoke();

    }

    /// <summary>
    /// 单帧移动：朝目标平滑转向，并直接位移 transform（不依赖 Rigidbody）
    /// </summary>
    /// <param name="target">目标位置</param>
    protected void MoveStep(Vector3 target)
    {
        // 计算目标方向
        Vector3 dir = (target - transform.position).normalized;

        // 计算目标角度（绕 Y 轴）
        float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // 平滑旋转到目标角度
        float angle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            targetAngle,
            ref mTurnSmoothVelocity,
            mTurnSmoothTime
        );

        // 设置旋转
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        // 直接位移（保持 Y 轴高度不变）
        transform.position += transform.forward * mMoveSpeed * Time.deltaTime;
    }
}
