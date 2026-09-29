// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="Convert"/> 提供转换扩展方法
/// </summary>
public static class ConvertExtension
{
    #region 转换为long

    /// <summary>
    /// 将 String 类型 转换为 Long 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">异常默认值，默认为 0L</param>
    /// <returns>将 String 类型 转换为 Long 类型</returns>
    public static long ParseToLong(this string value, bool isThrow = true, long defaultValue = 0L)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return long.Parse(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (long.TryParse(value, out long result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    /// 将 Enum 类型 转换为 Long 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的枚举值为空</exception>
    /// <param name="value">枚举值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">异常默认值，默认为 0L</param>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>将 Enum 类型 转换为 Long 类型</returns>
    public static long ParseToLong<TEnum>(this TEnum value, bool isThrow = true, long defaultValue = 0L)
        where TEnum : struct, Enum
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的枚举值为空");
            }

            return Convert.ToInt64(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        return Convert.ToInt64(value);
    }

    /// <summary>
    /// 将 可空的 Enum 类型 转换为 Long 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的枚举值为空</exception>
    /// <param name="value">枚举值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">异常默认值，默认为 0L</param>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>将 可空的 Enum 类型 转换为 Long 类型</returns>
    public static long ParseToLong<TEnum>(this TEnum? value, bool isThrow = true, long defaultValue = 0L)
        where TEnum : struct, Enum
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的枚举值为空");
            }

            return Convert.ToInt64(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        return Convert.ToInt64(value);
    }

    #endregion

    #region 转换为int

    /// <summary>
    /// 将 String 类型 转换为 Int 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">操作无法产生结果时使用的默认值</param>
    /// <returns>将 String 类型 转换为 Int 类型</returns>
    public static int ParseToInt(this string value, bool isThrow = true, int defaultValue = 0)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return int.Parse(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    /// 将 Enum 类型 转换为 Int 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的枚举值为空</exception>
    /// <param name="value">枚举值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">操作无法产生结果时使用的默认值</param>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>将 Enum 类型 转换为 Int 类型</returns>
    public static int ParseToInt<TEnum>(this TEnum value, bool isThrow = true, int defaultValue = 0) where TEnum : struct, Enum
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的枚举值为空");
            }

            return Convert.ToInt32(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        return Convert.ToInt32(value);
    }

    /// <summary>
    /// 将 可空的 Enum 类型 转换为 Int 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的枚举值为空</exception>
    /// <param name="value">枚举值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">操作无法产生结果时使用的默认值</param>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>将 可空的 Enum 类型 转换为 Int 类型</returns>
    public static int ParseToInt<TEnum>(this TEnum? value, bool isThrow = true, int defaultValue = 0) where TEnum : struct, Enum
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的枚举值为空");
            }

            return Convert.ToInt32(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        return Convert.ToInt32(value);
    }

    #endregion

    #region 转换为short

    /// <summary>
    /// 将 String 类型 转换为 Short 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">short 异常默认值，默认为 0</param>
    /// <returns>将 String 类型 转换为 Short 类型</returns>
    public static short ParseToShort(this string value, bool isThrow = true, short defaultValue = 0)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return short.Parse(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (short.TryParse(value, out short result))
        {
            return result;
        }

        return defaultValue;
    }

    #endregion

    #region 转换为decimal

    /// <summary>
    /// 将 String 类型 转换为 Decimal 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">decimal 异常默认值，默认为 0M</param>
    /// <returns>将 String 类型 转换为 Decimal 类型</returns>
    public static decimal ParseToDecimal(this string value, bool isThrow = true, decimal defaultValue = 0M)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return decimal.Parse(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (decimal.TryParse(value, out decimal result))
        {
            return result;
        }

        return defaultValue;
    }

    #endregion

    #region 转化为bool

    /// <summary>
    /// 将 String 类型 转换为 Bool 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">异常默认值，默认为 <see langword="false"/></param>
    /// <returns>解析得到的布尔值；禁用异常且输入无效时返回 <paramref name="defaultValue"/></returns>
    public static bool ParseToBool(this string value, bool isThrow = true, bool defaultValue = false)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return bool.Parse(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        return defaultValue;
    }

    #endregion

    #region 转换为float

    /// <summary>
    /// 将 String 类型 转换为 Float 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">float 异常默认值，默认为 0F</param>
    /// <returns>将 String 类型 转换为 Float 类型</returns>
    public static float ParseToFloat(this string value, bool isThrow = true, float defaultValue = 0F)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return float.Parse(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (float.TryParse(value, out float result))
        {
            return result;
        }

        return defaultValue;
    }

