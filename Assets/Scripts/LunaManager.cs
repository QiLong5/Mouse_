using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Spine.Unity;
using DG.Tweening;

#if UNITY_LUNA
using Bridge;
#endif

public enum JumpMode
{
    None,               // 全关
    ForceGotoStore,     // 结束时强制跳转
    GuideFinger,        // 引导跳转
    ProgressForceJump   // 75进度强制跳转
}

/// <summary>编辑器渠道模拟枚举（仅编辑器 / 非 Luna 包生效，用于不打包测试分渠道逻辑）。
/// 本枚举为自包含副本，不依赖 PALib.LunaManager（后者后续可能被删除）。</summary>
public enum EditorChannel
{
    Google,          // google
    GoogleAdManager, // google-manager
    Facebook,        // facebook
    UnityAds,        // unityads
    Applovin,        // applovin
    Other,           // other
}

#if UNITY_LUNA
/// <summary>
/// 读取外层打包器(soyoo / sass-playable)注入 index.html &lt;head&gt; 的渠道标记：
///   &lt;script&gt;window.currentChannel="google"&lt;/script&gt;
/// [External]+[Name("window")] 桥接 JS 全局 window；静态属性 -&gt; window.currentChannel（直接成员访问，不会走 new window()）。
/// 本类为自包含副本，不依赖 PALib.LunaManager（后者后续可能被删除）。
/// </summary>
[External]
[Name("window")]
public static class LunaBrowserGlobals
{
    public static extern string currentChannel { get; }
}
#endif

public class LunaManager : MonoBehaviour
{
    public bool isGameStart;
    public bool isGameOver;
    [Header("跳转模式")]
    [LunaPlaygroundField("跳转模式", 0, "Game Settings")]
    public JumpMode jumpMode = JumpMode.None;

    [Header("编辑器渠道模拟（仅编辑器测试用；Luna 导出包始终读真实 window.currentChannel）")]
    [LunaPlaygroundField("模拟渠道", 1, "Channel")]
    public EditorChannel editorChannel = EditorChannel.Google;

    // 进度打点去重标记，防止重复上报
    private bool hasReported25 = false;
    private bool hasReported50 = false;
    private bool hasReported75 = false;
    private bool hasReported100 = false;

#if UNITY_LUNA
    // AppLovin 打点转发桥（复用 PALib 目录下的 pc.MyTextListener 外部引用）
    private PALib.MyTextListener _listener = new PALib.MyTextListener();
#endif

    public event Action SceneResetEvent;
    public static LunaManager instance
    {
        get
        {
            return s_Instance;
        }
    }
    private static LunaManager s_Instance;

    public virtual void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = (LunaManager)this;
            s_Instance.gameObject.name = s_Instance.GetType().Name;
        }
        s_EditorChannel = editorChannel;
    }

#if UNITY_EDITOR && !UNITY_LUNA
    // Inspector 里改枚举时实时同步到静态镜像，无需运行即可让 IsGoogle()/CurrentName() 生效。
    // 守卫与 s_EditorChannel(#if !UNITY_LUNA) 对齐，避免 Luna 导出(UNITY_EDITOR+UNITY_LUNA 并存)时引用不存在的字段。
    private void OnValidate()
    {
        s_EditorChannel = editorChannel;
    }
