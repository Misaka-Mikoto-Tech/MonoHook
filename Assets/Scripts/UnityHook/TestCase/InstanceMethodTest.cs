#if ENABLE_HOOK_TEST_CASE
/*
 * 实例方法 Hook 测试用例
 * note: 静态方法 Hook 参考 PinnedLog.cs
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;


public class A
{
    public int val;

    [MethodImpl(MethodImplOptions.NoInlining)] // without this will hook fail at il2cpp release mode
    public int Func(int x)
    {
        return x + val;
    }
}

/// <summary>
/// 测试实例方法 Hook
/// </summary>
public class InstanceMethodTest
{
    static MethodHook _hook;
    public static bool s_callOriFunc = true;

    class InnerData
    {
        public int x;
        public A a;
        public int ret;
        public Action action;

        public InnerData()
        {
            action = Call;
        }

        public void Call()
        {
             ret = a.Func(x);
        }
    }

    static InnerData s_innerData = new InnerData();
    public static int FuncReplace(A a, int x)
    {
        //Debug.Log("call of InstanceMethodTest.FuncReplace");
        x += 1;
        a.val = 7;

        if (s_callOriFunc)
        {
            s_innerData.x = x;
            s_innerData.a = a;
            _hook.RunWithoutPatch(s_innerData.action);
            return s_innerData.ret;
        }
        else
            return x + a.val;
    }

    private static int CallAFunc(A a, int x)
    {
        return a.Func(x);
    }

    static int s_initVal = 2;

    public void Reset()
    {
        s_initVal = 2;
        s_callOriFunc = true;
    }

    public static void InstallPatch()
    {
        MethodInfo miAFunc = typeof(A).GetMethod("Func");
        MethodInfo miBReplace = typeof(InstanceMethodTest).GetMethod("FuncReplace", BindingFlags.Static | BindingFlags.Public);

        _hook = new MethodHook(miAFunc, miBReplace);
        _hook.Install();
    }

    public static void UnInstallPatch()
    {
        _hook.Uninstall();
    }

    public int Test()
    {
        // 调用原始A的方法测试
        A a = new A() { val = 5 };
        int ret = a.Func(s_initVal++);
        return ret;
    }
    

}
#endif