#if ENABLE_HOOK_TEST_CASE
/*
 * 构造函数 Hook 测试
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MonoHook.Test
{
    public class CtorHookTarget
    {
        public int x;
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public CtorHookTarget(int x)
        {
            this.x = x;
            Debug.LogFormat("ctor with x:{0}", x);
        }
    }

    public class CtorHookTest
    {
        public void Test()
        {
            MethodBase mbCtorA = typeof(CtorHookTarget).GetConstructor(new Type[] { typeof(int) });
            MethodInfo mbReplace = new Action<int>(CtorTargetReplace).Method;
            MethodInfo mbProxy = new Action<int>(CtorTargetProxy).Method;

            MethodHook hookder = new MethodHook(mbCtorA, mbReplace, mbProxy);
            hookder.Install();

            for (int i = 0; i < 2; i++)
            {
                CtorHookTarget hookTarget = new CtorHookTarget(1);
                Debug.Assert(hookTarget.x == 2, $"expect 2 but get {hookTarget.x} at i:{i}");
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CtorTargetReplace(int x)
        {
            x += 1;
            CtorTargetProxy(x);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CtorTargetProxy(int x)
        {
            Debug.Log("CtorTargetProxy");
        }
    }
}
#endif