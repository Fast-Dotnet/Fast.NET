// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Fast.Logging;

/// <summary>
/// <see cref="LogMessage"/> 扩展
/// </summary>
[SuppressSniffer]
public static class LogMessageExtension
{
    /// <summary>
    /// 高性能创建 JSON 对象字符串
    /// </summary>
    /// <param name="_">扩展方法接收者；该值不参与输出</param>
    /// <param name="writeAction">向 JSON 写入器填充属性的委托</param>
    /// <param name="writeIndented">是否格式化输出 JSON</param>
    /// <returns>高性能创建 JSON 对象字符串</returns>
    public static string Write(this LogMessage _, Action<Utf8JsonWriter> writeAction, bool writeIndented = false)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            // 解决中文乱码问题
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = writeIndented
        });

        writeAction?.Invoke(writer);

        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// 高性能创建 JSON 数组字符串
    /// </summary>
    /// <param name="logMsg">要格式化或输出的日志消息</param>
    /// <param name="writeAction">向 JSON 写入器填充属性的委托</param>
    /// <param name="writeIndented">是否格式化输出 JSON</param>
    /// <returns>高性能创建 JSON 数组字符串</returns>
    public static string WriteArray(this LogMessage logMsg, Action<Utf8JsonWriter> writeAction, bool writeIndented = false)
    {
        return logMsg.Write(writer =>
        {
            writer.WriteStartArray();

            writeAction?.Invoke(writer);

            writer.WriteEndArray();
        }, writeIndented);
    }
}
