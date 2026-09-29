// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Fast.Logging;

/// <summary>
/// 控制台默认格式化程序扩展
/// </summary>
internal sealed class ConsoleFormatterExtend : ConsoleFormatter, IDisposable
{
    /// <summary>
    /// 日志格式化选项刷新 Token
    /// </summary>
    private readonly IDisposable _formatOptionsReloadToken;

    /// <summary>
    /// 日志格式化配置选项
    /// </summary>
    private ConsoleFormatterExtendOptions _formatterOptions;

    /// <summary>
    /// 是否启用控制台颜色
    /// </summary>
    private bool _disableColors;

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="formatterOptions">formatter Options 配置</param>
    public ConsoleFormatterExtend(IOptionsMonitor<ConsoleFormatterExtendOptions> formatterOptions) : base(Penetrates
        .ConsoleFormatterName)
    {
        (_formatOptionsReloadToken, _formatterOptions) =
            (formatterOptions.OnChange(ReloadFormatterOptions), formatterOptions.CurrentValue);
        _disableColors = _formatterOptions.ColorBehavior == LoggerColorBehavior.Disabled
                         || (_formatterOptions.ColorBehavior == LoggerColorBehavior.Default && Console.IsOutputRedirected);
    }

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        string message = logEntry.Formatter!.Invoke(logEntry.State, logEntry.Exception);

        // 创建日志消息
        DateTime logDateTime = _formatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
        var logMsg = new LogMessage(logEntry.Category, logEntry.LogLevel, logEntry.EventId, message, logEntry.Exception, null,
            logEntry.State, logDateTime, Environment.CurrentManagedThreadId, _formatterOptions.UseUtcTimestamp,
            MAppContext.GetTraceId(Penetrates.RootServices, Penetrates.HttpContext));

        string standardMessage;

        // 是否自定义了自定义日志格式化程序，如果是则使用
        if (_formatterOptions.MessageFormat != null)
        {
            logMsg = Penetrates.SetLogContext(scopeProvider, logMsg, _formatterOptions.IncludeScopes);

            standardMessage = _formatterOptions.MessageFormat(logMsg);
        }
        else
        {
            // 获取标准化日志消息
            standardMessage = LoggingContext.OutputStandardMessage(logMsg, _formatterOptions.DateFormat, true, _disableColors,
                _formatterOptions.WithTraceId, _formatterOptions.WithStackFrame);
        }

        if (message is null)
        {
            return;
        }

        // 判断是否自定义了日志格式化程序
        if (_formatterOptions.WriteHandler != null)
        {
            _formatterOptions.WriteHandler?.Invoke(logMsg, scopeProvider, textWriter, standardMessage, _formatterOptions);
        }
        else
        {
            // 写入控制台
            textWriter.WriteLine(standardMessage);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _formatOptionsReloadToken?.Dispose();
    }

    /// <summary>
    /// 刷新日志格式化选项
    /// </summary>
    /// <param name="options">当前组件使用的选项</param>
    private void ReloadFormatterOptions(ConsoleFormatterExtendOptions options)
    {
        _formatterOptions = options;
        _disableColors = options.ColorBehavior == LoggerColorBehavior.Disabled
                         || (options.ColorBehavior == LoggerColorBehavior.Default && Console.IsOutputRedirected);
    }
}
