/*
 Desc: 一个可以运行时 Hook Mono 方法的工具，让你可以无需修改 UnityEditor.dll 等文件就可以重写其函数功能
 Author: Misaka Mikoto
 Github: https://github.com/Misaka-Mikoto-Tech/MonoHook
 */

using DotNetDetour;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


/*
>>>>>>> 原始 UnityEditor.LogEntries.Clear 一型(.net 4.x)
0000000000403A00 < | 55                                 | push rbp                                     |
0000000000403A01   | 48 8B EC                           | mov rbp,rsp                                  |
0000000000403A04   | 48 81 EC 80 00 00 00               | sub rsp,80                                   |
0000000000403A0B   | 48 89 65 B0                        | mov qword ptr ss:[rbp-50],rsp                |
0000000000403A0F   | 48 89 6D A8                        | mov qword ptr ss:[rbp-58],rbp                |
0000000000403A13   | 48 89 5D C8                        | mov qword ptr ss:[rbp-38],rbx                | <<
0000000000403A17   | 48 89 75 D0                        | mov qword ptr ss:[rbp-30],rsi                |
0000000000403A1B   | 48 89 7D D8                        | mov qword ptr ss:[rbp-28],rdi                |
0000000000403A1F   | 4C 89 65 E0                        | mov qword ptr ss:[rbp-20],r12                |
0000000000403A23   | 4C 89 6D E8                        | mov qword ptr ss:[rbp-18],r13                |
0000000000403A27   | 4C 89 75 F0                        | mov qword ptr ss:[rbp-10],r14                |
0000000000403A2B   | 4C 89 7D F8                        | mov qword ptr ss:[rbp-8],r15                 |
0000000000403A2F   | 49 BB 00 2D 1E 1A FE 7F 00 00      | mov r11,7FFE1A1E2D00                         |
0000000000403A39   | 4C 89 5D B8                        | mov qword ptr ss:[rbp-48],r11                |
0000000000403A3D   | 49 BB 08 2D 1E 1A FE 7F 00 00      | mov r11,7FFE1A1E2D08                         |


>>>>>>> 二型(.net 2.x)
0000000000403E8F   | 55                                 | push rbp                                     |
0000000000403E90   | 48 8B EC                           | mov rbp,rsp                                  |
0000000000403E93   | 48 83 EC 70                        | sub rsp,70                                   |
0000000000403E97   | 48 89 65 C8                        | mov qword ptr ss:[rbp-38],rsp                |
0000000000403E9B   | 48 89 5D B8                        | mov qword ptr ss:[rbp-48],rbx                |
0000000000403E9F   | 48 89 6D C0                        | mov qword ptr ss:[rbp-40],rbp                | <<(16)
0000000000403EA3   | 48 89 75 F8                        | mov qword ptr ss:[rbp-8],rsi                 |
0000000000403EA7   | 48 89 7D F0                        | mov qword ptr ss:[rbp-10],rdi                |
0000000000403EAB   | 4C 89 65 D0                        | mov qword ptr ss:[rbp-30],r12                |
0000000000403EAF   | 4C 89 6D D8                        | mov qword ptr ss:[rbp-28],r13                |
0000000000403EB3   | 4C 89 75 E0                        | mov qword ptr ss:[rbp-20],r14                |
0000000000403EB7   | 4C 89 7D E8                        | mov qword ptr ss:[rbp-18],r15                |
0000000000403EBB   | 48 83 EC 20                        | sub rsp,20                                   |
0000000000403EBF   | 49 BB 18 3F 15 13 FE 7F 00 00      | mov r11,7FFE13153F18                         |
0000000000403EC9   | 41 FF D3                           | call r11                                     |
0000000000403ECC   | 48 83 C4 20                        | add rsp,20                                   |

 */


/// <summary>
/// Hook 类，用来 Hook 某个 C# 方法
/// </summary>
public unsafe class MethodHook
{
    public bool isHooked { get; private set; }

    private MethodBase  _targetMethod;       // 需要被hook的目标方法
    private MethodBase  _replacementMethod;  // 被hook后的替代方法
    private MethodBase  _proxyMethod;        // 目标方法的代理方法(可以通过此方法调用被hook后的原方法)

    private IntPtr      _targetPtr;          // 目标方法被 jit 后的地址指针
    private IntPtr      _replacementPtr;
    private IntPtr      _proxyPtr;

    private struct JmpCode
    {
        public int codeSize { get { return code.Length; } }

        public readonly byte[]  code;
        public readonly int     addrOffset;

        public JmpCode(byte[] code_, int addrOffset_) { code = code_; addrOffset = addrOffset_; }
    }

#region jump buffer template define

