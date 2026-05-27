using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PurchaseZone_Clerk : PurchaseZone
{
    [Header("Clerk info")]
    public UnityEvent completeAction;
  
    protected override void OnPurchaseComplete()
    {
        base.OnPurchaseComplete();
        completeAction?.Invoke();
        cd.enabled = false;
        DisableGameObject(3);
        ClerkManager.instance?.Check();
        AudioManager.instance.Play(SK.解锁_建筑);
    }
}
