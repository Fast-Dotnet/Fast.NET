// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.Logging;

namespace Fast.Logging;

/// <summary>
/// 构建字符串日志部分类
/// </summary>
public sealed partial class StringLoggingPart
{
    /// <summary>
    /// 设置消息
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <returns>设置消息</returns>
    public StringLoggingPart SetMessage(string message)
    {
        if (message != null)
        {
            Message = message;
        }

        return this;
    }

    /// <summary>
    /// 设置日志级别
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>设置日志级别</returns>
    public StringLoggingPart SetLevel(LogLevel level)
    {
        Level = level;
        return this;
    }

    /// <summary>
    /// 设置消息格式化参数
    /// </summary>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <returns>设置消息格式化参数</returns>
    public StringLoggingPart SetArgs(params object[] args)
    {
        if (args != null && args.Length > 0)
        {
            Args = args;
        }

        return this;
    }

    /// <summary>
    /// 设置事件Id
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    /// <returns>设置事件Id</returns>
    public StringLoggingPart SetEventId(EventId eventId)
    {
        EventId = eventId;
        return this;
    }

    /// <summary>
    /// 设置日志分类
    /// </summary>
    /// <typeparam name="TClass">要处理的对象类型</typeparam>
    /// <returns>设置日志分类</returns>
    public StringLoggingPart SetCategory<TClass>()
    {
        CategoryType = typeof(TClass);
        return this;
    }

    /// <summary>
    /// 设置异常对象
    /// </summary>
    /// <param name="exception">要处理的异常</param>
    /// <returns>设置异常对象</returns>
    public StringLoggingPart SetException(Exception exception)
    {
        if (exception != null)
        {
            Exception = exception;
        }

        return this;
    }

    /// <summary>
    /// 设置日志服务作用域
    /// </summary>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    /// <returns>设置日志服务作用域</returns>
    public StringLoggingPart SetLoggerScoped(IServiceProvider serviceProvider)
    {
        if (serviceProvider != null)
        {
            LoggerScoped = serviceProvider;
        }

        return this;
    }

    /// <summary>
    /// 配置日志上下文
    /// </summary>
    /// <param name="properties">要写入上下文的属性集合</param>
    /// <returns>日志记录器以及用于结束作用域的释放句柄</returns>
    public StringLoggingPart ScopeContext(IDictionary<object, object> properties)
    {
        if (properties == null)
        {
            return this;
        }

        LogContext = new LogContext {Properties = properties};

        return this;
    }

    /// <summary>
    /// 配置日志上下文
    /// </summary>
    /// <param name="configure">日志上下文配置操作</param>
    /// <returns>日志记录器以及用于结束作用域的释放句柄</returns>
    public StringLoggingPart ScopeContext(Action<LogContext> configure)
    {
        var logContext = new LogContext();
        configure?.Invoke(logContext);

        LogContext = logContext;

        return this;
    }

    /// <summary>
    /// 配置日志上下文
    /// </summary>
    /// <param name="context">当前日志上下文</param>
    /// <returns>日志记录器以及用于结束作用域的释放句柄</returns>
    public StringLoggingPart ScopeContext(LogContext context)
    {
        if (context == null)
        {
            return this;
        }

        LogContext = context;

        return this;
    }
}
