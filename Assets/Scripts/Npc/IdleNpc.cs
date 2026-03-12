using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
/// <summary>
/// 站桩npc
/// </summary>
public class IdleNpc : Npc
{
    public Transform target;

    public UnityEvent targetAction;
    void Start()
    {
        mAnimator.Play("Run");
        MoveToTarget(target.position,()=>{ targetAction?.Invoke();StopMovement(); mAnimator.Play("idle");transform.forward=target.forward;});
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