#endif

    private void Start()
    {
        Luna.Unity.LifeCycle.GameLoaded();
        AudioListener.volume = 0;

        // 可玩广告初始化打点
        mOnInitPlayable();
        mOnLoaded();
        mOnDisplay();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isGameStart)
        {
            AudioListener.volume = 1;
            GameStart();
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        //objStartButton.OnDown();

        StartCoroutine(GameOverIE());

    }

    IEnumerator GameOverIE()
    {
        //yield return new WaitForSeconds(1.5f);
        if (jumpMode == JumpMode.ForceGotoStore) GotoStore();

        Luna.Unity.LifeCycle.GameEnded(new object[] { "result", "win" });
        // 结束卡展示 & 完成打点
        mOnShowEndCard();
        mOnCompleted();

        var ui = GameObject.Find("UIManager");
        bool isLandscape = Screen.width > Screen.height;
        var win = ui.transform.Find(isLandscape ? "Win2" : "Win");
        if (win != null)
        {
            if(!IsGoogle())
                win.transform.Find("Button (1)").DOPunchScale(-Vector3.one * 0.2f, 1.5f, 1).SetLoops(-1);
            win.gameObject.SetActive(true);//显示胜利界面
            AudioManager.instance.Play(SK.结算页面弹出);
            yield return new WaitForSeconds(0.5f);
            AudioManager.instance.StopAllGameSounds();
            AudioManager.instance.Play(SK.结算BGM);
        }
    }

    /// <summary>
    /// 重新加载场景
    /// </summary>
    public void ReLoad()
    {
        SceneResetEvent?.Invoke();
        mOnRetry();
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

    }

    /// <summary>
    /// 去商店
    /// </summary>
    public void GotoStore()
    {
        mOnInstall();
        Luna.Unity.Playable.InstallFullGame();

    }

    public void GameStart()
    {
        AudioManager.instance.Play(SK.BGM);
        isGameStart = true;
        Luna.Unity.LifeCycle.GameStarted();
        // 挑战开始打点（进度 0）
        mGameProgress(0);
        // 启动tipfinger显示
        //  UIManager.instance.StartTipFinger();
    }

    /// <summary>
    /// 更新游戏进度并追踪关键节点
    /// </summary>
    /// <param name="progress">进度值 0-100</param>
    public void GameUpdated(int progress)
    {
        // 进度节点打点（25 / 50 / 75 / 100，自动去重）
        if (progress >= 25 && !hasReported25)
        {
            hasReported25 = true;
            mGameProgress(25);
        }
        if (progress >= 50 && !hasReported50)
        {
            hasReported50 = true;
            mGameProgress(50);
        }
        if (progress >= 75 && !hasReported75)
        {
            hasReported75 = true;
            mGameProgress(75);
        }
        if (progress >= 100 && !hasReported100)
        {
            hasReported100 = true;
            mGameProgress(100);
        }

        if (jumpMode == JumpMode.ProgressForceJump && progress == 75)
            GotoStore();
    }

    #region 渠道判断（Channel）— 逻辑参考 PALib.LunaManager

    // Luna 包内读真实 window.currentChannel（google/facebook/...）；
    // 编辑器里返回 Inspector 上 editorChannel 模拟的渠道名，方便不打包测试。
    //
    // 【别踩的坑】
    //   1. Application.buildPlatform —— 被 Luna 固化成导出时的 Unity BuildTarget，与分发渠道无关。
    //   2. Luna.Unity.Playable.Platform —— 是“广告网络”(unityads/applovin/...)，非分发渠道。
    // 只有 window.currentChannel 才是外层按渠道打包时写死的真值。


    // 编辑器渠道模拟：由 Inspector 的 editorChannel 同步而来（见 Awake / OnValidate）。
    private static EditorChannel s_EditorChannel = EditorChannel.Google;

    private static string EditorChannelToName(EditorChannel c)
    {
        switch (c)
        {
            case EditorChannel.Google: return "google";
            case EditorChannel.GoogleAdManager: return "google-manager";
            case EditorChannel.Facebook: return "facebook";
            case EditorChannel.UnityAds: return "unityads";
            case EditorChannel.Applovin: return "applovin";
            default: return "other";
        }
    }

    /// <summary>当前渠道名。Luna 包内读真实 window.currentChannel；编辑器返回 editorChannel 模拟值。</summary>
    public static string CurrentName()
    {
#if UNITY_LUNA
        string ch = LunaBrowserGlobals.currentChannel;
        return string.IsNullOrEmpty(ch) ? EditorChannelToName(s_EditorChannel) : ch;
#else
        return EditorChannelToName(s_EditorChannel);
#endif
    }

    /// <summary>是否谷歌渠道。可直接控制显隐：obj.SetActive(LunaManager.IsGoogle())。
    /// 编辑器里由 Inspector 的 editorChannel 决定，包里由真实渠道决定。</summary>
    public static bool IsGoogle()
    {
        // 渠道值形如 "google"（"google-manager" 等变体 StartsWith 一并覆盖）。
        return CurrentName().StartsWith("google");
    }

    /// <summary>打印当前渠道。打包后在浏览器 console 里能看到这行。</summary>
    public static void LogCurrent()
    {
        Debug.Log("[LunaManager] 当前渠道 = " + CurrentName() + " , IsGoogle = " + IsGoogle());
    }

    #endregion

    #region 打点（MyTextListener 转发）

    private void mOnInitPlayable()
    {
#if UNITY_LUNA
        try { _listener.onInitPlayable(); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] onInitPlayable 失败: {e.Message}"); }
#else
        Debug.Log("[Analytics] onInitPlayable");
#endif
    }

    private void mOnLoaded()
    {
#if UNITY_LUNA
        try { _listener.onLoaded(); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] onLoaded 失败: {e.Message}"); }
#else
        Debug.Log("[Analytics] onLoaded");
#endif
    }

    private void mOnDisplay()
    {
#if UNITY_LUNA
        try { _listener.onDisplay(); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] onDisplay 失败: {e.Message}"); }
#else
        Debug.Log("[Analytics] onDisplay");
#endif
    }

    private void mGameProgress(int progress)
    {
#if UNITY_LUNA
        try { _listener.gameProgress(progress); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] gameProgress({progress}) 失败: {e.Message}"); }
#else
        Debug.Log($"[Analytics] gameProgress {progress}");
#endif
    }

    private void mOnShowEndCard()
    {
#if UNITY_LUNA
        try { _listener.onGameOver(); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] onGameOver 失败: {e.Message}"); }
#else
        Debug.Log("[Analytics] onShowEndCard");
#endif
    }

    private void mOnRetry()
    {
#if UNITY_LUNA
        try { _listener.onRetry(); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] onRetry 失败: {e.Message}"); }
#else
        Debug.Log("[Analytics] onRetry");
#endif
    }

    private void mOnInstall()
    {
#if UNITY_LUNA
        try { _listener.onInstall(); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] onInstall 失败: {e.Message}"); }
#else
        Debug.Log("[Analytics] onInstall");
#endif
    }

    private void mOnCompleted()
    {
#if UNITY_LUNA
        try { _listener.onCompleted(); }
        catch (Exception e) { Debug.LogWarning($"[Analytics] onCompleted 失败: {e.Message}"); }
#else
        Debug.Log("[Analytics] onCompleted");
#endif
    }

    #endregion
}
