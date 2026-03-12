using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class LunaManager :MonoBehaviour
{
    public bool isGameStart;
    public bool isGameOver;


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
    }
    private void Start()
    {
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
       // Luna.Unity.LifeCycle.GameEnded();
    }


    /// <summary>
    /// 重新加载场景
    /// </summary>
    public void ReLoad()
    {
        SceneResetEvent?.Invoke();
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
  
    /// <summary>
    /// 去商店
    /// </summary>
    public void GotoStore()
    {
       // Luna.Unity.Playable.InstallFullGame();
    }

    public void GameStart()
    {
        //AudioManager.instance.mBgm.Play();
        isGameStart = true;
    }
}
