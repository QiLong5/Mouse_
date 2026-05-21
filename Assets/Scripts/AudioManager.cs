using UnityEngine;
using System.Collections.Generic;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │  AudioManager — 配置化音效管理器                                         │
// │                                                                         │
// │  【AudioEntry 字段说明】                                                 │
// │    key            — 播放时引用的字符串标识                               │
// │    clip           — 音频文件                                             │
// │    volume / pitch — 音量 / 音调（默认 1）                               │
// │    loop           — 是否循环（默认关闭）                                 │
// │    pooled         — 是否池化（允许同类音效并发，内置 0.1s 冷却）          │
// │    maxPoolSize    — 池化并发上限（默认 5）                               │
// │    stopOnGameOver — 游戏结束时是否自动停止                               │
// │                                                                         │
// │  【播放 / 停止】                                                         │
// │    AudioManager.instance.Play("key");                                   │
// │    AudioManager.instance.Stop("key");                                   │
// │    AudioManager.instance.StopAllGameSounds(); // GameOver 时调用        │
// └─────────────────────────────────────────────────────────────────────────┘

[System.Serializable]
public class AudioEntry
{
    public string key;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public float pitch = 1f;
    public bool loop = false;
    public bool pooled = false;
    [Min(1)] public int maxPoolSize = 5;
    public bool stopOnGameOver = true;
}

public class AudioManager : MonoSingleton<AudioManager>
{
    #region Inspector 配置

    [Header("音效配置列表")]
    public List<AudioEntry> audioEntries = new List<AudioEntry>();

    #endregion

    #region 私有字段

    private const float COOLDOWN = 0.1f;
    private bool _gameOver;

    private class RuntimeEntry
    {
        public AudioEntry config;
        public AudioSource directSource;
        public List<AudioSource> pool;
        public float lastPlayTime;
    }

    private readonly Dictionary<string, RuntimeEntry> _entries = new Dictionary<string, RuntimeEntry>();

    #endregion

    #region 生命周期

    public override void Awake()
    {
        base.Awake();
        foreach (var entry in audioEntries)
        {
            if (string.IsNullOrEmpty(entry.key) || entry.clip == null) continue;
            var rt = new RuntimeEntry { config = entry };
            if (entry.pooled)
                rt.pool = new List<AudioSource>();
            else
                rt.directSource = CreateSource(entry);
            _entries[entry.key] = rt;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (audioEntries == null) return;
        foreach (var entry in audioEntries)
        {
            if (entry.volume == 0f) entry.volume = 1f;
            if (entry.pitch == 0f) entry.pitch = 1f;
            if (entry.maxPoolSize <= 0) entry.maxPoolSize = 5;
        }
    }
#endif

    #endregion

    #region 公共 API
    private void Play(string key)
    {
        if (!_entries.TryGetValue(key, out var rt))
        {
            Debug.LogWarning($"[AudioManager] 未找到 key: {key}");
            return;
        }
        if (_gameOver && rt.config.stopOnGameOver) return;
        if (rt.config.pooled) PlayPooled(rt);
        else if (rt.directSource != null) rt.directSource.Play();
    }

    public void Play(SK key, Vector3 pos = default)
    {
        if (pos != default)
        {
            var screenPos = Camera.main.WorldToScreenPoint(pos);
            if (screenPos.z < 0 || screenPos.x < 0 || screenPos.x > Screen.width ||
                screenPos.y < 0 || screenPos.y > Screen.height)
                return;
        }
        if (SKMap.Keys.TryGetValue(key, out string rawKey))
            Play(rawKey);
    }

    public void Stop(string key)
    {
        if (!_entries.TryGetValue(key, out var rt)) return;
        if (rt.config.pooled)
        {
            foreach (var src in rt.pool)
                if (src != null && src.isPlaying) src.Stop();
        }
        else
        {
            if (rt.directSource != null && rt.directSource.isPlaying)
                rt.directSource.Stop();
        }
    }

    public void StopAllGameSounds()
    {
        _gameOver = true;
        foreach (var rt in _entries.Values)
        {
            if (!rt.config.stopOnGameOver) continue;
            if (rt.config.pooled)
            {
                foreach (var src in rt.pool)
                    if (src != null && src.isPlaying) src.Stop();
            }
            else
            {
                if (rt.directSource != null && rt.directSource.isPlaying)
                    rt.directSource.Stop();
            }
        }
    }

    #endregion

    #region 私有方法

    private AudioSource CreateSource(AudioEntry entry)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.clip        = entry.clip;
        src.volume      = entry.volume;
        src.pitch       = entry.pitch;
        src.loop        = entry.loop;
        src.playOnAwake = false;
        return src;
    }

    private void PlayPooled(RuntimeEntry rt)
    {
        if (Time.time - rt.lastPlayTime < COOLDOWN) return;

        foreach (var src in rt.pool)
        {
            if (!src.isPlaying) { src.Play(); rt.lastPlayTime = Time.time; return; }
        }

        if (rt.pool.Count < rt.config.maxPoolSize)
        {
            var src = CreateSource(rt.config);
            rt.pool.Add(src);
            src.Play();
            rt.lastPlayTime = Time.time;
        }
    }

    #endregion
}
