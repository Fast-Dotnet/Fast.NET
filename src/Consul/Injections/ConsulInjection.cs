// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fast.Consul;

/// <summary>
/// Consul 服务注册扩展
/// </summary>
public static class ConsulServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Consul 服务发现、健康检查和键值服务
    /// </summary>
    /// <param name="services">服务注册集合</param>
    /// <param name="configuration">应用配置</param>
    /// <param name="section">配置节点名称</param>
    /// <returns>返回当前服务注册集合，便于链式调用</returns>
    public static IServiceCollection AddFastConsul(this IServiceCollection services, IConfiguration configuration,
        string section = "ConsulSettings")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(section))
        {
            throw new ArgumentException("配置节点名称不能为空。", nameof(section));
        }

        Debugging.Info("Registering consul......");

        services.AddConfigurableOptions<ConsulSettingsOptions>(section);

        Penetrates.ConsulSettings = configuration.GetSection(section).Get<ConsulSettingsOptions>().LoadPostConfigure();

        services.AddTransient<IConsulRegister, ConsulRegister>();
        services.AddTransient<IKeyValueService, KeyValueService>();
        services.TryAddEnumerable(ServiceDescriptor
            .Transient<Microsoft.AspNetCore.Hosting.IStartupFilter, ConsulStartupFilter>());

        return services;
    }
}
