using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// 出现动效类型
/// </summary>
public enum ShowEffectType
{
    Scale,          // 缩放出现
    Bounce,         // 弹性出现
    DropDown,       // 从上方落下
    RiseUp,         // 从下方升起
    Fade,           // 淡入出现
    FadeScale,      // 淡入+缩放
    Rotate          // 旋转出现
}

/// <summary>
/// 消失动效类型
/// </summary>
public enum HideEffectType
{
    Scale,          // 缩放消失
    FlyUp,          // 向上飞走
    SinkDown,       // 向下沉没
    Fade,           // 淡出消失
    FadeScale,      // 淡出+缩放
    Explode,        // 爆炸消失
    Shrink          // 收缩消失
}

public class ObjectFX : MonoBehaviour
{
    [Header("动效类型选择")]
    [Tooltip("选择物体出现时使用的动效")]
    public ShowEffectType showEffectType = ShowEffectType.Scale;
    [Tooltip("选择物体消失时使用的动效")]
    public HideEffectType hideEffectType = HideEffectType.Scale;

    [Header("动效设置")]
    public float duration = 0.5f;
    public Ease easeType = Ease.OutQuad;

    [Header("移动距离设置")]
    [Tooltip("用于下落、升起、飞走等动效的移动距离")]
    public float moveDistance = 1f;

    [Header("自动播放")]
    [Tooltip("启用后会在OnEnable时自动播放出现动效")]
    public bool autoPlayOnEnable = false;

    [Header("回调事件")]
    [Tooltip("出现动效完成时触发")]
    public UnityEvent onShowComplete;
    [Tooltip("消失动效完成时触发")]
    public UnityEvent onHideComplete;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Renderer[] renderers;
    private readonly Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();

    private void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        renderers = GetComponentsInChildren<Renderer>();

