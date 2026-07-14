using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class UIManager :MonoSingleton<UIManager>
{
    public List<GameObject> mCress;
    public List<GameObject> mVer;

    public List<Sprite> mNumSprites;

    public List<Image> gold;

    public RectTransform mCanvas;

    public Image mDangerImage;

    public bool mIsDanger;

    public Transform mEnemyHps;
    public TextMeshProUGUI mCoinUI;
    public TextMeshProUGUI mInjectionUI;
    public TextMeshProUGUI mInjectionMaxUI;
    public RectTransform mInjectionChangeUI;//-1
    public Button logoBtn;
    public float injectionPopUpDistance = 50f;//弹出距离
    private int maxPatient;
    private int currentPatient;
    private int coinNum;//当前金币数量
    private Vector3 startPos;
    [Header("引导手指")]
    public Transform tipfinger;
    public Image tipbutton;
    private CanvasGroup tipfingerCanvasGroup;
    private Coroutine tipfingerCoroutine;
    private Vector3 tipfingerOriginalScale;

    private void Start()
    {
        SetCoin(0, false);
        if (LunaManager.instance.jumpMode == JumpMode.GuideFinger)
            StartTipFinger();

        startPos = mInjectionChangeUI.anchoredPosition;
        if(!LunaManager.IsGoogle())
            logoBtn.transform.DOPunchScale(-Vector3.one*0.1f, 1.5f,1).SetLoops(-1);
        //currentPatient = maxPatient = GameDataEditor.instance.GetOtherData.maxInjection;
        currentPatient = maxPatient = 40;
        mInjectionMaxUI.text =  "/" + maxPatient;
        mInjectionUI.text = currentPatient.ToString();
    }

    void Update()
    {
        if (Screen.width>Screen.height)
        {
            foreach (var item in mCress)
            {
                item.SetActive(true);
            }
            foreach (var item in mVer)
            {
                item.SetActive(false);
            }
        }
        else
        {
            foreach (var item in mCress)
            {
                item.SetActive(false);
            }
            foreach (var item in mVer)
            {
                item.SetActive(true);
            }
        }
    }

    public void SetGold(int num)
    {
     //   SetNum(gold, num);
        SetCoin(num,true);
    }
    /// <summary>
    /// 设置图片数字
    /// </summary>
    /// <param name="NumSprite"></param>
    /// <param name="Num"></param>
    public void SetNum(List<Image> NumSprite, int Num)
    {
        if (NumSprite==null|| mNumSprites.Count==0)
        {
            return;
        }
        if (Num >= 9999)
        {
            NumSprite[0].gameObject.SetActive(true);
            NumSprite[1].gameObject.SetActive(true);
            NumSprite[2].gameObject.SetActive(true);
            NumSprite[3].gameObject.SetActive(true);
            NumSprite[3].sprite = mNumSprites[9];
            NumSprite[2].sprite = mNumSprites[9];
            NumSprite[1].sprite = mNumSprites[9];
            NumSprite[0].sprite = mNumSprites[9];            
        }
        else if (Num >= 1000)
        {
            NumSprite[0].gameObject.SetActive(true);
            NumSprite[1].gameObject.SetActive(true);
            NumSprite[2].gameObject.SetActive(true);
            NumSprite[3].gameObject.SetActive(true);
            NumSprite[0].sprite = mNumSprites[(int)(Num / 1000)];
            NumSprite[1].sprite = mNumSprites[(int)(Num % 1000 / 100)];
            NumSprite[2].sprite = mNumSprites[(int)(Num % 100 / 10)];
            NumSprite[3].sprite = mNumSprites[(int)(Num % 10)];          
        }
        else if (Num >= 100)
        {
            NumSprite[0].gameObject.SetActive(true);
            NumSprite[1].gameObject.SetActive(true);
            NumSprite[2].gameObject.SetActive(true);
            NumSprite[3].gameObject.SetActive(false);
            NumSprite[0].sprite = mNumSprites[(int)(Num / 100)];
            NumSprite[1].sprite = mNumSprites[(int)(Num % 100 / 10)];
            NumSprite[2].sprite = mNumSprites[(int)(Num % 10)];        
        }
        else if (Num >= 10)
        {
            NumSprite[3].gameObject.SetActive(false);
            NumSprite[2].gameObject.SetActive(false);
            NumSprite[0].gameObject.SetActive(true);
            NumSprite[1].gameObject.SetActive(true);
            NumSprite[0].sprite = mNumSprites[(int)(Num / 10)];
            NumSprite[1].sprite = mNumSprites[(int)(Num % 10)];           
        }
        else if (Num > 0)
        {
            NumSprite[3].gameObject.SetActive(false);
            NumSprite[2].gameObject.SetActive(false);
            NumSprite[1].gameObject.SetActive(false);
            NumSprite[0].gameObject.SetActive(true);
            NumSprite[0].sprite = mNumSprites[(int)(Num % 10)];            
        }
        else
        {
            NumSprite[3].gameObject.SetActive(false);
            NumSprite[2].gameObject.SetActive(false);
            NumSprite[1].gameObject.SetActive(false);
            NumSprite[0].gameObject.SetActive(true);
            NumSprite[0].sprite = mNumSprites[0];            
        }
    }

    public void StartDanger()
    {
        if (!mIsDanger)
        {
            mIsDanger = true;
            mDangerFlash = DangerFlash();
            StartCoroutine(mDangerFlash);
        }
     
    }

    public void StopDanger()
    {
        if (mIsDanger)
        {
            if (mDangerFlash != null)
            {
                StopCoroutine(mDangerFlash);
                mIsDanger = false;
                mDangerImage.gameObject.SetActive(false);
            }
        }
      
    }

    IEnumerator mDangerFlash;
    IEnumerator DangerFlash()
    {
        mDangerImage.gameObject.SetActive(true);
        while (!LunaManager.instance.isGameOver)
        {
            mDangerImage.DOFade(1, 0.2f);
            yield return new WaitForSeconds(0.2f);
            mDangerImage.DOFade(0, 0.2f);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private int lastNum;
    private Tween numberTween;
    /// <summary>
    /// 金币数量更改
    /// </summary>
    /// <param name="num"></param>
    /// <param name="isPlayAni"></param>
    public void SetCoin(int num, bool isPlayAni)
    {
        if (mCoinUI == null) return;
        lastNum = coinNum;
        coinNum = num;
        if (isPlayAni)
        {
            numberTween?.Kill();
            numberTween = DOTween.To(() => lastNum, x => mCoinUI.text = x.ToString(), coinNum, 0.5f).SetEase(Ease.OutQuad);
        }
        else
        {
            mCoinUI.text = coinNum.ToString();
        }
    }

    public void SetInjection(int changeNum)
    {
        if (changeNum == 0||currentPatient==0) return;
        currentPatient += changeNum;
        if (currentPatient <= 0)
            currentPatient = 0;

        // 弹出变化量文本
        NumChange(changeNum);

        // 更新主文本
        mInjectionUI.text = currentPatient.ToString();
        mInjectionUI.color = Color.green;
        mInjectionUI.transform.localScale = Vector3.one;
        mInjectionUI.transform.DOKill();
        if(!LunaManager.IsGoogle())
            mInjectionUI.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f).OnComplete(() => mInjectionUI.color = Color.white);
    }
    public void NumChange(int changeNum)
    {
        // 弹出变化量文本
        mInjectionChangeUI.gameObject.SetActive(true);
        mInjectionChangeUI.GetChild(0).GetComponent<TextMeshProUGUI>().text = changeNum.ToString();
        var rt = mInjectionChangeUI;
        rt.anchoredPosition = startPos;
        DOTween.Kill(rt);
        rt.DOAnchorPosY(startPos.y + injectionPopUpDistance, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            mInjectionChangeUI.gameObject.SetActive(false);
            rt.anchoredPosition = startPos;
        });
    }
    
    #region 引导手指
    // 初始化引导手指组件，Start 自动调用，外部也可手动调用
    public void TipClickInit()
    {
        if (tipfinger != null)
        {
            tipfingerCanvasGroup = tipfinger.GetComponent<CanvasGroup>();
            if (tipfingerCanvasGroup == null)
                tipfingerCanvasGroup = tipfinger.gameObject.AddComponent<CanvasGroup>();
            tipfingerOriginalScale = tipfinger.localScale;
            tipfinger.gameObject.SetActive(false);
        }
    }

    // 启动引导手指循环（30秒后首次显示，每隔10秒循环）
    public void StartTipFinger()
    {
        TipClickInit();
        if (tipfinger != null && tipfingerCoroutine == null)
            tipfingerCoroutine = StartCoroutine(TipFingerLoop());
    }

    // 停止引导手指循环并隐藏手指
    public void StopTipFinger()
    {
        if (tipfingerCoroutine != null)
        {
            StopCoroutine(tipfingerCoroutine);
            tipfingerCoroutine = null;
        }
        if (tipfinger != null)
        {
            tipfinger.DOKill();
            tipfinger.gameObject.SetActive(false);
        }
    }
    private IEnumerator TipFingerLoop()
    {
        yield return new WaitForSeconds(30f);
        while (!LunaManager.instance.isGameOver)
        {
            ShowTipFinger();
            yield return new WaitForSeconds(5f);
            HideTipFinger();
            yield return new WaitForSeconds(10f);
        }
    }

    private void ShowTipFinger()
    {
        if (tipfinger == null) return;

        tipfinger.gameObject.SetActive(true);
        tipfinger.DOKill();
        tipfinger.localRotation = Quaternion.identity;
        tipfingerCanvasGroup.alpha = 1;
        tipfinger.localScale = tipfingerOriginalScale * 0.5f;
        tipfinger.DOScale(tipfingerOriginalScale, 0.5f).SetEase(Ease.OutBack);
        StartTipFingerShake();
    }

    private void HideTipFinger()
    {
        if (tipfinger == null) return;

        tipfinger.DOKill();
        tipfingerCanvasGroup.DOFade(0, 0.5f).OnComplete(() => tipfinger.gameObject.SetActive(false));
    }

    private void StartTipFingerShake()
    {
        if (tipfinger == null) return;

        Sequence s = DOTween.Sequence();
        Color originalButtonColor = tipbutton != null ? tipbutton.color : Color.white;

        for (int i = 0; i < 3; i++)
        {
            s.Append(tipfinger.DORotate(new Vector3(0, 0, -15f), 0.125f));
            s.Join(tipfinger.DOScale(tipfingerOriginalScale, 0.125f));
            if (tipbutton != null) s.Join(tipbutton.DOColor(originalButtonColor, 0.125f));

            if (tipbutton != null)
            {
                s.Append(tipbutton.DOColor(originalButtonColor * 0.7f, 0.06f));
                s.Join(tipfinger.DORotate(new Vector3(0, 0, 15f), 0.25f));
                s.Join(tipfinger.DOScale(tipfingerOriginalScale * 1.1f, 0.25f));
            }
            else
            {
                s.Append(tipfinger.DORotate(new Vector3(0, 0, 15f), 0.25f));
                s.Join(tipfinger.DOScale(tipfingerOriginalScale * 1.1f, 0.25f));
            }

            s.Append(tipfinger.DORotate(new Vector3(0, 0, -15f), 0.25f));
            s.Join(tipfinger.DOScale(tipfingerOriginalScale, 0.25f));
            if (tipbutton != null) s.Join(tipbutton.DOColor(originalButtonColor, 0.25f));

            if (tipbutton != null)
            {
                s.Append(tipbutton.DOColor(originalButtonColor * 0.7f, 0.06f));
                s.Join(tipfinger.DORotate(new Vector3(0, 0, 15f), 0.25f));
                s.Join(tipfinger.DOScale(tipfingerOriginalScale * 1.1f, 0.25f));
            }
            else
            {
                s.Append(tipfinger.DORotate(new Vector3(0, 0, 15f), 0.25f));
                s.Join(tipfinger.DOScale(tipfingerOriginalScale * 1.1f, 0.25f));
            }

            s.Append(tipfinger.DORotate(Vector3.zero, 0.125f));
            s.Join(tipfinger.DOScale(tipfingerOriginalScale, 0.125f));
            if (tipbutton != null) s.Join(tipbutton.DOColor(originalButtonColor, 0.125f));

            if (i < 2) s.AppendInterval(1f);
        }
    }

    #endregion
}
