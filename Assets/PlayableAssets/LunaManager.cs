using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_LUNA
using Bridge;
#endif

namespace PALib
{
    public enum JumpType
    {
        Default,
        GuideJump,
        EndForceJump,
        Progress75Jump
    }

    /// <summary>编辑器渠道模拟枚举（仅编辑器 / 非 Luna 包生效，用于不打包测试分渠道逻辑）。</summary>
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
    [External]
    [Name("VibrationBridge")]
    public class VibrationBridge
    {
        public extern void vibrate(int duration);
        public extern void vibratePattern(int[] pattern);
        public extern void stop();
        public extern bool isSupported();
    }

    /// <summary>
    /// AppLovin 打点桥。对应外部引用 MyTextListener.js（pc.MyTextListener），
    /// 内部转发到宿主注入的 window.playableAnalytics。打包时须把 MyTextListener.js 加为外部引用。
    /// pc.MyTextListener 是构造函数，new 安全（区别于 window）。
    /// </summary>
    [External]
    [Name("pc.MyTextListener")]
    public class MyTextListener
    {
        public extern void onInitPlayable();
        public extern void onLoaded();
        public extern void onDisplay();
        public extern void gameProgress(int progress);
        public extern void onGameOver();
        public extern void onRetry();
        public extern void onInstall();
        public extern void onCompleted();
    }

    /// <summary>
    /// 读取外层打包器(soyoo / sass-playable)注入 index.html &lt;head&gt; 的渠道标记：
    ///   &lt;script&gt;window.currentChannel="google"&lt;/script&gt;
    /// 引擎启动前就赋值，运行时可读。同 VibrationBridge 一样用 [External]+[Name] 桥接；[Name("window")] 映射 JS 全局 window。
    /// 静态属性 -&gt; 转译成 window.currentChannel（直接成员访问，不会走 new window()）。
    /// </summary>
    [External]
    [Name("window")]
    public static class LunaBrowserGlobals
    {
        public static extern string currentChannel { get; }
    }
#endif

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │  LunaManager — Luna 广告可玩生命周期管理器                               │
    // │                                                                         │
    // │  【跳转类型（JumpType）】                                                │
    // │    Default        — 默认，GameOver 时仅展示结束卡                        │
    // │    GuideJump      — 引导跳转，GameStart 启动引导手指                     │
    // │    EndForceJump   — 结束强制跳转，GameOver 时直接 GotoStore              │
    // │    Progress75Jump — 进度跳转，进度 ≥75% 时自动 GotoStore                │
    // │                                                                         │
    // │  【游戏流程调用】                                                         │
    // │    LunaManager.instance.GameStart();          // 首次点击后调用          │
    // │    LunaManager.instance.GameUpdated(progress);// 进度更新 0-100          │
    // │    LunaManager.instance.GameOver();           // 游戏结束                │
    // │                                                                         │
    // │  【快捷操作】                                                             │
    // │    LunaManager.instance.ReLoad();             // 重载场景                │
    // │    LunaManager.instance.GotoStore();          // 跳转商店                │
    // │                                                                         │
    // │  【震动】                                                                 │
    // │    LunaManager.instance.Vibrate(200);         // 震动 200ms              │
    // │    LunaManager.instance.VibratePattern(..);   // 模式震动                │
    // │    LunaManager.instance.StopVibration();      // 停止震动                │
    // │                                                                         │
    // │  【打点】走 MyTextListener → window.playableAnalytics（AppLovin）         │
    // └─────────────────────────────────────────────────────────────────────────┘
    public class LunaManager : MonoBehaviour
    {
        #region Inspector 配置

        public bool isGameStart;
        public bool isGameOver;
        [Header("跳转模式")]
        [LunaPlaygroundField("跳转模式", 0, "Game Settings")]
       public JumpType jumpType = JumpType.Default;

        [Header("编辑器渠道模拟（仅编辑器测试用；Luna 导出包始终读真实 window.currentChannel）")]
        public EditorChannel editorChannel = EditorChannel.Google;
       
        #endregion

        #region 单例

        public static LunaManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var go = new GameObject("LunaManager");
                    s_Instance = go.AddComponent<LunaManager>();
                    DontDestroyOnLoad(go);
                }
                return s_Instance;
            }
        }
        private static LunaManager s_Instance;

        #endregion

        #region 事件

        public event Action SceneResetEvent;

        #endregion

        #region 私有字段

        private int _lastReportedProgress;   // 进度打点去重：只上报更高的里程碑
        private bool _hasDisplayed;          // onDisplay 只发一次

        #endregion

        #region 生命周期

        public virtual void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                s_Instance.gameObject.name = s_Instance.GetType().Name;
            }
#if !UNITY_LUNA
            s_EditorChannel = editorChannel;
