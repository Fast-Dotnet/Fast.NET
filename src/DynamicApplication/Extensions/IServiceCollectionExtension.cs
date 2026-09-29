// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.DynamicApplication;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供动态 API 扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加动态 API 服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="routePrefix">应用于生成路由的统一前缀</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddDynamicApplication(this IServiceCollection services, string routePrefix = null)
    {
        Debugging.Info("Registering dynamic application......");

        ApplicationPartManager partManager =
            services.FirstOrDefault(f => f.ServiceType == typeof(ApplicationPartManager))?.ImplementationInstance as
                ApplicationPartManager
            ?? throw new InvalidOperationException(
                "`AddDynamicApplication` must be invoked after `AddControllers` or `AddControllersWithViews`.");

        DynamicApplicationContext.RoutePrefix = routePrefix;

        // 解决项目类型为 <Project Sdk="Microsoft.NET.Sdk"> 不能加载 API 问题，默认支持 <Project Sdk="Microsoft.NET.Sdk.Web">
        foreach (Assembly assembly in MAppContext.Assemblies)
        {
            string assemblyName = assembly.GetName().Name;
            if (partManager.ApplicationParts.All(u => !string.Equals(u.Name, assemblyName, StringComparison.Ordinal)))
            {
                partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        }

        // 添加控制器特性提供器
        partManager.FeatureProviders.Add(new DynamicApplicationFeatureProvider());

        // 配置 Mvc 选项
        services.Configure<MvcOptions>(options =>
        {
            // 添加应用模型转换器
            options.Conventions.Add(new DynamicApiControllerApplicationModelConvention(services));

            // 添加 text/plain 请求 Body 参数支持
            options.InputFormatters.Add(new TextPlainMediaTypeFormatter());
        });

        return services;
    }
}
