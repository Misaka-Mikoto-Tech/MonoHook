#if NET6_0
/*
 * Author: Misaka-Mikoto-Tech
 * Desc: 允许在指定内存地址创建一个非托管的 class，不依赖 Emit
 * 
 * eg.
 *      TestA a = Factory<TestB>.Create();
        a.x = 10;

        int val = a.GetX();
        Console.WriteLine(val);
        a.Free();
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Native
{
    unsafe abstract class NativeClass
    {
        private void* _ptr;
        public void SetPtr(void* ptr)
        {
            if (_ptr != null) throw new Exception("ptr can only be assigned once");
            _ptr = ptr;
        }
        public void Free() { if (_ptr != null) NativeMemory.Free(_ptr); }
    }

    unsafe struct Factory<T> where T : NativeClass, new()
    {
        public static uint size { get; private set; }
        static byte[] header; // vtable *, monitor * and so on

        static readonly Func<IntPtr, T> cast2T;

        /// <summary>
        /// 在指定地址创建对象
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static T Create(void * ptr = null)
        {
            if (ptr == null) ptr = NativeMemory.Alloc(size);

            fixed (void* headerPtr = header) Buffer.MemoryCopy(headerPtr, ptr, header.Length, header.Length);

            T ret = cast2T(new IntPtr(ptr));
            ret.SetPtr(ptr);
            return ret;
        }

        public static object GetO_Dummy(object obj)
        {
            return obj;
        }

        static Factory()
        {
            IntPtr tptr = typeof(T).TypeHandle.Value;
            size = (uint)Marshal.ReadInt32(tptr, 4);

            T obj = new T();
            void* pObj = Unsafe.AsPointer(ref obj);

            header = new byte[MinClass.size];
            fixed (void* headerPtr = header)
                Buffer.MemoryCopy((void*)*(long*)pObj, headerPtr, header.Length, header.Length);

            var dummyFunc = GetO_Dummy;
            cast2T = Unsafe.Read<Func<IntPtr, T>>(Unsafe.AsPointer(ref dummyFunc));
        }
    }

    class MinClass
    {
        public static uint size { get; private set; }
        static MinClass()
        {
            IntPtr tptr = typeof(MinClass).TypeHandle.Value;
            size = (uint)Marshal.ReadInt32(tptr, 4);
        }
    }
}
#endif