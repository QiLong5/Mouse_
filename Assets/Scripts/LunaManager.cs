using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Spine.Unity;
using DG.Tweening;

public enum JumpMode
{
    None,               // 全关
    ForceGotoStore,     // 结束时强制跳转
    GuideFinger,        // 引导跳转
    ProgressForceJump   // 75进度强制跳转
}

public class LunaManager : MonoBehaviour
{
    public bool isGameStart;
    public bool isGameOver;
    [Header("跳转模式")]
    [LunaPlaygroundField("跳转模式", 0, "Game Settings")]
    public JumpMode jumpMode = JumpMode.None;

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

    public ObjStartButton objStartButton;
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
    }
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
