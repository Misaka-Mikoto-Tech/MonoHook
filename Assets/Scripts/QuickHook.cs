using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using MonoHook;

public class QuickHook
{
    Dictionary<string, List<MethodHook>> hookMethodDic;

    Type type;
    Func<string, string> replaceNameFunc;
    Func<string, string> proxyNameFunc;

    public QuickHook(Type type)
    {
        this.type = type;
        hookMethodDic = new Dictionary<string, List<MethodHook>>();
        replaceNameFunc = x => x;
        proxyNameFunc = x => x + "Proxy";
    }

    public QuickHook(Type type, Func<string, string> replaceNameFunc, Func<string, string> proxyNameFunc) : this(type)
    {
        this.replaceNameFunc = replaceNameFunc;
        this.proxyNameFunc = proxyNameFunc;
    }

    public bool HookMethod(Type hookType, string hookMethod, BindingFlags hookFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, BindingFlags selfFlags = BindingFlags.Static | BindingFlags.NonPublic)
    {
        if (hookMethodDic.TryGetValue(hookMethod, out var hooks))
            return false;

        hooks = new List<MethodHook>();
        hookMethodDic.Add(hookMethod, hooks);

        var originalMethods = hookType.GetMethods(hookFlags).Where(method => method.Name == hookMethod).ToList();
        var replaceMethods = type.GetMethods(selfFlags).Where(method => method.Name == replaceNameFunc(hookMethod)).ToList();
        var proxyMethods = type.GetMethods(selfFlags).Where(method => method.Name == proxyNameFunc(hookMethod)).ToList();

        foreach (var originalMethod in originalMethods)
        {
            var replaceMethod = FindMethodsBySignature(replaceMethods, originalMethod);
            var proxyMethod = FindMethodsBySignature(proxyMethods, originalMethod);
            if (replaceMethod != null && proxyMethod != null)
            {
                hooks.Add(new MethodHook(originalMethod, replaceMethod, proxyMethod));
            }
        }

        foreach (var item in hooks)
        {
            item.Install();
        }

        return hooks.Count > 0;
    }

    public void UnHookMethod(string methodName)
    {
        if (hookMethodDic.Remove(methodName, out var hooks))
        {
            foreach (var item in hooks)
            {
                item.Uninstall();
            }
        }
    }

    public void UnHookAll()
    {
        foreach (var hooks in hookMethodDic.Values)
        {
            foreach (var item in hooks)
            {
                item.Uninstall();
            }
        }

        hookMethodDic.Clear();
    }

    MethodInfo FindMethodsBySignature(List<MethodInfo> methods, MethodInfo compareMethod)
    {
        var compareParams = compareMethod.GetParameters();
        foreach (var method in methods)
        {
            if (compareMethod.ReturnType == method.ReturnType)
            {
                var @params = method.GetParameters();
                if (@params.Length == compareParams.Length)
                {
                    var isMatch = true;
                    for (int i = 0; i < @params.Length; i++)
                    {
                        if (@params[i].ParameterType != compareParams[i].ParameterType)
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    if (isMatch)
                    {
                        return method;
                    }
                }
            }
        }

        return null;
    }
}
