using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStack : MonoBehaviour
{
    [SerializeField] private ItemType stackedItemType;
    public int maxStackAmount = 16;  //这个物品堆最多能堆多少个物品

    /// <summary>
    /// 获取该堆栈对应的物品类型
    /// </summary>
    public ItemType GetStackedItemType()
    {
        return stackedItemType;
    }

    /// <summary>
    /// 获取该堆栈物品的宽度
    /// 如果堆栈中有物品，返回第一个物品的宽度
    /// 如果没有物品，返回默认宽度
    /// </summary>
    public float GetItemWidth()
    {
        if (stackedItemList != null && stackedItemList.Count > 0 && stackedItemList[0] != null)
        {
            return stackedItemList[0].itemWidth;
        }

        // 返回默认宽度
        return 0.25f;
    }

    [SerializeField]
    public int maxHeight =30;

    public int stackAmount { get; set; } = 0;  //当前堆叠的物品数量
    public Vector3 nextStackPosition { get; set; }  //下一个物品的堆叠位置，注意这里用的是本地空间的位置

    private ItemStackManager stackManager;
    protected List<Item> stackedItemList;
    [Header("弯曲效果设置")]
    [SerializeField] bool isOpenBend = false;
    [SerializeField] private float bendDistance = 0.1f; // 每个物品向后弯曲的距离
    [SerializeField] private float bendDuration = 0.5f; // 弯曲动画持续时间


    //用于存储物品弯曲前的状态，以便恢复
    private List<Vector3> originalLocalPositions = new List<Vector3>();
    public  bool iscomplet_bend=false;//是否处于弯曲
    public bool iscomplet_origina = true;//是否处于笔直
    protected virtual void Start()
    {
        stackManager = GetComponentInParent<ItemStackManager>();
        stackedItemList = new List<Item>();

        nextStackPosition = Vector3.zero;
    }

    protected virtual void Update()
    {
        AdjustPosition();
        if (isOpenBend)
        {
            if (Player.instance.IsMove)
            {
                BendItems(); //弯曲物品
            }
            else
            {
                RestoreItems();//恢复笔直
            }
        }
       
    }

    //调整物品堆在玩家背后的位置
    private void AdjustPosition()
    {
        if (stackManager == null) return;

        // 如果没有堆栈在使用，返回起始位置
        if (stackManager.amountOfStackInUse <= 0)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, stackManager.startPosition, 5f * Time.deltaTime);
            return;
        }

        // 计算当前堆栈在激活堆栈中的排名
        int rank = GetRankInActiveStacks();
        if (rank < 0)
        {
            // 如果没有找到排名（当前堆栈为空），保持当前位置
            return;
        }

        // 根据排名获取目标位置
        Vector3 targetPosition = stackManager.GetStackPositionByRank(rank);

        // 平滑移动到目标位置
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, 5f * Time.deltaTime);
    }

    /// <summary>
    /// 获取当前堆栈在所有激活堆栈中的排名（从0开始）
    /// 激活的堆栈按照在 stackList 中的顺序排序
    /// </summary>
    private int GetRankInActiveStacks()
    {
        if (stackAmount <= 0) return -1;

        int rank = 0;
        foreach (var stack in stackManager.stackList)
        {
            if (stack == this)
            {
                return rank;
            }

            // 只有有物品的堆栈才占用排名
            if (stack.stackAmount > 0)
            {
                rank++;
            }
        }

        return -1;
    }

    /// <summary>
    /// 堆叠物品
    /// </summary>
    /// <param name="_item"></param>
    public virtual void StackItem(Item _item)
    {
        if (_item.canDoFurtherMove == false)
        {
            return;
           // _item.StopAllCoroutines();
        }
        if (stackAmount >= maxStackAmount)
        {
            Player.instance.maxImg.gameObject.SetActive(true);
            return;
        }
       
        _item.cd.enabled=false;
        _item.transform.parent = transform;
        _item.hasBeenAddedToPlayer = true;



        if (iscomplet_bend)//处于弯曲状态时飞到弯曲的位置
        {
            int newItemIndex = stackedItemList.Count - 1; 
            float linearLayerRatio = newItemIndex >= maxHeight   //限定最多物品高度，超出的直接按照第maxHeight个物品的位置，飞到后隐藏掉
                ? 1f
                : (float)newItemIndex / (maxHeight - 1);
            float curveRatio = Mathf.Pow(linearLayerRatio, 2f); 
            Vector3 newItemBentPos = nextStackPosition + Vector3.back * bendDistance * curveRatio;
            _item.gameObject.SetActive(true);
            if (stackAmount <= maxHeight)
            {
                _item.MoveAlongCurve(_item.transform.localPosition, newItemBentPos);
            }
            else
            {
                _item.MoveAlongCurve(_item.transform.localPosition, newItemBentPos,()=> { _item.gameObject.SetActive(false); });
            }
            
        }
        else
        {
            if (stackAmount <= maxHeight)
            {
                _item.MoveAlongCurve(_item.transform.localPosition, nextStackPosition);
            }
            else
            {
                _item.MoveAlongCurve(_item.transform.localPosition, nextStackPosition, () => { _item.gameObject.SetActive(false); });
            }
                
        }
        stackAmount++;
        if (stackAmount >= maxStackAmount)
        {
            Player.instance.maxImg.gameObject.SetActive(true);
        }

        if (stackAmount >= maxHeight)
        {
            nextStackPosition = new Vector3(0, _item.stackHeight * maxHeight, 0);
        }
        else
        {
            nextStackPosition = new Vector3(0, _item.stackHeight * stackAmount, 0);
        }
        stackedItemList.Add(_item);
        originalLocalPositions.Add(nextStackPosition); 
        if (stackAmount == 1)
        {
            stackManager.ModifyAmountOfStackInUse(1);
        }

        Player.instance?.MoneyAmountChange(_item.value);
    }

 
    //取出顶部的物品
    public virtual Item RemoveTopItem()
    {
        if (stackAmount <= 0)
        {
            return null;
        }

        Item itemToRemove = stackedItemList[stackedItemList.Count - 1];

        if (itemToRemove.canDoFurtherMove == false)
        {
            itemToRemove.StopAllCoroutines();
            itemToRemove.canDoFurtherMove = true;
            //return null;
        }
        if (isOpenBend)
        {
            if (stackAmount >= maxHeight)
            {
                if (Player.instance.IsMove)
                {
                    itemToRemove.transform.localPosition = new Vector3(0, itemToRemove.stackHeight * maxHeight, 0) + Vector3.back * bendDistance; 
                }
                else
                {
                    itemToRemove.transform.localPosition = new Vector3(0, itemToRemove.stackHeight * maxHeight, 0);
                }
            }
               
        }
        stackAmount--;
        if (stackAmount >= maxHeight)
        {
            nextStackPosition = new Vector3(0, itemToRemove.stackHeight * maxHeight, 0);
        }
        else
        {
            nextStackPosition = new Vector3(0, itemToRemove.stackHeight * stackAmount, 0);
        }
        stackedItemList.Remove(itemToRemove);
        originalLocalPositions.RemoveAt(originalLocalPositions.Count - 1);
        itemToRemove.transform.parent = null;

        //如果这个物品堆移除了该物品后已经没有堆放物体了，那么manager中正在被使用的物品堆数量-1
        if (stackAmount == 0)
        {
            stackManager.ModifyAmountOfStackInUse(-1);
        }

        //金钱数量减去物品的价值
        Player.instance?.MoneyAmountChange(-itemToRemove.value);
        return itemToRemove;
    }



    #region 核心弯曲实现

    public void BendItems()
    {
        if (stackedItemList.Count <= 0 || iscomplet_bend) return;
        StopAllCoroutines();
        StartCoroutine(BendItemsCoroutine());
    }

    /// <summary>
    /// 弯曲协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator BendItemsCoroutine()
    {
        float elapsedTime = 0f;
        iscomplet_bend = true;//设置弯曲状态
        iscomplet_origina = false;
        while (elapsedTime < bendDuration)
        {
     
            float t = elapsedTime / bendDuration;

            for (int i = 0; i < stackedItemList.Count; i++)
            {
                Item item = stackedItemList[i];
                if (item == null) continue;
                if (!item.canDoFurtherMove) continue;
                // 计算当前物品的“最终曲线偏移位置”
                float linearLayerRatio = i >=maxHeight ? 1f : (float)i / (maxHeight - 1);
                float curveRatio = Mathf.Pow(linearLayerRatio, 2f); // 二次曲线比例
                Vector3 finalBentPosition = originalLocalPositions[i] + Vector3.back * bendDistance * curveRatio;

                // 动画插值：从原始位置 平滑过渡到 最终曲线位置
                // 通过 t 控制过渡进度
                item.transform.localPosition = Vector3.Lerp(
                    originalLocalPositions[i],  // 动画起点：原始位置
                    finalBentPosition,          // 动画终点：曲线位置
                    t                           // 过渡进度
                );
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        } 
    }
    public void RestoreItems()
    {
        if (stackedItemList.Count == 0 || iscomplet_origina) return;
        StopAllCoroutines();
        StartCoroutine(RestoreItemsCoroutine());
    }

    private IEnumerator RestoreItemsCoroutine()
    {
        float elapsedTime = 0f;
        iscomplet_bend = false;
        iscomplet_origina = true;
        while (elapsedTime < bendDuration)
        {
            float t = elapsedTime / bendDuration; 

            for (int i = 0; i < stackedItemList.Count; i++)
            {
                Item item = stackedItemList[i];
                if (item == null) continue;
                if (!item.canDoFurtherMove) continue;
                Vector3 currentPosition = item.transform.localPosition;

                //平滑过渡到原始位置
                item.transform.localPosition = Vector3.Lerp(
                    currentPosition,
                    originalLocalPositions[i],
                    t
                );
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < stackedItemList.Count; i++)
        {
            Item item = stackedItemList[i];
            if (item == null) continue;
            if (!item.canDoFurtherMove) continue;
            item.transform.localPosition = originalLocalPositions[i];
        }  
    }

    #endregion
}
