using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

[Serializable]
public class GameObjectEvent : UnityEvent<GameObject> { }

public class ConveyorController : MonoBehaviour
{
    [Header("传送带位置设置")]
    [SerializeField] private Transform spawnPosition;      // 物品出现位置
    [SerializeField] private Transform despawnPosition;    // 物品消失位置

    [Header("传送带参数")]
    [SerializeField] private float moveSpeed = 2f;         // 物品传送速度
    [SerializeField] private float spawnInterval = 2f;     // 物品生成间隔（秒）
    [SerializeField] private GameObject itemPrefab;        // 物品预制体

    [Header("对象池设置")]
    [SerializeField] private int poolSize = 10;            // 对象池初始大小

    [Header("运行状态")]
    [SerializeField] private bool autoStart = true;        // 自动开始

    [Header("回调事件")]
    public GameObjectEvent OnItemSpawned = new GameObjectEvent();          // 物品生成回调
    public GameObjectEvent OnItemReached = new GameObjectEvent();          // 物品抵达回调

    public GroundItemStackManager start_groundItemStackManager;
    public GroundItemStackManager end_groundItemStackManager;

    public Queue<Item> items= new Queue<Item>();
    // 私有变量
    private bool isRunning = false;
    private bool isPaused = false;
    private Coroutine spawnCoroutine;
    private readonly List<GameObject> activeItems = new List<GameObject>();
    private readonly Dictionary<GameObject, Tweener> itemTweens = new Dictionary<GameObject, Tweener>();  // 存储物品的动画
    private readonly Queue<GameObject> itemPool = new Queue<GameObject>();  // 对象池
    public Transform poolContainer;                       // 对象池容器

    // 用于保存暂停时的计时状态
    private float spawnTimer = 0f;                        // 当前生成计时器

    private void Start()
    {
        InitializePool();

        if (autoStart)
        {
            StartConveyor();
        }
    }
    void FixedUpdate()
    {
        // if (end_groundItemStackManager.totalStackedItemsAmount>= end_groundItemStackManager.totalMaxAmount)//看end_groundItemStackManager满了吗，满了暂停传送
        // {
        //     PauseConveyor();
        // }
        // else
        // {
        //     ResumeConveyor();
        // }
        if (start_groundItemStackManager.totalStackedItemsAmount >0)//有了就传
        {
            ResumeConveyor();
            
        }
        else if(itemPool.Count==0)
        {
            PauseConveyor();
        }
    }
    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePool()
    {
        if (itemPrefab == null) return;

        // 预先创建对象
        for (int i = 0; i < poolSize; i++)
        {
            GameObject item = Instantiate(itemPrefab, poolContainer);
            item.SetActive(false);
            itemPool.Enqueue(item);
        }
    }

    /// <summary>
    /// 从对象池获取物品
    /// </summary>
    private GameObject GetFromPool()
    {
        GameObject item;

        if (itemPool.Count > 0)
        {
            item = itemPool.Dequeue();
        }
        else
        {
            // 池中没有可用对象，创建新的
            item = Instantiate(itemPrefab, poolContainer);
        }

        item.SetActive(true);
        return item;
    }

    /// <summary>
    /// 将物品返回对象池
    /// </summary>
    private void ReturnToPool(GameObject item)
    {
        // 清理DOTween动画
        if (itemTweens.TryGetValue(item, out Tweener tween))
        {
            tween.Kill();
            itemTweens.Remove(item);
        }

        item.SetActive(false);
        item.transform.SetParent(poolContainer);
        itemPool.Enqueue(item);
    }

    /// <summary>
    /// 开始传送带
    /// </summary>
    public void StartConveyor()
    {
        if (isRunning) return;
      //  Debug.Log("启动了");
        isRunning = true;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        spawnCoroutine = StartCoroutine(SpawnItemRoutine());
    }

    /// <summary>
    /// 暂停传送带
    /// </summary>
    public void PauseConveyor()
    {
        if (!isRunning || isPaused) return;
       // Debug.Log("暂停了");
        isPaused = true;

        // 暂停所有物品的DOTween动画
        foreach (var kvp in itemTweens)
        {
            if (kvp.Value != null && kvp.Value.IsActive())
            {
                kvp.Value.Pause();
            }
        }

        // 不停止协程，保留计时器状态
    }

    /// <summary>
    /// 恢复传送带
    /// </summary>
    public void ResumeConveyor()
    {
        if (!isRunning || !isPaused) return;
       // Debug.Log("恢复运行");
        isPaused = false;

        // 恢复所有物品的DOTween动画
        foreach (var kvp in itemTweens)
        {
            if (kvp.Value != null && kvp.Value.IsActive())
            {
                kvp.Value.Play();
            }
        }

        // 协程会自动恢复计时
    }


    /// <summary>
    /// 生成一个物品
    /// </summary>
    public GameObject SpawnItem()
    {
        GameObject newItem = GetFromPool();
        newItem.transform.SetPositionAndRotation(spawnPosition.position, spawnPosition.rotation);
        activeItems.Add(newItem);
        // 计算移动时间
        float distance = Vector3.Distance(spawnPosition.position, despawnPosition.position);
        float duration = distance / moveSpeed;

        // 使用DOTween移动物品
        Tweener tween = newItem.transform.DOMove(despawnPosition.position, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => OnItemReachedDestination(newItem));

        itemTweens[newItem] = tween;
        // 触发生成回调
        OnItemSpawned?.Invoke(newItem);

        var _item= start_groundItemStackManager.RemoveItem();
        if (_item != null)
        {
            items.Enqueue(_item);
            _item.gameObject.SetActive(false);
        }
        return newItem;
    }

    /// <summary>
    /// 物品到达终点回调
    /// </summary>
    private void OnItemReachedDestination(GameObject item)
    {
        if (item == null || !activeItems.Contains(item)) return;

        // 触发抵达回调
        OnItemReached?.Invoke(item);
        // 返回对象池
        activeItems.Remove(item);

        var _item = items.Dequeue();
        if (_item != null)
        {
            _item.gameObject.SetActive(true);
            _item.transform.position= despawnPosition.position;
            end_groundItemStackManager.StackItem(_item);
            
        }
        ReturnToPool(item);
    }

    /// <summary>
    /// 物品生成协程
    /// </summary>
    private IEnumerator SpawnItemRoutine()
    {
        spawnTimer = 0f;  // 初始化计时器

        while (isRunning)
        {
            // 等待起始点有物品
            while (start_groundItemStackManager.totalStackedItemsAmount <= 0)
            {
                spawnTimer = 0f;  // 重置计时器
                yield return null;
            }

            // 如果未暂停，累加计时器
            if (!isPaused)
            {
                spawnTimer += Time.deltaTime;

                // 当计时器达到生成间隔时，生成物品
                if (spawnTimer >= spawnInterval)
                {
                    SpawnItem();
                    spawnTimer = 0f;  // 重置计时器
                }
            }
            // 如果暂停，计时器保持不变

            yield return null; 
        }
    }
}
