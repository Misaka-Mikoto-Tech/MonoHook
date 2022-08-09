#if ENABLE_HOOK_TEST_CASE && !UNITY_2019_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Reflection;

public class SceneViewHook
{
    private static MethodHook _hook;

    //[DidReloadScripts]
    public static void InstallHook()
    {
        //MethodInfo 
    }
}

#endif