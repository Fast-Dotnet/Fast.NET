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
/// 构建字符串日志部分类
/// </summary>
public sealed partial class StringLoggingPart
{
    /// <summary>
    /// Information
    /// </summary>
    public void LogInformation()
    {
        SetLevel(LogLevel.Information).Log();
    }

    /// <summary>
    /// Warning
    /// </summary>
    public void LogWarning()
    {
        SetLevel(LogLevel.Warning).Log();
    }

    /// <summary>
    /// Error
    /// </summary>
    public void LogError()
    {
        SetLevel(LogLevel.Error).Log();
    }

    /// <summary>
    /// Debug
    /// </summary>
    public void LogDebug()
    {
        SetLevel(LogLevel.Debug).Log();
    }

    /// <summary>
    /// Trace
    /// </summary>
    public void LogTrace()
    {
        SetLevel(LogLevel.Trace).Log();
    }

    /// <summary>
    /// Critical
    /// </summary>
    public void LogCritical()
    {
        SetLevel(LogLevel.Critical).Log();
    }

    /// <summary>
    /// 按当前日志级别、事件和异常配置写入日志
    /// </summary>
    public void Log()
    {
        if (Message == null)
        {
            return;
        }

        (ILogger logger, ILoggerFactory loggerFactory, bool hasException) = GetLogger();
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        using IDisposable scope = logger.BeginScope(LogContext);

        // 如果没有异常且事件Id为空
        if (Exception == null && EventId == null)
        {
            logger.Log(Level, Message, Args);
        }
        // 如果存在异常且事件Id为空
        else if (Exception != null && EventId == null)
        {
            logger.Log(Level, Exception, Message, Args);
        }
        // 如果异常为空且事件Id不为空
        else if (Exception == null && EventId != null)
        {
            logger.Log(Level, EventId.Value, Message, Args);
        }
        // 如果存在异常且事件Id不为空
        else if (Exception != null && EventId != null)
        {
            logger.Log(Level, EventId.Value, Exception, Message, Args);
        }

        // 释放临时日志工厂
        if (hasException)
        {
            loggerFactory?.Dispose();
        }
    }

    /// <summary>
    /// 获取日志实例
    /// </summary>
    /// <returns>获取到的日志实例</returns>
    internal (ILogger, ILoggerFactory, bool) GetLogger()
    {
        // 解析日志分类名
        Type categoryType = CategoryType ?? typeof(StringLogging);

        ILoggerFactory loggerFactory = null;
        ILogger logger = null;
        bool hasException = false;

        // 解决启动时打印日志问题
        if (Penetrates.RootServices == null)
        {
            hasException = true;
            loggerFactory = CreateDisposeLoggerFactory();
        }
        else
        {
            try
            {
                logger = MAppContext
                    .GetServiceProvider(typeof(ILogger<>), Penetrates.RootServices, Penetrates.InternalServices,
                        Penetrates.HttpContext)
                    .GetRequiredService(typeof(ILogger<>).MakeGenericType(categoryType)) as ILogger;
            }
            catch
            {
                hasException = true;
                loggerFactory = CreateDisposeLoggerFactory();
            }
        }

        // 创建日志实例
        logger ??= loggerFactory?.CreateLogger(categoryType.FullName);

        return (logger, loggerFactory, hasException);
    }

    /// <summary>
    /// 创建待释放的日志工厂
    /// </summary>
    /// <returns>创建的待释放的日志工厂</returns>
    private static ILoggerFactory CreateDisposeLoggerFactory()
    {
        return LoggerFactory.Create(builder => { builder.AddConsoleFormatter(); });
    }
}
