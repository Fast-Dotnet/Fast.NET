// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.Mapster;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供动态 API 扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加 Mapster 映射服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddMapster(this IServiceCollection services)
    {
        Debugging.Info("Registering mapster......");

        // 获取全局映射配置
        TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;

        // 获取当前入口所有程序集
        Assembly[] assemblies = MAppContext.Assemblies?.ToArray();

        // 扫描所有继承  IRegister 接口的对象映射配置
        if (assemblies?.Length > 0)
        {
            config.Scan(assemblies);
        }

        // 配置默认全局映射（支持覆盖）
        config.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible).PreserveReference(true);

        // 配置默认全局映射（忽略大小写敏感）
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase).PreserveReference(true);

        // 配置支持依赖注入
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
