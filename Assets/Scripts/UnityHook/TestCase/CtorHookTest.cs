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

public class CtorHookTarget
{
    public int x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public CtorHookTarget(int x)
    {
        this.x = x;
        Debug.LogFormat("ctor with x:{0}", x);
    }
}

public class CtorHookTest
{
    public static MethodHook hook;
    public void Test()
    {
        Type typeA = typeof(CtorHookTarget);
        Type typeB = typeof(CtorHookTest);

        MethodBase mbCtorA = typeA.GetConstructor(new Type[] { typeof(int) });
        MethodBase mbReplace = typeB.GetMethod("CtorTargetReplace");

        hook = new MethodHook(mbCtorA, mbReplace);
        hook.Install();

        CtorHookTarget hookTarget = new CtorHookTarget(1);
        Debug.Assert(hookTarget.x == 2);
    }

    public void CtorTargetReplace(int x)
    {
        Debug.Log("CtorTargetReplace");

        x += 1;
        CtorHookTest.hook.RunWithoutPatch(this, x); // scope now is CtorHookTarget,not CtorHookTest, so we should use static var
    }
}
#endif