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

    [External]
    [Name("pc.MyTextListener")]
    public class MyTextListener
    {
        public extern string onInitPlayable();
        public extern string onLoaded();
        public extern string onDisplay();
        public extern string gameProgress(int progress);
        public extern string onGameOver();
        public extern string onRetry();
        public extern string onInstall();
        public extern string onCompleted();
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
    // └─────────────────────────────────────────────────────────────────────────┘
    public class LunaManager : MonoBehaviour
    {
        #region Inspector 配置

        public bool isGameStart;
        public bool isGameOver;
        public JumpType jumpType = JumpType.Default;

        #endregion

        #region 单例

        public static LunaManager instance { get; private set; }

        #endregion

        #region 事件

        public event Action SceneResetEvent;

        #endregion

        #region 私有字段

#if UNITY_LUNA
        private MyTextListener _listener = new MyTextListener();
#endif
        private bool _hasReported25;
        private bool _hasReported50;
        private bool _hasReported75;
        private bool _hasReported100;

        #endregion

        #region 生命周期

        public virtual void Awake()
        {
            if (instance == null) instance = this;
        }

        private void Start()
        {
#if UNITY_LUNA
            Luna.Unity.LifeCycle.GameLoaded();
#endif
            AudioListener.volume = 0;

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

        #endregion

        #region 公共 API

        public void GameStart()
        {
            isGameStart = true;
#if UNITY_LUNA
            Luna.Unity.LifeCycle.GameStarted();
#endif
            mGameProgress(0);
        }

        public void GameUpdated(int progress)
        {
            if (progress >= 25 && !_hasReported25)
            {
                _hasReported25 = true;
                mGameProgress(25);
            }
            if (progress >= 50 && !_hasReported50)
            {
                _hasReported50 = true;
                mGameProgress(50);
            }
            if (progress >= 75 && !_hasReported75)
            {
                _hasReported75 = true;
                mGameProgress(75);
            }
            if (progress >= 100 && !_hasReported100)
            {
                _hasReported100 = true;
                mGameProgress(100);
            }

            if (jumpType == JumpType.Progress75Jump && progress >= 75)
                GotoStore();
        }

        public void GameOver()
        {
            if (isGameOver) return;
            isGameOver = true;
#if UNITY_LUNA
            Luna.Unity.LifeCycle.GameEnded(new object[] { "result", "win" });
#endif
            if (jumpType == JumpType.EndForceJump)
                GotoStore();

            mOnShowEndCard();
            mOnCompleted();
        }

        public void ReLoad()
        {
            SceneResetEvent?.Invoke();
            mOnRetry();
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }

        public void GotoStore()
        {
            Debug.Log("[LunaManager] GotoStore called");
            mOnInstall();
#if UNITY_LUNA
            Luna.Unity.Playable.InstallFullGame();
#endif
        }

        #endregion

        #region 打点（MyTextListener 转发）

        public void mOnInitPlayable()
        {
#if UNITY_LUNA
            try { _listener.onInitPlayable(); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] onInitPlayable 失败: {e.Message}"); }
#else
            Debug.Log("[Analytics] onInitPlayable");
#endif
        }

        public void mOnLoaded()
        {
#if UNITY_LUNA
            try { _listener.onLoaded(); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] onLoaded 失败: {e.Message}"); }
#else
            Debug.Log("[Analytics] onLoaded");
#endif
        }

        public void mOnDisplay()
        {
#if UNITY_LUNA
            try { _listener.onDisplay(); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] onDisplay 失败: {e.Message}"); }
#else
            Debug.Log("[Analytics] onDisplay");
#endif
        }

        public void mGameProgress(int progress)
        {
#if UNITY_LUNA
            try { _listener.gameProgress(progress); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] gameProgress({progress}) 失败: {e.Message}"); }
#else
            Debug.Log($"[Analytics] gameProgress {progress}");
#endif
        }

        public void mOnShowEndCard()
        {
#if UNITY_LUNA
            try { _listener.onGameOver(); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] onGameOver 失败: {e.Message}"); }
#else
            Debug.Log("[Analytics] onShowEndCard");
#endif
        }

        public void mOnRetry()
        {
#if UNITY_LUNA
            try { _listener.onRetry(); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] onRetry 失败: {e.Message}"); }
#else
            Debug.Log("[Analytics] onRetry");
#endif
        }

        public void mOnInstall()
        {
#if UNITY_LUNA
            try { _listener.onInstall(); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] onInstall 失败: {e.Message}"); }
#else
            Debug.Log("[Analytics] onInstall");
#endif
        }

        public void mOnCompleted()
        {
#if UNITY_LUNA
            try { _listener.onCompleted(); }
            catch (Exception e) { Debug.LogWarning($"[Analytics] onCompleted 失败: {e.Message}"); }
#else
            Debug.Log("[Analytics] onCompleted");
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
    }
}
