#if UNITY_4_7

using UnityEngine;
using System.Collections;

public static class Debug
{
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    public static void LogFormat(string format, params object[] args)
    {
        UnityEngine.Debug.Log(string.Format(format, args));
    }

    public static void Assert(bool condition)
    {
        if (!condition)
            UnityEngine.Debug.LogError("assert fail");
    }
}
#endif