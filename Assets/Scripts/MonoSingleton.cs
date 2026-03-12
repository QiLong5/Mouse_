using System;
using UnityEngine;
/// <summary>
/// MonoBehaviour单例
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T instance
    {
        get
        {
            return s_Instance;
        }
    }
    private static T s_Instance;

    public virtual void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = (T)this;
            s_Instance.gameObject.name = s_Instance.GetType().Name;
        }

       
    }
    private void Start()
    {
        LunaManager.instance.SceneResetEvent += OnSceneReset;
    }
    void OnSceneReset()
    {
        LunaManager.instance.SceneResetEvent -= OnSceneReset;

        // 销毁单例实例
        if (s_Instance == this)
        {
            Debug.Log("destory" + this.name);
            s_Instance = null;
            Destroy(gameObject);
        }
    }
}