// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fast.Logging;

/// <summary>
/// 全局日志静态类
/// </summary>
[SuppressSniffer]
public static class Log
{
    /// <summary>
    /// 手动构建方式
    /// </summary>
    /// <returns>手动构建方式</returns>
    public static StringLoggingPart Default()
    {
        return StringLoggingPart.Default();
    }

    /// <summary>
    /// 创建日志记录器
    /// </summary>
    /// <typeparam name="T">日志记录器使用的分类类型</typeparam>
    /// <returns>创建的日志记录器</returns>
    public static ILogger CreateLogger<T>()
    {
        return MAppContext
            .GetServiceProvider(typeof(ILogger<T>), Penetrates.RootServices, Penetrates.InternalServices, Penetrates.HttpContext)
            .GetRequiredService<ILogger<T>>();
    }

    /// <summary>
    /// 创建日志工厂
    /// </summary>
    /// <remarks><see cref="ILoggerFactory"/> 实现了 <see cref="IDisposable"/> 接口，注意使用 `using` 控制</remarks>
    /// <param name="configure">日志构建配置操作</param>
    /// <returns>创建的日志工厂</returns>
    public static ILoggerFactory CreateLoggerFactory(Action<ILoggingBuilder> configure = null)
    {
        return LoggerFactory.Create(builder =>
        {
            // 添加默认控制台输出
            builder.AddConsoleFormatter();

            configure?.Invoke(builder);
        });
    }

    /// <summary>
    /// 创建包含指定属性的日志作用域
    /// </summary>
    /// <param name="properties">要加入日志作用域的属性；并发修改时应使用 <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/></param>
    /// <returns>日志记录器以及用于结束作用域的释放句柄</returns>
    public static (ILogger logger, IDisposable scope) ScopeContext(IDictionary<object, object> properties)
    {
        return GetLogger(StringLoggingPart.Default().ScopeContext(properties));
    }

    /// <summary>
    /// 创建包含指定属性的日志作用域
    /// </summary>
    /// <param name="configure">日志上下文配置操作</param>
    /// <returns>日志记录器以及用于结束作用域的释放句柄</returns>
    public static (ILogger logger, IDisposable scope) ScopeContext(Action<LogContext> configure)
    {
        return GetLogger(StringLoggingPart.Default().ScopeContext(configure));
    }

    /// <summary>
    /// 创建包含指定属性的日志作用域
    /// </summary>
    /// <param name="context">当前日志上下文</param>
    /// <returns>日志记录器以及用于结束作用域的释放句柄</returns>
    public static (ILogger logger, IDisposable scope) ScopeContext(LogContext context)
    {
        return GetLogger(StringLoggingPart.Default().ScopeContext(context));
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Information(string message, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Information(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Information(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetException(exception).LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Information(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetMessage(message)
            .SetArgs(args)
            .SetEventId(eventId)
            .SetException(exception)
            .LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Information<TClass>(string message, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Information<TClass>(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetEventId(eventId).LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Information<TClass>(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetCategory<TClass>()
            .SetMessage(message)
            .SetArgs(args)
            .SetException(exception)
            .LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Information"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Information<TClass>(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetCategory<TClass>()
            .SetMessage(message)
            .SetArgs(args)
            .SetEventId(eventId)
            .SetException(exception)
            .LogInformation();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Warning(string message, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Warning(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Warning(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetException(exception).LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Warning(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).SetException(exception).LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Warning<TClass>(string message, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Warning<TClass>(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetEventId(eventId).LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Warning<TClass>(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetException(exception).LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Warning"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Warning<TClass>(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetCategory<TClass>()
            .SetMessage(message)
            .SetArgs(args)
            .SetEventId(eventId)
            .SetException(exception)
            .LogWarning();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Error(string message, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Error(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Error(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetException(exception).LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Error(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).SetException(exception).LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Error<TClass>(string message, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Error<TClass>(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetEventId(eventId).LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Error<TClass>(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetException(exception).LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Error"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Error<TClass>(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetCategory<TClass>()
            .SetMessage(message)
            .SetArgs(args)
            .SetEventId(eventId)
            .SetException(exception)
            .LogError();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Debug(string message, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Debug(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Debug(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetException(exception).LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Debug(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).SetException(exception).LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Debug<TClass>(string message, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Debug<TClass>(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetEventId(eventId).LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Debug<TClass>(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetException(exception).LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Debug"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Debug<TClass>(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetCategory<TClass>()
            .SetMessage(message)
            .SetArgs(args)
            .SetEventId(eventId)
            .SetException(exception)
            .LogDebug();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Trace(string message, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Trace(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Trace(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetException(exception).LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Trace(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).SetException(exception).LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Trace<TClass>(string message, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Trace<TClass>(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetEventId(eventId).LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Trace<TClass>(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetException(exception).LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Trace"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Trace<TClass>(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetCategory<TClass>()
            .SetMessage(message)
            .SetArgs(args)
            .SetEventId(eventId)
            .SetException(exception)
            .LogTrace();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Critical(string message, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).LogCritical();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Critical(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).LogCritical();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Critical(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetException(exception).LogCritical();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    public static void Critical(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetMessage(message).SetArgs(args).SetEventId(eventId).SetException(exception).LogCritical();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Critical<TClass>(string message, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).LogCritical();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Critical<TClass>(string message, EventId eventId, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetEventId(eventId).LogCritical();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Critical<TClass>(string message, Exception exception, params object[] args)
    {
        StringLoggingPart.Default().SetCategory<TClass>().SetMessage(message).SetArgs(args).SetException(exception).LogCritical();
    }

    /// <summary>
    /// 写入 <see cref="LogLevel.Critical"/> 级别日志
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="exception">要处理的异常</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    public static void Critical<TClass>(string message, EventId eventId, Exception exception, params object[] args)
    {
        StringLoggingPart.Default()
            .SetCategory<TClass>()
            .SetMessage(message)
            .SetArgs(args)
            .SetEventId(eventId)
            .SetException(exception)
            .LogCritical();
    }

    /// <summary>
    /// 获取日志实例
    /// </summary>
    /// <param name="loggingPart">用于创建日志记录器的日志消息构建器</param>
    /// <returns>获取到的日志实例</returns>
    private static (ILogger, IDisposable) GetLogger(StringLoggingPart loggingPart)
    {
        (ILogger logger, ILoggerFactory loggerFactory, bool hasException) = loggingPart.GetLogger();

        if (logger == null)
        {
            throw new InvalidOperationException("Unable to create a logger instance.");
        }

        IDisposable scope = logger.BeginScope(loggingPart.LogContext);
        if (hasException)
        {
            scope!.Dispose();
            loggerFactory?.Dispose();

            throw new InvalidOperationException("Unable to set log context data.");
        }

        return (logger, scope);
    }
}
