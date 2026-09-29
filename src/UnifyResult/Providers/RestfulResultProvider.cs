// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fast.UnifyResult;

/// <summary>
/// 规范化 RESTful 风格返回值
/// </summary>
internal sealed class RestfulResultProvider : IUnifyResultProvider
{
    /// <inheritdoc />
    public IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata, int? statusCode = null,
        string message = null)
    {
        // 判断是否为生产环境，避免安全起见，生产环境不返回错误对象
        IHostEnvironment hostEnvironment = context.HttpContext.RequestServices.GetService<IHostEnvironment>();

        // 如果获取到的为空，或者非开发环境，则不返回错误对象
        if (hostEnvironment == null || !hostEnvironment.IsDevelopment())
        {
            return new JsonResult(UnifyContext.GetRestfulResult(statusCode ?? metadata.StatusCode, false, null,
                message ?? context.Exception.Message, context.HttpContext)) {StatusCode = statusCode ?? metadata.StatusCode};
        }

        return new JsonResult(UnifyContext.GetRestfulResult(statusCode ?? metadata.StatusCode, false, context.Exception,
            message ?? context.Exception.Message, context.HttpContext)) {StatusCode = statusCode ?? metadata.StatusCode};
    }

    /// <inheritdoc />
    public IActionResult OnSucceeded(ActionExecutedContext context, object data)
    {
        return new JsonResult(UnifyContext.GetRestfulResult(
            // 处理没有返回值情况 204
            context.Result is EmptyResult ? StatusCodes.Status204NoContent : StatusCodes.Status200OK, true, data, "请求成功",
            context.HttpContext));
    }

    /// <inheritdoc />
    public IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata)
    {
        string message;
        // 一般为 Model 验证失败返回的结果
        if (metadata.ValidationResult is Dictionary<string, string[]> messageObj)
        {
            string newMessage = "";
            foreach (string dicVal in messageObj.SelectMany(dicItem => dicItem.Value))
            {
                newMessage += $"{dicVal}{Environment.NewLine}";
            }

            message = newMessage.Remove(newMessage.LastIndexOf(Environment.NewLine, StringComparison.Ordinal));
        }
        else
        {
            message = metadata.ValidationResult.ToString();
        }

        return new JsonResult(UnifyContext.GetRestfulResult(StatusCodes.Status400BadRequest, false, null, message,
            context.HttpContext)) {StatusCode = StatusCodes.Status400BadRequest};
    }

    /// <inheritdoc />
    public async Task OnResponseStatusCodes(HttpContext httpContext, int statusCode)
    {
        JsonSerializerOptions jsonSerializerOptions = httpContext.RequestServices.GetService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;

        // 设置响应状态码
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(UnifyContext.HandleRestfulStatusCode(statusCode, null, null, httpContext),
            jsonSerializerOptions);
    }
}
