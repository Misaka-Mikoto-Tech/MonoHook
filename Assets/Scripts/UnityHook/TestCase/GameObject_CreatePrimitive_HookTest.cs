#if ENABLE_HOOK_TEST_CASE
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

//[InitializeOnLoad]
public class GameObject_CreatePrimitive_HookTest
{
    private static MethodHook _hook;
    private static MethodInfo _targetMethod;

    static GameObject_CreatePrimitive_HookTest()
    {
        if (_hook == null)
        {
            Type type = typeof(AssetDatabase).Assembly.GetType("UnityEditor.GOCreationCommands");

            _targetMethod = type.GetMethod("CreateAndPlacePrimitive", BindingFlags.Static | BindingFlags.NonPublic);

            type = typeof(GameObject_CreatePrimitive_HookTest);
            MethodInfo miReplacement = type.GetMethod("CreateAndPlacePrimitive", BindingFlags.Static | BindingFlags.NonPublic);

            _hook = new MethodHook(_targetMethod, miReplacement);
            _hook.Install();
        }
    }

    private static void CreateAndPlacePrimitive(PrimitiveType type, GameObject parent)
    {
        Debug.LogFormat($"将要通过右键菜单创建类型内置类型 {type} ");

        _hook.RunWithoutPatch(() => _targetMethod.Invoke(null, new object[] { type, parent }));
    }
}
#endif
#endif