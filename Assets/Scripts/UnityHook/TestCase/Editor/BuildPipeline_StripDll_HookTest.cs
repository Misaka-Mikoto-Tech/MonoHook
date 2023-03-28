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
using UnityEditor.Build.Reporting;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace MonoHook.Test
{
    // 有需求时可以打开，也可以手动按需注册Hook
    [InitializeOnLoad]
    public class BuildPipeline_StripDll_HookTest
    {
        /// <summary>
        /// 裁剪执行完毕的回调，可能会被调用多次，一般而言同一次打包只需要处理第一次回调
        /// </summary>
        public static Action<string, BuildPostProcessArgs, BeeDriverResult> OnAssemblyStripped;

        // 尝试 Hook 4个函数，至少一个被调用就可以达到要求
        private static MethodHook _hook_PostprocessBuildPlayer_CompleteBuild;
        private static MethodHook _hook_Default_PostProcess;
        private static MethodHook _hook_ReportBuildResults;
        private static MethodHook _hook_StripAssembliesTo;

#region Fake Internal Structures
        public struct BuildPostProcessArgs
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

        public sealed class BeeDriverResult
        {
            public /*NodeResult*/object[] NodeResults { get; set; }
            public bool Success { get; set; }
            public /*Message*/object[] BeeDriverMessages { get; set; }
            public override string ToString() => Success.ToString();
        }
#endregion

        static BuildPipeline_StripDll_HookTest()
        {
            GenBatchFile();
            InstallHook();
            OnAssemblyStripped = DemoStripCallback;
        }

        [MenuItem("Tools/Build_HookTest")]
        static void BuildPlayer()
        {
            string buildDir = "WinBuild";
            if (Directory.Exists(buildDir))
                Directory.Delete(buildDir, true);
            Directory.CreateDirectory(buildDir);

            BuildPlayerOptions opt = new BuildPlayerOptions();
            opt.scenes = new string[] { "Assets/Scenes/SampleScene5+.unity" };
            opt.locationPathName = $"{buildDir}/MonoHook.exe";
            opt.targetGroup = BuildTargetGroup.Standalone;
            opt.target = BuildTarget.StandaloneWindows64;
            opt.options = BuildOptions.Development;

            Debug.Log("begin build player");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            if (report.summary.result != BuildResult.Succeeded)
            {
                foreach(BuildStep step in report.steps)
                {
                    foreach(var msg in step.messages)
                    {
                        if (msg.type == LogType.Error)
                            Debug.LogError(msg.content);
                    }
                }
            }
            Debug.Log("end build player");
        }

        /// <summary>
        /// 生成用来测试batchMode打包的批处理文件
        /// </summary>
        static void GenBatchFile()
        {
            string format = @"SET UNITY_PATH=""{0}""
SET PROJECT_PATH=""{1}""

%UNITY_PATH% -nographics -batchMode -projectPath %PROJECT_PATH% -executeMethod MonoHook.Test.BuildPipeline_StripDll_HookTest.BuildPlayer -quit -logFile %PROJECT_PATH%\logs\build_player.log
pause";
            string content = string.Format(format, Process.GetCurrentProcess().MainModule.FileName, Environment.CurrentDirectory);
            File.WriteAllText("BuildPlayer.bat", content);
        }

        /// <summary>
        /// 示例裁剪回调函数
        /// </summary>
        /// <param name="outputFolder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        static void DemoStripCallback(string outputFolder, BuildPostProcessArgs args, BeeDriverResult result)
        {
            if (outputFolder != null)
                Debug.Log($"stripped outputFolder is:{outputFolder}");
            else if (args.stagingAreaDataManaged != null)
                Debug.Log($"stripped staging folder is:{args.stagingAreaDataManaged}");
            else if (result != null)
                Debug.Log($"stripped result is: {result.Success}");
            else
                Debug.Log("stripped test called");
        }

        public static void InstallHook()
        {
            do
            {
                Type type = Type.GetType("UnityEditor.PostprocessBuildPlayer,UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (type == null)
                {
                    Debug.LogError($"can not find type: UnityEditor.PostprocessBuildPlayer");
                    break;
                }

                MethodInfo miTarget = type.GetMethod("PostProcessCompletedBuild", BindingFlags.Static | BindingFlags.Public);

                if (miTarget == null)
                {
                    Debug.LogError($"can not find method: UnityEditor.PostprocessBuildPlayer.PostProcessCompletedBuild");
                    break;
                }

                MethodInfo miReplace = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(PostprocessBuildPlayer_CompleteBuild_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo miProxy = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(PostprocessBuildPlayer_CompleteBuild_Proxy), BindingFlags.Static | BindingFlags.NonPublic);

                _hook_PostprocessBuildPlayer_CompleteBuild = new MethodHook(miTarget, miReplace, miProxy);
                _hook_PostprocessBuildPlayer_CompleteBuild.Install();

                Debug.Log("Hook BuildPipeline_StripDll_HookTest.PostprocessBuildPlayer_CompleteBuild installed");
            } while (false);

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

                MethodInfo miReplace = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(Default_PostProcess_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo miProxy = typeof(BuildPipeline_StripDll_HookTest).GetMethod(nameof(Default_PostProcess_Proxy), BindingFlags.Static | BindingFlags.NonPublic);

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
            _hook_PostprocessBuildPlayer_CompleteBuild?.Uninstall();
            _hook_Default_PostProcess?.Uninstall();
            _hook_ReportBuildResults?.Uninstall();
            _hook_StripAssembliesTo?.Uninstall();
        }

        static void PostprocessBuildPlayer_CompleteBuild_Replace(BuildPostProcessArgs args)
        {
            Debug.Log("PostprocessBuildPlayer_CompleteBuild_Replace called");

            OnAssemblyStripped?.Invoke(null, args, null);
            PostprocessBuildPlayer_CompleteBuild_Proxy(args);
        }

        static void Default_PostProcess_Replace(object obj, BuildPostProcessArgs args, out /*BuildProperties*/ object outProperties)
        {
            try
            {
                // 注意：此函数中途可能会被 Unity throw Exception
                Default_PostProcess_Proxy(obj, args, out outProperties);
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                Debug.Log("PostProcess_Replace called");
                OnAssemblyStripped?.Invoke(null, args, null);
            }
        }

        static void ReportBuildResults_Replace(object obj, BeeDriverResult result)
        {
            // TODO: 可以在这里把 Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped 目录下的文件复制出来
            Debug.Log("ReportBuildResults_Replace called");

            OnAssemblyStripped?.Invoke(null, default(BuildPostProcessArgs), result);
            ReportBuildResults_Proxy(obj, result);
        }

        static bool StripAssembliesTo_Replace(string outputFolder, out string output, out string error, IEnumerable<string> linkXmlFiles, /*UnityLinkerRunInformation*/ object runInformation)
        {
            bool ret = StripAssembliesTo_Proxy(outputFolder, out output, out error, linkXmlFiles, runInformation);

            // TODO: 可以在这里把 Temp\StagingArea\Data\Managed\tempStrip 目录下的文件复制出来
            Debug.Log("StripAssembliesTo_Replace called");

            OnAssemblyStripped?.Invoke(outputFolder, default(BuildPostProcessArgs), null);
            return ret;
        }

#region Proxy Methods
        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void PostprocessBuildPlayer_CompleteBuild_Proxy(BuildPostProcessArgs args)
        {
            Debug.Log("dummy code" + 200);
            Debug.Log(args.companyName);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void Default_PostProcess_Proxy(object obj, BuildPostProcessArgs args, out /*BuildProperties*/ object outProperties)
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