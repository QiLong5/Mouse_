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

#if UNITY_LUNA
    // 进度打点标记，防止重复上报（仅在Luna构建时使用）
    private bool hasReported25 = false;
    private bool hasReported50 = false;
    private bool hasReported75 = false;
    private bool hasChallengeStarted = false;
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
        isGameOver = true;
        //objStartButton.OnDown();

        StartCoroutine(GameOverIE());

    }

    IEnumerator GameOverIE()
    {
        yield return new WaitForSeconds(1.5f);
        if (jumpMode == JumpMode.ForceGotoStore) GotoStore();

        Luna.Unity.LifeCycle.GameEnded(new object[] { "result", "win" });
        // 追踪结束卡片显示事件
        TrackEndcardShown();

        var ui = GameObject.Find("UIManager");
        bool isLandscape = Screen.width > Screen.height;
        var win = ui.transform.Find(isLandscape ? "Win2" : "Win");
        // var sine = win.GetComponentInChildren<SkeletonGraphic>();
        // sine.AnimationState.SetAnimation(0, "yw", false);
        win.transform.Find("Button (1)").DOPunchScale(-Vector3.one * 0.2f, 1.5f, 1).SetLoops(-1);
        win.gameObject.SetActive(true);//显示胜利界面
        AudioManager.instance.Play(SK.结算页面弹出);
        yield return new WaitForSeconds(0.5f);
        AudioManager.instance.StopAllGameSounds();
        AudioManager.instance.Play(SK.结算BGM);
    }
    /// <summary>
    /// 追踪 ENDCARD_SHOWN 事件（Axon Analytics）
    /// </summary>
    private void TrackEndcardShown()
    {

#if UNITY_LUNA
        try
        { 
            Luna.Unity.Analytics.Applovin.LogChallengeSolved();
            LunaAnalytics analytics = new LunaAnalytics();
            analytics.trackEvent("ENDCARD_SHOWN");
            Debug.Log("[Analytics] ENDCARD_SHOWN 事件已发送");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] 发送 ENDCARD_SHOWN 失败: {e.Message}");
        }
#endif
    }


    /// <summary>
    /// 重新加载场景
    /// </summary>
    public void ReLoad()
    {
        SceneResetEvent?.Invoke();
        Luna.Unity.Analytics.Applovin.LogChallengeRetry();
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

    }

    /// <summary>
    /// 去商店
    /// </summary>
    public void GotoStore()
    {
        Luna.Unity.Playable.InstallFullGame();

    }

    public void GameStart()
    {
        AudioManager.instance.Play(SK.BGM);
        isGameStart = true;
        Luna.Unity.LifeCycle.GameStarted();
        // 追踪挑战开始事件
        TrackChallengeStarted();
        // 启动tipfinger显示
        //  UIManager.instance.StartTipFinger();
    }

    /// <summary>
    /// 更新游戏进度并追踪关键节点
    /// </summary>
    /// <param name="progress">进度值 0-100</param>
    public void GameUpdated(int progress)
    {
        // 向Luna平台上报进度（保留原有逻辑）
        // Luna.Unity.LifeCycle.GameUpdated(new object[] { "progress", progress });

        // 追踪AppLovin/Axon Analytics进度节点
        TrackProgressMilestones(progress);
        if (jumpMode == JumpMode.GuideFinger && progress == 50)
            UIManager.instance.StartTipFinger();
        else if (jumpMode == JumpMode.ProgressForceJump && progress == 75)
            GotoStore();
    }

    /// <summary>
    /// 追踪挑战开始事件
    /// </summary>
    private void TrackChallengeStarted()
    {

#if UNITY_LUNA
        if (!hasChallengeStarted)
        {
            try
            {
                Luna.Unity.Analytics.Applovin.LogChallengeStarted();
                hasChallengeStarted = true;
                Debug.Log("[Analytics] CHALLENGE_STARTED 事件已发送");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] 发送 CHALLENGE_STARTED 失败: {e.Message}");
            }
        }
#endif
    }

    /// <summary>
    /// 追踪进度节点事件（25%, 50%, 75%）
    /// </summary>
    /// <param name="progress">当前进度 0-100</param>
    private void TrackProgressMilestones(int progress)
    {
#if UNITY_LUNA
        try
        {
            // 25% 进度
            if (progress >= 25 && !hasReported25)
            {
               Luna.Unity.Analytics.Applovin.LogChallengePass25();
                hasReported25 = true;
                Debug.Log("[Analytics] CHALLENGE_PASS_25 事件已发送");
            }

            // 50% 进度
            if (progress >= 50 && !hasReported50)
            {
                Luna.Unity.Analytics.Applovin.LogChallengePass50();
                hasReported50 = true;
                Debug.Log("[Analytics] CHALLENGE_PASS_50 事件已发送");
            }

            // 75% 进度
            if (progress >= 75 && !hasReported75)
            {
                Luna.Unity.Analytics.Applovin.LogChallengePass75();
                hasReported75 = true;
                Debug.Log("[Analytics] CHALLENGE_PASS_75 事件已发送");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Analytics] 发送进度事件失败: {e.Message}");
        }
#endif
    }
}
