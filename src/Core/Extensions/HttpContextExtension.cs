// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;

namespace Fast.NET.Core;

/// <summary>
/// 为 <see cref="HttpContext"/> 提供扩展方法
/// </summary>
[SuppressSniffer]
public static class HttpContextExtension
{
    /// <summary>
    /// 获取请求方式
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>获取到的请求方式；当前请求上下文为空时返回 <see cref="HttpRequestMethodEnum.Unknown"/></returns>
    public static HttpRequestMethodEnum GetRequestMethod(this HttpContext httpContext)
    {
        if (httpContext == null)
        {
            return HttpRequestMethodEnum.Unknown;
        }

        return httpContext.Request.Method switch
        {
            "GET" => HttpRequestMethodEnum.Get,
            "POST" => HttpRequestMethodEnum.Post,
            "PUT" => HttpRequestMethodEnum.Put,
            "DELETE" => HttpRequestMethodEnum.Delete,
            "PATCH" => HttpRequestMethodEnum.Patch,
            "HEAD" => HttpRequestMethodEnum.Head,
            "OPTIONS" => HttpRequestMethodEnum.Options,
            "CONNECT" => HttpRequestMethodEnum.Connect,
            "TRACE" => HttpRequestMethodEnum.Trace,
            _ => throw new HttpRequestException("Unknown request mode.")
        };
    }
}
