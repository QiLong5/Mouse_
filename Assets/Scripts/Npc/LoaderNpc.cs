using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoaderNpc : Npc
{
    [Header("路径设置")]
    public List<Transform> movepath;

    [Header("物品管理器")]
    public ItemStackManager itemStackManager;
    public GroundItemStackManager sourceGroundStackManager;
    public GroundItemStackManager targetGroundStackManager;

    [Header("搬运设置")]
    public int loadCapacity = 3;
    public float loadUnloadDelay = 0.2f;
    public bool isWorking = false;

    // 缓存以减少GC分配
    private WaitForSeconds loadDelay;
    private WaitUntil waitForItems;
    private WaitForEndOfFrame waitFrame;

    void Start()
    {
        // 初始化缓存对象
        loadDelay = new WaitForSeconds(loadUnloadDelay);
        waitForItems = new WaitUntil(HasEnoughItems);
        waitFrame = new WaitForEndOfFrame();

        if (isWorking)
        {
            StartCoroutine(WorkCycle());
        }
    }

    private bool HasEnoughItems()
    {
        return sourceGroundStackManager.totalStackedItemsAmount >= loadCapacity;
    }

    private IEnumerator WorkCycle()
    {
        ItemStack npcStack = itemStackManager.stackList[0];

        yield return MoveToPointCoroutine(movepath[0].position);//跑到搬运点

        while (isWorking)
        {
            mAnimator.Play("Idle");
            // 1. 等待A点有足够物品
            yield return waitForItems;
            mAnimator.Play("Idle1");
            // 2. 装载物品
            int loaded = 0;
            while (loaded < loadCapacity && sourceGroundStackManager.totalStackedItemsAmount > 0)
            {
                Item item = sourceGroundStackManager.RemoveItem();
                if (item != null)
                {
                    npcStack.StackItem(item);
                    loaded++;
                    yield return loadDelay;
                }
            }
            mAnimator.Play("Run1");
            // 3. 前往B点
            int pathCount = movepath.Count;
            for (int i = 0; i < pathCount; i++)
            {
                yield return MoveToPointCoroutine(movepath[i].position);
            }
            mAnimator.Play("Idle1");
            // 4. 卸载物品
            while (npcStack.stackAmount > 0)
            {
                Item item = npcStack.RemoveTopItem();
                if (item != null)
                {
                    targetGroundStackManager.StackItem(item);
                    yield return loadDelay;
                }
            }
            mAnimator.Play("Run");
            // 5. 返回A点
            for (int i = pathCount - 1; i >= 0; i--)
            {
                yield return MoveToPointCoroutine(movepath[i].position);
            }
        }
    }

    private IEnumerator MoveToPointCoroutine(Vector3 targetPos)
    {
        bool reached = false;
      
        MoveToTarget(targetPos, () => reached = true);

        while (!reached)
        {
            yield return waitFrame;
        }
        mRigidbody.velocity = Vector3.zero;
    }

    public void StartWork()
    {
        if (!isWorking)
        {
            isWorking = true;
            StartCoroutine(WorkCycle());
        }
    }

    public void StopWork()
    {
        isWorking = false;
        StopAllCoroutines();
        mAnimator.Play("idle");
        StopMovement();
    }
}

