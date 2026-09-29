// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text.Json;

namespace Fast.Logging;

/// <summary>
/// 日志格式化静态类
/// </summary>
[SuppressSniffer]
public static class LoggerFormatter
{
    /// <summary>
    /// JSON 输出格式化
    /// </summary>
    public static readonly Func<LogMessage, string> Json = logMsg =>
    {
        return logMsg.Write(writer => WriteJson(logMsg, writer));
    };

    /// <summary>
    /// JSON 输出格式化
    /// </summary>
    public static readonly Func<LogMessage, string> JsonIndented = logMsg =>
    {
        return logMsg.Write(writer => WriteJson(logMsg, writer), true);
    };

    /// <summary>
    /// 写入 JSON
    /// </summary>
    /// <param name="logMsg">要格式化或输出的日志消息</param>
    /// <param name="writer">目标 JSON 写入器</param>
    private static void WriteJson(LogMessage logMsg, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        // 输出日志级别
        writer.WriteString("logLevel", logMsg.LogLevel.ToString());

        // 输出日志时间
        writer.WriteString("logDateTime", logMsg.LogDateTime.ToString("o"));

        // 输出日志类别
        writer.WriteString("logName", logMsg.LogName);

        // 输出日志事件Id
        if (logMsg.EventId != null)
        {
            writer.WriteNumber("eventId", logMsg.EventId.Value.Id);
        }

        // 输出日志消息
        writer.WriteString("message", logMsg.Message);

        // 输出日志所在线程Id
        writer.WriteNumber("threadId", logMsg.ThreadId);

        // 输出是否使用 UTC 时间戳
        writer.WriteBoolean("useUtcTimestamp", logMsg.UseUtcTimestamp);

        // 输出请求的 TraceId
        writer.WriteString("traceId", logMsg.TraceId);

        // 输出异常信息
        writer.WritePropertyName("exception");
        if (logMsg.Exception == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(logMsg.Exception.ToString());
        }

        writer.WriteEndObject();
    }
}
