#if ENABLE_HOOK_TEST_CASE
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Runtime.CompilerServices;

namespace MonoHook.Test
{
    //[InitializeOnLoad]
    public class GameObject_CreatePrimitive_HookTest
    {
        private static MethodHook _hook;

        static GameObject_CreatePrimitive_HookTest()
        {
            if (_hook == null)
            {
                Type type = typeof(AssetDatabase).Assembly.GetType("UnityEditor.GOCreationCommands");
                MethodInfo miTarget = type.GetMethod("CreateAndPlacePrimitive", BindingFlags.Static | BindingFlags.NonPublic);

                MethodInfo miReplacement = new Action<PrimitiveType, GameObject>(CreateAndPlacePrimitive).Method;
                MethodInfo miProxy = new Action<PrimitiveType, GameObject>(CreateAndPlacePrimitiveProxy).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        private static void CreateAndPlacePrimitive(PrimitiveType type, GameObject parent)
        {
            Debug.LogFormat($"将要通过右键菜单创建类型内置类型 {type} ");

            CreateAndPlacePrimitiveProxy(type, parent);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void CreateAndPlacePrimitiveProxy(PrimitiveType type, GameObject parent)
        {
            // dummy code
            Debug.Log("something" + parent.ToString() + type.ToString() + 2);
        }
    }
}
#endif
#endif