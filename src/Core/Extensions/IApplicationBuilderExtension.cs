// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;

namespace Fast.NET.Core;

/// <summary>
/// 为 <see cref="IApplicationBuilder"/> 提供扩展方法
/// </summary>
[SuppressSniffer]
public static class IApplicationBuilderExtension
{
    /// <summary>
    /// 启用 Body 重复读功能
    /// </summary>
    /// <remarks>须在 app.UseRouting() 之前注册</remarks>
    /// <param name="app">要配置的应用管道</param>
    /// <returns>返回 <paramref name="app"/>，便于链式调用</returns>
    public static IApplicationBuilder EnableBuffering(this IApplicationBuilder app)
    {
        return app.Use(next => context =>
        {
            context.Request.EnableBuffering();
            return next(context);
        });
    }

    /// <summary>
    /// 启用集线器
    /// </summary>
    /// <remarks>须在 app.UseRouting() 之后注册</remarks>
    /// <param name="app">要配置的应用管道</param>
    /// <returns>返回 <paramref name="app"/>，便于链式调用</returns>
    public static IApplicationBuilder UseMapHub(this IApplicationBuilder app)
    {
        Type hubTypes = typeof(Hub);

        var hubsTypes = MAppContext.EffectiveTypes.Where(wh => hubTypes.IsAssignableFrom(wh))
            .Select(sl => new {Type = sl, HubRoutes = sl.GetCustomAttributes<MapHubAttribute>().Select(p => p.Pattern).ToList()})
            .ToList();

        MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions).GetMethods()
            .Where(wh => wh.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub))
            .Where(wh => wh.IsGenericMethodDefinition)
            .First(wh => wh.GetParameters().Length == 2);

        app.UseEndpoints(endpoints =>
        {
            foreach (var hubsType in hubsTypes)
            {
                foreach (string hubRoute in hubsType.HubRoutes)
                {
                    mapHub.MakeGenericMethod(hubsType.Type).Invoke(null, [endpoints, hubRoute]);
                }
            }
        });

        return app;
    }

    /// <summary>
    /// 启用静态文件中间件，并从指定配置节补充或覆盖文件扩展名与 MIME 类型的映射。
    /// </summary>
    /// <remarks>
    /// 配置节以包含前导点的文件扩展名（如 <c>.pdf</c>）为键，以 MIME 类型（如 <c>application/pdf</c>）为值。
    /// 未在配置中指定的扩展名保留默认映射。
    /// </remarks>
    /// <param name="app">要配置的应用管道</param>
    /// <param name="configuration">提供静态文件 MIME 类型映射的配置源</param>
    /// <param name="section">MIME 类型映射所在的配置节路径，默认为 <c>StaticFileSettings</c></param>
    /// <returns>返回 <paramref name="app"/>，便于链式调用</returns>
    public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app, IConfiguration configuration,
        string section = "StaticFileSettings")
    {
        var provider = new FileExtensionContentTypeProvider();
        Dictionary<string, string> mappings = configuration.GetSection(section).Get<Dictionary<string, string>>() ?? [];

        foreach ((string extension, string contentType) in mappings)
        {
            provider.Mappings[extension] = contentType;
        }

        app.UseStaticFiles(new StaticFileOptions {ContentTypeProvider = provider});

        return app;
    }
}
