using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoHook
{
    /// <summary>
    /// Hook 池，防止重复 Hook
    /// </summary>
    public static class HookPool
    {
        private static Dictionary<MethodBase, MethodHook> _hooks = new Dictionary<MethodBase, MethodHook>();

        public static void AddHook(MethodBase method, MethodHook hook)
        {
            MethodHook preHook;
            if (_hooks.TryGetValue(method, out preHook))
            {
                preHook.Uninstall();
                _hooks[method] = hook;
            }
            else
                _hooks.Add(method, hook);
        }

        public static MethodHook GetHook(MethodBase method)
        {
            if (method == null) return null;

            MethodHook hook;
            if (_hooks.TryGetValue(method, out hook))
                return hook;
            return null;
        }

        public static void RemoveHooker(MethodBase method)
        {
            if (method == null) return;

            _hooks.Remove(method);
        }

        public static void UninstallAll()
        {
            var list = _hooks.Values.ToList();
            foreach (var hook in list)
                hook.Uninstall();

            _hooks.Clear();
        }




        #region Editor下Reload逻辑处理
#if UNITY_EDITOR
        [System.Serializable]
        public class HookInfos_ForSave
        {
            public List<HookInfoItem_ForSave> items;

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

        [System.Serializable]
        public class HookInfoItem_ForSave
        {
            public MethodInfoData target;
            public MethodInfoData replace;
            public MethodInfoData proxy;
        }
        [System.Serializable]
        public class MethodInfoData
        {
            public string memberType; // Constructor, MethodInfo
            public string module;
            public string name;
            public string fullName;
            public bool isPublic;
            public bool isStatic;

            public MethodInfoData(MethodBase mb)
            {
                module = mb.Module.Name;
                name = mb.Name;
                fullName = mb.ToString();
                isPublic = mb.IsPublic;
                isStatic = mb.IsStatic;
            }

            public MethodBase GetMethodInfo()
            {
                return null;
            }
        }

        const string kHooksSavePath = "Temp/MonoHook_Save.json";
        static HookPool()
        {
            //AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            //AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        static void OnBeforeAssemblyReload()
        {
            // save all hooks
            Debug.Log($"OnBeforeAssemblyReload: hook count:{_hooks.Count}");

            var hookListForSave = new HookInfos_ForSave(_hooks.Values.ToList());
            string jsonStr = JsonUtility.ToJson(hookListForSave, true);
            File.WriteAllText(kHooksSavePath, jsonStr);
            Debug.Log($"hook list:{jsonStr}");

        }

        static void OnAfterAssemblyReload()
        {
            // restore all hooks
            Debug.Log($"OnBeforeAssemblyReload: hook count:{_hooks.Count}");
            if (!File.Exists(kHooksSavePath))
                return;

            HookInfos_ForSave hookListForSave = JsonUtility.FromJson<HookInfos_ForSave>(kHooksSavePath);
            foreach (var hook in hookListForSave.items)
            {

            }
        }
#endif
        #endregion
    }

}
