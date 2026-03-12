using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示动画设置
/// </summary>
[Serializable]
public class ShowAnimSettings
{
    [Tooltip("显示动画模式")]
    public UIFollowerBase.AnimMode animMode = UIFollowerBase.AnimMode.ScaleAndFade;

    [Tooltip("缩放起始值")]
    public float scaleStart = 0.3f;

    [Tooltip("缩放弹跳峰值")]
    public float scaleBounce = 1.2f;

    [Tooltip("缩放动画时长")]
    public float duration = 0.2f;

    [Tooltip("淡入淡出时长")]
    public float fadeDuration = 0.1f;

    [Tooltip("上浮高度")]
    public float floatHeight = 20f;

    [Tooltip("上浮时长")]
    public float floatDuration = 0.3f;
}

/// <summary>
/// 隐藏动画设置
/// </summary>
[Serializable]
public class HideAnimSettings
{
    [Tooltip("隐藏动画模式")]
    public UIFollowerBase.AnimMode animMode = UIFollowerBase.AnimMode.ScaleAndFade;

    [Tooltip("隐藏动画时长")]
    public float duration = 0.3f;
}

/// <summary>
/// 循环动画设置
/// </summary>
[Serializable]
public class LoopAnimSettings
{
    [Tooltip("循环动画模式")]
    public UIFollowerBase.LoopAnimMode animMode = UIFollowerBase.LoopAnimMode.None;

    [Tooltip("循环缩放幅度(0-1)")]
    public float scaleAmount = 0.1f;

    [Tooltip("循环缩放周期")]
    public float scaleDuration = 1f;

    [Tooltip("循环浮动高度")]
    public float floatHeight = 5f;

    [Tooltip("循环浮动周期")]
    public float floatDuration = 1.5f;
}

