using DotNetDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public unsafe abstract class CodePatcher
{
    public bool isValid { get; protected set; }

    protected void* _pTarget, _pReplace, _pProxy;
    protected int _jmpCodeSize;
    protected byte[] _targetHeaderBackup;

    public CodePatcher(IntPtr target, IntPtr replace, IntPtr proxy, int jmpCodeSize)
    {
        _pTarget = target.ToPointer();
        _pReplace = replace.ToPointer();
        _pProxy = proxy.ToPointer();
        _jmpCodeSize = jmpCodeSize;
    }

    public void ApplyPatch()
    {
        if (CheckHasHooked()) // TODO 此时可能无法 Uninstall
            return;

        BackupHeader();
        EnableAddrModifiable();
        PatchTargetMethod();
        PatchProxyMethod();
    }

    /// <summary>
    /// 通过检查Signature判断是否已经被hook过（仅Editor下需要）
    /// </summary>
    /// <returns></returns>
    public virtual bool CheckHasHooked() { return false; }

    public void RemovePatch()
    {
        RestoreHeader();
    }

    protected void BackupHeader()
    {
        if (_targetHeaderBackup != null)
            return;

        uint requireSize = LDasm.SizeofMinNumByte(_pTarget, _jmpCodeSize);
        _targetHeaderBackup = new byte[requireSize];

        fixed (void* ptr = _targetHeaderBackup)
            HookUtils.MemCpy(ptr, _pTarget, _targetHeaderBackup.Length);
    }

    protected void RestoreHeader()
    {
        if (_targetHeaderBackup == null)
            return;

        HookUtils.FlushICache(_pTarget, _targetHeaderBackup.Length);

        fixed (void* ptr = _targetHeaderBackup)
            HookUtils.MemCpy(_pTarget, ptr, _targetHeaderBackup.Length);
    }

    protected void PatchTargetMethod()
    {
        HookUtils.FlushICache(_pTarget, _targetHeaderBackup.Length);
        FlushJmpCode(_pTarget, _pReplace);
    }
    protected void PatchProxyMethod()
    {
        HookUtils.FlushICache(_pProxy, _targetHeaderBackup.Length * 2);

        // copy target's code to proxy
        fixed (byte* ptr = _targetHeaderBackup)
            HookUtils.MemCpy(_pProxy, ptr, _targetHeaderBackup.Length);

        // jmp to target's new position
        long jmpFrom = (long)_pProxy + _targetHeaderBackup.Length;
        long jmpTo = (long)_pTarget + _targetHeaderBackup.Length;

        FlushJmpCode((void*)jmpFrom, (void *)jmpTo);
    }
    protected abstract void FlushJmpCode(void* jmpFrom, void* jmpTo);

#if ENABLE_HOOK_DEBUG
    protected string PrintAddrs()
    {
        if (IntPtr.Size == 4)
            return $"target:0x{(uint)_pTarget:x}, replace:0x{(uint)_pReplace:x}, proxy:0x{(uint)_pProxy:x}";
        else
            return $"target:0x{(ulong)_pTarget:x}, replace:0x{(ulong)_pReplace:x}, proxy:0x{(ulong)_pProxy:x}";
    }
#endif

    private void EnableAddrModifiable()
    {
        if (!LDasm.IsIL2CPP())
            return;

        HookUtils.SetAddrFlagsToRWE(new IntPtr(_pTarget), _targetHeaderBackup.Length);
        HookUtils.SetAddrFlagsToRWE(new IntPtr(_pProxy), _targetHeaderBackup.Length + _jmpCodeSize);
    }
}

public unsafe class CodePatcher_x86 : CodePatcher
{
#if UNITY_EDITOR
    /// <summary>
    /// 为了避免Unity2021.3.1后编辑器下没有完整reload从而导致dll被多次hook后Crash的问题，
    /// 给hook代码增加特征代码用于校验
    /// </summary>
    protected byte[] _signatureCode;
#endif

    protected static readonly byte[] s_signatureCode = new byte[]
    {
#if UNITY_EDITOR
        /*
         * 冗余的特征代码,用于检查是否已被Hook过
         * push eax
         * mov eax, 0xbeafdead
         * pop eax
         */
        0x50,
        0xB8, 0xAD, 0xDE, 0xAF, 0xBE,
        0x58
#endif
    };

    //protected static readonly byte[] s_jmpCode = s_signatureCode.Concat(new byte[] // 5 bytes
    //{
    //    0xE9, 0x00, 0x00, 0x00, 0x00,                     // jmp $val   ; $val = $dst - $src - 5 
    //}).ToArray();

