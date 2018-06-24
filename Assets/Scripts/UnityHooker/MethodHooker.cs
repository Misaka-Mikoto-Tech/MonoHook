/*
 Desc: 一个可以运行时 Hook Mono 方法的工具，让你可以无需修改 UnityEditor.dll 等文件就可以重写其函数功能
 Author: Misaka Mikoto
 Github: https://github.com/easy66/MonoHooker
 */

#if UNITY_EDITOR
using System;
using System.Reflection;


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
0000000000403A47   | 4C 89 5D C0                        | mov qword ptr ss:[rbp-40],r11                |
0000000000403A4B   | 65 48 8B 04 25 80 17 00 00         | mov rax,qword ptr gs:[1780]                  | rax:EntryPoint
0000000000403A54   | 48 85 C0                           | test rax,rax                                 | rax:EntryPoint
0000000000403A57   | 48 74 07                           | je mgd.403A61                                |
0000000000403A5A   | 48 8B 80 C8 01 00 00               | mov rax,qword ptr ds:[rax+1C8]               | rax:EntryPoint, rax+1C8:sub_403BD0+8
0000000000403A61   | 48 8B F0                           | mov rsi,rax                                  | rax:EntryPoint
0000000000403A64   | 48 83 C6 10                        | add rsi,10                                   |
0000000000403A68   | 48 8B C5                           | mov rax,rbp                                  | rax:EntryPoint
0000000000403A6B   | 48 83 C0 A0                        | add rax,FFFFFFFFFFFFFFA0                     | rax:EntryPoint
0000000000403A6F   | 48 8B 0E                           | mov rcx,qword ptr ds:[rsi]                   |
0000000000403A72   | 48 89 4D A0                        | mov qword ptr ss:[rbp-60],rcx                |
0000000000403A76   | 48 89 06                           | mov qword ptr ds:[rsi],rax                   | rax:EntryPoint
0000000000403A79   | 41 BB 00 00 00 00                  | mov r11d,0                                   |
0000000000403A7F   | 4D 85 DB                           | test r11,r11                                 |
0000000000403A82   | 74 07                              | je mgd.403A8B                                |
0000000000403A84   | 4C 8B 5D C0                        | mov r11,qword ptr ss:[rbp-40]                |
0000000000403A88   | 41 FF 13                           | call qword ptr ds:[r11]                      |
0000000000403A8B   | 90                                 | nop                                          |


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

>>>>> ProxyCall(13字节)
0000000000403C0A   | 49 BB 08 2D 1E 1A FE 7F 00 00      | mov r11,7FFE1A1E2D08                         |
0000000000403C14   | 41 FF E3                           | jmp r11                                      |
 */


/// <summary>
/// Hook 类，用来 Hook 某个 C# 方法
/// </summary>
public unsafe class MethodHooker
{
    public bool isHooked { get; private set; }

    private MethodInfo  _targetMethod;       // 需要被hook的目标方法
    private MethodInfo  _replacementMethod;  // 被hook后的替代方法
    private MethodInfo  _proxyMethod;        // 目标方法的代理方法(可以通过此方法调用被hook后的原方法)

    private IntPtr      _targetPtr;          // 目标方法被 jit 后的地址指针
    private IntPtr      _replacementPtr;
    private IntPtr      _proxyPtr;

    private static readonly byte[] s_jmpBuff = new byte[]
    {
        0x49, 0xBB,                                     // mov r11,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // $val
        0x41, 0xFF, 0xE3                                // jmp r11
    };

    private static readonly byte[] s_proxyBuff_4 = new byte[] // .net 4.x
    {
        0x55,                                       // push rbp
        0x48, 0x8B, 0xEC,                           // mov rbp,rsp
        0x48, 0x81, 0xEC, 0x80, 0x00, 0x00, 0x00,   // sub rsp,80 // unity 2017 是 90, 因此改为从原始数据 copy
        0x48, 0x89, 0x65, 0xB0,                     // mov qword ptr ss:[rbp-50],rsp
        0x48, 0x89, 0x6D, 0xA8,                     // mov qword ptr ss:[rbp-58],rbp

        // _jmpBuff
    };

