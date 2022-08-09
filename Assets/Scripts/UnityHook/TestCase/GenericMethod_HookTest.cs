#if ENABLE_HOOK_TEST_CASE
/*
 * 泛型方法 Hook（目前仅处理了 IL2CPP，mono的会crash）
 * 
 * 查看 il2cpp 函数定义会发现尾部多了一个 RuntimeMethod* 类型的参数
 * 此参数在调用非泛型方法时总是传递 null, 而在调用泛型方法时传递泛型方法实例化后的方法的 RuntimeMethod 指针
 * 
 * 例如 `T UnityEngine.Object::Instantiate<System.Object>(T)` 的 il2cpp 定义为:
 * IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Object_Instantiate_TisRuntimeObject_mCD6FC6BB14BA9EF1A4B314841EB4D40675E3C1DB_gshared (RuntimeObject* ___original0, const RuntimeMethod* method) 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MonoHook.Test
{
    public class GenericMethod_HookTest
    {
        private static MethodHook _hook;

        public static void Install()
        {
            if (!DotNetDetour.LDasm.IsIL2CPP())
                return;

            if (_hook == null)
            {
                // 仅用作示例，此处实际应该去hook `UnityEngine.Object.Internal_CloneSingle`，而不是去hook它的上层泛型方法
                MethodInfo gMiTarget = new Func<Texture2D, Texture2D>(UnityEngine.Object.Instantiate).Method.GetGenericMethodDefinition();
                MethodInfo miTarget = gMiTarget.MakeGenericMethod(typeof(UnityEngine.Object));

                MethodInfo miReplacement = new Func<UnityEngine.Object, UnityEngine.Object>(GameInstanceNew).Method;
                MethodInfo miProxy = new Func<UnityEngine.Object, UnityEngine.Object>(GameInstanceProxy).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static T GameInstanceNew<T>(T go) where T : UnityEngine.Object
        {
            Debug.LogFormat("【自定义实现】Object.Instantiate(Object original), Prefab名称：{0}", go.name);

            /* 此处不能写 GameInstanceProxy<T>(go); 或者 GameInstanceProxy(go);
             * 否则会在生成 il2cpp 代码时使用固定索引 1 从 RuntimeMethod 动态取函数地址，导致异常(当前函数索引为0)
             * 而使用 GameInstanceProxy<UnityEngine.Object>(go); 则会在编译期就确定函数地址, 不会动态获取, 
             * 此时 RuntimeInfo 参数会传递 `GameInstanceProxy<Object>`， 但由于此参数主要用来校验参数类型，而 GameInstanceProxy 的参数类型与原有定义一致，因此不会报错
             */
            var obj = GameInstanceProxy<UnityEngine.Object>(go);
            return obj as T;
        }
        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static T GameInstanceProxy<T>(T go) where T : UnityEngine.Object
        {
            // 此处的函数实现永远不会起作用，随便写
            Debug.Log("something" + go.ToString());
            return null;
        }
    }
}
#endif