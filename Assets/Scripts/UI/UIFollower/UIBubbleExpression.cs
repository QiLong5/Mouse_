using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 表情类型枚举
/// </summary>
public enum ExpressionType
{
    Default,    // 默认
    Happy,      // 开心
    Angry       // 生气
}

/// <summary>
/// 气泡表情UI - 用于显示角色头顶的表情或图标
/// </summary>
public class UIBubbleExpression : UIFollowerBase
{
    [Header("气泡组件")]
    [SerializeField] private Image expressionImage;

    [Header("表情资源配置")]
    [SerializeField] private Sprite defaultExpression;
    [SerializeField] private Sprite happyExpression;
    [SerializeField] private Sprite angryExpression;

    [Header("气泡设置")]
    [SerializeField] bool isAutoHide=false;
    [SerializeField] private float autoHideDelay = 2f;

    private Coroutine autoHideCoroutine;
    private ExpressionType currentExpressionType = ExpressionType.Default;

    protected override void OnInit()
    {
        // 初始化默认表情
        ShowExpression(ExpressionType.Default, false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (isAutoHide)
        {
            StartAutoHide();
        }
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        StopAutoHide();
    }

    /// <summary>
    /// 显示表情（通过表情类型）
    /// </summary>
    public void ShowExpression(ExpressionType expressionType, bool autoHide = true)
    {
        currentExpressionType = expressionType;
        Sprite sprite = GetExpressionSprite(expressionType);

        if (sprite != null && expressionImage != null)
        {
            expressionImage.sprite = sprite;
        }

        if (autoHide)
        {
            StartAutoHide();
        }
    }

    /// <summary>
    /// 显示表情（直接传入Sprite）
    /// </summary>
    public void ShowExpression(Sprite expressionSprite, bool autoHide = true)
    {
        currentExpressionType = ExpressionType.Default;

        if (expressionImage != null)
        {
            expressionImage.sprite = expressionSprite;
        }

        if (autoHide)
        {
            StartAutoHide();
        }
    }

    /// <summary>
    /// 获取当前表情类型
    /// </summary>
    public ExpressionType GetCurrentExpression()
    {
        return currentExpressionType;
    }

    /// <summary>
    /// 根据表情类型获取对应的Sprite
    /// </summary>
    private Sprite GetExpressionSprite(ExpressionType expressionType)
    {
        switch (expressionType)
        {
            case ExpressionType.Happy:
                return happyExpression;
            case ExpressionType.Angry:
                return angryExpression;
            case ExpressionType.Default:
            default:
                return defaultExpression;
        }
    }

    /// <summary>
    /// 开始自动隐藏
    /// </summary>
    private void StartAutoHide()
    {
        StopAutoHide();
        autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
    }

    /// <summary>
    /// 自动隐藏协程
    /// </summary>
    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(autoHideDelay);
        Hide();
    }

    /// <summary>
    /// 停止自动隐藏
    /// </summary>
    private void StopAutoHide()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }
}


