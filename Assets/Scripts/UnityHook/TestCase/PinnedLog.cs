#if ENABLE_HOOK_TEST_CASE
/*
 Desc: 不会被Unity Clear按钮清掉的日志
 Author: Misaka Mikoto
 Github: https://github.com/Misaka-Mikoto-Tech/MonoHook
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;



namespace MonoHook.Test
{
#if UNITY_EDITOR
    using UnityEditor.Callbacks;
    /// <summary>
    /// 不会被清掉的日志
    /// </summary>
    //[InitializeOnLoad]
    public static class PinnedLog
    {
        private static Dictionary<int, string> _msgs = new Dictionary<int, string>(); // 点击编辑器运行按钮时会被Unity清空
        private static MethodHook _hook;

        static PinnedLog()
        {
            if (_hook == null)
            {
#if UNITY_2017_1_OR_NEWER
                Type type = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
#else
            Type type = Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");

#endif
                MethodInfo miTarget = type.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);

                MethodInfo miReplacement = new Action(NewClearLog).Method;
                MethodInfo miProxy = new Action(ProxyClearLog).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        public static int AddMsg(string msg)
        {
            UnityEngine.Debug.LogError(msg);

            int key = _msgs.Count;
            _msgs.Add(key, msg);
            return key;
        }

        public static void RemoveMsg(int key)
        {
            _msgs.Remove(key);
        }

        public static void ClearAll()
        {
            _msgs.Clear();
            ProxyClearLog();
        }

        #region private
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void NewClearLog()
        {
            ProxyClearLog();
            foreach (var item in _msgs)
                UnityEngine.Debug.LogError(item.Value);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void ProxyClearLog()
        {
            // 随便乱写点东西以占据空间
            for (int i = 0; i < 100; i++)
            {
                UnityEngine.Debug.Log("something");
            }
            UnityEngine.Debug.Log(Application.targetFrameRate);
        }
        #endregion
    }
#else
public class PinnedLog
{
    public static int AddMsg(string msg)
    {
        return -1;
    }

    public static void RemoveMsg(int key)
    {
    }

    public static void ClearAll()
    {
    }
}
#endif
}

#endif