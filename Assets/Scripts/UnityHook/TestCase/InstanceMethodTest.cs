#if ENABLE_HOOK_TEST_CASE
/*
 * 实例方法 Hook 测试用例
 * note: 静态方法 Hook 参考 PinnedLog.cs
 */
using DotNetDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MonoHook.Test
{
    public class A
    {
        public int val;

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public int Func(int x)
        {
            x += 2;
            val -= 1;
            return x + val + 1;
        }
    }

    public class B
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public int FuncReplace(int x)
        {
            object obj = this;
            A a = obj as A;

            x += 1;
            a.val = 7;

            if (InstanceMethodTest.callOriFunc)
                return FuncProxy(x);
            else
                return x + 1;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public int FuncProxy(int x)
        {
            Debug.Log("随便乱写");
            return x;
        }
    }

    /// <summary>
    /// 测试实例方法 Hook
    /// </summary>
    public class InstanceMethodTest
    {
        public static MethodHook _hook;
        public static bool callOriFunc;
        public static void InstallPatch()
        {
            Type typeA = typeof(A);
            Type typeB = typeof(B);

            MethodInfo miAFunc = typeA.GetMethod("Func");
            MethodInfo miBReplace = typeB.GetMethod("FuncReplace");
            MethodInfo miBProxy = typeB.GetMethod("FuncProxy");

            _hook = new MethodHook(miAFunc, miBReplace, miBProxy);
            _hook.Install();
        }
        public static void UninstallPatch()
        {
            if (_hook != null)
                _hook.Uninstall();

            _hook = null;
        }

        A _a = new A();

        public int Test()
        {
            _a.val = 5;
            int ret = _a.Func(2);
            return ret;
        }


    }
}
#endif