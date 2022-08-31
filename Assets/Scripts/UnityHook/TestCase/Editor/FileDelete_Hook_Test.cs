#if ENABLE_HOOK_TEST_CASE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.CompilerServices;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using UnityEditor.Callbacks;

namespace MonoHook.Test
{
    public class FileDelete_Hook_Test
    {
        static MethodHook _File_Delete_Hook;
        static MethodHook _Directory_Delete_Hook;
        static MethodHook _AssetDatabase_DeleteAsset_Hook;
        static MethodHook _AssetDatabase_DeleteAssetsCommon_Hook;

        //[DidReloadScripts]
        static void Install()
        {
            {
                MethodInfo target = typeof(File).GetMethod("Delete");
                MethodInfo replace = new Action<string>(File_Delete_Replace).Method;
                MethodInfo proxy = new Action<string>(File_Delete_Proxy).Method;
                if (target != null && replace != null && proxy != null)
                {
                    _File_Delete_Hook = new MethodHook(target, replace, proxy);
                    _File_Delete_Hook.Install();
                }

            }
            {
                MethodInfo target = (from mi in typeof(Directory).GetMethods() where mi.Name == "Delete" && mi.GetParameters().Length == 2 select mi).FirstOrDefault();
                MethodInfo replace = typeof(FileDelete_Hook_Test).GetMethod(nameof(Directory_Delete_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo proxy = typeof(FileDelete_Hook_Test).GetMethod(nameof(Directory_Delete_Proxy), BindingFlags.Static | BindingFlags.NonPublic);
                if (target != null && replace != null && proxy != null)
                {
                    _Directory_Delete_Hook = new MethodHook(target, replace, proxy);
                    _Directory_Delete_Hook.Install();
                }
            }
            {
                MethodInfo target = typeof(AssetDatabase).GetMethod("DeleteAsset", BindingFlags.Static | BindingFlags.Public);
                MethodInfo replace = typeof(FileDelete_Hook_Test).GetMethod(nameof(AssetDatabase_DeleteAsset_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo proxy = typeof(FileDelete_Hook_Test).GetMethod(nameof(AssetDatabase_DeleteAsset_Proxy), BindingFlags.Static | BindingFlags.NonPublic);
                if (target != null && replace != null && proxy != null)
                {
                    _AssetDatabase_DeleteAsset_Hook = new MethodHook(target, replace, proxy);
                    _AssetDatabase_DeleteAsset_Hook.Install();
                }
            }
            {
                MethodInfo target = typeof(AssetDatabase).GetMethod("DeleteAssetsCommon", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo replace = typeof(FileDelete_Hook_Test).GetMethod(nameof(AssetDatabase_DeleteAssetsCommon_Replace), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo proxy = typeof(FileDelete_Hook_Test).GetMethod(nameof(AssetDatabase_DeleteAssetsCommon_Proxy), BindingFlags.Static | BindingFlags.NonPublic);
                if (target != null && replace != null && proxy != null)
                {
                    _AssetDatabase_DeleteAssetsCommon_Hook = new MethodHook(target, replace, proxy);
                    _AssetDatabase_DeleteAssetsCommon_Hook.Install();
                }
            }
        }

        static void Uninstall()
        {
            _File_Delete_Hook?.Uninstall();
            _Directory_Delete_Hook?.Uninstall();
            _AssetDatabase_DeleteAsset_Hook?.Uninstall();
            _AssetDatabase_DeleteAssetsCommon_Hook?.Uninstall();
        }

        static void OnFileOrDirDelete(string path, string[] paths = null)
        {
            // 文件删除回调, 可以在此下断点或者写自定义逻辑
            if (paths == null)
                Debug.Log($"File will delete: {path}");
            else
                Debug.Log($"Files will delete: {path}");
        }

        static void File_Delete_Replace(string path)
        {
            OnFileOrDirDelete(path);
            File_Delete_Proxy(path);
        }

        static void Directory_Delete_Replace(string path, bool recursive)
        {
            OnFileOrDirDelete(path);
            Directory_Delete_Proxy(path, recursive);
        }

        static void AssetDatabase_DeleteAsset_Replace(string path)
        {
            OnFileOrDirDelete(path);
            AssetDatabase_DeleteAsset_Proxy(path);
        }

        static void AssetDatabase_DeleteAssetsCommon_Replace(string[] paths, object outFailedPaths, bool moveAssetsToTrash)
        {
            OnFileOrDirDelete(null, paths);
            AssetDatabase_DeleteAssetsCommon_Proxy(paths, outFailedPaths, moveAssetsToTrash);
        }



        #region proxy
        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void File_Delete_Proxy(string path)
        {
            Debug.Log($"dummy code{path}");
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void Directory_Delete_Proxy(string path, bool recursive)
        {
            Debug.Log($"dummy code{path}");
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void AssetDatabase_DeleteAsset_Proxy(string path)
        {
            Debug.Log($"dummy code{path}");
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void AssetDatabase_DeleteAssetsCommon_Proxy(string[] paths, object outFailedPaths, bool moveAssetsToTrash)
        {
            Debug.Log($"dummy code{outFailedPaths}");
        }
        #endregion
    }
}

#endif