// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Fast.Logging;

/// <summary>
/// 日志上下文
/// </summary>
[SuppressSniffer]
public static class LoggingContext
{
    /// <summary>
    /// 异常分隔符
    /// </summary>
    private const string EXCEPTION_SEPARATOR = "++++++++++++++++++++++++++++++++++++++++++++++++++++++++";

    /// <summary>
    /// 输出标准日志消息
    /// </summary>
    /// <param name="logMsg">要格式化或输出的日志消息</param>
    /// <param name="dateFormat">日期时间格式字符串</param>
    /// <param name="isConsole">日志是否写入控制台</param>
    /// <param name="disableColors">是否禁用控制台日志颜色</param>
    /// <param name="withTraceId">with Trace 的唯一标识</param>
    /// <param name="withStackFrame">是否在日志中包含调用堆栈位置</param>
    /// <returns>输出标准日志消息</returns>
    public static string OutputStandardMessage(LogMessage logMsg, string dateFormat = "yyyy-MM-dd HH:mm:ss.fffffff zzz dddd",
        bool isConsole = false, bool disableColors = true, bool withTraceId = false, bool withStackFrame = false)
    {
        if (logMsg.Message is null)
        {
            return null;
        }

        // 创建默认日志格式化模板
        var formatString = new StringBuilder();

        // 获取日志级别对应控制台的颜色
        bool disableConsoleColor = !isConsole || disableColors;
        ConsoleColors logLevelColors = GetLogLevelConsoleColors(logMsg.LogLevel, disableConsoleColor);
        ConsoleColors logLevelMessageColors = GetLogLevelMessageConsoleColors(logMsg.LogLevel, disableConsoleColor);

        _ = AppendWithColor(formatString, GetLogLevelString(logMsg.LogLevel), logLevelColors);
        formatString.Append(": ");
        formatString.Append(logMsg.LogDateTime.ToString(dateFormat));
        formatString.Append(' ');
        formatString.Append(logMsg.UseUtcTimestamp ? "U" : "L");
        formatString.Append(' ');
        _ = AppendWithColor(formatString, logMsg.LogName,
            disableConsoleColor ? new ConsoleColors(null, null) : new ConsoleColors(ConsoleColor.Cyan, ConsoleColor.DarkCyan));

        if (logMsg.EventId != null)
        {
            formatString.Append('[');
            formatString.Append(logMsg.EventId?.Id);
            formatString.Append(']');
        }

        formatString.Append(' ');
        formatString.Append($"#{logMsg.ThreadId}");
        if (withTraceId && !string.IsNullOrWhiteSpace(logMsg.TraceId))
        {
            formatString.Append(' ');
            _ = AppendWithColor(formatString, $"'{logMsg.TraceId}'",
                disableConsoleColor ? new ConsoleColors(null, null) : new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black));
        }

        formatString.AppendLine();

        // 输出日志输出所在方法，类型，程序集
        if (withStackFrame)
        {
            var stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();
            int pos = isConsole ? 6 : 5;
            if (stackFrames.Length > pos)
            {
                MethodBase targetMethod = stackFrames.Where((_, i) => i == pos).First().GetMethod();
                Type declaringType = targetMethod?.DeclaringType;
                Assembly targetAssembly = declaringType?.Assembly;

                formatString.Append(PadLeftAlign($"[{targetAssembly?.GetName().Name}.dll] {targetMethod}"));
                formatString.AppendLine();
            }
        }

        // 消息颜色和前缀对齐在同一步完成，避免 ANSI 控制符影响缩进计算
        _ = AppendWithColor(formatString, PadLeftAlign(logMsg.Message),
            disableConsoleColor ? new ConsoleColors(null, null) : logLevelMessageColors);

        // 如果包含异常信息，则创建新一行写入
        if (logMsg.Exception != null)
        {
            string EXCEPTION_SEPARATOR_WITH_COLOR = AppendWithColor(null, EXCEPTION_SEPARATOR, logLevelMessageColors).ToString();
            string exceptionMessage =
                $"{Environment.NewLine}{EXCEPTION_SEPARATOR_WITH_COLOR}{Environment.NewLine}{AppendWithColor(null, logMsg.Exception.ToString(), logLevelMessageColors)}{Environment.NewLine}{EXCEPTION_SEPARATOR_WITH_COLOR}";

            formatString.Append(PadLeftAlign(exceptionMessage));
        }

