using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Hooker 池，防止重复 Hook
/// </summary>
public static class HookerPool
{
    private static Dictionary<MethodInfo, MethodHooker> _hookers = new Dictionary<MethodInfo, MethodHooker>();

    public static void AddHooker(MethodInfo method, MethodHooker hooker)
    {
        MethodHooker preHooker;
        if (_hookers.TryGetValue(method, out preHooker))
        {
            preHooker.Uninstall();
            _hookers[method] = hooker;
        }
        else
            _hookers.Add(method, hooker);
    }

    public static void RemoveHooker(MethodInfo method)
    {
        _hookers.Remove(method);
    }
}
