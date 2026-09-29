// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fast.NET.Core;

/// <summary>
/// 应用启动时自动注册中间件
/// </summary>
internal sealed class CoreStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> action)
    {
        return app =>
        {
            // 存储应用程序构建起
            FastContext.WebApplication = app;

            // 存储根服务
            FastContext.RootServices = app.ApplicationServices;

            // 兼容显式登记的应用级资源，只在应用停止时释放，不再由任意请求提前释放。
            app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopped.Register(MAppContext.DisposeUnmanagedObjects);

            // 解决 IIS 或者 Nginx 反向代理获取不到真实客户端 IP 的问题
            app.UseForwardedHeaders();

            // 环境名
            string envName = FastContext.WebHostEnvironment?.EnvironmentName ?? "Unknown";

            // 设置响应报文头信息
            app.Use(async (context, next) =>
            {
                // 处理 WebSocket 请求
                if (context.WebSockets.IsWebSocketRequest)
                {
                    await next.Invoke();
                }
                else
                {
                    // 输出当前程序集版本号
                    context.Response.Headers.TryAdd(nameof(Fast) + "-Version", $"v{MAppContext.AssemblyVersion}");

                    // 输出当前环境标识
                    context.Response.Headers.TryAdd(nameof(Fast) + "-Environment", envName);

                    // 输出当前请求时间
                    context.Response.Headers.TryAdd(nameof(Fast) + "-Request-Time",
                        DateTimeOffset.Now.ToString("dddd, zzz, yyyy-MM-dd HH:mm:ss.fffffff", new CultureInfo("en-US")));

                    // 默认输出信息
                    context.Response.Headers.TryAdd(nameof(Fast) + "-Site-Url", "https://fastdotnet.com");
                    context.Response.Headers.TryAdd(nameof(Fast) + "-Repository-Url", "https://gitee.com/FastDotnet/Fast.NET");

                    // 执行下一个中间件
                    await next.Invoke();

                    // 解决刷新 Token 时间和 Token 时间相近问题
                    if (!context.Response.HasStarted
                        && context.Response.StatusCode == StatusCodes.Status401Unauthorized
                        && context.Response.Headers.ContainsKey("access-token")
                        && context.Response.Headers.ContainsKey("x-access-token"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    }
                }
            });

            // 调用启动层的 Startup
            action(app);
        };
    }
}
