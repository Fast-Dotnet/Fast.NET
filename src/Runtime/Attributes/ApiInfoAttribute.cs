// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

// ReSharper disable once CheckNamespace

namespace System;

/// <summary>
/// 接口信息
/// </summary>
[SuppressSniffer]
[AttributeUsage(AttributeTargets.Method)]
public class ApiInfoAttribute : Attribute
{
    /// <summary>
    /// 接口名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 操作方式
    /// </summary>
    public HttpRequestActionEnum Action { get; set; }

    /// <summary>
    /// 接口信息
    /// </summary>
    /// <param name="name">接口名称</param>
    public ApiInfoAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Action = HttpRequestActionEnum.None;
    }

    /// <summary>
    /// 接口信息
    /// </summary>
    /// <param name="name">接口名称</param>
    /// <param name="action">HttpRequestActionEnum 操作方式</param>
    public ApiInfoAttribute(string name, HttpRequestActionEnum action)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Action = action;
    }
}