#endif
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
            ReportLoaded();
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

        #endregion

        #region 公共 API

        public void GameStart()
        {
            isGameStart = true;
            if (!_hasDisplayed)
            {
                _hasDisplayed = true;
                ReportDisplay();
            }
        }

        public void GameUpdated(int progress)
        {
            if (progress > _lastReportedProgress)
            {
                _lastReportedProgress = progress;
                ReportProgress(progress);
            }
            if (jumpType == JumpType.Progress75Jump && progress >= 75)
                GotoStore();
        }

        public void GameOver()
        {
            isGameOver = true;
            if (jumpType == JumpType.EndForceJump)
                GotoStore();
            ReportGameOver();
        }

        public void ReLoad()
        {
            if (SceneResetEvent != null)
                SceneResetEvent.Invoke();
            ReportRetry();
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }

        public void GotoStore()
        {
            Debug.Log("[LunaManager] GotoStore called");
            ReportInstall();
#if UNITY_LUNA
            Luna.Unity.Playable.InstallFullGame();
#endif
        }

        #endregion

        #region 震动

        public void Vibrate(int milliseconds = 200)
        {
#if UNITY_LUNA
            var bridge = new VibrationBridge();
            if (bridge.isSupported())
                bridge.vibrate(milliseconds);
#else
            Debug.Log($"[Vibration] Vibrate {milliseconds}ms");
#endif
        }

        public void VibratePattern(int[] pattern)
        {
#if UNITY_LUNA
            var bridge = new VibrationBridge();
            if (bridge.isSupported())
                bridge.vibratePattern(pattern);
#else
            Debug.Log($"[Vibration] VibratePattern [{string.Join(",", pattern)}]");
#endif
        }

        public void StopVibration()
        {
#if UNITY_LUNA
            var bridge = new VibrationBridge();
            if (bridge.isSupported())
                bridge.stop();
#else
            Debug.Log("[Vibration] Stop");
#endif
        }

        public bool IsVibrationSupported()
        {
#if UNITY_LUNA
            var bridge = new VibrationBridge();
            return bridge.isSupported();
#else
            return false;
#endif
        }

        #endregion

        #region 渠道判断（Channel）

#if !UNITY_LUNA
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
#endif

        /// <summary>
        /// 当前渠道名。Luna 包内读真实 window.currentChannel（google/facebook/...）；
        /// 编辑器里返回 Inspector 上 editorChannel 模拟的渠道名，方便不打包测试。
        ///
        /// 【别再踩的坑】
        ///   1. UnityEngine.Application.buildPlatform —— 被 Luna 固化成导出时的 Unity BuildTarget
        ///      （如 "StandaloneWindows64"），任何渠道都一样，与分发渠道无关。
        ///   2. Luna.Unity.Playable.Platform —— 是“广告网络”(unityads/applovin/...)，非分发渠道。
        /// 只有 window.currentChannel 才是外层按渠道打包时写死的真值。
        /// </summary>
        public static string CurrentName()
        {
#if UNITY_LUNA
            string ch = LunaBrowserGlobals.currentChannel;
            return string.IsNullOrEmpty(ch) ? "Unknown" : ch;
#else
            return EditorChannelToName(s_EditorChannel);
#endif
        }

        /// <summary>是否谷歌渠道。可直接控制显隐：obj.SetActive(LunaManager.IsGoogle())。
        /// 编辑器里由 Inspector 的 editorChannel 决定，包里由真实渠道决定。</summary>
        public static bool IsGoogle()
        {
            // 渠道值形如 "google"（"google-manager" 等变体 StartsWith 一并覆盖）。
            // 编辑器与包共用同一判断：CurrentName() 已分别返回模拟值/真实值。
            return CurrentName().StartsWith("google");
        }

        /// <summary>打印当前渠道。打包后在浏览器 console 里能看到这行。</summary>
        public static void LogCurrent()
        {
            Debug.Log("[LunaManager] 当前渠道 = " + CurrentName() + " , IsGoogle = " + IsGoogle());
        }

        #endregion

        #region 打点（MyTextListener → window.playableAnalytics）

#if UNITY_LUNA
        private MyTextListener _listener;
        private MyTextListener Listener
        {
            get
            {
                if (_listener == null)
                    _listener = new MyTextListener();
                return _listener;
            }
        }
#endif

        private void ReportLoaded()
        {
#if UNITY_LUNA
            try
            {
                Listener.onInitPlayable();
                Listener.onLoaded();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Analytics] onLoaded 失败: " + e.Message);
            }
#endif
        }

        private void ReportDisplay()
        {
#if UNITY_LUNA
            try
            {
                Listener.onDisplay();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Analytics] onDisplay 失败: " + e.Message);
            }
#endif
        }

        private void ReportProgress(int progress)
        {
#if UNITY_LUNA
            try
            {
                Listener.gameProgress(progress);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Analytics] gameProgress 失败: " + e.Message);
            }
#endif
        }

        private void ReportGameOver()
        {
#if UNITY_LUNA
            try
            {
                Listener.onCompleted();
                Listener.onGameOver();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Analytics] onGameOver 失败: " + e.Message);
            }
#endif
        }

        private void ReportRetry()
        {
#if UNITY_LUNA
            try
            {
                Listener.onRetry();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Analytics] onRetry 失败: " + e.Message);
            }
#endif
        }

        private void ReportInstall()
        {
#if UNITY_LUNA
            try
            {
                Listener.onInstall();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Analytics] onInstall 失败: " + e.Message);
            }
#endif
        }

        #endregion
    }
}