        // 返回日志消息模板
        return formatString.ToString();
    }

    /// <summary>
    /// 将日志内容进行对齐
    /// </summary>
    /// <param name="message">要记录或返回的消息</param>
    /// <returns>将日志内容进行对齐</returns>
    private static string PadLeftAlign(string message)
    {
        string newMessage = string.Join(Environment.NewLine,
            message.Split(new[] {Environment.NewLine, "\n"}, StringSplitOptions.None)
                .Select(line => string.Empty.PadLeft(6, ' ') + line));

        return newMessage;
    }

    /// <summary>
    /// 获取日志级别短名称
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <returns>获取到的日志级别短名称</returns>
    public static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    /// <summary>
    /// 扩展 StringBuilder 增加带颜色写入
    /// </summary>
    /// <param name="formatString">要写入的复合格式字符串</param>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="colors">写入文本时使用的前景色和背景色</param>
    /// <returns>扩展 StringBuilder 增加带颜色写入</returns>
    private static StringBuilder AppendWithColor(StringBuilder formatString, string message, ConsoleColors colors)
    {
        formatString ??= new StringBuilder();

        if (colors.Background.HasValue)
        {
            formatString.Append(GetBackgroundColorEscapeCode(colors.Background.Value));
        }

        if (colors.Foreground.HasValue)
        {
            formatString.Append(GetForegroundColorEscapeCode(colors.Foreground.Value));
        }

        formatString.Append(message);

        if (colors.Foreground.HasValue)
        {
            formatString.Append("\u001b[39m\u001b[22m");
        }

        if (colors.Background.HasValue)
        {
            formatString.Append("\u001b[49m");
        }

        return formatString;
    }

    /// <summary>
    /// 输出控制台字体颜色 UniCode 码
    /// </summary>
    /// <param name="color">要转换为 ANSI 转义序列的控制台颜色</param>
    /// <returns>输出控制台字体颜色 UniCode 码</returns>
    private static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\u001b[30m",
            ConsoleColor.DarkRed => "\u001b[31m",
            ConsoleColor.DarkGreen => "\u001b[32m",
            ConsoleColor.DarkYellow => "\u001b[33m",
            ConsoleColor.DarkBlue => "\u001b[34m",
            ConsoleColor.DarkMagenta => "\u001b[35m",
            ConsoleColor.DarkCyan => "\u001b[36m",
            ConsoleColor.Gray => "\u001b[37m",
            ConsoleColor.Red => "\u001b[1m\u001b[31m",
            ConsoleColor.Green => "\u001b[1m\u001b[32m",
            ConsoleColor.Yellow => "\u001b[1m\u001b[33m",
            ConsoleColor.Blue => "\u001b[1m\u001b[34m",
            ConsoleColor.Magenta => "\u001b[1m\u001b[35m",
            ConsoleColor.Cyan => "\u001b[1m\u001b[36m",
            ConsoleColor.White => "\u001b[1m\u001b[37m",
            ConsoleColor.DarkGray => "\u001b[90m",
            _ => "\u001b[39m\u001b[22m"
        };
    }

    /// <summary>
    /// 输出控制台背景颜色 UniCode 码
    /// </summary>
    /// <param name="color">要转换为 ANSI 转义序列的控制台颜色</param>
    /// <returns>输出控制台背景颜色 UniCode 码</returns>
    private static string GetBackgroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\u001b[40m",
            ConsoleColor.Red => "\u001b[41m",
            ConsoleColor.Green => "\u001b[42m",
            ConsoleColor.Yellow => "\u001b[43m",
            ConsoleColor.Blue => "\u001b[44m",
            ConsoleColor.Magenta => "\u001b[45m",
            ConsoleColor.Cyan => "\u001b[46m",
            ConsoleColor.White => "\u001b[47m",
            _ => "\u001b[49m"
        };
    }

    /// <summary>
    /// 获取控制台日志级别对应的颜色
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <param name="disableColors">是否禁用控制台日志颜色</param>
    /// <returns>获取到的控制台日志级别对应的颜色</returns>
    private static ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel, bool disableColors = false)
    {
        if (disableColors)
        {
            return new ConsoleColors(null, null);
        }

        return logLevel switch
        {
            LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.Red),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.Red),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevel.Debug => new ConsoleColors(ConsoleColor.DarkGray, ConsoleColor.Black),
            LogLevel.Trace => new ConsoleColors(ConsoleColor.DarkGray, ConsoleColor.Black),
            _ => new ConsoleColors(null, null)
        };
    }

    /// <summary>
    /// 获取控制台日志级别消息对应的颜色
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <param name="disableColors">是否禁用控制台日志颜色</param>
    /// <returns>获取到的控制台日志级别消息对应的颜色</returns>
    private static ConsoleColors GetLogLevelMessageConsoleColors(LogLevel logLevel, bool disableColors = false)
    {
        if (disableColors)
        {
            return new ConsoleColors(null, null);
        }

        return logLevel switch
        {
            LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.Red),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.Red),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Debug => new ConsoleColors(ConsoleColor.DarkGray, ConsoleColor.Black),
            LogLevel.Trace => new ConsoleColors(ConsoleColor.DarkGray, ConsoleColor.Black),
            _ => new ConsoleColors(null, null)
        };
    }
}
