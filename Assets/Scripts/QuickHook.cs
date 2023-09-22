using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using MonoHook;

public class QuickHook
{
    enum HookID
    {
        Ctor,
        Method,
        Property,
    }

    Dictionary<Type, Dictionary<string, List<MethodHook>>> hookDic;

    Type type;
    Func<string, string> replaceNameFunc;
    Func<string, string> proxyNameFunc;
    Func<string, string> setterNameFunc;
    Func<string, string> getterNameFunc;
    BindingFlags hookFlags;
    BindingFlags selfFlags;

    public QuickHook(Type type, BindingFlags? hookFlags = null, BindingFlags? selfFlags = null)
    {
        this.type = type;
        hookDic = new Dictionary<Type, Dictionary<string, List<MethodHook>>>();
        replaceNameFunc = x => x;
        proxyNameFunc = x => x + "Proxy";
        setterNameFunc = x => "set_" + x;
        getterNameFunc = x => "get_" + x;

        var defaultFlags = BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        this.hookFlags = hookFlags ?? defaultFlags;
        this.selfFlags = selfFlags ?? defaultFlags;
    }

    public QuickHook(object inst) : this(inst.GetType())
    {
    }

    public QuickHook(Type type, Func<string, string> replaceNameFunc, Func<string, string> proxyNameFunc,
        Func<string, string> setterNameFunc, Func<string, string> getterNameFunc,
        BindingFlags? hookFlags = null, BindingFlags? selfFlags = null) : this(type, hookFlags, selfFlags)
    {
        this.replaceNameFunc = replaceNameFunc;
        this.proxyNameFunc = proxyNameFunc;
        this.setterNameFunc = setterNameFunc;
        this.getterNameFunc = getterNameFunc;
    }

    public int HookCtor(Type hookType, string replaceName = null, string proxyName = null)
    {
        var counter = 0;
        while (Internal_Hook(hookType, HookID.Ctor, string.Empty, replaceName ?? replaceNameFunc("Ctor"), proxyName ?? proxyNameFunc("Ctor"), counter > 0))
        {
            counter++;
        }

        return counter;
    }

    public int HookMethod(Type hookType, string hookMethod, string replaceName = null, string proxyName = null)
    {
        var counter = 0;
        while (Internal_Hook(hookType, HookID.Method, hookMethod, replaceName ?? replaceNameFunc(hookMethod),
            proxyName ?? proxyNameFunc(hookMethod), counter > 0))
        {
            counter++;
        }

        return counter;
    }

    public int HookProperty(Type hookType, string hookProperty, string replaceName = null, string proxyName = null)
    {
        var counter = 0;
        while (Internal_Hook(hookType, HookID.Property, hookProperty, replaceName ?? replaceNameFunc(hookProperty),
            proxyName ?? proxyNameFunc(hookProperty), counter > 0))
        {
            counter++;
        }

        return counter;
    }

    public void UnHookCtor(Type hookType)
    {
        UnHookMethod(hookType, string.Empty);
    }

    public void UnHookMethod(Type hookType, string hookMethod)
    {
        if (hookDic.TryGetValue(hookType, out var typeToHooks) && typeToHooks.Remove(hookMethod, out var hooks))
        {
            foreach (var item in hooks)
            {
                item.Uninstall();
            }
        }
    }

    public void UnHookProperty(Type hookType, string hookProperty)
    {
        if (hookDic.TryGetValue(hookType, out var typeToHooks) && typeToHooks.Remove(hookProperty, out var hooks))
        {
            foreach (var item in hooks)
            {
                item.Uninstall();
            }
        }
    }

    public void UnHookAll()
    {
        foreach (var typeToHooks in hookDic.Values)
        {
            foreach (var hooks in typeToHooks.Values)
            {
                foreach (var item in hooks)
                    item.Uninstall();
            }
        }

        hookDic.Clear();
    }

