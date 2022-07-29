using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Hook 池，防止重复 Hook
/// </summary>
public static class HookPool
{
    private static Dictionary<MethodBase, MethodHook> _hooks = new Dictionary<MethodBase, MethodHook>();

#if UNITY_EDITOR
    static HookPool()
    {
        EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
    }

    static void EditorApplication_playModeStateChanged(PlayModeStateChange act)
    {
        switch(act)
        {
            case PlayModeStateChange.ExitingPlayMode:
                /*
                 * Unity2021.3.1f1 之后即使勾选 Enter Play Mode Option->Reload Domains 二次进入PlayMode时也不会全部被Reload, 导致会被Hook多次
                 * TODO 检测是否重复Hook的模式修改为判断目标函数二进制是否与Hook代码相同
                 */
                // Debug.Log("退出播放前移除所有Hook，避免下次Play时Unity没有全部Reload导致多次Hook导致Crash");
                UninstallAll();
                break;
        }
    }
#endif

    public static void AddHook(MethodBase method, MethodHook hook)
    {
        MethodHook preHook;
        if (_hooks.TryGetValue(method, out preHook))
        {
            preHook.Uninstall();
            _hooks[method] = hook;
        }
        else
            _hooks.Add(method, hook);
    }

    public static MethodHook GetHook(MethodBase method)
    {
        MethodHook hook;
        if (_hooks.TryGetValue(method, out hook))
            return hook;
        return null;
    }

    public static void RemoveHooker(MethodBase method)
    {
        _hooks.Remove(method);
    }

    public static void UninstallAll()
    {
        var list = _hooks.Values.ToList();
        foreach (var hook in list)
            hook.Uninstall();

        _hooks.Clear();
    }
}
