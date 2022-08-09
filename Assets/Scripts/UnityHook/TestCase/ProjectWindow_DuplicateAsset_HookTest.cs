#if ENABLE_HOOK_TEST_CASE
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MonoHook.Test
{
    /// <summary>
    /// 监控资源复制操作
    /// </summary>
    //[InitializeOnLoad]
    public class ProjectWindow_DuplicateAsset_HookTest
    {

        private static MethodHook _hook;

        static ProjectWindow_DuplicateAsset_HookTest()
        {
            if (_hook == null)
            {
#if UNITY_2021_2_OR_NEWER
                Type type = typeof(UnityEditor.AssetDatabase).Assembly.GetType("UnityEditor.AssetClipboardUtility");
#else
            Type type = typeof(UnityEditor.AssetDatabase).Assembly.GetType("UnityEditor.ProjectWindowUtil");
#endif
                MethodInfo miTarget = type.GetMethod("DuplicateSelectedAssets", BindingFlags.Static | BindingFlags.NonPublic);

                MethodInfo miReplacement = new Action(DuplicateSelectedAssets).Method;
                MethodInfo miProxy = new Action(DuplicateSelectedAssetsProxy).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        private static void DuplicateSelectedAssets()
        {
            var selectedsPre = Selection.objects;
            DuplicateSelectedAssetsProxy();
            var selectedAfter = Selection.objects;

            Debug.Assert(selectedsPre.Length == selectedAfter.Length);

            string[] pathPre = new string[selectedsPre.Length];
            string[] pathAfter = new string[selectedAfter.Length];

            for (int i = 0, imax = selectedsPre.Length; i < imax; i++)
                pathPre[i] = AssetDatabase.GetAssetPath(selectedsPre[i]);

            for (int i = 0, imax = selectedAfter.Length; i < imax; i++)
                pathAfter[i] = AssetDatabase.GetAssetPath(selectedAfter[i]);

            StringBuilder sb = new StringBuilder(1024);
            sb.AppendLine("执行了复制文件：");
            for (int i = 0, imax = Math.Min(pathPre.Length, pathAfter.Length); i < imax; i++)
            {
                sb.AppendLine($"{pathPre[i]} -> {pathAfter[i]}");
            }

            Debug.Log(sb.ToString());
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void DuplicateSelectedAssetsProxy()
        {
            // dummy
        }
    }
}
#endif
#endif