    protected static readonly byte[] s_jmpCode = new byte[] // 5 bytes
    {
        0xE9, 0x00, 0x00, 0x00, 0x00,                     // jmp $val   ; $val = $dst - $src - 5 
    };

    protected static readonly int s_signatureLen = s_signatureCode.Length + 1;

    public CodePatcher_x86(IntPtr target, IntPtr replace, IntPtr proxy):base(target, replace, proxy, s_jmpCode.Length) { }

    protected override unsafe void FlushJmpCode(void* jmpFrom, void* jmpTo)
    {
        int val = (int)jmpTo - (int)jmpFrom - 5;

        byte* ptr = (byte*)jmpFrom;
        *ptr = 0xE9;
        int* pOffset = (int*)(ptr + 1);
        *pOffset = (int)val;
    }

    public override bool CheckHasHooked()
    {
        return false;
    }
}

public unsafe class CodePatcher_x64 : CodePatcher_x86 // x64 pathcer code is same to x86
{
    // push rax 与 push eax 二进制相同，都是0x50，因此无需特意处理

    public CodePatcher_x64(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy) { }
}

public unsafe class CodePatcher_arm32_near : CodePatcher
{
    private static readonly byte[] s_jmpCode = new byte[]    // 4 bytes
    {
        0x00, 0x00, 0x00, 0xEA,                         // B $val   ; $val = (($dst - $src) / 4 - 2) & 0x1FFFFFF
    };

    public CodePatcher_arm32_near(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length)
    {
        if (Math.Abs((long)target - (long)replace) >= ((1 << 25) - 1))
            throw new ArgumentException("address offset of target and replace must less than ((1 << 25) - 1)");

#if ENABLE_HOOK_DEBUG
        Debug.Log($"CodePatcher_arm32_near: { PrintAddrs() }");
#endif
    }

    protected override unsafe void FlushJmpCode(void* jmpFrom, void* jmpTo)
    {
        int val = ((int)jmpTo - (int)jmpFrom) / 4 - 2;

        byte* ptr = (byte*)jmpFrom;
        *ptr++ = (byte)val;
        *ptr++ = (byte)(val >> 8);
        *ptr++ = (byte)(val >> 16);
        *ptr++ = 0xEA;
    }
}

public unsafe class CodePatcher_arm32_far : CodePatcher
{
    private static readonly byte[] s_jmpCode = new byte[]    // 8 bytes
    {
        0x04, 0xF0, 0x1F, 0xE5,                         // LDR PC, [PC, #-4]
        0x00, 0x00, 0x00, 0x00,                         // $val
    };

    public CodePatcher_arm32_far(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length)
    {
        if (Math.Abs((long)target - (long)replace) < ((1 << 25) - 1))
            throw new ArgumentException("address offset of target and replace must larger than ((1 << 25) - 1), please use InstructionModifier_arm32_near instead");

#if ENABLE_HOOK_DEBUG
        Debug.Log($"CodePatcher_arm32_far: { PrintAddrs() }");
#endif
    }

    protected override unsafe void FlushJmpCode(void* jmpFrom, void* jmpTo)
    {
        uint* ptr = (uint*)jmpFrom;
        *ptr++ = 0xE51FF004;
        *ptr = (uint)jmpTo;
    }
}

public unsafe class CodePatcher_arm64 : CodePatcher
{
    private static readonly byte[] s_jmpCode = new byte[]    // 4 bytes
    {
        /*
         * from 0x14 to 0x17 is B opcode
         * offset bits is 26
         * https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/B--Branch-
         */
        0x00, 0x00, 0x00, 0x14,                         //  B $val   ; $val = (($dst - $src)/4) & 7FFFFFF
    };

    public CodePatcher_arm64(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length)
    {
        if (Math.Abs((long)target - (long)replace) >= ((1 << 26) - 1) * 4)
            throw new ArgumentException("address offset of target and replace must less than (1 << 26) - 1) * 4");

#if ENABLE_HOOK_DEBUG
        Debug.Log($"CodePatcher_arm64: { PrintAddrs() }");
#endif
    }

    protected override unsafe void FlushJmpCode(void* jmpFrom, void* jmpTo)
    {
        int val = (int)((long)jmpTo - (long)jmpFrom) / 4;

        byte* ptr = (byte*)jmpFrom;
        *ptr++ = (byte)val;
        *ptr++ = (byte)(val >> 8);
        *ptr++ = (byte)(val >> 16);

        byte last = (byte)(val >> 24);
        last &= 0b11;
        last |= 0x14;

        *ptr = last;
    }
}

