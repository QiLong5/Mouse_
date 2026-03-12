using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血条UI - 用于显示角色或敌人的血量，带缓冲血条效果
/// </summary>
public class UIHealthBar : UIFollowerBase
{
    [Header("血条组件")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image bufferFillImage;

    [Header("血条动画设置")]
    [SerializeField] private float fillAnimDuration = 0.2f;
    [SerializeField] private float bufferDelay = 0.3f;
    [SerializeField] private float bufferAnimDuration = 0.5f;

    private float currentHealthPercent = 1f;
    private Tween bufferTweener;

    protected override void OnInit()
    {
        SetHpFill(1f,false,false);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (bufferTweener != null)
        {
            bufferTweener.Kill();
            bufferTweener = null;
        }
    }

    /// <summary>
    /// 设置血量百分比 (0-1)
    /// </summary>
    /// <param name="percent">血量百分比 (0-1)</param>
    /// <param name="useBuffer">是否使用缓冲血条效果</param>
    public void SetHpFill(float percent, bool useBuffer = true, bool isdofill = true)
    {
        if (isdofill)
        {
            currentHealthPercent = Mathf.Clamp01(percent);
            UpdateHealthBar(useBuffer);
        }
        else
        {
            fillImage.DOKill();
            bufferFillImage.DOKill();
            currentHealthPercent = Mathf.Clamp01(percent);
            fillImage.fillAmount = currentHealthPercent;
            bufferFillImage.fillAmount = currentHealthPercent;
        }
    }


    /// <summary>
    /// 更新血条显示
    /// </summary>
    private void UpdateHealthBar(bool useBuffer)
    {
        if (fillImage == null) return;

        float targetPercent = currentHealthPercent;
        float currentPercent = fillImage.fillAmount;

        // 停止之前的动画
        fillImage.DOKill();

        // 主血条立即变化
        fillImage.DOFillAmount(targetPercent, fillAnimDuration);

        // 缓冲血条延迟跟随
        if (bufferFillImage != null)
        {
            // 不使用缓冲效果，直接同步
            if (!useBuffer)
            {
                if (bufferTweener != null)
                {
                    bufferTweener.Kill();
                    bufferTweener = null;
                }
                bufferFillImage.DOKill();
                bufferFillImage.DOFillAmount(targetPercent, fillAnimDuration);
                return;
            }

            // 如果血量下降，缓冲条延迟后慢慢减少
            if (targetPercent < currentPercent)
            {
                if (bufferTweener != null)
                {
                    bufferTweener.Kill();
                }

                bufferTweener = DOVirtual.DelayedCall(bufferDelay, () =>
                {
                    bufferFillImage.DOKill();
                    bufferFillImage.DOFillAmount(targetPercent, bufferAnimDuration);
                });
            }
            // 如果血量上升，缓冲条立即跟随
            else if (targetPercent > currentPercent)
            {
                if (bufferTweener != null)
                {
                    bufferTweener.Kill();
                    bufferTweener = null;
                }

                bufferFillImage.DOKill();
                bufferFillImage.DOFillAmount(targetPercent, fillAnimDuration);
            }
        }
    }

    /// <summary>
    /// 获取当前血量百分比
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealthPercent;
    }
}
