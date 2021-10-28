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
        get { return _x; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        set
        {
            _x = value;
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
    public void PropXSetReplace(int val)
    {
        Debug.LogFormat("PropXSetReplace with value:{0}", val);

        val += 1;

        Debug.Assert(this.GetType() == typeof(PropClassA));

        HookPool.GetHook(typeof(PropClassA).GetProperty("X").GetSetMethod()).RunWithoutPatch(this, val);
    }
}

public class PropertyHookTest
{
    public static MethodHook hook;
    public void Test()
    {
        Type typeA = typeof(PropClassA);
        Type typeB = typeof(PropClassB);

        PropertyInfo pi = typeA.GetProperty("X");
        MethodInfo miASet = pi.GetSetMethod();

        MethodInfo miBReplace = typeB.GetMethod("PropXSetReplace");

        hook = new MethodHook(miASet, miBReplace);
        hook.Install();

        PropClassA a = new PropClassA(5);
        a.X = 7;
        Debug.Assert(a.X == 8);
    }
	
}
#endif