    bool Internal_Hook(Type hookType, HookID hookID, string hookMember, string replaceName, string proxyName, bool ignoreError = false)
    {
        if (!hookDic.TryGetValue(hookType, out var typeToHooks))
        {
            typeToHooks = new Dictionary<string, List<MethodHook>>();
            hookDic.Add(hookType, typeToHooks);
        }

        if (!typeToHooks.TryGetValue(hookMember, out var hooks))
        {
            hooks = new List<MethodHook>();
            typeToHooks.Add(hookMember, hooks);
        }

        var originalMethods = default(List<MethodBase>);
        switch (hookID)
        {
            case HookID.Ctor:
                originalMethods = hookType.GetConstructors(hookFlags).ToList().ConvertAll(x => (MethodBase)x);
                break;
            case HookID.Method:
                originalMethods = hookType.GetMethods(hookFlags).Where(x => x.Name == hookMember && !x.IsGenericMethod).ToList().ConvertAll(x => (MethodBase)x);
                break;
            case HookID.Property:
                var properties = hookType.GetProperties(hookFlags).Where(x => x.Name == hookMember).ToList();
                originalMethods = new List<MethodBase>(properties.Count * 2);
                foreach (var property in properties)
                {
                    if (property.SetMethod != null)
                        originalMethods.Add(property.SetMethod);
                    if (property.GetMethod != null)
                        originalMethods.Add(property.GetMethod);
                }
                break;
        }

        foreach (var hook in hooks)
            originalMethods.Remove(hook.targetMethod);

        if (originalMethods.Count == 0)
        {
            if (!ignoreError)
            {
                switch (hookID)
                {
                    case HookID.Ctor:
                        Debug.LogError($"Failed to hook {hookID}. No constructor found in type {hookType}.");
                        break;
                    case HookID.Method:
                        Debug.LogError($"Failed to hook {hookID}. No method {hookMember} found in type {hookType}.");
                        break;
                    case HookID.Property:
                        Debug.LogError($"Failed to hook {hookID}. No property {hookMember} found in type {hookType}.");
                        break;
                    default:
                        Debug.LogError($"Failed to hook {hookID}.");
                        break;
                }
            }

            return false;
        }

        var methodHook = default(MethodHook);
        var replaceMethods = default(List<MethodInfo>);
        var proxyMethods = default(List<MethodInfo>);

        switch (hookID)
        {
            case HookID.Ctor:
            case HookID.Method:
                {
                    replaceMethods = type.GetMethods(selfFlags).Where(x => x.Name == replaceName).ToList();
                    proxyMethods = type.GetMethods(selfFlags).Where(x => x.Name == proxyName).ToList();
                }
                break;
            case HookID.Property:
                {
                    // 查找属性
                    var replaceProperties = type.GetProperties(selfFlags).Where(x => x.Name == replaceName).ToList();
                    var proxyProperties = type.GetProperties(selfFlags).Where(x => x.Name == proxyName).ToList();

                    replaceMethods = new List<MethodInfo>(replaceProperties.Count * 2);
                    proxyMethods = new List<MethodInfo>(proxyProperties.Count * 2);

                    foreach (var item in replaceProperties)
                    {
                        if (item.SetMethod != null)
                            replaceMethods.Add(item.SetMethod);
                        if (item.GetMethod != null)
                            replaceMethods.Add(item.GetMethod);
                    }

                    foreach (var item in proxyProperties)
                    {
                        if (item.SetMethod != null)
                            proxyMethods.Add(item.SetMethod);
                        if (item.GetMethod != null)
                            proxyMethods.Add(item.GetMethod);
                    }

                    // 查找方法
                    if (replaceMethods.Count != proxyMethods.Count || replaceMethods.Count == 0)
                    {
                        var setReplaceMethods = type.GetMethods(selfFlags).Where(x => x.Name == setterNameFunc(replaceName)).ToList();
                        var getReplaceMethods = type.GetMethods(selfFlags).Where(x => x.Name == getterNameFunc(replaceName)).ToList();

                        var setProxyMethods = type.GetMethods(selfFlags).Where(x => x.Name == setterNameFunc(proxyName)).ToList();
                        var getProxyMethods = type.GetMethods(selfFlags).Where(x => x.Name == getterNameFunc(proxyName)).ToList();

                        replaceMethods = new List<MethodInfo>(setReplaceMethods.Count * 2);
                        proxyMethods = new List<MethodInfo>(setProxyMethods.Count * 2);

                        replaceMethods.AddRange(setReplaceMethods);
                        replaceMethods.AddRange(getReplaceMethods);

                        proxyMethods.AddRange(setProxyMethods);
                        proxyMethods.AddRange(getProxyMethods);
                    }
                }
                break;
        }

        foreach (var originalMethod in originalMethods)
        {
            var replaceMethod = FindMethodBySignature(hookID, replaceMethods, originalMethod);
            var proxyMethod = FindMethodBySignature(hookID, proxyMethods, originalMethod);
            if (replaceMethod != null && proxyMethod != null)
            {
                methodHook = new MethodHook(originalMethod, replaceMethod, proxyMethod);
                switch (hookID)
                {
                    case HookID.Ctor:
                        Debug.Log($"Successfully hooked {hookID}. {hookType} ({originalMethod}) -> {type} ({replaceMethod}),");
                        break;
                    case HookID.Method:
                        Debug.Log($"Successfully hooked {hookID}. {hookType} ({originalMethod}) -> {type} ({replaceMethod}),");
                        break;
                    case HookID.Property:
                        Debug.Log($"Successfully hooked {hookID} . {hookType} ({originalMethod}) -> {type} ({replaceMethod}),");
                        break;
                    default:
                        Debug.Log($"Successfully hooked {hookID}.");
                        break;
                }
                break;
            }
        }

        if (methodHook == null)
        {
            if (!ignoreError)
            {
                Debug.LogError($"Failed to hook {hookID}. No method {replaceName} or {proxyName} found in type {type}, or their signatures do not match.");
            }

            return false;
        }
        else
        {
            methodHook.Install();
            hooks.Add(methodHook);
            return true;
        }
    }