    private static readonly JmpCode s_jmpCode_x86 = new JmpCode(new byte[] // 6 bytes
    {
        0x68, 0x00, 0x00, 0x00, 0x00,                       // push $val
        0xC3                                                // ret
    }, 1);
    private static readonly JmpCode s_jmpCode_x64 = new JmpCode(new byte[] // 14 bytes
    {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,                 // jmp [rip]
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,     // $val
    }, 6);
    private static readonly JmpCode s_jmpCode_arm32_arm = new JmpCode(new byte[] // 8 bytes
    {
        0x04, 0xF0, 0x1F, 0xE5,                             // LDR PC, [PC, #-4]
        0x00, 0x00, 0x00, 0x00,                             // $val
    }, 4);
    private static readonly JmpCode s_jmpCode_arm64 = new JmpCode(new byte[] //source https://github.com/MonoMod/MonoMod.Common
    {
        0x4F, 0x00, 0x00, 0x58,                         // LDR X15, .+8
        0xE0, 0x01, 0x1F, 0xD6,                         // BR X15
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // $val
    }, 8);

    // arm thumb arch support has removed

    #endregion

    private static readonly JmpCode s_jmpCode;

    private byte[]                  _targetHeaderBackup;

#if UNITY_EDITOR
    /// <summary>
    /// call `MethodInfo.MethodHandle.GetFunctionPointer()` 
    /// will visit static class `UnityEditor.IMGUI.Controls.TreeViewGUI.Styles` and invoke its static constructor,
    /// and init static filed `foldout`, but `GUISKin.current` is null now,
    /// so we should wait until `GUISKin.current` has a valid value
    /// </summary>
    private static FieldInfo s_fi_GUISkin_current;
#endif

    static MethodHook()
    {
        if (LDasm.IsAndroidARM())
            s_jmpCode = IntPtr.Size == 4 ? s_jmpCode_arm32_arm : s_jmpCode_arm64;
        else // x86/x64
            s_jmpCode = IntPtr.Size == 4 ? s_jmpCode_x86 : s_jmpCode_x64;

#if UNITY_EDITOR
        s_fi_GUISkin_current = typeof(GUISkin).GetField("current", BindingFlags.Static | BindingFlags.NonPublic);
#endif
    }

    /// <summary>
    /// 创建一个 Hook
    /// </summary>
    /// <param name="targetMethod">需要替换的目标方法</param>
    /// <param name="replacementMethod">准备好的替换方法</param>
    /// <param name="proxyMethod">如果还需要调用原始目标方法，可以通过此参数的方法调用，如果不需要可以填 null</param>
    public MethodHook(MethodBase targetMethod, MethodBase replacementMethod, MethodBase proxyMethod = null)
    {
        _targetMethod       = targetMethod;
        _replacementMethod  = replacementMethod;
        _proxyMethod        = proxyMethod;
    }

    public void Install()
    {
        if (_targetMethod == null || _replacementMethod == null)
            throw new Exception("MethodHook:_targetMethod and _replacementMethod can not be null");

        if (LDasm.IsiOS()) // iOS 不支持修改 code 所在区域 page
            return;

        if (isHooked)
            return;

#if UNITY_EDITOR
        if (s_fi_GUISkin_current.GetValue(null) != null)
            DoInstall();
        else
            EditorApplication.update += OnEditorUpdate;
#else
            DoInstall();
#endif
    }

    public void Uninstall()
    {
        if (!isHooked)
            return;

        byte* pTarget = (byte*)_targetPtr.ToPointer();
        for (int i = 0; i < _targetHeaderBackup.Length; i++)
            *pTarget++ = _targetHeaderBackup[i];

        isHooked = false;
        HookPool.RemoveHooker(_targetMethod);
    }

    #region private
    private void DoInstall()
    {
        HookPool.AddHooker(_targetMethod, this);

        if(GetFunctionAddr())
        {
            BackupHeader();
            PatchTargetMethod();
            PatchProxyMethod();
        }
        
        isHooked = true;
    }

    /// <summary>
    /// 获取对应函数jit后的native code的地址
    /// </summary>
    private bool GetFunctionAddr()
    {
        _targetPtr = GetFunctionAddr(_targetMethod);
        _replacementPtr = GetFunctionAddr(_replacementMethod);
        if (_proxyMethod != null)
            _proxyPtr = GetFunctionAddr(_proxyMethod);

        if (_targetPtr == IntPtr.Zero || _proxyPtr == IntPtr.Zero)
            return false;

        if(LDasm.IsThumb(_targetPtr) || LDasm.IsThumb(_replacementPtr))
        {
            throw new Exception("does not support thumb arch");
        }

        return true;
    }

    /// <summary>
    /// 备份原始方法头
    /// </summary>
    private void BackupHeader()
    {
        byte* pTarget = (byte*)_targetPtr.ToPointer();

        uint requireSize = LDasm.SizeofMinNumByte(pTarget, s_jmpCode.codeSize);
        _targetHeaderBackup = new byte[requireSize];

        for (int i = 0, imax = _targetHeaderBackup.Length; i < imax; i++)
            _targetHeaderBackup[i] = *pTarget++;
    }

