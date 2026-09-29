// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;

namespace Fast.EventBus;

/// <summary>
/// 提供事件总线扩展方法
/// </summary>
[SuppressSniffer]
public static class EventBusExtension
{
    /// <summary>
    /// 将事件枚举Id转换成字符串对象
    /// </summary>
    /// <param name="em">要转换为事件标识字符串的枚举值</param>
    /// <returns>将事件枚举Id转换成字符串对象</returns>
    public static string EventBusToString(this Enum em)
    {
        Type enumType = em.GetType();
        return $"{enumType.Assembly.GetName().Name};{enumType.FullName}.{em}";
    }

    /// <summary>
    /// 将事件枚举字符串转换成枚举对象
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>将事件枚举字符串转换成枚举对象</returns>
    public static Enum EventBusToEnum(this string str)
    {
        string assemblyName = str[..str.IndexOf(';')];
        string fullName = str[(str.IndexOf(';') + 1)..str.LastIndexOf('.')];
        string name = str[(str.LastIndexOf('.') + 1)..];

        return Enum.Parse(Assembly.Load(assemblyName).GetType(fullName), name) as Enum;
    }
}
