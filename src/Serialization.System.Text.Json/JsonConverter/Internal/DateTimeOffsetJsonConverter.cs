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
/// DateTimeOffset 类型 JSON 返回处理
/// </summary>
internal sealed class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// 格式化 默认：yyyy-MM-dd HH:mm:ss
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// 是否将输出时间转换为本地时区
    /// </summary>
    public bool Localized { get; private set; }

    public DateTimeOffsetJsonConverter()
    {
        Format = "yyyy-MM-dd HH:mm:ss";
    }

    public DateTimeOffsetJsonConverter(string format)
    {
        Format = format;
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="outputToLocalDateTime">是否将输出时间转换为本地时区</param>
    public DateTimeOffsetJsonConverter(string format, bool outputToLocalDateTime)
    {
        Format = format;
        Localized = outputToLocalDateTime;
    }

    /// <inheritdoc />
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString()!;

        DateTime result;

        if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
        {
            result = DateTime.Parse(value, CultureInfo.InvariantCulture);
        }
        else
        {
            switch (value.Length)
            {
                case 4:
                    {
                        result = DateTime.ParseExact(value, "yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, 1, 1, 0, 0, 0);
                    }
                    break;
                case 6:
                    {
                        result = DateTime.ParseExact(value, "yyyyMM", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, 1, 0, 0, 0);
                    }
                    break;
                case 8:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                    }
                    break;
                case 10:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMddHH", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, 0, 0);
                    }
                    break;
                case 12:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMddHHmm", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, 0);
                    }
                    break;
                default:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.CurrentCulture, DateTimeStyles.None);
                    }
                    break;
            }
        }

        return DateTime.SpecifyKind(result, Localized ? DateTimeKind.Local : DateTimeKind.Utc);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // 判断是否序列化成当地时间
        DateTimeOffset formatDateTime = value;
        if (Localized)
        {
            if (value.Offset.Equals(TimeSpan.Zero))
            {
                formatDateTime = value.UtcDateTime;
            }
            else if (value.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(value.Date)))
            {
                formatDateTime = value.ToLocalTime().DateTime;
            }
            else
            {
                formatDateTime = value.DateTime;
            }
        }

        writer.WriteStringValue(formatDateTime.ToString(Format, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// DateTimeOffset? 类型 JSON 返回处理
/// </summary>
internal sealed class NullableDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
{
    /// <summary>
    /// 格式化 默认：yyyy-MM-dd HH:mm:ss
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// 是否将输出时间转换为本地时区
    /// </summary>
    public bool Localized { get; private set; }

    public NullableDateTimeOffsetJsonConverter()
    {
        Format = "yyyy-MM-dd HH:mm:ss";
    }

    public NullableDateTimeOffsetJsonConverter(string format)
    {
        Format = format;
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="outputToLocalDateTime">是否将输出时间转换为本地时区</param>
    public NullableDateTimeOffsetJsonConverter(string format, bool outputToLocalDateTime)
    {
        Format = format;
        Localized = outputToLocalDateTime;
    }

    /// <inheritdoc />
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString()!;

        DateTime result;

        if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
        {
            result = DateTime.Parse(value, CultureInfo.InvariantCulture);
        }
        else
        {
            switch (value.Length)
            {
                case 4:
                    {
                        result = DateTime.ParseExact(value, "yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, 1, 1, 0, 0, 0);
                    }
                    break;
                case 6:
                    {
                        result = DateTime.ParseExact(value, "yyyyMM", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, 1, 0, 0, 0);
                    }
                    break;
                case 8:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                    }
                    break;
                case 10:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMddHH", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, 0, 0);
                    }
                    break;
                case 12:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMddHHmm", CultureInfo.CurrentCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, 0);
                    }
                    break;
                default:
                    {
                        result = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.CurrentCulture, DateTimeStyles.None);
                    }
                    break;
            }
        }

        return DateTime.SpecifyKind(result, Localized ? DateTimeKind.Local : DateTimeKind.Utc);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            // 判断是否序列化成当地时间
            DateTimeOffset formatDateTime = value.Value;
            if (Localized)
            {
                if (value.Value.Offset.Equals(TimeSpan.Zero))
                {
                    formatDateTime = value.Value.UtcDateTime;
                }
                else if (value.Value.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(value.Value.Date)))
                {
                    formatDateTime = value.Value.ToLocalTime().DateTime;
                }
                else
                {
                    formatDateTime = value.Value.DateTime;
                }
            }

            writer.WriteStringValue(formatDateTime.ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}