    // 将原始方法跳转到我们的方法
    private void PatchTargetMethod()
    {
        EnableAddrModifiable(_targetPtr, _targetHeaderBackup.Length);

        byte* pTarget = (byte*)_targetPtr.ToPointer();
        byte* pAddr = pTarget + s_jmpCode.addrOffset;

        for (int i = 0, imax = s_jmpCode.codeSize; i < imax; i++)
            *pTarget++ = s_jmpCode.code[i];

        if (IntPtr.Size == 4)
            *(uint*)pAddr = (uint)_replacementPtr.ToInt32();
        else
            *(ulong*)pAddr = (ulong)_replacementPtr.ToInt64();
    }

    /// <summary>
    /// 让 Proxy 方法的功能变成跳转向原始方法
    /// </summary>
    private void PatchProxyMethod()
    {
        EnableAddrModifiable(_proxyPtr, _targetHeaderBackup.Length + s_jmpCode.codeSize);

        byte * pProxy = (byte*)_proxyPtr.ToPointer();

        // fill backuped original target header code
        for (int i = 0; i < _targetHeaderBackup.Length; i++)
            *pProxy++ = _targetHeaderBackup[i];

        // fill jump code
        byte* pAddr = pProxy + s_jmpCode.addrOffset;
        for (int i = 0, imax = s_jmpCode.codeSize; i < imax; i++)
            *pProxy++ = s_jmpCode.code[i];

        // fill jmp addr value(to target method)
        if (IntPtr.Size == 4)
            *(uint*)pAddr = (uint)_targetPtr.ToInt32() + (uint)_targetHeaderBackup.Length;
        else
            *(ulong*)pAddr = (ulong)_targetPtr.ToInt64() + (ulong)_targetHeaderBackup.Length;
    }

    private void EnableAddrModifiable(IntPtr ptr, int size)
    {
        if (!LDasm.IsIL2CPP())
            return;

        IL2CPPHelper.SetAddrFlagsToRWE(ptr, size);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)] // 好像在 IL2CPP 里无效
    private struct __ForCopy
    {
        public long         __dummy;
        public MethodBase   method;
    }
    /// <summary>
    /// 获取方法指令地址
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    private IntPtr GetFunctionAddr(MethodBase method)
    {
        if (!LDasm.IsIL2CPP())
            return method.MethodHandle.GetFunctionPointer();
        else
        {
            /*
                // System.Reflection.MonoMethod
                typedef struct Il2CppReflectionMethod
                {
                    Il2CppObject object;
                    const MethodInfo *method;
                    Il2CppString *name;
                    Il2CppReflectionType *reftype;
                } Il2CppReflectionMethod;

                typedef Il2CppClass Il2CppVTable;
                typedef struct Il2CppObject
                {
                    union
                    {
                        Il2CppClass *klass;
                        Il2CppVTable *vtable;
                    };
                    MonitorData *monitor;
                } Il2CppObject;

            typedef struct MethodInfo
            {
                Il2CppMethodPointer methodPointer; // this is the pointer to native code of method
                InvokerMethod invoker_method;
                const char* name;
                Il2CppClass *klass;
                const Il2CppType *return_type;
                const ParameterInfo* parameters;
            // ...
            }
             */

            __ForCopy __forCopy = new __ForCopy() { method = method };

            long* ptr = &__forCopy.__dummy;
            ptr++; // addr of _forCopy.method

            IntPtr methodAddr = IntPtr.Zero;
            if(sizeof(IntPtr) == 8)
            {
                long methodDataAddr = *(long*)ptr;
                byte* ptrData = (byte *)methodDataAddr + sizeof(IntPtr) * 2; // offset of Il2CppReflectionMethod::const MethodInfo *method;

                long methodPtr = 0;
                methodPtr = *(long*)ptrData;
                methodAddr = new IntPtr(*(long*)methodPtr); // MethodInfo::Il2CppMethodPointer methodPointer;
            }
            else
            {
                int methodDataAddr = *(int*)ptr;
                byte* ptrData = (byte *)methodDataAddr + sizeof(IntPtr) * 2; // offset of Il2CppReflectionMethod::const MethodInfo *method;

                int methodPtr = 0;
                methodPtr = *(int*)ptrData;
                methodAddr = new IntPtr(*(int*)methodPtr);
            }
            return methodAddr;
        }
    }

#if UNITY_EDITOR
    private void OnEditorUpdate()
    {
        if(s_fi_GUISkin_current.GetValue(null) != null)
        {
            DoInstall();
            EditorApplication.update -= OnEditorUpdate;
        }
    }
#endif

#endregion
}