        // 保存原始颜色
        foreach (var renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                originalColors[renderer] = renderer.material.color;
            }
        }
    }

    private void OnEnable()
    {
        if (autoPlayOnEnable)
        {
            Show();
        }
    }

    #region 统一调用方法

    /// <summary>
    /// 根据配置的类型播放出现动效 - 使用面板配置的回调
    /// </summary>
    public void Show()
    {
        Show(null);
    }

    /// <summary>
    /// 根据配置的类型播放出现动效 - 使用自定义回调(覆盖面板配置)
    /// </summary>
    /// <param name="onComplete">自定义回调,如果为null则使用面板配置的回调</param>
    public void Show(Action onComplete)
    {
        // 确定使用哪个回调
        Action finalCallback = onComplete ?? (() => onShowComplete?.Invoke());

        switch (showEffectType)
        {
            case ShowEffectType.Scale:
                ShowByScale(finalCallback);
                break;
            case ShowEffectType.Bounce:
                ShowByBounce(finalCallback);
                break;
            case ShowEffectType.DropDown:
                ShowByDropDown(moveDistance, finalCallback);
                break;
            case ShowEffectType.RiseUp:
                ShowByRiseUp(moveDistance, finalCallback);
                break;
            case ShowEffectType.Fade:
                ShowByFade(finalCallback);
                break;
            case ShowEffectType.FadeScale:
                ShowByFadeScale(finalCallback);
                break;
            case ShowEffectType.Rotate:
                ShowByRotate(finalCallback);
                break;
        }
    }

    /// <summary>
    /// 根据配置的类型播放消失动效 - 使用面板配置的回调
    /// </summary>
    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            Hide(null);
        }
      
    }

    /// <summary>
    /// 根据配置的类型播放消失动效 - 使用自定义回调(覆盖面板配置)
    /// </summary>
    /// <param name="onComplete">自定义回调,如果为null则使用面板配置的回调</param>
    public void Hide(Action onComplete)
    {
        // 确定使用哪个回调
        Action finalCallback = onComplete ?? (() => onHideComplete?.Invoke());

        switch (hideEffectType)
        {
            case HideEffectType.Scale:
                HideByScale(finalCallback);
                break;
            case HideEffectType.FlyUp:
                HideByFlyUp(moveDistance, finalCallback);
                break;
            case HideEffectType.SinkDown:
                HideBySinkDown(moveDistance, finalCallback);
                break;
            case HideEffectType.Fade:
                HideByFade(finalCallback);
                break;
            case HideEffectType.FadeScale:
                HideByFadeScale(finalCallback);
                break;
            case HideEffectType.Explode:
                HideByExplode(finalCallback);
                break;
            case HideEffectType.Shrink:
                HideByShrink(finalCallback);
                break;
        }
    }

    #endregion

    #region 出现动效

    /// <summary>
    /// 缩放出现 - 从小到大
    /// </summary>
    public void ShowByScale(Action onComplete = null)
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, duration)
            .SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 弹性出现 - 带回弹效果
    /// </summary>
    public void ShowByBounce(Action onComplete = null)
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, duration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 从上方落下
    /// </summary>
    public void ShowByDropDown(float dropHeight = 5f, Action onComplete = null)
    {
        Vector3 startPos = originalPosition + Vector3.up * dropHeight;
        transform.localPosition = startPos;
        transform.DOLocalMove(originalPosition, duration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 从下方升起
    /// </summary>
    public void ShowByRiseUp(float riseHeight = 5f, Action onComplete = null)
    {
        Vector3 startPos = originalPosition + Vector3.down * riseHeight;
        transform.localPosition = startPos;
        transform.DOLocalMove(originalPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 淡入出现
    /// </summary>
    public void ShowByFade(Action onComplete = null)
    {
        SetAlpha(0f);
        DOTween.To(() => 0f, x => SetAlpha(x), 1f, duration)
            .SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 淡入+缩放出现
    /// </summary>
    public void ShowByFadeScale(Action onComplete = null)
    {
        transform.localScale = originalScale * 0.5f;
        SetAlpha(0f);

        transform.DOScale(originalScale, duration).SetEase(easeType);
        DOTween.To(() => 0f, x => SetAlpha(x), 1f, duration)
            .SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 旋转出现
    /// </summary>
    public void ShowByRotate(Action onComplete = null)
    {
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 180, 0);

        transform.DOScale(originalScale, duration).SetEase(easeType);
        transform.DOLocalRotate(Vector3.zero, duration)
            .SetEase(easeType)
            .OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region 消失动效

    /// <summary>
    /// 缩放消失 - 从大到小
    /// </summary>
    public void HideByScale(Action onComplete = null)
    {
        transform.DOScale(Vector3.zero, duration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 向上飞走消失
    /// </summary>
    public void HideByFlyUp(float flyHeight = 5f, Action onComplete = null)
    {
        Vector3 targetPos = transform.localPosition + Vector3.up * flyHeight;
        transform.DOLocalMove(targetPos, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                transform.localPosition = originalPosition;
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 向下沉没消失
    /// </summary>
    public void HideBySinkDown(float sinkHeight = 5f, Action onComplete = null)
    {
        Vector3 targetPos = transform.localPosition + Vector3.down * sinkHeight;
        transform.DOLocalMove(targetPos, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                transform.localPosition = originalPosition;
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 淡出消失
    /// </summary>
    public void HideByFade(Action onComplete = null)
    {
        DOTween.To(() => 1f, x => SetAlpha(x), 0f, duration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                SetAlpha(1f);
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 淡出+缩放消失
    /// </summary>
    public void HideByFadeScale(Action onComplete = null)
    {
        transform.DOScale(originalScale * 0.5f, duration).SetEase(easeType);
        DOTween.To(() => 1f, x => SetAlpha(x), 0f, duration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                transform.localScale = originalScale;
                SetAlpha(1f);
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 爆炸消失 - 缩放+旋转
    /// </summary>
    public void HideByExplode(Action onComplete = null)
    {
        transform.DOScale(originalScale * 1.5f, duration * 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(Vector3.zero, duration * 0.7f).SetEase(Ease.InQuad);
            });

        transform.DORotate(new Vector3(0, 360, 0), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                transform.localScale = originalScale;
                transform.localRotation = Quaternion.identity;
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 收缩消失 - 先放大后缩小
    /// </summary>
    public void HideByShrink(Action onComplete = null)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(originalScale * 1.2f, duration * 0.3f).SetEase(Ease.OutQuad));
        sequence.Append(transform.DOScale(Vector3.zero, duration * 0.7f).SetEase(Ease.InBack));
        sequence.OnComplete(() =>
        {
            transform.localScale = originalScale;
            onComplete?.Invoke();
            gameObject.SetActive(false);
        });
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 设置物体透明度
    /// </summary>
    private void SetAlpha(float alpha)
    {
        foreach (var renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// 重置物体状态
    /// </summary>
    public void ResetObject()
    {
        DOTween.Kill(transform);
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        transform.localRotation = Quaternion.identity;

        // 恢复原始颜色
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null && kvp.Key.material.HasProperty("_Color"))
            {
                kvp.Key.material.color = kvp.Value;
            }
        }
    }

    /// <summary>
    /// 停止所有动效
    /// </summary>
    public void StopAllAnimations()
    {
        DOTween.Kill(transform);
    }

    #endregion

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}
