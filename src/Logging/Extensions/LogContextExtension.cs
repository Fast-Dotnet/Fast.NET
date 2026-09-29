// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.Logging;

/// <summary>
/// LogContext 扩展
/// </summary>
[SuppressSniffer]
public static class LogContextExtension
{
    /// <summary>
    /// 设置上下文数据
    /// </summary>
    /// <param name="logContext">当前日志上下文</param>
    /// <param name="key">日志上下文属性键</param>
    /// <param name="value">要写入日志上下文的属性值</param>
    /// <returns>设置上下文数据</returns>
    public static LogContext Set(this LogContext logContext, object key, object value)
    {
        if (logContext == null || key == null)
        {
            return logContext;
        }

        logContext.Properties ??= new Dictionary<object, object>();

        if (logContext.Properties.ContainsKey(key))
        {
            logContext.Properties.Remove(key);
        }

        logContext.Properties.Add(key, value);
        return logContext;
    }

    /// <summary>
    /// 批量设置上下文数据
    /// </summary>
    /// <param name="logContext">当前日志上下文</param>
    /// <param name="properties">要写入上下文的属性集合</param>
    /// <returns>批量设置上下文数据</returns>
    public static LogContext SetRange(this LogContext logContext, IDictionary<object, object> properties)
    {
        if (logContext == null || properties == null || properties.Count == 0)
        {
            return logContext;
        }

        foreach ((object key, object value) in properties)
        {
            logContext.Set(key, value);
        }

        return logContext;
    }

    /// <summary>
    /// 获取上下文数据
    /// </summary>
    /// <param name="logContext">当前日志上下文</param>
    /// <param name="key">日志上下文属性键</param>
    /// <returns>获取到的上下文数据</returns>
    public static object Get(this LogContext logContext, object key)
    {
        if (logContext == null || key == null || logContext.Properties == null || logContext.Properties.Count == 0)
        {
            return null;
        }

        bool isExists = logContext.Properties.TryGetValue(key, out object value);
        return isExists ? value : null;
    }

    /// <summary>
    /// 获取上下文数据
    /// </summary>
    /// <param name="logContext">当前日志上下文</param>
    /// <param name="key">日志上下文属性键</param>
    /// <typeparam name="T">上下文属性值的目标类型</typeparam>
    /// <returns>获取到的上下文数据</returns>
    public static object Get<T>(this LogContext logContext, object key)
    {
        object value = logContext.Get(key);
        return (T)value;
    }
}
