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
/// decimal 类型 JSON 返回处理
/// </summary>
internal sealed class DecimalJsonConverter : JsonConverter<decimal>
{
    /// <summary>
    /// 小数点位数
    /// </summary>
    public int? Places { get; set; }

    public DecimalJsonConverter()
    {
        Places = null;
    }

    public DecimalJsonConverter(int places)
    {
        Places = places;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
    {
        writer.WriteValue(Places == null ? value : Math.Round(value, Places.Value));
    }

    /// <inheritdoc />
    public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        // 同时接受 JSON 字符串和数字令牌
        if (reader.TokenType == JsonToken.String)
        {
            var jToken = JToken.ReadFrom(reader);
            string value = jToken.Value<string>();
            return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        return Convert.ToDecimal(reader.Value);
    }
}

/// <summary>
/// decimal? 类型 JSON 返回处理
/// </summary>
internal sealed class NullableDecimalJsonConverter : JsonConverter<decimal?>
{
    /// <summary>
    /// 小数点位数
    /// </summary>
    public int? Places { get; set; }

    public NullableDecimalJsonConverter()
    {
        Places = null;
    }

    public NullableDecimalJsonConverter(int places)
    {
        Places = places;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, decimal? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(Places == null ? value.Value : Math.Round(value.Value, Places.Value));
        }
    }

    /// <inheritdoc />
    public override decimal? ReadJson(JsonReader reader, Type objectType, decimal? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        // 同时接受 JSON 字符串和数字令牌；空字符串按 null 处理
        if (reader.TokenType != JsonToken.String)
        {
            return Convert.ToDecimal(reader.Value);
        }

        var jToken = JToken.ReadFrom(reader);
        string value = jToken.Value<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
