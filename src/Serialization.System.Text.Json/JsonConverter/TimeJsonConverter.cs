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
/// DateTime 类型 JSON 返回时间处理
/// </summary>
public class TimeJsonConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// 日期格式化 <para>默认：HH:mm:ss</para>
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// DateTime 类型 JSON 返回时间处理
    /// </summary>
    public TimeJsonConverter()
    {
        Format = "HH:mm:ss";
    }

    /// <summary>
    /// DateTime 类型 JSON 返回时间处理
    /// </summary>
    /// <param name="format">日期格式化</param>
    public TimeJsonConverter(string format)
    {
        Format = format;
    }

    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString()!;

        if (value.Contains('-') || value.Contains('/') || value.Contains(':'))
        {
            var result = DateTime.Parse(value, CultureInfo.InvariantCulture);

            result = new DateTime(1970, 1, 1, result.Hour, result.Minute, result.Second);
            return result;
        }

        switch (value.Length)
        {
            case 2:
                {
                    var result = DateTime.ParseExact(value, "HH", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(1970, 1, 1, result.Hour, 0, 0);
                    return result;
                }
            case 4:
                {
                    var result = DateTime.ParseExact(value, "HHmm", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(1970, 1, 1, result.Hour, result.Minute, 0);
                    return result;
                }
            default:
                {
                    var result = DateTime.ParseExact(value, "HHmmss", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(1970, 1, 1, result.Hour, result.Minute, result.Second);
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
/// DateTime? 类型 JSON 返回时间处理
/// </summary>
public class NullableTimeJsonConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// 日期格式化 <para>默认：HH:mm:ss</para>
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// DateTime? 类型 JSON 返回时间处理
    /// </summary>
    public NullableTimeJsonConverter()
    {
        Format = "HH:mm:ss";
    }

    /// <summary>
    /// DateTime? 类型 JSON 返回时间处理
    /// </summary>
    /// <param name="format">日期格式化</param>
    public NullableTimeJsonConverter(string format)
    {
        Format = format;
    }

    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Contains('-') || value.Contains('/') || value.Contains(':'))
        {
            var result = DateTime.Parse(value, CultureInfo.InvariantCulture);

            result = new DateTime(1970, 1, 1, result.Hour, result.Minute, result.Second);
            return result;
        }

        switch (value.Length)
        {
            case 2:
                {
                    var result = DateTime.ParseExact(value, "HH", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(1970, 1, 1, result.Hour, 0, 0);
                    return result;
                }
            case 4:
                {
                    var result = DateTime.ParseExact(value, "HHmm", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(1970, 1, 1, result.Hour, result.Minute, 0);
                    return result;
                }
            default:
                {
                    var result = DateTime.ParseExact(value, "HHmmss", CultureInfo.CurrentCulture, DateTimeStyles.None);

                    result = new DateTime(1970, 1, 1, result.Hour, result.Minute, result.Second);
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
