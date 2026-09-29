// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.DependencyInjection;

namespace Fast.EventBus;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供动态 API 扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加事件总线服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        Debugging.Info("Registering event bus......");

        // 创建默认内存通道事件源对象，超过 n 条待处理消息，第 n+1 条将进入等待，默认为 3000
        var defaultStorerOfChannel = new ChannelEventSourceStorer(3000);

        // 注册后台任务队列接口/实例为单例，采用工厂方式创建
        services.AddSingleton<IEventSourceStorer>(_ => { return defaultStorerOfChannel; });

        // 注册默认内存通道事件发布者
        services.AddSingleton<IEventPublisher, ChannelEventPublisher>();

        // 注册事件总线工厂
        services.AddSingleton<IEventBusFactory, EventBusFactory>();

        #region 构建事件总线服务

        var entryAssemblyType = MAppContext.EffectiveTypes.ToList();

        // 查找所有继承了 IEventSubscriber 的类
        IEnumerable<Type> iEventSubscriberTypes = entryAssemblyType.Where(wh =>
            typeof(IEventSubscriber).IsAssignableFrom(wh) && wh.IsClass && !wh.IsAbstract && !wh.ContainsGenericParameters);

        // 注册事件订阅者
        foreach (Type iEventSubscriberType in iEventSubscriberTypes)
        {
            services.AddSingleton(typeof(IEventSubscriber), iEventSubscriberType);
        }

        if (!services.Any(descriptor => !descriptor.IsKeyedService && descriptor.ServiceType == typeof(IEventHandlerMonitor)))
        {
            Type[] monitors = entryAssemblyType.Where(type =>
                    typeof(IEventHandlerMonitor).IsAssignableFrom(type)
                    && type.IsClass
                    && !type.IsAbstract
                    && !type.ContainsGenericParameters)
                .ToArray();
            if (monitors.Length > 1)
            {
                throw new InvalidOperationException("发现多个 IEventHandlerMonitor 实现，请显式注册所需监视器。");
            }

            if (monitors.Length == 1)
            {
                services.AddSingleton(typeof(IEventHandlerMonitor), monitors[0]);
            }
        }

        // 查找继承了 IEventFallbackPolicy 的类
        var iEventFallbackPolicyTypes = entryAssemblyType.Where(f =>
                typeof(IEventFallbackPolicy).IsAssignableFrom(f)
                && f is {IsClass: true, IsAbstract: false, ContainsGenericParameters: false})
            .ToList();

        // 特性保存的是具体策略类型，因此必须同时按具体类型注册，否则运行时按 Type 解析始终得到 null
        foreach (Type fallbackPolicyType in iEventFallbackPolicyTypes)
        {
            services.AddSingleton(fallbackPolicyType);
        }

        #endregion

        // 通过工厂模式创建
        services.AddHostedService(serviceProvider =>
        {
            // 创建事件总线后台服务对象
            EventBusHostedService eventBusHostedService =
                ActivatorUtilities.CreateInstance<EventBusHostedService>(serviceProvider);

            // 订阅未察觉任务异常事件
            eventBusHostedService.UnobservedTaskException += (_, _) => { };

            return eventBusHostedService;
        });

        return services;
    }
}
