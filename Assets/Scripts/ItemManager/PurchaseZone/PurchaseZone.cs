using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class PurchaseZone : MonoBehaviour
{
    [Header("Purchase info / 购买信息")]
    public int id;
    public int price; //总共需要的价格
    public int remainingPrice { get; protected set; } //剩余还需要付的钱
    public ItemType requiredItemType;  //需要的物品类型
    protected float itemDropOffCooldown;  //往购买区域丢物品的CD
    protected float itemDropOffTimer;  //与上面的cooldown联动的计时器


    [Space]
    [Header("Purchase progress info / 购买进度信息")]
    [SerializeField] protected Image fillImage; //用于填充的图片（绿色背景）
    [SerializeField] private List<Image> remainingPrice_Img; //显示还需要的金钱数量
    protected float purchaseProgress;

    public bool iscomplete = false;
    
    [Header("Sprite info / 地面ui框信息")]
    [SerializeField] private Image borderNomal;
    [SerializeField] private Image contentHeighLight;
    [SerializeField] private Image borderNomoney;



    public bool hasCompletedPurchase { get; protected set; } = false;
    public bool isPurchasing { get; protected set; } = false;
    protected Vector3 itemMoveTargetPosition = new Vector3(0, -1, 0);
    protected ItemStackManager playerStackManager;

    protected ItemStack cachedTargetStack;

    protected Canvas canvas => GetComponentInChildren<Canvas>();
    protected Collider cd => GetComponent<Collider>();
    public UnityEvent mPurchase;
    public bool isCanBreath;
    bool isbreath;
    bool isfinshPurchase=true;
    Vector3 mlocalescale;
    protected virtual void Start()
    {
        remainingPrice = price;

        itemDropOffCooldown = Player.instance.itemDropOffCooldown;
        itemDropOffTimer = -1;

        playerStackManager = Player.instance.itemStackManager;

        // 初始化时缓存所需物品类型对应的堆栈
        cachedTargetStack = playerStackManager.GetStackByItemType(requiredItemType);

        purchaseProgress = 0;

        UIManager.instance.SetNum(remainingPrice_Img, remainingPrice);
        mlocalescale = canvas.transform.localScale;
        StartBreath();
    }

    protected virtual void Update()
    {
        if (!hasCompletedPurchase && isPurchasing)
        {
            Purchase();
        }
    }

    public void InitPrice(int price)
    {
        this.price=price;
        remainingPrice = price;
    }


    protected virtual void OnTriggerEnter(Collider other)
    {
      //  Debug.Log("Targte: " + other.name);
        if (other.tag.Equals("Player") && !hasCompletedPurchase)
        {
            isPurchasing = true;
            ShowBlueSprite();
            StopBreath();
            NoMoneyShake();
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            isPurchasing = false;
            itemDropOffTimer = -1;

            ShowWhiteSprite();
         
            if (!isCanBreath)
            {
                canvas.transform.DOScale(mlocalescale, 0.2f);
            }
            else
            {

                StartBreath();
            }
            borderNomoney.gameObject.SetActive(false);
        }
    }


    protected virtual void Purchase()
    {
        itemDropOffTimer -= Time.deltaTime;

        if (itemDropOffTimer < 0)
        {
            // 使用缓存的堆栈
            if (cachedTargetStack == null)
            {
                Debug.LogWarning($"未找到物品类型 {requiredItemType} 对应的堆栈");
                return;
            }

            int _count = 1;
            if (remainingPrice>30 && cachedTargetStack.stackAmount>30)
            {
                _count=2;
            }
            if (remainingPrice >= 50 && cachedTargetStack.stackAmount > 50)
            {
                _count = 3;
            }
            for (int i = 0; i < _count; i++)
            {
                Item targetItem = null;
                targetItem = cachedTargetStack.RemoveTopItem();
                if (targetItem != null)
                {
                    targetItem.transform.parent = transform;
                    Player.instance.maxImg.gameObject.SetActive(false);
                    targetItem.gameObject.SetActive(true);
                    //增涨购买进度要放在物品移动之前，防止由于物品飞行时间过长，购买完成的时间不能够卡上物品飞行的进度导致多余的物品也飞过去
                    ProgressPurchase(targetItem);
                    targetItem.MoveAlongCurve(targetItem.transform.localPosition, itemMoveTargetPosition, () =>
                    {
                        if (isfinshPurchase)
                        {
                            if (!iscomplete)
                            {
                                isfinshPurchase = false;
                                canvas.transform.DOScale(mlocalescale * 1.2f, 0.05f).OnComplete
                                (() => { NoMoneyShake(); canvas.transform.DOScale(mlocalescale * 1.1f, 0.05f).OnComplete(() => { isfinshPurchase = true; }); });
                            }

                        }
                        PoolManager.instance.ReturnItem(targetItem);


                    });
                    mPurchase?.Invoke();

                }
            }

            itemDropOffTimer = itemDropOffCooldown;
        }
    }

    

    protected virtual void ProgressPurchase(Item _item)
    {
        if (requiredItemType == ItemType.Money)
        {
            remainingPrice -= _item.value;
        }
     
        int paidAmount = price - remainingPrice;
        purchaseProgress = (float)paidAmount / (float)price;
        fillImage.fillAmount = purchaseProgress;
        UIManager.instance.SetNum(remainingPrice_Img, remainingPrice);
        OnPurchaseProgress();

        if (remainingPrice <= 0)
        {
            hasCompletedPurchase = true;
            isPurchasing = false;

            OnPurchaseComplete();

        }
    }

    /// <summary>
    /// 当购买进度变化时会触发的函数，使用时写个子类override这个函数就行
    /// </summary>
    protected virtual void OnPurchaseProgress()
    {
        StopBreath();
    }

    /// <summary>
    /// 当购买完成时会触发的函数，使用时写个子类override这个函数就行
    /// </summary>
    protected virtual void OnPurchaseComplete()
    {
        if (iscomplete)
        {
            return;
        }
        iscomplete = true;
       
    }

    protected virtual void DisableGameObject(float _delay)
    {
        StartCoroutine(DisableGameObject_Coroutine(_delay));
    }

    public void NoMoneyShake()
    {
        // 检查当前 Zone 所需物品类型的堆栈是否为空
        if (cachedTargetStack != null && cachedTargetStack.stackAmount <= 0)
        {
            canvas.transform.DOKill();
            canvas.transform.localRotation = Quaternion.identity;
            if (isPurchasing)
            {
                borderNomoney.gameObject.SetActive(true);
            }
            canvas.transform.DOShakeRotation(0.5f, new Vector3(0, 0, 10));
            canvas.transform.DOScale(mlocalescale * 1.1f, 0.2f);
        }
    }
        

    protected IEnumerator DisableGameObject_Coroutine(float _delay)
    {
        cd.enabled = false;
        canvas.transform.DOKill();
        canvas.transform.DOScale(mlocalescale * 1.2f, 0.2f);
        yield return new WaitForSeconds(0.2f);
        canvas.transform.DOScale(mlocalescale * 0.5f, 0.2f);
        yield return new WaitForSeconds(0.2f);
        canvas.gameObject.SetActive(false);
        yield return new WaitForSeconds(_delay-0.6f);
        yield return null;
        gameObject.SetActive(false);
    }

   
        
    private void ShowBlueSprite()
    {
        if (borderNomal == null || contentHeighLight == null)
        {
            return;
        }

        borderNomal.gameObject.SetActive(false);
        contentHeighLight.gameObject.SetActive(true);
    }

    private void ShowWhiteSprite()
    {
        if (borderNomal == null || contentHeighLight == null)
        {
            return;
        }

        borderNomal.gameObject.SetActive(true);
        contentHeighLight.gameObject.SetActive(false);
    }

    IEnumerator breathIE;
    IEnumerator BreathIE()
    {

        while (isbreath)
        {
            canvas.transform.DOScale(mlocalescale * 1.1f, 1f);
            yield return new WaitForSeconds(1f);
            canvas.transform.DOScale(mlocalescale * 1f, 1f);
            yield return new WaitForSeconds(1f);
        }
    }

    public void StartBreath()
    {
        if(isCanBreath)
        {
            if (!isbreath)
            {
                isbreath = true;
                canvas.DOKill();
                breathIE = BreathIE();
                StartCoroutine(breathIE);
            }

        }

    }
    public void StopBreath()
    {
        if (isbreath)
        {
            isbreath = false;
            StopCoroutine(breathIE);
            canvas.transform.DOScale(mlocalescale * 1.1f, 1f);
        }
    }
}
