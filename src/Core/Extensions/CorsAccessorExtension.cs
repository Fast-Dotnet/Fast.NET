// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.NET.Core;

/// <summary>
/// 提供跨域处理扩展方法
/// </summary>
[SuppressSniffer]
public static class CorsAccessorExtension
{
    /// <summary>
    /// 默认跨域导出响应头 Key
    /// </summary>
    /// <remarks>解决 ajax，XMLHttpRequest，axios 不能获取请求头问题</remarks>
    private static readonly string[] _defaultExposedHeaders = {"access-token", "x-access-token"};

    /// <summary>
    /// 设置跨域策略
    /// </summary>
    /// <param name="builder">要配置的跨域策略构建器</param>
    /// <param name="corsAccessorSettings">允许的来源、请求头、方法及凭据配置</param>
    /// <param name="isMiddleware">是否为应用中间件阶段生成策略</param>
    internal static void SetCorsPolicy(CorsPolicyBuilder builder, CorsAccessorSettingsOptions corsAccessorSettings,
        bool isMiddleware = false)
    {
        // 判断是否设置了来源，因为 AllowAnyOrigin 不能和 AllowCredentials 一起公用
        bool isNotSetOrigins = corsAccessorSettings.WithOrigins == null || corsAccessorSettings.WithOrigins.Length == 0;

        // https://learn.microsoft.com/aspnet/core/signalr/security
        bool isSupportSignalR = isMiddleware && corsAccessorSettings.SignalRSupport == true;

        // 设置总是允许跨域源配置
        builder.SetIsOriginAllowed(_ => true);

        // 如果没有配置来源，则允许所有来源
        if (isNotSetOrigins)
        {
            // 解决 SignalR  不能配置允许所有源问题
            if (!isSupportSignalR)
            {
                builder.AllowAnyOrigin();
            }
        }
        else
        {
            builder.WithOrigins(corsAccessorSettings.WithOrigins).SetIsOriginAllowedToAllowWildcardSubdomains();
        }

        // 如果没有配置请求标头，则允许所有表头，包含处理 SignalR 情况
        if (corsAccessorSettings.WithHeaders == null || corsAccessorSettings.WithHeaders.Length == 0 || isSupportSignalR)
        {
            builder.AllowAnyHeader();
        }
        else
        {
            builder.WithHeaders(corsAccessorSettings.WithHeaders);
        }

        // 如果没有配置任何请求谓词，则允许所有请求谓词
        if (corsAccessorSettings.WithMethods == null || corsAccessorSettings.WithMethods.Length == 0)
        {
            builder.AllowAnyMethod();
        }
        else
        {
            // 解决 SignalR 必须允许 GET POST 问题
            if (isSupportSignalR)
            {
                builder.WithMethods(corsAccessorSettings.WithMethods.Concat(new[] {"GET", "POST"})
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray());
            }
            else
            {
                builder.WithMethods(corsAccessorSettings.WithMethods);
            }
        }

        // 配置跨域凭据，包含处理 SignalR 情况
        if ((corsAccessorSettings.AllowCredentials == true && !isNotSetOrigins) || isSupportSignalR)
        {
            builder.AllowCredentials();
        }

        // 配置响应头，如果前端不能获取自定义的 header 信息，必须配置该项，默认配置了 access-token 和 x-access-token，可取消默认行为
        List<string> exposedHeaders =
            corsAccessorSettings.FixedClientToken == true ? _defaultExposedHeaders.ToList() : new List<string>();
        if (corsAccessorSettings.WithExposedHeaders != null && corsAccessorSettings.WithExposedHeaders.Length > 0)
        {
            exposedHeaders.AddRange(corsAccessorSettings.WithExposedHeaders);
            exposedHeaders = exposedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        if (exposedHeaders.Any())
        {
            builder.WithExposedHeaders(exposedHeaders.ToArray());
        }

        // 设置预检过期时间，如果不设置默认为 24 小时
        builder.SetPreflightMaxAge(TimeSpan.FromSeconds(corsAccessorSettings.SetPreflightMaxAge ?? 24 * 60 * 60));
    }

    /// <summary>
    /// 添加跨域服务
    /// </summary>
    /// <param name="builder">要配置的应用构建器</param>
    /// <returns>返回 <paramref name="builder"/>，便于链式调用</returns>
    public static WebApplicationBuilder AddCorsAccessor(this WebApplicationBuilder builder)
    {
        builder.Services.AddCorsAccessor(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// 添加跨域服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：CorsAccessorSettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddCorsAccessor(this IServiceCollection services, IConfiguration configuration,
        string section = "CorsAccessorSettings")
    {
        Debugging.Info("Registering for the Cors accessor service......");

        services.AddConfigurableOptions<CorsAccessorSettingsOptions>(section);

        // 获取跨域配置选项
        CorsAccessorSettingsOptions corsAccessorSettings = configuration
            .GetSection(section)
            .Get<CorsAccessorSettingsOptions>()
            .LoadPostConfigure();

        // 添加跨域服务
        services.AddCors(options =>
        {
            // 添加策略跨域
            options.AddPolicy(corsAccessorSettings.PolicyName,
                configurePolicy => { SetCorsPolicy(configurePolicy, corsAccessorSettings); });
        });

        // 注册 CorsAccessor Startup 过滤器
        services.AddTransient(typeof(IStartupFilter), typeof(CorsAccessorStartupFilter));

        return services;
    }

    /// <summary>
    /// 添加跨域服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="optionAction">跨域访问配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddCorsAccessor(this IServiceCollection services,
        Action<CorsAccessorSettingsOptions> optionAction)
    {
        Debugging.Info("Registering for the Cors accessor service......");

        services.Configure(optionAction);

        var corsAccessorSettings = new CorsAccessorSettingsOptions();
        optionAction.Invoke(corsAccessorSettings);

        // 添加跨域服务
        services.AddCors(options =>
        {
            // 添加策略跨域
            options.AddPolicy(corsAccessorSettings.PolicyName,
                configurePolicy => { SetCorsPolicy(configurePolicy, corsAccessorSettings); });
        });

        // 注册 CorsAccessor Startup 过滤器
        services.AddTransient(typeof(IStartupFilter), typeof(CorsAccessorStartupFilter));

        return services;
    }
}
