// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.OpenApi;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供动态 API 扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加 OpenAPI 设置
    /// </summary>
    /// <remarks>适用于只使用工具类</remarks>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：OpenAPISettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddOpenApi(this IServiceCollection services, IConfiguration configuration,
        string section = "OpenApiSettings")
    {
        services.AddConfigurableOptions<OpenApiSettingsOptions>(section);

        Penetrates.OpenApiSettings = configuration.GetSection(section).Get<OpenApiSettingsOptions>().LoadPostConfigure();

        return services;
    }
}
