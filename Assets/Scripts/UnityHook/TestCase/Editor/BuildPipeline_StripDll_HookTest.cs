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
using System.Linq;

namespace MonoHook.Test
{
    // 有需求时可以打开，也可以手动按需注册Hook
    //[InitializeOnLoad]
    public class BuildPipeline_StripDll_HookTest
    {
        // 尝试 Hook 3个函数，至少一个被调用就可以达到要求
        private static MethodHook _hook_Default_PostProcess;
        private static MethodHook _hook_ReportBuildResults;
        private static MethodHook _hook_StripAssembliesTo;

        struct BuildPostProcessArgs
        {
            public BuildTarget target;
            public int subTarget;
            public string stagingArea;
            public string stagingAreaData;
            public string stagingAreaDataManaged;
            public string playerPackage;
            public string installPath;
            public string companyName;
            public string productName;
            public Guid productGUID;
            public BuildOptions options;
            public UnityEditor.Build.Reporting.BuildReport report;
            internal /*RuntimeClassRegistry*/object usedClassRegistry;
        }

        static BuildPipeline_StripDll_HookTest()
        {
            InstallHook();
        }

        public static void InstallHook()
        {
            do
            {
                Type type = Type.GetType("UnityEditor.Modules.DefaultBuildPostprocessor,UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (type == null)
                {
                    Debug.LogError($"can not find type: UnityEditor.Modules.DefaultBuildPostprocessor");
                    break;
                }

                MethodInfo[] miTargets = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                MethodInfo miTarget = (from mi in miTargets where mi.Name == "PostProcess" && mi.GetParameters().Length == 2 select mi).FirstOrDefault();

                if (miTarget == null)
                {
                    Debug.LogError($"can not find method: UnityEditor.Modules.DefaultBuildPostprocessor.PostProcess");
                    break;
                }

                MethodInfo miReplace = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(PostProcess_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo miProxy = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(PostProcess_Proxy), BindingFlags.Static | BindingFlags.NonPublic);

                _hook_Default_PostProcess = new MethodHook(miTarget, miReplace, miProxy);
                _hook_Default_PostProcess.Install();

                Debug.Log("Hook BuildPipeline_StripDll_HookTest.PostProcess installed");
            } while (false);

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

                Debug.Log("Hook BuildPipeline_StripDll_HookTest.StripAssembliesTo installed");
            } while (false);
        }

        public static void UninstallHook()
        {
            _hook_Default_PostProcess?.Uninstall();
            _hook_ReportBuildResults?.Uninstall();
            _hook_StripAssembliesTo?.Uninstall();
        }

        static void PostProcess_Replace(object obj, BuildPostProcessArgs args, out /*BuildProperties*/ object outProperties)
        {
            try
            {
                // 注意：此函数中途可能会被 Unity throw Exception
                PostProcess_Proxy(obj, args, out outProperties);
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                // TODO: 可以在此把裁剪后的dll复制出来
                Debug.LogError("PostProcess_Replace called");
            }
        }

        static void ReportBuildResults_Replace(object obj, /*BeeDriverResult*/ object result)
        {
            // TODO: 可以在这里把 Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped 目录下的文件复制出来
            Debug.LogError("ReportBuildResults_Replace called");
            ReportBuildResults_Proxy(obj, result);
        }

        static bool StripAssembliesTo_Replace(string outputFolder, out string output, out string error, IEnumerable<string> linkXmlFiles, /*UnityLinkerRunInformation*/ object runInformation)
        {
            bool ret = StripAssembliesTo_Proxy(outputFolder, out output, out error, linkXmlFiles, runInformation);

            // TODO: 可以在这里把 Temp\StagingArea\Data\Managed\tempStrip 目录下的文件复制出来
            Debug.Log("StripAssembliesTo_Replace called");
            return ret;
        }

#region Proxy Methods
        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void PostProcess_Proxy(object obj, BuildPostProcessArgs args, out /*BuildProperties*/ object outProperties)
        {
            Debug.Log("dummy code" + 100);
            outProperties = null;
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
#endregion
    }
}

#endif