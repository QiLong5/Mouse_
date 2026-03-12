using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GroundItemStackManager : MonoBehaviour
{
    //存放的物品类型
    public ItemType stackedItemType; 

    public List<GroundItemStack> stackList;

    //目标物品堆在list中的index，用于控制下一个物品会被堆放在哪个物品堆中，也用于移除物品时会从哪个物品堆移除
    public int targetStackListIndex { get;protected set; } = 0;  
    
    public int totalStackedItemsAmount= 0;
    public int totalMaxAmount = 0;
    [SerializeField] protected SpriteRenderer nomalSR;
    [SerializeField] protected SpriteRenderer heighLightSR;
    public UnityEvent stackItemAcion;

    protected virtual void Update()
    {
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            ShowBlueSprite();
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {

        if (other.tag.Equals("Player"))
        {
            ShowBlueSprite();
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            ShowWhiteSprite();
        }
    }

    /// <summary>
    /// 堆放物品至地面物品堆中（具体是到哪个地面物品堆会由GroundItemStackManager自行管理）
    /// </summary>
    /// <param name="_item"></param>
    public virtual void StackItem(Item _item)
    {
        stackList[targetStackListIndex].StackItem(_item);
        ModifyTargetStackListIndex(1);
        totalStackedItemsAmount++;
        stackItemAcion?.Invoke();
    }
   
    /// <summary>
    /// 从地面物品堆中移除物品，并返回被移除的物品
    /// </summary>
    /// <returns></returns>
    public Item RemoveItem()
    {
        //防止刚堆叠了新物品之后往后移的index指向空的堆，所以应该在移除的时候先把index-1
        ModifyTargetStackListIndex(-1);
        if (stackList[targetStackListIndex].stackAmount<=0)
        {
            for (int i = 0; i < stackList.Count; i++)
            {
                ModifyTargetStackListIndex(-1);
                if (stackList[targetStackListIndex].stackAmount>0)
                {
                    break;
                }
            }
        }
      
        Item itemToRemove = stackList[targetStackListIndex].RemoveTopItem();
        if(itemToRemove!=null)
        {
            totalStackedItemsAmount--;
        }
        return itemToRemove;
    }
    public bool Test;
    protected virtual void ModifyTargetStackListIndex(int _value)
    {
        targetStackListIndex += _value;
        if (targetStackListIndex >= stackList.Count)
        {
            targetStackListIndex = 0;
        }

        if (targetStackListIndex < 0)
        {
            targetStackListIndex = stackList.Count - 1;
        }
    }

    public void ShowBlueSprite()
    {
        if (nomalSR == null || heighLightSR == null)
        {
            return;
        }
        nomalSR.gameObject.SetActive(false);
        heighLightSR.gameObject.SetActive(true);
    }

    public void ShowWhiteSprite()
    {
        if (nomalSR == null || heighLightSR == null)
        {
            return;
        }
        nomalSR.gameObject.SetActive(true);
        heighLightSR.gameObject.SetActive(false);
    }

}
