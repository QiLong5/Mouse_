using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_LUNA
using Bridge;
[External][Name("window.ALPlayableAnalytics")]
#endif
public class LunaAnalytics
{
    /// <summary>
    /// 追踪自定义事件
    /// </summary>
    /// <param name="eventName">事件名称，例如 "ENDCARD_SHOWN"</param>
    public extern void trackEvent(string eventName);

    // 进度追踪常量
    public const string CHALLENGE_STARTED = "CHALLENGE_STARTED";
    public const string CHALLENGE_PASS_25 = "CHALLENGE_PASS_25";
    public const string CHALLENGE_PASS_50 = "CHALLENGE_PASS_50";
    public const string CHALLENGE_PASS_75 = "CHALLENGE_PASS_75";
    public const string CHALLENGE_SOLVED = "CHALLENGE_SOLVED";
    public const string CHALLENGE_FAILED = "CHALLENGE_FAILED";
    public const string ENDCARD_SHOWN = "ENDCARD_SHOWN";
}
