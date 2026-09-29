// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fast.UnifyResult;

/// <summary>
/// 规范化响应数据提供器
/// </summary>
public interface IUnifyResponseProvider
{
    /// <summary>
    /// 响应异常处理
    /// </summary>
    /// <param name="context">当前异常处理上下文</param>
    /// <param name="metadata">异常响应使用的元数据</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>表示异步响应异常处理的任务，任务结果为响应异常处理</returns>
    Task<(int statusCode, string message)> ResponseExceptionAsync(ExceptionContext context, ExceptionMetadata metadata,
        HttpContext httpContext);

    /// <summary>
    /// 响应数据验证异常处理
    /// </summary>
    /// <param name="context">当前操作上下文</param>
    /// <param name="metadata">验证失败响应使用的元数据</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>表示异步“响应数据验证异常处理”操作的任务</returns>
    Task ResponseValidationExceptionAsync(ActionExecutingContext context, ValidationMetadata metadata, HttpContext httpContext);

    /// <summary>
    /// 响应数据处理
    /// </summary>
    /// <remarks>只有响应成功且为正常返回才会调用</remarks>
    /// <param name="timestamp">时间戳</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>表示异步响应数据处理的任务，任务结果为响应数据处理</returns>
    Task<object> ResponseDataAsync(long timestamp, object data, HttpContext httpContext);
}
