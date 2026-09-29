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
/// int 类型 JSON 返回处理
/// </summary>
internal sealed class IntJsonConverter : JsonConverter<int>
{
    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 同时接受 JSON 字符串和数字令牌
        return reader.TokenType == JsonTokenType.String
            ? int.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture)
            : reader.GetInt32();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

/// <summary>
/// int? 类型 JSON 返回处理
/// </summary>
internal sealed class NullableIntJsonConverter : JsonConverter<int?>
{
    /// <inheritdoc />
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 同时接受 JSON 字符串和数字令牌；空字符串按 null 处理
        if (reader.TokenType != JsonTokenType.String)
        {
            return reader.GetInt32();
        }

        string intString = reader.GetString();
        if (string.IsNullOrWhiteSpace(intString))
        {
            return null;
        }

        return int.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
