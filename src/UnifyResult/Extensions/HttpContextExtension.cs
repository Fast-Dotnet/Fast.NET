// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Fast.UnifyResult;

/// <summary>
/// 为 <see cref="HttpContext"/> 提供扩展方法
/// </summary>
internal static class HttpContextExtension
{
    /// <summary>
    /// 设置规范化响应时间戳
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="timestamp">要写入响应头的毫秒时间戳</param>
    public static void UnifyResponseTimestamp(this HttpContext httpContext, long timestamp)
    {
        httpContext?.Response.Headers.TryAdd(nameof(Fast) + "-NET-Timestamp", $"{timestamp}");
    }

    /// <summary>
    /// 获取规范化响应时间戳
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>响应时间戳</returns>
    public static long UnifyResponseTimestamp(this HttpContext httpContext)
    {
        StringValues? timestampStr = httpContext?.Response.Headers[nameof(Fast) + "-NET-Timestamp"];

        if (string.IsNullOrEmpty(timestampStr))
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 将请求开始时间写入响应头，供耗时统计复用
            httpContext.UnifyResponseTimestamp(timestamp);

            return timestamp;
        }

        return long.Parse(timestampStr);
    }

    /// <summary>
    /// 获取终结点元数据中的指定特性
    /// </summary>
    /// <param name="metadata">终结点元数据集合</param>
    /// <param name="attributeType">要读取的特性类型</param>
    /// <returns>匹配的特性实例；不存在时返回 <see langword="null"/></returns>
    public static object GetMetadata(this EndpointMetadataCollection metadata, Type attributeType)
    {
        return metadata?.GetType()
            .GetMethod(nameof(EndpointMetadataCollection.GetMetadata))
            ?.MakeGenericMethod(attributeType)
            .Invoke(metadata, null);
    }

    /// <summary>
    /// 获取终结点元数据中的指定特性
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="attributeType">要读取的特性类型</param>
    /// <returns>当前终结点上匹配的特性实例；不存在时返回 <see langword="null"/></returns>
    public static object GetMetadata(this HttpContext httpContext, Type attributeType)
    {
        return httpContext?.GetEndpoint()?.Metadata.GetMetadata(attributeType);
    }
}
