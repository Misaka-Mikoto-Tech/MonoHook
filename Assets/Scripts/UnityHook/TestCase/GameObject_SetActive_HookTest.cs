#if ENABLE_HOOK_TEST_CASE
/*
 * 对 GameObject.SetActive 进行hook的测试用例
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace MonoHook.Test
{
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
            go.SetActive(false);
            Debug.Assert(s_testVal == 0);
            go.SetActive(true);
            Debug.Assert(s_testVal == 1);
        }

        public static void Init()
        {
            if (_hook == null)
            {
                Type type = typeof(GameObject).Assembly.GetType("UnityEngine.GameObject");
                MethodInfo miTarget = type.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public);

                MethodInfo miReplacement = new Action<GameObject, bool>(SetActiveNew).Method;
                MethodInfo miProxy = new Action<GameObject, bool>(SetActiveProxy).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        static int s_testVal = -1;

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void SetActiveNew(GameObject go, bool value)
        {
            SetActiveProxy(go, value);
            Debug.LogFormat("GameObject [{0}] SetActive({1})", go.name, value);
            s_testVal = value ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void SetActiveProxy(GameObject go, bool value)
        {
            // dummy code
            Debug.Log("something" + go.ToString());
        }
    }
}
#endif