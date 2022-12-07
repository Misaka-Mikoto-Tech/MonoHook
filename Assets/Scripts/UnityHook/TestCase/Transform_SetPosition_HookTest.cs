#if ENABLE_HOOK_TEST_CASE || true
/*
 * 对 Transform.SetPosition 进行hook的测试用例
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
    public static class Transform_SetPosition_HookTest
    {
        private static MethodHook _hook;

        static Transform_SetPosition_HookTest()
        {
            Init();
        }

        public static void Init()
        {
            if (_hook == null)
            {
                Type type = typeof(Transform);
                MethodInfo miTarget = type.GetMethod("set_position_Injected", BindingFlags.Instance | BindingFlags.NonPublic);

                MethodInfo miReplacement = typeof(Transform_SetPosition_HookTest).GetMethod("SetPositionNew", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo miProxy = typeof(Transform_SetPosition_HookTest).GetMethod("SetPositionProxy", BindingFlags.Static | BindingFlags.NonPublic);

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void SetPositionNew(Transform t, ref Vector3 value)
        {
            SetPositionProxy(t, ref value);
            Debug.LogFormat("Transfrom [{0}] SetPosition({1})", t.name, value);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void SetPositionProxy(Transform t, ref Vector3 value)
        {
            // dummy code
            Debug.Log("something" + t.ToString());
        }
    }
}
#endif