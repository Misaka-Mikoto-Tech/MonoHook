using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Hook 池，防止重复 Hook
/// </summary>
public static class HookPool
{
    private static Dictionary<MethodBase, MethodHook> _hookers = new Dictionary<MethodBase, MethodHook>();

    public static void AddHooker(MethodBase method, MethodHook hooker)
    {
        MethodHook preHooker;
        if (_hookers.TryGetValue(method, out preHooker))
        {
            preHooker.Uninstall();
            _hookers[method] = hooker;
        }
        else
            _hookers.Add(method, hooker);
    }

    public static void RemoveHooker(MethodBase method)
    {
        _hookers.Remove(method);
    }
}
