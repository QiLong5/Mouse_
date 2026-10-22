using System;
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

/// <summary>
/// 物体出现/消失动效。
/// 全部动效由 DOTween 驱动 Transform 完成,不会访问 Renderer.material,
/// 因此不会克隆材质实例,不会破坏合批。
/// Fade / FadeScale:物体上有 CanvasGroup 时用 CanvasGroup 做真实透明度(同样不产生材质),
/// 否则用等价的缩放动画替代。
/// </summary>
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

    /// <summary>没有 CanvasGroup 时,用于替代淡入淡出的起始缩放比例</summary>
    private const float FadeSubstituteScaleRatio = 0.75f;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        // 只取自身的 CanvasGroup:有则做真实透明度,没有就用缩放替代,全程不碰材质
        canvasGroup = GetComponent<CanvasGroup>();
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
        KillTweens();
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, duration)
            .SetEase(easeType)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 弹性出现 - 带回弹效果
    /// </summary>
    public void ShowByBounce(Action onComplete = null)
    {
        KillTweens();
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, duration)
            .SetEase(Ease.OutBounce)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 从上方落下
    /// </summary>
    public void ShowByDropDown(float dropHeight = 5f, Action onComplete = null)
    {
        KillTweens();
        Vector3 startPos = originalPosition + Vector3.up * dropHeight;
        transform.localPosition = startPos;
        transform.DOLocalMove(originalPosition, duration)
            .SetEase(Ease.OutBounce)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 从下方升起
    /// </summary>
    public void ShowByRiseUp(float riseHeight = 5f, Action onComplete = null)
    {
        KillTweens();
        Vector3 startPos = originalPosition + Vector3.down * riseHeight;
        transform.localPosition = startPos;
        transform.DOLocalMove(originalPosition, duration)
            .SetEase(easeType)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 淡入出现
    /// 有 CanvasGroup 时用透明度,否则用缩放(0.75 -> 1)等价替代
    /// </summary>
    public void ShowByFade(Action onComplete = null)
    {
        KillTweens();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, duration)
                .SetEase(easeType)
                .SetTarget(this)
                .OnComplete(() => onComplete?.Invoke());
            return;
        }

        transform.localScale = originalScale * FadeSubstituteScaleRatio;
        transform.DOScale(originalScale, duration)
            .SetEase(easeType)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 淡入+缩放出现
    /// </summary>
    public void ShowByFadeScale(Action onComplete = null)
    {
        KillTweens();

        transform.localScale = originalScale * 0.5f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, duration).SetEase(easeType).SetTarget(this);
        }

        transform.DOScale(originalScale, duration)
            .SetEase(easeType)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 旋转出现
    /// </summary>
    public void ShowByRotate(Action onComplete = null)
    {
        KillTweens();
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 180, 0);

        transform.DOScale(originalScale, duration).SetEase(easeType).SetTarget(this);
        transform.DOLocalRotate(Vector3.zero, duration)
            .SetEase(easeType)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region 消失动效

    /// <summary>
    /// 缩放消失 - 从大到小
    /// </summary>
    public void HideByScale(Action onComplete = null)
    {
        KillTweens();
        transform.DOScale(Vector3.zero, duration)
            .SetEase(easeType)
            .SetTarget(this)
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
        KillTweens();
        Vector3 targetPos = transform.localPosition + Vector3.up * flyHeight;
        transform.DOLocalMove(targetPos, duration)
            .SetEase(Ease.InQuad)
            .SetTarget(this)
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
        KillTweens();
        Vector3 targetPos = transform.localPosition + Vector3.down * sinkHeight;
        transform.DOLocalMove(targetPos, duration)
            .SetEase(Ease.InQuad)
            .SetTarget(this)
            .OnComplete(() =>
            {
                transform.localPosition = originalPosition;
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 淡出消失
    /// 有 CanvasGroup 时用透明度,否则用缩放(1 -> 0)等价替代
    /// </summary>
    public void HideByFade(Action onComplete = null)
    {
        KillTweens();

        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, duration)
                .SetEase(easeType)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    canvasGroup.alpha = 1f;
                    onComplete?.Invoke();
                    gameObject.SetActive(false);
                });
            return;
        }

        transform.DOScale(Vector3.zero, duration)
            .SetEase(easeType)
            .SetTarget(this)
            .OnComplete(() =>
            {
                transform.localScale = originalScale;
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 淡出+缩放消失
    /// </summary>
    public void HideByFadeScale(Action onComplete = null)
    {
        KillTweens();

        if (canvasGroup != null)
        {
            transform.DOScale(originalScale * 0.5f, duration).SetEase(easeType).SetTarget(this);
            canvasGroup.DOFade(0f, duration)
                .SetEase(easeType)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    transform.localScale = originalScale;
                    canvasGroup.alpha = 1f;
                    onComplete?.Invoke();
                    gameObject.SetActive(false);
                });
            return;
        }

        transform.DOScale(Vector3.zero, duration)
            .SetEase(easeType)
            .SetTarget(this)
            .OnComplete(() =>
            {
                transform.localScale = originalScale;
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 爆炸消失 - 缩放+旋转
    /// </summary>
    public void HideByExplode(Action onComplete = null)
    {
        KillTweens();

        transform.DOScale(originalScale * 1.5f, duration * 0.3f)
            .SetEase(Ease.OutQuad)
            .SetTarget(this)
            .OnComplete(() =>
            {
                transform.DOScale(Vector3.zero, duration * 0.7f).SetEase(Ease.InQuad).SetTarget(this);
            });

        transform.DORotate(new Vector3(0, 360, 0), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.InQuad)
            .SetTarget(this)
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
        KillTweens();

        Sequence sequence = DOTween.Sequence().SetTarget(this);
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
    /// 杀掉本组件创建的所有动画(不影响其他脚本在同一 Transform 上的动画)
    /// </summary>
    private void KillTweens()
    {
        DOTween.Kill(this);
    }

    /// <summary>
    /// 重置物体状态
    /// </summary>
    public void ResetObject()
    {
        KillTweens();
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        transform.localRotation = Quaternion.identity;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// 停止所有动效
    /// </summary>
    public void StopAllAnimations()
    {
        KillTweens();
    }

    #endregion

    private void OnDestroy()
    {
        KillTweens();
    }
}
