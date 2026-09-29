// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Fast.Swagger;

/// <summary>
/// 为 <see cref="IApplicationBuilder"/> 提供 Swagger 扩展方法
/// </summary>
[SuppressSniffer]
public static class IApplicationBuilderExtension
{
    /// <summary>
    /// 启用 Swagger
    /// </summary>
    /// <param name="app">要配置的应用管道</param>
    /// <param name="configure">Swagger 配置操作</param>
    /// <param name="configureUI">Swagger UI 配置操作</param>
    /// <returns>返回当前应用管道构建器，便于链式调用</returns>
    public static IApplicationBuilder UseSwaggerDocuments(this IApplicationBuilder app, Action<SwaggerOptions> configure = null,
        Action<SwaggerUIOptions> configureUI = null)
    {
        if (Penetrates.SwaggerSettings.Enable!.Value)
        {
            // 配置 Swagger 全局参数
            app.UseSwagger(options => SwaggerDocumentBuilder.Build(options, configure));

            // 配置 Swagger UI 参数
            app.UseSwaggerUI(options => SwaggerDocumentBuilder.BuildUI(options, configureUI));
        }

        return app;
    }
}
