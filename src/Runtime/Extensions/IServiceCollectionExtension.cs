// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fast.Runtime;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加选项配置
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="path">配置中对应的 Key</param>
    /// <typeparam name="TOptions">配置选项类型</typeparam>
    /// <returns>返回当前服务注册集合，便于链式调用</returns>
    public static IServiceCollection AddConfigurableOptions<TOptions>(this IServiceCollection services, string path = null)
        where TOptions : class, new()
    {
        // 获取配置选项名称
        path ??= MAppContext.GetOptionName<TOptions>();

        OptionsBuilder<TOptions> optionsConfigure = services.AddOptions<TOptions>()
            .BindConfiguration(path, options =>
            {
                // 绑定私有变量
                options.BindNonPublicProperties = true;
            })
            .ValidateDataAnnotations();

        // 获取类型
        Type optionsType = typeof(TOptions);

        // 复杂后期配置
        Type postConfigureInterface = optionsType.GetInterfaces().FirstOrDefault(f => typeof(IPostConfigure).IsAssignableFrom(f));

        if (postConfigureInterface != null)
        {
            MethodInfo postConfigureMethod = optionsType.GetMethod(nameof(IPostConfigure.PostConfigure));

            if (postConfigureMethod != null)
            {
                optionsConfigure.PostConfigure(options => postConfigureMethod.Invoke(options, Array.Empty<object>()));
            }
        }

        return services;
    }

    /// <summary>
    /// 注册 Mvc 过滤器
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configure">额外的 MVC 配置操作</param>
    /// <typeparam name="TFilter">要注册的 MVC 过滤器类型</typeparam>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddMvcFilter<TFilter>(this IServiceCollection services, Action<MvcOptions> configure = null)
        where TFilter : IFilterMetadata
    {
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<TFilter>();

            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// 注册 Mvc 过滤器
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="filter">要注册的 MVC 过滤器实例</param>
    /// <param name="configure">额外的 MVC 配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddMvcFilter(this IServiceCollection services, IFilterMetadata filter,
        Action<MvcOptions> configure = null)
    {
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(filter);

            configure?.Invoke(options);
        });

        return services;
    }
}
