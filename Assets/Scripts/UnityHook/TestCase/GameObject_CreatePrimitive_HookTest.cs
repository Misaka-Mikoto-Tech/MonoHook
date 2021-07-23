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

    static GameObject_CreatePrimitive_HookTest()
    {
        if (_hook == null)
        {
            Type type = typeof(AssetDatabase).Assembly.GetType("UnityEditor.GOCreationCommands");

            MethodInfo miTarget = type.GetMethod("CreateAndPlacePrimitive", BindingFlags.Static | BindingFlags.NonPublic);

            type = typeof(GameObject_CreatePrimitive_HookTest);
            MethodInfo miReplacement = type.GetMethod("CreateAndPlacePrimitive", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo miProxy = type.GetMethod("CreateAndPlacePrimitiveProxy", BindingFlags.Static | BindingFlags.NonPublic);

            _hook = new MethodHook(miTarget, miReplacement, miProxy);
            _hook.Install();
        }
    }

    private static void CreateAndPlacePrimitive(PrimitiveType type, GameObject parent)
    {
        Debug.LogFormat($"将要通过右键菜单创建类型内置类型 {type} ");

        CreateAndPlacePrimitiveProxy(type, parent);
    }

    private static void CreateAndPlacePrimitiveProxy(PrimitiveType type, GameObject parent)
    {
        // dummy
    }
}
