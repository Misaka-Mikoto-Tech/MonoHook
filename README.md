# MonoHooker
本代码的功能是运行时修改C#函数
## 特点：
* 运行时直接修改内存中的 jit 代码，不会修改 UnityEditor.dll 等文件，避免让别人修改文件的尴尬
* 同时支持 .net 2.x 与 .net 4.x
* 目前测试支持 unity 2017 与 unity 2018

## 原理
* MethodInfo.MethodHandle.GetFunctionPointer().ToPointer() 指向了 jit 后的 native 代码，因此修改此代码即可以修改实现
* 通过一系列跳转就可以巧妙的替换原函数实现，同时也保留调用原函数的功能。
* 本代码的实现与下面 rederence 的实现略有不同，表现在寄存器的使用以及原函数的替换逻辑上，具体实现可以看代码

## reference
* https://github.com/bigbaldy1128/DotNetDetour