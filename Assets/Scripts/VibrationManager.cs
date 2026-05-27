using UnityEngine;

#if UNITY_LUNA
using Bridge;

[External]
[Name("VibrationBridge")]
public class VibrationBridge
{
    public extern void vibrate(int duration);
    public extern void vibratePattern(int[] pattern);
    public extern void stop();
    public extern bool isSupported();
}
#endif

public static class VibrationManager
{
    public static void Vibrate(int milliseconds = 200)
    {
#if UNITY_LUNA
        var bridge = new VibrationBridge();
        if (bridge.isSupported())
            bridge.vibrate(milliseconds);
#else
        Debug.Log($"[Vibration] Vibrate {milliseconds}ms");
#endif
    }

    public static void VibratePattern(int[] pattern)
    {
#if UNITY_LUNA
        var bridge = new VibrationBridge();
        if (bridge.isSupported())
            bridge.vibratePattern(pattern);
#else
        Debug.Log($"[Vibration] VibratePattern [{string.Join(",", pattern)}]");
#endif
    }

    public static void Stop()
    {
#if UNITY_LUNA
        var bridge = new VibrationBridge();
        if (bridge.isSupported())
            bridge.stop();
#else
        Debug.Log("[Vibration] Stop");
#endif
    }

    public static bool IsSupported()
    {
#if UNITY_LUNA
        var bridge = new VibrationBridge();
        return bridge.isSupported();
#else
        return false;
#endif
    }
}
