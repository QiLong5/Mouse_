using System.Collections.Generic;
using UnityEngine;

public class GroundItemStack : ItemStack
{
    protected override void Start()
    {
        stackedItemList = new List<Item>();

        nextStackPosition = Vector3.zero;
    }

    protected override void Update()
    {
    }

    //堆叠物品
    public override void StackItem(Item _item)
    {
        _item.transform.parent = transform;
        _item.gameObject.SetActive(true);
        if (stackAmount >= maxHeight)
        {
            _item.MoveAlongCurve(_item.transform.localPosition, nextStackPosition,()=> { _item.gameObject.SetActive(false);});
        }
        else
        {
            _item.MoveAlongCurve(_item.transform.localPosition, nextStackPosition);
        }
    
        stackAmount++;
        if (stackAmount >= maxHeight)
        {
            nextStackPosition = new Vector3(0, _item.stackHeight * maxHeight, 0);
        }
        else
        {
            nextStackPosition = new Vector3(0, _item.stackHeight * stackAmount, 0);
        }
      
        stackedItemList.Add(_item);
    }


    //移除最上面的物品
    public override Item RemoveTopItem()
    {
        if (stackAmount <= 0)
        {
            return null;
        }

        Item itemToRemove = stackedItemList[stackedItemList.Count - 1];
        if(!itemToRemove.canDoFurtherMove)
        {
            itemToRemove.StopAllCoroutines();
            itemToRemove.canDoFurtherMove=true;
          // return null;
        }
        stackAmount--;
        if (stackAmount >= maxHeight)
        {
            nextStackPosition =new Vector3(0, itemToRemove.stackHeight * maxHeight, 0);
        }
        else
        {
            nextStackPosition =new Vector3(0, itemToRemove.stackHeight * stackAmount, 0);
        }
       
        stackedItemList.Remove(itemToRemove);
        itemToRemove.gameObject.SetActive(true);
        return itemToRemove;
    }

}
