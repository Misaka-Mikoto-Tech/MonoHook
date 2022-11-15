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
    public class BuildMainWindowTitle_Hook_Test
    {
        private static MethodHook _hook;

        static BuildMainWindowTitle_Hook_Test()
        {
            if (_hook == null)
            {
                Type type = typeof(UnityEditor.EditorApplication);
                MethodInfo miTarget = type.GetMethod("BuildMainWindowTitle", BindingFlags.Static | BindingFlags.NonPublic);

                MethodInfo miReplacement = new Func<string>(BuildMainWindowTitle).Method;
                MethodInfo miProxy = new Func<string>(BuildMainWindowTitleProxy).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        private static string BuildMainWindowTitle()
        {
            string newTitle = BuildMainWindowTitleProxy();
            Debug.Log($"将要设置新窗口标题为: {newTitle}");
            return newTitle;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static string BuildMainWindowTitleProxy()
        {
            // dummy code
            Debug.Log("something" + typeof(BuildMainWindowTitle_Hook_Test).ToString() + 2);
            return string.Empty;
        }
    }
}
#endif
#endif