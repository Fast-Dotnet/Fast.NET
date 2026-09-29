// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fast.Consul;

/// <summary>
/// 应用启动时自动注册中间件
/// </summary>
internal sealed class ConsulStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> action)
    {
        return app =>
        {
            if (Penetrates.ConsulSettings.Enable == true)
            {
                // 注册心跳响应
                app.Map(Penetrates.ConsulSettings.HealthCheck, options =>
                {
                    options.Run(async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteAsync("OK");
                    });
                });

                // 获取 IHostApplicationLifetime 实例
                IHostApplicationLifetime hostApplicationLifetime =
                    app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

                // IServerAddressesFeature 仅在应用启动完成后可用；同时必须观察注册任务的异常
                hostApplicationLifetime.ApplicationStarted.Register(() =>
                {
                    Task registerTask = app.ApplicationServices.GetService<IConsulRegister>()?.ConsulRegisterAsync();
                    if (registerTask == null)
                    {
                        return;
                    }

                    _ = registerTask.ContinueWith(
                        task => { Console.Error.WriteLine($"[Fast.Consul] Service registration failed: {task.Exception}"); },
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                });
            }

            // 无论 Consul 是否启用，都必须继续配置应用管道
            action(app);
        };
    }
}
