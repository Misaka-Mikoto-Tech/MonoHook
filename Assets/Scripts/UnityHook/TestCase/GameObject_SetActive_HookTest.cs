#if ENABLE_HOOK_TEST_CASE
/*
 * 对 GameObject.SetActive 进行hook的测试用例
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

//[InitializeOnLoad]
public static class GameObject_SetActive_HookTest
{
    private static MethodHook _hook;

    static GameObject_SetActive_HookTest()
    {
        Init();
    }

    public static void Test(GameObject go)
    {

    }

    public static void Init()
    {
        if (_hook == null)
        {
            Type type = typeof(GameObject).Assembly.GetType("UnityEngine.GameObject");

            MethodInfo miTarget = type.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public);

            type = typeof(GameObject_SetActive_HookTest);
            MethodInfo miReplacement = type.GetMethod("SetActiveNew", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo miProxy = type.GetMethod("SetActiveProxy", BindingFlags.Static | BindingFlags.NonPublic);

            _hook = new MethodHook(miTarget, miReplacement, miProxy);
            _hook.Install();
        }
    }

    private static void SetActiveNew(GameObject go, bool value)
    {
        SetActiveProxy(go, value);
        Debug.LogFormat("GameObject [{0}] SetActive({1})", go.name, value);
    }

    private static void SetActiveProxy(GameObject go, bool value)
    {
        // dummy
    }
}
#endif