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
}
