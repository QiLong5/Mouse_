using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStackManager : MonoBehaviour
{
    [Header("堆栈管理")]
    [Tooltip("需要管理的物品堆列表")]
    public List<ItemStack> stackList; //需要管理的物品堆列表

    [Header("自动位置配置")]
    [Tooltip("第一个堆栈的起始位置（本地空间）")]
    public Vector3 startPosition = Vector3.zero;

    [Tooltip("间距方向（默认向后，即负Z方向）")]
    public Vector3 spacingDirection = Vector3.back;

    //目前有被堆放物品的物品堆数量
    public int amountOfStackInUse = 0;

    private Player player;

    private void Start()
    {
        player = Player.instance;
    }

    /// <summary>
    /// 根据堆栈在激活列表中的排名获取位置
    /// 第一个激活的堆栈在 startPosition + 自己宽度的一半
    /// 之后的堆栈位置 = 前面所有堆栈的宽度累加 + 自己宽度的一半
    /// 这样可以确保每个堆栈都完整显示，不会被前面的堆栈遮挡
    /// </summary>
    public Vector3 GetStackPositionByRank(int rank)
    {
        // 累加前面所有激活堆栈的物品宽度
        float accumulatedWidth = 0f;
        int currentRank = 0;
        ItemStack targetStack = null;

        foreach (var stack in stackList)
        {
            // 只有有物品的堆栈才占用排名
            if (stack.stackAmount > 0)
            {
                if (currentRank < rank)
                {
                    accumulatedWidth += stack.GetItemWidth();
                    currentRank++;
                }
                else if (currentRank == rank)
                {
                    targetStack = stack;
                    break;
                }
            }
        }

        // 如果找到了目标堆栈，加上自己宽度的一半
        float halfWidth = targetStack != null ? targetStack.GetItemWidth() * 0.5f : 0f;

        return startPosition + spacingDirection * (accumulatedWidth + halfWidth);
    }

    /// <summary>
    /// 根据物品类型获取对应的堆栈索引
    /// 自动遍历 stackList，找到 stackedItemType 匹配的堆栈
    /// </summary>
    public int GetStackIndexByItemType(ItemType type)
    {
        for (int i = 0; i < stackList.Count; i++)
        {
            if (stackList[i].GetStackedItemType() == type)
            {
                return i;
            }
        }

        Debug.LogWarning($"未找到物品类型 {type} 对应的堆栈，使用默认堆栈索引0");
        return 0;
    }

    /// <summary>
    /// 根据物品类型获取对应的 ItemStack
    /// </summary>
    public ItemStack GetStackByItemType(ItemType type)
    {
        int stackIndex = GetStackIndexByItemType(type);

        if (stackIndex >= 0 && stackIndex < stackList.Count)
        {
            return stackList[stackIndex];
        }

        Debug.LogWarning($"物品类型 {type} 对应的堆栈索引 {stackIndex} 超出范围，返回null");
        return null;
    }

    public void ModifyAmountOfStackInUse(int _value)
    {
        amountOfStackInUse += _value;
        amountOfStackInUse = Mathf.Clamp(amountOfStackInUse, 0, stackList.Count);
    }

}
