// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Fast.Runtime;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;


// ReSharper disable once CheckNamespace
namespace System;

/// <summary>
/// Microsoft App 上下文
/// </summary>
[SuppressSniffer]
public static class MAppContext
{
    /// <summary>
    /// 入口程序集版本号
    /// </summary>
    public static readonly string AssemblyVersion;

    /// <summary>
    /// 应用运行库
    /// </summary>
    public static readonly IEnumerable<DependencyLibrary> RuntimeLibraries;

    /// <summary>
    /// 应用有效程序集
    /// </summary>
    public static readonly IEnumerable<Assembly> Assemblies;

    /// <summary>
    /// 应用本地引用项目有效程序集
    /// </summary>
    public static readonly IEnumerable<Assembly> ProjectAssemblies;

    /// <summary>
    /// 有效程序集类型
    /// </summary>
    /// <remarks>所有公共的类型</remarks>
    public static readonly IEnumerable<Type> Types;

    /// <summary>
    /// 有效程序集类型
    /// </summary>
    /// <remarks>排除使用了 <see cref="SuppressSnifferAttribute"/> 特性的类型</remarks>
    public static readonly IEnumerable<Type> EffectiveTypes;

    /// <summary>
    /// 本地引用项目有效程序集类型
    /// </summary>
    /// <remarks>排除使用了 <see cref="SuppressSnifferAttribute"/> 特性的类型</remarks>
    public static readonly IEnumerable<Type> ProjectEffectiveTypes;

    /// <summary>
    /// 应用级显式登记的可释放对象集合
    /// </summary>
    public static ConcurrentBag<IDisposable> UnmanagedObjects { get; private set; }

    static MAppContext()
    {
        // 未托管的对象
        UnmanagedObjects = new ConcurrentBag<IDisposable>();

        // 加载入口程序集
        var entryAssembly = Assembly.GetEntryAssembly();

        // 获取入口程序集版本号
        AssemblyVersion = entryAssembly?.GetName().Version?.ToString();

        // 获取应用运行库
        List<DependencyLibrary> runtimeLibraries = entryAssembly.GetEntryRuntimeLibraries();
        RuntimeLibraries = runtimeLibraries;

        // 获取所有程序集
        Assemblies = entryAssembly.GetEntryReferencedAssembly(runtimeLibraries);

        // 获取本地引用项目所有程序集
        ProjectAssemblies = entryAssembly.GetEntryReferencedAssembly(runtimeLibraries
            .Where(wh => wh.Type.Equals("project", StringComparison.OrdinalIgnoreCase))
            .ToList());

        // 获取有效的类型集合
        Types = Assemblies.SelectMany(assembly => assembly.GetAssemblyTypes()).ToList();

        // 获取排除使用了 SuppressSnifferAttribute 特性的类型
        Type suppressSnifferAttributeType = typeof(SuppressSnifferAttribute);
        EffectiveTypes = Assemblies
            .SelectMany(assembly => assembly.GetAssemblyTypes(wh => !wh.IsDefined(suppressSnifferAttributeType, false)))
            .ToList();
        ProjectEffectiveTypes = ProjectAssemblies
            .SelectMany(assembly => assembly.GetAssemblyTypes(wh => !wh.IsDefined(suppressSnifferAttributeType, false)))
            .ToList();
    }

    /// <summary>
    /// 同步写入控制台并自动恢复原始前景色和背景色
    /// </summary>
    /// <param name="action">使用颜色与文本输出上下文的同步回调</param>
    /// <remarks>
    /// 同一输出流上的写入回调串行执行，回调结束或抛出异常时均恢复原始颜色
    /// 输出重定向时忽略颜色设置并仅写入文本，回调异常继续向调用方传播
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> 为 <see langword="null"/></exception>
    public static void ConsoleWrite(Action<ConsoleWriter> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        TextWriter output = Console.Out;
        lock (output)
        {
            var writer = new ConsoleWriter(output);
            try
            {
                action(writer);
            }
            finally
            {
                writer.ResetColor();
            }
        }
    }