    #endregion

    #region 转换为double

    /// <summary>
    /// 将 String 类型 转换为 Float 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">double 异常默认值，默认为 0D</param>
    /// <returns>将 String 类型 转换为 Float 类型</returns>
    public static double ParseToDouble(this string value, bool isThrow = true, double defaultValue = 0D)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return double.Parse(value);
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (double.TryParse(value, out double result))
        {
            return result;
        }

        return defaultValue;
    }

    #endregion

    #region 转换为Guid

    /// <summary>
    /// 将 String 类型 转换为 Guid 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">Guid 异常默认值，默认为 Guid.Empty</param>
    /// <returns>将 String 类型 转换为 Guid 类型</returns>
    public static Guid ParseToGuid(this string value, bool isThrow = true, Guid? defaultValue = null)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            return Guid.Parse(value);
        }

        if (value.IsEmpty())
        {
            if (defaultValue == null)
            {
                return Guid.Empty;
            }

            return defaultValue.Value;
        }

        if (Guid.TryParse(value, out Guid result))
        {
            return result;
        }

        if (defaultValue == null)
        {
            return Guid.Empty;
        }

        return defaultValue.Value;
    }

    #endregion

    #region 转换为DateTime

    /// <summary>
    /// 将 String 类型 转换为 DateTime 类型
    /// </summary>
    /// <exception cref="ArgumentNullException">传入的值为空或者空字符串</exception>
    /// <param name="value">要转换的值</param>
    /// <param name="isThrow">处理失败时是否抛出异常</param>
    /// <param name="defaultValue">DateTime 异常默认值，默认为 DateTime.MinValue</param>
    /// <returns>将 String 类型 转换为 DateTime 类型</returns>
    public static DateTime ParseToDateTime(this string value, bool isThrow = true, DateTime defaultValue = default)
    {
        if (isThrow)
        {
            if (value.IsEmpty())
            {
                throw new ArgumentNullException(nameof(value), "传入的值为空或者空字符串");
            }

            if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
            {
                return DateTime.Parse(value);
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
                case 8:
                    {
                        var result = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                        return result;
                    }
                case 10:
                    {
                        var result = DateTime.ParseExact(value, "yyyyMMddHH", CultureInfo.InvariantCulture, DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, 0, 0);
                        return result;
                    }
                case 12:
                    {
                        var result = DateTime.ParseExact(value, "yyyyMMddHHmm", CultureInfo.InvariantCulture,
                            DateTimeStyles.None);

                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, 0);
                        return result;
                    }
                default:
                    {
                        var result = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                            DateTimeStyles.None);

                        return result;
                    }
            }
        }

        if (value.IsEmpty())
        {
            return defaultValue;
        }

        if (value.Contains("-") || value.Contains("/") || value.Contains(":"))
        {
            if (DateTime.TryParse(value, out DateTime result))
            {
                return result;
            }
        }

        switch (value.Length)
        {
            case 4:
                {
                    if (DateTime.TryParseExact(value, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out DateTime result))
                    {
                        result = new DateTime(result.Year, 1, 1, 0, 0, 0);
                        return result;
                    }
                }

                break;
            case 6:
                {
                    if (DateTime.TryParseExact(value, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out DateTime result))
                    {
                        result = new DateTime(result.Year, result.Month, 1, 0, 0, 0);
                        return result;
                    }
                }

                break;
            case 8:
                {
                    if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out DateTime result))
                    {
                        result = new DateTime(result.Year, result.Month, result.Day, 0, 0, 0);
                        return result;
                    }
                }

                break;
            case 10:
                {
                    if (DateTime.TryParseExact(value, "yyyyMMddHH", CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out DateTime result))
                    {
                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, 0, 0);
                        return result;
                    }
                }

                break;
            case 12:
                {
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out DateTime result))
                    {
                        result = new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, 0);
                        return result;
                    }
                }

                break;
            default:
                {
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out DateTime result))
                    {
                        return result;
                    }
                }
                break;
        }

        return defaultValue;
    }

    /// <summary>
    /// 将 DateTimeOffset 转换成本地 DateTime
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>将 DateTimeOffset 转换成本地 DateTime</returns>
    public static DateTime ParseToDateTime(this DateTimeOffset dateTime)
    {
        if (dateTime.Offset.Equals(TimeSpan.Zero))
        {
            return dateTime.UtcDateTime;
        }

        if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
        {
            return dateTime.ToLocalTime().DateTime;
        }

        return dateTime.DateTime;
    }

    /// <summary>
    /// 将 DateTimeOffset? 转换成本地 DateTime
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>将 DateTimeOffset? 转换成本地 DateTime</returns>
    public static DateTime? ParseToDateTime(this DateTimeOffset? dateTime)
    {
        return dateTime?.ParseToDateTime();
    }

    /// <summary>
    /// 将 DateTime 转换成 DateTimeOffset
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>将 DateTime 转换成 DateTimeOffset</returns>
    public static DateTimeOffset ParseToDateTimeOffset(this DateTime dateTime)
    {
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
    }

    /// <summary>
    /// 将 DateTime? 转换成 DateTimeOffset
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>将 DateTime? 转换成 DateTimeOffset</returns>
    public static DateTimeOffset? ParseToDateTimeOffset(this DateTime? dateTime)
    {
        return dateTime?.ParseToDateTimeOffset();
    }

    /// <summary>
    /// 将毫秒时间戳转换为 DateTime，若转换失败，则返回日期最小值。不抛出异常
    /// </summary>
    /// <param name="timeStamps">时间戳集合</param>
    /// <returns>将毫秒时间戳转换为 DateTime，若转换失败，则返回日期最小值。不抛出异常</returns>
    public static DateTime ParseToDateTime_Milliseconds(this long timeStamps)
    {
        try
        {
            // 当地时区
            return timeStamps == 0 ? DateTime.MinValue : GlobalConstant.DefaultTime.AddMilliseconds(timeStamps);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// 将毫秒时间戳转换为 DateTime，若转换失败，则返回默认值
    /// </summary>
    /// <param name="timeStamps">时间戳集合</param>
    /// <param name="defaultValue">时间戳为 0 或转换失败时返回的默认值</param>
    /// <returns>将毫秒时间戳转换为 DateTime，若转换失败，则返回默认值</returns>
    public static DateTime ParseToDateTime_Milliseconds(this long timeStamps, DateTime? defaultValue)
    {
        try
        {
            // 当地时区
            return timeStamps == 0 ? defaultValue.GetValueOrDefault() : GlobalConstant.DefaultTime.AddMilliseconds(timeStamps);
        }
        catch
        {
            return defaultValue.GetValueOrDefault();
        }
    }

    /// <summary>
    /// 将秒时间戳转换为 DateTime，若转换失败，则返回日期最小值。不抛出异常
    /// </summary>
    /// <param name="timeStamps">时间戳集合</param>
    /// <returns>将秒时间戳转换为 DateTime，若转换失败，则返回日期最小值。不抛出异常</returns>
    public static DateTime ParseToDateTime_Seconds(this long timeStamps)
    {
        try
        {
            // 当地时区
            return timeStamps == 0 ? DateTime.MinValue : GlobalConstant.DefaultTime.AddSeconds(timeStamps);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// 将秒时间戳转换为 DateTime，若转换失败，则返回默认值
    /// </summary>
    /// <param name="timeStamps">时间戳集合</param>
    /// <param name="defaultValue">时间戳为 0 或转换失败时返回的默认值</param>
    /// <returns>将秒时间戳转换为 DateTime，若转换失败，则返回默认值</returns>
    public static DateTime ParseToDateTime_Seconds(this long timeStamps, DateTime? defaultValue)
    {
        try
        {
            // 当地时区
            return timeStamps == 0 ? defaultValue.GetValueOrDefault() : GlobalConstant.DefaultTime.AddSeconds(timeStamps);
        }
        catch
        {
            return defaultValue.GetValueOrDefault();
        }
    }

    #endregion

    #region 转换为ToUnixTime

    /// <summary>
    /// 将 DateTime 转为 UnixTime
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>将 DateTime 转为 UnixTime</returns>
    public static long ParseToUnixTime(this DateTime dateTime)
    {
        var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return (long)Math.Round((dateTime - startTime).TotalMilliseconds, MidpointRounding.AwayFromZero);
    }

    #endregion

    #region 强制转换类型

    /// <summary>
    /// 强制转换类型
    /// </summary>
    /// <param name="source">源对象</param>
    /// <typeparam name="TResult">操作结果类型</typeparam>
    /// <returns>强制转换类型集合</returns>
    public static IEnumerable<TResult> CastSuper<TResult>(this IEnumerable source)
    {
        return
            from object item in source
            select (TResult)Convert.ChangeType(item, typeof(TResult));
    }

    #endregion
}
