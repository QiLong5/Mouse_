using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Processor : MonoBehaviour
{
    [Header("原材料信息")]
    [SerializeField] public GroundItemStackManager rawMaterialStackManager;

    [Header("产品信息")]
    [SerializeField] private ItemType itemType;  //产品的类型
    [SerializeField] public GroundItemStackManager productStackManager;

    [Header("加工信息")]
    [SerializeField] private UIProgress progressBar; //如果需要在加工机器上方显示进度条才用这个
    [SerializeField] public Transform processPosition; //加工位置，原材料会移动到这个位置然后被摧毁，然后产品会生成在这个位置
    [SerializeField] private float itemSpawnInterval => GameDataEditor.instance.itemSpawnInterval; //生成每个物品的间隔时间

   
    private int itemsPerCustomer => GameDataEditor.instance.customerNeedCount; //每个顾客需要交付的原材料数量
    private int productsPerTransaction => GameDataEditor.instance.customerGiveGoin; //完成交易后一次性给的产品（金币）数量

    public bool ispprocess;
    private bool isProcessing = false; //是否正在处理中

    private WaitForSeconds itemWaitTime;
    private readonly WaitForSeconds productWaitTime = new WaitForSeconds(0.8f);
    private readonly WaitForSeconds customerWaitTime = new WaitForSeconds(0.6f);
    private readonly WaitForSeconds productSpawnWaitTime = new WaitForSeconds(0.02f); //产品生成间隔
    private readonly WaitForSeconds materialDestroyWaitTime = new WaitForSeconds(0.2f); //原材料销毁等待时间

    private void Start()
    {
        itemWaitTime = new WaitForSeconds(itemSpawnInterval);
    }

    private void Update()
    {
        if (ispprocess && !isProcessing)
        {
            if (rawMaterialStackManager.totalStackedItemsAmount > 0)
            {
                StartCoroutine(ProcessingCoroutine());
            }
        }
    }

    /// <summary>
    /// 物体进栈时调用更新进度
    /// </summary>
    public void StackitemProgress()
    {
       // float progress = (float)rawMaterialStackManager.totalStackedItemsAmount / (float)GameDataEditor.instance.materialCount_Rat;
        // if (rawMaterialStackManager.totalStackedItemsAmount == 1)
        // {
        //     progressBar.gameObject.SetActive(true);
        //     progressBar.SetFill(progress, false);
        // }
        // else
        // {
        //     progressBar.SetFill(progress);
        // }

    }
    // 主协程：处理整个生产流程
    private IEnumerator ProcessingCoroutine()
    {
        isProcessing = true;

        // 显示进度条
        progressBar.SetFill(0);
        progressBar.gameObject.SetActive(true);

        // 生产指定数量的物品
        for (int i = 0; i < itemsPerCustomer; i++)
        {
            // 等待直到有原材料可用
            while (rawMaterialStackManager.totalStackedItemsAmount <= 0)
            {
                yield return itemWaitTime; 
            }

            // 更新进度条
            float progress = (float)(i + 1) / itemsPerCustomer;
            progressBar.SetFill(progress);

            // 处理一个原材料
            ProcessMaterial();

            // 等待生成间隔
            yield return itemWaitTime;
        }

        // 隐藏进度条
        progressBar.Hide();

        // 等待最后一个原材料处理完成
        yield return productWaitTime;

        // 批量生成产品（金币）
        StartCoroutine(SpawnProducts());

        // 等待产品全部生成
        yield return productWaitTime;

        // 让顾客离开
        NpcManager.instance.DequeueCustomer();

        // 等待UI消失和顾客离开，以及下一个顾客停止移动
        yield return customerWaitTime;

        isProcessing = false;
    }


    protected virtual void OnTriggerEnter(Collider other)
    {

        if (other.tag.Equals("Player"))
        {
            //ispprocess = true;
        }
       
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            //ispprocess = true;
        }
           
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            //ispprocess = false;
        }
       
    }
    //加工原材料（只处理原材料，不生成产品）
    public void ProcessMaterial()
    {
        if (rawMaterialStackManager.totalStackedItemsAmount > 0)
        {
            Item rawMaterial = rawMaterialStackManager.RemoveItem();
            if (rawMaterial != null)
            {
                rawMaterial.gameObject.SetActive(true);
                rawMaterial.transform.parent = transform;
                rawMaterial.MoveAlongCurve(rawMaterial.transform.localPosition, processPosition.localPosition,
                    ()=>{
                         StartCoroutine(DestroyRawMaterial(rawMaterial)); });
            }
        }
    }


    //只摧毁原材料
    IEnumerator DestroyRawMaterial(Item _rawMaterial)
    {
        yield return materialDestroyWaitTime; //让动效跳完再销毁
        PoolManager.instance.ReturnItem(_rawMaterial);
    }

    //批量生成产品（金币）
    IEnumerator SpawnProducts()
    {
        for (int i = 0; i < productsPerTransaction; i++)
        {
            var product = PoolManager.instance.GetItem(itemType);
            if (product != null)
            {
                product.transform.position = transform.position;
                product.transform.parent = productStackManager.transform;
                product.cd.enabled = false;
                productStackManager.StackItem(product);
            }
            yield return productSpawnWaitTime; //每个产品之间稍微间隔一下，让堆叠动画更流畅
        }
    }



}
