// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.JwtBearer;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 JWT Bearer 配置、认证与授权扩展
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加 JwtBearer 设置
    /// </summary>
    /// <remarks>适用于只使用工具类</remarks>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：JWTSettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddJwtBearerSetting(this IServiceCollection services, IConfiguration configuration,
        string section = "JWTSettings")
    {
        services.AddConfigurableOptions<JWTSettingsOptions>(section);

        Penetrates.JWTSettings = configuration.GetSection(section).Get<JWTSettingsOptions>().LoadPostConfigure();

        // 未配置 Redis 等 IDistributedCache 实现时提供进程内回退
        // AddDistributedMemoryCache 使用 TryAdd 注册，不会覆盖用户已经配置的缓存实现
        services.AddDistributedMemoryCache();

        return services;
    }

    /// <summary>
    /// 添加 JwtBearer 设置
    /// </summary>
    /// <remarks>适用于只使用工具类</remarks>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="optionAction">JWT 配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddJwtBearerSetting(this IServiceCollection services,
        Action<JWTSettingsOptions> optionAction)
    {
        services.Configure(optionAction);

        var jwtSettings = new JWTSettingsOptions();
        optionAction.Invoke(jwtSettings);

        Penetrates.JWTSettings = jwtSettings.LoadPostConfigure();

        // 未配置 Redis 等 IDistributedCache 实现时提供进程内回退
        services.AddDistributedMemoryCache();

        return services;
    }

    /// <summary>
    /// 添加 JwtBearer 授权
    /// </summary>
    /// <remarks>适用于自定义验证</remarks>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：JWTSettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, IConfiguration configuration,
        string section = "JWTSettings")
    {
        Debugging.Info("Registering jwt bearer......");

        services.AddJwtBearerSetting(configuration, section);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtBearerUtil.CreateTokenValidationParameters(Penetrates.JWTSettings);

                // 保留调用方已配置的 Token 提取逻辑，并确保其优先执行
                Func<MessageReceivedContext, Task> onMessageReceived = options.Events.OnMessageReceived;
                options.Events.OnMessageReceived = async context =>
                {
                    await onMessageReceived(context);

                    if (!string.IsNullOrEmpty(context.Token)
                        || context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>() == null)
                    {
                        return;
                    }

                    // 仅补充从标准 access_token 查询参数提取 Token，不改变 Hub 端点的授权要求
                    string accessToken = context.Request.Query["access_token"].ToString();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// 添加 JwtBearer 授权
    /// </summary>
    /// <remarks>适用于自定义验证</remarks>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="optionAction">JWT 配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services,
        Action<JWTSettingsOptions> optionAction)
    {
        Debugging.Info("Registering jwt bearer......");

        services.AddJwtBearerSetting(optionAction);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtBearerUtil.CreateTokenValidationParameters(Penetrates.JWTSettings);

                // 保留调用方已配置的 Token 提取逻辑，并确保其优先执行
                Func<MessageReceivedContext, Task> onMessageReceived = options.Events.OnMessageReceived;
                options.Events.OnMessageReceived = async context =>
                {
                    await onMessageReceived(context);

                    if (!string.IsNullOrEmpty(context.Token)
                        || context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>() == null)
                    {
                        return;
                    }

                    // 仅补充从标准 access_token 查询参数提取 Token，不改变 Hub 端点的授权要求
                    string accessToken = context.Request.Query["access_token"].ToString();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// 添加 JwtBearer 服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：JWTSettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddJwtBearer(this IServiceCollection services, IConfiguration configuration,
        string section = "JWTSettings")
    {
        Debugging.Info("Registering jwt bearer......");

        services.AddJwtBearerSetting(configuration, section);

        RegisterAuthorizationServices(services);

        // 未显式标记匿名访问的端点统一要求授权
        if (Penetrates.JWTSettings.Enable.HasValue && Penetrates.JWTSettings.Enable.Value)
        {
            services.Configure<MvcOptions>(options => { options.Filters.Add(new AuthorizeFilter()); });
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtBearerUtil.CreateTokenValidationParameters(Penetrates.JWTSettings);

                // 保留调用方已配置的 Token 提取逻辑，并确保其优先执行
                Func<MessageReceivedContext, Task> onMessageReceived = options.Events.OnMessageReceived;
                options.Events.OnMessageReceived = async context =>
                {
                    await onMessageReceived(context);

                    if (!string.IsNullOrEmpty(context.Token)
                        || context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>() == null)
                    {
                        return;
                    }

                    // 仅补充从标准 access_token 查询参数提取 Token，不改变 Hub 端点的授权要求
                    string accessToken = context.Request.Query["access_token"].ToString();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// 添加 JwtBearer 服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="optionAction">JWT 配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddJwtBearer(this IServiceCollection services, Action<JWTSettingsOptions> optionAction)
    {
        services.AddJwtBearerSetting(optionAction);

        RegisterAuthorizationServices(services);

        // 未显式标记匿名访问的端点统一要求授权
        if (Penetrates.JWTSettings.Enable.HasValue && Penetrates.JWTSettings.Enable.Value)
        {
            services.Configure<MvcOptions>(options => { options.Filters.Add(new AuthorizeFilter()); });
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtBearerUtil.CreateTokenValidationParameters(Penetrates.JWTSettings);

                // 保留调用方已配置的 Token 提取逻辑，并确保其优先执行
                Func<MessageReceivedContext, Task> onMessageReceived = options.Events.OnMessageReceived;
                options.Events.OnMessageReceived = async context =>
                {
                    await onMessageReceived(context);

                    if (!string.IsNullOrEmpty(context.Token)
                        || context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>() == null)
                    {
                        return;
                    }

                    // 仅补充从标准 access_token 查询参数提取 Token，不改变 Hub 端点的授权要求
                    string accessToken = context.Request.Query["access_token"].ToString();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                };
            });

        return services;
    }

    /// <summary>注册 Fast 策略，不覆盖已有自定义提供器的非 Fast 策略。</summary>
    private static void RegisterAuthorizationServices(IServiceCollection services)
    {
        // 显式注册优先；扫描只接受闭合、可实例化的类型，多个候选不依赖程序集枚举顺序。
        if (!services.Any(descriptor => !descriptor.IsKeyedService && descriptor.ServiceType == typeof(IJwtBearerHandle)))
        {
            Type[] handles = MAppContext.EffectiveTypes.Where(type =>
                    typeof(IJwtBearerHandle).IsAssignableFrom(type)
                    && type.IsClass
                    && !type.IsAbstract
                    && !type.ContainsGenericParameters)
                .ToArray();
            if (handles.Length > 1)
            {
                throw new InvalidOperationException("发现多个 IJwtBearerHandle 实现，请显式注册所需处理器。");
            }

            if (handles.Length == 1)
            {
                services.AddScoped(typeof(IJwtBearerHandle), handles[0]);
            }
        }

        services.AddAuthorization();
        ServiceDescriptor current = services.Last(descriptor => descriptor.ServiceType == typeof(IAuthorizationPolicyProvider)
                                                                && !descriptor.IsKeyedService);
        if (current.ImplementationType != typeof(AppAuthorizationPolicyProvider))
        {
            // 使用独立 keyed 描述符保留原生命周期及释放责任，不创建第二个服务容器。
            string key = AppAuthorizationPolicyProvider.FallbackServiceKey;
            ServiceDescriptor fallback;
            if (current.ImplementationInstance != null)
            {
                fallback = new ServiceDescriptor(typeof(IAuthorizationPolicyProvider), key, current.ImplementationInstance);
            }
            else if (current.ImplementationFactory != null)
            {
                fallback = new ServiceDescriptor(typeof(IAuthorizationPolicyProvider), key,
                    (provider, _) => current.ImplementationFactory(provider), current.Lifetime);
            }
            else
            {
                fallback = new ServiceDescriptor(typeof(IAuthorizationPolicyProvider), key, current.ImplementationType,
                    current.Lifetime);
            }

            foreach (ServiceDescriptor descriptor in services
                         .Where(item => item.ServiceType == typeof(IAuthorizationPolicyProvider) && !item.IsKeyedService)
                         .ToArray())
            {
                services.Remove(descriptor);
            }

            services.Add(fallback);
            services.Add(ServiceDescriptor.Describe(typeof(IAuthorizationPolicyProvider), typeof(AppAuthorizationPolicyProvider),
                current.Lifetime));
        }

        if (!services.Any(descriptor => !descriptor.IsKeyedService
                                        && descriptor.ServiceType == typeof(IAuthorizationHandler)
                                        && descriptor.ImplementationType == typeof(AppAuthorizationHandler)))
        {
            // 令牌刷新先于标准角色/声明验证执行；其他处理器全部保留。
            services.Insert(0, ServiceDescriptor.Singleton<IAuthorizationHandler, AppAuthorizationHandler>());
        }
    }
}
