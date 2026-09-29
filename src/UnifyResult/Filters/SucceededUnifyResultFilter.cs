// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Fast.UnifyResult;

/// <summary>
/// 规范化结构（请求成功）过滤器
/// </summary>
internal sealed class SucceededUnifyResultFilter : IAsyncActionFilter, IOrderedFilter
{
    /// <summary>
    /// 过滤器排序
    /// </summary>
    private const int FilterOrder = 8888;

    /// <inheritdoc />
    public int Order => FilterOrder;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 执行 Action 并获取结果
        ActionExecutedContext actionExecutedContext = await next();

        // 排除 WebSocket 请求处理
        if (actionExecutedContext.HttpContext.WebSockets.IsWebSocketRequest)
        {
            return;
        }

        // 处理已经含有状态码结果的 Result
        if (actionExecutedContext.Result is IStatusCodeActionResult statusCodeActionResult
            && statusCodeActionResult.StatusCode != null)
        {
            // 小于 200 或者 大于 299 都不是成功值，直接跳过
            if (statusCodeActionResult.StatusCode.Value < 200 || statusCodeActionResult.StatusCode.Value > 299)
            {
                if (!UnifyContext.CheckStatusCodeNonUnify(context.HttpContext, out IUnifyResultProvider failUnifyResult))
                {
                    HttpContext httpContext = context.HttpContext;
                    int statusCode = statusCodeActionResult.StatusCode.Value;

                    // 解决刷新 Token 时间和 Token 时间相近问题
                    if (statusCodeActionResult.StatusCode.Value == StatusCodes.Status401Unauthorized
                        && httpContext.Request.Headers.ContainsKey("access-token")
                        && httpContext.Request.Headers.ContainsKey("x-access-token"))
                    {
                        httpContext.Response.StatusCode = statusCode = StatusCodes.Status403Forbidden;
                    }

                    // 如果 Response 已经完成输出，则禁止写入
                    if (httpContext.Response.HasStarted)
                    {
                        return;
                    }

                    await failUnifyResult.OnResponseStatusCodes(httpContext, statusCode);
                }

                return;
            }
        }

        // 如果出现异常，则不会进入该过滤器
        if (actionExecutedContext.Exception != null)
        {
            return;
        }

        // 获取控制器信息
        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;

        // 判断是否跳过规范化处理
        if (UnifyContext.CheckSucceededNonUnify(context.HttpContext, controllerActionDescriptor!.MethodInfo,
                out IUnifyResultProvider unifyResult))
        {
            return;
        }

        // 处理 BadRequestObjectResult 类型规范化处理
        if (actionExecutedContext.Result is BadRequestObjectResult badRequestObjectResult)
        {
            // 解析验证消息
            ValidationMetadata validationMetadata = UnifyContext.GetValidationMetadata(badRequestObjectResult.Value);

            IActionResult result = unifyResult.OnValidateFailed(context, validationMetadata);

            if (result != null)
            {
                actionExecutedContext.Result = result;
            }
        }
        else
        {
            IActionResult result = null;

            // 检查是否是有效的结果（可进行规范化的结果）
            if (UnifyContext.CheckValidResult(actionExecutedContext.Result, out object data))
            {
                long timestamp = context.HttpContext.UnifyResponseTimestamp();

                if (!UnifyContext.CheckResponseNonUnify(context.HttpContext, controllerActionDescriptor!.MethodInfo,
                        out IUnifyResponseProvider unifyResponse))
                {
                    data = await unifyResponse.ResponseDataAsync(timestamp, data, context.HttpContext);
                }

                result = unifyResult.OnSucceeded(actionExecutedContext, data);
            }

            // 如果是不能规范化的结果类型，则跳过
            if (result == null)
            {
                return;
            }

            actionExecutedContext.Result = result;
        }
    }
}
