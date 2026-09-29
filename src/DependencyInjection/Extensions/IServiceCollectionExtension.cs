// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.DependencyInjection;

namespace Fast.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供依赖注入扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 当前进程唯一宿主的命名服务映射
    /// </summary>
    private static readonly Dictionary<string, NamedServiceRegistration> NamedTypes = new(StringComparer.Ordinal);

    /// <summary>
    /// 当前进程唯一宿主中存在歧义的命名服务别名
    /// </summary>
    private static readonly HashSet<string> AmbiguousNamedAliases = new(StringComparer.Ordinal);

    /// <summary>
    /// 添加依赖注入服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
    {
        Debugging.Info("Registering dependency injection......");

        Type dependencyType = typeof(IDependency);
        var assemblies = MAppContext.Assemblies.ToHashSet();
        var lifetimeInterfaces = new HashSet<Type>
        {
            typeof(ITransientDependency), typeof(IScopedDependency), typeof(ISingletonDependency)
        };

        var namedRegistrations = new List<NamedServiceRegistration>();

        // 获取程序集需要依赖注入的类型
        Type[] injectTypes = MAppContext.EffectiveTypes
            .Where(wh => dependencyType.IsAssignableFrom(wh) && wh.IsClass && !wh.IsInterface && !wh.IsAbstract)
            .OrderBy(value => value.FullName, StringComparer.Ordinal)
            .ToArray();

        // 执行依赖注入
        foreach (Type type in injectTypes)
        {
            Type[] interfaces = type.GetInterfaces().OrderBy(value => value.FullName, StringComparer.Ordinal).ToArray();

            // 获取所有能注册的接口
            Type[] canInjectInterfaces = interfaces.Where(u =>
                    u != typeof(IDisposable)
                    && u != typeof(IAsyncDisposable)
                    && u != typeof(IDependency)
                    && !lifetimeInterfaces.Contains(u)
                    && assemblies.Contains(u.Assembly)
                    && (!type.ContainsGenericParameters
                        || (u.IsGenericType
                            && u.ContainsGenericParameters
                            && type.GetGenericArguments().Length == u.GetGenericArguments().Length)))
                .ToArray();

            // 获取生存周期类型
            Type[] dependencies = interfaces.Where(lifetimeInterfaces.Contains).ToArray();
            if (dependencies.Length != 1)
            {
                throw new InvalidOperationException($"类型 {type.FullName} 必须声明且只能声明一个生命周期标记。");
            }

            Type lifetimeType = dependencies[0];

            // 注册服务
            RegisterService(services, lifetimeType, type, canInjectInterfaces);

            // 缓存类型注册
            namedRegistrations.Add(new NamedServiceRegistration(FixedGenericType(type), lifetimeType));
        }

        BuildNamedTypes(namedRegistrations);

        // 注册命名服务（接口多实现）
        RegisterNamedService<ITransientDependency>(services);
        RegisterNamedService<IScopedDependency>(services);
        RegisterNamedService<ISingletonDependency>(services);

        return services;
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="dependencyType">用于确定服务生命周期的依赖标记类型</param>
    /// <param name="type">类型</param>
    /// <param name="canInjectInterfaces">能被注册的接口</param>
    private static void RegisterService(IServiceCollection services, Type dependencyType, Type type,
        IEnumerable<Type> canInjectInterfaces)
    {
        // 立即执行接口筛选，避免重复枚举，并用于判断是否存在可注册的业务接口
        Type[] interfaces = canInjectInterfaces.ToArray();
        Type fixedType = FixedGenericType(type);
        ServiceLifetime lifetime = TryGetServiceLifetime(dependencyType);

        // 开放泛型不支持 factory 注册，保留实现类直接映射的容器约定
        if (fixedType.ContainsGenericParameters)
        {
            if (interfaces.Length > 1)
            {
                throw new InvalidOperationException($"开放泛型 {fixedType.FullName} 实现了多个业务接口，无法在 Microsoft DI 中共享同一实例。");
            }

            if (interfaces.Length == 0)
            {
                services.Add(ServiceDescriptor.Describe(fixedType, fixedType, lifetime));
                return;
            }

            foreach (Type inter in interfaces)
            {
                services.Add(ServiceDescriptor.Describe(FixedGenericType(inter), fixedType, lifetime));
            }

            return;
        }

        // 具体类型是唯一实例入口，业务接口只做定向别名
        services.Add(ServiceDescriptor.Describe(fixedType, fixedType, lifetime));

        foreach (Type inter in interfaces)
        {
            Type fixedInter = FixedGenericType(inter);
            services.Add(ServiceDescriptor.Describe(fixedInter, provider => provider.GetRequiredService(fixedType), lifetime));
        }
    }

    /// <summary>
    /// 将包含未绑定参数的泛型规范化为定义，保留闭合泛型和嵌套类型。
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>用于服务注册的原类型或泛型定义</returns>
    private static Type FixedGenericType(Type type)
    {
        return type.IsGenericType && type.ContainsGenericParameters ? type.GetGenericTypeDefinition() : type;
    }

    /// <summary>
    /// 构建命名服务映射
    /// </summary>
    /// <param name="registrations">命名服务注册</param>
    private static void BuildNamedTypes(IEnumerable<NamedServiceRegistration> registrations)
    {
        NamedTypes.Clear();
        AmbiguousNamedAliases.Clear();

        foreach (NamedServiceRegistration registration in registrations)
        {
            Type implementation = registration.Implementation;
            string canonicalName = implementation.AssemblyQualifiedName ?? implementation.FullName ?? implementation.Name;
            NamedTypes.Add(canonicalName, registration);

            AddNamedAlias(implementation.FullName, registration);
            AddNamedAlias(implementation.Name, registration);
        }
    }

    /// <summary>
    /// 添加无歧义的命名服务别名
    /// </summary>
    /// <param name="alias">别名</param>
    /// <param name="registration">命名服务注册</param>
    private static void AddNamedAlias(string alias, NamedServiceRegistration registration)
    {
        if (string.IsNullOrWhiteSpace(alias) || AmbiguousNamedAliases.Contains(alias))
        {
            return;
        }

        if (NamedTypes.TryGetValue(alias, out NamedServiceRegistration existingRegistration)
            && existingRegistration == registration)
        {
            return;
        }

        if (NamedTypes.TryAdd(alias, registration))
        {
            return;
        }

        NamedTypes.Remove(alias);
        AmbiguousNamedAliases.Add(alias);
    }

    /// <summary>
    /// 注册命名服务（接口多实现）
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <typeparam name="TDependency">要注册的依赖服务类型</typeparam>
    private static void RegisterNamedService<TDependency>(IServiceCollection services) where TDependency : IDependency
    {
        ServiceLifetime lifetime = TryGetServiceLifetime(typeof(TDependency));

        // 注册命名服务
        services.Add(ServiceDescriptor.Describe(typeof(Func<string, TDependency, object>), provider =>
        {
            object ResolveService(string named, TDependency _)
            {
                if (!NamedTypes.TryGetValue(named, out NamedServiceRegistration registration)
                    || registration.Dependency != typeof(TDependency))
                {
                    return null;
                }

                if (registration.Implementation.ContainsGenericParameters)
                {
                    throw new InvalidOperationException("开放泛型命名服务需要明确类型参数，请通过闭合接口解析。");
                }

                return provider.GetService(registration.Implementation);
            }

            return (Func<string, TDependency, object>)ResolveService;
        }, lifetime));
    }

    /// <summary>
    /// 根据依赖接口类型解析 ServiceLifetime 对象
    /// </summary>
    /// <param name="dependencyType">dependency 类型</param>
    /// <returns>根据依赖接口类型解析 ServiceLifetime 对象</returns>
    private static ServiceLifetime TryGetServiceLifetime(Type dependencyType)
    {
        return dependencyType switch
        {
            _ when dependencyType == typeof(ITransientDependency) => ServiceLifetime.Transient,
            _ when dependencyType == typeof(IScopedDependency) => ServiceLifetime.Scoped,
            _ when dependencyType == typeof(ISingletonDependency) => ServiceLifetime.Singleton,
            _ => throw new InvalidCastException("Invalid service registration lifetime.")
        };
    }

    /// <summary>
    /// 命名服务注册
    /// </summary>
    /// <param name="Implementation">实现类型</param>
    /// <param name="Dependency">生命周期标记类型</param>
    private readonly record struct NamedServiceRegistration(Type Implementation, Type Dependency);
}
