// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Reflection;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="Assembly"/> 提供扩展方法
/// </summary>
public static class AssemblyExtension
{
    /// <summary>
    /// 获取所有类型
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <param name="exported">是否只返回导出的类型</param>
    /// <returns>获取到的所有类型集合</returns>
    public static Type[] GetTypes(this Assembly assembly, bool exported)
    {
        return exported ? assembly.GetExportedTypes() : assembly.GetTypes();
    }

    /// <summary>
    /// 获取程序集描述
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <returns>获取到的程序集描述</returns>
    public static string GetDescription(this Assembly assembly)
    {
        var descriptionAttribute =
            Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;

        return descriptionAttribute?.Description;
    }

    /// <summary>
    /// 获取程序集版本
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <returns>获取到的程序集版本</returns>
    public static Version GetVersion(this Assembly assembly)
    {
        return assembly.GetName().Version;
    }

    /// <summary>
    /// 获取程序集名称
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <returns>获取到的程序集名称</returns>
    public static string GetAssemblyName(this Assembly assembly)
    {
        return assembly.GetName().Name;
    }

    /// <summary>
    /// 根据程序集和类型完整限定名获取运行时类型
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <param name="typeFullName">目标类型的完全限定名</param>
    /// <returns>根据程序集和类型完整限定名获取运行时类型</returns>
    public static Type GetType(Assembly assembly, string typeFullName)
    {
        return assembly.GetType(typeFullName);
    }
}