/// <summary>
/// UI跟随基类 - 处理UI跟随世界物体的核心逻辑
/// </summary>
public abstract class UIFollowerBase : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] protected Transform targetObject;

    [Header("UI组件")]
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected Transform animationRoot;

    [Header("跟随设置")]
    [SerializeField] protected Vector3 worldOffset = new Vector3(0, 1, 0);
    [SerializeField] protected float cameraScaleFactor = 13f;

    [Header("动画设置")]
    [SerializeField] protected ShowAnimSettings showAnim = new ShowAnimSettings();
    [SerializeField] protected HideAnimSettings hideAnim = new HideAnimSettings();
    [SerializeField] protected LoopAnimSettings loopAnim = new LoopAnimSettings();

    private Tween loopScaleTween;
    private Tween loopFloatTween;
    private Vector3 originalLocalPosition;

    /// <summary>
    /// 动画模式枚举
    /// </summary>
    public enum AnimMode
    {
        None,               // 无动画
        Fade,               // 仅淡入淡出
        Scale,              // 仅缩放
        Float,              // 仅上浮
        ScaleAndFade,       // 缩放+淡入淡出
        FloatAndFade,       // 上浮+淡入淡出
        ScaleAndFloat,      // 缩放+上浮
        All                 // 缩放+上浮+淡入淡出
    }

    /// <summary>
    /// 循环动画模式枚举
    /// </summary>
    public enum LoopAnimMode
    {
        None,           // 无循环动画
        Scale,          // 循环缩放
        Float,          // 上下浮动
        ScaleAndFloat   // 缩放+浮动
    }

    protected virtual void OnEnable()
    {
        FollowObjectWithUI();
        UpdateScaleByCameraZoom();
        Show();
        RegisterCameraEvents();
    }

    protected virtual void OnDisable()
    {
        StopLoopAnimation();
        UnregisterCameraEvents();
    }

    /// <summary>
    /// 初始化UI跟随目标
    /// </summary>
    public virtual void Init(Transform target)
    {
        targetObject = target;
        OnInit();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏UI
    /// </summary>
    public virtual void Hide()
    {
        // 停止循环动画
        StopLoopAnimation();

        // 停止所有动画
        if (animationRoot != null)
        {
            animationRoot.DOKill();
        }
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }

        // 根据隐藏模式播放动画
        switch (hideAnim.animMode)
        {
            case AnimMode.None:
                OnHideComplete();
                break;

            case AnimMode.Fade:
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, hideAnim.duration).OnComplete(OnHideComplete);
                }
                else
                {
                    OnHideComplete();
                }
                break;

            case AnimMode.Scale:
                if (animationRoot != null)
                {
                    animationRoot.DOScale(1.1f, hideAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(0.1f, hideAnim.duration)
                        .OnComplete(OnHideComplete));
                }
                else
                {
                    OnHideComplete();
                }
                break;

            case AnimMode.Float:
                if (animationRoot != null)
                {
                    Vector3 targetPos = animationRoot.localPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.DOLocalMove(targetPos, hideAnim.duration)
                        .SetEase(Ease.InBack)
                        .OnComplete(OnHideComplete);
                }
                else
                {
                    OnHideComplete();
                }
                break;

            case AnimMode.ScaleAndFade:
                bool hasAnim = false;

                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, hideAnim.duration).OnComplete(OnHideComplete);
                    hasAnim = true;
                }

                if (animationRoot != null)
                {
                    animationRoot.DOScale(1.1f, hideAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(0.1f, hideAnim.duration)
                        .OnComplete(() => { if (!hasAnim) OnHideComplete(); }));
                    hasAnim = true;
                }

                if (!hasAnim)
                {
                    OnHideComplete();
                }
                break;

            case AnimMode.FloatAndFade:
                hasAnim = false;

                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, hideAnim.duration).OnComplete(OnHideComplete);
                    hasAnim = true;
                }

                if (animationRoot != null)
                {
                    Vector3 targetPos = animationRoot.localPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.DOLocalMove(targetPos, hideAnim.duration)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => { if (!hasAnim) OnHideComplete(); });
                    hasAnim = true;
                }

                if (!hasAnim)
                {
                    OnHideComplete();
                }
                break;

            case AnimMode.ScaleAndFloat:
                if (animationRoot != null)
                {
                    Vector3 targetPos = animationRoot.localPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.DOLocalMove(targetPos, hideAnim.duration).SetEase(Ease.InBack);

                    animationRoot.DOScale(1.1f, hideAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(0.1f, hideAnim.duration)
                        .OnComplete(OnHideComplete));
                }
                else
                {
                    OnHideComplete();
                }
                break;

            case AnimMode.All:
                hasAnim = false;

                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, hideAnim.duration).OnComplete(OnHideComplete);
                    hasAnim = true;
                }

                if (animationRoot != null)
                {
                    Vector3 targetPos = animationRoot.localPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.DOLocalMove(targetPos, hideAnim.duration).SetEase(Ease.InBack);

                    animationRoot.DOScale(1.1f, hideAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(0.1f, hideAnim.duration)
                        .OnComplete(() => { if (!hasAnim) OnHideComplete(); }));
                    hasAnim = true;
                }

                if (!hasAnim)
                {
                    OnHideComplete();
                }
                break;
        }
    }

    /// <summary>
    /// UI跟随物体
    /// </summary>
    protected virtual void FollowObjectWithUI()
    {
        if (targetObject == null) return;

        Vector3 targetScreenPos = Camera.main.WorldToScreenPoint(targetObject.position + worldOffset);
        transform.position = new Vector2(targetScreenPos.x, targetScreenPos.y);
    }

    /// <summary>
    /// 根据摄像机视野范围调整UI尺寸
    /// </summary>
    protected virtual void UpdateScaleByCameraZoom()
    {
        if (CameraManager.instance.isCress)
        {
            transform.localScale = Vector3.one * (cameraScaleFactor * 0.5f) / Camera.main.orthographicSize;
        }
        else
        {
            transform.localScale = Vector3.one * cameraScaleFactor / Camera.main.orthographicSize;
        }
    }

    /// <summary>
    /// 播放显示动画
    /// </summary>
    protected virtual void Show()
    {
        if (animationRoot != null)
        {
            originalLocalPosition = animationRoot.localPosition;
        }

        // 根据显示模式播放动画
        switch (showAnim.animMode)
        {
            case AnimMode.None:
                // 无动画，直接显示
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
                if (animationRoot != null)
                {
                    animationRoot.localScale = Vector3.one;
                    animationRoot.localPosition = originalLocalPosition;
                }
                PlayLoopAnimation();
                break;

            case AnimMode.Fade:
                // 仅淡入淡出
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1, showAnim.fadeDuration).OnComplete(PlayLoopAnimation);
                }
                else
                {
                    PlayLoopAnimation();
                }

                if (animationRoot != null)
                {
                    animationRoot.localScale = Vector3.one;
                    animationRoot.localPosition = originalLocalPosition;
                }
                break;

            case AnimMode.Scale:
                // 仅缩放
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                if (animationRoot != null)
                {
                    animationRoot.localPosition = originalLocalPosition;
                    animationRoot.localScale = Vector3.one * showAnim.scaleStart;

                    // 弹跳缩放动画
                    animationRoot.DOScale(showAnim.scaleBounce, showAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(1f, showAnim.duration)
                        .OnComplete(PlayLoopAnimation));
                }
                else
                {
                    PlayLoopAnimation();
                }
                break;

            case AnimMode.Float:
                // 仅上浮
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                if (animationRoot != null)
                {
                    Vector3 startPos = originalLocalPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.localPosition = startPos;
                    animationRoot.localScale = Vector3.one;

                    // 上浮动画
                    animationRoot.DOLocalMove(originalLocalPosition, showAnim.floatDuration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(PlayLoopAnimation);
                }
                else
                {
                    PlayLoopAnimation();
                }
                break;

            case AnimMode.ScaleAndFade:
                // 缩放+淡入淡出
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1, showAnim.fadeDuration);
                }

                if (animationRoot != null)
                {
                    animationRoot.localPosition = originalLocalPosition;
                    animationRoot.localScale = Vector3.one * showAnim.scaleStart;

                    // 弹跳缩放动画
                    animationRoot.DOScale(showAnim.scaleBounce, showAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(1f, showAnim.duration)
                        .OnComplete(PlayLoopAnimation));
                }
                else
                {
                    PlayLoopAnimation();
                }
                break;

            case AnimMode.FloatAndFade:
                // 上浮+淡入淡出
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1, showAnim.fadeDuration);
                }

                if (animationRoot != null)
                {
                    Vector3 startPos = originalLocalPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.localPosition = startPos;
                    animationRoot.localScale = Vector3.one;

                    // 上浮动画
                    animationRoot.DOLocalMove(originalLocalPosition, showAnim.floatDuration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(PlayLoopAnimation);
                }
                else
                {
                    PlayLoopAnimation();
                }
                break;

            case AnimMode.ScaleAndFloat:
                // 缩放+上浮
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                if (animationRoot != null)
                {
                    Vector3 startPos = originalLocalPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.localPosition = startPos;
                    animationRoot.localScale = Vector3.one * showAnim.scaleStart;

                    // 上浮动画
                    animationRoot.DOLocalMove(originalLocalPosition, showAnim.floatDuration).SetEase(Ease.OutBack);

                    // 弹跳缩放动画
                    animationRoot.DOScale(showAnim.scaleBounce, showAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(1f, showAnim.duration)
                        .OnComplete(PlayLoopAnimation));
                }
                else
                {
                    PlayLoopAnimation();
                }
                break;

            case AnimMode.All:
                // 缩放+上浮+淡入淡出
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1, showAnim.fadeDuration);
                }

                if (animationRoot != null)
                {
                    Vector3 startPos = originalLocalPosition - new Vector3(0, showAnim.floatHeight, 0);
                    animationRoot.localPosition = startPos;
                    animationRoot.localScale = Vector3.one * showAnim.scaleStart;

                    // 上浮动画
                    animationRoot.DOLocalMove(originalLocalPosition, showAnim.floatDuration).SetEase(Ease.OutBack);

                    // 弹跳缩放动画
                    animationRoot.DOScale(showAnim.scaleBounce, showAnim.duration)
                        .OnComplete(() => animationRoot.DOScale(1f, showAnim.duration)
                        .OnComplete(PlayLoopAnimation));
                }
                else
                {
                    PlayLoopAnimation();
                }
                break;
        }
    }

    /// <summary>
    /// 播放循环动画
    /// </summary>
    protected virtual void PlayLoopAnimation()
    {
        if (animationRoot == null || loopAnim.animMode == LoopAnimMode.None) return;

        StopLoopAnimation();

        switch (loopAnim.animMode)
        {
            case LoopAnimMode.Scale:
                PlayLoopScale();
                break;

            case LoopAnimMode.Float:
                PlayLoopFloat();
                break;

            case LoopAnimMode.ScaleAndFloat:
                PlayLoopScale();
                PlayLoopFloat();
                break;
        }
    }

    /// <summary>
    /// 播放循环缩放动画
    /// </summary>
    private void PlayLoopScale()
    {
        if (animationRoot == null) return;

        Vector3 targetScale = Vector3.one * (1f + loopAnim.scaleAmount);
        loopScaleTween = animationRoot.DOScale(targetScale, loopAnim.scaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 播放循环浮动动画
    /// </summary>
    private void PlayLoopFloat()
    {
        if (animationRoot == null) return;

        Vector3 targetPos = originalLocalPosition + new Vector3(0, loopAnim.floatHeight, 0);
        loopFloatTween = animationRoot.DOLocalMove(targetPos, loopAnim.floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 停止循环动画
    /// </summary>
    protected virtual void StopLoopAnimation()
    {
        if (loopScaleTween != null)
        {
            loopScaleTween.Kill();
            loopScaleTween = null;
        }

        if (loopFloatTween != null)
        {
            loopFloatTween.Kill();
            loopFloatTween = null;
        }

        // 恢复原始状态
        if (animationRoot != null)
        {
            animationRoot.localScale = Vector3.one;
            animationRoot.localPosition = originalLocalPosition;
        }
    }

    /// <summary>
    /// 注册摄像机事件
    /// </summary>
    protected virtual void RegisterCameraEvents()
    {
        if (targetObject != null && CameraManager.instance != null)
        {
            CameraManager.instance.CameraMoveAction += FollowObjectWithUI;
        }

        if (CameraManager.instance != null)
        {
            CameraManager.instance.CameraZoomAction += UpdateScaleByCameraZoom;
        }
    }

    /// <summary>
    /// 注销摄像机事件
    /// </summary>
    protected virtual void UnregisterCameraEvents()
    {
        if (targetObject != null && CameraManager.instance != null)
        {
            CameraManager.instance.CameraMoveAction -= FollowObjectWithUI;
        }

        if (CameraManager.instance != null)
        {
            CameraManager.instance.CameraZoomAction -= UpdateScaleByCameraZoom;
        }
    }

    /// <summary>
    /// 隐藏完成回调
    /// </summary>
    protected virtual void OnHideComplete()
    {
      
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 子类初始化逻辑
    /// </summary>
    protected abstract void OnInit();
}