    private static readonly byte[] s_proxyBuff_2 = new byte[] // .net 2.x
    {
        0x55,                       // push rbp
        0x48, 0x8B, 0xEC,           // mov rbp,rsp
        0x48, 0x83, 0xEC, 0x70,     // sub rsp,70
        0x48, 0x89, 0x65, 0xC8,     // mov qword ptr ss:[rbp-38],rsp
        0x48, 0x89, 0x5D, 0xB8      // mov qword ptr ss:[rbp-48],rbx

        // _jmpBuff
    };

    /// <summary>
    /// 代码类型
    /// </summary>
    private enum CodeType
    {
        Net4, 
        Net2,
    }

    private byte[]      _jmpBuff;
    private byte[]      _proxyBuff;
    private CodeType    _codeType;
    

    public MethodHooker(MethodInfo targetMethod, MethodInfo replacementMethod, MethodInfo proxyMethod)
    {
        _targetMethod       = targetMethod;
        _replacementMethod  = replacementMethod;
        _proxyMethod        = proxyMethod;

        _targetPtr      = _targetMethod.MethodHandle.GetFunctionPointer();
        _replacementPtr = _replacementMethod.MethodHandle.GetFunctionPointer();
        _proxyPtr       = _proxyMethod.MethodHandle.GetFunctionPointer();

        _jmpBuff = new byte[s_jmpBuff.Length];
    }

    public bool Install()
    {
        if (isHooked)
            return true;
        if (!ValidateTargetCode())
            return false;

        BackupHeader();
        PatchTargetMethod();
        PatchProxyMethod();

        isHooked = true;
        return true;
    }

    public void Uninstall()
    {
        if (!isHooked)
            return;

        byte* pTarget = (byte*)_targetPtr.ToPointer();
        for (int i = 0; i < _proxyBuff.Length; i++)
            *pTarget++ = _proxyBuff[i];
    }

    #region private
    /// <summary>
    ///  判断此方法是否可以被 Hook, 要求有固定的函数头(绝大部分 jit 代码都是这样的)
    /// </summary>
    /// <returns></returns>
    private bool ValidateTargetCode()
    {
        byte* pTarget = (byte*)_targetPtr.ToPointer();

        for(int i = 0; i <= 4; i++)
        {
            if (*pTarget++ != s_proxyBuff_4[i])
                return false;
        }

        if (*pTarget == 0x81) // .net 4.x
        {
            _codeType = CodeType.Net4;
            _proxyBuff = new byte[s_proxyBuff_4.Length];

            pTarget += 6;
        }
        else if (*pTarget == 0x83) // .net 2.x
        {
            _proxyBuff = new byte[s_proxyBuff_2.Length];
            _codeType = CodeType.Net2;

            pTarget += 3;
        }

        if (*pTarget++ != 0x48) return false;
        if (*pTarget++ != 0x89) return false;
        pTarget += 2;
        if (*pTarget++ != 0x48) return false;
        if (*pTarget++ != 0x89) return false;

        return true;
    }

    /// <summary>
    /// 因为原始数据不同 Mono 版本有稍许不一致，因此把数据备份一下
    /// </summary>
    private void BackupHeader()
    {
        byte* pTarget = (byte*)_targetPtr.ToPointer();
        for (int i = 0; i < _proxyBuff.Length; i++)
            _proxyBuff[i] = *pTarget++;
    }

    // 将原始方法跳转到我们的方法
    private void PatchTargetMethod()
    {
        Array.Copy(s_jmpBuff, _jmpBuff, _jmpBuff.Length);
        fixed (byte* p = &_jmpBuff[2])
        {
            *((ulong*)p) = (ulong)_replacementPtr.ToInt64();
        }

        byte* pTarget = (byte*)_targetPtr.ToPointer();
        for (int i = 0; i < _jmpBuff.Length; i++)
            *pTarget++ = _jmpBuff[i];
    }

    /// <summary>
    /// 让 Proxy 方法的功能变成跳转向原始方法
    /// </summary>
    private void PatchProxyMethod()
    {
        byte* pProxy = (byte*)_proxyPtr.ToPointer();
        for (int i = 0; i < _proxyBuff.Length; i++)     // 先填充头
            *pProxy++ = _proxyBuff[i];

        fixed (byte* p = &_jmpBuff[2])                  // 将跳转指向原函数跳过头的位置
        {
            *((ulong*)p) = (ulong)(_targetPtr.ToInt64() + _proxyBuff.Length);
        }

        for (int i = 0; i < _jmpBuff.Length; i++)       // 再填充跳转
            *pProxy++ = _jmpBuff[i];
    }

    #endregion
}
#endif