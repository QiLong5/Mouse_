using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager :MonoSingleton<UIManager>
{
    public List<GameObject> mCress;
    public List<GameObject> mVer;

    public List<Sprite> mNumSprites;

    public List<Image> gold;


    public RectTransform mGoinUI;

    public RectTransform mCanvas;

    public Image mDangerImage;

    public bool mIsDanger;

    public Transform mEnemyHps;
    void Start()
    {
       // SetGold(0);
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
}
