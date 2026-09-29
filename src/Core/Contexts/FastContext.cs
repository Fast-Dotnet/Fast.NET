// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Fast.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fast.NET.Core;

/// <summary>
/// App 上下文
/// </summary>
[SuppressSniffer]
public static class FastContext
{
    /// <summary>
    /// 获取 Web 主机环境
    /// </summary>
    public static IWebHostEnvironment WebHostEnvironment { get; internal set; }

    /// <summary>
    /// 获取主机环境
    /// </summary>
    public static IHostEnvironment HostEnvironment { get; internal set; }

    /// <summary>
    /// 应用服务
    /// </summary>
    public static IServiceCollection InternalServices { get; internal set; }

    /// <summary>
    /// 应用程序构建器
    /// </summary>
    public static IApplicationBuilder WebApplication { get; set; }

    /// <summary>
    /// 存储根服务，可能为空
    /// </summary>
    public static IServiceProvider RootServices { get; internal set; }

    private static IConfiguration _configuration { get; set; }

    /// <summary>
    /// 应用配置
    /// </summary>
    public static IConfiguration Configuration
    {
        get => MAppContext.CatchOrDefault(() => _configuration.Reload(), new ConfigurationBuilder().Build());
        internal set => _configuration = value;
    }

    /// <summary>
    /// 请求上下文
    /// </summary>
    public static HttpContext HttpContext =>
        MAppContext.CatchOrDefault(() => RootServices?.GetService<IHttpContextAccessor>()?.HttpContext);

    /// <summary>
    /// 获取请求生存周期的服务
    /// </summary>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>获取到的请求生存周期的服务</returns>
    public static TService GetService<TService>(IServiceProvider serviceProvider = null) where TService : class
    {
        return GetService(typeof(TService), serviceProvider) as TService;
    }

    /// <summary>
    /// 获取请求生存周期的服务
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    /// <returns>获取到的请求生存周期的服务</returns>
    public static object GetService(Type type, IServiceProvider serviceProvider = null)
    {
        return (serviceProvider ?? MAppContext.GetServiceProvider(type, RootServices, InternalServices, HttpContext))
            .GetService(type);
    }

    /// <summary>
    /// 获取请求生存周期的服务集合
    /// </summary>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>获取到的请求生存周期的服务集合集合</returns>
    public static IEnumerable<TService> GetServices<TService>(IServiceProvider serviceProvider = null) where TService : class
    {
        return (serviceProvider ?? MAppContext.GetServiceProvider(typeof(TService), RootServices, InternalServices, HttpContext))
            .GetServices<TService>();
    }

    /// <summary>
    /// 获取请求生存周期的服务集合
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    /// <returns>获取到的请求生存周期的服务集合集合</returns>
    public static IEnumerable<object> GetServices(Type type, IServiceProvider serviceProvider = null)
    {
        return (serviceProvider ?? MAppContext.GetServiceProvider(type, RootServices, InternalServices, HttpContext))
            .GetServices(type);
    }

    /// <summary>
    /// 获取请求生存周期的服务
    /// </summary>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>获取到的请求生存周期的服务</returns>
    public static TService GetRequiredService<TService>(IServiceProvider serviceProvider = null) where TService : class
    {
        return GetRequiredService(typeof(TService), serviceProvider) as TService;
    }

    /// <summary>
    /// 获取请求生存周期的服务
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    /// <returns>获取到的请求生存周期的服务</returns>
    public static object GetRequiredService(Type type, IServiceProvider serviceProvider = null)
    {
        return (serviceProvider ?? MAppContext.GetServiceProvider(type, RootServices, InternalServices, HttpContext))
            .GetRequiredService(type);
    }

    /// <summary>
    /// 获取配置
    /// </summary>
    /// <param name="path">配置中对应的 Key</param>
    /// <typeparam name="TOptions">配置选项类型</typeparam>
    /// <returns>获取到的配置</returns>
    public static TOptions GetConfig<TOptions>(string path = null) where TOptions : class, new()
    {
        // 获取配置选项名称
        path ??= MAppContext.GetOptionName<TOptions>();

        TOptions options = Configuration.GetSection(path).Get<TOptions>();

        // 判断是否继承了 IPostConfigure
        if (typeof(IPostConfigure).IsAssignableFrom(typeof(TOptions)))
        {
            MethodInfo postConfigureMethod = typeof(TOptions).GetMethod(nameof(IPostConfigure.PostConfigure));

            // 空值判断
            options ??= Activator.CreateInstance<TOptions>();

            // 加载后期配置
            postConfigureMethod!.Invoke(options, null);
        }

        return options;
    }

    /// <summary>
    /// 配置选项
    /// </summary>
    /// <typeparam name="TOptions">配置选项类型</typeparam>
    /// <returns>配置选项</returns>
    public static TOptions GetOptions<TOptions>() where TOptions : class, new()
    {
        return GetService<IOptions<TOptions>>()?.Value;
    }

    /// <summary>
    /// 获取服务注册的生命周期类型
    /// </summary>
    /// <param name="serviceType">要查询的服务类型</param>
    /// <returns>获取到的服务注册的生命周期类型</returns>
    public static ServiceLifetime? GetServiceLifetime(Type serviceType)
    {
        ServiceDescriptor serviceDescriptor = InternalServices.FirstOrDefault(u =>
            u.ServiceType == (serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType));

        return serviceDescriptor?.Lifetime;
    }
}
