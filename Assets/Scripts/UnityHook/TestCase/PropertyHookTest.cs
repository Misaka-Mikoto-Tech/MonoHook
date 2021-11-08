#if ENABLE_HOOK_TEST_CASE
/*
 * 属性 Hook 测试用例
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PropClassA
{
    public int X
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get { return _x; }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        set
        {
            _x = value;
            _x += 2;
            _x -= 1;
            _x *= 2;
            _x /= 2;
            _x -= 1; // code size too short will cause random crash on arm il2cpp
            Debug.LogFormat("original prop X set:{0}", value);
        }
    }
    private int _x;

    public PropClassA(int val)
    {
        _x = val;
    }
}

public class PropClassB
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PropXSetReplace(PropClassA a, int val)
    {
        Debug.LogFormat("PropXSetReplace with value:{0}", val);

        val += 1;
        PropXSetProxy(a, val);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PropXSetProxy(PropClassA a, int val)
    {
        Debug.Log("PropXSetProxy" + val);
    }
}

public class PropertyHookTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Test()
    {
        Type typeA = typeof(PropClassA);
        Type typeB = typeof(PropClassB);

        PropertyInfo pi = typeA.GetProperty("X");
        MethodInfo miASet = pi.GetSetMethod();

        MethodInfo miBReplace = typeB.GetMethod("PropXSetReplace");
        MethodInfo miBProxy = typeB.GetMethod("PropXSetProxy");

        if (miBProxy == null)
            throw new Exception("PropXSetProxy is null");

        MethodHook hook = new MethodHook(miASet, miBReplace, miBProxy);
        hook.Install();

        PropClassA a = new PropClassA(5);
        a.X = 7;
        Debug.Assert(a.X == 8);
    }
	
}
#endif