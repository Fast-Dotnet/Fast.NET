// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.Configuration;

namespace Fast.NET.Core;

/// <summary>
/// 为 <see cref="IConfiguration"/> 提供扩展方法
/// </summary>
[SuppressSniffer]
public static class IConfigurationExtension
{
    /// <summary>
    /// 刷新配置对象
    /// </summary>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <returns>刷新配置对象</returns>
    public static IConfiguration Reload(this IConfiguration configuration)
    {
        if (FastContext.RootServices == null)
        {
            return configuration;
        }

        IConfiguration newConfiguration = FastContext.GetService<IConfiguration>(FastContext.RootServices);
        FastContext.Configuration = newConfiguration;

        return newConfiguration;
    }
}
