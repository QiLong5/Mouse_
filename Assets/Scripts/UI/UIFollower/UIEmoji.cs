using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 表情状态枚举
/// </summary>
public enum EmojiState
{
    zhengtong,
    qiangzhuang,
}

/// <summary>
/// 表情UI - 跟随目标显示不同表情图标
/// </summary>
public class UIEmoji : UIFollowerBase
{
    [Header("表情图标列表")]
    [SerializeField] private List<Sprite> imgLst = new List<Sprite>();
    [SerializeField] private Image icon;

    [Header("闪红背景")]
    [SerializeField] private Image imgRed;
    [SerializeField] private float flashDuration = 0.6f;

    private EmojiState currentState;
    private Coroutine flashCoroutine;

    protected override void OnInit()
    {

    }

    private void LateUpdate()
    {
        FollowObjectWithUI();
    }

    /// <summary>
    /// 切换表情
    /// </summary>
    public void SwitchEmoji(EmojiState state)
    {
        currentState = state;
        icon.sprite = imgLst[(int)state];

        if (state == EmojiState.zhengtong)
        {
            flashCoroutine ??= StartCoroutine(FlashRedCoroutine());
        }
        else
        {
            StopFlash();
        }
    }

    private void StopFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        SetRedAlpha(0f);
    }

    private IEnumerator FlashRedCoroutine()
    {
        float half = flashDuration * 0.5f;
        while (true)
        {
            // 0 → 1
            yield return StartCoroutine(FadeRed(0f, 1f, half));
            // 1 → 0
            yield return StartCoroutine(FadeRed(1f, 0f, half));
        }
    }

    private IEnumerator FadeRed(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetRedAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetRedAlpha(to);
    }

    private void SetRedAlpha(float a)
    {
        if (imgRed == null) return;
        Color c = imgRed.color;
        c.a = a;
        imgRed.color = c;
    }

    /// <summary>
    /// 获取当前表情状态
    /// </summary>
    public EmojiState GetCurrentState()
    {
        return currentState;
    }

    protected override void OnDisable()
    {
        StopFlash();
        base.OnDisable();
    }
}
