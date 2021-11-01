#if ENABLE_HOOK_TEST_CASE
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

//[InitializeOnLoad]
public class SceneHierarchyStageHandling_HookTest
{
    private static MethodHook _hooker;
    static SceneHierarchyStageHandling_HookTest()
    {
        if (_hooker == null)
        {
            Type type = Type.GetType("UnityEditor.SceneHierarchyStageHandling,UnityEditor.dll");
#if UNITY_2021_2_OR_NEWER
            var target = type.GetMethod("StageHeaderGUI", BindingFlags.Instance | BindingFlags.Public);
#else
            var target = type.GetMethod("PrefabStageHeaderGUI", BindingFlags.Instance | BindingFlags.Public);
#endif
            var dst = typeof(SceneHierarchyStageHandling_HookTest).GetMethod("PrefabStageHeaderGUINew",
                BindingFlags.Static | BindingFlags.NonPublic);
            var old = typeof(SceneHierarchyStageHandling_HookTest).GetMethod("PrefabStageHeaderGUIOld",
                BindingFlags.Static | BindingFlags.NonPublic);

            _hooker = new MethodHook(target, dst, old);
            _hooker.Install();
        }
    }
    static void PrefabStageHeaderGUINew(object handle, Rect rect)
    {
        PrefabStageHeaderGUIOld(handle, rect);
        GUILayout.Button("^wow^");
        // GUI.Button(new Rect(rect.xMax - 100, rect.y, 16, rect.height), "x");
    }
    static void PrefabStageHeaderGUIOld(object handle, Rect rect)
    {
        // 随便乱写点东西以占据空间
        for (int i = 0; i < 100; i++)
        {
            UnityEngine.Debug.Log("something");
        }
        UnityEngine.Debug.Log(Application.targetFrameRate);
    }


}
#endif
#endif