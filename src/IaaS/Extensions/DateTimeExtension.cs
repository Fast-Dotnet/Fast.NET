// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Globalization;
using System.Linq;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="DateTime"/> 提供扩展方法
/// </summary>
public static class DateTimeExtension
{
    /// <summary>
    /// 得到问好
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>得到问好</returns>
    public static string GetSayHello(this DateTime dateTime)
    {
        int hour = dateTime.Hour;
        if (hour < 6)
        {
            return "凌晨好！";
        }

        if (hour < 9)
        {
            return "早上好！";
        }

        if (hour < 12)
        {
            return "上午好！";
        }

        if (hour < 14)
        {
            return "中午好！";
        }

        if (hour < 17)
        {
            return "下午好！";
        }

        if (hour < 19)
        {
            return "傍晚好！";
        }

        if (hour < 22)
        {
            return "晚上好！";
        }

        return "夜里好！";
    }

    /// <summary>
    /// 获取当前月的第一天
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>获取到的当前月的第一天</returns>
    public static DateTime GetCurMonthFirstDay(this DateTime dateTime)
    {
        return DateTimeUtil.GetYearMonthFirstDay(dateTime.Year, dateTime.Month);
    }

    /// <summary>
    /// 获取当前月的最后一天
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>获取到的当前月的最后一天</returns>
    public static DateTime GetCurMonthLastDay(this DateTime dateTime)
    {
        return DateTimeUtil.GetYearMonthLastDay(dateTime.Year, dateTime.Month);
    }

    /// <summary>
    /// 获取上月的第一天
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>获取到的上月的第一天</returns>
    public static DateTime GetUpMonthFirstDay(this DateTime dateTime)
    {
        DateTime nowDate = dateTime.AddMonths(-1);
        return new DateTime(nowDate.Year, nowDate.Month, 01, 00, 00, 00); // 该方法可以指定，年、月、日
    }

    /// <summary>
    /// 获取上月的最后一天
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>获取到的上月的最后一天</returns>
    public static DateTime GetUpMonthLastDay(this DateTime dateTime)
    {
        var internalDate = new DateTime(dateTime.Year, dateTime.Month, 01, 23, 59, 59);
        return internalDate.AddMonths(-1);
    }

    /// <summary>
    /// 获取本周时间
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>时间范围的起始时间和结束时间</returns>
    public static (DateTime startTime, DateTime lastTime) GetCurWeekDay(this DateTime dateTime)
    {
        DateTime startTime = dateTime.AddDays(0 - Convert.ToInt16(dateTime.DayOfWeek) + 1);
        DateTime lastTime = dateTime.AddDays(6 - Convert.ToInt16(dateTime.DayOfWeek) + 1);
        return (new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0),
            new DateTime(lastTime.Year, lastTime.Month, lastTime.Day, 23, 59, 59));
    }

    /// <summary>
    /// 获取上周时间
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>时间范围的起始时间和结束时间</returns>
    public static (DateTime startTime, DateTime lastTime) GetUpWeekDay(this DateTime dateTime)
    {
        DateTime startTime = dateTime.AddDays(0 - Convert.ToInt16(dateTime.DayOfWeek) - 6);
        DateTime lastTime = dateTime.AddDays(6 - Convert.ToInt16(dateTime.DayOfWeek) - 6);
        return (new DateTime(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0),
            new DateTime(lastTime.Year, lastTime.Month, lastTime.Day, 23, 59, 59));
    }

    /// <summary>
    /// 获取当天时间
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>时间范围的起始时间和结束时间</returns>
    public static (DateTime startTime, DateTime lastTime) GetCurDay(this DateTime dateTime)
    {
        return (new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0),
            new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59));
    }

    /// <summary>
    /// 获取昨天时间
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>时间范围的起始时间和结束时间</returns>
    public static (DateTime startTime, DateTime lastTime) GetUpDay(this DateTime dateTime)
    {
        DateTime internalDate = dateTime.AddDays(-1);
        return (new DateTime(internalDate.Year, internalDate.Month, internalDate.Day, 0, 0, 0),
            new DateTime(internalDate.Year, internalDate.Month, internalDate.Day, 23, 59, 59));
    }

    /// <summary>
    /// 获取生肖
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>获取到的生肖</returns>
    public static string GetZodiac(this DateTime dateTime)
    {
        if (dateTime < new DateTime(1900, 1, 1))
        {
            return "";
        }

        var calendar = new ChineseLunisolarCalendar();

        const string zodiac = "鼠牛虎兔龙蛇马羊猴鸡狗猪";

        int year = calendar.GetSexagenaryYear(dateTime);

        return zodiac.Substring(calendar.GetTerrestrialBranch(year) - 1, 1);
    }

    /// <summary>
    /// 获取星座
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>获取到的星座</returns>
    public static string GetConstellation(this DateTime dateTime)
    {
        if (dateTime < new DateTime(1900, 1, 1))
        {
            return "";
        }

        string monthDay = dateTime.ToString("MMdd");

        if (dateTime.Month == 1 && dateTime.Day < 20)
        {
            monthDay = "13" + dateTime.Day.ToString("00");
        }

        string[] atomBound =
        {
            "0120", "0219", "0321", "0420", "0521", "0622", "0723", "0823", "0923", "1024", "1123", "1222", "1320"
        };
        string[] atoms = {"水瓶座", "双鱼座", "白羊座", "金牛座", "双子座", "巨蟹座", "狮子座", "处女座", "天秤座", "天蝎座", "射手座", "魔羯座"};

        string result = "未知";

        for (int i = 0; i < atomBound.Length - 1; i++)
        {
            if (string.Compare(atomBound[i], monthDay, StringComparison.Ordinal) > 1
                || string.Compare(atomBound[i + 1], monthDay, StringComparison.Ordinal) <= 0)
            {
                continue;
            }

            result = atoms[i];
            break;
        }

        return result;
    }

    /// <summary>
    /// 生命密码
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>生命密码</returns>
    public static int GetLifeCode(this DateTime dateTime)
    {
        if (dateTime < new DateTime(1900, 1, 1))
        {
            return -1;
        }

        int lifeNum = GetSum(dateTime.Year) + GetSum(dateTime.Month) + GetSum(dateTime.Day);

        while (lifeNum > 9)
        {
            lifeNum = GetSum(lifeNum);
        }

        return lifeNum;
    }

    private static int GetSum(int num)
    {
        char[] b = num.ToString().ToCharArray();
        return b.Sum(t => Convert.ToInt32(t.ToString()));
    }
}
