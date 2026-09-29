// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fast.Serialization;

/// <summary>
/// Long 类型 JSON 返回处理
/// </summary>
internal sealed class LongJsonConverter : JsonConverter<long>
{
    /// <inheritdoc />
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 同时接受 JSON 字符串和数字令牌
        return reader.TokenType == JsonTokenType.String
            ? long.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture)
            : reader.GetInt64();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value}");
    }
}

/// <summary>
/// Long? 类型 JSON 返回处理
/// </summary>
internal sealed class NullableLongJsonConverter : JsonConverter<long?>
{
    /// <inheritdoc />
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 同时接受 JSON 字符串和数字令牌；空字符串按 null 处理
        if (reader.TokenType != JsonTokenType.String)
        {
            return reader.GetInt64();
        }

        string longString = reader.GetString();
        if (string.IsNullOrWhiteSpace(longString))
        {
            return null;
        }

        return long.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue($"{value}");
        }
    }
}
