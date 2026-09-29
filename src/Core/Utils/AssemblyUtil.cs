// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using System.Runtime.Loader;

namespace Fast.NET.Core;

/// <summary>
/// <see cref="Assembly"/> 工具类
/// </summary>
[SuppressSniffer]
public static class AssemblyUtil
{
    /// <summary>
    /// 根据程序集名称获取运行时程序集
    /// </summary>
    /// <param name="assemblyName">目标程序集名称</param>
    /// <returns>根据程序集名称获取运行时程序集</returns>
    public static Assembly GetAssembly(string assemblyName)
    {
        // 加载程序集
        return AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
    }

    /// <summary>
    /// 根据路径加载程序集
    /// </summary>
    /// <param name="path">绝对路径</param>
    /// <returns>根据路径加载程序集</returns>
    public static Assembly LoadAssembly(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return Assembly.LoadFrom(path);
    }

    /// <summary>
    /// 通过流加载程序集
    /// </summary>
    /// <param name="assembly">MemoryStream 内存流</param>
    /// <returns>通过流加载程序集</returns>
    public static Assembly LoadAssembly(MemoryStream assembly)
    {
        return Assembly.Load(assembly.ToArray());
    }

    /// <summary>
    /// 根据程序集名称、类型完整限定名获取运行时类型
    /// </summary>
    /// <param name="assemblyName">目标程序集名称</param>
    /// <param name="typeFullName">目标类型的完全限定名</param>
    /// <returns>根据程序集名称、类型完整限定名获取运行时类型</returns>
    public static Type GetType(string assemblyName, string typeFullName)
    {
        return GetAssembly(assemblyName).GetType(typeFullName);
    }

    /// <summary>
    /// 根据程序集和类型完全限定名获取运行时类型
    /// </summary>
    /// <param name="assembly">MemoryStream 内存流</param>
    /// <param name="typeFullName">目标类型的完全限定名</param>
    /// <returns>根据程序集和类型完全限定名获取运行时类型</returns>
    public static Type GetType(MemoryStream assembly, string typeFullName)
    {
        return LoadAssembly(assembly).GetType(typeFullName);
    }
}
