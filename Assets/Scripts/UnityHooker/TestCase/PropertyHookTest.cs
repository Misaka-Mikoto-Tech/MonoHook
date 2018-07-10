/*
 * 属性 Hook 测试用例
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PropClassA
{
    public int X
    {
        get { return _x; }
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
    public static void PropXSetReplace(PropClassA a, int val)
    {
        Debug.LogFormat("PropXSetReplace with value:{0}", val);

        val += 1;
        PropXSetProxy(a, val);
    }

    public static void PropXSetProxy(PropClassA a, int val)
    {
    }
}

public class PropertyHookTest
{
    public void Test()
    {
        Type typeA = typeof(PropClassA);
        Type typeB = typeof(PropClassB);

        PropertyInfo pi = typeA.GetProperty("X");
        MethodInfo miASet = pi.GetSetMethod();

        MethodInfo miBReplace = typeB.GetMethod("PropXSetReplace");
        MethodInfo miBProxy = typeB.GetMethod("PropXSetProxy");

        MethodHooker hooker = new MethodHooker(miASet, miBReplace, miBProxy);
        hooker.Install();

        PropClassA a = new PropClassA(5);
        a.X = 7;
        Debug.Assert(a.X == 8);
    }
	
}
