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
        Debug.Log("call of A.Func");
        return x + val;
    }
}

/// <summary>
/// 测试实例方法 Hook
/// </summary>
public class InstanceMethodTest
{
    static MethodHook _hook;

    public static int FuncReplace(A a, int x)
    {
        Debug.Log("call of InstanceMethodTest.FuncReplace");
        x += 1;
        a.val = 7;

        // 可以调用原方法或者不调用
        if (x < 100)
        {
            int ret = 0;
            _hook.RunWithoutPatch(()=> ret = a.Func(x));
            return ret;
        }
        else
            return x + 1;
    }

    public string Test()
    {
        MethodInfo miAFunc = typeof(A).GetMethod("Func");
        MethodInfo miBReplace = typeof(InstanceMethodTest).GetMethod("FuncReplace", BindingFlags.Static | BindingFlags.Public);

        _hook = new MethodHook(miAFunc, miBReplace);
        _hook.Install();

        // 调用原始A的方法测试
        A a = new A() { val = 5 };
        int ret = a.Func(2);
        string info = string.Format("ret:{0}", ret);
        Debug.Log(info);
        Debug.Assert(ret == 10);
        return info;
    }
    

}
#endif