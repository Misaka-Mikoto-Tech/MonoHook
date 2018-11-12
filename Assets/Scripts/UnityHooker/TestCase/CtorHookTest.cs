/*
 * 构造函数 Hook 测试
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CtorHookTarget
{
    public int x;
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
        Type typeA = typeof(CtorHookTarget);
        Type typeB = typeof(CtorHookTest);

        MethodBase mbCtorA = typeA.GetConstructor(new Type[] { typeof(int) });
        MethodBase mbReplace = typeB.GetMethod("CtorTargetReplace");
        MethodBase mbProxy = typeB.GetMethod("CtorTargetProxy");

        MethodHooker hookder = new MethodHooker(mbCtorA, mbReplace, mbProxy);
        hookder.Install();

        CtorHookTarget hookTarget = new CtorHookTarget(1);
        Debug.Assert(hookTarget.x == 2);
    }

    public void CtorTargetReplace(int x)
    {
        x += 1;
        CtorTargetProxy(x);
    }

    public void CtorTargetProxy(int x)
    {
        Debug.Log("CtorTargetProxy");
    }
}
