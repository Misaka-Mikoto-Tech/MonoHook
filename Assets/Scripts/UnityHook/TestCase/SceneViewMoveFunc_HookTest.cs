#if ENABLE_HOOK_TEST_CASE
#if UNITY_EDITOR
/*
 * 测试修改SceneView摄像机加速移动函数的实现（默认是 Mathf.Pow(1.8f, deltaTime)）
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEditor.AnimatedValues;
using System.Runtime.CompilerServices;

namespace MonoHook.Test
{
    //[InitializeOnLoad] // 取消此行注释即生效（建议使用unity2020, 因为有几个变量名称与unity2019不一致，此代码未做兼容）
    public static class SceneViewMoveFunc_HookTest
    {
        #region 反射定义原有字段
        private static Vector3 s_Motion
        {
            get => (Vector3)_fi_s_Motion.GetValue(null);
            set => _fi_s_Motion.SetValue(null, value);
        }

        private static bool s_Moving
        {
            get => (bool)_fi_s_Moving.GetValue(null);
            set => _fi_s_Moving.SetValue(null, value);
        }

        private static float s_FlySpeedTarget
        {
            get => (float)_fi_s_FlySpeedTarget.GetValue(null);
            set => _fi_s_FlySpeedTarget.SetValue(null, value);
        }

        /// <summary>
        /// 此属性每帧只能调用一次（因为内部是调用的Timeer.Update）
        /// </summary>
        private static float s_deltaTime
        {
            get => (float)_mi_deltaTime.Invoke(null, null);
        }

        private static SceneView s_CurrentSceneView
        {
            // 此变量与当前context有关，因此每次使用都必须即时获取
            get => _fi_s_CurrentSceneView.GetValue(null) as SceneView;
        }

        private static Type _sceneViewMotionType;
        // 这几个字段是值类型，因为不能直接获取对象引用，每次都需要使用反射读取或者设置
        private static FieldInfo _fi_s_Motion;
        private static FieldInfo _fi_s_Moving;
        private static FieldInfo _fi_s_FlySpeedTarget;
        private static FieldInfo _fi_s_CurrentSceneView;
        private static MethodInfo _mi_deltaTime;

        private static AnimVector3 s_FlySpeed;

        private const float k_FlySpeed = 9f;
        private const float k_FlySpeedAcceleration = 1.8f;
        #endregion

        private static MethodHook _hook;

        /// <summary>
        /// 自定义SceneView加速移动函数
        /// </summary>
        /// <returns></returns>
        private static float CustomAccMoveFunction(float deltaTime)
        {
            float FlySpeedTarget = s_FlySpeedTarget;
            float speed = (FlySpeedTarget < Mathf.Epsilon) ? k_FlySpeed : (FlySpeedTarget * Mathf.Pow(k_FlySpeedAcceleration, deltaTime));

            return speed;
        }

        static SceneViewMoveFunc_HookTest()
        {
            if (_hook == null)
            {
                _sceneViewMotionType = typeof(BuildPipeline).Assembly.GetType("UnityEditor.SceneViewMotion");

                // 反射原有字段和属性
                {
                    _fi_s_Motion = _sceneViewMotionType.GetField("s_Motion", BindingFlags.Static | BindingFlags.NonPublic);
                    _fi_s_Moving = _sceneViewMotionType.GetField("s_Moving", BindingFlags.Static | BindingFlags.NonPublic);
                    _fi_s_FlySpeedTarget = _sceneViewMotionType.GetField("s_FlySpeedTarget", BindingFlags.Static | BindingFlags.NonPublic);
                    _fi_s_CurrentSceneView = _sceneViewMotionType.GetField("s_CurrentSceneView", BindingFlags.Static | BindingFlags.NonPublic);

                    s_FlySpeed = _sceneViewMotionType.GetField("s_FlySpeed", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as AnimVector3;

                    _mi_deltaTime = typeof(BuildPipeline).Assembly.GetType("UnityEditor.CameraFlyModeContext")
                        .GetProperty("deltaTime", BindingFlags.Static | BindingFlags.Public).GetGetMethod();
                }


                MethodInfo miTarget = _sceneViewMotionType.GetMethod("GetMovementDirection", BindingFlags.Static | BindingFlags.NonPublic);

                MethodInfo miReplacement = new Func<Vector3>(GetMovementDirectionNew).Method;
                MethodInfo miProxy = new Func<Vector3>(GetMovementDirectionProxy).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();

                Debug.Log("已重定义SceneView摄像机移动速度");
            }
        }

        /// <summary>
        /// 重写原有的完整的移动逻辑(此方法也可以被完全修改)
        /// </summary>
        /// <returns></returns>
        private static Vector3 GetMovementDirectionNew()
        {
            //return GetMovementDirectionProxy();

            s_Moving = s_Motion.sqrMagnitude > 0f;
            var _CurrentSceneView = s_CurrentSceneView; // 缓存变量以避免多次反射调用
            float speed = _CurrentSceneView.cameraSettings.speed;
            float deltaTime = s_deltaTime;              // s_deltaTime 不可被多次访问
            if (Event.current.shift)
            {
                speed *= 5f;
            }
            if (s_Moving)
            {
                if (_CurrentSceneView.cameraSettings.accelerationEnabled)
                {
                    s_FlySpeedTarget = CustomAccMoveFunction(deltaTime); // 自定义加速移动函数
                }
                else
                {
                    s_FlySpeedTarget = k_FlySpeed;
                }
            }
            else
            {
                s_FlySpeedTarget = 0f;
            }
            if (_CurrentSceneView.cameraSettings.easingEnabled)
            {
                s_FlySpeed.speed = 1f / _CurrentSceneView.cameraSettings.easingDuration;
                s_FlySpeed.target = (s_Motion.normalized * s_FlySpeedTarget) * speed;
            }
            else
            {
                s_FlySpeed.value = (s_Motion.normalized * s_FlySpeedTarget) * speed;
            }
            return s_FlySpeed.value * deltaTime;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static Vector3 GetMovementDirectionProxy()
        {
            // dummy
            return Vector3.zero;
        }


    }
}
#endif
#endif