// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fast.UnifyResult;

/// <summary>
/// 规范化结果提供器
/// </summary>
public interface IUnifyResultProvider
{
    /// <summary>
    /// 异常返回值
    /// </summary>
    /// <param name="context">当前异常处理上下文</param>
    /// <param name="metadata">异常响应使用的元数据</param>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="message">要记录或返回的消息</param>
    /// <returns>异常返回值</returns>
    IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata, int? statusCode = null,
        string message = null);

    /// <summary>
    /// 成功返回值
    /// </summary>
    /// <param name="context">当前操作上下文</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <returns>成功返回值</returns>
    IActionResult OnSucceeded(ActionExecutedContext context, object data);

    /// <summary>
    /// 验证失败返回值
    /// </summary>
    /// <param name="context">当前操作上下文</param>
    /// <param name="metadata">验证失败响应使用的元数据</param>
    /// <returns>验证失败返回值</returns>
    IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata);

    /// <summary>
    /// 拦截返回状态码
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <returns>表示异步“拦截返回状态码”操作的任务</returns>
    Task OnResponseStatusCodes(HttpContext httpContext, int statusCode);
}
