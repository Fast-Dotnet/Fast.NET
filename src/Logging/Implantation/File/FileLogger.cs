// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.Logging;

namespace Fast.Logging;

/// <summary>
/// 文件日志记录器
/// </summary>
/// <remarks>实现遵循 Microsoft.Extensions.Logging 自定义日志提供器约定</remarks>
internal sealed class FileLogger : ILogger
{
    /// <summary>
    /// 记录器类别名称
    /// </summary>
    private readonly string _logName;

    /// <summary>
    /// 文件日志记录器提供器
    /// </summary>
    private readonly FileLoggerProvider _fileLoggerProvider;

    /// <summary>
    /// 日志配置选项
    /// </summary>
    private readonly FileLoggerOptions _options;

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="logName">记录器类别名称</param>
    /// <param name="fileLoggerProvider">文件日志记录器提供器</param>
    public FileLogger(string logName, FileLoggerProvider fileLoggerProvider)
    {
        _logName = logName;
        _fileLoggerProvider = fileLoggerProvider;
        _options = fileLoggerProvider.LoggerOptions;
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
    {
        return _fileLoggerProvider.ScopeProvider.Push(state);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _options.MinimumLevel;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        // 判断日志级别是否有效
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // 检查日志格式化器
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        string message = formatter(state, exception);

        DateTime logDateTime = _options.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
        var logMsg = new LogMessage(_logName, logLevel, eventId, message, exception, null, state, logDateTime,
            Environment.CurrentManagedThreadId, _options.UseUtcTimestamp,
            MAppContext.GetTraceId(Penetrates.RootServices, Penetrates.HttpContext));

        logMsg = Penetrates.SetLogContext(_fileLoggerProvider.ScopeProvider, logMsg, _options.IncludeScopes);

        // 判断是否自定义了日志筛选器，如果是则检查是否符合条件
        if (_options.WriteFilter?.Invoke(logMsg) == false)
        {
            return;
        }

        logMsg.Message = _options.MessageFormat != null
            ? _options.MessageFormat(logMsg)
            : LoggingContext.OutputStandardMessage(logMsg, _options.DateFormat, withTraceId: _options.WithTraceId,
                withStackFrame: _options.WithStackFrame);

        if (logMsg.Message is null)
        {
            return;
        }

        // 写入日志队列
        _fileLoggerProvider.WriteToQueue(logMsg);
    }
}
