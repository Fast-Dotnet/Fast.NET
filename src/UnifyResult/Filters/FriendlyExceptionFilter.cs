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
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.UnifyResult;

/// <summary>
/// 友好异常拦截器
/// </summary>
internal sealed class FriendlyExceptionFilter : IAsyncExceptionFilter
{
    /// <inheritdoc />
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        bool isUserFriendlyException = false;
        bool isValidationException = false;

        // 判断是否为友好异常
        if (context.Exception is UserFriendlyException userFriendlyException)
        {
            isUserFriendlyException = true;

            // 判断是否为验证异常
            if (userFriendlyException.ValidationException)
            {
                isValidationException = true;
            }
        }

        // 解析异常处理服务，实现自定义操作
        IGlobalExceptionHandler globalExceptionHandler =
            context.HttpContext.RequestServices.GetService<IGlobalExceptionHandler>();

        if (globalExceptionHandler != null)
        {
            await globalExceptionHandler.OnExceptionAsync(context, isUserFriendlyException, isValidationException);
        }

        // 排除 WebStock 请求处理
        if (context.HttpContext.WebSockets.IsWebSocketRequest)
        {
            return;
        }

        // 如果异常在其他地方被标记处理，那么这里不再处理
        if (context.ExceptionHandled)
        {
            return;
        }

        // 解析异常信息
        ExceptionMetadata exceptionMetadata = UnifyContext.GetExceptionMetadata(context);

        // 判断是否是 Razor Pages
        bool isPageDescriptor = context.ActionDescriptor is CompiledPageActionDescriptor;

        // 判断是否是验证异常，如果是，则不处理
        if (isValidationException)
        {
            // 从 HttpContext 上下文中读取验证执行结果
            object resultHttpContext = context.HttpContext.Items[nameof(DataValidationFilter) + nameof(UserFriendlyException)];

            if (resultHttpContext != null)
            {
                IActionResult result = (resultHttpContext as ActionExecutedContext)?.Result;

                // 直接将验证结果设置为异常结果
                context.Result = result
                                 ?? new BadPageResult(StatusCodes.Status400BadRequest)
                                 {
                                     Code = UnifyContext
                                         .GetValidationMetadata((context.Exception as UserFriendlyException)?.ErrorMessage)
                                         .Message
                                 };

                // 标记验证异常已被处理
                context.ExceptionHandled = true;
                return;
            }
        }

        // 处理 Razor Pages
        if (isPageDescriptor)
        {
            // 返回自定义错误页面
            context.Result =
                new BadPageResult(isValidationException ? StatusCodes.Status400BadRequest : exceptionMetadata.StatusCode)
                {
                    Title = isValidationException ? "ModelState Invalid" : "Internal Server: " + exceptionMetadata.Errors,
                    Code = isValidationException
                        ? UnifyContext.GetValidationMetadata((context.Exception as UserFriendlyException)?.ErrorMessage)
                            .Message
                        : context.Exception.ToString()
                };
        }
        // 处理 Mvc/WebApi
        else
        {
            // 获取控制器信息
            if (context.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
            {
                return;
            }

            // 判断是否跳过规范化结果，如果是，则只处理友好异常消息
            if (UnifyContext.CheckFailedNonUnify(context.HttpContext, controllerActionDescriptor.MethodInfo,
                    out IUnifyResultProvider unifyResult))
            {
                // 返回 JsonResult
                context.Result = new JsonResult(exceptionMetadata.Errors) {StatusCode = exceptionMetadata.StatusCode};
            }
            else
            {
                int? statusCode = null;
                string message = null;
                if (!UnifyContext.CheckResponseNonUnify(context.HttpContext, controllerActionDescriptor!.MethodInfo,
                        out IUnifyResponseProvider unifyResponse))
                {
                    (statusCode, message) =
                        await unifyResponse.ResponseExceptionAsync(context, exceptionMetadata, context.HttpContext);
                }

                context.Result = unifyResult.OnException(context, exceptionMetadata, statusCode, message);
            }
        }
    }
}
