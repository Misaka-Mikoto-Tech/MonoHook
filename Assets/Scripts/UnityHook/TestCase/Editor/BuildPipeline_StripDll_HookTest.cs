#if ENABLE_HOOK_TEST_CASE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MonoHook.Test
{
    //[InitializeOnLoad] // 有需求时可以打开，也可以手动按需注册Hook
    public class BuildPipeline_StripDll_HookTest
    {
        private static MethodHook _hook_ReportBuildResults;
        private static MethodHook _hook_StripAssembliesTo;

        static BuildPipeline_StripDll_HookTest()
        {
            InstallHook();
        }

        public static void InstallHook()
        {
            do
            {
                Type type = Type.GetType("UnityEditor.Modules.BeeBuildPostprocessor,UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (type == null)
                {
                    Debug.LogError($"can not find type: UnityEditor.Modules.BeeBuildPostprocessor");
                    break;
                }

                MethodInfo miTarget = type.GetMethod("ReportBuildResults", BindingFlags.Instance | BindingFlags.NonPublic);
                if (miTarget == null)
                {
                    Debug.LogError($"can not find method: UnityEditor.Modules.BeeBuildPostprocessor.ReportBuildResults");
                    break;
                }

                MethodInfo miReplace = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(ReportBuildResults_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo miProxy = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(ReportBuildResults_Proxy), BindingFlags.Static | BindingFlags.NonPublic);

                _hook_ReportBuildResults = new MethodHook(miTarget, miReplace, miProxy);
                _hook_ReportBuildResults.Install();

                Debug.Log("Hook BuildPipeline_StripDll_HookTest.ReportBuildResults installed");
            } while (false);

            do
            {
                Type type = Type.GetType("UnityEditorInternal.AssemblyStripper,UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (type == null)
                {
                    Debug.LogError($"can not find type: UnityEditorInternal.AssemblyStripper");
                    break;
                }

                MethodInfo miTarget = type.GetMethod("StripAssembliesTo", BindingFlags.Static | BindingFlags.NonPublic);
                if (miTarget == null)
                {
                    Debug.LogError($"can not find method: UnityEditorInternal.AssemblyStripper.StripAssembliesTo");
                    break;
                }

                MethodInfo miReplace = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(StripAssembliesTo_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo miProxy = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(StripAssembliesTo_Proxy), BindingFlags.Static | BindingFlags.NonPublic);

                _hook_StripAssembliesTo = new MethodHook(miTarget, miReplace, miProxy);
                _hook_StripAssembliesTo.Install();

                Debug.Log("Hook BuildPipeline_StripDll_HookTest._hook_StripAssembliesTo installed");
            } while (false);
        }

        public static void UninstallHook()
        {
            _hook_ReportBuildResults?.Uninstall();
            _hook_StripAssembliesTo?.Uninstall();
        }


        static void ReportBuildResults_Replace(object obj, /*BeeDriverResult*/ object result)
        {
            // TODO: 可以在这里把 Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped 目录下的文件复制出来
            Debug.LogError("ReportBuildResults_Replace called");
            ReportBuildResults_Proxy(obj, result);
        }

        static bool StripAssembliesTo_Replace(string outputFolder, out string output, out string error, IEnumerable<string> linkXmlFiles, /*UnityLinkerRunInformation*/ object runInformation)
        {
            // TODO: 可以在这里把 Temp\StagingArea\Data\Managed\tempStrip 目录下的文件复制出来
            Debug.Log("StripAssembliesTo_Replace called");
            return StripAssembliesTo_Proxy(outputFolder, out output, out error, linkXmlFiles, runInformation);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void ReportBuildResults_Proxy(object obj, /*BeeDriverResult*/ object result)
        {
            // dummy code
            Debug.Log("something" + obj.ToString() + result.ToString() + 2);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static bool StripAssembliesTo_Proxy(string outputFolder, out string output, out string error, IEnumerable<string> linkXmlFiles, /*UnityLinkerRunInformation*/ object runInformation)
        {
            Debug.Log("StripAssembliesTo_Proxy called");
            output = null;
            error = null;
            return true;
        }
    }
}

#endif