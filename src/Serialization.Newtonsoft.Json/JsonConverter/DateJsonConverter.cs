// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fast.Serialization;

/// <summary>
/// DateTime 类型 JSON 返回日期处理
/// </summary>
public class DateJsonConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// 日期格式化 <para>默认：yyyy-MM-dd</para>
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// DateTime 类型 JSON 返回日期处理
    /// </summary>
    public DateJsonConverter()
    {
        Format = "yyyy-MM-dd";
    }

    /// <summary>
    /// DateTime 类型 JSON 返回日期处理
    /// </summary>
    /// <param name="format">日期格式化 默认：yyyy-MM-dd</param>
    public DateJsonConverter(string format)
    {
        Format = format;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var jToken = JToken.ReadFrom(reader);
        string value = jToken.Value<string>();

        if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
        {
            var result = DateTime.Parse(value, CultureInfo.InvariantCulture);

            return result.Date;
        }

        switch (value.Length)
        {
            case 4:
                {
                    var result = DateTime.ParseExact(value, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, 1, 1, 0, 0, 0);
                    return result;
                }
            case 6:
                {
                    var result = DateTime.ParseExact(value, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, 1, 0, 0, 0);
                    return result;
                }
            default:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                    return result;
                }
        }
    }
}

/// <summary>
/// DateTime? 类型 JSON 返回日期处理
/// </summary>
public class NullableDateJsonConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// 日期格式化 <para>默认：yyyy-MM-dd</para>
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// DateTime? 类型 JSON 返回日期处理
    /// </summary>
    public NullableDateJsonConverter()
    {
        Format = "yyyy-MM-dd";
    }

    /// <summary>
    /// DateTime? 类型 JSON 返回日期处理
    /// </summary>
    /// <param name="format">日期格式化 默认：yyyy-MM-dd</param>
    public NullableDateJsonConverter(string format)
    {
        Format = format;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, DateTime? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc />
    public override DateTime? ReadJson(JsonReader reader, Type objectType, DateTime? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var jToken = JToken.ReadFrom(reader);
        string value = jToken.Value<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
        {
            var result = DateTime.Parse(value, CultureInfo.InvariantCulture);

            return result.Date;
        }

        switch (value.Length)
        {
            case 4:
                {
                    var result = DateTime.ParseExact(value, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, 1, 1, 0, 0, 0);
                    return result;
                }
            case 6:
                {
                    var result = DateTime.ParseExact(value, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, 1, 0, 0, 0);
                    return result;
                }
            default:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                    return result;
                }
        }
    }
}
