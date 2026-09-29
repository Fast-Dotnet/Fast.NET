// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fast.Swagger;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 Swagger 扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加 Swagger 服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">配置节名称</param>
    /// <param name="configure">Swagger 生成配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddSwaggerDocuments(this IServiceCollection services, IConfiguration configuration,
        string section = "SwaggerSettings", Action<SwaggerGenOptions> configure = null)
    {
        Debugging.Info("Registering swagger......");

        services.AddConfigurableOptions<SwaggerSettingsOptions>(section);

        // 获取 Swagger 文档配置选项
        Penetrates.SwaggerSettings = configuration.GetSection(section).Get<SwaggerSettingsOptions>().LoadPostConfigure();

        services.AddEndpointsApiExplorer();

        if (Penetrates.SwaggerSettings.Enable!.Value)
        {
            services.AddSwaggerGen(options => SwaggerDocumentBuilder.BuildGen(options, configure));
        }

        return services;
    }

    /// <summary>
    /// 添加 Swagger 服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="optionAction">Swagger 配置操作</param>
    /// <param name="configure">Swagger 生成配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddSwaggerDocuments(this IServiceCollection services,
        Action<SwaggerSettingsOptions> optionAction, Action<SwaggerGenOptions> configure = null)
    {
        Debugging.Info("Registering swagger......");

        services.Configure(optionAction);

        var swaggerSettings = new SwaggerSettingsOptions();
        optionAction.Invoke(swaggerSettings);

        Penetrates.SwaggerSettings = swaggerSettings;

        services.AddEndpointsApiExplorer();

        if (Penetrates.SwaggerSettings.Enable!.Value)
        {
            services.AddSwaggerGen(options => SwaggerDocumentBuilder.BuildGen(options, configure));
        }

        return services;
    }
}
