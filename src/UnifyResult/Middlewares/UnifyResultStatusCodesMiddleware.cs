// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;

namespace Fast.UnifyResult;

/// <summary>
/// 状态码中间件
/// </summary>
internal sealed class UnifyResultStatusCodesMiddleware
{
    /// <summary>
    /// 请求委托
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="next">处理管道中的下一个委托</param>
    public UnifyResultStatusCodesMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 中间件执行方法
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>表示异步中间件执行方法的任务</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        await _next(httpContext);

        // 只有请求错误（短路状态码）和非 WebSocket 才支持规范化处理
        if (httpContext.Response.HasStarted
            || httpContext.Response.ContentLength > 0
            || httpContext.WebSockets.IsWebSocketRequest
            || httpContext.Response.StatusCode < 400
            || httpContext.Response.StatusCode == 404)
        {
            return;
        }

        if (!UnifyContext.CheckStatusCodeNonUnify(httpContext, out IUnifyResultProvider unifyResult))
        {
            // 如果 Response 已经完成输出，则禁止写入
            if (httpContext.Response.HasStarted)
            {
                return;
            }

            await unifyResult.OnResponseStatusCodes(httpContext, httpContext.Response.StatusCode);
        }
    }
}