    /// <summary>
    /// 处理获取对象异常问题
    /// </summary>
    /// <param name="action">要执行的操作委托</param>
    /// <param name="defaultValue">操作无法产生结果时使用的默认值</param>
    /// <typeparam name="T">操作返回的引用类型</typeparam>
    /// <returns>处理获取对象异常问题</returns>
    public static T CatchOrDefault<T>(Func<T> action, T defaultValue = null) where T : class
    {
        try
        {
            return action();
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 获取选项名称
    /// </summary>
    /// <typeparam name="TOptions">配置选项类型</typeparam>
    /// <returns>获取到的选项名称</returns>
    public static string GetOptionName<TOptions>() where TOptions : class, new()
    {
        // 默认后缀
        const string defaultSuffix = "Options";

        Type optionsType = typeof(TOptions);

        // 判断是否已 “Options” 结尾
        return optionsType.Name.EndsWith(defaultSuffix, StringComparison.Ordinal)
            ? optionsType.Name[..^defaultSuffix.Length]
            : optionsType.Name;
    }

    /// <summary>
    /// 解析服务提供器
    /// </summary>
    /// <param name="serviceType">要查询的服务类型</param>
    /// <param name="rootServices">应用根服务提供器</param>
    /// <param name="internalServices">框架内部使用的服务注册集合</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>请求所属的服务提供器，或已确认单例服务的根提供器</returns>
    /// <exception cref="InvalidOperationException">没有可用容器，或在非请求环境未提供非单例服务的显式作用域</exception>
    /// <remarks>不再创建隐式作用域；后台任务应持有并释放自己创建的作用域。</remarks>
    public static IServiceProvider GetServiceProvider(Type serviceType, IServiceProvider rootServices,
        IServiceCollection internalServices, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        // 优先获取 HttpContext 对象的 RequestServices
        if (httpContext?.RequestServices != null)
        {
            return httpContext.RequestServices;
        }

        if (rootServices == null)
        {
            throw new InvalidOperationException("根服务容器尚未初始化，不能临时构造第二个服务容器。");
        }

        // 与 DI 的最后注册、闭合类型优先语义保持一致，不能因较早的 singleton 注册绕过当前 scoped 合同。
        ServiceDescriptor descriptor =
            internalServices?.LastOrDefault(item => !item.IsKeyedService && item.ServiceType == serviceType);
        if (descriptor == null && serviceType.IsConstructedGenericType)
        {
            descriptor = internalServices?.LastOrDefault(item => !item.IsKeyedService
                                                                 && item.ServiceType == serviceType.GetGenericTypeDefinition());
        }

        if (descriptor?.Lifetime == ServiceLifetime.Singleton)
        {
            return rootServices;
        }

        throw new InvalidOperationException(
            "非请求环境解析非单例服务时，请由调用方创建并释放作用域，通过 scope.ServiceProvider 直接解析，或传给 FastContext.GetService 的 serviceProvider 参数。");
    }

    /// <summary>
    /// 获取当前程序启动 Uri 信息
    /// </summary>
    /// <remarks>默认获取第一个地址，可能为空，请勿在程序启动过程中使用</remarks>
    /// <param name="server">目标服务实例</param>
    /// <returns>获取到的当前程序启动 Uri 信息</returns>
    public static Uri GetCurrentStartupUri(IServer server)
    {
        string addresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

        if (string.IsNullOrEmpty(addresses))
        {
            return null;
        }

        return new Uri(addresses);
    }

    /// <summary>
    /// 获取当前线程Id
    /// </summary>
    /// <returns>获取到的当前线程Id</returns>
    public static int GetThreadId()
    {
        return Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// 获取当前请求的 TraceId
    /// </summary>
    /// <param name="rootServices">应用根服务提供器</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>获取到的当前请求 TraceId</returns>
    public static string GetTraceId(IServiceProvider rootServices, HttpContext httpContext)
    {
        return Activity.Current?.Id ?? (rootServices == null ? null : httpContext?.TraceIdentifier);
    }

    /// <summary>
    /// 获取一段代码执行耗时
    /// </summary>
    /// <param name="action">要执行的操作委托</param>
    /// <returns>获取到的一段代码执行耗时</returns>
    public static long GetExecutionTime(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        // 计算接口执行时间
        var timeOperation = Stopwatch.StartNew();
        action();
        timeOperation.Stop();
        return timeOperation.ElapsedMilliseconds;
    }

    /// <summary>
    /// 添加未托管的对象
    /// </summary>
    /// <param name="dsp">用于解析动态服务的服务提供器</param>
    /// <exception cref="ArgumentNullException"><paramref name="dsp"/> 为 <see langword="null"/></exception>
    public static void AddUnmanagedObjects(IDisposable dsp)
    {
        ArgumentNullException.ThrowIfNull(dsp);
        UnmanagedObjects.Add(dsp);
    }

    /// <summary>
    /// 释放显式登记的应用级对象；不能用于清理其他请求或任务拥有的作用域。
    /// </summary>
    public static void DisposeUnmanagedObjects()
    {
        // 逐个移除后再释放，避免 Clear() 丢弃遍历期间并发加入但尚未释放的对象
        while (UnmanagedObjects.TryTake(out IDisposable dsp))
        {
            dsp.Dispose();
        }
    }
}
