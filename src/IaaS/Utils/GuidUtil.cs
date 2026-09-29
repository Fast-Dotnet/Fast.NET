// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Linq;

namespace Fast.IaaS;

/// <summary>
/// Guid 工具类
/// </summary>
public static class GuidUtil
{
    /// <summary>
    /// 生成一个 Guid
    /// </summary>
    /// <remarks>
    /// <para>只支持 N D B P</para>
    /// <para>N ece4f4a60b764339b94a07c84e338a27</para>
    /// <para>D 5bf99df1-dc49-4023-a34a-7bd80a42d6bb</para>
    /// <para>B 2280f8d7-fd18-4c72-a9ab-405de3fcfbc9</para>
    /// <para>P 25e6e09f-fb66-4cab-b4cd-bfb429566549</para>
    /// </remarks>
    /// <param name="format">格式化方式</param>
    /// <returns>生成的一个 Guid</returns>
    public static string GetGuid(string format = "N")
    {
        return Guid.NewGuid().ToString(format);
    }

    /// <summary>
    /// 生成一个短的 Guid
    /// </summary>
    /// <returns>生成的一个短的 Guid</returns>
    public static string GetShortGuid()
    {
        long i = Guid.NewGuid().ToByteArray().Aggregate<byte, long>(1, (current, b) => current * (b + 1));

        return $"{i - DateTime.Now.Ticks:x}";
    }
}
