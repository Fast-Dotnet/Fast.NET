// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fast.Serialization;

/// <summary>
/// Long 类型 JSON 返回处理
/// </summary>
internal sealed class LongJsonConverter : JsonConverter<long>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, long value, JsonSerializer serializer)
    {
        writer.WriteValue($"{value}");
    }

    /// <inheritdoc />
    public override long ReadJson(JsonReader reader, Type objectType, long existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        // 同时接受 JSON 字符串和数字令牌
        if (reader.TokenType == JsonToken.String)
        {
            var jToken = JToken.ReadFrom(reader);
            string value = jToken.Value<string>();
            return long.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        return Convert.ToInt64(reader.Value);
    }
}

/// <summary>
/// Long? 类型 JSON 返回处理
/// </summary>
internal sealed class NullableLongJsonConverter : JsonConverter<long?>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue($"{value}");
        }
    }

    /// <inheritdoc />
    public override long? ReadJson(JsonReader reader, Type objectType, long? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        // 同时接受 JSON 字符串和数字令牌；空字符串按 null 处理
        if (reader.TokenType != JsonToken.String)
        {
            return Convert.ToInt64(reader.Value);
        }

        var jToken = JToken.ReadFrom(reader);
        string value = jToken.Value<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return long.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
