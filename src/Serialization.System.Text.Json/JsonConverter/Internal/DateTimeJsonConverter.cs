// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fast.Serialization;

/// <summary>
/// DateTime 类型 JSON 返回处理
/// </summary>
internal sealed class DateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// 格式化 默认：yyyy-MM-dd HH:mm:ss
    /// </summary>
    public string Format { get; set; }

    public DateTimeJsonConverter()
    {
        Format = "yyyy-MM-dd HH:mm:ss";
    }

    public DateTimeJsonConverter(string format)
    {
        Format = format;
    }

    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString()!;

        if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
        {
            var result = DateTime.Parse(value, CultureInfo.InvariantCulture);

            return result;
        }

        switch (value.Length)
        {
            case 4:
                {
                    var result = DateTime.ParseExact(value, "yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, 1, 1, 0, 0, 0);
                    return result;
                }
            case 6:
                {
                    var result = DateTime.ParseExact(value, "yyyyMM", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, 1, 0, 0, 0);
                    return result;
                }
            case 8:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                    return result;
                }
            case 10:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMddHH", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, result.Hour, 0, 0);
                    return result;
                }
            case 12:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMddHHmm", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, 0);
                    return result;
                }
            default:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    return result;
                }
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// DateTime? 类型 JSON 返回处理
/// </summary>
internal sealed class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// 格式化 默认：yyyy-MM-dd HH:mm:ss
    /// </summary>
    public string Format { get; set; }

    public NullableDateTimeJsonConverter()
    {
        Format = "yyyy-MM-dd HH:mm:ss";
    }

    public NullableDateTimeJsonConverter(string format)
    {
        Format = format;
    }

    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString()!;

        if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
        {
            var result = DateTime.Parse(value, CultureInfo.InvariantCulture);

            return result;
        }

        switch (value.Length)
        {
            case 4:
                {
                    var result = DateTime.ParseExact(value, "yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, 1, 1, 0, 0, 0);
                    return result;
                }
            case 6:
                {
                    var result = DateTime.ParseExact(value, "yyyyMM", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, 1, 0, 0, 0);
                    return result;
                }
            case 8:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                    return result;
                }
            case 10:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMddHH", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, result.Hour, 0, 0);
                    return result;
                }
            case 12:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMddHHmm", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, 0);
                    return result;
                }
            default:
                {
                    var result = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    return result;
                }
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}
