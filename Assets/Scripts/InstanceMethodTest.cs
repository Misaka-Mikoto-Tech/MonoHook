using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


public class A
{
    public int val;

    public int Func(int x)
    {
        Debug.Log("call of A.Func");
        return x + val;
    }
}

public class B
{
    public static int FuncReplace(A a, int x) // 目前没找到合法传递非本类 this 的方法，只好用 static 凑合了
    {
        Debug.Log("call of B.Func");
        x += 1;
        a.val = 7;
        // 修改参数后调用原方法
        return FuncProxy(a, x);
    }

    public static int FuncProxy(A a, int x)
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

    public void Test()
    {
        Type typeA = typeof(A);
        Type typeB = typeof(B);

        MethodInfo miAFunc = typeA.GetMethod("Func");
        MethodInfo miBReplace = typeB.GetMethod("FuncReplace");
        MethodInfo miBProxy = typeB.GetMethod("FuncProxy");

        MethodHooker hooker = new MethodHooker(miAFunc, miBReplace, miBProxy);
        hooker.Install();

        // 调用原始A的方法测试
        A a = new A() { val = 5 };
        int ret = a.Func(2);
        Debug.LogFormat("ret:{0}", ret);
        Debug.Assert(ret == 10);
    }
    

}
