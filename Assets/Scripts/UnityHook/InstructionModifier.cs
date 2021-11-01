using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public unsafe class InstructionModifier
{
    public bool isValid { get; protected set; }

    protected void* _target, _replace, _proxy;

    public InstructionModifier(void* target, void* replace, void* proxy)
    {
        _target = target;
        _replace = replace;
        _proxy = proxy;
    }
}

public unsafe class InstructionModifier_x86 : InstructionModifier
{
    private static readonly byte[] code = new byte[]    // 5 bytes
    {
        0xE9, 0x00, 0x00, 0x00, 0x00,                   // jmp $val   ; $val = $dst - $src - 5 
    };

    public InstructionModifier_x86(void* target, void* replace, void* proxy):base(target, replace, proxy) { }
}

public unsafe class InstructionModifier_x64 : InstructionModifier
{
    private static readonly byte[] code = new byte[]    // 5 bytes
    {
        0xE9, 0x00, 0x00, 0x00, 0x00,                   // jmp $val   ; $val = $dst - $src - 5 
    };

    public InstructionModifier_x64(void* target, void* replace, void* proxy) : base(target, replace, proxy) { }
}

public unsafe class InstructionModifier_arm32_near : InstructionModifier
{
    private static readonly byte[] code = new byte[]    // 4 bytes
    {
        0x00, 0x00, 0x00, 0xEA,                         // B $val   ; $val = (($dst - $src) / 4 - 2) & 0x1FFFFFF
    };

    public InstructionModifier_arm32_near(void* target, void* replace, void* proxy) : base(target, replace, proxy)
    {
        if (Math.Abs((long)target - (long)replace) >= ((1 << 25) - 1))
            throw new ArgumentException("address offset of target and replace must less than ((1 << 25) - 1)");
    }
}

public unsafe class InstructionModifier_arm32_far : InstructionModifier
{
    private static readonly byte[] code = new byte[]    // 8 bytes
    {
        0x04, 0xF0, 0x1F, 0xE5,                         // LDR PC, [PC, #-4]
        0x00, 0x00, 0x00, 0x00,                         // $val
    };

    public InstructionModifier_arm32_far(void* target, void* replace, void* proxy) : base(target, replace, proxy)
    {
        if (Math.Abs((long)target - (long)replace) < ((1 << 25) - 1))
            throw new ArgumentException("address offset of target and replace must larger than ((1 << 25) - 1), please use InstructionModifier_arm32_near instead");
    }
}

public unsafe class InstructionModifier_arm64 : InstructionModifier
{
    private static readonly byte[] code = new byte[]    // 4 bytes
    {
        /*
         * from 0x14 to 0x17 is B opcode
         * offset bits is 26
         * https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/B--Branch-
         */
        0x00, 0x00, 0x00, 0x14,                         //  B $val   ; $val = (($dst - $src)/4) & 7FFFFFF
    };

    public InstructionModifier_arm64(void* target, void* replace, void* proxy) : base(target, replace, proxy)
    {
        if (Math.Abs((long)target - (long)replace) >= ((1 << 27) - 1))
            throw new ArgumentException("address offset of target and replace must less than (1 << 27) - 1)");
    }
}

