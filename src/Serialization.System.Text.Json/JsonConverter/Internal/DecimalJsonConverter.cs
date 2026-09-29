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
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 同时接受 JSON 字符串和数字令牌
        return reader.TokenType == JsonTokenType.String
            ? decimal.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture)
            : reader.GetDecimal();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Places == null ? value : Math.Round(value, Places.Value));
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
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 同时接受 JSON 字符串和数字令牌；空字符串按 null 处理
        if (reader.TokenType != JsonTokenType.String)
        {
            return reader.GetDecimal();
        }

        string decimalString = reader.GetString();
        if (string.IsNullOrWhiteSpace(decimalString))
        {
            return null;
        }

        return decimal.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(Places == null ? value.Value : Math.Round(value.Value, Places.Value));
        }
    }
}
