using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI number;

    [Header("进度设置")]
    [SerializeField] private float progressAnimDuration = 0.3f;

    [Header("草药不足提示")]
    [SerializeField] private List<Image> alertImages = new List<Image>();
    [SerializeField] private TextMeshProUGUI alertText;
    [SerializeField] private float flashInterval = 0.4f;
    [SerializeField] private float breathScale = 1.3f;
    [SerializeField] private float breathDuration = 0.5f;

    private static readonly Color RedColor = Color.red;

    private float currentProgress = 0f;
    private Coroutine flashCoroutine;
    private Sequence breathSequence;
    private readonly List<Color> originalImageColors = new List<Color>();
    private Color originalTextColor;

    protected override void OnInit()
    {
        SetFill(0f,0,false);
        CacheOriginalColors();
    }

    private void CacheOriginalColors()
    {
        originalImageColors.Clear();
        foreach (var img in alertImages)
            originalImageColors.Add(img != null ? img.color : Color.white);
        if (alertText != null)
            originalTextColor = alertText.color;
    }

    /// <summary>
    /// 设置进度 (0-1)
    /// </summary>
    public void SetFill(float progress,int currentNum=0, bool isdofill = true)
    {
        currentProgress = Mathf.Clamp01(progress);
        if (isdofill)
        {
            UpdateProgressBar();
        }
        else
        {
            progressFillImage.fillAmount = currentProgress;
        }
        number.text = "x" + currentNum;
    }

    // 草药为0时开始闪红+呼吸动画
    public void StartHerbAlert()
    {
        if (flashCoroutine != null) return;
        CacheOriginalColors();
        flashCoroutine = StartCoroutine(FlashRoutine());
        StartBreathAnim();
    }

    // 草药数量恢复后停止
    public void StopHerbAlert()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        StopBreathAnim();
        RestoreColors();
    }

    private IEnumerator FlashRoutine()
    {
        bool red = false;
        while (true)
        {
            red = !red;
            for (int i = 0; i < alertImages.Count; i++)
            {
                if (alertImages[i] == null) continue;
                Color orig = i < originalImageColors.Count ? originalImageColors[i] : Color.white;
                alertImages[i].color = red ? RedColor : orig;
            }
            if (alertText != null)
                alertText.color = red ? RedColor : originalTextColor;

            yield return new WaitForSeconds(flashInterval);
        }
    }

    private void StartBreathAnim()
    {
        if (alertText == null) return;
        breathSequence?.Kill();
        alertText.gameObject.SetActive(true);
        alertText.transform.localScale = Vector3.one;
        breathSequence = DOTween.Sequence()
            .Append(alertText.transform.DOScale(breathScale, breathDuration).SetEase(Ease.InOutSine))
            .Append(alertText.transform.DOScale(1f, breathDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1);
    }

    private void StopBreathAnim()
    {
        breathSequence?.Kill();
        breathSequence = null;
        if (alertText != null)
        {
            alertText.gameObject.SetActive(false);
            alertText.transform.localScale = Vector3.one;
        }
    }

    private void RestoreColors()
    {
        for (int i = 0; i < alertImages.Count; i++)
        {
            if (alertImages[i] == null) continue;
            alertImages[i].color = i < originalImageColors.Count ? originalImageColors[i] : Color.white;
        }
        if (alertText != null)
            alertText.color = originalTextColor;
    }

    private void UpdateProgressBar()
    {
        if (progressFillImage != null)
        {
            progressFillImage.DOKill();
            progressFillImage.DOFillAmount(currentProgress, progressAnimDuration);
        }
    }
}

