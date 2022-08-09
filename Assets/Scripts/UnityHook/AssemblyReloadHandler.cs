#if UNITY_EDITOR && false
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace MonoHook
{
    /// <summary>
    /// 由于Unity2021.2之后Unity的Reload操作会忽略引擎自带的Assembly，
    /// 因此如果用户代码Hook了引擎dll，那么reload之后会Crash，
    /// 解决方案是在Reload之前全部Uninstall，Reload之后再重新Install
    /// </summary>
    [InitializeOnLoad]
    public static class AssemblyReloadHandler
    {
        const string kHooksSavePath = "Temp/MonoHook_Save.json";

        static AssemblyReloadHandler()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnBeforeAssemblyReload()
        {
            SaveHooks();
        }

        static void OnAfterAssemblyReload()
        {
            RestoreHooks();
        }


        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            return;
            switch(state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.ExitingEditMode:
                    SaveHooks();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    RestoreHooks();
                    break;
            }
        }

        static void SaveHooks()
        {
            // save all hooks
            var allHooks = HookPool.GetAllHooks();
#if ENABLE_HOOK_DEBUG
            Debug.Log($"OnBeforeAssemblyReload: hook count:{allHooks.Count}, isPlaying:{Application.isPlaying}");
#endif

            var hookListForSave = new HookInfos_ForSave(allHooks);
            string jsonStr = JsonUtility.ToJson(hookListForSave, true);
            File.WriteAllText(kHooksSavePath, jsonStr);

            HookPool.UninstallAll();
        }

        static void RestoreHooks()
        {
            return;
            // restore all hooks
#if ENABLE_HOOK_DEBUG
            var allHooks = HookPool.GetAllHooks();
            Debug.Log($"OnAfterAssemblyReload: hook count:{allHooks.Count}, isPlaying:{Application.isPlaying}");
#endif
            if (!File.Exists(kHooksSavePath))
                return;

            HookInfos_ForSave hookListForSave = null;
            try
            {
                string jsonStr = File.ReadAllText(kHooksSavePath);
                hookListForSave = JsonUtility.FromJson<HookInfos_ForSave>(jsonStr);
            }catch(Exception ex)
            {
                Debug.LogError($"Load MethodHook json fail:{ex.Message}");
                return;
            }
            
            foreach (var hook in hookListForSave.items)
            {
                MethodBase targetMethod = hook.target.GetMethodInfo();
                if (targetMethod == null)
                {
#if ENABLE_HOOK_DEBUG
                    Debug.LogError($"Restore Hook.targetMethod [{hook.target.name}] fail");
#endif
                    continue;
                }
                
                MethodInfo replaceMethod = hook.replace.GetMethodInfo() as MethodInfo;
                if (replaceMethod == null)
                {
#if ENABLE_HOOK_DEBUG
                    Debug.LogError($"Restore Hook.replaceMethod [{hook.replace.name}] fail");
#endif
                    continue;
                }
                    
                MethodInfo proxyMethod = null;
                if(hook.proxy.isValid)
                {
                    proxyMethod = hook.proxy.GetMethodInfo() as MethodInfo;
                    if (proxyMethod == null)
                    {
#if ENABLE_HOOK_DEBUG
                        Debug.LogError($"Restore Hook.proxyMethod [{hook.proxy.name}] fail");
#endif
                        continue;
                    }
                }

                MethodHook hookInstance = new MethodHook(targetMethod, replaceMethod, proxyMethod);
                hookInstance.Install();
            }
        }

        [Serializable]
        public class HookInfos_ForSave
        {
            public List<HookInfoItem_ForSave> items;

            public HookInfos_ForSave() { }
            public HookInfos_ForSave(List<MethodHook> hooks)
            {
                items = new List<HookInfoItem_ForSave>();
                foreach (var hook in hooks)
                {
                    var info = new HookInfoItem_ForSave();
                    info.target = new MethodInfoData(hook.targetMethod);
                    info.replace = new MethodInfoData(hook.replacementMethod);
                    info.proxy = new MethodInfoData(hook.proxyMethod);
                    items.Add(info);
                }
            }
        }

        [Serializable]
        public class HookInfoItem_ForSave
        {
            public MethodInfoData target;
            public MethodInfoData replace;
            public MethodInfoData proxy;
        }

        [Serializable]
        public class MethodInfoData
        {
            public bool isValid;
            public string memberType; // Constructor, MethodInfo
            public bool isPublic;
            public bool isStatic;
            public string declType;
            public string name;
            public List<string> argTypes;

            public MethodInfoData(MethodBase mb)
            {
                isValid = false;
                if (mb == null) return;

                memberType  = mb.MemberType.ToString();
                isPublic    = mb.IsPublic;
                isStatic    = mb.IsStatic;
                declType    = mb.DeclaringType.AssemblyQualifiedName;
                name        = mb.Name;
                argTypes    = new List<string>();
                var args    = mb.GetParameters();
                foreach(var arg in args)
                {
                    argTypes.Add(arg.ParameterType.AssemblyQualifiedName);
                }
                isValid = true;
            }

            public MethodBase GetMethodInfo()
            {
                Type type = Type.GetType(declType);
                if (type == null)
                {
#if ENABLE_HOOK_DEBUG
                    Debug.LogError($"MethodInfoData.GetMethodInfo: GetType({declType}) fail");
#endif
                    return null;
                }
                
                BindingFlags flags = BindingFlags.Default;
                if (isStatic)
                    flags |= BindingFlags.Static;
                else
                    flags |= BindingFlags.Instance;

                if (isPublic)
                    flags |= BindingFlags.Public;
                else
                    flags |= BindingFlags.NonPublic;

                Type[] argTypeArr = new Type[argTypes.Count];
                for(int i = 0; i < argTypeArr.Length; i++)
                {
                    argTypeArr[i] = Type.GetType(argTypes[i]);
                    if (argTypeArr[i] == null)
                    {
#if ENABLE_HOOK_DEBUG
                        Debug.LogError($"MethodInfoData.GetMethodInfo: args[{i}] [{argTypes[i]}] fail");
#endif
                        return null;
                    }
                }

                MethodBase ret = null;
                if (memberType == "Constructor")
                    ret = type.GetConstructor(argTypeArr);
                else if(memberType == "Method")
                    ret = type.GetMethod(name, flags, null, argTypeArr, null);

#if ENABLE_HOOK_DEBUG
                if(ret == null)
                {
                    Debug.LogError($"GetMethodInfo: [{name}] finally fail");
                }
#endif

                return ret;
            }
        }
    }
}
#endif