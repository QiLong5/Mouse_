using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 进度UI - 用于显示任务进度、加载进度等
/// </summary>
public class UIProgress : UIFollowerBase
{
    [Header("进度条组件")]
    [SerializeField] private Image progressFillImage;

    [Header("数字显示")]
    [SerializeField] private List<Image> numberImages=new List<Image>();

    [Header("进度设置")]
    [SerializeField] private float progressAnimDuration = 0.3f;

    private float currentProgress = 0f;

    protected override void OnInit()
    {
        SetFill(0f,false);
    }

    /// <summary>
    /// 设置进度 (0-1)
    /// </summary>
    public void SetFill(float progress, bool isdofill = true)
    {
        if (currentProgress == progress)
        {
            return;
        }
        currentProgress = Mathf.Clamp01(progress);
        if (isdofill)
        {
            UpdateProgressBar();
        }
        else
        {
            progressFillImage.fillAmount = currentProgress;
        }
        // UpdateNumberDisplay();
    }

    /// <summary>
    /// 更新进度条
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressFillImage != null)
        {
            progressFillImage.DOKill();
            progressFillImage.DOFillAmount(currentProgress, progressAnimDuration);
        }
    }

    /// <summary>
    /// 更新数字显示
    /// </summary>
    private void UpdateNumberDisplay()
    {
        if (numberImages.Count > 0)
        {
            int percentValue = Mathf.RoundToInt(currentProgress * 100);
            UIManager.instance.SetNum(numberImages, percentValue);
        }
    }
}

