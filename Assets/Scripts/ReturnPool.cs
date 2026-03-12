using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池按时间回收脚本
/// </summary>
public class ReturnPool : MonoBehaviour
{
    public int mId;

    //public Npc mNpc;

    public float mTime;
    public float mTimeMax;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //mTime += Time.deltaTime;
        //if (mTime >= mTimeMax)
        //{
        //    mTime = 0;
        //    PoolReturn();
        //}
    }

    //public void PoolReturn()
    //{
    //    this.gameObject.SetActive(false);
    //    switch (mId)
    //    {
    //        case 0:
    //            PoolManager.instance.ReturnBullet(mNpc);
    //            break;           
    //        default:
    //            break;
    //    }
    //}
}
