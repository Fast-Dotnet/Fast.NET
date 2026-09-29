// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.Cache;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供缓存扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加缓存服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：RedisSettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddCache(this IServiceCollection services, string section = "RedisSettings")
    {
        services.AddConfigurableOptions<RedisSettingsOptions>(section);

        // 添加默认缓存服务
        services.AddSingleton<ICache, Cache>();

        // 查找所有集成了 ICacheContextLocator 类的缓存上下文定位器
        Type cacheContextLocatorType = typeof(ICacheContextLocator);
        IEnumerable<Type> cacheContextLocatorTypes = MAppContext.EffectiveTypes.Where(wh =>
            cacheContextLocatorType.IsAssignableFrom(wh) && wh.IsClass && !wh.IsAbstract && !wh.ContainsGenericParameters);

        // 循环所有上下文，注册单例服务
        foreach (Type contextLocatorType in cacheContextLocatorTypes)
        {
            services.AddSingleton(typeof(ICache<>).MakeGenericType(contextLocatorType),
                typeof(Cache<>).MakeGenericType(contextLocatorType));
        }

        return services;
    }

    /// <summary>
    /// 添加缓存服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="optionAction">Redis 配置操作。默认连接直接设置在选项上，命名连接通过 Services 配置。</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddCache(this IServiceCollection services, Action<RedisSettingsOptions> optionAction)
    {
        ArgumentNullException.ThrowIfNull(optionAction);
        services.AddOptions<RedisSettingsOptions>()
            .Configure(options =>
            {
                optionAction(options);
                options.Services ??= [];
                if (options.Services.Any(item => item == null || string.IsNullOrWhiteSpace(item.ServiceName))
                    || options.Services.GroupBy(item => item.ServiceName, StringComparer.Ordinal).Any(group => group.Count() > 1))
                {
                    throw new InvalidOperationException("Redis 命名连接不能为空或重名。");
                }

                options.PostConfigure();
            });

        // 添加默认缓存服务
        services.AddSingleton<ICache, Cache>();

        // 查找所有集成了 ICacheContextLocator 类的缓存上下文定位器
        Type cacheContextLocatorType = typeof(ICacheContextLocator);
        IEnumerable<Type> cacheContextLocatorTypes = MAppContext.EffectiveTypes.Where(wh =>
            cacheContextLocatorType.IsAssignableFrom(wh) && wh.IsClass && !wh.IsAbstract && !wh.ContainsGenericParameters);

        // 循环所有上下文，注册单例服务
        foreach (Type contextLocatorType in cacheContextLocatorTypes)
        {
            services.AddSingleton(typeof(ICache<>).MakeGenericType(contextLocatorType),
                typeof(Cache<>).MakeGenericType(contextLocatorType));
        }

        return services;
    }
}
