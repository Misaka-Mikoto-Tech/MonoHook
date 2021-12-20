#if NET_6
/*
 * Author: Misaka-Mikoto-Tech
 * Desc: 允许在指定内存地址创建一个非托管的 class，不依赖 Emit
 * 
 * eg.
 *      uint size = Factory<TestB>.size;
        void* ptr = NativeMemory.Alloc(size);
        TestB b = Factory<TestB>.CreateAt(ptr);
        b.x = 10;
        b.y = 100;

        void* ptr2 = NativeMemory.Alloc(size); // 复制一份内存测试其内容是否正确
        Buffer.MemoryCopy(ptr, ptr2, size, size);
        
        NativeMemory.Free(ptr); // 释放第一份内存

        TestB b2 = Unsafe.Read<TestB>(&ptr2);
        int val = b2.GetX();
        Console.WriteLine(val);

        NativeMemory.Free(ptr2); // 释放第二份内存
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NativeClass
{
    unsafe struct Factory<T> where T : class, new()
    {
        public static uint size { get; private set; }
        static byte[] header; // vtable *, monitor * and so on

        static readonly Func<IntPtr, T> createFunc;

        /// <summary>
        /// 在指定地址创建对象
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static T CreateAt(void * ptr)
        {
            fixed (void* headerPtr = header) Buffer.MemoryCopy(headerPtr, ptr, header.Length, header.Length);

            return createFunc(new IntPtr(ptr));
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
            createFunc = Unsafe.Read<Func<IntPtr, T>>(Unsafe.AsPointer(ref dummyFunc));
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