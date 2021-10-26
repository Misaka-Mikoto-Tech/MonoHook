using DotNetDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

public static class IL2CPPHelper
{
    /// <summary>
    /// set flags of address to `read write execute`
    /// </summary>
    public static void SetAddrFlagsToRWE(IntPtr ptr, int size)
    {
#if UNITY_STANDALONE_WIN

        uint oldProtect;
        bool ret = VirtualProtect(ptr, (uint)size, Protection.PAGE_EXECUTE_READWRITE, out oldProtect);
        UnityEngine.Debug.Assert(ret);

#elif UNITY_ANDROID

    SetMemPerms(ptr,size,MmapProts.PROT_READ | MmapProts.PROT_WRITE | MmapProts.PROT_EXEC);

#endif
    }

#if UNITY_STANDALONE_WIN
    [Flags]
    public enum Protection
    {
        PAGE_NOACCESS           = 0x01,
        PAGE_READONLY           = 0x02,
        PAGE_READWRITE          = 0x04,
        PAGE_WRITECOPY          = 0x08,
        PAGE_EXECUTE            = 0x10,
        PAGE_EXECUTE_READ       = 0x20,
        PAGE_EXECUTE_READWRITE  = 0x40,
        PAGE_EXECUTE_WRITECOPY  = 0x80,
        PAGE_GUARD              = 0x100,
        PAGE_NOCACHE            = 0x200,
        PAGE_WRITECOMBINE       = 0x400
    }

    [DllImport("kernel32")]
    public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out uint lpflOldProtect);

#elif UNITY_ANDROID
    [Flags]
    public enum MmapProts : int {
        PROT_READ       = 0x1,
        PROT_WRITE      = 0x2,
        PROT_EXEC       = 0x4,
        PROT_NONE       = 0x0,
        PROT_GROWSDOWN  = 0x01000000,
        PROT_GROWSUP    = 0x02000000,
    }

    static IL2CPPHelper()
    {
        PropertyInfo p_SystemPageSize = typeof(Environment).GetProperty("SystemPageSize");
        if (p_SystemPageSize == null)
            throw new NotSupportedException("Unsupported runtime");
        _Pagesize = (int) p_SystemPageSize.GetValue(null, new object[0]);
        
    }
    [DllImport("libc", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mprotect(IntPtr start, IntPtr len, MmapProts prot);
    
    private static readonly long _Pagesize;
    public static unsafe void SetMemPerms(IntPtr start, ulong len, MmapProts prot) {
        long pagesize = _Pagesize;
        long startPage = ((long) start) & ~(pagesize - 1);
        long endPage = ((long) start + (long) len + pagesize - 1) & ~(pagesize - 1);

        if (mprotect((IntPtr) startPage, (IntPtr) (endPage - startPage), prot) != 0)
            throw new Win32Exception();
    }

#endif

}



