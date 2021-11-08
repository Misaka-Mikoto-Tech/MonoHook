using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Hook 池，防止重复 Hook
/// </summary>
public static class HookPool
{
    private static Dictionary<MethodBase, MethodHook> _hooks = new Dictionary<MethodBase, MethodHook>();

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

    public static void RemoveHook(MethodBase method)
    {
        _hooks.Remove(method);
    }
}
