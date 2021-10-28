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

    public static void AddHooker(MethodBase method, MethodHook hooker)
    {
        MethodHook preHooker;
        if (_hooks.TryGetValue(method, out preHooker))
        {
            preHooker.Uninstall();
            _hooks[method] = hooker;
        }
        else
            _hooks.Add(method, hooker);
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
}
