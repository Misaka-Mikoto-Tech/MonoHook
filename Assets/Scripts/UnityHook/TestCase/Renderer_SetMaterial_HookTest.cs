#if ENABLE_HOOK_TEST_CASE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MonoHook.Test
{
    public class Renderer_SetMaterial_HookTest
    {
        public static Action<string> logCallback;

        private static MethodHook _hookSetMat;

        public static void InstallHook()
        {
            MethodInfo miOriFunc = typeof(Renderer).GetMethod("SetMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo miNew = new Action<Renderer, Material>(SetMaterial).Method;
            MethodInfo miProxy = new Action<Renderer, Material>(SetMaterialProxy).Method;

            _hookSetMat = new MethodHook(miOriFunc, miNew, miProxy);
            _hookSetMat.Install();
            logCallback("Renderer.SetMaterial hook installed");
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void SetMaterial(Renderer renderer, Material mat)
        {
            string matName = mat == null ? "null" : mat.name;
            logCallback($"SetMaterial Called: obj:{renderer.gameObject.name}, mat:{matName}");

            SetMaterialProxy(renderer, mat);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void SetMaterialProxy(Renderer renderer, Material mat)
        {
            string str = "SetMaterialProxy";
            logCallback($"can not call this {str}");
        }

    }
}
#endif