    MethodInfo FindMethodBySignature(HookID hookID, List<MethodInfo> methods, MethodBase originalMethod)
    {
        if (methods?.Count == 0)
            return null;

        Type compareReturnType = null;
        switch (hookID)
        {
            case HookID.Ctor:
                compareReturnType = typeof(void);
                break;
            case HookID.Method:
            case HookID.Property:
                compareReturnType = ((MethodInfo)originalMethod).ReturnType;
                break;
        }

        // 查找实例方法
        var compareParamTypes = originalMethod.GetParameters().ToList().ConvertAll(x => x.ParameterType);
        var method = FindMethodBySignature(methods, compareParamTypes, compareReturnType, true);
        if (method != null)
            return method;

        // 查找静态方法
        if (!originalMethod.IsStatic)
            compareParamTypes.Insert(0, originalMethod.DeclaringType);

        method = FindMethodBySignature(methods, compareParamTypes, compareReturnType, false);
        if (method != null)
            return method;

        return null;
    }

    MethodInfo FindMethodBySignature(List<MethodInfo> methods, List<Type> compareParamTypes, Type compareReturnType, bool searchInstance)
    {
        foreach (var method in methods)
        {
            if (searchInstance && method.IsStatic)
                continue;

            if (TypeIsEqual(method.ReturnType, compareReturnType))
            {
                var @params = method.GetParameters();
                if (@params.Length == compareParamTypes?.Count)
                {
                    var isMatch = true;
                    for (int i = 0; i < @params.Length; i++)
                    {
                        if (!TypeIsEqual(@params[i].ParameterType, compareParamTypes[i]))
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

    bool TypeIsEqual(Type x, Type y)
    {
        if (x == y)
            return true;

        if (x.IsAssignableFrom(y))
            return true;

        return false;
    }
}
